using System;
using System.Web;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class SessionFeature : IPlugin
    {
        public const string OnlyAspNet = "Only ASP.NET Requests accessible via Singletons are supported";
        public const string SessionId = "ss-id";
        public const string PermanentSessionId = "ss-pid";
        public const string SessionOptionsKey = "ss-opt";
        public const string XUserAuthId = HttpHeaders.XUserAuthId;
        public static TimeSpan DefaultSessionExpiry = TimeSpan.FromDays(7 * 2); //2 weeks
        public TimeSpan SessionExpiry { get; set; }

        public SessionFeature()
        {
            this.SessionExpiry = DefaultSessionExpiry;
        }

        private static bool alreadyConfigured;

        public void Register(IAppHost appHost)
        {
            if (alreadyConfigured) return;
            alreadyConfigured = true;

            //Add permanent and session cookies if not already set.
            appHost.GlobalRequestFilters.Add(AddSessionIdToRequestFilter);
        }

        public static void AddSessionIdToRequestFilter(IRequest req, IResponse res, object requestDto)
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

        public static string GetSessionId(IRequest httpReq = null)
        {
            if (httpReq == null && HttpContext.Current == null)
                throw new NotImplementedException(OnlyAspNet);

            httpReq = httpReq ?? HttpContext.Current.ToRequest();

            return httpReq.GetSessionId();
        }

        public static void CreateSessionIds(IRequest httpReq = null, IResponse httpRes = null)
        {
            if (httpReq == null || httpRes == null)
            {
                if (HttpContext.Current == null)
                    throw new NotImplementedException(OnlyAspNet);
            }

            httpReq = httpReq ?? HttpContext.Current.ToRequest();
            httpRes = httpRes ?? httpReq.Response;

            httpRes.CreateSessionIds(httpReq);
        }

        public static string GetSessionKey(IRequest httpReq = null)
        {
            var sessionId = GetSessionId(httpReq);
            return sessionId == null ? null : GetSessionKey(sessionId);
        }

        public static string GetSessionKey(string sessionId)
        {
            return IdUtils.CreateUrn<IAuthSession>(sessionId);
        }

        public static T GetOrCreateSession<T>(ICacheClient cacheClient, IRequest httpReq = null, IResponse httpRes = null) 
            where T : class
        {
            T session = null;
            if (GetSessionKey(httpReq) != null)
                session = cacheClient.Get<T>(GetSessionKey(httpReq));
            else
                CreateSessionIds(httpReq, httpRes);

            return session ?? typeof(T).New<T>();
        }
    }
}