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

        protected override void LoadUserAuthInfo(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            if (authInfo.ContainsKey("user_id"))
                tokens.UserId = authInfo.GetValueOrDefault("user_id");

            if (authInfo.ContainsKey("screen_name"))
                tokens.UserName = authInfo.GetValueOrDefault("screen_name");

            try
            {
                if (tokens.UserId != null)
                {
                    var oauthToken = new OAuthAccessToken
                    {
                        OAuthProvider = this,
                        AccessToken = tokens.AccessToken,
                        AccessTokenSecret = tokens.AccessTokenSecret,
                    };
                    var json = AuthHttpGateway.DownloadTwitterUserInfo(oauthToken, tokens.UserId);
                    var objs = JsonObject.ParseArray(json);
                    if (objs.Count > 0)
                    {
                        var obj = objs[0];
                        tokens.DisplayName = obj.Get("name");

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
                Log.Error($"Could not retrieve twitter user info for '{userSession.TwitterUserId}'", ex);
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
        }
    }

}