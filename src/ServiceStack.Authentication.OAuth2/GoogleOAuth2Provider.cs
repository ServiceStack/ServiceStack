using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Authentication.OAuth2
{
    /*
        Create an OAuth2 App at: https://code.google.com/apis/console/
        The Apps Callback URL should match the CallbackUrl here.
     
        Google OAuth2 info: https://developers.google.com/accounts/docs/OAuth2Login
        Google OAuth2 Scopes from: https://www.googleapis.com/discovery/v1/apis/oauth2/v2/rest?fields=auth(oauth2(scopes))
            https://www.googleapis.com/auth/plus.login: Know your name, basic info, and list of people you're connected to on Google+
            https://www.googleapis.com/auth/plus.me Know who you are on Google+
            https://www.googleapis.com/auth/userinfo.email View your email address
            https://www.googleapis.com/auth/userinfo.profile View basic information about your account
     */
    [Obsolete("Use built-in GoogleAuthProvider in ServiceStack.Auth")]
    public class GoogleOAuth2Provider : OAuth2Provider
    {
        public const string Name = "GoogleOAuth";

        public const string Realm = "https://accounts.google.com/o/oauth2/v2/auth";

        public GoogleOAuth2Provider(IAppSettings appSettings)
            : base(appSettings, Realm, Name)
        {
            this.AuthorizeUrl ??= Realm;
            this.AccessTokenUrl ??= "https://oauth2.googleapis.com/token";
            this.UserProfileUrl ??= "https://www.googleapis.com/oauth2/v2/userinfo";

            if (this.Scopes.Length == 0)
            {
                this.Scopes = new[] {
                    "https://www.googleapis.com/auth/userinfo.profile",
                    "https://www.googleapis.com/auth/userinfo.email"
                };
            }

            this.VerifyAccessToken = OnVerifyAccessToken;
        }

        public string VerifyAccessTokenUrl { get; set; } = "https://www.googleapis.com/oauth2/v2/tokeninfo?access_token={0}";

        public override async Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token=default)
        {
            var httpRequest = authService.Request;
            var code = httpRequest.QueryString[Keywords.Code];
            if (code == null)
                return await base.AuthenticateAsync(authService, session, request, token).ConfigAwait();

            var tokens = Init(authService, ref session, request);

            try
            {
                var accessTokenUrl = $"{AccessTokenUrl}?code={code}&client_id={ConsumerKey}&client_secret={ConsumerSecret}&redirect_uri={this.CallbackUrl.UrlEncode()}&grant_type=authorization_code";
                var contents = await AccessTokenUrlFilter(this, accessTokenUrl).PostToUrlAsync("").ConfigAwait();
                var authInfo = JsonObject.Parse(contents);

                var accessToken = authInfo["access_token"];

                return await AuthenticateWithAccessTokenAsync(authService, session, tokens, accessToken, token).ConfigAwait()
                       ?? authService.Redirect(SuccessRedirectUrlFilter(this, session.ReferrerUrl.SetParam("s", "1"))); //Haz Access!
            }
            catch (WebException we)
            {
                string errorBody = await we.GetResponseBodyAsync(token);
                var statusCode = ((HttpWebResponse)we.Response).StatusCode;
                if (statusCode == HttpStatusCode.BadRequest)
                {
                    return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", "AccessTokenFailed")));
                }
            }

            //Shouldn't get here
            return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", "Unknown")));
        }

        public bool OnVerifyAccessToken(string accessToken)
        {
            var url = VerifyAccessTokenUrl.Fmt(accessToken);
            var json = url.GetJsonFromUrl();
            var obj = JsonObject.Parse(json);
            var issuedTo = obj["issued_to"];
            return issuedTo == ConsumerKey;
        }

        protected override Dictionary<string, string> CreateAuthInfo(string accessToken)
        {
            var url = this.UserProfileUrl.AddQueryParam("access_token", accessToken);
            string json = url.GetJsonFromUrl();
            var obj = JsonObject.Parse(json);
            var authInfo = new Dictionary<string, string>
            {
                { "user_id", obj["id"] }, 
                { "username", obj["email"] }, 
                { "email", obj["email"] }, 
                { "name", obj["name"] }, 
                { "first_name", obj["given_name"] }, 
                { "last_name", obj["family_name"] },
                { "gender", obj["gender"] },
                { "birthday", obj["birthday"] },
                { "link", obj["link"] },
                { "picture", obj["picture"] },
                { "locale", obj["locale"] },
                { AuthMetadataProvider.ProfileUrlKey, obj["picture"] },
            };
            return authInfo;
        }
    }
}