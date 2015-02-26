using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using ServiceStack.Auth;
using ServiceStack.Caching;
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

        public static string GetSessionId(this IRequest httpReq)
        {
            if (httpReq == null)
                httpReq = HostContext.GetCurrentRequest();

            var sessionOptions = GetSessionOptions(httpReq);

            return sessionOptions.Contains(SessionOptions.Permanent)
                ? httpReq.GetPermanentSessionId()
                : httpReq.GetTemporarySessionId();
        }

        public static string GetPermanentSessionId(this IRequest httpReq)
        {
            return httpReq.GetItemOrCookie(SessionFeature.PermanentSessionId)
                ?? httpReq.GetHeader("X-" + SessionFeature.PermanentSessionId);
        }

        public static string GetTemporarySessionId(this IRequest httpReq)
        {
            return httpReq.GetItemOrCookie(SessionFeature.SessionId)
                ?? httpReq.GetHeader("X-" + SessionFeature.SessionId);
        }

        /// <summary>
        /// Create the active Session or Permanent Session Id cookie.
        /// </summary>
        /// <returns></returns>
        public static string CreateSessionId(this IResponse res, IRequest req)
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
        public static string CreateSessionIds(this IResponse res, IRequest req)
        {
            var sessionOptions = GetSessionOptions(req);
            var permId = res.CreatePermanentSessionId(req);
            var tempId = res.CreateTemporarySessionId(req);
            return sessionOptions.Contains(SessionOptions.Permanent)
                ? permId
                : tempId;
        }

        static readonly RandomNumberGenerator randgen = new RNGCryptoServiceProvider();
        public static string CreateRandomSessionId()
        {
            string base64Id;
            do
            {
                base64Id = CreateRandomBase64Id();
            } while (Base64StringContainsUrlUnfriendlyChars(base64Id));
            return base64Id;
        }

        public static string CreateRandomBase64Id()
        {
            var data = new byte[15];
            randgen.GetBytes(data);
            return Convert.ToBase64String(data);
        }

        static readonly char[] UrlUnsafeBase64Chars = new[] { '+', '/' };
        public static bool Base64StringContainsUrlUnfriendlyChars(string base64)
        {
            return base64.IndexOfAny(UrlUnsafeBase64Chars) >= 0;
        }

        public static string CreatePermanentSessionId(this IResponse res, IRequest req)
        {
            var sessionId = CreateRandomSessionId();

            var httpRes = res as IHttpResponse;
            if (httpRes != null)
                httpRes.Cookies.AddPermanentCookie(SessionFeature.PermanentSessionId, sessionId);

            req.Items[SessionFeature.PermanentSessionId] = sessionId;
            return sessionId;
        }

        public static string CreateTemporarySessionId(this IResponse res, IRequest req)
        {
            var sessionId = CreateRandomSessionId();

            var httpRes = res as IHttpResponse;
            if (httpRes != null)
            {
                httpRes.Cookies.AddSessionCookie(SessionFeature.SessionId, sessionId,
                    (HostContext.Config.OnlySendSessionCookiesSecurely && req.IsSecureConnection));
            }

            req.Items[SessionFeature.SessionId] = sessionId;
            return sessionId;
        }

        public static HashSet<string> GetSessionOptions(this IRequest httpReq)
        {
            var sessionOptions = httpReq.GetItemOrCookie(SessionFeature.SessionOptionsKey);
            var headerOptions = httpReq.GetHeader("X-" + SessionFeature.SessionOptionsKey);
            if (headerOptions != null)
            {
                sessionOptions = sessionOptions.IsNullOrEmpty()
                    ? headerOptions
                    : headerOptions + "," + sessionOptions;
            }                
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
            if (userAuthRepo == null)
                userAuthRepo = req.TryResolve<IAuthRepository>();

            if (userAuthRepo == null) return;

            var userAuth = userAuthRepo.GetUserAuth(session, null);
            session.UpdateSession(userAuth);
        }

        public static HashSet<string> AddSessionOptions(this IRequest req, params string[] options)
        {
            if (req == null || options.Length == 0) return new HashSet<string>();

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

            var httpRes = req.Response as IHttpResponse;
            if (httpRes != null)
                httpRes.Cookies.AddPermanentCookie(SessionFeature.SessionOptionsKey, strOptions);

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
            httpRes.Cookies.DeleteCookie(SessionFeature.SessionId);
            httpRes.Cookies.DeleteCookie(SessionFeature.PermanentSessionId);
        }
    }
}