using System;
using System.Net;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Logging;
using ServiceStack.Redis;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack
{
    public static class ServiceExtensions
    {
        public static ILog Log = LogManager.GetLogger(typeof(ServiceExtensions));

        public static IHttpResult Redirect(this IServiceBase service, string url)
        {
            return service.Redirect(url, "Moved Temporarily");
        }

        public static IHttpResult Redirect(this IServiceBase service, string url, string message)
        {
            return new HttpResult(HttpStatusCode.Redirect, message)
            {
                ContentType = service.Request.ResponseContentType,
                Headers = {
                    { HttpHeaders.Location, url }
                },
            };
        }

        public static IHttpResult AuthenticationRequired(this IServiceBase service)
        {
            return new HttpResult
            {
                StatusCode = HttpStatusCode.Unauthorized,
                ContentType = service.Request.ResponseContentType,
                Headers = {
                    { HttpHeaders.WwwAuthenticate, $"{AuthenticateService.DefaultOAuthProvider} realm=\"{AuthenticateService.DefaultOAuthRealm}\"" }
                },
            };
        }

        public static string GetSessionId(this IServiceBase service)
        {
            var req = service.Request;
            var sessionId = req.GetSessionId();
            if (sessionId == null)
                throw new ArgumentNullException("sessionId", "Session not set. Is Session being set in RequestFilters?");

            return sessionId;
        }

        /// <summary>
        /// If they don't have an ICacheClient configured use an In Memory one.
        /// </summary>
        internal static readonly MemoryCacheClient DefaultCache = new MemoryCacheClient();

        public static ICacheClient GetCacheClient(this IResolver service)
        {
            var cache = service.TryResolve<ICacheClient>();
            if (cache != null)
                return cache;

            var redisManager = service.TryResolve<IRedisClientsManager>();
            if (redisManager != null)
                return redisManager.GetCacheClient();

            return DefaultCache;
        }

        public static void SaveSession(this IServiceBase service, IAuthSession session, TimeSpan? expiresIn = null)
        {
            if (service == null || session == null) return;

            service.Request.SaveSession(session, expiresIn);
        }

        public static void RemoveSession(this IServiceBase service)
        {
            service?.Request.RemoveSession();
        }

        public static void RemoveSession(this Service service)
        {
            service?.Request.RemoveSession();
        }

        public static void CacheSet<T>(this ICacheClient cache, string key, T value, TimeSpan? expiresIn)
        {
            if (expiresIn.HasValue)
                cache.Set(key, value, expiresIn.Value);
            else
                cache.Set(key, value);
        }

        public static void SaveSession(this IRequest httpReq, IAuthSession session, TimeSpan? expiresIn = null)
        {
            HostContext.AppHost.OnSaveSession(httpReq, session, expiresIn);
        }

        public static void RemoveSession(this IRequest httpReq)
        {
            RemoveSession(httpReq, httpReq.GetSessionId());
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

        public static IAuthSession GetSession(this IServiceBase service, bool reload = false)
        {
            return service.Request.GetSession(reload);
        }

        public static TUserSession SessionAs<TUserSession>(this IRequest req)
        {
            if (HostContext.TestMode)
            {
                var mockSession = req.TryResolve<TUserSession>();
                if (!Equals(mockSession, default(TUserSession)))
                    mockSession = req.TryResolve<IAuthSession>() is TUserSession
                        ? (TUserSession)req.TryResolve<IAuthSession>()
                        : default(TUserSession);

                if (!Equals(mockSession, default(TUserSession)))
                    return mockSession;
            }

            return SessionFeature.GetOrCreateSession<TUserSession>(req.GetCacheClient(), req, req.Response);
        }

        public static IAuthSession GetSession(this IRequest httpReq, bool reload = false)
        {
            if (httpReq == null)
                return null;

            if (HostContext.TestMode)
            {
                var mockSession = httpReq.TryResolve<IAuthSession>(); //testing
                if (mockSession != null)
                    return mockSession;
            }

            object oSession = null;
            if (!reload)
                httpReq.Items.TryGetValue(Keywords.Session, out oSession);

            if (oSession == null && !httpReq.Items.ContainsKey(Keywords.HasPreAuthenticated))
            {
                try
                {
                    HostContext.AppHost.ApplyPreAuthenticateFilters(httpReq, httpReq.Response);
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
                session = HostContext.AppHost.OnSessionFilter(session, sessionId);
            if (session != null)
                return session;

            var sessionKey = SessionFeature.GetSessionKey(sessionId);
            if (sessionKey != null)
            {
                session = httpReq.GetCacheClient().Get<IAuthSession>(sessionKey);

                if (session != null)
                    session = HostContext.AppHost.OnSessionFilter(session, sessionId);
            }

            if (session == null)
            {
                var newSession = SessionFeature.CreateNewSession(httpReq, sessionId);
                session = HostContext.AppHost.OnSessionFilter(newSession, sessionId) ?? newSession;
            }

            httpReq.Items[Keywords.Session] = session;
            return session;
        }

        public static TimeSpan? GetSessionTimeToLive(this ICacheClient cache, string sessionId)
        {
            var sessionKey = SessionFeature.GetSessionKey(sessionId);
            return cache.GetTimeToLive(sessionKey);
        }

        public static TimeSpan? GetSessionTimeToLive(this IRequest httpReq)
        {
            return httpReq.GetCacheClient().GetSessionTimeToLive(httpReq.GetSessionId());
        }

        public static object RunAction<TService, TRequest>(
            this TService service, TRequest request, Func<TService, TRequest, object> invokeAction,
            IRequest requestContext = null)
            where TService : IService
        {
            var actionCtx = new ActionContext
            {
                RequestFilters = TypeConstants<IHasRequestFilter>.EmptyArray,
                ResponseFilters = TypeConstants<IHasResponseFilter>.EmptyArray,
                ServiceType = typeof(TService),
                RequestType = typeof(TRequest),
                ServiceAction = (instance, req) => invokeAction(service, request)
            };

            requestContext = requestContext ?? new MockHttpRequest();
            ServiceController.InjectRequestContext(service, requestContext);
            var runner = HostContext.CreateServiceRunner<TRequest>(actionCtx);
            var response = runner.Execute(requestContext, service, request);
            return response;
        }
    }
}
