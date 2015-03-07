using System.Collections.Generic;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Authentication.OAuth2
{
        /*
            Create an OAuth2 App at: https://account.live.com/developers/applications
            The Apps Callback URL should match the CallbackUrl here.
     
            Microsoft Account OAuth2 info: https://msdn.microsoft.com/en-us/library/dn659752.aspx
            Microsoft Account OAuth2 Scopes from: https://msdn.microsoft.com/en-us/library/hh243646.aspx)

         */
        public class MicrosoftLiveOAuth2Provider : OAuth2Provider
        {
            public const string Name = "MicrosoftOAuth";

            public const string Realm = "https://login.live.com/oauth20_authorize.srf";

            public MicrosoftLiveOAuth2Provider(IAppSettings appSettings)
                : base(appSettings, Realm, Name)
            {
                this.AuthorizeUrl = this.AuthorizeUrl ?? Realm;
                this.AccessTokenUrl = this.AccessTokenUrl ?? "https://login.live.com/oauth20_token.srf";
                this.UserProfileUrl = this.UserProfileUrl ?? "https://apis.live.net/v5.0/me";

                if (this.Scopes.Length == 0)
                {
                    this.Scopes = new[] {
                    "wl.signin",
                    "wl.basic",
                    "wl.emails"
                };
                }
            }

            protected override Dictionary<string, string> CreateAuthInfo(string accessToken)
            {
                var url = this.UserProfileUrl.AddQueryParam("access_token", accessToken);
                string json = url.GetJsonFromUrl();
                var obj = JsonObject.Parse(json);
                var emails = obj.Object("emails");
                var authInfo = new Dictionary<string, string>
            {
                { "user_id", obj["id"] }, 
                { "username", emails["account"] }, 
                { "email", emails["preferred"] }, 
                { "name", obj["name"] }, 
                { "first_name", obj["first_name"] }, 
                { "last_name", obj["last_name"] },
                { "gender", obj["gender"] },
                { "locale", obj["locale"] }
            };
                return authInfo;
            }
        }
    }
