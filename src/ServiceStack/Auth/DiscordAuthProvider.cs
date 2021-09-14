using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    /// <summary>
    /// Create an OAuth2 App at: https://discord.com/developers/applications
    /// The Apps Callback URL should match the CallbackUrl here.
    /// Discord OAuth2 info: https://discord.com/developers/docs/topics/oauth2
    /// Discord OAuth2 Scopes from: https://discord.com/developers/docs/topics/oauth2#shared-resources-oauth2-scopes
    /// email: Basic info, plus will return email info from /users/@me API, this is the minimum required for ServiceStack
    /// integration.
    ///
    /// Checking of email verification is enforced due to Discord not requiring verified emails.
    ///
    /// Use `oauth.discord.ClientId` and `oauth.discord.ClientSecret` for Discord App settings.
    /// </summary>
    public class DiscordAuthProvider : OAuth2Provider
    {
        public const string Name = "discord";
        public static string Realm = DefaultAuthorizeUrl;

        const string DefaultAuthorizeUrl = "https://discord.com/api/oauth2/authorize";
        const string DefaultAccessTokenUrl = "https://discord.com/api/oauth2/token";
        const string DefaultUserProfileUrl = "https://discord.com/api/users/@me";

        public DiscordAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name, "ClientId", "ClientSecret")
        {
            AuthorizeUrl = appSettings.Get($"oauth.{Name}.AuthorizeUrl", DefaultAuthorizeUrl);
            AccessTokenUrl = appSettings.Get($"oauth.{Name}.AccessTokenUrl", DefaultAccessTokenUrl);
            UserProfileUrl = appSettings.Get($"oauth.{Name}.UserProfileUrl", DefaultUserProfileUrl);

            if (Scopes == null || Scopes.Length == 0)
            {
                Scopes = new[]
                {
                    "email"
                };
            }

            NavItem = new NavItem
            {
                Href = "/auth/" + Name,
                Label = "Sign In with Discord",
                Id = "btn-" + Name,
                ClassName = "btn-social btn-discord",
                IconClass = "fab fa-discord",
            };
        }

        protected override async Task<string> GetAccessTokenJsonAsync(string code, AuthContext ctx,
            CancellationToken token = new())
        {
            var payload =
                $"client_id={ConsumerKey}&client_secret={ConsumerSecret}&code={code}&redirect_uri={CallbackUrl.UrlEncode()}&grant_type=authorization_code";
            var url = AccessTokenUrlFilter(ctx, AccessTokenUrl);
            var contents = await url.PostToUrlAsync(payload, "application/json", token: token)
                .ConfigAwait();
            return contents;
        }

        protected override async Task<Dictionary<string, string>> CreateAuthInfoAsync(string accessToken,
            CancellationToken token = new())
        {
            var json = await DefaultUserProfileUrl
                .GetJsonFromUrlAsync(request => { request.Headers.Add("Authorization", "Bearer " + accessToken); },
                    token: token).ConfigAwait();
            var obj = JsonObject.Parse(json);
            var verifiedEmail = obj.ContainsKey("verified") && obj.Get<bool>("verified");
            if (!verifiedEmail)
                throw new Exception("Email not verified");
            obj.Add("name", obj["username"]);
            obj.MoveKey("id", "user_id");
            obj.MoveKey("username", "first_name");
            obj.MoveKey("avatar", AuthMetadataProvider.ProfileUrlKey, val =>
                "https://cdn.discordapp.com/avatars/" + obj["user_id"] + "/" + val + ".png");
            return obj;
        }
    }
}