using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Auth;
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
            return request?.Headers.Get(headerName);
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
        /// Returns the optimized result for the IRequest. 
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

            using var ms = new MemoryStream();
            using var compressionStream = GetCompressionStream(ms, compressionType);
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

        /// <summary>
        /// Returns the optimized result for the IRequest. 
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

            using var ms = new MemoryStream();
            using var compressionStream = GetCompressionStream(ms, compressionType);
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

        private static Stream GetCompressionStream(Stream outputStream, string compressionType)
        {
            if (compressionType == CompressionTypes.Deflate)
                return StreamExt.DeflateProvider.DeflateStream(outputStream);
            if (compressionType == CompressionTypes.GZip)
                return StreamExt.GZipProvider.GZipStream(outputStream);

            throw new NotSupportedException(compressionType);
        }

        /// <summary>
        /// Returning the most optimized result based on the MimeType and CompressionType from the IRequest.
        /// </summary>
        public static object ToOptimizedResultUsingCache<T>(
            this IRequest req, ICacheClient cacheClient, string cacheKey, Func<T> factoryFn)
        {
            return req.ToOptimizedResultUsingCache(cacheClient, cacheKey, null, factoryFn);
        }

        /// <summary>
        /// Returning the most optimized result based on the MimeType and CompressionType from the IRequest.
        /// </summary>
        public static Task<object> ToOptimizedResultUsingCacheAsync<T>(
            this IRequest req, ICacheClientAsync cacheClient, string cacheKey, Func<T> factoryFn, CancellationToken token=default)
        {
            return req.ToOptimizedResultUsingCacheAsync(cacheClient, cacheKey, null, factoryFn, token);
        }

        /// <summary>
        /// Returning the most optimized result based on the MimeType and CompressionType from the IRequest.
        /// <param name="expireCacheIn">How long to cache for, null is no expiration</param>
        /// </summary>
        public static object ToOptimizedResultUsingCache<T>(
            this IRequest req, ICacheClient cacheClient, string cacheKey,
            TimeSpan? expireCacheIn, Func<T> factoryFn)
        {
            var cacheResult = cacheClient.ResolveFromCache(cacheKey, req);
            if (cacheResult != null)
                return cacheResult;

            cacheResult = cacheClient.Cache(cacheKey, factoryFn(), req, expireCacheIn);
            return cacheResult;
        }

        /// <summary>
        /// Returning the most optimized result based on the MimeType and CompressionType from the IRequest.
        /// <param name="expireCacheIn">How long to cache for, null is no expiration</param>
        /// </summary>
        public static async Task<object> ToOptimizedResultUsingCacheAsync<T>(
            this IRequest req, ICacheClientAsync cacheClient, string cacheKey,
            TimeSpan? expireCacheIn, Func<T> factoryFn, CancellationToken token=default)
        {
            var cacheResult = await cacheClient.ResolveFromCacheAsync(cacheKey, req, token).ConfigAwait();
            if (cacheResult != null)
                return cacheResult;

            cacheResult = await cacheClient.CacheAsync(cacheKey, factoryFn(), req, expireCacheIn, token).ConfigAwait();
            return cacheResult;
        }

        /// <summary>
        /// Store an entry in the IHttpRequest.Items Dictionary
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetItem(this IRequest httpReq, string key, object value)
        {
            if (httpReq == null) return;

            httpReq.Items[key] = value;
        }

        /// <summary>
        /// Get an entry from the IHttpRequest.Items Dictionary
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object GetItem(this IRequest httpReq, string key)
        {
            if (httpReq == null) 
                return null;

            httpReq.Items.TryGetValue(key, out var value);
            return value;
        }

