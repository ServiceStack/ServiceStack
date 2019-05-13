using System;
using System.Collections.Generic;
using ServiceStack.Configuration;
using ServiceStack.Text;
using ServiceStack.Text.Pools;

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
        
        public static string PhotoUrl { get; set; } = "https://graph.microsoft.com/beta/me/photo/$value";

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
                };
            }

            NavItem = new NavItem {
                Href = "/auth/" + Name,
                Label = "Sign in with Microsoft",
                Id = "btn" + Name,
                ClassName = "btn-social btn-microsoft",
                IconHtml = "<i class=\"fab fa-microsoft\"></i>",
            };
        }

        protected override string GetAccessTokenJson(string code)
        {
            var accessTokenParams = $"code={code}&client_id={ConsumerKey}&client_secret={ConsumerSecret}&redirect_uri={this.CallbackUrl.UrlEncode()}&grant_type=authorization_code";
            var contents = AccessTokenUrlFilter(this, AccessTokenUrl)
                .PostToUrl(accessTokenParams, requestFilter:req => req.ContentType = MimeTypes.FormUrlEncoded);
            return contents;
        }

        protected override Dictionary<string, string> CreateAuthInfo(string accessToken)
        {
            var url = this.UserProfileUrl;
            var json = url.GetJsonFromUrl(requestFilter:req => req.AddBearerToken(accessToken));
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
                    obj[AuthMetadataProvider.ProfileUrlKey] = AuthHttpGateway.CreateMicrosoftPhotoUrl(accessToken, SavePhotoSize);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Could not retrieve '{Name}' photo", ex);
                }
            }

            return obj;
        }
    }
}