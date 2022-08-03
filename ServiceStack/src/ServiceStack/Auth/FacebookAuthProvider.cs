using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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
        public static string Realm = "https://graph.facebook.com/v3.2/";
        public static string PreAuthUrl = "https://www.facebook.com/dialog/oauth";
        public static string[] DefaultFields = { "id", "name", "first_name", "last_name", "email" };

        public string AppId { get; set; }
        public string AppSecret { get; set; }
        public string[] Permissions { get; set; }
        public string[] Fields { get; set; }

        public bool RetrieveUserPicture { get; set; } = true;

        public override Dictionary<string, string> Meta { get; } = new Dictionary<string, string> {
            [Keywords.Allows] = Keywords.Embed + "," + Keywords.AccessTokenAuth,
        };
        
        public FacebookAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name, "AppId", "AppSecret")
        {
            this.AppId = appSettings.GetString("oauth.facebook.AppId");
            this.AppSecret = appSettings.GetString("oauth.facebook.AppSecret");
            this.Permissions = appSettings.Get("oauth.facebook.Permissions", TypeConstants.EmptyStringArray);
            this.Fields = appSettings.Get("oauth.facebook.Fields", DefaultFields);

            Icon = Svg.ImageSvg("<svg xmlns='http://www.w3.org/2000/svg' fill='currentColor' viewBox='0 0 20 20'><path fill-rule='evenodd' d='M20 10c0-5.523-4.477-10-10-10S0 4.477 0 10c0 4.991 3.657 9.128 8.438 9.878v-6.987h-2.54V10h2.54V7.797c0-2.506 1.492-3.89 3.777-3.89 1.094 0 2.238.195 2.238.195v2.46h-1.26c-1.243 0-1.63.771-1.63 1.562V10h2.773l-.443 2.89h-2.33v6.988C16.343 19.128 20 14.991 20 10z' clip-rule='evenodd' /></svg>");
            NavItem = new NavItem {
                Href = "/auth/" + Name,
                Label = "Sign In with Facebook",
                Id = "btn-" + Name,
                ClassName = "btn-social btn-facebook",
                IconClass = "fab svg-facebook",
            };
        }

        public override async Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token = default)
        {
            var tokens = Init(authService, ref session, request);
            var ctx = CreateAuthContext(authService, session, tokens);

            //Transferring AccessToken/Secret from Mobile/Desktop App to Server
            if (request?.AccessToken != null)
            {
                if (!await AuthHttpGateway.VerifyFacebookAccessTokenAsync(AppId, request.AccessToken, token).ConfigAwait())
                    return HttpError.Unauthorized("AccessToken is not for App: " + AppId);

                var isHtml = authService.Request.IsHtml();
                var failedResult = await AuthenticateWithAccessTokenAsync(authService, session, tokens, request.AccessToken, token).ConfigAwait();
                if (failedResult != null)
                    return ConvertToClientError(failedResult, isHtml);

                return isHtml
                    ? await authService.Redirect(SuccessRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("s", "1"))).SuccessAuthResultAsync(authService,session).ConfigAwait()
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
                return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", error)));
            }             
        
            var code = httpRequest.QueryString[Keywords.Code];
            var isPreAuthCallback = !code.IsNullOrEmpty();
            if (!isPreAuthCallback)
            {
                var preAuthUrl = $"{PreAuthUrl}?client_id={AppId}&redirect_uri={this.CallbackUrl.UrlEncode()}&scope={string.Join(",", Permissions)}&{Keywords.State}={session.Id}";

                await this.SaveSessionAsync(authService, session, SessionExpiry, token).ConfigAwait();
                return authService.Redirect(PreAuthUrlFilter(ctx, preAuthUrl));
            }

            try
            {
                var accessTokenUrl = $"{AccessTokenUrl}?client_id={AppId}&redirect_uri={this.CallbackUrl.UrlEncode()}&client_secret={AppSecret}&code={code}";
                var contents = await AccessTokenUrlFilter(ctx, accessTokenUrl).GetJsonFromUrlAsync(token: token).ConfigAwait();
                var authInfo = JsonObject.Parse(contents);

                var accessToken = authInfo["access_token"];

                //Haz Access!
                return await AuthenticateWithAccessTokenAsync(authService, session, tokens, accessToken, token).ConfigAwait()
                    ?? await authService.Redirect(SuccessRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("s", "1"))).SuccessAuthResultAsync(authService,session).ConfigAwait();
            }
            catch (WebException we)
            {
                var statusCode = ((HttpWebResponse)we.Response).StatusCode;
                if (statusCode == HttpStatusCode.BadRequest)
                {
                    return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", "AccessTokenFailed")));
                }
            }

            //Shouldn't get here
            return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", "Unknown")));
        }

        protected virtual async Task<object> AuthenticateWithAccessTokenAsync(IServiceBase authService, IAuthSession session, IAuthTokens tokens, string accessToken, CancellationToken token=default)
        {
            tokens.AccessTokenSecret = accessToken;

            var json = AuthHttpGateway.DownloadFacebookUserInfo(accessToken, Fields);
            var authInfo = JsonObject.Parse(json);

            session.IsAuthenticated = true;

            return await OnAuthenticatedAsync(authService, session, tokens, authInfo, token).ConfigAwait();
        }

        protected override async Task LoadUserAuthInfoAsync(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo, CancellationToken token=default)
        {
            try
            {
                tokens.UserId = authInfo.Get("id");
                tokens.UserName = authInfo.Get("id") ?? authInfo.Get("username");
                tokens.DisplayName = authInfo.Get("name");
                tokens.FirstName = authInfo.Get("first_name");
                tokens.LastName = authInfo.Get("last_name");
                tokens.Email = authInfo.Get("email");

                if (RetrieveUserPicture)
                {
                    var json = await AuthHttpGateway.DownloadFacebookUserInfoAsync(tokens.AccessTokenSecret, new[]{ "picture" }, token).ConfigAwait();
                    var obj = JsonObject.Parse(json);
                    var picture = obj.Object("picture");
                    var data = picture?.Object("data");
                    if (data != null)
                    {
                        if (data.TryGetValue("url", out var profileUrl))
                        {
                            tokens.Items[AuthMetadataProvider.ProfileUrlKey] = profileUrl.SanitizeOAuthUrl();
                            
                            if (string.IsNullOrEmpty(userSession.ProfileUrl))
                                userSession.ProfileUrl = profileUrl.SanitizeOAuthUrl();
                        }
                    }
                }

                userSession.UserAuthName = tokens.Email;
            }
            catch (Exception ex)
            {
                Log.Error($"Could not retrieve facebook user info for '{tokens.DisplayName}'", ex);
            }

            await LoadUserOAuthProviderAsync(userSession, tokens).ConfigAwait();
        }

        public override Task LoadUserOAuthProviderAsync(IAuthSession authSession, IAuthTokens tokens)
        {
            if (authSession is AuthUserSession userSession)
            {
                userSession.FacebookUserId = tokens.UserId ?? userSession.FacebookUserId;
                userSession.FacebookUserName = tokens.UserName ?? userSession.FacebookUserName;
                userSession.DisplayName = tokens.DisplayName ?? userSession.DisplayName;
                userSession.FirstName = tokens.FirstName ?? userSession.FirstName;
                userSession.LastName = tokens.LastName ?? userSession.LastName;
                userSession.PrimaryEmail = tokens.Email ?? userSession.PrimaryEmail ?? userSession.Email;
            }
            return Task.CompletedTask;
        }
        
    }
}
