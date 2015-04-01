using System;
using System.Collections.Generic;
using System.Net;
using ServiceStack.Configuration;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    /// <summary>
    ///   Create Yandex App at: https://oauth.yandex.ru/client/new
    ///   The Callback URL for your app should match the CallbackUrl provided.
    /// </summary>
    public class YandexAuthProvider : OAuthProvider
    {
        public const string Name = "yandex";
        public static string Realm = "https://oauth.yandex.ru/";
        public static string PreAuthUrl = "https://oauth.yandex.ru/authorize";
        public static string TokenUrl = "https://oauth.yandex.ru/token";

        static YandexAuthProvider()
        {
        }

        public YandexAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name, "AppId", "AppPassword")
        {
            ApplicationId = appSettings.GetString("oauth.Yandex.AppId");
            ApplicationPassword = appSettings.GetString("oauth.Yandex.AppPassword");
            AccessTokenUrl = TokenUrl;
        }

        public string ApplicationId { get; set; }

        public string ApplicationPassword { get; set; }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            IAuthTokens tokens = Init(authService, ref session, request);
            IRequest httpRequest = authService.Request;


            string error = httpRequest.QueryString["error"]
                           ?? httpRequest.QueryString["error_uri"]
                           ?? httpRequest.QueryString["error_description"];

            bool hasError = !error.IsNullOrEmpty();
            if (hasError)
            {
                Log.Error("Yandex error callback. {0}".Fmt(httpRequest.QueryString));
                return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", error)));
            }

            string code = httpRequest.QueryString["code"];
            bool isPreAuthCallback = !code.IsNullOrEmpty();
            if (!isPreAuthCallback)
            {
                string preAuthUrl = PreAuthUrl + "?response_type=code&client_id={0}&redirect_uri={1}&display=popup&state={2}".Fmt(ApplicationId, CallbackUrl.UrlEncode(), Guid.NewGuid().ToString("N"));

                authService.SaveSession(session, SessionExpiry);
                return authService.Redirect(PreAuthUrlFilter(this, preAuthUrl));
            }

            try
            {
                string payload = "grant_type=authorization_code&code={0}&client_id={1}&client_secret={2}".Fmt(code, ApplicationId, ApplicationPassword);
                string contents = AccessTokenUrl.PostStringToUrl(payload);

                var authInfo = JsonObject.Parse(contents);

                //Yandex does not throw exception, but returns error property in JSON response
                // http://api.yandex.ru/oauth/doc/dg/reference/obtain-access-token.xml
                string accessTokenError = authInfo.Get("error");

                if (!accessTokenError.IsNullOrEmpty())
                {
                    Log.Error("Yandex access_token error callback. {0}".Fmt(authInfo.ToString()));
                    return authService.Redirect(session.ReferrerUrl.SetParam("f", "AccessTokenFailed"));
                }
                tokens.AccessTokenSecret = authInfo.Get("access_token");

                session.IsAuthenticated = true;

                return OnAuthenticated(authService, session, tokens, authInfo.ToDictionary())
                    ?? authService.Redirect(SuccessRedirectUrlFilter(this, session.ReferrerUrl.SetParam("s", "1")));
            }
            catch (WebException webException)
            {
                //just in case Yandex will start throwing exceptions 
                HttpStatusCode statusCode = ((HttpWebResponse)webException.Response).StatusCode;
                if (statusCode == HttpStatusCode.BadRequest)
                {
                    return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", "AccessTokenFailed")));
                }
            }
            return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", "Unknown")));
        }

        protected override void LoadUserAuthInfo(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            try
            {
                string json = "https://login.yandex.ru/info?format=json&oauth_token={0}".Fmt(tokens.AccessTokenSecret).GetJsonFromUrl();
                JsonObject obj = JsonObject.Parse(json);

                tokens.UserId = obj.Get("id");
                tokens.UserName = obj.Get("display_name");
                tokens.DisplayName = obj.Get("real_name");
                tokens.FirstName = obj.Get("first_name");
                tokens.LastName = obj.Get("last_name");
                tokens.Email = obj.Get("default_email");
                tokens.BirthDateRaw = obj.Get("birthday");

                LoadUserOAuthProvider(userSession, tokens);
            }
            catch (Exception ex)
            {
                Log.Error("Could not retrieve Yandex user info for '{0}'".Fmt(tokens.DisplayName), ex);
            }
        }

        public override void LoadUserOAuthProvider(IAuthSession authSession, IAuthTokens tokens)
        {
            var userSession = authSession as AuthUserSession;
            if (userSession == null) return;

            userSession.UserName = tokens.UserName ?? userSession.UserName;
            userSession.DisplayName = tokens.DisplayName ?? userSession.DisplayName;
            userSession.FirstName = tokens.FirstName ?? userSession.DisplayName;
            userSession.LastName = tokens.LastName ?? userSession.LastName;
            userSession.BirthDateRaw = tokens.BirthDateRaw ?? userSession.BirthDateRaw;
            userSession.PrimaryEmail = tokens.Email ?? userSession.PrimaryEmail ?? userSession.Email;
            userSession.Email = tokens.Email ?? userSession.PrimaryEmail ?? userSession.Email;
        }
    }
}
