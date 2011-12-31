using System;
using System.Linq;
using System.Collections.Generic;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.CacheAccess.Providers
{
	/// <summary>
	/// Provides static helper methods to serialize and compress results
	/// </summary>
	public static class ContentCacheManager
	{
		private static IContentTypeFilter contentTypeFilter;
		public static IContentTypeFilter ContentTypeFilter
		{
			get
			{
				return contentTypeFilter;
			}
			set
			{
				contentTypeFilter = value;
				var contentTypes = new HashSet<string>(AllCachedContentTypes);
				foreach (var contentType in contentTypeFilter.ContentTypeFormats.Values)
				{
					contentTypes.Add(contentType);
				}
				AllCachedContentTypes = contentTypes.ToArray();
			}
		}

		public static string[] AllCachedContentTypes = new[] {
			ContentType.XmlText, ContentType.JsonText, ContentType.JsvText
		};

		/// <summary>
		/// Returns a serialized dto based on the mime-type, created using the factoryFn().
		/// If the correct mimeType is found, it will return the cached result.
		/// If a compressionType is set it will return a compressed version of the result.
		/// If no result is found, the dto is created by the factoryFn()
		/// The dto is set on the cacheClient[cacheKey] =&gt; factoryFn()
		/// e.g. urn:user:1 =&gt; dto
		/// The serialized dto is set on the cacheClient[cacheKey.mimeType] =&gt; serialized(factoryFn())
		/// e.g. urn:user:1.xml =&gt; xmlDto
		/// Finally, if a compressionType is specified, the compressed dto is set on the
		/// cacheClient[cacheKey.mimeType.compressionType] =&gt; compressed(serialized(factoryFn()))
		/// e.g. urn:user:1.xml.gzip =&gt; compressedXmlDto
		/// </summary>
		/// <typeparam name="TResult">The type of the result.</typeparam>
		/// <param name="factoryFn">The factory fn.</param>
		/// <param name="serializationContext">The serialization context.</param>
		/// <param name="compressionType">Type of the compression. -optional null means no compression</param>
		/// <param name="cacheClient">The cache client</param>
		/// <param name="cacheKey">The base cache key</param>
		/// <param name="expireCacheIn">How long to cache for, default is no expiration</param>
		/// <returns></returns>
		public static object Resolve<TResult>(
			Func<TResult> factoryFn,
			IRequestContext serializationContext,
			ICacheClient cacheClient,
			string cacheKey,
			TimeSpan? expireCacheIn)
			where TResult : class
		{
			factoryFn.ThrowIfNull("factoryFn");
			serializationContext.ThrowIfNull("mimeType");
			cacheClient.ThrowIfNull("cacheClient");
			cacheKey.ThrowIfNull("cacheKey");

			var contentType = serializationContext.ResponseContentType;
			var compressionType = serializationContext.CompressionType;
			string modifiers = null, jsonp = null;

			if (contentType == ContentType.Json)
			{
				jsonp = serializationContext.Get<IHttpRequest>().GetJsonpCallback();
				if (jsonp != null)
					modifiers = ".jsonp," + jsonp.SafeVarName();
			}

			var cacheKeySerialized = GetSerializedCacheKey(cacheKey, contentType, modifiers);

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
						contentType);
				}
			}
			else
			{
				var serializedResult = cacheClient.Get<string>(cacheKeySerialized);
				if (serializedResult != null)
				{
					return serializedResult;
				}
			}

			var dto = factoryFn();

			CacheSet(cacheClient, cacheKey, dto, expireCacheIn);

			var serializedDto = ContentSerializer<TResult>.ToSerializedString(dto, serializationContext);

			if (jsonp != null)
				serializedDto = jsonp + "(" + serializedDto + ")";

			CacheSet(cacheClient, cacheKeySerialized, serializedDto, expireCacheIn);

			if (doCompression)
			{
				var compressedSerializedDto = ContentSerializer<TResult>.ToCompressedBytes(
					serializedDto, compressionType);

				CacheSet(cacheClient, cacheKeySerializedZip, compressedSerializedDto, expireCacheIn);

				return (compressedSerializedDto != null)
					? new CompressedResult(compressedSerializedDto, compressionType, contentType)
					: null;
			}

			return serializedDto;
		}

		public static void CacheSet<T>(ICacheClient cacheClient, string cacheKey, T result, TimeSpan? expireCacheIn)
		{
			if (expireCacheIn.HasValue)
			{
				cacheClient.Set(cacheKey, result, expireCacheIn.Value);
			}
			else
			{
				cacheClient.Set(cacheKey, result);
			}
		}

		public static string GetSerializedCacheKey(string cacheKey, string mimeType, string modifiers)
		{
			return cacheKey + MimeTypes.GetExtension(mimeType) + modifiers;
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
				foreach (var serializedExt in AllCachedContentTypes)
				{
					var serializedCacheKey = GetSerializedCacheKey(cacheKey, serializedExt, null);
					allCacheKeys.Add(serializedCacheKey);

					foreach (var compressionType in CompressionTypes.AllCompressionTypes)
					{
						allCacheKeys.Add(GetCompressedCacheKey(serializedCacheKey, compressionType));
					}
				}
			}

			cacheClient.RemoveAll(allCacheKeys);
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
		public static object ResolveFromCache<T>(this ContentSerializer<T> contentSerializer,
				ICacheClient cacheClient, string cacheKey)
			where T : class
		{
			var serializationContext = new SerializationContext(contentSerializer.ContentType)
			{
                CompressionType = contentSerializer.CompressionType
			};

			return Resolve(
				contentSerializer.FactoryFn,
				serializationContext,
				cacheClient,
				cacheKey,
				null);
		}
	}
}