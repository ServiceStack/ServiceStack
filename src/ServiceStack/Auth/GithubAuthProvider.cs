using System;
using System.Collections.Generic;
using System.Net;
using System.Web;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    /// <summary>
    ///   Create an App at: https://github.com/settings/applications/new
    ///   The Callback URL for your app should match the CallbackUrl provided.
    /// </summary>
    public class GithubAuthProvider : OAuthProvider
    {
        public const string Name = "github";
        public static string Realm = "https://github.com/login/";
        public static string PreAuthUrl = "https://github.com/login/oauth/authorize";

        static GithubAuthProvider()
        {
        }

        public GithubAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name, "ClientId", "ClientSecret")
        {
            ClientId = appSettings.GetString("oauth.github.ClientId");
            ClientSecret = appSettings.GetString("oauth.github.ClientSecret");
            Scopes = appSettings.Get("oauth.github.Scopes", new[] { "user" });
        }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string[] Scopes { get; set; }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            var tokens = Init(authService, ref session, request);
            var httpRequest = authService.Request;

            //https://developer.github.com/v3/oauth/#common-errors-for-the-authorization-request
            var error = httpRequest.QueryString["error"]
                ?? httpRequest.QueryString["error_uri"]
                ?? httpRequest.QueryString["error_description"];

            var hasError = !error.IsNullOrEmpty();
            if (hasError)
            {
                Log.Error("GitHub error callback. {0}".Fmt(httpRequest.QueryString));
                return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", error)));
            }

            var code = httpRequest.QueryString["code"];
            var isPreAuthCallback = !code.IsNullOrEmpty();
            if (!isPreAuthCallback)
            {
                string preAuthUrl = PreAuthUrl + "?client_id={0}&redirect_uri={1}&scope={2}&state={3}"
                  .Fmt(ClientId, CallbackUrl.UrlEncode(), Scopes.Join(","), Guid.NewGuid().ToString("N"));

                authService.SaveSession(session, SessionExpiry);
                return authService.Redirect(PreAuthUrlFilter(this, preAuthUrl));
            }

            string accessTokenUrl = AccessTokenUrl + "?client_id={0}&redirect_uri={1}&client_secret={2}&code={3}"
              .Fmt(ClientId, CallbackUrl.UrlEncode(), ClientSecret, code);

            try
            {
                var contents = AccessTokenUrlFilter(this, accessTokenUrl).GetStringFromUrl();
                var authInfo = HttpUtility.ParseQueryString(contents);

                //GitHub does not throw exception, but just return error with descriptions
                //https://developer.github.com/v3/oauth/#common-errors-for-the-access-token-request
                var accessTokenError = authInfo["error"]
                    ?? authInfo["error_uri"]
                    ?? authInfo["error_description"];

                if (!accessTokenError.IsNullOrEmpty())
                {
                    Log.Error("GitHub access_token error callback. {0}".Fmt(authInfo.ToString()));
                    return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", "AccessTokenFailed")));
                }
                tokens.AccessTokenSecret = authInfo["access_token"];

                session.IsAuthenticated = true;
                
                return OnAuthenticated(authService, session, tokens, authInfo.ToDictionary())
                    ?? authService.Redirect(SuccessRedirectUrlFilter(this, session.ReferrerUrl.SetParam("s", "1"))); //Haz Access!
            }
            catch (WebException webException)
            {
                //just in case GitHub will start throwing exceptions 
                var statusCode = ((HttpWebResponse)webException.Response).StatusCode;
                if (statusCode == HttpStatusCode.BadRequest)
                {
                    return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", "AccessTokenFailed")));
                }
            }
            return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", "Unknown")));

        }

        /// <summary>
        ///   Calling to Github API without defined Useragent throws
        ///   exception "The server committed a protocol violation. Section=ResponseStatusLine"
        /// </summary>
        protected virtual void UserRequestFilter(HttpWebRequest request)
        {
            request.UserAgent = ServiceClientBase.DefaultUserAgent;
        }

        protected override void LoadUserAuthInfo(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            try
            {
                var json = "https://api.github.com/user?access_token={0}".Fmt(tokens.AccessTokenSecret)
                  .GetStringFromUrl("*/*", UserRequestFilter);
                var obj = JsonObject.Parse(json);
                tokens.UserId = obj.Get("id");
                tokens.UserName = obj.Get("login");
                tokens.DisplayName = obj.Get("name");
                tokens.Email = obj.Get("email");
                tokens.Company = obj.Get("company");
                tokens.Country = obj.Get("country");

                if (SaveExtendedUserInfo)
                {
                    obj.Each(x => authInfo[x.Key] = x.Value);
                }

                string profileUrl;
                if (obj.TryGetValue("avatar_url", out profileUrl))
                    tokens.Items[AuthMetadataProvider.ProfileUrlKey] = profileUrl;
            }
            catch (Exception ex)
            {
                Log.Error("Could not retrieve github user info for '{0}'".Fmt(tokens.DisplayName), ex);
            }

            LoadUserOAuthProvider(userSession, tokens);
        }

        public override void LoadUserOAuthProvider(IAuthSession authSession, IAuthTokens tokens)
        {
            var userSession = authSession as AuthUserSession;
            if (userSession == null) return;

            userSession.UserName = tokens.UserName ?? userSession.UserName;
            userSession.DisplayName = tokens.DisplayName ?? userSession.DisplayName;
            userSession.Company = tokens.Company ?? userSession.Company;
            userSession.Country = tokens.Country ?? userSession.Country;
            userSession.PrimaryEmail = tokens.Email ?? userSession.PrimaryEmail ?? userSession.Email;
            userSession.Email = tokens.Email ?? userSession.PrimaryEmail ?? userSession.Email;
        }
    }
}
