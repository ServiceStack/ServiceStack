namespace ServiceStack.ServiceInterface.Auth
{
    using System.Collections.Specialized;

    public class DropboxOAuthAuthorizer : OAuthAuthorizer
    {
        public DropboxOAuthAuthorizer(OAuthProvider provider)
            : base(provider)
        {
        }

        protected override bool AcquireRequestToken(NameValueCollection requestTokenResult)
        {
            if (requestTokenResult["oauth_token"] != null)
            {
                this.RequestToken = requestTokenResult["oauth_token"];
                this.RequestTokenSecret = requestTokenResult["oauth_token_secret"];

                return true;
            }

            return false;
        }
    }
}