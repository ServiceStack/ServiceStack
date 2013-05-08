namespace ServiceStack.ServiceInterface.Auth
{
    using ServiceStack.Configuration;

    public class DropboxAuthProvider : OAuthProvider
    {
        public const string Name = "dropbox";

        public static string Realm = "https://api.dropbox.com/1/";

        public DropboxAuthProvider(IResourceManager appSettings)
            : base(appSettings, Realm, Name)
        {
            this.OAuthUtils = new DropboxOAuthAuthorizer(this);
        }
    }
}