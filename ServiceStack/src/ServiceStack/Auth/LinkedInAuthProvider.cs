using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    /// <summary>
    /// Create new App at: https://www.linkedin.com/secure/developer
    /// </summary>
    public class LinkedInAuthProvider : OAuth2Provider
    {
        public const string Name = "linkedin";

        public const string Realm = DefaultAuthorizeUrl;

        public const string DefaultAuthorizeUrl = "https://www.linkedin.com/uas/oauth2/authorization";

        public const string DefaultAccessTokenUrl = "https://www.linkedin.com/uas/oauth2/accessToken";

        public const string DefaultUserProfileUrl = "https://api.linkedin.com/v1/people/~:(id,email-address,formatted-name,first-name,last-name,date-of-birth,public-profile-url,picture-url)";

        public LinkedInAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name)
        {
            this.AuthorizeUrl = appSettings.Get($"oauth.{Name}.AuthorizeUrl", DefaultAuthorizeUrl);
            this.AccessTokenUrl = appSettings.Get($"oauth.{Name}.AccessTokenUrl", DefaultAccessTokenUrl);

            //Fields available at: http://developer.linkedin.com/documents/profile-fields
            this.UserProfileUrl = appSettings.Get($"oauth.{Name}.UserProfileUrl", DefaultUserProfileUrl);

            if (this.Scopes == null || this.Scopes.Length == 0)
            {
                this.Scopes = new[] {
                    "r_emailaddress", 
                    "r_basicprofile"
                };
            }

            Icon = Svg.ImageSvg("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><g fill='none'><path fill-rule='evenodd' clip-rule='evenodd' d='M1 2.838A1.838 1.838 0 0 1 2.838 1H21.16A1.837 1.837 0 0 1 23 2.838V21.16A1.838 1.838 0 0 1 21.161 23H2.838A1.838 1.838 0 0 1 1 21.161V2.838zm8.708 6.55h2.979v1.496c.43-.86 1.53-1.634 3.183-1.634c3.169 0 3.92 1.713 3.92 4.856v5.822h-3.207v-5.106c0-1.79-.43-2.8-1.522-2.8c-1.515 0-2.145 1.089-2.145 2.8v5.106H9.708V9.388zm-5.5 10.403h3.208V9.25H4.208v10.54zM7.875 5.812a2.063 2.063 0 1 1-4.125 0a2.063 2.063 0 0 1 4.125 0z' fill='currentColor'/></g></svg>");
            NavItem = new NavItem {
                Href = "/auth/" + Name,
                Label = "Sign In with LinkedIn",
                Id = "btn-" + Name,
                ClassName = "btn-social btn-linkedin",
                IconClass = "fab svg-linkedin",
            };
        }

        protected override async Task<Dictionary<string, string>> CreateAuthInfoAsync(string accessToken, CancellationToken token = default)
        {
            var url = this.UserProfileUrl.AddQueryParam("oauth2_access_token", accessToken);
            var contents = await url.GetXmlFromUrlAsync(token: token).ConfigAwait();
            var xml = XDocument.Parse(contents);
            var el = xml.Root;
            
            var authInfo = new Dictionary<string, string>
            {
                { "user_id", el.GetString("id") }, 
                { "username", el.GetString("email-address") }, 
                { "email", el.GetString("email-address") }, 
                { "name", el.GetString("formatted-name") }, 
                { "first_name", el.GetString("first-name") }, 
                { "last_name", el.GetString("last-name") },
                { "birthday", el.GetString("date-of-birth") },
                { "link", el.GetString("public-profile-url") },
                { AuthMetadataProvider.ProfileUrlKey,  el.GetString("picture-url") },
            };

            return authInfo;
        }
        
    }
}