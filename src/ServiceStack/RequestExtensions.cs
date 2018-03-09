using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.IO;
using ServiceStack.Text;
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

        public static string GetContentEncoding(this IRequest request)
        {
            return request.Headers.Get(HttpHeaders.ContentEncoding);
        }

        public static Stream GetInputStream(this IRequest req, Stream stream)
        {
            var enc = req.GetContentEncoding();
            if (enc == CompressionTypes.Deflate)
                return new DeflateStream(stream, CompressionMode.Decompress);
            if (enc == CompressionTypes.GZip)
                return new GZipStream(stream, CompressionMode.Decompress);

            return stream;
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
        [Obsolete("Use ToOptimizedResultAsync")]
        public static object ToOptimizedResult(this IRequest request, object dto)
        {
            var httpResult = dto as IHttpResult;
            if (httpResult != null)
                dto = httpResult.Response;
            
            request.Response.Dto = dto;

            var compressionType = request.GetCompressionType();
            if (compressionType == null)
                return HostContext.ContentTypes.SerializeToString(request, dto);

            using (var ms = new MemoryStream())
            using (var compressionStream = GetCompressionStream(ms, compressionType))
            {
                using (httpResult?.ResultScope?.Invoke())
                {
                    using (var msBuffer = MemoryStreamFactory.GetStream())
                    {
                        HostContext.ContentTypes.SerializeToStreamAsync(request, dto, msBuffer).Wait();
                        msBuffer.Position = 0;
                        msBuffer.CopyTo(compressionStream);
                    }
                    compressionStream.Close();
                }

                var compressedBytes = ms.ToArray();
                return new CompressedResult(compressedBytes, compressionType, request.ResponseContentType)
                {
                    Status = request.Response.StatusCode
                };
            }
        }

        /// <summary>
        /// Returns the optimized result for the IRequestContext. 
        /// Does not use or store results in any cache.
        /// </summary>
        public static async Task<object> ToOptimizedResultAsync(this IRequest request, object dto)
        {
            var httpResult = dto as IHttpResult;
            if (httpResult != null)
                dto = httpResult.Response;

            request.Response.Dto = dto;

            var compressionType = request.GetCompressionType();
            if (compressionType == null)
                return HostContext.ContentTypes.SerializeToString(request, dto);

            using (var ms = new MemoryStream())
            using (var compressionStream = GetCompressionStream(ms, compressionType))
            {
                using (httpResult?.ResultScope?.Invoke())
                {
                    await HostContext.ContentTypes.SerializeToStreamAsync(request, dto, compressionStream);
                    compressionStream.Close();
                }

                var compressedBytes = ms.ToArray();
                return new CompressedResult(compressedBytes, compressionType, request.ResponseContentType)
                {
                    Status = request.Response.StatusCode
                };
            }
        }

        private static Stream GetCompressionStream(Stream outputStream, string compressionType)
        {
            if (compressionType == CompressionTypes.Deflate)
                return StreamExt.DeflateProvider.DeflateStream(outputStream);
            if (compressionType == CompressionTypes.GZip)
                return StreamExt.GZipProvider.GZipStream(outputStream);

            throw new NotSupportedException(compressionType);
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

            httpReq.Items.TryGetValue(key, out var value);
            return value;
        }

#if !NETSTANDARD2_0
        public static RequestBaseWrapper ToHttpRequestBase(this IRequest httpReq)
        {
            return new RequestBaseWrapper((IHttpRequest)httpReq);
        }
#endif

        public static void SetInProcessRequest(this IRequest httpReq)
        {
            if (httpReq == null) return;

            httpReq.RequestAttributes |= RequestAttributes.InProcess;
        }

        public static bool IsInProcessRequest(this IRequest httpReq)
        {
            return (RequestAttributes.InProcess & httpReq?.RequestAttributes) == RequestAttributes.InProcess;
        }

        public static void ReleaseIfInProcessRequest(this IRequest httpReq)
        {
            if (httpReq == null) return;

            httpReq.RequestAttributes = httpReq.RequestAttributes & ~RequestAttributes.InProcess;
        }

        internal static T TryResolveInternal<T>(this IRequest request)
        {
            if (typeof(T) == typeof(IRequest))
                return (T)request;
            if (typeof(T) == typeof(IResponse))
                return (T)request.Response;

            return request is IHasResolver hasResolver 
                ? hasResolver.Resolver.TryResolve<T>() 
                : Service.GlobalResolver.TryResolve<T>();
        }

        public static IVirtualFile GetFile(this IRequest request) => request is IHasVirtualFiles vfs ? vfs.GetFile() : null;
        public static IVirtualDirectory GetDirectory(this IRequest request) => request is IHasVirtualFiles vfs ? vfs.GetDirectory() : null;
        public static bool IsFile(this IRequest request) => request is IHasVirtualFiles vfs && vfs.IsFile;
        public static bool IsDirectory(this IRequest request) => request is IHasVirtualFiles vfs && vfs.IsDirectory;

        public static T GetRuntimeConfig<T>(this IRequest req, string name, T defaultValue)
        {
            return req != null 
                ? HostContext.AppHost.GetRuntimeConfig(req, name, defaultValue)
                : defaultValue;
        }
    }
}