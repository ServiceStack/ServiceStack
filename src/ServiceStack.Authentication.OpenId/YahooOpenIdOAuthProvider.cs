using ServiceStack.Configuration;

namespace ServiceStack.Authentication.OpenId
{
    public class YahooOpenIdOAuthProvider : OpenIdOAuthProvider
    {
        public const string Name = "YahooOpenId";
        public static string Realm = "https://me.yahoo.com";

        public YahooOpenIdOAuthProvider(IAppSettings appSettings)
            : base(appSettings, Name, Realm) { }
    }
}