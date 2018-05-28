using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public abstract class AuthProvider : IAuthProvider
    {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(AuthProvider));

        public TimeSpan? SessionExpiry { get; set; }
        public string AuthRealm { get; set; }
        public string Provider { get; set; }
        public string CallbackUrl { get; set; }
        public string RedirectUrl { get; set; }

        public bool PersistSession { get; set; }
        public bool SaveExtendedUserInfo { get; set; }

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
            PersistSession = !GetType().HasInterface(typeof(IAuthWithRequest));
        }

        protected AuthProvider(IAppSettings appSettings, string authRealm, string oAuthProvider)
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
            var referrerUrl = request?.Continue
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
                return service.Redirect(LogoutUrlFilter(this, referrerUrl.SetParam("s", "-1")));

            return new AuthenticateResponse();
        }

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

                authInfo.ForEach((x, y) => tokens.Items[x] = y);
            }

            if (session is IAuthSessionExtended authSession)
            {
                var failed = authSession.Validate(authService, session, tokens, authInfo);
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

                    foreach (var oAuthToken in session.GetAuthTokens())
                    {
                        var authProvider = AuthenticateService.GetAuthProvider(oAuthToken.Provider);
                        var userAuthProvider = authProvider as OAuthProvider;
                        userAuthProvider?.LoadUserOAuthProvider(session, oAuthToken);
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
                this.SaveSession(authService, session, SessionExpiry);
                authService.Request.Items[Keywords.DidAuthenticate] = true;
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

        public abstract bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null);

        public abstract object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request);

        public virtual Task OnFailedAuthentication(IAuthSession session, IRequest httpReq, IResponse httpRes)
        {
            httpRes.StatusCode = (int)HttpStatusCode.Unauthorized;
            httpRes.AddHeader(HttpHeaders.WwwAuthenticate, "{0} realm=\"{1}\"".Fmt(this.Provider, this.AuthRealm));
            return HostContext.AppHost.HandleShortCircuitedErrors(httpReq, httpRes, httpReq.Dto);
        }

        public static Task HandleFailedAuth(IAuthProvider authProvider,
            IAuthSession session, IRequest httpReq, IResponse httpRes)
        {
            if (authProvider is AuthProvider baseAuthProvider)
                return baseAuthProvider.OnFailedAuthentication(session, httpReq, httpRes);

            httpRes.StatusCode = (int)HttpStatusCode.Unauthorized;
            httpRes.AddHeader(HttpHeaders.WwwAuthenticate, $"{authProvider.Provider} realm=\"{authProvider.AuthRealm}\"");
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

            var referrerUrl = session.ReferrerUrl;
            if (referrerUrl.IsNullOrEmpty())
                referrerUrl = request?.Continue
                    ?? authService.Request.GetHeader("Referer");

            var requestUri = authService.Request.AbsoluteUri;
            if (referrerUrl.IsNullOrEmpty()
                || referrerUrl.IndexOf("/auth", StringComparison.OrdinalIgnoreCase) >= 0)
                referrerUrl = this.RedirectUrl
                    ?? authService.Request.GetBaseUrl()
                    ?? requestUri.InferBaseUrl();

            return referrerUrl;
        }

        public void PopulateSession(IUserAuthRepository authRepo, IUserAuth userAuth, IAuthSession session)
        {
            if (authRepo == null)
                return;

            var holdSessionId = session.Id;
            session.PopulateWith(userAuth); //overwrites session.Id
            session.Id = holdSessionId;
            session.IsAuthenticated = true;
            session.UserAuthId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
            session.ProviderOAuthAccess = authRepo.GetUserAuthDetails(session.UserAuthId)
                .ConvertAll(x => (IAuthTokens)x);
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
    }

    public class AuthContext
    {
        public IRequest Request { get; set; }
        public IServiceBase Service { get; set; }
        public AuthProvider AuthProvider { get; set; }
        public IAuthSession Session { get; set; }
        public IAuthTokens AuthTokens { get; set; }
        public Dictionary<string, string> AuthInfo { get; set; }
        public IAuthRepository AuthRepository { get; set; }
    }

    public static class AuthExtensions
    {
        private static ILog Log = LogManager.GetLogger(typeof(AuthExtensions));

        public static bool IsAuthorizedSafe(this IAuthProvider authProvider, IAuthSession session, IAuthTokens tokens)
        {
            return authProvider != null && authProvider.IsAuthorized(session, tokens);
        }

        public static string SanitizeOAuthUrl(this string url)
        {
            return (url ?? "").Replace("\\/", "/");
        }

        internal static bool PopulateFromRequestIfHasSessionId(this IRequest req, object requestDto)
        {
            var hasSession = requestDto as IHasSessionId;
            if (hasSession?.SessionId != null)
            {
                req.SetSessionId(hasSession.SessionId);
                return true;
            }
            return false;
        }

        internal static string NotLogoutUrl(this string url)
        {
            return url == null || url.EndsWith("/auth/logout")
                ? null
                : url;
        }

        public static void SaveSession(this IAuthProvider provider, IServiceBase authService, IAuthSession session, TimeSpan? sessionExpiry = null)
        {
            var persistSession = !(provider is AuthProvider authProvider) || authProvider.PersistSession;
            if (persistSession)
            {
                authService.SaveSession(session, sessionExpiry);
            }
            else
            {
                authService.Request.Items[Keywords.Session] = session;
            }
        }

        public static void PopulatePasswordHashes(this IUserAuth newUser, string password, IUserAuth existingUser = null)
        {
            if (newUser == null)
                throw new ArgumentNullException(nameof(newUser));
            
            var hash = existingUser?.PasswordHash;
            var salt = existingUser?.Salt;

            if (password != null)
            {
                var passwordHasher = !HostContext.Config.UseSaltedHash
                    ? HostContext.TryResolve<IPasswordHasher>()
                    : null;

                if (passwordHasher != null)
                {
                    salt = null; // IPasswordHasher stores its Salt in PasswordHash
                    hash = passwordHasher.HashPassword(password);
                }
                else
                {
                    var hashProvider = HostContext.Resolve<IHashProvider>();
                    hashProvider.GetHashAndSaltString(password, out hash, out salt);
                }
            }

            newUser.PasswordHash = hash;
            newUser.Salt = salt;
            
            newUser.PopulateDigestAuthHash(password, existingUser);
        }

        private static void PopulateDigestAuthHash(this IUserAuth newUser, string password, IUserAuth existingUser = null)
        {
            var createDigestAuthHashes = HostContext.GetPlugin<AuthFeature>()?.CreateDigestAuthHashes;
            if (createDigestAuthHashes == true)
            {
                if (existingUser == null)
                {
                    var digestHelper = new DigestAuthFunctions();
                    newUser.DigestHa1Hash = digestHelper.CreateHa1(newUser.UserName, DigestAuthProvider.Realm, password);
                }
                else
                {
                    newUser.DigestHa1Hash = existingUser.DigestHa1Hash;

                    // If either one changes the digest hash has to be recalculated
                    if (password != null || existingUser.UserName != newUser.UserName)
                        newUser.DigestHa1Hash = new DigestAuthFunctions().CreateHa1(newUser.UserName, DigestAuthProvider.Realm, password);
                }
            }
            else if (createDigestAuthHashes == false)
            {
                newUser.DigestHa1Hash = null;
            }
        }

        public static bool VerifyPassword(this IUserAuth userAuth, string providedPassword, out bool needsRehash)
        {
            needsRehash = false;
            if (userAuth == null)
                throw new ArgumentNullException(nameof(userAuth));

            if (userAuth.PasswordHash == null)
                return false;

            var passwordHasher = HostContext.TryResolve<IPasswordHasher>();

            var usedOriginalSaltedHash = userAuth.Salt != null;
            if (usedOriginalSaltedHash)
            {
                var oldSaltedHashProvider = HostContext.Resolve<IHashProvider>();
                if (oldSaltedHashProvider.VerifyHashString(providedPassword, userAuth.PasswordHash, userAuth.Salt))
                {
                    needsRehash = !HostContext.Config.UseSaltedHash;
                    return true;
                }

                return false;
            }

            if (passwordHasher == null)
            {
                if (Log.IsDebugEnabled)
                    Log.Debug("Found newer PasswordHash without Salt but no registered IPasswordHasher to verify it");

                return false;
            }

            if (passwordHasher.VerifyPassword(userAuth.PasswordHash, providedPassword, out needsRehash))
            {
                needsRehash = HostContext.Config.UseSaltedHash;
                return true;
            }

            if (HostContext.Config.FallbackPasswordHashers.Count > 0)
            {
                var decodedHashedPassword = Convert.FromBase64String(userAuth.PasswordHash);
                if (decodedHashedPassword.Length == 0)
                {
                    if (Log.IsDebugEnabled)
                        Log.Debug("userAuth.PasswordHash is empty");

                    return false;
                }

                var formatMarker = decodedHashedPassword[0];

                foreach (var oldPasswordHasher in HostContext.Config.FallbackPasswordHashers)
                {
                    if (oldPasswordHasher.Version == formatMarker)
                    {
                        if (oldPasswordHasher.VerifyPassword(userAuth.PasswordHash, providedPassword, out _))
                        {
                            needsRehash = true;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool VerifyDigestAuth(this IUserAuth userAuth, Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence)
        {
            if (userAuth == null)
                throw new ArgumentNullException(nameof(userAuth));

            return new DigestAuthFunctions().ValidateResponse(digestHeaders, privateKey, nonceTimeOut, userAuth.DigestHa1Hash, sequence);
        }
    }

}

