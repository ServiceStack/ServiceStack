using System;
using ServiceStack.Caching;
using ServiceStack.Web;

namespace ServiceStack
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
		{
			string serializedDto = HostContext.ContentTypes.SerializeToString(requestContext, dto);
			if (requestContext.CompressionType == null)
				return (object)serializedDto;

			byte[] compressedBytes = serializedDto.Compress(requestContext.CompressionType);
            return new CompressedResult(compressedBytes, requestContext.CompressionType, requestContext.ResponseContentType);
		}

		/// <summary>
		/// Overload for the <see cref="ContentCacheManager.Resolve"/> method returning the most
		/// optimized result based on the MimeType and CompressionType from the IRequestContext.
		/// </summary>
		public static object ToOptimizedResultUsingCache<T>(
			this IRequestContext requestContext, ICacheClient cacheClient, string cacheKey,
			Func<T> factoryFn)
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

	    /// <summary>
	    /// Store an entry in the IHttpRequest.Items Dictionary
	    /// </summary>
	    public static void SetItem(this IRequestContext requestContext, string key, object value)
	    {
	        if (requestContext == null) return;
	        requestContext.Get<IHttpRequest>().SetItem(key, value);
	    }

	    /// <summary>
	    /// Store an entry in the IHttpRequest.Items Dictionary
	    /// </summary>
	    public static void SetItem(this IHttpRequest httpReq, string key, object value)
	    {
	        if (httpReq == null) return;

	        httpReq.Items[key] = value;
	    }

	    /// <summary>
	    /// Get an entry from the IHttpRequest.Items Dictionary
	    /// </summary>
	    public static object GetItem(this IRequestContext requestContext, string key)
	    {
	        return requestContext == null ? null : requestContext.Get<IHttpRequest>().GetItem(key);
	    }

	    /// <summary>
	    /// Get an entry from the IHttpRequest.Items Dictionary
	    /// </summary>
	    public static object GetItem(this IHttpRequest httpReq, string key)
	    {
	        if (httpReq == null) return null;

	        object value;
	        httpReq.Items.TryGetValue(key, out value);
	        return value;
	    }
	}
}