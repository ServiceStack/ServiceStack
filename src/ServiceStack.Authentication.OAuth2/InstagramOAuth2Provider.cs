using System;
using System.Collections.Generic;
using System.Net;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Authentication.OAuth2
{
    /// <summary>
    /// More info at: http://instagram.com/developer/authentication/
    /// </summary>
    public class InstagramOAuth2Provider : OAuth2Provider
    {
        public const string Name = "instagram";

        public const string Realm = "https://api.instagram.com/oauth/authorize";

        public InstagramOAuth2Provider(IAppSettings appSettings)
            : base(appSettings, Realm, Name)
        {
            this.AuthorizeUrl = this.AuthorizeUrl ?? Realm;
            this.AccessTokenUrl = this.AccessTokenUrl ?? "https://api.instagram.com/oauth/access_token";

            this.UserProfileUrl = this.UserProfileUrl
                ?? "https://api.instagram.com/v1/users/self";

            if (this.Scopes.Length == 0)
            {
                this.Scopes = new[] {
                    "basic"
                };
            }
            
            this.AuthClientFilter += authClient => authClient.JsonReaderQuotas.MaxDepth = 10;
        }

        protected override Dictionary<string, string> CreateAuthInfo(string accessToken)
        {
            var url = this.UserProfileUrl.AddQueryParam("access_token", accessToken);
            var json = url.GetJsonFromUrl();

            var obj = JsonObject.Parse(json);
            var data = obj.Object("data");
            var authInfo = new Dictionary<string, string>
            {
                { "user_id", data["id"] }, 
                { "username", data["username"] }, 
                { "name", data["full_name"] }, 
                { "picture", data["profile_picture"] },
                { AuthMetadataProvider.ProfileUrlKey, data["profile_picture"] },
            };

            return authInfo;
        }
    }
}
