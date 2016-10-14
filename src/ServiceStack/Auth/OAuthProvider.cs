using System.Linq;
using ServiceStack.Configuration;

namespace ServiceStack.Auth
{
    public class OAuthProvider : AuthProvider
    {
        public OAuthProvider() { }

        public OAuthProvider(IAppSettings appSettings, string authRealm, string oAuthProvider)
            : this(appSettings, authRealm, oAuthProvider, "ConsumerKey", "ConsumerSecret") { }

        public OAuthProvider(IAppSettings appSettings, string authRealm, string oAuthProvider,
                             string consumerKeyName, string consumerSecretName)
        {
            this.AuthRealm = appSettings.Get("OAuthRealm", authRealm);

            this.Provider = oAuthProvider;
            this.RedirectUrl = appSettings.GetString($"oauth.{oAuthProvider}.RedirectUrl")
                ?? FallbackConfig(appSettings.GetString("oauth.RedirectUrl"));
            this.CallbackUrl = appSettings.GetString($"oauth.{oAuthProvider}.CallbackUrl")
                ?? FallbackConfig(appSettings.GetString("oauth.CallbackUrl"));
            this.ConsumerKey = appSettings.GetString($"oauth.{oAuthProvider}.{consumerKeyName}");
            this.ConsumerSecret = appSettings.GetString($"oauth.{oAuthProvider}.{consumerSecretName}");

            this.RequestTokenUrl = appSettings.Get($"oauth.{oAuthProvider}.RequestTokenUrl", authRealm + "oauth/request_token");
            this.AuthorizeUrl = appSettings.Get($"oauth.{oAuthProvider}.AuthorizeUrl", authRealm + "oauth/authorize");
            this.AccessTokenUrl = appSettings.Get($"oauth.{oAuthProvider}.AccessTokenUrl", authRealm + "oauth/access_token");
            this.SaveExtendedUserInfo = appSettings.Get($"oauth.{oAuthProvider}.SaveExtendedUserInfo", true);

            this.OAuthUtils = new OAuthAuthorizer(this);
            this.AuthHttpGateway = new AuthHttpGateway();
        }

        public IAuthHttpGateway AuthHttpGateway { get; set; }

        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }
        public string RequestTokenUrl { get; set; }
        public string AuthorizeUrl { get; set; }
        public string AccessTokenUrl { get; set; }
        public OAuthAuthorizer OAuthUtils { get; set; }

        public bool SaveExtendedUserInfo { get; set; }

        public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null)
        {
            if (request != null)
            {
                if (!LoginMatchesSession(session, request.UserName)) return false;
            }

            return session != null && session.IsAuthenticated && !string.IsNullOrEmpty(tokens?.AccessTokenSecret);
        }

        /// <summary>
        /// The entry point for all AuthProvider providers. Runs inside the AuthService so exceptions are treated normally.
        /// Overridable so you can provide your own Auth implementation.
        /// </summary>
        /// <param name="authService"></param>
        /// <param name="session"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            var tokens = Init(authService, ref session, request);

            //Default OAuth logic based on Twitter's OAuth workflow
            if (!tokens.RequestToken.IsNullOrEmpty() && !request.oauth_token.IsNullOrEmpty())
            {
                OAuthUtils.RequestToken = tokens.RequestToken;
                OAuthUtils.RequestTokenSecret = tokens.RequestTokenSecret;
                OAuthUtils.AuthorizationToken = request.oauth_token;
                OAuthUtils.AuthorizationVerifier = request.oauth_verifier;

                if (OAuthUtils.AcquireAccessToken())
                {
                    session.IsAuthenticated = true;
                    tokens.AccessToken = OAuthUtils.AccessToken;
                    tokens.AccessTokenSecret = OAuthUtils.AccessTokenSecret;

                    return OnAuthenticated(authService, session, tokens, OAuthUtils.AuthInfo)
                        ?? authService.Redirect(SuccessRedirectUrlFilter(this, session.ReferrerUrl.SetParam("s", "1"))); //Haz Access
                }

                //No Joy :(
                tokens.RequestToken = null;
                tokens.RequestTokenSecret = null;
                this.SaveSession(authService, session, SessionExpiry);
                return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", "AccessTokenFailed")));
            }
            if (OAuthUtils.AcquireRequestToken())
            {
                tokens.RequestToken = OAuthUtils.RequestToken;
                tokens.RequestTokenSecret = OAuthUtils.RequestTokenSecret;
                this.SaveSession(authService, session, SessionExpiry);

                //Redirect to OAuth provider to approve access
                return authService.Redirect(AccessTokenUrlFilter(this, this.AuthorizeUrl
                    .AddQueryParam("oauth_token", tokens.RequestToken)
                    .AddQueryParam("oauth_callback", session.ReferrerUrl)));
            }

            return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", "RequestTokenFailed")));
        }

        /// <summary>
        /// Sets the CallbackUrl and session.ReferrerUrl if not set and initializes the session tokens for this AuthProvider
        /// </summary>
        /// <param name="authService"></param>
        /// <param name="session"></param>
        /// <param name="request"> </param>
        /// <returns></returns>
        protected IAuthTokens Init(IServiceBase authService, ref IAuthSession session, Authenticate request)
        {
            if (this.CallbackUrl.IsNullOrEmpty())
                this.CallbackUrl = authService.Request.AbsoluteUri;

            session.ReferrerUrl = GetReferrerUrl(authService, session, request);

            var tokens = session.GetAuthTokens(Provider);
            if (tokens == null)
                session.AddAuthToken(tokens = new AuthTokens { Provider = Provider });

            return tokens;
        }

        public virtual void LoadUserOAuthProvider(IAuthSession userSession, IAuthTokens tokens) { }
    }
}