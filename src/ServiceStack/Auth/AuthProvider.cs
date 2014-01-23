using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public abstract class AuthProvider : IAuthProvider
    {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(AuthProvider));

        public TimeSpan SessionExpiry { get; set; }
        public string AuthRealm { get; set; }
        public string Provider { get; set; }
        public string CallbackUrl { get; set; }
        public string RedirectUrl { get; set; }

        protected AuthProvider()
        {
            this.SessionExpiry = SessionFeature.DefaultSessionExpiry;
        }

        protected AuthProvider(IAppSettings appSettings, string authRealm, string oAuthProvider)
            : this()
        {
            // Enhancement per https://github.com/ServiceStack/ServiceStack/issues/741
            this.AuthRealm = appSettings != null ? appSettings.Get("OAuthRealm", authRealm) : authRealm;

            this.Provider = oAuthProvider;
            if (appSettings != null)
            {
                this.CallbackUrl = appSettings.GetString("oauth.{0}.CallbackUrl".Fmt(oAuthProvider))
                    ?? FallbackConfig(appSettings.GetString("oauth.CallbackUrl"));
                this.RedirectUrl = appSettings.GetString("oauth.{0}.RedirectUrl".Fmt(oAuthProvider))
                    ?? FallbackConfig(appSettings.GetString("oauth.RedirectUrl"));
            }
        }

        /// <summary>
        /// Allows specifying a global fallback config that if exists is formatted with the Provider as the first arg.
        /// E.g. this appSetting with the TwitterAuthProvider: 
        /// oauth.CallbackUrl="http://localhost:11001/auth/{0}"
        /// Would result in: 
        /// oauth.CallbackUrl="http://localhost:11001/auth/twitter"
        /// </summary>
        /// <returns></returns>
        protected string FallbackConfig(string fallback)
        {
            return fallback != null ? fallback.Fmt(Provider) : null;
        }

        /// <summary>
        /// Remove the Users Session
        /// </summary>
        /// <param name="service"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual object Logout(IServiceBase service, Authenticate request)
        {
            var session = service.GetSession();
            var referrerUrl = (request != null ? request.Continue : null)
                ?? session.ReferrerUrl
                ?? service.Request.GetHeader("Referer")
                ?? this.CallbackUrl;

            session.OnLogout(service);

            service.RemoveSession();

            if (service.Request.ResponseContentType == MimeTypes.Html && !String.IsNullOrEmpty(referrerUrl))
                return service.Redirect(referrerUrl.AddHashParam("s", "-1"));

            return new AuthenticateResponse();
        }

        /// <summary>
        /// Saves the Auth Tokens for this request. Called in OnAuthenticated(). 
        /// Overrideable, the default behaviour is to call IUserAuthRepository.CreateOrMergeAuthSession().
        /// </summary>
        protected virtual void SaveUserAuth(IServiceBase authService, IAuthSession session, IAuthRepository authRepo, IAuthTokens tokens)
        {
            if (authRepo == null) return;
            if (tokens != null)
            {
                session.UserAuthId = authRepo.CreateOrMergeAuthSession(session, tokens);
            }

            authRepo.LoadUserAuth(session, tokens);

            foreach (var oAuthToken in session.ProviderOAuthAccess)
            {
                var authProvider = AuthenticateService.GetAuthProvider(oAuthToken.Provider);
                if (authProvider == null) continue;
                var userAuthProvider = authProvider as OAuthProvider;
                if (userAuthProvider != null)
                {
                    userAuthProvider.LoadUserOAuthProvider(session, oAuthToken);
                }
            }

            authRepo.SaveUserAuth(session);

            var httpRes = authService.Request.Response as IHttpResponse;
            if (httpRes != null)
            {
                httpRes.Cookies.AddPermanentCookie(HttpHeaders.XUserAuthId, session.UserAuthId);
            }
            OnSaveUserAuth(authService, session);
        }

        public virtual void OnSaveUserAuth(IServiceBase authService, IAuthSession session) { }

        public virtual void OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            var userSession = session as AuthUserSession;
            if (userSession != null)
            {
                LoadUserAuthInfo(userSession, tokens, authInfo);
            }

            var authRepo = authService.TryResolve<IAuthRepository>();
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
                    var authProvider = AuthenticateService.GetAuthProvider(oAuthToken.Provider);
                    if (authProvider == null) continue;
                    var userAuthProvider = authProvider as OAuthProvider;
                    if (userAuthProvider != null)
                    {
                        userAuthProvider.LoadUserOAuthProvider(session, oAuthToken);
                    }
                }
        
                var httpRes = authService.Request.Response as IHttpResponse;
                if (httpRes != null)
                {
                    httpRes.Cookies.AddPermanentCookie(HttpHeaders.XUserAuthId, session.UserAuthId);
                }
                
            }

            try
            {
                session.OnAuthenticated(authService, session, tokens, authInfo);
            }
            finally
            {
                authService.SaveSession(session, SessionExpiry);
            }
        }

        protected virtual void LoadUserAuthInfo(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo) { }

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

        public abstract bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null);

        public abstract object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request);

        public virtual void OnFailedAuthentication(IAuthSession session, IRequest httpReq, IResponse httpRes)
        {
            httpRes.StatusCode = (int)HttpStatusCode.Unauthorized;
            httpRes.AddHeader(HttpHeaders.WwwAuthenticate, "{0} realm=\"{1}\"".Fmt(this.Provider, this.AuthRealm));
            httpRes.EndRequest();
        }

        public static void HandleFailedAuth(IAuthProvider authProvider,
            IAuthSession session, IRequest httpReq, IResponse httpRes)
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

            httpRes.EndRequest();
        }
    }

    public static class AuthExtensions
    {
        public static bool IsAuthorizedSafe(this IAuthProvider authProvider, IAuthSession session, IAuthTokens tokens)
        {
            return authProvider != null && authProvider.IsAuthorized(session, tokens);
        }

        public static IUserAuthRepository AsUserAuthRepository(this IAuthRepository authRepo, IResolver resolver=null)
        {
            if (resolver == null)
                resolver = HostContext.AppHost;

            var userAuthRepo = resolver.TryResolve<IUserAuthRepository>()
                ?? (authRepo as IUserAuthRepository);

            if (userAuthRepo == null)
                throw new ConfigurationErrorsException(
                    "Required dependency IAuthRepository or IUserAuthRepository could not be found.");

            return userAuthRepo;
        }
    }

}

