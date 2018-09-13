using System;
using System.Collections.Generic;
using System.Net;
using System.Web;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    /// <summary>
    ///Create an OAuth2 App at: https://code.google.com/apis/console/
    ///The Apps Callback URL should match the CallbackUrl here.

    ///Google OAuth2 info: https://developers.google.com/accounts/docs/OAuth2Login
    ///Google OAuth2 Scopes from: https://www.googleapis.com/discovery/v1/apis/oauth2/v2/rest?fields=auth(oauth2(scopes))
    ///https://www.googleapis.com/auth/plus.login: Know your name, basic info, and list of people you're connected to on Google+
    ///https://www.googleapis.com/auth/plus.me Know who you are on Google+
    ///https://www.googleapis.com/auth/userinfo.email View your email address
    ///https://www.googleapis.com/auth/userinfo.profile View basic information about your account
    /// </summary>
    public class GoogleAuthProvider : OAuthProvider
    {
        public const string Name = "google";
        public static string Realm = "https://oauth2.googleapis.com/token";

        public string UserProfileUrl { get; set; }
        protected string[] Scopes { get; set; }

        public GoogleAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name)
        {
            this.AuthorizeUrl = appSettings.Get($"oauth.{Name}.AuthorizeUrl", "https://accounts.google.com/o/oauth2/v2/auth");
            this.AccessTokenUrl = appSettings.Get($"oauth.{Name}.AccessTokenUrl", "https://oauth2.googleapis.com/token");
            this.UserProfileUrl = appSettings.Get($"oauth.{Name}.UserProfileUrl", "https://www.googleapis.com/oauth2/v2/userinfo");

            if (this.Scopes == null || this.Scopes.Length == 0)
            {
                this.Scopes = new[] {
                    "https://www.googleapis.com/auth/userinfo.profile",
                    "https://www.googleapis.com/auth/userinfo.email"
                };
            }
        }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            var tokens = Init(authService, ref session, request);

            //Transfering AccessToken/Secret from Mobile/Desktop App to Server
            if (request?.AccessToken != null)
            {
                if (!AuthHttpGateway.VerifyGoogleAccessToken(ConsumerKey, request.AccessToken))
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
                Log.Error($"Google error callback. {httpRequest.QueryString}");
                return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", error)));
            }             
        
            var code = httpRequest.QueryString[Keywords.Code];
            var isPreAuthCallback = !code.IsNullOrEmpty();
            if (!isPreAuthCallback)
            {
                var preAuthUrl = AuthorizeUrl
                    .AddQueryParam("response_type", "code")
                    .AddQueryParam("client_id", ConsumerKey)
                    .AddQueryParam("redirect_uri", this.CallbackUrl)
                    .AddQueryParam("scope", string.Join(" ", Scopes));

                this.SaveSession(authService, session, SessionExpiry);
                return authService.Redirect(PreAuthUrlFilter(this, preAuthUrl));
            }

            try
            {
                var accessTokenUrl = $"{AccessTokenUrl}?code={code}&client_id={ConsumerKey}&client_secret={ConsumerSecret}&redirect_uri={this.CallbackUrl.UrlEncode()}&grant_type=authorization_code";
                var contents = AccessTokenUrlFilter(this, accessTokenUrl).PostToUrl("");
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

        protected virtual object AuthenticateWithAccessToken(IServiceBase authService, IAuthSession session, IAuthTokens tokens, string accessToken)
        {
            tokens.AccessTokenSecret = accessToken;

            var json = AuthHttpGateway.DownloadGoogleUserInfo(accessToken);
            var authInfo = JsonObject.Parse(json);

            session.IsAuthenticated = true;

            return OnAuthenticated(authService, session, tokens, authInfo);
        }

        protected override void LoadUserAuthInfo(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            try
            {
                tokens.UserId = authInfo.Get("id");
                tokens.UserName = authInfo.Get("id") ?? authInfo.Get("username");
                tokens.DisplayName = authInfo.Get("name");
                tokens.FirstName = authInfo.Get("first_name");
                tokens.LastName = authInfo.Get("last_name");
                tokens.Email = authInfo.Get("email");

                var json = AuthHttpGateway.DownloadGoogleUserInfo(tokens.AccessTokenSecret);
                var obj = (Dictionary<string,object>)JSON.parse(json);

                if (obj.TryGetValue("picture", out var oProfileUrl) && oProfileUrl is string profileUrl)
                    tokens.Items[AuthMetadataProvider.ProfileUrlKey] = profileUrl.SanitizeOAuthUrl();

            }
            catch (Exception ex)
            {
                Log.Error($"Could not retrieve google user info for '{tokens.DisplayName}'", ex);
            }

            LoadUserOAuthProvider(userSession, tokens);
        }

        public override void LoadUserOAuthProvider(IAuthSession authSession, IAuthTokens tokens)
        {
            if (!(authSession is AuthUserSession userSession)) return;

            userSession.DisplayName = tokens.DisplayName ?? userSession.DisplayName;
            userSession.FirstName = tokens.FirstName ?? userSession.FirstName;
            userSession.LastName = tokens.LastName ?? userSession.LastName;
            userSession.PrimaryEmail = tokens.Email ?? userSession.PrimaryEmail ?? userSession.Email;
        }
    }
}
