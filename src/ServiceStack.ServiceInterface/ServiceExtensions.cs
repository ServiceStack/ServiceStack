using System;
using System.Net;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Redis;
using ServiceStack.Server;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;
using ServiceStack.Web;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface
{
    public static class ServiceExtensions
    {
        public static IHttpResult Redirect(this IServiceBase service, string url)
        {
            return service.Redirect(url, "Moved Temporarily");
        }

        public static IHttpResult Redirect(this IServiceBase service, string url, string message)
        {
            return new HttpResult(HttpStatusCode.Redirect, message)
            {
                ContentType = service.RequestContext.ResponseContentType,
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
                ContentType = service.RequestContext.ResponseContentType,
                Headers = {
                    { HttpHeaders.WwwAuthenticate, AuthenticateService.DefaultOAuthProvider + " realm=\"{0}\"".Fmt(AuthenticateService.DefaultOAuthRealm) }
                },
            };
        }

        public static string GetSessionId(this IServiceBase service)
        {
            var req = service.RequestContext.Get<IHttpRequest>();
            var id = req.GetSessionId();
            if (id == null)
                throw new ArgumentNullException("Session not set. Is Session being set in RequestFilters?");

            return id;
        }

        /// <summary>
        /// If they don't have an ICacheClient configured use an In Memory one.
        /// </summary>
        private static readonly MemoryCacheClient DefaultCache = new MemoryCacheClient { FlushOnDispose = true };

        public static ICacheClient GetCacheClient(this IResolver service)
        {
            return service.TryResolve<ICacheClient>()
                ?? (service.TryResolve<IRedisClientsManager>()!=null ? service.TryResolve<IRedisClientsManager>().GetCacheClient():null)
                ?? DefaultCache;
        }

        public static void SaveSession(this IServiceBase service, IAuthSession session, TimeSpan? expiresIn = null)
        {
            if (service == null) return;

            service.RequestContext.Get<IHttpRequest>().SaveSession(session, expiresIn);
        }

        public static void RemoveSession(this IServiceBase service)
        {
            if (service == null) return;

            service.RequestContext.Get<IHttpRequest>().RemoveSession();
        }

        public static void RemoveSession(this Service service)
        {
            if (service == null) return;

            service.RequestContext.Get<IHttpRequest>().RemoveSession();
        }

        public static void CacheSet<T>(this ICacheClient cache, string key, T value, TimeSpan? expiresIn)
        {
            if (expiresIn.HasValue)
                cache.Set(key, value, expiresIn.Value);
            else
                cache.Set(key, value);
        }

        public static void SaveSession(this IHttpRequest httpReq, IAuthSession session, TimeSpan? expiresIn = null)
        {
            if (httpReq == null) return;

            using (var cache = httpReq.GetCacheClient())
            {
                var sessionKey = SessionFeature.GetSessionKey(httpReq.GetSessionId());
                cache.CacheSet(sessionKey, session, expiresIn ?? AuthFeature.GetDefaultSessionExpiry());
            }

            httpReq.Items[RequestItemsSessionKey] = session;
        }

        public static void RemoveSession(this IHttpRequest httpReq)
        {
            if (httpReq == null) return;

            using (var cache = httpReq.GetCacheClient())
            {
                var sessionKey = SessionFeature.GetSessionKey(httpReq.GetSessionId());
                cache.Remove(sessionKey);
            }

            httpReq.Items.Remove(RequestItemsSessionKey);
        }

        public static IAuthSession GetSession(this IServiceBase service, bool reload = false)
        {
            return service.RequestContext.Get<IHttpRequest>().GetSession(reload);
        }

        public static IAuthSession GetSession(this Service service, bool reload = false)
        {
            var req = service.RequestContext.Get<IHttpRequest>();
            if (req.GetSessionId() == null)
                service.RequestContext.Get<IHttpResponse>().CreateSessionIds(req);
            return req.GetSession(reload);
        }

        public const string RequestItemsSessionKey = "__session";
        public static IAuthSession GetSession(this IHttpRequest httpReq, bool reload = false)
        {
            if (httpReq == null) return null;

            object oSession = null;
            if (!reload)
                httpReq.Items.TryGetValue(RequestItemsSessionKey, out oSession);

            if (oSession != null)
                return (IAuthSession)oSession;

            using (var cache = httpReq.GetCacheClient())
            {
                var sessionId = httpReq.GetSessionId();
                var session = cache.Get<IAuthSession>(SessionFeature.GetSessionKey(sessionId));
                if (session == null)
                {
                    session = AuthenticateService.CurrentSessionFactory();
                    session.Id = sessionId;
                    session.CreatedAt = session.LastModified = DateTime.UtcNow;
                    session.OnCreated(httpReq);
                }

                if (httpReq.Items.ContainsKey(RequestItemsSessionKey))
                    httpReq.Items.Remove(RequestItemsSessionKey);

                httpReq.Items.Add(RequestItemsSessionKey, session);
                return session;
            }
        }

        public static object RunAction<TService, TRequest>(
            this TService service, TRequest request, Func<TService, TRequest, object> invokeAction,
            IRequestContext requestContext = null)
            where TService : IService
        {
            var actionCtx = new ActionContext
            {
                RequestFilters = new IHasRequestFilter[0],
                ResponseFilters = new IHasResponseFilter[0],
                RequestType = service.GetType(),
                ServiceAction = (instance, req) => invokeAction(service, request)
            };

            requestContext = requestContext ?? new MockRequestContext();
            var runner = new ServiceRunner<TRequest>(EndpointHost.AppHost, actionCtx);
            var response = runner.Execute(requestContext, service, request);
            return response;
        }
    }
}
