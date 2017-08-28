using System;
using System.Collections.Generic;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    /// <summary>
    /// Create an app at https://dev.twitter.com/apps to get your ConsumerKey and ConsumerSecret for your app.
    /// The Callback URL for your app should match the CallbackUrl provided.
    /// </summary>
    public class TwitterAuthProvider : OAuthProvider
    {
        public const string Name = "twitter";
        public static string Realm = "https://api.twitter.com/";

        public TwitterAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name)
        {
            this.AuthorizeUrl = appSettings.Get("oauth.twitter.AuthorizeUrl", Realm + "oauth/authenticate");
        }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            if (string.IsNullOrEmpty(ConsumerKey))
                throw new Exception("oauth.twitter.ConsumerKey is required");

            if (string.IsNullOrEmpty(ConsumerSecret))
                throw new Exception("oauth.twitter.ConsumerSecret is required");
            
            var tokens = Init(authService, ref session, request);

            //Transfering AccessToken/Secret from Mobile/Desktop App to Server
            if (request.AccessToken != null && request.AccessTokenSecret != null)
            {
                tokens.AccessToken = request.AccessToken;
                tokens.AccessTokenSecret = request.AccessTokenSecret;

                string userId;
                var validToken = AuthHttpGateway.VerifyTwitterAccessToken(
                    ConsumerKey, ConsumerSecret,
                    tokens.AccessToken, tokens.AccessTokenSecret, out userId);

                if (!validToken)
                    return HttpError.Unauthorized("AccessToken is invalid");

                if (!string.IsNullOrEmpty(request.UserName) && userId != request.UserName)
                    return HttpError.Unauthorized("AccessToken does not match UserId: " + request.UserName);

                tokens.UserId = userId;
                session.IsAuthenticated = true;

                var failedResult = OnAuthenticated(authService, session, tokens, new Dictionary<string, string>());
                var isHtml = authService.Request.IsHtml();
                if (failedResult != null)
                    return ConvertToClientError(failedResult, isHtml);

                return isHtml
                    ? authService.Redirect(SuccessRedirectUrlFilter(this, session.ReferrerUrl.SetParam("s", "1")))
                    : null; //return default AuthenticateResponse
            }

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

        protected override void LoadUserAuthInfo(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            if (authInfo.ContainsKey("user_id"))
                tokens.UserId = authInfo.GetValueOrDefault("user_id");

            if (authInfo.ContainsKey("screen_name"))
                tokens.UserName = authInfo.GetValueOrDefault("screen_name");

            var userId = tokens.UserId ?? userSession.TwitterUserId;

            try
            {
                if (userId != null)
                {
                    var json = AuthHttpGateway.DownloadTwitterUserInfo(
                        ConsumerKey, ConsumerSecret,
                        tokens.AccessToken, tokens.AccessTokenSecret,
                        userId);

                    var objs = JsonObject.ParseArray(json);
                    if (objs.Count > 0)
                    {
                        var obj = objs[0];

                        tokens.DisplayName = obj.Get("name");

                        var userName = obj.Get("screen_name");
                        if (!string.IsNullOrEmpty(userName))
                            tokens.UserName = userName;

                        var email = obj.Get("email");
                        if (!string.IsNullOrEmpty(email))
                            tokens.Email = email;

                        string profileUrl;
                        if (obj.TryGetValue("profile_image_url", out profileUrl))
                            tokens.Items[AuthMetadataProvider.ProfileUrlKey] = profileUrl;

                        if (SaveExtendedUserInfo)
                        {
                            obj.Each(x => authInfo[x.Key] = x.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (userId != null)
                    Log.Error($"Could not retrieve twitter user info for '{userId}'", ex);

                throw;
            }

            LoadUserOAuthProvider(userSession, tokens);
        }

        public override void LoadUserOAuthProvider(IAuthSession authSession, IAuthTokens tokens)
        {
            var userSession = authSession as AuthUserSession;
            if (userSession == null) return;
            
            userSession.TwitterUserId = tokens.UserId ?? userSession.TwitterUserId;
            userSession.TwitterScreenName = tokens.UserName ?? userSession.TwitterScreenName;
            userSession.DisplayName = tokens.DisplayName ?? userSession.DisplayName;
            userSession.Email = tokens.Email ?? userSession.Email;
        }
    }

}