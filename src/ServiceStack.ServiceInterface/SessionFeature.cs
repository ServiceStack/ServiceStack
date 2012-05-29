using System;
using System.Web;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints;

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
            if (req.GetCookieValue(SessionId) == null)
            {
                res.CreateTemporarySessionId(req);
            }
            if (req.GetCookieValue(PermanentSessionId) == null)
            {
                res.CreatePermanentSessionId(req);
            }
        }

        public static string GetSessionId()
        {
            if (HttpContext.Current == null)
                throw new NotImplementedException(OnlyAspNet);

            return HttpContext.Current.Request.ToRequest().GetSessionId();
        }

        public static void CreateSessionIds()
        {
            if (HttpContext.Current != null)
            {
                HttpContext.Current.Response.ToResponse()
                .CreateSessionIds(HttpContext.Current.Request.ToRequest());
                return;
            }

            throw new NotImplementedException(OnlyAspNet);
        }

        public static string GetSessionKey(string sessionId)
        {
            return IdUtils.CreateUrn<IAuthSession>(sessionId);
        }
    }
}