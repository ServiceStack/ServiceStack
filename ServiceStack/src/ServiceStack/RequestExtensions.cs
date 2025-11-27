using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

public static class RequestExtensions
{
    public static AuthUserSession ReloadSession(this IRequest request)
    {
        return request.GetSession() as AuthUserSession;
    }

    public static string GetCompressionType(this IRequest request) =>
        HostContext.AssertAppHost().GetCompressionType(request);

    public static IStreamCompressor GetCompressor(this IRequest request)
    {
        var encoding = request.GetCompressionType();
        return encoding != null ? StreamCompressors.Get(encoding) : null;
    }

    public static string GetContentEncoding(this IRequest request)
    {
        return request.Headers.Get(HttpHeaders.ContentEncoding);
    }

    public static Stream GetInputStream(this IRequest req, Stream stream)
    {
        var enc = req.GetContentEncoding();
        var compressor = enc != null
            ? StreamCompressors.Get(enc)
            : null;
        if (compressor != null)
            return compressor.Decompress(stream);

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

        var compressor = request.GetCompressor();
        if (compressor == null)
            return HostContext.ContentTypes.SerializeToString(request, dto);

        using var ms = new MemoryStream();
        using var compressionStream = compressor.Compress(ms);
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
        return new CompressedResult(compressedBytes, compressor.Encoding, request.ResponseContentType) {
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

        var compressor = request.GetCompressor();
        if (compressor == null)
            return HostContext.ContentTypes.SerializeToString(request, dto);

        using var ms = new MemoryStream();
        using var compressionStream = compressor.Compress(ms);
        using (httpResult?.ResultScope?.Invoke())
        {
            await HostContext.ContentTypes.SerializeToStreamAsync(request, dto, compressionStream);
            compressionStream.Close();
        }

        var compressedBytes = ms.ToArray();
        return new CompressedResult(compressedBytes, compressor.Encoding, request.ResponseContentType)
        {
            Status = request.Response.StatusCode
        };
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
    /// <param name="req"></param>
    /// <param name="cacheClient"></param>
    /// <param name="cacheKey"></param>
    /// <param name="expireCacheIn">How long to cache for, null is no expiration</param>
    /// <param name="factoryFn"></param>
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
    /// <param name="req"></param>
    /// <param name="cacheClient"></param>
    /// <param name="cacheKey"></param>
    /// <param name="expireCacheIn">How long to cache for, null is no expiration</param>
    /// <param name="factoryFn"></param>
    /// <param name="token"></param>
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
    /// Returning the most optimized result based on the MimeType and CompressionType from the IRequest.
    /// <param name="req"></param>
    /// <param name="cacheClient"></param>
    /// <param name="cacheKey"></param>
    /// <param name="expireCacheIn">How long to cache for, null is no expiration</param>
    /// <param name="factoryFn"></param>
    /// <param name="token"></param>
    /// </summary>
    public static async Task<object> ToOptimizedResultUsingCacheAsync<T>(
        this IRequest req, ICacheClientAsync cacheClient, string cacheKey,
        TimeSpan? expireCacheIn, Func<Task<T>> factoryFn, CancellationToken token=default)
    {
        var cacheResult = await cacheClient.ResolveFromCacheAsync(cacheKey, req, token).ConfigAwait();
        if (cacheResult != null)
            return cacheResult;

        var responseDto = await factoryFn().ConfigAwait();
        cacheResult = await cacheClient.CacheAsync(cacheKey, responseDto, req, expireCacheIn, token).ConfigAwait();
        return cacheResult;
    }

    /// <summary>
    /// Store an entry in the IHttpRequest.Items Dictionary
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetItem(this IRequest httpReq, string key, object value)
    {
        httpReq?.Items[key] = value;
    }

    /// <summary>
    /// Mark an entry in the IRequest.Items Dictionary as true
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetTrue(this IRequest httpReq, string key)
    {
        httpReq?.Items[key] = bool.TrueString;
    }

    /// <summary>
    /// Mark an entry in the IResponse.Items Dictionary as true
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetTrue(this IResponse httpRes, string key)
    {
        httpRes?.Items[key] = bool.TrueString;
    }

    /// <summary>
    /// Whether an entry exists in the IRequest.Items Dictionary
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSet(this IRequest httpReq, string key)
    {
        return httpReq != null && httpReq.Items.ContainsKey(key);
    }

    /// <summary>
    /// Whether an entry exists in the IResponse.Items Dictionary
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSet(this IResponse httpRes, string key)
    {
        return httpRes != null && httpRes.Items.ContainsKey(key);
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

#if NETFX || NET472
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

        var instance = request is IHasResolver hasResolver 
            ? hasResolver.Resolver.TryResolve<T>() 
            : Service.GlobalResolver.TryResolve<T>();
        if (instance is IRequiresRequest requiresRequest)
            requiresRequest.Request ??= request;
        return instance;
    }
    internal static object TryResolveInternal(this IRequest request, Type type)
    {
        if (type == typeof(IRequest))
            return request;
        if (type == typeof(IResponse))
            return request.Response;
        return request.GetService(type);
    }

    public static IVirtualFile GetFile(this IRequest request) => request is IHasVirtualFiles vfs ? vfs.GetFile() : null;
    public static IVirtualDirectory GetDirectory(this IRequest request) => request is IHasVirtualFiles vfs ? vfs.GetDirectory() : null;
    public static bool IsFile(this IRequest request) => request is IHasVirtualFiles { IsFile: true };
    public static bool IsDirectory(this IRequest request) => request is IHasVirtualFiles { IsDirectory: true };

    public static IVirtualFiles GetVirtualFiles(this IRequest request) => HostContext.VirtualFiles;
    public static IVirtualPathProvider GetVirtualFileSources(this IRequest request) => HostContext.VirtualFileSources;

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
#if NETCORE
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
            request.SetItem(key, disposable);
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
            session = await userSessionSource.GetUserSessionAsync(userAuthId, request, token).ConfigAwait();
            if (session == null)
                throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(request));

            roles = session.Roles;
            permissions = session.Permissions;
            return new SessionSourceResult(session, roles, permissions);
        }

