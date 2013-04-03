using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
using ServiceStack.Common.Web;
using ServiceStack.Common.Extensions;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.CacheAccess.Providers
{
	public static class CacheClientExtensions
	{
		public static void Set<T>(this ICacheClient cacheClient, string cacheKey, T value, TimeSpan? expireCacheIn)
		{
			if (expireCacheIn.HasValue)
				cacheClient.Set(cacheKey, value, expireCacheIn.Value);
			else
				cacheClient.Set(cacheKey, value);
		}

		public static object ResolveFromCache(this ICacheClient cacheClient, 
			string cacheKey, 
			IRequestContext context)
		{
			string modifiers = null;
            if (!context.ResponseContentType.IsBinary())
            {

                if (context.ResponseContentType == ContentType.Json)
                {
                    string jsonp = context.Get<IHttpRequest>().GetJsonpCallback();
                    if (jsonp != null)
                        modifiers = ".jsonp," + jsonp.SafeVarName();
                }

                var cacheKeySerialized = GetCacheKeyForSerialized(cacheKey, context.ResponseContentType, modifiers);

                bool doCompression = context.CompressionType != null;
                if (doCompression)
                {
                    var cacheKeySerializedZip = GetCacheKeyForCompressed(cacheKeySerialized, context.CompressionType);

                    var compressedResult = cacheClient.Get<byte[]>(cacheKeySerializedZip);
                    if (compressedResult != null)
                    {
                        return new CompressedResult(
                            compressedResult,
                            context.CompressionType,
                            context.ResponseContentType);
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
            }
            else
            {
                var cacheKeySerialized = GetCacheKeyForSerialized(cacheKey, context.ResponseContentType, modifiers);
                var serializedResult = cacheClient.Get<byte[]>(cacheKeySerialized);
                if (serializedResult != null)
                {
                    return serializedResult;
                }
            }

		    return null;
		}

		public static object Cache(this ICacheClient cacheClient, 
			string cacheKey, 
			object responseDto, 
			IRequestContext context, 
			TimeSpan? expireCacheIn = null)
		{
			cacheClient.Set(cacheKey, responseDto, expireCacheIn);

            if (!context.ResponseContentType.IsBinary())
            {
                string serializedDto = EndpointHost.ContentTypeFilter.SerializeToString(context, responseDto);

                string modifiers = null;
                if (context.ResponseContentType.MatchesContentType(ContentType.Json))
                {
                    var jsonp = context.Get<IHttpRequest>().GetJsonpCallback();
                    if (jsonp != null)
                    {
                        modifiers = ".jsonp," + jsonp.SafeVarName();
                        serializedDto = jsonp + "(" + serializedDto + ")";

                        //Add a default expire timespan for jsonp requests,
                        //because they aren't cleared when calling ClearCaches()
                        if (expireCacheIn == null)
                            expireCacheIn = EndpointHost.Config.DefaultJsonpCacheExpiration;
                    }
                }

                var cacheKeySerialized = GetCacheKeyForSerialized(cacheKey, context.ResponseContentType, modifiers);
                cacheClient.Set(cacheKeySerialized, serializedDto, expireCacheIn);

                bool doCompression = context.CompressionType != null;
                if (doCompression)
                {
                    var cacheKeySerializedZip = GetCacheKeyForCompressed(cacheKeySerialized, context.CompressionType);

                    byte[] compressedSerializedDto = Common.StreamExtensions.Compress(serializedDto, context.CompressionType);
                    cacheClient.Set(cacheKeySerializedZip, compressedSerializedDto, expireCacheIn);

                    return (compressedSerializedDto != null)
                        ? new CompressedResult(compressedSerializedDto, context.CompressionType, context.ResponseContentType)
                        : null;
                }

                return serializedDto;
            }
            else
            {
                string modifiers = null;
                byte[] serializedDto = EndpointHost.ContentTypeFilter.SerializeToBytes(context, responseDto);
                var cacheKeySerialized = GetCacheKeyForSerialized(cacheKey, context.ResponseContentType, modifiers);
                cacheClient.Set(cacheKeySerialized, serializedDto, expireCacheIn);
                return serializedDto;
            }
		}

		public static void ClearCaches(this ICacheClient cacheClient, params string[] cacheKeys)
		{
			var allContentTypes = new List<string>(EndpointHost.ContentTypeFilter.ContentTypeFormats.Values)
			{ ContentType.XmlText, ContentType.JsonText, ContentType.JsvText };

			var allCacheKeys = new List<string>();

			foreach (var cacheKey in cacheKeys)
			{
				allCacheKeys.Add(cacheKey);
				foreach (var serializedExt in allContentTypes)
				{
					var serializedCacheKey = GetCacheKeyForSerialized(cacheKey, serializedExt, null);
					allCacheKeys.Add(serializedCacheKey);

					foreach (var compressionType in CompressionTypes.AllCompressionTypes)
					{
						allCacheKeys.Add(GetCacheKeyForCompressed(serializedCacheKey, compressionType));
					}
				}
			}

			cacheClient.RemoveAll(allCacheKeys);
		}

		public static string GetCacheKeyForSerialized(string cacheKey, string mimeType, string modifiers)
		{
			return cacheKey + MimeTypes.GetExtension(mimeType) + modifiers;
		}

		public static string GetCacheKeyForCompressed(string cacheKeySerialized, string compressionType)
		{
			return cacheKeySerialized + "." + compressionType;
		}

		/// <summary>
		/// Removes items from cache that have keys matching the specified wildcard pattern
		/// </summary>
		/// <param name="cacheClient">Cache client</param>
		/// <param name="pattern">The wildcard, where "*" means any sequence of characters and "?" means any single character.</param>
		public static void RemoveByPattern(this ICacheClient cacheClient, string pattern)
		{
			var canRemoveByPattern = cacheClient as IRemoveByPattern;
			if (canRemoveByPattern == null)
				throw new NotImplementedException("ICacheRemovableByPattern is not implemented by the cache client: " + cacheClient.GetType().FullName);

			canRemoveByPattern.RemoveByPattern(pattern);
		}
		/// <summary>
		/// Removes items from the cache based on the specified regular expression pattern
		/// </summary>
		/// <param name="cacheClient">Cache client</param>
		/// <param name="regex">Regular expression pattern to search cache keys</param>
		public static void RemoveByRegex(this ICacheClient cacheClient, string regex)
		{
			var canRemoveByPattern = cacheClient as IRemoveByPattern;
			if (canRemoveByPattern == null)
				throw new NotImplementedException("ICacheRemovableByPattern is not implemented by the cache client: " + cacheClient.GetType().FullName);

			canRemoveByPattern.RemoveByRegex(regex);
		}
	}
}
