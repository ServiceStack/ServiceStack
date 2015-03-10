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
                    { HttpHeaders.WwwAuthenticate, AuthenticateService.DefaultOAuthProvider + " realm=\"{0}\"".Fmt(AuthenticateService.DefaultOAuthRealm) }
                },
            };
        }

        public static string GetSessionId(this IServiceBase service)
        {
            var req = service.Request;
            var id = req.GetSessionId();
            if (id == null)
                throw new ArgumentNullException("Session not set. Is Session being set in RequestFilters?");

            return id;
        }

        /// <summary>
        /// If they don't have an ICacheClient configured use an In Memory one.
        /// </summary>
        internal static readonly MemoryCacheClient DefaultCache = new MemoryCacheClient();

        public static ICacheClient GetCacheClient(this IResolver service)
        {
            return service.TryResolve<ICacheClient>()
                ?? (service.TryResolve<IRedisClientsManager>()!=null ? service.TryResolve<IRedisClientsManager>().GetCacheClient():null)
                ?? DefaultCache;
        }

        public static void SaveSession(this IServiceBase service, IAuthSession session, TimeSpan? expiresIn = null)
        {
            if (service == null || session == null) return;

            service.Request.SaveSession(session, expiresIn);
        }

        public static void RemoveSession(this IServiceBase service)
        {
            if (service == null) return;

            service.Request.RemoveSession();
        }

        public static void RemoveSession(this Service service)
        {
            if (service == null) return;

            service.Request.RemoveSession();
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
                throw new ArgumentNullException("sessionId");

            using (var cache = httpReq.GetCacheClient())
            {
                var sessionKey = SessionFeature.GetSessionKey(sessionId);
                cache.Remove(sessionKey);
            }

            httpReq.Items.Remove(SessionFeature.RequestItemsSessionKey);
        }

        public static IAuthSession GetSession(this IServiceBase service, bool reload = false)
        {
            return service.Request.GetSession(reload);
        }

        [Obsolete("Use SessionFeature.RequestItemsSessionKey")]
        public const string RequestItemsSessionKey = SessionFeature.RequestItemsSessionKey;

        private static IAuthSession FilterSession(IAuthSession session, string withSessionId)
        {
            if (session == null || !SessionFeature.VerifyCachedSessionId)
                return session;

            if (session.Id == withSessionId)
                return session;

            if (Log.IsDebugEnabled)
            {
                Log.Debug("ignoring cached sessionId '{0}' which is different to request '{1}'"
                    .Fmt(session.Id, withSessionId));
            }
            return null;
        }

        public static IAuthSession GetSession(this IRequest httpReq, bool reload = false)
        {
            if (httpReq == null) return null;

            if (HostContext.TestMode)
            {
                var mockSession = httpReq.TryResolve<IAuthSession>(); //testing
                if (mockSession != null)
                    return mockSession;
            }

            object oSession = null;
            if (!reload)
                httpReq.Items.TryGetValue(SessionFeature.RequestItemsSessionKey, out oSession);

            var sessionId = httpReq.GetSessionId();
            var cachedSession = FilterSession(oSession as IAuthSession, sessionId);
            if (cachedSession != null)
            {
                return cachedSession;
            }

            using (var cache = httpReq.GetCacheClient())
            {
                var sessionKey = SessionFeature.GetSessionKey(sessionId);
                var session = (sessionKey != null ? FilterSession(cache.Get<IAuthSession>(sessionKey), sessionId) : null)
                    ?? SessionFeature.CreateNewSession(httpReq, sessionId);

                if (httpReq.Items.ContainsKey(SessionFeature.RequestItemsSessionKey))
                    httpReq.Items.Remove(SessionFeature.RequestItemsSessionKey);

                httpReq.Items.Add(SessionFeature.RequestItemsSessionKey, session);
                return session;
            }
        }

        public static TimeSpan? GetSessionTimeToLive(this ICacheClient cache, string sessionId)
        {
            var sessionKey = SessionFeature.GetSessionKey(sessionId);
            return cache.GetTimeToLive(sessionKey);
        }

        public static TimeSpan? GetSessionTimeToLive(this IRequest httpReq)
        {
            using (var cache = httpReq.GetCacheClient())
            {
                return cache.GetSessionTimeToLive(httpReq.GetSessionId());
            }
        }

        public static object RunAction<TService, TRequest>(
            this TService service, TRequest request, Func<TService, TRequest, object> invokeAction,
            IRequest requestContext = null)
            where TService : IService
        {
            var actionCtx = new ActionContext
            {
                RequestFilters = new IHasRequestFilter[0],
                ResponseFilters = new IHasResponseFilter[0],
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
