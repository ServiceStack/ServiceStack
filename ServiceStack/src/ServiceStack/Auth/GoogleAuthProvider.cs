using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    /// <summary>
    ///Create an OAuth2 App at: https://code.google.com/apis/console/
    ///The Apps Callback URL should match the CallbackUrl here.
    ///Google OAuth2 info: https://developers.google.com/accounts/docs/OAuth2Login
    ///Google OAuth2 Scopes from: https://www.googleapis.com/discovery/v1/apis/oauth2/v2/rest?fields=auth(oauth2(scopes))
    ///https://www.googleapis.com/auth/plus.login: Know your name, basic info, and list of people you're connected to on Google+
    ///https://www.googleapis.com/auth/plus.me Know who you are on Google+
    ///https://www.googleapis.com/auth/userinfo.email View your email address
    ///https://www.googleapis.com/auth/userinfo.profile View basic information about your account
    /// </summary>
    public class GoogleAuthProvider : OAuth2Provider
    {
        public const string Name = "google";
        public static string Realm = DefaultAuthorizeUrl;

        public const string DefaultAuthorizeUrl = "https://accounts.google.com/o/oauth2/v2/auth";
        public const string DefaultAccessTokenUrl = "https://oauth2.googleapis.com/token";
        public const string DefaultUserProfileUrl = "https://www.googleapis.com/oauth2/v2/userinfo";
        public const string DefaultVerifyTokenUrl = "https://www.googleapis.com/oauth2/v2/tokeninfo?access_token={0}";

        public GoogleAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name)
        {
            VerifyAccessTokenAsync = OnVerifyAccessTokenAsync;
            this.AuthorizeUrl = appSettings.Get($"oauth.{Name}.AuthorizeUrl", DefaultAuthorizeUrl);
            this.AccessTokenUrl = appSettings.Get($"oauth.{Name}.AccessTokenUrl", DefaultAccessTokenUrl);
            this.UserProfileUrl = appSettings.Get($"oauth.{Name}.UserProfileUrl", DefaultUserProfileUrl);
            this.VerifyTokenUrl = appSettings.Get($"oauth.{Name}.VerifyTokenUrl", DefaultVerifyTokenUrl);

            if (this.Scopes == null || this.Scopes.Length == 0)
            {
                this.Scopes = new[] {
                    "https://www.googleapis.com/auth/userinfo.profile",
                    "https://www.googleapis.com/auth/userinfo.email"
                };
            }

            Icon = Svg.ImageSvg("<svg xmlns='http://www.w3.org/2000/svg' fill='currentColor' viewBox='0 0 24 24'><path d='M11.99 13.9v-3.72h9.36c.14.63.25 1.22.25 2.05c0 5.71-3.83 9.77-9.6 9.77c-5.52 0-10-4.48-10-10S6.48 2 12 2c2.7 0 4.96.99 6.69 2.61l-2.84 2.76c-.72-.68-1.98-1.48-3.85-1.48c-3.31 0-6.01 2.75-6.01 6.12s2.7 6.12 6.01 6.12c3.83 0 5.24-2.65 5.5-4.22h-5.51v-.01z' fill='currentColor' /></svg>");
            NavItem = new NavItem {
                Href = "/auth/" + Name,
                Label = "Sign In with Google",
                Id = "btn-" + Name,
                ClassName = "btn-social btn-google",
                IconClass = "fab svg-google",
            };
        }

        public virtual async Task<bool> OnVerifyAccessTokenAsync(string accessToken, AuthContext ctx)
        {
            var url = VerifyTokenUrl.Fmt(accessToken);
            var json = await url.GetJsonFromUrlAsync();
            var obj = JsonObject.Parse(json);
            var issuedTo = obj["issued_to"];
            return issuedTo == ConsumerKey;
        }

        protected override async Task<object> AuthenticateWithAccessTokenAsync(IServiceBase authService, IAuthSession session, IAuthTokens tokens,
            string accessToken, Dictionary<string, string> authInfo = null, CancellationToken token = default)
        {
            tokens.AccessTokenSecret = accessToken;

            var accessTokenAuthInfo = await CreateAuthInfoAsync(accessToken, token).ConfigAwait();

            session.IsAuthenticated = true;

            return await OnAuthenticatedAsync(authService, session, tokens, accessTokenAuthInfo, token).ConfigAwait();
        }

        protected override async Task<Dictionary<string, string>> CreateAuthInfoAsync(string accessToken, CancellationToken token = default)
        {
            var url = this.UserProfileUrl.AddQueryParam("access_token", accessToken);
            var json = await url.GetJsonFromUrlAsync(token:token).ConfigAwait();
            var obj = JsonObject.Parse(json);
            
            obj.MoveKey("id", "user_id");
            obj.MoveKey("given_name", "first_name");
            obj.MoveKey("family_name", "last_name");
            obj.MoveKey("picture", AuthMetadataProvider.ProfileUrlKey, profileUrl => profileUrl.SanitizeOAuthUrl());

            return obj;
        }
    }
}
