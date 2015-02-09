﻿using System;
using System.Net;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Redis;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack
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
            if (httpReq == null) return;

            using (var cache = httpReq.GetCacheClient())
            {
                var sessionKey = SessionFeature.GetSessionKey(httpReq.GetSessionId());
                cache.CacheSet(sessionKey, session, expiresIn ?? HostContext.GetDefaultSessionExpiry());
            }

            httpReq.Items[RequestItemsSessionKey] = session;
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

            httpReq.Items.Remove(RequestItemsSessionKey);
        }

        public static IAuthSession GetSession(this IServiceBase service, bool reload = false)
        {
            return service.Request.GetSession(reload);
        }

        public const string RequestItemsSessionKey = "__session";
        public static IAuthSession GetSession(this IRequest httpReq, bool reload = false)
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

                    var authEvents = HostContext.TryResolve<IAuthEvents>();
                    if (authEvents != null) 
                        authEvents.OnCreated(httpReq, session);
                }

                if (httpReq.Items.ContainsKey(RequestItemsSessionKey))
                    httpReq.Items.Remove(RequestItemsSessionKey);

                httpReq.Items.Add(RequestItemsSessionKey, session);
                return session;
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
