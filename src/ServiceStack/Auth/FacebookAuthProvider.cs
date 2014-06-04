using System;
using System.Net;
using System.Web;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    /// <summary>
    /// Create a Facebook App at: https://developers.facebook.com/apps
    /// The Callback URL for your app should match the CallbackUrl provided.
    /// </summary>
    public class FacebookAuthProvider : OAuthProvider
    {
        public const string Name = "facebook";
        public static string Realm = "https://graph.facebook.com/";
        public static string PreAuthUrl = "https://www.facebook.com/dialog/oauth";

        public string AppId { get; set; }
        public string AppSecret { get; set; }
        public string[] Permissions { get; set; }

        public FacebookAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name, "AppId", "AppSecret")
        {
            this.AppId = appSettings.GetString("oauth.facebook.AppId");
            this.AppSecret = appSettings.GetString("oauth.facebook.AppSecret");
            this.Permissions = appSettings.Get("oauth.facebook.Permissions", new string[0]);
        }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            var tokens = Init(authService, ref session, request);
            var httpRequest = authService.Request;

            var error = httpRequest.QueryString["error_reason"]
                ?? httpRequest.QueryString["error"]
                ?? httpRequest.QueryString["error_code"]
                ?? httpRequest.QueryString["error_description"];

            var hasError = !error.IsNullOrEmpty();
            if (hasError)
            {
                Log.Error("Facebook error callback. {0}".Fmt(httpRequest.QueryString));
                return authService.Redirect(session.ReferrerUrl);
            }             
        
            var code = httpRequest.QueryString["code"];
            var isPreAuthCallback = !code.IsNullOrEmpty();
            if (!isPreAuthCallback)
            {
                var preAuthUrl = PreAuthUrl + "?client_id={0}&redirect_uri={1}&scope={2}"
                    .Fmt(AppId, this.CallbackUrl.UrlEncode(), string.Join(",", Permissions));

                authService.SaveSession(session, SessionExpiry);
                return authService.Redirect(preAuthUrl);
            }

            var accessTokenUrl = this.AccessTokenUrl + "?client_id={0}&redirect_uri={1}&client_secret={2}&code={3}"
                .Fmt(AppId, this.CallbackUrl.UrlEncode(), AppSecret, code);

            try
            {
                var contents = accessTokenUrl.GetStringFromUrl();
                var authInfo = HttpUtility.ParseQueryString(contents);
                tokens.AccessTokenSecret = authInfo["access_token"];

                session.IsAuthenticated = true;
                
                return OnAuthenticated(authService, session, tokens, authInfo.ToDictionary())
                    ?? authService.Redirect(session.ReferrerUrl.AddHashParam("s", "1")); //Haz access!
            }
            catch (WebException we)
            {
                var statusCode = ((HttpWebResponse)we.Response).StatusCode;
                if (statusCode == HttpStatusCode.BadRequest)
                {
                    return authService.Redirect(session.ReferrerUrl.AddHashParam("f", "AccessTokenFailed"));
                }
            }

            //Shouldn't get here
            return authService.Redirect(session.ReferrerUrl.AddHashParam("f", "Unknown"));
        }

        protected override void LoadUserAuthInfo(AuthUserSession userSession, IAuthTokens tokens, System.Collections.Generic.Dictionary<string, string> authInfo)
        {
            try
            {
                var json = AuthHttpGateway.DownloadFacebookUserInfo(tokens.AccessTokenSecret);
                var obj = JsonObject.Parse(json);
                tokens.UserId = obj.Get("id");
                tokens.UserName = obj.Get("username");
                tokens.DisplayName = obj.Get("name");
                tokens.FirstName = obj.Get("first_name");
                tokens.LastName = obj.Get("last_name");
                tokens.Email = obj.Get("email");

                LoadUserOAuthProvider(userSession, tokens);
            }
            catch (Exception ex)
            {
                Log.Error("Could not retrieve facebook user info for '{0}'".Fmt(tokens.DisplayName), ex);
            }
        }

        public override void LoadUserOAuthProvider(IAuthSession authSession, IAuthTokens tokens)
        {
            var userSession = authSession as AuthUserSession;
            if (userSession == null) return;

            userSession.FacebookUserId = tokens.UserId ?? userSession.FacebookUserId;
            userSession.FacebookUserName = tokens.UserName ?? userSession.FacebookUserName;
            userSession.DisplayName = tokens.DisplayName ?? userSession.DisplayName;
            userSession.FirstName = tokens.FirstName ?? userSession.FirstName;
            userSession.LastName = tokens.LastName ?? userSession.LastName;
            userSession.PrimaryEmail = tokens.Email ?? userSession.PrimaryEmail ?? userSession.Email;
        }
    }
}