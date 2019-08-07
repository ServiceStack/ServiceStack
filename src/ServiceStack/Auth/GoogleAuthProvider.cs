using System;
using System.Collections.Generic;
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
    public class GoogleAuthProvider : OAuth2Provider
    {
        public const string Name = "google";
        public static string Realm = DefaultAuthorizeUrl;

        public const string DefaultAuthorizeUrl = "https://accounts.google.com/o/oauth2/v2/auth";
        public const string DefaultAccessTokenUrl = "https://oauth2.googleapis.com/token";
        public const string DefaultUserProfileUrl = "https://www.googleapis.com/oauth2/v2/userinfo";
        public const string DefaultVerifyTokenUrl = "https://www.googleapis.com/oauth2/v2/tokeninfo?access_token={0}";

        public GoogleAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name)
        {
            VerifyAccessToken = OnVerifyAccessToken;
            this.AuthorizeUrl = appSettings.Get($"oauth.{Name}.AuthorizeUrl", DefaultAuthorizeUrl);
            this.AccessTokenUrl = appSettings.Get($"oauth.{Name}.AccessTokenUrl", DefaultAccessTokenUrl);
            this.UserProfileUrl = appSettings.Get($"oauth.{Name}.UserProfileUrl", DefaultUserProfileUrl);
            this.VerifyTokenUrl = appSettings.Get($"oauth.{Name}.VerifyTokenUrl", DefaultVerifyTokenUrl);

            if (this.Scopes == null || this.Scopes.Length == 0)
            {
                this.Scopes = new[] {
                    "https://www.googleapis.com/auth/userinfo.profile",
                    "https://www.googleapis.com/auth/userinfo.email"
                };
            }

            NavItem = new NavItem {
                Href = "/auth/" + Name,
                Label = "Sign in with Google",
                Id = "btn-" + Name,
                ClassName = "btn-social btn-google",
                IconClass = "fab svg-google",
            };
        }

        public virtual bool OnVerifyAccessToken(string accessToken)
        {
            var url = VerifyTokenUrl.Fmt(accessToken);
            var json = url.GetJsonFromUrl();
            var obj = JsonObject.Parse(json);
            var issuedTo = obj["issued_to"];
            return issuedTo == ConsumerKey;
        }

        protected override object AuthenticateWithAccessToken(IServiceBase authService, IAuthSession session, IAuthTokens tokens, string accessToken)
        {
            tokens.AccessTokenSecret = accessToken;

            var authInfo = CreateAuthInfo(accessToken);

            session.IsAuthenticated = true;

            return OnAuthenticated(authService, session, tokens, authInfo);
        }

        protected override Dictionary<string, string> CreateAuthInfo(string accessToken)
        {
            var url = this.UserProfileUrl.AddQueryParam("access_token", accessToken);
            var json = url.GetJsonFromUrl();
            var obj = JsonObject.Parse(json);
            
            obj.MoveKey("id", "user_id");
            obj.MoveKey("given_name", "first_name");
            obj.MoveKey("family_name", "last_name");
            obj.MoveKey("picture", AuthMetadataProvider.ProfileUrlKey, profileUrl => profileUrl.SanitizeOAuthUrl());

            return obj;
        }
    }
}
