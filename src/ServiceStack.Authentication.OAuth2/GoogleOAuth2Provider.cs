using System.Collections.Generic;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Authentication.OAuth2
{
    /*
        Create an OAuth2 App at: https://code.google.com/apis/console/
        The Apps Callback URL should match the CallbackUrl here.
     
        Google OAuth2 info: https://developers.google.com/accounts/docs/OAuth2Login
        Google OAuth2 Scopes from: https://www.googleapis.com/discovery/v1/apis/oauth2/v2/rest?fields=auth(oauth2(scopes))
            https://www.googleapis.com/auth/plus.login: Know your name, basic info, and list of people you're connected to on Google+
            https://www.googleapis.com/auth/plus.me Know who you are on Google+
            https://www.googleapis.com/auth/userinfo.email View your email address
            https://www.googleapis.com/auth/userinfo.profile View basic information about your account
     */
    public class GoogleOAuth2Provider : OAuth2Provider
    {
        public const string Name = "GoogleOAuth";

        public const string Realm = "https://accounts.google.com/o/oauth2/auth";

        public GoogleOAuth2Provider(IAppSettings appSettings)
            : base(appSettings, Realm, Name)
        {
            this.AuthorizeUrl = this.AuthorizeUrl ?? Realm;
            this.AccessTokenUrl = this.AccessTokenUrl ?? "https://accounts.google.com/o/oauth2/token";
            this.UserProfileUrl = this.UserProfileUrl ?? "https://www.googleapis.com/oauth2/v1/userinfo";

            if (this.Scopes.Length == 0)
            {
                this.Scopes = new[] {
                    "https://www.googleapis.com/auth/userinfo.profile",
                    "https://www.googleapis.com/auth/userinfo.email"
                };
            }
        }

        protected override Dictionary<string, string> CreateAuthInfo(string accessToken)
        {
            var url = this.UserProfileUrl.AddQueryParam("access_token", accessToken);
            string json = url.GetJsonFromUrl();
            var obj = JsonObject.Parse(json);
            var authInfo = new Dictionary<string, string>
            {
                { "user_id", obj["id"] }, 
                { "username", obj["email"] }, 
                { "email", obj["email"] }, 
                { "name", obj["name"] }, 
                { "first_name", obj["given_name"] }, 
                { "last_name", obj["family_name"] },
                { "gender", obj["gender"] },
                { "birthday", obj["birthday"] },
                { "link", obj["link"] },
                { "picture", obj["picture"] },
                { "locale", obj["locale"] },
                { AuthMetadataProvider.ProfileUrlKey, obj["picture"] },
            };
            return authInfo;
        }
    }
}