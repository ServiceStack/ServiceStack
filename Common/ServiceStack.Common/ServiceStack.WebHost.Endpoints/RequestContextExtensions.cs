using System;
using Funq;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints
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
			return ContentSerializer<T>.ToOptimizedResult(
				requestContext.MimeType, requestContext.CompressionType, dto);
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
			return ContentCacheManager.Resolve(
				factoryFn, requestContext.MimeType, requestContext.CompressionType,
				cacheClient, cacheKey, null);
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
			return ContentCacheManager.Resolve(
				factoryFn, requestContext.MimeType, requestContext.CompressionType,
				cacheClient, cacheKey, expireCacheIn);
		}

		/// <summary>
		/// Overload for the <see cref="ContentCacheManager.Resolve"/> method returning the most
		/// optimized result based on the MimeType and CompressionType from the IRequestContext.
		/// The default ICacheClient registered in the AppHost will be used.
		/// </summary>
		public static object ToOptimizedResultUsingCache<T>(this IRequestContext requestContext, 
			string cacheKey, Func<T> factoryFn)
			where T : class
		{
			var cacheClient = GetDefaultCacheClient();

			return ContentCacheManager.Resolve(
				factoryFn, requestContext.MimeType, requestContext.CompressionType, 
				cacheClient, cacheKey, null);
		}

		private static ICacheClient GetDefaultCacheClient()
		{
			var cacheClient = AppHostBase.Instance.Container.TryResolve<ICacheClient>();
			if (cacheClient == null)
			{
				throw new ResolutionException("An ICacheClient is required to be registered in AppHost");
			}
			return cacheClient;
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
			ContentCacheManager.Clear(cacheClient, cacheKeys);
		}

		/// <summary>
		/// Clears all the serialized and compressed caches set
		/// by the 'Resolve' method for the cacheKey provided.
		/// The default ICacheClient registered in the AppHost will be used.
		/// </summary>
		/// <param name="requestContext">The request context.</param>
		/// <param name="cacheKeys">The cache keys.</param>
		public static void RemoveFromCache(
			this IRequestContext requestContext, params string[] cacheKeys)
		{
			var cacheClient = GetDefaultCacheClient();
			ContentCacheManager.Clear(cacheClient, cacheKeys);
		}
	}
}