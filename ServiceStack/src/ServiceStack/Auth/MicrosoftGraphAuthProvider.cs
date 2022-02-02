using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    /// <summary>
    ///   Create an Microsoft Graph App at: https://apps.dev.microsoft.com
    ///   The Apps Callback URL should match the CallbackUrl here.
    ///    
    ///   Microsoft Graph Info: https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-overview
    ///   Microsoft Graph Scopes: https://developer.microsoft.com/en-us/graph/docs/concepts/permissions_reference
    /// </summary>
    public class MicrosoftGraphAuthProvider : OAuth2Provider
    {
        public const string Name = "microsoftgraph";

        public const string Realm = DefaultUserProfileUrl;

        public const string DefaultUserProfileUrl = "https://graph.microsoft.com/v1.0/me";
        
        // Valid Sizes: 48x48, 64x64, 96x96, 120x120, 240x240, 360x360, 432x432, 504x504, and 648x648
        public static Func<string,string> DefaultPhotoUrl = size => size != null
            ? $"https://graph.microsoft.com/v1.0/me/photos/{size}/$value"
            : $"https://graph.microsoft.com/v1.0/me/photo/$value";
        
        public static Func<string,string> PhotoUrl { get; set; } = DefaultPhotoUrl;

        public bool SavePhoto { get; set; }
        
        public string SavePhotoSize { get; set; }

        private string tenant;
        public string Tenant
        {
            get => tenant;
            set
            {
                tenant = value;
                AuthorizeUrl = $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize";
                AccessTokenUrl = $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token";
            }
        }
        
        public string AppId
        {
            get => ConsumerKey;
            set => ConsumerKey = value;
        }

        public string AppSecret
        {
            get => ConsumerSecret;
            set => ConsumerSecret = value;
        }
        
        public MicrosoftGraphAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name)
        {
            this.Tenant = appSettings.Get($"oauth.{Name}.Tenant", "common");
            this.AuthorizeUrl = appSettings.Get($"oauth.{Name}.AuthorizeUrl", AuthorizeUrl);
            this.AccessTokenUrl = appSettings.Get($"oauth.{Name}.AccessTokenUrl", AccessTokenUrl);
            this.UserProfileUrl = appSettings.Get($"oauth.{Name}.UserProfileUrl", DefaultUserProfileUrl);

            this.AppId = appSettings.GetString($"oauth.{Name}.AppId");
            this.AppSecret = appSettings.GetString($"oauth.{Name}.AppSecret");
            this.SavePhoto = appSettings.Get($"oauth.{Name}.SavePhoto", false);
            this.SavePhotoSize = appSettings.GetString($"oauth.{Name}.SavePhotoSize");

            if (this.Scopes == null || this.Scopes.Length == 0)
            {
                this.Scopes = new[] {
                    "User.Read",
                    "openid",
                };
            }

            Icon = Svg.ImageSvg("<svg xmlns='http://www.w3.org/2000/svg' fill='currentColor' viewBox='0 0 20 20'><path d='M11.55 21H3v-8.55h8.55V21zM21 21h-8.55v-8.55H21V21zm-9.45-9.45H3V3h8.55v8.55zm9.45 0h-8.55V3H21v8.55z' fill='currentColor'/></svg>");
            NavItem = new NavItem {
                Href = "/auth/" + Name,
                Label = "Sign In with Microsoft",
                Id = "btn-" + Name,
                ClassName = "btn-social btn-microsoft",
                IconClass = "fab svg-microsoft",
            };
        }

        protected override async Task<string> GetAccessTokenJsonAsync(string code, AuthContext ctx, CancellationToken token = default)
        {
            var accessTokenParams = $"code={code}&client_id={ConsumerKey}&client_secret={ConsumerSecret}&redirect_uri={this.CallbackUrl.UrlEncode()}&grant_type=authorization_code";
            var contents = await AccessTokenUrlFilter(ctx, AccessTokenUrl)
                .SendStringToUrlAsync(method:HttpMethods.Post, requestBody:accessTokenParams, contentType: MimeTypes.FormUrlEncoded, token: token).ConfigAwait();
            return contents;
        }

        protected override async Task<Dictionary<string, string>> CreateAuthInfoAsync(string accessToken, CancellationToken token = default)
        {
            var url = this.UserProfileUrl;
            var json = await url.GetJsonFromUrlAsync(
                req => req.AddBearerToken(accessToken), token: token).ConfigAwait();
            var obj = JsonObject.Parse(json);

            obj.MoveKey("id", "user_id");
            obj.MoveKey("displayName","name");
            obj.MoveKey("givenName","first_name");
            obj.MoveKey("surname","last_name");
            obj.MoveKey("userPrincipalName","email");

            if (SavePhoto)
            {
                try
                {
                    var profileUrl = await AuthHttpGateway.CreateMicrosoftPhotoUrlAsync(accessToken, SavePhotoSize, token).ConfigAwait();
                    obj[AuthMetadataProvider.ProfileUrlKey] = profileUrl;
                }
                catch (Exception ex)
                {
                    Log.Warn($"Could not retrieve '{Name}' photo", ex);
                }
            }

            return obj;
        }

        public override void LoadUserOAuthProvider(IAuthSession authSession, IAuthTokens tokens)
        {
            if (!(authSession is AuthUserSession userSession))
                return;
            
            base.LoadUserOAuthProvider(authSession, tokens);
            
            // if the id_token has been returned populate any roles
            var idTokens  = JwtAuthProviderReader.ExtractPayload(tokens.Items["id_token"]);
            if(idTokens.ContainsKey("roles"))
            {
                authSession.Roles ??= new List<string>();
                var roles = (idTokens["roles"] as List<object>).ConvertTo<List<string>>();
                authSession.Roles.AddRange(roles);
            }
        }
    }
}