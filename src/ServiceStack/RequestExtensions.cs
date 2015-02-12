using System;
using ServiceStack.Caching;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack
{
	public static class RequestExtensions
	{
        public static AuthUserSession ReloadSession(this IRequest request)
        {
            return request.GetSession() as AuthUserSession;
        }

        public static string GetCompressionType(this IRequest request)
        {
            if (request.RequestPreferences.AcceptsDeflate)
                return CompressionTypes.Deflate;

            if (request.RequestPreferences.AcceptsGzip)
                return CompressionTypes.GZip;

            return null;
        }

        public static string GetHeader(this IRequest request, string headerName)
        {
            return request.Headers.Get(headerName);
        }

        public static string GetParamInRequestHeader(this IRequest request, string name)
        {
            //Avoid reading request body for non x-www-form-urlencoded requests
            return request.Headers[name]
                ?? request.QueryString[name]
                ?? (!HostContext.Config.SkipFormDataInCreatingRequest && request.ContentType.MatchesContentType(MimeTypes.FormUrlEncoded)
                        ? request.FormData[name]
                        : null);
        }

		/// <summary>
		/// Returns the optimized result for the IRequestContext. 
		/// Does not use or store results in any cache.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="dto"></param>
		/// <returns></returns>
		public static object ToOptimizedResult<T>(this IRequest request, T dto)
		{
		    request.Response.Dto = dto;

			string serializedDto = HostContext.ContentTypes.SerializeToString(request, dto);
		    var compressionType = request.GetCompressionType();
		    if (compressionType == null)
				return (object)serializedDto;

            byte[] compressedBytes = serializedDto.Compress(compressionType);
            return new CompressedResult(compressedBytes, compressionType, request.ResponseContentType) {
                Status = request.Response.StatusCode
            };
		}

		/// <summary>
		/// Overload for the <see cref="ContentCacheManager.Resolve"/> method returning the most
		/// optimized result based on the MimeType and CompressionType from the IRequestContext.
		/// </summary>
		public static object ToOptimizedResultUsingCache<T>(
			this IRequest requestContext, ICacheClient cacheClient, string cacheKey,
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
			this IRequest requestContext, ICacheClient cacheClient, string cacheKey,
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
			this IRequest requestContext, ICacheClient cacheClient, params string[] cacheKeys)
		{
			cacheClient.ClearCaches(cacheKeys);
		}

	    /// <summary>
	    /// Store an entry in the IHttpRequest.Items Dictionary
	    /// </summary>
	    public static void SetItem(this IRequest httpReq, string key, object value)
	    {
	        if (httpReq == null) return;

	        httpReq.Items[key] = value;
	    }

	    /// <summary>
	    /// Get an entry from the IHttpRequest.Items Dictionary
	    /// </summary>
	    public static object GetItem(this IRequest httpReq, string key)
	    {
	        if (httpReq == null) return null;

	        object value;
	        httpReq.Items.TryGetValue(key, out value);
	        return value;
	    }

        public static RequestBaseWrapper ToHttpRequestBase(this IRequest httpReq)
        {
            return new RequestBaseWrapper((IHttpRequest) httpReq);
        }
    }
}