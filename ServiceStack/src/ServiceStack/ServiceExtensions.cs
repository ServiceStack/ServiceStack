using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Host;
using ServiceStack.Logging;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

public static class ServiceExtensions
{
    public static ILog Log = LogManager.GetLogger(typeof(ServiceExtensions));

    public static IHttpResult Redirect(this IServiceBase service, string redirect) => 
        service.Redirect(redirect, HelpMessages.DefaultRedirectMessage);

    public static IHttpResult Redirect(this IServiceBase service, string redirect, string message) =>
        HostContext.AppHost.Redirect(service, redirect, message);

    public static IHttpResult LocalRedirect(this IServiceBase service, string redirect) =>
        service.LocalRedirect(redirect, HelpMessages.DefaultRedirectMessage);
        
    public static IHttpResult LocalRedirect(this IServiceBase service, string redirect, string message) =>
        HostContext.AppHost.LocalRedirect(service, redirect, message);

    public static IHttpResult AuthenticationRequired(this IServiceBase service) =>
        HostContext.AppHost.AuthenticationRequired(service);

    public static string GetSessionId(this IServiceBase service)
    {
        var req = service.Request;
        var sessionId = req.GetSessionId();
        if (sessionId == null)
            throw new ArgumentNullException(nameof(sessionId), ErrorMessages.SessionIdEmpty);

        return sessionId;
    }

    public static ICacheClient GetCacheClient(this IRequest request) => HostContext.AppHost.GetCacheClient(request);
    public static ICacheClientAsync GetCacheClientAsync(this IRequest request) => HostContext.AppHost.GetCacheClientAsync(request);

    public static ICacheClient GetMemoryCacheClient(this IRequest request) => HostContext.AppHost.GetMemoryCacheClient(request);

    [Obsolete("Use SaveSessionAsync")]
    public static void SaveSession(this IServiceBase service, IAuthSession session, TimeSpan? expiresIn = null)
    {
        if (service == null || session == null) return;

        service.Request.SaveSession(session, expiresIn);
    }

    public static async Task SaveSessionAsync(this IServiceBase service, IAuthSession session, TimeSpan? expiresIn = null, CancellationToken token=default)
    {
        if (service == null || session == null) return;

        await service.Request.SaveSessionAsync(session, expiresIn, token).ConfigAwait();
    }

    public static void RemoveSession(this IServiceBase service)
    {
        service?.Request.RemoveSession();
    }
    public static Task RemoveSessionAsync(this IServiceBase service, CancellationToken token=default)
    {
        return service?.Request.RemoveSessionAsync(token);
    }

    public static void RemoveSession(this Service service)
    {
        service?.Request.RemoveSession();
    }
    public static Task RemoveSessionAsync(this Service service, CancellationToken token=default)
    {
        return service?.Request.RemoveSessionAsync(token);
    }

    public static void CacheSet<T>(this ICacheClient cache, string key, T value, TimeSpan? expiresIn)
    {
        if (expiresIn.HasValue)
            cache.Set(key, value, expiresIn.Value);
        else
            cache.Set(key, value);
    }

    public static async Task CacheSetAsync<T>(this ICacheClientAsync cache, string key, T value, TimeSpan? expiresIn, CancellationToken token=default)
    {
        if (expiresIn.HasValue)
            await cache.SetAsync(key, value, expiresIn.Value, token).ConfigAwait();
        else
            await cache.SetAsync(key, value, token).ConfigAwait();
    }

    [Obsolete("Use SaveSessionAsync")]
    public static void SaveSession(this IRequest httpReq, IAuthSession session, TimeSpan? expiresIn = null)
    {
        HostContext.AppHost.OnSaveSession(httpReq, session, expiresIn);
    }

    public static Task SaveSessionAsync(this IRequest httpReq, IAuthSession session, TimeSpan? expiresIn = null, CancellationToken token=default)
    {
        return HostContext.AppHost.OnSaveSessionAsync(httpReq, session, expiresIn, token);
    }

    public static void RemoveSession(this IRequest httpReq)
    {
        RemoveSession(httpReq, httpReq.GetSessionId());
    }

