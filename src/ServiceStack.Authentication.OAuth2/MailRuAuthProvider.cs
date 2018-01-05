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

        public string Method { get; private set; }
        public string Secure { get; private set; }

        public MailRuAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name)
        {
            Method = appSettings.GetString($"oauth.{Name}.Method");
            Secure = appSettings.GetString($"oauth.{Name}.Secure");

            this.AuthorizeUrl = this.AuthorizeUrl ?? Realm;
            this.AccessTokenUrl = this.AccessTokenUrl ?? "https://connect.mail.ru/oauth/token";
            this.UserProfileUrl = this.UserProfileUrl ?? "https://www.appsmail.ru/platform/api";
        }

        protected override Dictionary<string, string> CreateAuthInfo(string accessToken)
        {
            var sig = WebRequestUtils.CalculateMD5Hash(
                $"app_id={base.ConsumerKey}method={Method}secure={Secure}session_key={accessToken}{ConsumerSecret}");

            var url = this.UserProfileUrl
                .AddQueryParam("method", Method)
                .AddQueryParam("secure", Secure)
                .AddQueryParam("app_id", ConsumerKey)
                .AddQueryParam("session_key", accessToken)
                .AddQueryParam("sig", sig);

            var json = url.GetJsonFromUrl();

            var objList = JsonSerializer.DeserializeFromString<List<Dictionary<string, string>>>(json);

            if (objList.IsNullOrEmpty())
                return null;

            var obj = objList[0];

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