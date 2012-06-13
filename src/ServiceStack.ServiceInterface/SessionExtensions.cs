using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.ServiceInterface
{
    public class SessionOptions
    {
        public const string Temporary = "temp";
        public const string Permanent = "perm";
    }

    /// <summary>
    /// Configure ServiceStack to have ISession support
    /// </summary>
    public static class SessionExtensions 
    {
        public static string GetSessionId(this IHttpRequest httpReq)
        {
            var sessionOptions = GetSessionOptions(httpReq);

            return sessionOptions.Contains(SessionOptions.Permanent)
                ? httpReq.GetItemOrCookie(SessionFeature.PermanentSessionId)
                : httpReq.GetItemOrCookie(SessionFeature.SessionId);
        }

        public static string GetPermanentSessionId(this IHttpRequest httpReq)
        {
            return httpReq.GetItemOrCookie(SessionFeature.PermanentSessionId);
        }

        public static string GetTemporarySessionId(this IHttpRequest httpReq)
        {
            return httpReq.GetItemOrCookie(SessionFeature.SessionId);
        }

        /// <summary>
        /// Create the active Session or Permanent Session Id cookie.
        /// </summary>
        /// <returns></returns>
        public static string CreateSessionId(this IHttpResponse res, IHttpRequest req)
        {
            var sessionOptions = GetSessionOptions(req);
            return sessionOptions.Contains(SessionOptions.Permanent)
                ? res.CreatePermanentSessionId(req)
                : res.CreateTemporarySessionId(req);
        }

        /// <summary>
        /// Create both Permanent and Session Id cookies and return the active sessionId
        /// </summary>
        /// <returns></returns>
        public static string CreateSessionIds(this IHttpResponse res, IHttpRequest req)
        {
            var sessionOptions = GetSessionOptions(req);
            var permId = res.CreatePermanentSessionId(req);
            var tempId = res.CreateTemporarySessionId(req);
            return sessionOptions.Contains(SessionOptions.Permanent)
                ? permId
                : tempId;
        }

        public static string CreatePermanentSessionId(this IHttpResponse res, IHttpRequest req)
        {
            var sessionId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            res.Cookies.AddPermanentCookie(SessionFeature.PermanentSessionId, sessionId);
            req.Items[SessionFeature.PermanentSessionId] = sessionId;
            return sessionId;
        }

        public static string CreateTemporarySessionId(this IHttpResponse res, IHttpRequest req)
        {
            var sessionId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            res.Cookies.AddSessionCookie(SessionFeature.SessionId, sessionId);
            req.Items[SessionFeature.SessionId] = sessionId;
            return sessionId;
        }

        public static HashSet<string> GetSessionOptions(this IHttpRequest httpReq)
        {
            var sessionOptions = httpReq.GetItemOrCookie(SessionFeature.SessionOptionsKey);
            return sessionOptions.IsNullOrEmpty()
                ? new HashSet<string>()
                : sessionOptions.Split(',').ToHashSet();
        }

        public static void UpdateSession(this IAuthSession session, UserAuth userAuth)
        {
            if (userAuth == null || session == null) return;
            session.Roles = userAuth.Roles;
            session.Permissions = userAuth.Permissions;
        }

        public static void UpdateFromUserAuthRepo(this IAuthSession session, IHttpRequest req, IUserAuthRepository userAuthRepo = null)
        {
            if (userAuthRepo == null)
                userAuthRepo = req.TryResolve<IUserAuthRepository>();

            if (userAuthRepo == null) return;

            var userAuth = userAuthRepo.GetUserAuth(session, null);
            session.UpdateSession(userAuth);
        }

        public static HashSet<string> AddSessionOptions(this IHttpResponse res, IHttpRequest req, params string[] options)
        {
            if (res == null || req == null || options.Length == 0) return new HashSet<string>();

            var existingOptions = req.GetSessionOptions();
            foreach (var option in options)
            {
                if (option.IsNullOrEmpty()) continue;

                if (option == SessionOptions.Permanent)
                    existingOptions.Remove(SessionOptions.Temporary);
                else if (option == SessionOptions.Temporary)
                    existingOptions.Remove(SessionOptions.Permanent);

                existingOptions.Add(option);
            }

            var strOptions = String.Join(",", existingOptions.ToArray());
            res.Cookies.AddPermanentCookie(SessionFeature.SessionOptionsKey, strOptions);
            req.Items[SessionFeature.SessionOptionsKey] = strOptions;
            
            return existingOptions;
        }

        public static IHttpRequest ToRequest(this HttpRequest aspnetHttpReq)
        {
            return new HttpRequestWrapper(aspnetHttpReq) {
                Container = AppHostBase.Instance != null ? AppHostBase.Instance.Container : null
            };
        }

        public static IHttpRequest ToRequest(this HttpListenerRequest listenerHttpReq)
        {
            return new HttpListenerRequestWrapper(listenerHttpReq) {
                Container = AppHostBase.Instance != null ? AppHostBase.Instance.Container : null
            };
        }

        public static IHttpResponse ToResponse(this HttpResponse aspnetHttpRes)
        {
            return new HttpResponseWrapper(aspnetHttpRes);
        }

        public static IHttpResponse ToResponse(this HttpListenerResponse listenerHttpRes)
        {
            return new HttpListenerResponseWrapper(listenerHttpRes);
        }
    }
}