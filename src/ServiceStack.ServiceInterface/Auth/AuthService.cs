using System;
using System.Configuration;
using System.Runtime.Serialization;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface.Auth
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

    [DataContract]
    public class Auth : IReturn<AuthResponse>
    {
        [DataMember(Order=1)] public string provider { get; set; }
        [DataMember(Order=2)] public string State { get; set; }
        [DataMember(Order=3)] public string oauth_token { get; set; }
        [DataMember(Order=4)] public string oauth_verifier { get; set; }
        [DataMember(Order=5)] public string UserName { get; set; }
        [DataMember(Order=6)] public string Password { get; set; }
        [DataMember(Order=7)] public bool? RememberMe { get; set; }
        [DataMember(Order=8)] public string Continue { get; set; }
        // Thise are used for digest auth
        [DataMember(Order=9)] public string nonce { get; set; }
        [DataMember(Order=10)] public string uri { get; set; }
        [DataMember(Order=11)] public string response { get; set; }
        [DataMember(Order=12)] public string qop { get; set; }
        [DataMember(Order=13)] public string nc { get; set; }
        [DataMember(Order=14)] public string cnonce { get; set; }
    }

    [DataContract]
    public class AuthResponse
    {
        public AuthResponse()
        {
            this.ResponseStatus = new ResponseStatus();
        }

        [DataMember(Order=1)] public string SessionId { get; set; }
        [DataMember(Order=2)] public string UserName { get; set; }
        [DataMember(Order=3)] public string ReferrerUrl { get; set; }
        [DataMember(Order=4)] public ResponseStatus ResponseStatus { get; set; }
    }

    [DefaultRequest(typeof(Auth))]
    public class AuthService : Service
    {
        public const string BasicProvider = "basic";
        public const string CredentialsProvider = "credentials";
        public const string LogoutAction = "logout";
        public const string DigestProvider = "digest";

        public static Func<IAuthSession> CurrentSessionFactory { get; set; }
        public static ValidateFn ValidateFn { get; set; }

        public static string DefaultOAuthProvider { get; private set; }
        public static string DefaultOAuthRealm { get; private set; }
        public static string HtmlRedirect { get; internal set; }
        public static IAuthProvider[] AuthProviders { get; private set; }


        static AuthService()
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
            if (EndpointHost.Config == null)
                EndpointHost.Config = new EndpointHostConfig("AuthService", new ServiceManager(typeof(AuthService).Assembly));

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
                throw new ConfigurationException("No OAuth providers have been registered in your AppHost.");
        }

        public void Options(Auth request) {}

        public object Get(Auth request)
        {
            return Post(request);
        }

        public object Post(Auth request)
        {
            AssertAuthProviders();

            if (ValidateFn != null)
            {
                var validationResponse = ValidateFn(this, HttpMethods.Get, request);
                if (validationResponse != null) return validationResponse;
            }

            if (request.RememberMe.HasValue)
            {
                var opt = request.RememberMe.GetValueOrDefault(false)
                    ? SessionOptions.Permanent
                    : SessionOptions.Temporary;

                base.RequestContext.Get<IHttpResponse>()
                    .AddSessionOptions(base.RequestContext.Get<IHttpRequest>(), opt);
            }

            var provider = request.provider ?? AuthProviders[0].Provider;
            var oAuthConfig = GetAuthProvider(provider);
            if (oAuthConfig == null)
                throw HttpError.NotFound("No configuration was added for OAuth provider '{0}'".Fmt(provider));

            if (request.provider == LogoutAction)
                return oAuthConfig.Logout(this, request);

            var session = this.GetSession();

            var isHtml = base.RequestContext.ResponseContentType.MatchesContentType(ContentType.Html);
            try
            {
                var response = Authenticate(request, provider, session, oAuthConfig);

                // The above Authenticate call may end an existing session and create a new one so we need
                // to refresh the current session reference.
                session = this.GetSession();

                var referrerUrl = request.Continue
                    ?? session.ReferrerUrl
                    ?? this.RequestContext.GetHeader("Referer")
                    ?? oAuthConfig.CallbackUrl;

                var alreadyAuthenticated = response == null;
                response = response ?? new AuthResponse {
                    UserName = session.UserAuthName,
                    SessionId = session.Id,
                    ReferrerUrl = referrerUrl,
                };

                if (isHtml)
                {
                    if (alreadyAuthenticated)
                        return this.Redirect(referrerUrl.AddHashParam("s", "0"));

                    if (!(response is IHttpResult) && !String.IsNullOrEmpty(referrerUrl))
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
                var errorReferrerUrl = this.RequestContext.GetHeader("Referer");
                if (isHtml && errorReferrerUrl != null)
                {
                    errorReferrerUrl = errorReferrerUrl.SetQueryParam("error", ex.Message);
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
        public AuthResponse Authenticate(Auth request)
        {
            //Remove HTML Content-Type to avoid auth providers issuing browser re-directs
            ((HttpRequestContext)this.RequestContext).ResponseContentType = ContentType.PlainText;

            var provider = request.provider ?? AuthProviders[0].Provider;
            var oAuthConfig = GetAuthProvider(provider);
            if (oAuthConfig == null)
                throw HttpError.NotFound("No configuration was added for OAuth provider '{0}'".Fmt(provider));

            if (request.provider == LogoutAction)
                return oAuthConfig.Logout(this, request) as AuthResponse;

            var result = Authenticate(request, provider, this.GetSession(), oAuthConfig);
            var httpError = result as HttpError;
            if (httpError != null)
                throw httpError;

            return result as AuthResponse;
        }

        /// <summary>
        /// The specified <paramref name="session"/> may change as a side-effect of this method. If
        /// subsequent code relies on current <see cref="IAuthSession"/> data be sure to reload
        /// the session istance via <see cref="ServiceExtensions.GetSession(ServiceStack.ServiceInterface.IServiceBase,bool)"/>.
        /// </summary>
        private object Authenticate(Auth request, string provider, IAuthSession session, IAuthProvider oAuthConfig)
        {
            object response = null;
            if (!oAuthConfig.IsAuthorized(session, session.GetOAuthTokens(provider), request))
            {
                response = oAuthConfig.Authenticate(this, session, request);
            }
            return response;
        }

        public object Delete(Auth request)
        {
            if (ValidateFn != null)
            {
                var response = ValidateFn(this, HttpMethods.Delete, request);
                if (response != null) return response;
            }

            this.RemoveSession();

            return new AuthResponse();
        }
    }

}

