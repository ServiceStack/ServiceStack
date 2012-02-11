using System;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Common;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.Common.Web;

namespace ServiceStack.ServiceHost
{
	public static class RequestContextExtensions
	{
		/// <summary>
		/// Returns the optimized result for the IRequestContext. 
		/// Does not use or store results in any cache.
		/// </summary>
		/// <param name="requestContext"></param>
		/// <param name="dto"></param>
		/// <returns></returns>
		public static object ToOptimizedResult<T>(this IRequestContext requestContext, T dto) 
			where T : class
		{
			string serializedDto = EndpointHost.ContentTypeFilter.SerializeToString(requestContext, dto);
			if (requestContext.CompressionType == null)
				return (object)serializedDto;

			byte[] compressedBytes = StreamExtensions.Compress(serializedDto, requestContext.CompressionType);
			return new CompressedResult(compressedBytes, requestContext.CompressionType, requestContext.ContentType);
		}

		/// <summary>
		/// Overload for the <see cref="ContentCacheManager.Resolve"/> method returning the most
		/// optimized result based on the MimeType and CompressionType from the IRequestContext.
		/// </summary>
		public static object ToOptimizedResultUsingCache<T>(
			this IRequestContext requestContext, ICacheClient cacheClient, string cacheKey,
			Func<T> factoryFn)
			where T : class
		{
			return requestContext.ToOptimizedResultUsingCache(cacheClient, cacheKey, null, factoryFn);
		}

		/// <summary>
		/// Overload for the <see cref="ContentCacheManager.Resolve"/> method returning the most
		/// optimized result based on the MimeType and CompressionType from the IRequestContext.
		/// <param name="expireCacheIn">How long to cache for, null is no expiration</param>
		/// </summary>
		public static object ToOptimizedResultUsingCache<T>(
			this IRequestContext requestContext, ICacheClient cacheClient, string cacheKey,
			TimeSpan? expireCacheIn, Func<T> factoryFn)
			where T : class
		{
			var cacheResult = cacheClient.ResolveFromCache(cacheKey, requestContext);
			if (cacheResult != null)
				return cacheResult;

			cacheResult = cacheClient.Cache(cacheKey, factoryFn(), requestContext, expireCacheIn);
			return cacheResult;
		}

		/// <summary>
		/// Clears all the serialized and compressed caches set 
		/// by the 'Resolve' method for the cacheKey provided
		/// </summary>
		/// <param name="requestContext"></param>
		/// <param name="cacheClient"></param>
		/// <param name="cacheKeys"></param>
		public static void RemoveFromCache(
			this IRequestContext requestContext, ICacheClient cacheClient, params string[] cacheKeys)
		{
			cacheClient.ClearCaches(cacheKeys);
		}

	}
}