using System;
using Funq;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Extensions
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
		public static object ToOptimizedResultUsingCache<T>(this IRequestContext requestContext, 
			ICacheClient cacheClient, Func<T> factoryFn, string cacheKey)
			where T : class
		{
			return ContentCacheManager.Resolve(
				factoryFn, requestContext.MimeType, requestContext.CompressionType, 
				cacheClient, cacheKey);
		}

		/// <summary>
		/// Overload for the <see cref="ContentCacheManager.Resolve"/> method returning the most
		/// optimized result based on the MimeType and CompressionType from the IRequestContext.
		/// The default ICacheClient registered in the AppHost will be used to resolve and store the results.
		/// </summary>
		public static object ToOptimizedResultUsingCache<T>(this IRequestContext requestContext,
			Func<T> factoryFn, string cacheKey)
			where T : class
		{
			var cacheClient = AppHostBase.Instance.Container.TryResolve<ICacheClient>();
			if (cacheClient == null)
			{
				throw new ResolutionException("An ICacheClient is required to be registered in AppHost");
			}

			return ContentCacheManager.Resolve(
					factoryFn, requestContext.MimeType, requestContext.CompressionType, 
					cacheClient, cacheKey);
		}
	}
}