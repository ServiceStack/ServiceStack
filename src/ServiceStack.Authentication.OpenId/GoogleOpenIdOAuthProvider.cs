using ServiceStack.Configuration;

namespace ServiceStack.Authentication.OpenId
{
    public class GoogleOpenIdOAuthProvider : OpenIdOAuthProvider
    {
        public const string Name = "GoogleOpenId";
        public static string Realm = "https://www.google.com/accounts/o8/id";

        public GoogleOpenIdOAuthProvider(IAppSettings appSettings)
            : base(appSettings, Name, Realm) { }
    }
}