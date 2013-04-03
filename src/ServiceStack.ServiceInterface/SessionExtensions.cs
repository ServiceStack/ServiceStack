using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Web;
using ServiceStack.CacheAccess;
using ServiceStack.Common;
using ServiceStack.Configuration;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Text;
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

        static readonly RandomNumberGenerator randgen = new RNGCryptoServiceProvider();
        internal static string CreateRandomSessionId()
        {
            var data = new byte[15];
            randgen.GetBytes(data);
            return Convert.ToBase64String(data);
        }

        public static string CreatePermanentSessionId(this IHttpResponse res, IHttpRequest req)
        {
            var sessionId = CreateRandomSessionId();
            res.Cookies.AddPermanentCookie(SessionFeature.PermanentSessionId, sessionId);
            req.Items[SessionFeature.PermanentSessionId] = sessionId;
            return sessionId;
        }

        public static string CreateTemporarySessionId(this IHttpResponse res, IHttpRequest req)
        {
            var sessionId = CreateRandomSessionId();
            res.Cookies.AddSessionCookie(SessionFeature.SessionId, sessionId,
                (EndpointHost.Config != null && EndpointHost.Config.OnlySendSessionCookiesSecurely && req.IsSecureConnection));
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

        public static string GetSessionKey(IHttpRequest httpReq = null)
        {
            var sessionId = SessionFeature.GetSessionId(httpReq);
            return sessionId == null ? null : SessionFeature.GetSessionKey(sessionId);
        }

        public static TUserSession SessionAs<TUserSession>(this ICacheClient cache,
            IHttpRequest httpReq = null, IHttpResponse httpRes = null)
        {
            var sessionKey = GetSessionKey(httpReq);

            if (sessionKey != null)
            {
                var userSession = cache.Get<TUserSession>(sessionKey);
                if (!Equals(userSession, default(TUserSession)))
                    return userSession;
            }

            if (sessionKey == null)
                SessionFeature.CreateSessionIds(httpReq, httpRes);

            var unAuthorizedSession = (TUserSession)typeof(TUserSession).CreateInstance();
            return unAuthorizedSession;
        }

        public static void ClearSession(this ICacheClient cache, IHttpRequest httpReq = null)
        {
            cache.Remove(GetSessionKey(httpReq));
        }
    }
}