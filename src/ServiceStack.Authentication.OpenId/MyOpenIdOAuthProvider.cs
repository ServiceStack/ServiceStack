using ServiceStack.Configuration;

namespace ServiceStack.Authentication.OpenId
{
    public class MyOpenIdOAuthProvider : OpenIdOAuthProvider
    {
        public const string Name = "MyOpenId";
        public static string Realm = "http://www.myopenid.com";

        public MyOpenIdOAuthProvider(IResourceManager appSettings)
            : base(appSettings, Name, Realm) { }
    }
}