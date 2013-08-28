namespace ServiceStack.Authentication.OAuth2
{
    using System.Collections.Generic;

    using ServiceStack.Configuration;
    using ServiceStack.Text;

    public class GoogleOAuth2Provider : OAuth2Provider
    {
        public const string Name = "GoogleOAuth";

        public const string Realm = "https://accounts.google.com/o/oauth2/auth";

        public GoogleOAuth2Provider(IResourceManager appSettings)
            : base(appSettings, Realm, Name)
        {
        }

        protected override Dictionary<string, string> CreateAuthInfo(string accessToken)
        {
            var url = this.UserProfileUrl + accessToken;
            string json = url.GetJsonFromUrl();
            var obj = JsonObject.Parse(json);
            var authInfo = new Dictionary<string, string>
                               {
                                   { "user_id", obj.Get("id") }, 
                                   { "username", obj.Get("email") }, 
                                   { "email", obj.Get("email") }, 
                                   { "name", obj.Get("name") }, 
                                   { "first_name", obj.Get("given_name") }, 
                                   { "last_name", obj.Get("family_name") }
                               };
            return authInfo;
        }
    }
}