    public static Task RemoveSessionAsync(this IRequest httpReq, CancellationToken token=default)
    {
        return RemoveSessionAsync(httpReq, httpReq.GetSessionId(), token);
    }

    public static void RemoveSession(this IRequest httpReq, string sessionId)
    {
        if (httpReq == null) return;
        if (sessionId == null)
            throw new ArgumentNullException(nameof(sessionId));

        var sessionKey = SessionFeature.GetSessionKey(sessionId);
        httpReq.GetCacheClient().Remove(sessionKey);

        httpReq.Items.Remove(Keywords.Session);
    }

    public static async Task RemoveSessionAsync(this IRequest httpReq, string sessionId, CancellationToken token=default)
    {
        if (httpReq == null) return;
        if (sessionId == null)
            throw new ArgumentNullException(nameof(sessionId));

        var sessionKey = SessionFeature.GetSessionKey(sessionId);
        await httpReq.GetCacheClientAsync().RemoveAsync(sessionKey, token).ConfigAwait();

        httpReq.Items.Remove(Keywords.Session);
    }

    public static IAuthSession GetSession(this IServiceBase service, bool reload = false)
    {
        return service.Request.GetSession(reload);
    }

    public static Task<IAuthSession> GetSessionAsync(this IServiceBase service, bool reload = false, CancellationToken token=default)
    {
        return service.Request.GetSessionAsync(reload, token);
    }

    public static TUserSession SessionAs<TUserSession>(this IRequest req)
    {
        if (HostContext.TestMode)
        {
            var mockSession = req.TryResolve<TUserSession>();
            if (!Equals(mockSession, default(TUserSession)))
                mockSession = req.TryResolve<IAuthSession>() is TUserSession
                    ? (TUserSession)req.TryResolve<IAuthSession>()
                    : default;

            if (!Equals(mockSession, default(TUserSession)))
                return mockSession;
        }

        return SessionFeature.GetOrCreateSession<TUserSession>(req.GetCacheClient(), req, req.Response);
    }

    public static async Task<TUserSession> SessionAsAsync<TUserSession>(this IRequest req, CancellationToken token=default)
    {
        if (HostContext.TestMode)
        {
            var mockSession = req.TryResolve<TUserSession>();
            if (!Equals(mockSession, default(TUserSession)))
                mockSession = req.TryResolve<IAuthSession>() is TUserSession
                    ? (TUserSession)req.TryResolve<IAuthSession>()
                    : default;

            if (!Equals(mockSession, default(TUserSession)))
                return mockSession;
        }

        return await SessionFeature.GetOrCreateSessionAsync<TUserSession>(req.GetCacheClientAsync(), req, req.Response, token).ConfigAwait();
    }

    [Obsolete("Use IsAuthenticatedAsync")]
    public static bool IsAuthenticated(this IRequest req) => AuthenticateAttribute.Authenticate(req, req.Dto);

    public static Task<bool> IsAuthenticatedAsync(this IRequest req) => AuthenticateAttribute.AuthenticateAsync(req, req.Dto);

    public static IAuthSession AssertAuthenticatedSession(this IRequest req, bool reload = false) 
        => HostContext.AppHost.HasValidAuthSecret(req)
            ? HostContext.GetAuthSecretSession()
            : HostContext.AppHost.AssertAuthenticated(req.GetSession(), req);

    public static async Task<IAuthSession> AssertAuthenticatedSessionAsync(this IRequest req, bool reload=false, CancellationToken token=default) 
        => HostContext.AppHost.HasValidAuthSecret(req)
            ? HostContext.GetAuthSecretSession()
            : HostContext.AppHost.AssertAuthenticated(await req.GetSessionAsync(token: token).ConfigAwait(), req);

    public static IAuthSession GetSession(this IRequest httpReq, bool reload = false)
    {
        var task = GetSessionInternalAsync(httpReq, reload, async: false);
        var ret = task.GetResult();
        return ret;
    }

    public static Task<IAuthSession> GetSessionAsync(this IRequest httpReq, bool reload = false, CancellationToken token=default)
    {
        return GetSessionInternalAsync(httpReq, reload, async: true, token);
    }

