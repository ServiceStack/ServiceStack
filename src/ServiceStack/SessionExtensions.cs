using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Web;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack
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
        public static string GetOrCreateSessionId(this IRequest httpReq)
        {
            var sessionId = httpReq.GetSessionId();
            return sessionId ?? SessionFeature.CreateSessionIds(httpReq);
        }

        public static void SetSessionId(this IRequest req, string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return;

            var sessionKey = req.IsPermanentSession()
                ? SessionFeature.PermanentSessionId
                : SessionFeature.SessionId;

            req.Items[sessionKey] = sessionId;
        }

        public static string GetSessionId(this IRequest req)
        {
            if (req == null)
                req = HostContext.GetCurrentRequest();

            return req.IsPermanentSession()
                ? req.GetPermanentSessionId()
                : req.GetTemporarySessionId();
        }

        public static string GetPermanentSessionId(this IRequest httpReq)
        {
            return httpReq.GetSessionParam(SessionFeature.PermanentSessionId);
        }

        public static string GetTemporarySessionId(this IRequest httpReq)
        {
            return httpReq.GetSessionParam(SessionFeature.SessionId);
        }

        public static string GetSessionParam(this IRequest httpReq, string sessionKey)
        {
            return httpReq.GetItem(sessionKey) as string
                ?? httpReq.GetHeader("X-" + sessionKey)
                ?? (HostContext.Config.AllowSessionIdsInHttpParams
                    ? (httpReq.QueryString[sessionKey] ?? httpReq.FormData[sessionKey])
                    : null)
                ?? httpReq.GetCookieValue(sessionKey);
        }

        /// <summary>
        /// Create the active Session or Permanent Session Id cookie.
        /// </summary>
        /// <returns></returns>
        public static string CreateSessionId(this IResponse res, IRequest req)
        {
            return req.IsPermanentSession()
                ? res.CreatePermanentSessionId(req)
                : res.CreateTemporarySessionId(req);
        }

        /// <summary>
        /// Create both Permanent and Session Id cookies and return the active sessionId
        /// </summary>
        /// <returns></returns>
        public static string CreateSessionIds(this IResponse res, IRequest req)
        {
            var permId = res.CreatePermanentSessionId(req);
            var tempId = res.CreateTemporarySessionId(req);
            return req.IsPermanentSession()
                ? permId
                : tempId;
        }

        static readonly RandomNumberGenerator randgen = RandomNumberGenerator.Create();

        [ThreadStatic] static byte[] SessionBytesCache;

        public static string CreateRandomSessionId()
        {
            if (SessionBytesCache == null)
                SessionBytesCache = new byte[15];

            string base64Id;
            do
            {
                PopulateWithSecureRandomBytes(SessionBytesCache);
                base64Id = Convert.ToBase64String(SessionBytesCache);
            } while (Base64StringContainsUrlUnfriendlyChars(base64Id));
            return base64Id;
        }

        public static void PopulateWithSecureRandomBytes(byte[] bytes)
        {
            randgen.GetBytes(bytes);
        }

        public static string CreateRandomBase64Id(int size = 15)
        {
            var data = new byte[size];
            randgen.GetBytes(data);
            return Convert.ToBase64String(data);
        }

        public static string CreateRandomBase62Id(int size)
        {
            var bytes = new byte[size];
            string base64Id;
            do
            {
                PopulateWithSecureRandomBytes(bytes);
                base64Id = Convert.ToBase64String(bytes);
            } while (Base64StringContainsUrlUnfriendlyChars(base64Id));
            return base64Id;
        }

        static readonly char[] UrlUnsafeBase64Chars = { '+', '/' };
        public static bool Base64StringContainsUrlUnfriendlyChars(string base64)
        {
            return base64.IndexOfAny(UrlUnsafeBase64Chars) >= 0;
        }

        public static string CreatePermanentSessionId(this IResponse res, IRequest req)
        {
            var sessionId = CreateRandomSessionId();

            var httpRes = res as IHttpResponse;
            httpRes?.Cookies.AddPermanentCookie(SessionFeature.PermanentSessionId, sessionId,
                HostContext.Config.OnlySendSessionCookiesSecurely && req.IsSecureConnection);

            req.Items[SessionFeature.PermanentSessionId] = sessionId;
            return sessionId;
        }

        public static string CreateTemporarySessionId(this IResponse res, IRequest req)
        {
            var sessionId = CreateRandomSessionId();

            var httpRes = res as IHttpResponse;
            httpRes?.Cookies.AddSessionCookie(SessionFeature.SessionId, sessionId,
                HostContext.Config.OnlySendSessionCookiesSecurely && req.IsSecureConnection);

            req.Items[SessionFeature.SessionId] = sessionId;
            return sessionId;
        }

        public static bool IsPermanentSession(this IRequest req)
        {
            return req != null && GetSessionOptions(req).Contains(SessionOptions.Permanent);
        }

        public static HashSet<string> GetSessionOptions(this IRequest httpReq)
        {
            var sessionOptions = httpReq.GetSessionParam(SessionFeature.SessionOptionsKey);
            return sessionOptions.IsNullOrEmpty()
                ? new HashSet<string>()
                : sessionOptions.Split(',').ToHashSet();
        }

        public static void UpdateSession(this IAuthSession session, IUserAuth userAuth)
        {
            if (userAuth == null || session == null) return;
            session.Roles = userAuth.Roles;
            session.Permissions = userAuth.Permissions;
        }

        public static void UpdateFromUserAuthRepo(this IAuthSession session, IRequest req, IAuthRepository userAuthRepo = null)
        {
            if (session == null)
                return;

            if (userAuthRepo == null)
                userAuthRepo = HostContext.AppHost.GetAuthRepository(req);

            if (userAuthRepo == null)
                return;

            using (userAuthRepo as IDisposable)
            {
                var userAuth = userAuthRepo.GetUserAuth(session, null);
                session.UpdateSession(userAuth);
            }
        }

        public static HashSet<string> AddSessionOptions(this IRequest req, params string[] options)
        {
            if (req == null || options.Length == 0)
                return new HashSet<string>();

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

            var strOptions = string.Join(",", existingOptions.ToArray());

            var httpRes = req.Response as IHttpResponse;
            httpRes?.Cookies.AddPermanentCookie(SessionFeature.SessionOptionsKey, strOptions);

            req.Items[SessionFeature.SessionOptionsKey] = strOptions;

            return existingOptions;
        }

        public static string GetSessionKey(IRequest httpReq = null)
        {
            var sessionId = httpReq.GetSessionId();
            return sessionId == null ? null : SessionFeature.GetSessionKey(sessionId);
        }

        public static TUserSession SessionAs<TUserSession>(this ICacheClient cache,
            IRequest httpReq = null, IResponse httpRes = null)
        {
            return SessionFeature.GetOrCreateSession<TUserSession>(cache, httpReq, httpRes);
        }

        public static IAuthSession GetUntypedSession(this ICacheClient cache,
            IRequest httpReq = null, IResponse httpRes = null)
        {
            var sessionKey = GetSessionKey(httpReq);

            if (sessionKey != null)
            {
                var userSession = cache.Get<IAuthSession>(sessionKey);
                if (!Equals(userSession, default(AuthUserSession)))
                    return userSession;
            }

            if (sessionKey == null)
                SessionFeature.CreateSessionIds(httpReq, httpRes);

            var unAuthorizedSession = (IAuthSession)typeof(AuthUserSession).CreateInstance();
            return unAuthorizedSession;
        }

        public static void ClearSession(this ICacheClient cache, IRequest httpReq = null)
        {
            cache.Remove(GetSessionKey(httpReq));
        }

        public static ISession GetSessionBag(this IRequest request)
        {
            var factory = request.TryResolve<ISessionFactory>() ?? new SessionFactory(request.GetCacheClient());
            return factory.GetOrCreateSession(request, request.Response);
        }

        public static ISession GetSessionBag(this IServiceBase service)
        {
            return service.Request.GetSessionBag();
        }

        public static T Get<T>(this ISession session)
        {
            return session.Get<T>(typeof(T).Name);
        }

        public static void Set<T>(this ISession session, T value)
        {
            session.Set(typeof(T).Name, value);
        }

        public static void DeleteSessionCookies(this IResponse response)
        {
            var httpRes = response as IHttpResponse;
            if (httpRes == null) return;
            httpRes.Cookies.DeleteCookie(Keywords.SessionId);
            httpRes.Cookies.DeleteCookie(Keywords.PermanentSessionId);
            httpRes.Cookies.DeleteCookie(HttpHeaders.XUserAuthId);
        }

        public static void DeleteJwtCookie(this IResponse response)
        {
            var httpRes = response as IHttpResponse;
            httpRes?.Cookies.DeleteCookie(Keywords.TokenCookie);
        }

        public static void GenerateNewSessionCookies(this IRequest req, IAuthSession session)
        {
            var httpRes = req.Response as IHttpResponse;
            if (httpRes == null)
                return;

            var sessionId = req.GetSessionId();
            if (sessionId != null)
                req.RemoveSession(sessionId);

            req.Response.ClearCookies();

            var tempId = req.Response.CreateTemporarySessionId(req);
            var permId = req.Response.CreatePermanentSessionId(req);

            var isPerm = req.IsPermanentSession();
            req.AddSessionOptions(isPerm ? SessionOptions.Permanent : SessionOptions.Temporary);

            session.Id = isPerm
                ? permId
                : tempId;

            req.Items[Keywords.Session] = session;
        }
    }
}