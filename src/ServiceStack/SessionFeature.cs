using System;
using System.Web;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Web;

namespace ServiceStack
{
    public class SessionFeature : IPlugin
    {
        public const string SessionId = "ss-id";
        public const string PermanentSessionId = "ss-pid";
        public const string SessionOptionsKey = "ss-opt";
        public const string XUserAuthId = HttpHeaders.XUserAuthId;

        [Obsolete("Use Keywords.Session")]
        public const string RequestItemsSessionKey = "__session";

        public static TimeSpan DefaultSessionExpiry = TimeSpan.FromDays(7 * 2); //2 weeks
        public static TimeSpan DefaultPermanentSessionExpiry = TimeSpan.FromDays(7 * 4); //4 weeks

        public TimeSpan? SessionExpiry { get; set; }
        public TimeSpan? PermanentSessionExpiry { get; set; }

        [Obsolete("Removing rarely used feature, if needed override OnSessionFilter() and return null if invalid session")]
        public static bool VerifyCachedSessionId = false;

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
            if (req.PopulateFromRequestIfHasSessionId(requestDto)) return;

            if (req.GetTemporarySessionId() == null)
            {
                res.CreateTemporarySessionId(req);
            }
            if (req.GetPermanentSessionId() == null)
            {
                res.CreatePermanentSessionId(req);
            }
        }

        public static string CreateSessionIds(IRequest httpReq = null, IResponse httpRes = null)
        {
            if (httpReq == null)
                httpReq = HostContext.GetCurrentRequest();
            if (httpRes == null)
                httpRes = httpReq.Response;

            return httpRes.CreateSessionIds(httpReq);
        }

        public static string GetSessionKey(IRequest httpReq = null)
        {
            var sessionId = httpReq.GetSessionId();
            return sessionId == null ? null : GetSessionKey(sessionId);
        }

        public static string GetSessionKey(string sessionId)
        {
            return sessionId == null ? null : IdUtils.CreateUrn<IAuthSession>(sessionId);
        }

        public static T GetOrCreateSession<T>(ICacheClient cache = null, IRequest httpReq = null, IResponse httpRes = null)
        {
            if (httpReq == null)
                httpReq = HostContext.GetCurrentRequest();

            var iSession = httpReq.GetSession(reload:false);
            if (iSession is T)
                return (T)iSession;

            var sessionId = httpReq.GetSessionId();
            var sessionKey = GetSessionKey(sessionId);
            if (sessionKey != null)
            {
                var session = (cache ?? httpReq.GetCacheClient()).Get<T>(sessionKey);
                if (!Equals(session, default(T)))
                    return (T)HostContext.AppHost.OnSessionFilter((IAuthSession)session, sessionId);
            }

            return (T)CreateNewSession(httpReq, sessionId);
        }

        public static IAuthSession CreateNewSession(IRequest httpReq, string sessionId)
        {
            var session = AuthenticateService.CurrentSessionFactory();
            session.Id = sessionId ?? CreateSessionIds(httpReq);
            session.CreatedAt = session.LastModified = DateTime.UtcNow;
            session.OnCreated(httpReq);

            var authEvents = HostContext.TryResolve<IAuthEvents>();
            authEvents?.OnCreated(httpReq, session);

            return session;
        }
    }
}