    internal static async Task<IAuthSession> GetSessionInternalAsync(this IRequest httpReq, bool reload, bool async, CancellationToken token=default)
    {
        if (httpReq == null)
            return null;

        if (HostContext.TestMode)
        {
            var mockSession = httpReq.TryResolve<IAuthSession>(); //testing
            if (mockSession != null)
                return mockSession;
        }

        httpReq.Items.TryGetValue(Keywords.Session, out var oSession);
        if (reload && (oSession as IAuthSession)?.FromToken != true) // can't reload FromToken sessions from cache
            oSession = null;

        var appHost = HostContext.AppHost;
        if (oSession == null && !httpReq.Items.ContainsKey(Keywords.HasPreAuthenticated))
        {
            try
            {
                await appHost.ApplyPreAuthenticateFiltersAsync(httpReq, httpReq.Response).ConfigAwait();
                httpReq.Items.TryGetValue(Keywords.Session, out oSession);
            }
            catch (Exception ex)
            {
                Log.Error("Error in GetSession() when ApplyPreAuthenticateFilters", ex);
                /*treat errors as non-existing session*/
            }
        }

        var sessionId = httpReq.GetSessionId();
        var session = oSession as IAuthSession;
        if (session != null)
            session = appHost.OnSessionFilter(httpReq, session, sessionId);
        if (session != null)
            return session;

        if (appHost.HasValidAuthSecret(httpReq))
        {
            session = HostContext.GetAuthSecretSession();
            if (session != null)
                return session;
        }

        var sessionKey = SessionFeature.GetSessionKey(sessionId);
        if (sessionKey != null)
        {
            // If changing global JsConfig configuration to use snake_case serialization convention
            session = async
                ? await httpReq.GetCacheClientAsync().GetAsync<IAuthSession>(sessionKey, token).ConfigAwait()
                : httpReq.GetCacheClient().Get<IAuthSession>(sessionKey);

            if (session != null)
                session = appHost.OnSessionFilter(httpReq, session, sessionId);
        }

        if (session == null)
        {
            var newSession = SessionFeature.CreateNewSession(httpReq, sessionId);
            session = appHost.OnSessionFilter(httpReq, newSession, sessionId) ?? newSession;
        }

        httpReq.Items[Keywords.Session] = session;
        return session;
    }

    public static TimeSpan? GetSessionTimeToLive(this ICacheClient cache, string sessionId)
    {
        var sessionKey = SessionFeature.GetSessionKey(sessionId);
        return cache.GetTimeToLive(sessionKey);
    }

    public static async Task<TimeSpan?> GetSessionTimeToLiveAsync(this ICacheClientAsync cache, string sessionId, CancellationToken token=default)
    {
        var sessionKey = SessionFeature.GetSessionKey(sessionId);
        return await cache.GetTimeToLiveAsync(sessionKey, token).ConfigAwait();
    }

    public static TimeSpan? GetSessionTimeToLive(this IRequest httpReq)
    {
        return httpReq.GetCacheClient().GetSessionTimeToLive(httpReq.GetSessionId());
    }

    public static Task<TimeSpan?> GetSessionTimeToLiveAsync(this IRequest httpReq, CancellationToken token=default)
    {
        return httpReq.GetCacheClientAsync().GetSessionTimeToLiveAsync(httpReq.GetSessionId(), token);
    }

    public static object RunAction<TService, TRequest>(
        this TService service, TRequest request, Func<TService, TRequest, object> invokeAction,
        IRequest requestContext = null)
        where TService : IService
    {
        var actionCtx = new ActionContext
        {
            RequestFilters = TypeConstants<IRequestFilterBase>.EmptyArray,
            ResponseFilters = TypeConstants<IResponseFilterBase>.EmptyArray,
            ServiceType = typeof(TService),
            RequestType = typeof(TRequest),
            ServiceAction = (instance, req) => invokeAction(service, request)
        };

        requestContext ??= new MockHttpRequest();
        ServiceController.InjectRequestContext(service, requestContext);
        var runner = HostContext.CreateServiceRunner<TRequest>(actionCtx);
        var responseTask = runner.ExecuteAsync(requestContext, service, request);
        var response = responseTask.Result;
        return response;
    }
}