#if NET45 || NET472
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
            if (httpReq == null) 
                return;

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

        public static void RegisterForDispose(this IRequest request, IDisposable disposable)
        {
            if (disposable == null)
                return;
#if NETSTANDARD2_0
            var netcoreReq = (Microsoft.AspNetCore.Http.HttpRequest) request.OriginalRequest;
            netcoreReq.HttpContext.Response.RegisterForDispose(disposable);
#else
            // IDisposable's in IRequest.Items are disposed in AppHost.OnEndRequest()
            var typeName = disposable.GetType().Name;
            var i = 0;
            var key = typeName;
            while (request.Items.ContainsKey(key))
            {
                key = typeName + (++i);
            }
            request.Items[key] = disposable;
#endif
        }

        public static async Task<SessionSourceResult> GetSessionFromSourceAsync(this IRequest request, 
            string userAuthId, Func<IAuthRepositoryAsync,IUserAuth,Task> validator, CancellationToken token=default)
        {
            IAuthSession session = null;
            IEnumerable<string> roles = null;
            IEnumerable<string> permissions = null;

            var userSessionSource = AuthenticateService.GetUserSessionSourceAsync();
            if (userSessionSource != null)
            {
                session = await userSessionSource.GetUserSessionAsync(userAuthId, token).ConfigAwait();
                if (session == null)
                    throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(request));

                roles = session.Roles;
                permissions = session.Permissions;
                return new SessionSourceResult(session, roles, permissions);
            }

            var userRepo = HostContext.AppHost.GetAuthRepositoryAsync(request);
#if NET472 || NETSTANDARD2_0
            await using (userRepo as IAsyncDisposable)
#else
            using (userRepo as IDisposable)
#endif
            {
                var userAuth = await userRepo.GetUserAuthAsync(userAuthId, token).ConfigAwait();
                if (userAuth == null)
                    throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(request));

                if (validator != null)
                    await validator(userRepo, userAuth).ConfigAwait();

                session = SessionFeature.CreateNewSession(request, SessionExtensions.CreateRandomSessionId());
                await session.PopulateSessionAsync(userAuth, userRepo, token).ConfigAwait();

                if (userRepo is IManageRolesAsync manageRoles && session.UserAuthId != null)
                {
                    roles = await manageRoles.GetRolesAsync(session.UserAuthId, token).ConfigAwait();
                    permissions = await manageRoles.GetPermissionsAsync(session.UserAuthId, token).ConfigAwait();
                }
                return new SessionSourceResult(session, roles, permissions);
            }
            
            return null;
        }
    }

    public class SessionSourceResult
    {
        public IAuthSession Session { get; }
        public IEnumerable<string> Roles { get; }
        public IEnumerable<string> Permissions { get; }
        public SessionSourceResult(IAuthSession session, IEnumerable<string> roles, IEnumerable<string> permissions)
        {
            Session = session;
            Roles = roles;
            Permissions = permissions;
        }
    }
    
    public static class RequestUtils
    {
        public static async Task AssertAccessRoleOrDebugModeAsync(IRequest req, string accessRole=null, string authSecret=null, CancellationToken token=default)
        {
            if (!HostContext.DebugMode)
            {
                if (HostContext.Config.AdminAuthSecret == null || HostContext.Config.AdminAuthSecret != authSecret)
                {
                    await RequiredRoleAttribute.AssertRequiredRoleAsync(req, accessRole, token);
                }
            }
        }

        public static async Task AssertAccessRoleAsync(IRequest req, string accessRole=null, string authSecret=null, CancellationToken token=default)
        {
            if (HostContext.Config.AdminAuthSecret == null || HostContext.Config.AdminAuthSecret != authSecret)
            {
                await RequiredRoleAttribute.AssertRequiredRoleAsync(req, accessRole, token);
            }
        }
    }

    // Share same buffered impl/behavior across all Hosts
    internal static class BufferedExtensions
    {
        internal static MemoryStream CreateBufferedStream(this IResponse response)
        {
            return MemoryStreamFactory.GetStream();
        }

        internal static MemoryStream CreateBufferedStream(this Stream stream)
        {
            return stream.CopyToNewMemoryStream();
        }

        internal static string ReadBufferedStreamToEnd(this MemoryStream stream, IRequest req)
        {
            return req.GetInputStream(stream).ReadToEnd();
        }

        internal static void FlushBufferIfAny(this IResponse response, MemoryStream buffer, Stream output)
        {
            if (buffer == null)
                return;

            try {
                response.SetContentLength(buffer.Length); //safe to set Length in Buffered Response
            } catch {}

            buffer.WriteTo(output);
            buffer.SetLength(buffer.Position = 0); //reset
        }

        internal static async Task FlushBufferIfAnyAsync(this IResponse response, MemoryStream buffer, Stream output, CancellationToken token=default(CancellationToken))
        {
            if (buffer == null)
                return;

            try {
                response.SetContentLength(buffer.Length); //safe to set Length in Buffered Response
            } catch {}

            await buffer.WriteToAsync(output, token: token);
            buffer.SetLength(buffer.Position = 0); //reset
        }
    }
}