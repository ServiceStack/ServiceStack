using ServiceStack.Configuration;

namespace ServiceStack.Authentication.OpenId
{
    public class GoogleOpenIdOAuthProvider : OpenIdOAuthProvider
    {
        public const string Name = "GoogleOpenId";
        public static string Realm = "https://www.google.com/accounts/o8/id";

        public GoogleOpenIdOAuthProvider(IResourceManager appSettings)
            : base(appSettings, Name, Realm) { }

        public override bool IsAuthorized(ServiceInterface.Auth.IAuthSession session, ServiceInterface.Auth.IOAuthTokens tokens, ServiceInterface.Auth.Auth request = null)
        {
            if (request != null)
            {
                if (!LoginMatchesSession(session, request.UserName)) return false;
            }

            // For GoogleOpenId, AccessTokenSecret is null/empty, but UserId is populated w/ authenticated url from Google            
            return tokens != null && !string.IsNullOrEmpty(tokens.UserId);
        }
    }
}