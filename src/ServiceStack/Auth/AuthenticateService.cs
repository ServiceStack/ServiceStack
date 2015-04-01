using System;
using System.Collections.Generic;
using System.Configuration;
using ServiceStack.Host;
using ServiceStack.Text;
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
        public static IAuthProvider[] AuthProviders { get; private set; }

        static AuthenticateService()
        {
            CurrentSessionFactory = () => new AuthUserSession();
        }

        public static IAuthProvider GetAuthProvider(string provider)
        {
            if (AuthProviders == null || AuthProviders.Length == 0) return null;
            if (provider == LogoutAction) return AuthProviders[0];

            foreach (var authConfig in AuthProviders)
            {
                if (string.Compare(authConfig.Provider, provider,
                    StringComparison.InvariantCultureIgnoreCase) == 0)
                    return authConfig;
            }

            return null;
        }

        public static void Init(Func<IAuthSession> sessionFactory, params IAuthProvider[] authProviders)
        {
            if (authProviders.Length == 0)
                throw new ArgumentNullException("authProviders");

            DefaultOAuthProvider = authProviders[0].Provider;
            DefaultOAuthRealm = authProviders[0].AuthRealm;

            AuthProviders = authProviders;
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

            var oAuthConfig = GetAuthProvider(provider);
            if (oAuthConfig == null)
                throw HttpError.NotFound(ErrorMessages.UnknownAuthProviderFmt.Fmt(provider));

            if (LogoutAction.EqualsIgnoreCase(request.provider))
                return oAuthConfig.Logout(this, request);

            var session = this.GetSession();

            var isHtml = base.Request.ResponseContentType.MatchesContentType(MimeTypes.Html);
            try
            {
                var response = Authenticate(request, provider, session, oAuthConfig);

                // The above Authenticate call may end an existing session and create a new one so we need
                // to refresh the current session reference.
                session = this.GetSession();

                if (request.provider == null && !session.IsAuthenticated)
                    throw HttpError.Unauthorized(ErrorMessages.NotAuthenticated);

                var referrerUrl = request.Continue
                    ?? session.ReferrerUrl
                    ?? this.Request.GetHeader("Referer")
                    ?? oAuthConfig.CallbackUrl;

                var alreadyAuthenticated = response == null;
                response = response ?? new AuthenticateResponse {
                    UserId = session.UserAuthId,
                    UserName = session.UserAuthName,
                    DisplayName = session.DisplayName 
                        ?? session.UserName 
                        ?? "{0} {1}".Fmt(session.FirstName, session.LastName).Trim(),
                    SessionId = session.Id,
                    ReferrerUrl = referrerUrl,
                };

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
                var errorReferrerUrl = this.Request.GetHeader("Referer");
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

            object response = null;
            if (!oAuthConfig.IsAuthorized(session, session.GetOAuthTokens(provider), request))
            {
                response = oAuthConfig.Authenticate(this, session, request);
            }
            return response;
        }

        public object Delete(Authenticate request)
        {
            if (ValidateFn != null)
            {
                var response = ValidateFn(this, HttpMethods.Delete, request);
                if (response != null) return response;
            }

            this.RemoveSession();

            return new AuthenticateResponse();
        }
    }
}

