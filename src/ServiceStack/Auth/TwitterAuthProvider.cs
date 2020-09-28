using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        public const string DefaultAuthorizeUrl = "https://api.twitter.com/oauth/authenticate";

        public bool RetrieveEmail { get; set; } = true;

        public override Dictionary<string, string> Meta { get; } = new Dictionary<string, string> {
            [Keywords.Allows] = Keywords.AccessTokenAuth,
        };

        public TwitterAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name)
        {
            this.AuthorizeUrl = appSettings.Get("oauth.twitter.AuthorizeUrl", DefaultAuthorizeUrl);

            NavItem = new NavItem {
                Href = "/auth/" + Name,
                Label = "Sign In with Twitter",
                Id = "btn-" + Name,
                ClassName = "btn-social btn-twitter",
                IconClass = "fab svg-twitter",
            };
        }

        public override async Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token = default)
        {
            var tokens = Init(authService, ref session, request);

            //Transferring AccessToken/Secret from Mobile/Desktop App to Server
            if (request.AccessToken != null && request.AccessTokenSecret != null)
            {
                tokens.AccessToken = request.AccessToken;
                tokens.AccessTokenSecret = request.AccessTokenSecret;

                var validToken = await AuthHttpGateway.VerifyTwitterAccessTokenAsync(
                    ConsumerKey, ConsumerSecret,
                    tokens.AccessToken, tokens.AccessTokenSecret, token).ConfigAwait();

                if (validToken == null)
                    return HttpError.Unauthorized("AccessToken is invalid");

                if (!string.IsNullOrEmpty(request.UserName) && validToken.UserId != request.UserName)
                    return HttpError.Unauthorized("AccessToken does not match UserId: " + request.UserName);

                tokens.UserId = validToken.UserId;
                session.IsAuthenticated = true;

                var failedResult = await OnAuthenticatedAsync(authService, session, tokens, new Dictionary<string, string>(), token).ConfigAwait();
                var isHtml = authService.Request.IsHtml();
                if (failedResult != null)
                    return ConvertToClientError(failedResult, isHtml);

                return isHtml
                    ? await authService.Redirect(SuccessRedirectUrlFilter(this, session.ReferrerUrl.SetParam("s", "1"))).SuccessAuthResultAsync(authService,session).ConfigAwait()
                    : null; //return default AuthenticateResponse
            }

            //Default OAuth logic based on Twitter's OAuth workflow
            if (!tokens.RequestTokenSecret.IsNullOrEmpty() && !request.oauth_token.IsNullOrEmpty())
            {
                if (OAuthUtils.AcquireAccessToken(tokens.RequestTokenSecret, request.oauth_token, request.oauth_verifier))
                {
                    session.IsAuthenticated = true;
                    tokens.AccessToken = OAuthUtils.AccessToken;
                    tokens.AccessTokenSecret = OAuthUtils.AccessTokenSecret;

                    //Haz Access
                    return await OnAuthenticatedAsync(authService, session, tokens, OAuthUtils.AuthInfo, token).ConfigAwait()
                        ?? await authService.Redirect(SuccessRedirectUrlFilter(this, session.ReferrerUrl.SetParam("s", "1"))).SuccessAuthResultAsync(authService,session).ConfigAwait();
                }

                //No Joy :(
                tokens.RequestToken = null;
                tokens.RequestTokenSecret = null;
                await this.SaveSessionAsync(authService, session, SessionExpiry, token).ConfigAwait();
                return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", "AccessTokenFailed")));
            }
            if (OAuthUtils.AcquireRequestToken())
            {
                tokens.RequestToken = OAuthUtils.RequestToken;
                tokens.RequestTokenSecret = OAuthUtils.RequestTokenSecret;
                await this.SaveSessionAsync(authService, session, SessionExpiry, token).ConfigAwait();

                //Redirect to OAuth provider to approve access
                return authService.Redirect(AccessTokenUrlFilter(this, this.AuthorizeUrl
                    .AddQueryParam("oauth_token", tokens.RequestToken)
                    .AddQueryParam("oauth_callback", session.ReferrerUrl)
                    .AddQueryParam(Keywords.State, session.Id) // doesn't support state param atm, but it's here when it does
                ));
            }

            return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", "RequestTokenFailed")));
        }

        protected override async Task LoadUserAuthInfoAsync(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo, CancellationToken token = default)
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
                    var json = await AuthHttpGateway.DownloadTwitterUserInfoAsync(
                        ConsumerKey, ConsumerSecret,
                        tokens.AccessToken, tokens.AccessTokenSecret,
                        userId, token).ConfigAwait();

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
                        {
                            tokens.Email = email;
                        }
                        else if (RetrieveEmail)
                        {
                            try 
                            { 
                                var authId = await AuthHttpGateway.VerifyTwitterAccessTokenAsync(
                                    ConsumerKey, ConsumerSecret,
                                    tokens.AccessToken, tokens.AccessTokenSecret, token).ConfigAwait();

                                tokens.Email = authId?.Email;
                            }
                            catch (Exception ex)
                            {
                                Log.Warn($"Could not retrieve Twitter Email", ex);
                            }
                        }

                        if (obj.TryGetValue("profile_image_url", out var profileUrl))
                        {
                            tokens.Items[AuthMetadataProvider.ProfileUrlKey] = profileUrl;

                            if (string.IsNullOrEmpty(userSession.ProfileUrl))
                                userSession.ProfileUrl = profileUrl.SanitizeOAuthUrl();
                        }

                        if (SaveExtendedUserInfo)
                        {
                            obj.Each(x => authInfo[x.Key] = x.Value);
                        }
                    }
                }
                userSession.UserAuthName = tokens.UserName ?? tokens.Email;
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
            if (!(authSession is AuthUserSession userSession)) return;
            
            userSession.TwitterUserId = tokens.UserId ?? userSession.TwitterUserId;
            userSession.TwitterScreenName = tokens.UserName ?? userSession.TwitterScreenName;
            userSession.DisplayName = tokens.DisplayName ?? userSession.DisplayName;
            userSession.Email = tokens.Email ?? userSession.Email;
        }
    }

}