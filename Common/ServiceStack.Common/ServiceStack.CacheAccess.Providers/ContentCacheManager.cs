using System;
using System.Collections.Generic;
using ServiceStack.Common.Web;

namespace ServiceStack.CacheAccess.Providers
{
	/// <summary>
	/// Provides static helper methods to serialize and compress results
	/// </summary>
	public static class ContentCacheManager
	{
		public static string[] CachedMimeTypeExtensions = new[] {
			".xml", ".js", ".jsv"
		};

		/// <summary>
		/// Returns a serialized dto based on the mime-type, created using the factoryFn().
		/// If the correct mimeType is found, it will return the cached result.
		/// If a compressionType is set it will return a compressed version of the result.
		/// 
		/// If no result is found, the dto is created by the factoryFn()
		/// The dto is set on the cacheClient => cacheKey
		///		e.g. dto => urn:user:1
		/// 
		/// The serialized dto is set on the cacheClient => cacheKey.mimeType 
		///		e.g. xmlDto => urn:user:1.xml
		/// 
		/// Finally, if a compressionType is specified, the compressed dto 
		/// is set on the cacheClient => cacheKey.mimeType.compressionType
		///		e.g. compressedXmlDto => urn:user:1.xml.gzip
		/// 
		/// </summary>
		/// <typeparam name="TResult">The type of the result.</typeparam>
		/// <param name="factoryFn">The factory fn.</param>
		/// <param name="mimeType">The mime type to serialize it to.</param>
		/// <param name="compressionType">Type of the compression. null means no compression</param>
		/// <param name="cacheClient">The cache client</param>
		/// <param name="cacheKey">The base cache key</param>
		/// <returns></returns>
		public static object Resolve<TResult>(
			Func<TResult> factoryFn,
			string mimeType,
			string compressionType,
			ICacheClient cacheClient,
			string cacheKey)
			where TResult : class
		{
			var cacheKeySerialized = GetSerializedCacheKey(cacheKey, mimeType);

			var doCompression = compressionType != null;

			var cacheKeySerializedZip = GetCompressedCacheKey(
				cacheKeySerialized, compressionType);

			if (doCompression)
			{
				var compressedResult = cacheClient.Get<byte[]>(cacheKeySerializedZip);
				if (compressedResult != null)
				{
					return new CompressedResult(
						compressedResult,
						compressionType,
						mimeType);
				}
			}
			else
			{
				var serializedResult = cacheClient.Get(cacheKeySerialized);
				if (serializedResult != null)
				{
					return serializedResult;
				}
			}

			var dto = factoryFn();

			cacheClient.Set(cacheKey, dto);
			
			var serializedDto = ContentSerializer<TResult>.ToSerializedString(
				dto, mimeType);

			cacheClient.Set(cacheKeySerialized, serializedDto);

			if (doCompression)
			{
				var compressedSerializedDto = ContentSerializer<TResult>.ToCompressedResult(
					serializedDto, compressionType);

				cacheClient.Set(cacheKeySerializedZip, cacheKeySerializedZip);

				return new CompressedResult(compressedSerializedDto, compressionType, mimeType);
			}

			return serializedDto;
		}

		public static string GetSerializedCacheKey(string cacheKey, string mimeType)
		{
			return cacheKey + MimeTypes.GetExtension(mimeType);
		}

		public static string GetCompressedCacheKey(
			string cacheKeySerialized, string compressionType)
		{
			return compressionType != null
			       	? cacheKeySerialized + "." + compressionType
			       	: null;
		}

		/// <summary>
		/// Clears all the serialized and compressed caches set 
		/// by the 'Resolve' method for the cacheKey provided
		/// </summary>
		/// <param name="cacheClient">The cache client.</param>
		/// <param name="cacheKeys">The cache keys.</param>
		public static void Clear(ICacheClient cacheClient, params string[] cacheKeys)
		{
			var allCacheKeys = new List<string>();

			foreach (var cacheKey in cacheKeys)
			{
				allCacheKeys.Add(cacheKey);
				foreach (var serializedExt in CachedMimeTypeExtensions)
				{
					var serializedCacheKey = GetSerializedCacheKey(cacheKey, serializedExt);
					allCacheKeys.Add(serializedCacheKey);
					allCacheKeys.Add(GetCompressedCacheKey(serializedCacheKey, CompressionTypes.Deflate));
					allCacheKeys.Add(GetCompressedCacheKey(serializedCacheKey, CompressionTypes.GZip));
				}
			}

			cacheClient.RemoveAll(cacheKeys);
		}


		/// <summary>
		/// Helper method on ContentSerializer to 'Resolve' the serialized/compressed
		/// result using the cacheClient and cacheKey provided
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="contentSerializer">The content serializer.</param>
		/// <param name="cacheClient">The cache client.</param>
		/// <param name="cacheKey">The cache key.</param>
		/// <returns></returns>
		public static object Resolve<T>(this ContentSerializer<T> contentSerializer,
				ICacheClient cacheClient, string cacheKey)
			where T : class
		{
			return Resolve(
				contentSerializer.FactoryFn,
				contentSerializer.MimeType,
				contentSerializer.CompressionType,
				cacheClient,
				cacheKey);
		}
	}
}