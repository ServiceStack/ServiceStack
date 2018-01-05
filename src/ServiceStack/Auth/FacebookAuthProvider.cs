using System;
using System.Collections.Generic;
using System.Net;
using System.Web;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    /// <summary>
    /// Create a Facebook App at: https://developers.facebook.com/apps
    /// The Callback URL for your app should match the CallbackUrl provided.
    /// </summary>
    public class FacebookAuthProvider : OAuthProvider
    {
        public const string Name = "facebook";
        public static string Realm = "https://graph.facebook.com/v2.8/";
        public static string PreAuthUrl = "https://www.facebook.com/dialog/oauth";
        public static string[] DefaultFields = { "id", "name", "first_name", "last_name", "email" };

        public string AppId { get; set; }
        public string AppSecret { get; set; }
        public string[] Permissions { get; set; }
        public string[] Fields { get; set; }

        public FacebookAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name, "AppId", "AppSecret")
        {
            this.AppId = appSettings.GetString("oauth.facebook.AppId");
            this.AppSecret = appSettings.GetString("oauth.facebook.AppSecret");
            this.Permissions = appSettings.Get("oauth.facebook.Permissions", TypeConstants.EmptyStringArray);
            this.Fields = appSettings.Get("oauth.facebook.Fields", DefaultFields);
        }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            var tokens = Init(authService, ref session, request);

            //Transfering AccessToken/Secret from Mobile/Desktop App to Server
            if (request?.AccessToken != null)
            {
                if (!AuthHttpGateway.VerifyFacebookAccessToken(AppId, request.AccessToken))
                    return HttpError.Unauthorized("AccessToken is not for App: " + AppId);

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
                Log.Error($"Facebook error callback. {httpRequest.QueryString}");
                return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", error)));
            }             
        
            var code = httpRequest.QueryString[Keywords.Code];
            var isPreAuthCallback = !code.IsNullOrEmpty();
            if (!isPreAuthCallback)
            {
                var preAuthUrl = $"{PreAuthUrl}?client_id={AppId}&redirect_uri={this.CallbackUrl.UrlEncode()}&scope={string.Join(",", Permissions)}";

                this.SaveSession(authService, session, SessionExpiry);
                return authService.Redirect(PreAuthUrlFilter(this, preAuthUrl));
            }

            try
            {
                var accessTokenUrl = $"{AccessTokenUrl}?client_id={AppId}&redirect_uri={this.CallbackUrl.UrlEncode()}&client_secret={AppSecret}&code={code}";
                var contents = AccessTokenUrlFilter(this, accessTokenUrl).GetJsonFromUrl();
                var authInfo = JsonObject.Parse(contents);

                var accessToken = authInfo["access_token"];

                return AuthenticateWithAccessToken(authService, session, tokens, accessToken)
                       ?? authService.Redirect(SuccessRedirectUrlFilter(this, session.ReferrerUrl.SetParam("s", "1"))); //Haz Access!
            }
            catch (WebException we)
            {
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

            var json = AuthHttpGateway.DownloadFacebookUserInfo(accessToken, Fields);
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

                var json = AuthHttpGateway.DownloadFacebookUserInfo(tokens.AccessTokenSecret, "picture");
                var obj = JsonObject.Parse(json);
                var picture = obj.Object("picture");
                var data = picture?.Object("data");
                if (data != null)
                {
                    string profileUrl;
                    if (data.TryGetValue("url", out profileUrl))
                        tokens.Items[AuthMetadataProvider.ProfileUrlKey] = profileUrl.SanitizeOAuthUrl();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Could not retrieve facebook user info for '{tokens.DisplayName}'", ex);
            }

            LoadUserOAuthProvider(userSession, tokens);
        }

        public override void LoadUserOAuthProvider(IAuthSession authSession, IAuthTokens tokens)
        {
            var userSession = authSession as AuthUserSession;
            if (userSession == null) return;

            userSession.FacebookUserId = tokens.UserId ?? userSession.FacebookUserId;
            userSession.FacebookUserName = tokens.UserName ?? userSession.FacebookUserName;
            userSession.DisplayName = tokens.DisplayName ?? userSession.DisplayName;
            userSession.FirstName = tokens.FirstName ?? userSession.FirstName;
            userSession.LastName = tokens.LastName ?? userSession.LastName;
            userSession.PrimaryEmail = tokens.Email ?? userSession.PrimaryEmail ?? userSession.Email;
        }
    }
}
