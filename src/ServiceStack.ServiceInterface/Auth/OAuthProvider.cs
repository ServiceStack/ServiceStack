using System;
using System.Linq;
using ServiceStack.Common;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface.Auth
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
            this.RedirectUrl = appSettings.GetString("oauth.{0}.RedirectUrl".Fmt(oAuthProvider));
            this.CallbackUrl = appSettings.GetString("oauth.{0}.CallbackUrl".Fmt(oAuthProvider));
            this.ConsumerKey = appSettings.GetString("oauth.{0}.{1}".Fmt(oAuthProvider, consumerKeyName));
            this.ConsumerSecret = appSettings.GetString("oauth.{0}.{1}".Fmt(oAuthProvider, consumerSecretName));

            this.RequestTokenUrl = appSettings.Get("oauth.{0}.RequestTokenUrl".Fmt(oAuthProvider), authRealm + "oauth/request_token");
            this.AuthorizeUrl = appSettings.Get("oauth.{0}.AuthorizeUrl".Fmt(oAuthProvider), authRealm + "oauth/authorize");
            this.AccessTokenUrl = appSettings.Get("oauth.{0}.AccessTokenUrl".Fmt(oAuthProvider), authRealm + "oauth/access_token");

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

        public override bool IsAuthorized(IAuthSession session, IOAuthTokens tokens, Authenticate request = null)
        {
            if (request != null)
            {
                if (!LoginMatchesSession(session, request.UserName)) return false;
            }

            return tokens != null && !string.IsNullOrEmpty(tokens.AccessTokenSecret);
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
                    tokens.AccessToken = OAuthUtils.AccessToken;
                    tokens.AccessTokenSecret = OAuthUtils.AccessTokenSecret;
                    session.IsAuthenticated = true;
                    OnAuthenticated(authService, session, tokens, OAuthUtils.AuthInfo);
                    authService.SaveSession(session, SessionExpiry);

                    //Haz access!
                    return authService.Redirect(session.ReferrerUrl.AddHashParam("s", "1"));
                }

                //No Joy :(
                tokens.RequestToken = null;
                tokens.RequestTokenSecret = null;
                authService.SaveSession(session, SessionExpiry);
                return authService.Redirect(session.ReferrerUrl.AddHashParam("f", "AccessTokenFailed"));
            }
            if (OAuthUtils.AcquireRequestToken())
            {
                tokens.RequestToken = OAuthUtils.RequestToken;
                tokens.RequestTokenSecret = OAuthUtils.RequestTokenSecret;
                authService.SaveSession(session, SessionExpiry);

                //Redirect to OAuth provider to approve access
                return authService.Redirect(this.AuthorizeUrl
                    .AddQueryParam("oauth_token", tokens.RequestToken)
                    .AddQueryParam("oauth_callback", session.ReferrerUrl));
            }

            return authService.Redirect(session.ReferrerUrl.AddHashParam("f", "RequestTokenFailed"));
        }

        /// <summary>
        /// Sets the CallbackUrl and session.ReferrerUrl if not set and initializes the session tokens for this AuthProvider
        /// </summary>
        /// <param name="authService"></param>
        /// <param name="session"></param>
        /// <param name="request"> </param>
        /// <returns></returns>
        protected IOAuthTokens Init(IServiceBase authService, ref IAuthSession session, Authenticate request)
        {
            if (request != null && !LoginMatchesSession(session, request.UserName))
            {
                //authService.RemoveSession();
                //session = authService.GetSession();
            }

            var requestUri = authService.RequestContext.AbsoluteUri;
            if (this.CallbackUrl.IsNullOrEmpty())
                this.CallbackUrl = requestUri;

            if (session.ReferrerUrl.IsNullOrEmpty())
                session.ReferrerUrl = (request != null ? request.Continue : null)
                    ?? authService.RequestContext.GetHeader("Referer");

            if (session.ReferrerUrl.IsNullOrEmpty() 
                || session.ReferrerUrl.IndexOf("/auth", StringComparison.OrdinalIgnoreCase) >= 0)
                session.ReferrerUrl = this.RedirectUrl 
                    ?? ServiceStackHttpHandlerFactory.GetBaseUrl()
                    ?? requestUri.Substring(0, requestUri.IndexOf("/", "https://".Length + 1, StringComparison.Ordinal));

            var tokens = session.ProviderOAuthAccess.FirstOrDefault(x => x.Provider == Provider);
            if (tokens == null)
                session.ProviderOAuthAccess.Add(tokens = new OAuthTokens { Provider = Provider });

            return tokens;
        }

        public virtual void LoadUserOAuthProvider(IAuthSession userSession, IOAuthTokens tokens) { }
    }
}