        var userRepo = HostContext.AppHost.GetAuthRepositoryAsync(request);
        await using (userRepo as IAsyncDisposable)
        {
            var userAuth = await userRepo.GetUserAuthAsync(userAuthId, token).ConfigAwait();
            if (userAuth == null)
                throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(request));

            if (validator != null)
                await validator(userRepo, userAuth).ConfigAwait();

            session = SessionFeature.CreateNewSession(request, HostContext.AppHost.CreateSessionId());
            await session.PopulateSessionAsync(userAuth, userRepo, token).ConfigAwait();

            if (userRepo is IManageRolesAsync manageRoles && session.UserAuthId != null)
            {
                roles = await manageRoles.GetRolesAsync(session.UserAuthId, token).ConfigAwait();
                permissions = await manageRoles.GetPermissionsAsync(session.UserAuthId, token).ConfigAwait();
            }
            return new SessionSourceResult(session, roles, permissions);
        }
    }

    public static string GetTraceId(this IRequest req)
    {
        if (req is IHasTraceId hasTraceId)
            return hasTraceId.TraceId;
        if (req.Items.TryGetValue(Keywords.TraceId, out var traceId))
            return (string)traceId;
        var newId = Guid.NewGuid().ToString();
        req.SetItem(Keywords.TraceId, newId);
        return newId;
    }

    private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
    public static TimeSpan GetElapsed(this IRequest req)
    {
        var oLong = req.GetItem(Keywords.RequestDuration);
        var currentTimestamp = Stopwatch.GetTimestamp();
        return oLong is long startTimestamp
            ? new TimeSpan((long)(TimestampToTicks * (currentTimestamp - startTimestamp))) 
            : TimeSpan.Zero;
    }

    public static bool AllowConnection(this IRequest req, bool requireSecureConnection) =>
        !requireSecureConnection || req.IsSecureConnection || req.RequestAttributes.HasFlag(RequestAttributes.MessageQueue) || req.IsInProcessRequest();

    public static void CompletedAuthentication(this IRequest req)
    {
        req.SetTrue(Keywords.DidAuthenticate);
    }

    public static Dictionary<string, string> GetRequestParams(this IRequest request) =>
        GetRequestParams(request, HostContext.AppHost?.Config.IgnoreWarningsOnPropertyNames);
            
    /// <summary>
    /// Duplicate Params are given a unique key by appending a #1 suffix
    /// </summary>
    public static Dictionary<string, string> GetRequestParams(this IRequest request, HashSet<string> exclude)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
            
        var map = new Dictionary<string, string>();

        request.QueryString.AddToMap(map, exclude);

        if (request.Verb is HttpMethods.Post or HttpMethods.Put && request.FormData != null)
            request.FormData.AddToMap(map, exclude);

        return map;
    }

    public static Dictionary<string, string> GetDtoQueryParams(this IRequest request) =>
        GetDtoQueryParams(request, HostContext.AppHost?.Config.IgnoreWarningsOnPropertyNames);
    public static Dictionary<string, string> GetDtoQueryParams(this IRequest request, HashSet<string> exclude)
    {
        var map = new Dictionary<string, string>();
        if (request.Dto is IHasQueryParams hasQueryParams && hasQueryParams.QueryParams?.Count > 0)
        {
            foreach (var param in hasQueryParams.QueryParams)
            {
                if (exclude != null && exclude.Contains(param.Key))
                    continue;

                map[param.Key] = param.Value;
            }
        }
        return map;
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

    [Obsolete("Use AssertAccessRoleAsync")]
    public static void AssertAccessRole(IRequest req, string accessRole=null, string authSecret=null)
    {
        if (HostContext.Config.AdminAuthSecret == null || HostContext.Config.AdminAuthSecret != authSecret)
        {
            RequiredRoleAttribute.AssertRequiredRoles(req, accessRole);
        }
    }

    public static async Task AssertAccessRoleAsync(IRequest req, string accessRole=null, string authSecret=null, RequireApiKey requireApiKey=null, CancellationToken token=default)
    {
        if (HostContext.Config.AdminAuthSecret == null || HostContext.Config.AdminAuthSecret != authSecret)
        {
            if (requireApiKey != null)
            {
                var apiKeyValidator = new ApiKeyValidator(req.GetRequiredService<IApiKeySource>, req.GetRequiredService<IApiKeyResolver>);
                if (requireApiKey.Scope != null)
                    apiKeyValidator.Scope = requireApiKey.Scope;
                if (!await apiKeyValidator.IsValidAsync(req.Dto, req))
                    throw new HttpError(403, nameof(HttpStatusCode.Forbidden),
                        ErrorMessages.ApiKeyInvalid.Localize(req));
            }
            else
            {
                await RequiredRoleAttribute.AssertRequiredRoleAsync(req, accessRole, token);
            }
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

    public static T AddTimingsIfNeeded<T>(this T req, ServiceStackHost appHost=null) where T : IRequest
    {
        appHost ??= HostContext.AppHost;
        if (appHost == null) return req;
        
        var shouldProfile = appHost.ShouldProfileRequest(req);
        if (shouldProfile || appHost.AddTimings)
        {
            req.SetItem(Keywords.RequestDuration, Stopwatch.GetTimestamp());
        }
        return req;
    }
}