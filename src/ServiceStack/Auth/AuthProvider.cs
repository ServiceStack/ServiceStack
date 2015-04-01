using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using ServiceStack.Configuration;
using ServiceStack.Logging;
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

        public Action<AuthUserSession, IAuthTokens, Dictionary<string, string>> LoadUserAuthFilter { get; set; }

        public Func<AuthContext, IHttpResult> CustomValidationFilter { get; set; }

        public Func<AuthProvider, string, string> PreAuthUrlFilter = UrlFilter;
        public Func<AuthProvider, string, string> AccessTokenUrlFilter = UrlFilter;
        public Func<AuthProvider, string, string> SuccessRedirectUrlFilter = UrlFilter;
        public Func<AuthProvider, string, string> FailedRedirectUrlFilter = UrlFilter;
        public Func<AuthProvider, string, string> LogoutUrlFilter = UrlFilter;

        public static string UrlFilter(AuthProvider provider, string url)
        {
            return url;
        }
        
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

        public IAuthEvents AuthEvents
        {
            get { return HostContext.TryResolve<IAuthEvents>() ?? new AuthEvents(); }
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
            AuthEvents.OnLogout(service.Request, session, service);

            service.RemoveSession();

            var feature = HostContext.GetPlugin<AuthFeature>();
            if (feature != null && feature.DeleteSessionCookiesOnLogout)
            {
                service.Request.Response.DeleteSessionCookies();
            }

            if (service.Request.ResponseContentType == MimeTypes.Html && !String.IsNullOrEmpty(referrerUrl))
                return service.Redirect(LogoutUrlFilter(this, referrerUrl.SetParam("s", "-1")));

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
                session.UserAuthId = authRepo.CreateOrMergeAuthSession(session, tokens).UserAuthId.ToString();
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

        public virtual IHttpResult OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            var userSession = session as AuthUserSession;
            if (userSession != null)
            {
                LoadUserAuthInfo(userSession, tokens, authInfo);
                HostContext.TryResolve<IAuthMetadataProvider>().SafeAddMetadata(tokens, authInfo);

                if (LoadUserAuthFilter != null)
                {
                    LoadUserAuthFilter(userSession, tokens, authInfo);
                }
            }

            var hasTokens = tokens != null && authInfo != null;
            if (hasTokens)
            {
                authInfo.ForEach((x, y) => tokens.Items[x] = y);
            }

            var authRepo = authService.TryResolve<IAuthRepository>();

            if (CustomValidationFilter != null)
            {
                var ctx = new AuthContext
                {
                    Service = authService,
                    AuthProvider = this,
                    Session = session,
                    AuthTokens = tokens,
                    AuthInfo = authInfo,
                    AuthRepository = authRepo,
                };
                var response = CustomValidationFilter(ctx);
                if (response != null)
                {
                    authService.RemoveSession();
                    return response;
                }
            }

            if (authRepo != null)
            {
                var failed = ValidateAccount(authService, authRepo, session, tokens);
                if (failed != null)
                {
                    authService.RemoveSession();
                    return failed;
                }

                if (hasTokens)
                {
                    var authDetails = authRepo.CreateOrMergeAuthSession(session, tokens);
                    session.UserAuthId = authDetails.UserAuthId.ToString();

                    var firstTimeAuthenticated = authDetails.CreatedDate == authDetails.ModifiedDate;
                    if (firstTimeAuthenticated)
                    {
                        session.OnRegistered(authService.Request, session, authService);
                        AuthEvents.OnRegistered(authService.Request, session, authService);
                    }
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

                var httpRes = authService.Request.Response as IHttpResponse;
                if (session.UserAuthId != null && httpRes != null)
                {
                    httpRes.Cookies.AddPermanentCookie(HttpHeaders.XUserAuthId, session.UserAuthId);
                }
            }
            else
            {
                if (hasTokens)
                {
                    session.UserAuthId = CreateOrMergeAuthSession(session, tokens);
                }
            }

            try
            {
                session.IsAuthenticated = true;
                session.OnAuthenticated(authService, session, tokens, authInfo);
                AuthEvents.OnAuthenticated(authService.Request, session, authService, tokens, authInfo);
            }
            finally
            {
                authService.SaveSession(session, SessionExpiry);
            }

            return null;
        }

        // Keep in-memory map of userAuthId's when no IAuthRepository exists 
        private static long transientUserAuthId;
        static readonly ConcurrentDictionary<string, long> transientUserIdsMap = new ConcurrentDictionary<string, long>();

        // Merge tokens into session when no IAuthRepository exists
        public virtual string CreateOrMergeAuthSession(IAuthSession session, IAuthTokens tokens)
        {
            if (session.UserName.IsNullOrEmpty())
                session.UserName = tokens.UserName;
            if (session.DisplayName.IsNullOrEmpty())
                session.DisplayName = tokens.DisplayName;
            if (session.Email.IsNullOrEmpty())
                session.Email = tokens.Email;

            var oAuthProvider = session.ProviderOAuthAccess.FirstOrDefault(
                x => x.Provider == tokens.Provider && x.UserId == tokens.UserId);
            if (oAuthProvider != null)
            {
                if (!oAuthProvider.UserName.IsNullOrEmpty())
                    session.UserName = oAuthProvider.UserName;
                if (!oAuthProvider.DisplayName.IsNullOrEmpty())
                    session.DisplayName = oAuthProvider.DisplayName;
                if (!oAuthProvider.Email.IsNullOrEmpty())
                    session.Email = oAuthProvider.Email;
                if (!oAuthProvider.FirstName.IsNullOrEmpty())
                    session.FirstName = oAuthProvider.FirstName;
                if (!oAuthProvider.LastName.IsNullOrEmpty())
                    session.LastName = oAuthProvider.LastName;
            }

            var key = tokens.Provider + ":" + (tokens.UserId ?? tokens.UserName);
            return transientUserIdsMap.GetOrAdd(key,
                k => Interlocked.Increment(ref transientUserAuthId)).ToString(CultureInfo.InvariantCulture);
        }

        protected virtual void LoadUserAuthInfo(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo) { }

        protected static bool LoginMatchesSession(IAuthSession session, string userName)
        {
            if (session == null || userName == null) return false;
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

        protected virtual bool UserNameAlreadyExists(IAuthRepository authRepo, IUserAuth userAuth, IAuthTokens tokens = null)
        {
            if (tokens != null && tokens.UserName != null)
            {
                var userWithUserName = authRepo.GetUserAuthByUserName(tokens.UserName);
                if (userWithUserName == null)
                    return false;

                var isAnotherUser = userAuth == null || (userAuth.Id != userWithUserName.Id);
                return isAnotherUser;
            }
            return false;
        }

        protected virtual bool EmailAlreadyExists(IAuthRepository authRepo, IUserAuth userAuth, IAuthTokens tokens = null)
        {
            if (tokens != null && tokens.Email != null)
            {
                var userWithEmail = authRepo.GetUserAuthByUserName(tokens.Email);
                if (userWithEmail == null) 
                    return false;

                var isAnotherUser = userAuth == null || (userAuth.Id != userWithEmail.Id);
                return isAnotherUser;
            }
            return false;
        }

        protected virtual string GetAuthRedirectUrl(IServiceBase authService, IAuthSession session)
        {
            return session.ReferrerUrl;
        }

        protected virtual bool IsAccountLocked(IAuthRepository authRepo, IUserAuth userAuth, IAuthTokens tokens=null)
        {
            if (userAuth == null) return false;
            return userAuth.LockedDate != null;
        }

        protected virtual IHttpResult ValidateAccount(IServiceBase authService, IAuthRepository authRepo, IAuthSession session, IAuthTokens tokens)
        {
            var userAuth = authRepo.GetUserAuth(session, tokens);

            var authFeature = HostContext.GetPlugin<AuthFeature>();

            if (authFeature != null && authFeature.ValidateUniqueUserNames && UserNameAlreadyExists(authRepo, userAuth, tokens))
            {
                return authService.Redirect(FailedRedirectUrlFilter(this, GetReferrerUrl(authService, session).SetParam("f", "UserNameAlreadyExists")));
            }

            if (authFeature != null && authFeature.ValidateUniqueEmails && EmailAlreadyExists(authRepo, userAuth, tokens))
            {
                return authService.Redirect(FailedRedirectUrlFilter(this, GetReferrerUrl(authService, session).SetParam("f", "EmailAlreadyExists")));
            }

            if (IsAccountLocked(authRepo, userAuth, tokens))
            {
                return authService.Redirect(FailedRedirectUrlFilter(this, GetReferrerUrl(authService, session).SetParam("f", "AccountLocked")));
            }

            return null;
        }

        protected virtual string GetReferrerUrl(IServiceBase authService, IAuthSession session, Authenticate request = null)
        {
            if (request == null)
                request = authService.Request.Dto as Authenticate;

            var referrerUrl = session.ReferrerUrl;
            if (referrerUrl.IsNullOrEmpty())
                referrerUrl = (request != null ? request.Continue : null)
                    ?? authService.Request.GetHeader("Referer");

            var requestUri = authService.Request.AbsoluteUri;
            if (referrerUrl.IsNullOrEmpty()
                || referrerUrl.IndexOf("/auth", StringComparison.OrdinalIgnoreCase) >= 0)
                referrerUrl = this.RedirectUrl
                    ?? authService.Request.GetBaseUrl()
                    ?? requestUri.InferBaseUrl();

            return referrerUrl;
        }
    }

    public class AuthContext
    {
        public IServiceBase Service { get; set; }
        public AuthProvider AuthProvider { get; set; }
        public IAuthSession Session { get; set; }
        public IAuthTokens AuthTokens { get; set; }
        public Dictionary<string, string> AuthInfo { get; set; }
        public IAuthRepository AuthRepository { get; set; }
    }

    public static class AuthExtensions
    {
        public static bool IsAuthorizedSafe(this IAuthProvider authProvider, IAuthSession session, IAuthTokens tokens)
        {
            return authProvider != null && authProvider.IsAuthorized(session, tokens);
        }

        public static IUserAuthRepository AsUserAuthRepository(this IAuthRepository authRepo, IResolver resolver = null)
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

        public static string SanitizeOAuthUrl(this string url)
        {
            return (url ?? "").Replace("\\/", "/");
        }
    }

}

