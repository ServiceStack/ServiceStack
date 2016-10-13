using System.Collections.Generic;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Authentication.OAuth2
{
    public class MailRuAuthProvider : OAuth2Provider
    {
        public const string Name = "mailru";
        public const string Realm = "https://connect.mail.ru/oauth/authorize";

        public MailRuAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name)
        {
            this.AuthorizeUrl = this.AuthorizeUrl ?? Realm;
            this.AccessTokenUrl = this.AccessTokenUrl ?? "https://connect.mail.ru/oauth/token";
            this.UserProfileUrl = this.UserProfileUrl ?? "https://www.appsmail.ru/platform/api";
        }

        protected override Dictionary<string, string> CreateAuthInfo(string accessToken)
        {
            var url = this.UserProfileUrl.AddQueryParam("access_token", accessToken);
            string json = url.GetJsonFromUrl();
            var obj = JsonObject.Parse(json);

            var authInfo = new Dictionary<string, string>
                {
                    { "user_id", obj["uid"] },
                    { "username", obj["email"] },
                    { "email", obj["email"] },
                    { "name", obj["nick"] },
                    { "first_name", obj["first_name"] },
                    { "last_name", obj["last_name"] },
                    { "gender", obj["sex"] },
                    { "birthday", obj["birthday"] },
                    { "link", obj["link"] },
                    { "picture", obj["pic"] },
                    { AuthMetadataProvider.ProfileUrlKey, obj["pic_180"] }
                };

            return authInfo;
        }
    }
}