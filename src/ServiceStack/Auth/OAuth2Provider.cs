using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    public abstract class OAuth2Provider : OAuthProvider
    {
        public OAuth2Provider(IAppSettings appSettings, string authRealm, string oAuthProvider) 
            : base(appSettings, authRealm, oAuthProvider) {}
        public OAuth2Provider(IAppSettings appSettings, string authRealm, string oAuthProvider, string consumerKeyName, string consumerSecretName) 
            : base(appSettings, authRealm, oAuthProvider, consumerKeyName, consumerSecretName) {}

        protected string[] Scopes { get; set; }
        
        public Func<string, bool> VerifyAccessToken { get; set; }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            var tokens = Init(authService, ref session, request);

            //Transferring AccessToken/Secret from Mobile/Desktop App to Server
            if (request?.AccessToken != null && VerifyAccessToken != null)
            {
                if (!VerifyAccessToken(request.AccessToken))
                    return HttpError.Unauthorized("AccessToken is not for client_id: " + ConsumerKey);

                var isHtml = authService.Request.IsHtml();
                var failedResult = AuthenticateWithAccessToken(authService, session, tokens, request.AccessToken);
                if (failedResult != null)
                    return ConvertToClientError(failedResult, isHtml);

                return isHtml
                    ? authService.Redirect(SuccessRedirectUrlFilter(this, session.ReferrerUrl.SetParam("s", "1")))
                    : null; //return default AuthenticateResponse
            }

            var httpRequest = authService.Request;
            var error = httpRequest.QueryString["error_reason"]
                        ?? httpRequest.QueryString["error"]
                        ?? httpRequest.QueryString["error_code"]
                        ?? httpRequest.QueryString["error_description"];

            var hasError = !error.IsNullOrEmpty();
            if (hasError)
            {
                Log.Error($"OAuth2 Error callback. {httpRequest.QueryString}");
                return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", error)));
            }             
        
            var code = httpRequest.QueryString[Keywords.Code];
            var isPreAuthCallback = !code.IsNullOrEmpty();
            if (!isPreAuthCallback)
            {
                var oauthstate = SessionExtensions.CreateRandomSessionId();                
                var preAuthUrl = AuthorizeUrl
                    .AddQueryParam("response_type", "code")
                    .AddQueryParam("client_id", ConsumerKey)
                    .AddQueryParam("redirect_uri", this.CallbackUrl)
                    .AddQueryParam("scope", string.Join(" ", Scopes))
                    .AddQueryParam(Keywords.State, oauthstate);

                if (session is AuthUserSession authSession)
                    (authSession.Meta ?? (authSession.Meta = new Dictionary<string, string>()))["oauthstate"] = oauthstate;

                this.SaveSession(authService, session, SessionExpiry);
                return authService.Redirect(PreAuthUrlFilter(this, preAuthUrl));
            }

            try
            {
                var state = httpRequest.QueryString[Keywords.State];
                if (state != null && session is AuthUserSession authSession)
                {
                    if (authSession.Meta == null)
                        authSession.Meta = new Dictionary<string, string>();
                    
                    if (authSession.Meta.TryGetValue("oauthstate", out var oauthState) && state != oauthState)
                        return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", "InvalidState")));

                    authSession.Meta.Remove("oauthstate");
                }
                
                var contents = GetAccessTokenJson(code);
                var authInfo = JsonObject.Parse(contents);

                var accessToken = authInfo["access_token"];

                return AuthenticateWithAccessToken(authService, session, tokens, accessToken)
                       ?? authService.Redirect(SuccessRedirectUrlFilter(this, session.ReferrerUrl.SetParam("s", "1"))); //Haz Access!
            }
            catch (WebException we)
            {
                string errorBody = we.GetResponseBody();
                var statusCode = ((HttpWebResponse)we.Response).StatusCode;
                if (statusCode == HttpStatusCode.BadRequest)
                {
                    return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", "AccessTokenFailed")));
                }
            }

            //Shouldn't get here
            return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", "Unknown")));
        }

        protected virtual string GetAccessTokenJson(string code)
        {
            var accessTokenUrl = $"{AccessTokenUrl}?code={code}&client_id={ConsumerKey}&client_secret={ConsumerSecret}&redirect_uri={this.CallbackUrl.UrlEncode()}&grant_type=authorization_code";
            var contents = AccessTokenUrlFilter(this, accessTokenUrl).PostToUrl("");
            return contents;
        }

        protected virtual object AuthenticateWithAccessToken(IServiceBase authService, IAuthSession session, IAuthTokens tokens, string accessToken)
        {
            tokens.AccessToken = accessToken;

            var authInfo = this.CreateAuthInfo(accessToken);

            session.IsAuthenticated = true;

            return OnAuthenticated(authService, session, tokens, authInfo);
        }

        protected abstract Dictionary<string, string> CreateAuthInfo(string accessToken);

        protected override void LoadUserAuthInfo(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            try
            {
                tokens.UserId = authInfo.Get("user_id");
                tokens.UserName = authInfo.Get("email") ?? authInfo.Get("username") ?? tokens.UserId;
                tokens.DisplayName = authInfo.Get("name");
                tokens.FirstName = authInfo.Get("first_name");
                tokens.LastName = authInfo.Get("last_name");
                tokens.Email = authInfo.Get("email");
                userSession.UserAuthName = tokens.Email;

                if (authInfo.TryGetValue(AuthMetadataProvider.ProfileUrlKey, out var profileUrl))
                {
                    if (string.IsNullOrEmpty(userSession.ProfileUrl))
                        userSession.ProfileUrl = profileUrl.SanitizeOAuthUrl();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Could not retrieve '{Provider}' profile user info for '{tokens.DisplayName}'", ex);
            }

            LoadUserOAuthProvider(userSession, tokens);
        }
        
        public override void LoadUserOAuthProvider(IAuthSession authSession, IAuthTokens tokens)
        {
            if (!(authSession is AuthUserSession userSession))
                return;

            userSession.UserName = tokens.UserName ?? userSession.UserName;
            userSession.DisplayName = tokens.DisplayName ?? userSession.DisplayName;
            userSession.FirstName = tokens.FirstName ?? userSession.FirstName;
            userSession.LastName = tokens.LastName ?? userSession.LastName;
            userSession.Email = userSession.PrimaryEmail = tokens.Email ?? userSession.PrimaryEmail ?? userSession.Email;
        }

    }
}