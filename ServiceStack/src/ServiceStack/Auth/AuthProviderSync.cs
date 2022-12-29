using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Logging;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    /// <summary>
    /// Sync AuthProvider base class for compatibility with Sync Auth Providers
    /// </summary>
    public abstract class AuthProviderSync : IAuthProvider, IAuthPlugin
    {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(AuthProviderSync));

        public virtual string Type => GetType().Name;
        public virtual Dictionary<string, string> Meta => null;

        public TimeSpan? SessionExpiry { get; set; }
        public string AuthRealm { get; set; }
        public string Provider { get; set; }
        public string CallbackUrl { get; set; }
        public string RedirectUrl { get; set; }

        public bool PersistSession { get; set; }
        public bool SaveExtendedUserInfo { get; set; }
        
        public bool? RestoreSessionFromState { get; set; }

        public Action<AuthUserSession, IAuthTokens, Dictionary<string, string>> LoadUserAuthFilter { get; set; }

        public Func<AuthContext, IHttpResult> CustomValidationFilter { get; set; }

        public Func<AuthProviderSync, string, string> PreAuthUrlFilter = UrlFilter;
        public Func<AuthProviderSync, string, string> AccessTokenUrlFilter = UrlFilter;
        public Func<AuthProviderSync, string, string> SuccessRedirectUrlFilter = UrlFilter;
        public Func<AuthProviderSync, string, string> FailedRedirectUrlFilter = UrlFilter;
        public Func<AuthProviderSync, string, string> LogoutUrlFilter = UrlFilter;
        
        public Func<IAuthRepository, IUserAuth, IAuthTokens, bool> AccountLockedValidator { get; set; }

        public static string UrlFilter(AuthProviderSync provider, string url) => url;

        public NavItem NavItem { get; set; }

        protected AuthProviderSync()
        {
#pragma warning disable CS0618
            PersistSession = !(GetType().HasInterface(typeof(IAuthWithRequest)) || GetType().HasInterface(typeof(IAuthWithRequestSync)));
#pragma warning restore CS0618
        }

        protected AuthProviderSync(IAppSettings appSettings, string authRealm, string oAuthProvider)
            : this()
        {
            // Enhancement per https://github.com/ServiceStack/ServiceStack/issues/741
            this.AuthRealm = appSettings != null ? appSettings.Get("OAuthRealm", authRealm) : authRealm;

            this.Provider = oAuthProvider;
            if (appSettings != null)
            {
                this.CallbackUrl = appSettings.GetString($"oauth.{oAuthProvider}.CallbackUrl")
                    ?? FallbackConfig(appSettings.GetString("oauth.CallbackUrl"));
                this.RedirectUrl = appSettings.GetString($"oauth.{oAuthProvider}.RedirectUrl")
                    ?? FallbackConfig(appSettings.GetString("oauth.RedirectUrl"));
            }
        }

        public IAuthEvents AuthEvents => HostContext.TryResolve<IAuthEvents>() ?? new AuthEvents();

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
            return fallback?.Fmt(Provider);
        }

        /// <summary>
        /// Remove the Users Session
        /// </summary>
        /// <param name="service"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual object Logout(IServiceBase service, Authenticate request)
        {
            var feature = HostContext.GetPlugin<AuthFeature>();

            var session = service.GetSession();
            var referrerUrl = service.Request.GetReturnUrl()
                ?? (feature.HtmlLogoutRedirect != null ? service.Request.ResolveAbsoluteUrl(feature.HtmlLogoutRedirect) : null)
                ?? session.ReferrerUrl
                ?? service.Request.GetHeader("Referer").NotLogoutUrl()
                ?? this.RedirectUrl;

            session.OnLogout(service);
            AuthEvents.OnLogout(service.Request, session, service);

            service.RemoveSession();

            if (feature != null && feature.DeleteSessionCookiesOnLogout)
            {
                service.Request.Response.DeleteSessionCookies();
                service.Request.Response.DeleteJwtCookie();
            }

            if (service.Request.ResponseContentType == MimeTypes.Html && !string.IsNullOrEmpty(referrerUrl))
                return service.Redirect(LogoutUrlFilter(this, referrerUrl));

            return new AuthenticateResponse();
        }

        public HashSet<string> ExcludeAuthInfoItems { get; set; } = new HashSet<string>(new[]{ "user_id", "email", "username", "name", "first_name", "last_name", "email" }, StringComparer.OrdinalIgnoreCase);

        public virtual IHttpResult OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            session.AuthProvider = Provider;

            if (session is AuthUserSession userSession)
            {
                LoadUserAuthInfo(userSession, tokens, authInfo);
                HostContext.TryResolve<IAuthMetadataProvider>().SafeAddMetadata(tokens, authInfo);

                LoadUserAuthFilter?.Invoke(userSession, tokens, authInfo);
            }

            var hasTokens = tokens != null && authInfo != null;
            if (hasTokens && SaveExtendedUserInfo)
            {
                if (tokens.Items == null)
                    tokens.Items = new Dictionary<string, string>();

                foreach (var entry in authInfo)
                {
                    if (ExcludeAuthInfoItems.Contains(entry.Key)) 
                        continue;

                    tokens.Items[entry.Key] = entry.Value;
                }
            }

            if (session is IAuthSessionExtended authSession)
            {
                var failed = authSession.Validate(authService, session, tokens, authInfo)
                    ?? AuthEvents.Validate(authService, session, tokens, authInfo);
                if (failed != null)
                {
                    authService.RemoveSession();
                    return failed;
                }
            }

            var authRepo = GetAuthRepository(authService.Request);
            using (authRepo as IDisposable)
            {
                if (CustomValidationFilter != null)
                {
                    var ctx = new AuthContext
                    {
                        Request = authService.Request,
                        Service = authService,
                        AuthProviderSync = this,
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

                    foreach (var oAuthToken in session.GetAuthTokens())
                    {
                        var authProvider = AuthenticateService.GetAuthProvider(oAuthToken.Provider);
                        var userAuthProvider = authProvider as OAuthProvider;
                        userAuthProvider?.LoadUserOAuthProviderAsync(session, oAuthToken);
                    }

                    var httpRes = authService.Request.Response as IHttpResponse;
                    if (session.UserAuthId != null)
                    {
                        httpRes?.Cookies.AddPermanentCookie(HttpHeaders.XUserAuthId, session.UserAuthId);
                    }
                }
                else
                {
                    if (hasTokens)
                    {
                        session.UserAuthId = CreateOrMergeAuthSession(session, tokens);
                    }
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
#pragma warning disable 618
                this.SaveSession(authService, session, SessionExpiry);
#pragma warning restore 618
                authService.Request.CompletedAuthentication();
            }

            return null;
        }

        protected virtual IAuthRepository GetAuthRepository(IRequest req)
        {
            return HostContext.AppHost.GetAuthRepository(req);
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

            var oAuthTokens = session.GetAuthTokens(tokens.Provider);
            if (oAuthTokens != null && oAuthTokens.UserId == tokens.UserId)
            {
                if (!oAuthTokens.UserName.IsNullOrEmpty())
                    session.UserName = oAuthTokens.UserName;
                if (!oAuthTokens.DisplayName.IsNullOrEmpty())
                    session.DisplayName = oAuthTokens.DisplayName;
                if (!oAuthTokens.Email.IsNullOrEmpty())
                    session.Email = oAuthTokens.Email;
                if (!oAuthTokens.FirstName.IsNullOrEmpty())
                    session.FirstName = oAuthTokens.FirstName;
                if (!oAuthTokens.LastName.IsNullOrEmpty())
                    session.LastName = oAuthTokens.LastName;
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
                if (!userName.EqualsIgnoreCase(session.UserAuthName))
                    return false;
            }
            return true;
        }

        public Task<object> LogoutAsync(IServiceBase service, Authenticate request, CancellationToken token = default)
        {
            return Logout(service, request).InTask();
        }

        public Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token = default)
        {
            var response = Authenticate(authService, session, request);
            return response.InTask();
        }

        public abstract bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null);

        public abstract object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request);

        public virtual Task OnFailedAuthentication(IAuthSession session, IRequest httpReq, IResponse httpRes)
        {
            httpRes.StatusCode = (int)HttpStatusCode.Unauthorized;
            httpRes.AddHeader(HttpHeaders.WwwAuthenticate, "{0} realm=\"{1}\"".Fmt(this.Provider, this.AuthRealm));
            return HostContext.AppHost.HandleShortCircuitedErrors(httpReq, httpRes, httpReq.Dto);
        }

        protected virtual bool UserNameAlreadyExists(IAuthRepository authRepo, IUserAuth userAuth, IAuthTokens tokens = null)
        {
            if (tokens?.UserName != null)
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
            if (tokens?.Email != null)
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

        public virtual bool IsAccountLocked(IAuthRepository authRepo, IUserAuth userAuth, IAuthTokens tokens=null)
        {
            if (AccountLockedValidator != null)
                return AccountLockedValidator(authRepo, userAuth, tokens);
            
            return userAuth?.LockedDate != null;
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

            var referrerUrl = authService.Request.GetReturnUrl() ?? session.ReferrerUrl;
            if (!string.IsNullOrEmpty(referrerUrl))
                return referrerUrl;

            referrerUrl = authService.Request.GetHeader("Referer");
            if (!string.IsNullOrEmpty(referrerUrl))
                return referrerUrl;

            var requestUri = authService.Request.AbsoluteUri;
            if (requestUri.IndexOf("/auth", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                referrerUrl = this.RedirectUrl
                    ?? authService.Request.GetBaseUrl()
                    ?? requestUri.InferBaseUrl();
            }

            return referrerUrl;
        }
        
        protected virtual object ConvertToClientError(object failedResult, bool isHtml)
        {
            if (!isHtml)
            {
                if (failedResult is IHttpResult httpRes)
                {
                    if (httpRes.Headers.TryGetValue(HttpHeaders.Location, out var location))
                    {
                        var parts = location.SplitOnLast("f=");
                        if (parts.Length == 2)
                        {
                            return new HttpError(HttpStatusCode.BadRequest, parts[1], parts[1].SplitCamelCase());
                        }
                    }
                }
            }
            return failedResult;
        }

        public virtual void Register(IAppHost appHost, AuthFeature feature)
        {
            RestoreSessionFromState ??= appHost.Config.UseSameSiteCookies == true;
        }
    }
}
