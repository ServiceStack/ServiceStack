using System;
using System.Configuration;
using System.Linq;
using System.Net;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    /// <summary>
    /// Inject logic into existing services by introspecting the request and injecting your own
    /// validation logic. Exceptions thrown will have the same behaviour as if the service threw it.
    /// 
    /// If a non-null object is returned the request will short-circuit and return that response.
    /// </summary>
    /// <param name="service">The instance of the service</param>
    /// <param name="httpMethod">GET,POST,PUT,DELETE</param>
    /// <param name="requestDto"></param>
    /// <returns>Response DTO; non-null will short-circuit execution and return that response</returns>
    public delegate object ValidateFn(IServiceBase service, string httpMethod, object requestDto);

    [DefaultRequest(typeof(Authenticate))]
    public class AuthenticateService : Service
    {
        public const string BasicProvider = "basic";
        public const string ApiKeyProvider = "apikey";
        public const string JwtProvider = "jwt";
        public const string CredentialsProvider = "credentials";
        public const string WindowsAuthProvider = "windowsauth";
        public const string CredentialsAliasProvider = "login";
        public const string LogoutAction = "logout";
        public const string DigestProvider = "digest";

        public static Func<IAuthSession> CurrentSessionFactory { get; set; }
        public static ValidateFn ValidateFn { get; set; }

        public static string DefaultOAuthProvider { get; private set; }
        public static string DefaultOAuthRealm { get; private set; }
        public static string HtmlRedirect { get; internal set; }
        public static Func<IServiceBase, Authenticate, AuthenticateResponse, object> AuthResponseDecorator { get; internal set; }
        internal static IAuthProvider[] AuthProviders = TypeConstants<IAuthProvider>.EmptyArray;
        internal static IAuthWithRequest[] AuthWithRequestProviders = TypeConstants<IAuthWithRequest>.EmptyArray;
        internal static IAuthResponseFilter[] AuthResponseFilters = TypeConstants<IAuthResponseFilter>.EmptyArray;

        static AuthenticateService()
        {
            CurrentSessionFactory = () => new AuthUserSession();
        }

        public static IAuthProvider GetAuthProvider(string provider)
        {
            if (AuthProviders.Length == 0)
                return null;
            if (provider == LogoutAction)
                return AuthProviders[0];

            foreach (var authConfig in AuthProviders)
            {
                if (string.Compare(authConfig.Provider, provider,
                    StringComparison.OrdinalIgnoreCase) == 0)
                    return authConfig;
            }

            return null;
        }

        public static IAuthProvider[] GetAuthProviders()
        {
            return AuthProviders ?? TypeConstants<IAuthProvider>.EmptyArray;
        }

        public static void Init(Func<IAuthSession> sessionFactory, params IAuthProvider[] authProviders)
        {
            if (authProviders.Length == 0)
                throw new ArgumentNullException(nameof(authProviders));

            DefaultOAuthProvider = authProviders[0].Provider;
            DefaultOAuthRealm = authProviders[0].AuthRealm;

            AuthProviders = authProviders;
            AuthWithRequestProviders = authProviders.OfType<IAuthWithRequest>().ToArray();
            AuthResponseFilters = authProviders.OfType<IAuthResponseFilter>().ToArray();

            if (sessionFactory != null)
                CurrentSessionFactory = sessionFactory;
        }

        private void AssertAuthProviders()
        {
            if (AuthProviders == null || AuthProviders.Length == 0)
                throw new ConfigurationErrorsException("No OAuth providers have been registered in your AppHost.");
        }

        public void Options(Authenticate request) { }

        public object Get(Authenticate request)
        {
            return Post(request);
        }

        public object Post(Authenticate request)
        {
            AssertAuthProviders();

            if (ValidateFn != null)
            {
                var validationResponse = ValidateFn(this, Request.Verb, request);
                if (validationResponse != null) return validationResponse;
            }

            if (request.RememberMe.HasValue)
            {
                var opt = request.RememberMe.GetValueOrDefault(false)
                    ? SessionOptions.Permanent
                    : SessionOptions.Temporary;

                base.Request.AddSessionOptions(opt);
            }

            var provider = request.provider ?? AuthProviders[0].Provider;
            if (provider == CredentialsAliasProvider)
                provider = CredentialsProvider;

            var authProvider = GetAuthProvider(provider);
            if (authProvider == null)
                throw HttpError.NotFound(ErrorMessages.UnknownAuthProviderFmt.Fmt(provider));

            if (LogoutAction.EqualsIgnoreCase(request.provider))
                return authProvider.Logout(this, request);

            var authWithRequest = authProvider as IAuthWithRequest;
            if (authWithRequest != null && !base.Request.IsInProcessRequest())
            {
                //IAuthWithRequest normally doesn't call Authenticate directly, but they can to return Auth Info
                //But as AuthenticateService doesn't have [Authenticate] we need to call it manually
                new AuthenticateAttribute().Execute(base.Request, base.Response, request);
                if (base.Response.IsClosed)
                    return null;
            }

            var session = this.GetSession();

            var isHtml = base.Request.ResponseContentType.MatchesContentType(MimeTypes.Html);
            try
            {
                var response = Authenticate(request, provider, session, authProvider);

                // The above Authenticate call may end an existing session and create a new one so we need
                // to refresh the current session reference.
                session = this.GetSession();

                if (request.provider == null && !session.IsAuthenticated)
                    throw HttpError.Unauthorized(ErrorMessages.NotAuthenticated);

                var referrerUrl = request.Continue
                    ?? session.ReferrerUrl
                    ?? this.Request.GetHeader(HttpHeaders.Referer)
                    ?? authProvider.CallbackUrl;

                var alreadyAuthenticated = response == null;
                response = response ?? new AuthenticateResponse {
                    UserId = session.UserAuthId,
                    UserName = session.UserAuthName,
                    DisplayName = session.DisplayName 
                        ?? session.UserName 
                        ?? $"{session.FirstName} {session.LastName}".Trim(),
                    SessionId = session.Id,
                    ReferrerUrl = referrerUrl,
                };

                var authResponse = response as AuthenticateResponse;
                if (authResponse != null)
                {
                    foreach (var responseFilter in AuthResponseFilters)
                    {
                        authResponse = responseFilter.Execute(this, authProvider, session, authResponse) ?? authResponse;
                    }

                    if (AuthResponseDecorator != null)
                    {
                        return AuthResponseDecorator(this, request, authResponse);
                    }
                }

                if (isHtml && request.provider != null)
                {
                    if (alreadyAuthenticated)
                        return this.Redirect(referrerUrl.SetParam("s", "0"));

                    if (!(response is IHttpResult) && !string.IsNullOrEmpty(referrerUrl))
                    {
                        return new HttpResult(response) {
                            Location = referrerUrl
                        };
                    }
                }

                return response;
            }
            catch (HttpError ex)
            {
                var errorReferrerUrl = this.Request.GetHeader(HttpHeaders.Referer);
                if (isHtml && errorReferrerUrl != null)
                {
                    errorReferrerUrl = errorReferrerUrl.SetParam("f", ex.Message.Localize(Request));
                    return HttpResult.Redirect(errorReferrerUrl);
                }

                throw;
            }
        }

        /// <summary>
        /// Public API entry point to authenticate via code
        /// </summary>
        /// <param name="request"></param>
        /// <returns>null; if already autenticated otherwise a populated instance of AuthResponse</returns>
        public AuthenticateResponse Authenticate(Authenticate request)
        {
            //Remove HTML Content-Type to avoid auth providers issuing browser re-directs
            var hold = this.Request.ResponseContentType;
            try
            {
                this.Request.ResponseContentType = MimeTypes.PlainText;

                if (request.RememberMe.HasValue)
                {
                    var opt = request.RememberMe.GetValueOrDefault(false)
                        ? SessionOptions.Permanent
                        : SessionOptions.Temporary;

                    base.Request.AddSessionOptions(opt);
                }

                var provider = request.provider ?? AuthProviders[0].Provider;
                var oAuthConfig = GetAuthProvider(provider);
                if (oAuthConfig == null)
                    throw HttpError.NotFound(ErrorMessages.UnknownAuthProviderFmt.Fmt(provider));

                if (request.provider == LogoutAction)
                    return oAuthConfig.Logout(this, request) as AuthenticateResponse;

                var result = Authenticate(request, provider, this.GetSession(), oAuthConfig);
                var httpError = result as HttpError;
                if (httpError != null)
                    throw httpError;

                return result as AuthenticateResponse;
            }
            finally
            {
                this.Request.ResponseContentType = hold;
            }
        }

        /// <summary>
        /// The specified <paramref name="session"/> may change as a side-effect of this method. If
        /// subsequent code relies on current <see cref="IAuthSession"/> data be sure to reload
        /// the session istance via <see cref="ServiceExtensions.GetSession(IServiceBase,bool)"/>.
        /// </summary>
        private object Authenticate(Authenticate request, string provider, IAuthSession session, IAuthProvider oAuthConfig)
        {
            if (request.provider == null && request.UserName == null)
                return null; //Just return sessionInfo if no provider or username is given

            var authFeature = HostContext.GetPlugin<AuthFeature>();
            var generateNewCookies = authFeature == null || authFeature.GenerateNewSessionCookiesOnAuthentication;

            object response = null;
            if (!oAuthConfig.IsAuthorized(session, session.GetAuthTokens(provider), request))
            {
                if (generateNewCookies)
                    this.Request.GenerateNewSessionCookies(session);

                response = oAuthConfig.Authenticate(this, session, request);
            }
            else
            {
                if (generateNewCookies)
                {
                    this.Request.GenerateNewSessionCookies(session);
                    oAuthConfig.SaveSession(this, session, (oAuthConfig as AuthProvider)?.SessionExpiry);
                }
            }
            return response;
        }

        public object Delete(Authenticate request)
        {
            var response = ValidateFn?.Invoke(this, HttpMethods.Delete, request);
            if (response != null)
                return response;

            this.RemoveSession();

            return new AuthenticateResponse();
        }
    }
}

