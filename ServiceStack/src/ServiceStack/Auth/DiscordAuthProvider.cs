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
                    "identify",
                    "email"
                };
            }

            Icon = Svg.ImageSvg("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path d='M14.82 4.26a10.14 10.14 0 0 0-.53 1.1a14.66 14.66 0 0 0-4.58 0a10.14 10.14 0 0 0-.53-1.1a16 16 0 0 0-4.13 1.3a17.33 17.33 0 0 0-3 11.59a16.6 16.6 0 0 0 5.07 2.59A12.89 12.89 0 0 0 8.23 18a9.65 9.65 0 0 1-1.71-.83a3.39 3.39 0 0 0 .42-.33a11.66 11.66 0 0 0 10.12 0q.21.18.42.33a10.84 10.84 0 0 1-1.71.84a12.41 12.41 0 0 0 1.08 1.78a16.44 16.44 0 0 0 5.06-2.59a17.22 17.22 0 0 0-3-11.59a16.09 16.09 0 0 0-4.09-1.35zM8.68 14.81a1.94 1.94 0 0 1-1.8-2a1.93 1.93 0 0 1 1.8-2a1.93 1.93 0 0 1 1.8 2a1.93 1.93 0 0 1-1.8 2zm6.64 0a1.94 1.94 0 0 1-1.8-2a1.93 1.93 0 0 1 1.8-2a1.92 1.92 0 0 1 1.8 2a1.92 1.92 0 0 1-1.8 2z' fill='currentColor'/></svg>");
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
            // Username is not unique in Discord, in fact users can change easily.
            // Randomly generated 4 digit discriminator also changes whenever
            // the user changes their username. Only store user_id for lookups.
            obj.Remove("username");
            obj.MoveKey("avatar", AuthMetadataProvider.ProfileUrlKey, val =>
                "https://cdn.discordapp.com/avatars/" + obj["user_id"] + "/" + val + ".png");
            return obj;
        }
    }
}