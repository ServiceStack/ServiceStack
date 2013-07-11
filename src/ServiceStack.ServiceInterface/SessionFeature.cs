using System;
using System.Web;
using ServiceStack.CacheAccess;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.ServiceInterface
{
    public class SessionFeature : IPlugin
    {
        public const string OnlyAspNet = "Only ASP.NET Requests accessible via Singletons are supported";
        public const string SessionId = "ss-id";
        public const string PermanentSessionId = "ss-pid";
        public const string SessionOptionsKey = "ss-opt";
        public const string XUserAuthId = HttpHeaders.XUserAuthId;

        private static bool alreadyConfigured;

        public void Register(IAppHost appHost)
        {
            if (alreadyConfigured) return;
            alreadyConfigured = true;

            //Add permanent and session cookies if not already set.
            appHost.RequestFilters.Add(AddSessionIdToRequestFilter);
        }

        public static void AddSessionIdToRequestFilter(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            if (req.GetItemOrCookie(SessionId) == null)
            {
                res.CreateTemporarySessionId(req);
            }
            if (req.GetItemOrCookie(PermanentSessionId) == null)
            {
                res.CreatePermanentSessionId(req);
            }
        }

        public static string GetSessionId(IHttpRequest httpReq = null)
        {
            if (httpReq == null && HttpContext.Current == null)
                throw new NotImplementedException(OnlyAspNet);

            httpReq = httpReq ?? HttpContext.Current.Request.ToRequest();

            return httpReq.GetSessionId();
        }

        public static void CreateSessionIds(IHttpRequest httpReq = null, IHttpResponse httpRes = null)
        {
            if (httpReq == null || httpRes == null)
            {
                if (HttpContext.Current == null)
                    throw new NotImplementedException(OnlyAspNet);
            }

            httpReq = httpReq ?? HttpContext.Current.Request.ToRequest();
            httpRes = httpRes ?? HttpContext.Current.Response.ToResponse();

            httpRes.CreateSessionIds(httpReq);
        }

        public static string GetSessionKey()
        {
            var sessionId = GetSessionId();
            return sessionId == null ? null : GetSessionKey(sessionId);
        }

        public static string GetSessionKey(string sessionId)
        {
            return IdUtils.CreateUrn<IAuthSession>(sessionId);
        }

        public static T GetOrCreateSession<T>(ICacheClient cacheClient) where T : class, new()
        {
            T session = null;
            if (GetSessionKey() != null)
                session = cacheClient.Get<T>(GetSessionKey());
            else
                CreateSessionIds();

            return session ?? new T();
        }
    }
}