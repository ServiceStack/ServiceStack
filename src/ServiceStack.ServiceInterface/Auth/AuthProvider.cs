using System;
using System.Collections.Generic;
using System.Net;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.ServiceInterface.Auth
{
    public abstract class AuthProvider : IAuthProvider
    {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(AuthProvider));
        public static TimeSpan DefaultSessionExpiry = TimeSpan.FromDays(7 * 2); //2 weeks

        public TimeSpan? SessionExpiry { get; set; }
        public string AuthRealm { get; set; }
        public string Provider { get; set; }
        public string CallbackUrl { get; set; }
        public string RedirectUrl { get; set; }

        protected AuthProvider() { }

        protected AuthProvider(IResourceManager appSettings, string authRealm, string oAuthProvider)
        {
            this.AuthRealm = appSettings.Get("OAuthRealm", authRealm);

            this.Provider = oAuthProvider;
            this.CallbackUrl = appSettings.GetString("oauth.{0}.CallbackUrl".Fmt(oAuthProvider));
            this.RedirectUrl = appSettings.GetString("oauth.{0}.RedirectUrl".Fmt(oAuthProvider));
            this.SessionExpiry = DefaultSessionExpiry;
        }

        /// <summary>
        /// Remove the Users Session
        /// </summary>
        /// <param name="service"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual object Logout(IServiceBase service, Auth request)
        {
            var session = service.GetSession();
            var referrerUrl = (request != null ? request.Continue : null)
                ?? session.ReferrerUrl
                ?? service.RequestContext.GetHeader("Referer")
                ?? this.CallbackUrl;

            session.OnLogout(service);

            service.RemoveSession();

            if (service.RequestContext.ResponseContentType == ContentType.Html && !String.IsNullOrEmpty(referrerUrl))
                return service.Redirect(referrerUrl.AddHashParam("s", "-1"));

            return new AuthResponse();
        }

        /// <summary>
        /// Saves the Auth Tokens for this request. Called in OnAuthenticated(). 
        /// Overrideable, the default behaviour is to call IUserAuthRepository.CreateOrMergeAuthSession().
        /// </summary>
        protected virtual void SaveUserAuth(IServiceBase authService, IAuthSession session, IUserAuthRepository authRepo, IOAuthTokens tokens)
        {
            if (authRepo == null) return;
            if (tokens != null)
            {
                session.UserAuthId = authRepo.CreateOrMergeAuthSession(session, tokens);
            }

            authRepo.LoadUserAuth(session, tokens);

            foreach (var oAuthToken in session.ProviderOAuthAccess)
            {
                var authProvider = AuthService.GetAuthProvider(oAuthToken.Provider);
                if (authProvider == null) continue;
                var userAuthProvider = authProvider as OAuthProvider;
                if (userAuthProvider != null)
                {
                    userAuthProvider.LoadUserOAuthProvider(session, oAuthToken);
                }
            }

            authRepo.SaveUserAuth(session);

            var httpRes = authService.RequestContext.Get<IHttpResponse>();
            if (httpRes != null)
            {
                httpRes.Cookies.AddPermanentCookie(HttpHeaders.XUserAuthId, session.UserAuthId);
            }
            OnSaveUserAuth(authService, session);
        }

        public virtual void OnSaveUserAuth(IServiceBase authService, IAuthSession session) { }

        public virtual void OnAuthenticated(IServiceBase authService, IAuthSession session, IOAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            var userSession = session as AuthUserSession;
            if (userSession != null)
            {
                LoadUserAuthInfo(userSession, tokens, authInfo);
            }

            var authRepo = authService.TryResolve<IUserAuthRepository>();
            if (authRepo != null)
            {
                if (tokens != null)
                {
                    authInfo.ForEach((x, y) => tokens.Items[x] = y);
                    session.UserAuthId = authRepo.CreateOrMergeAuthSession(session, tokens);
                }
                //SaveUserAuth(authService, userSession, authRepo, tokens);
                
                authRepo.LoadUserAuth(session, tokens);

                foreach (var oAuthToken in session.ProviderOAuthAccess)
                {
                    var authProvider = AuthService.GetAuthProvider(oAuthToken.Provider);
                    if (authProvider == null) continue;
                    var userAuthProvider = authProvider as OAuthProvider;
                    if (userAuthProvider != null)
                    {
                        userAuthProvider.LoadUserOAuthProvider(session, oAuthToken);
                    }
                }
        
                var httpRes = authService.RequestContext.Get<IHttpResponse>();
                if (httpRes != null)
                {
                    httpRes.Cookies.AddPermanentCookie(HttpHeaders.XUserAuthId, session.UserAuthId);
                }
                
            }

            //OnSaveUserAuth(authService, session);
            authService.SaveSession(session, SessionExpiry);
            session.OnAuthenticated(authService, session, tokens, authInfo);
        }

        protected virtual void LoadUserAuthInfo(AuthUserSession userSession, IOAuthTokens tokens, Dictionary<string, string> authInfo) { }

        protected static bool LoginMatchesSession(IAuthSession session, string userName)
        {
            if (userName == null) return false;
            var isEmail = userName.Contains("@");
            if (isEmail)
            {
                if (!userName.EqualsIgnoreCase(session.Email))
                    return false;
            }
            else
            {
                if (!userName.EqualsIgnoreCase(session.UserName))
                    return false;
            }
            return true;
        }

        public abstract bool IsAuthorized(IAuthSession session, IOAuthTokens tokens, Auth request = null);

        public abstract object Authenticate(IServiceBase authService, IAuthSession session, Auth request);

        public virtual void OnFailedAuthentication(IAuthSession session, IHttpRequest httpReq, IHttpResponse httpRes)
        {
            httpRes.StatusCode = (int)HttpStatusCode.Unauthorized;
            httpRes.AddHeader(HttpHeaders.WwwAuthenticate, "{0} realm=\"{1}\"".Fmt(this.Provider, this.AuthRealm));
            httpRes.EndServiceStackRequest();
        }

        public static void HandleFailedAuth(IAuthProvider authProvider,
            IAuthSession session, IHttpRequest httpReq, IHttpResponse httpRes)
        {
            var baseAuthProvider = authProvider as AuthProvider;
            if (baseAuthProvider != null)
            {
                baseAuthProvider.OnFailedAuthentication(session, httpReq, httpRes);
                return;
            }

            httpRes.StatusCode = (int)HttpStatusCode.Unauthorized;
            httpRes.AddHeader(HttpHeaders.WwwAuthenticate, "{0} realm=\"{1}\""
                .Fmt(authProvider.Provider, authProvider.AuthRealm));

            httpRes.EndServiceStackRequest();
        }
    }

    public static class AuthConfigExtensions
    {
        public static bool IsAuthorizedSafe(this IAuthProvider authProvider, IAuthSession session, IOAuthTokens tokens)
        {
            return authProvider != null && authProvider.IsAuthorized(session, tokens);
        }
    }

}

