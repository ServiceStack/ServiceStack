using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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

            NavItem = new NavItem {
                Href = "/auth/" + Name,
                Label = "Sign In with Yandex",
                Id = "btn-" + Name,
                ClassName = "btn-social btn-yandex",
                IconClass = "fab svg-yandex",
            };
        }

        public string ApplicationId { get; set; }

        public string ApplicationPassword { get; set; }

        public override async Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token = default)
        {
            IAuthTokens tokens = Init(authService, ref session, request);
            var ctx = CreateAuthContext(authService, session, tokens);
            IRequest httpRequest = authService.Request;

            string error = httpRequest.QueryString["error"]
                           ?? httpRequest.QueryString["error_uri"]
                           ?? httpRequest.QueryString["error_description"];

            bool hasError = !error.IsNullOrEmpty();
            if (hasError)
            {
                Log.Error($"Yandex error callback. {httpRequest.QueryString}");
                return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", error)));
            }

            string code = httpRequest.QueryString["code"];
            bool isPreAuthCallback = !code.IsNullOrEmpty();
            if (!isPreAuthCallback)
            {
                string preAuthUrl = $"{PreAuthUrl}?response_type=code&client_id={ApplicationId}&redirect_uri={CallbackUrl.UrlEncode()}&display=popup&state={Guid.NewGuid().ToString("N")}";
                await this.SaveSessionAsync(authService, session, SessionExpiry, token).ConfigAwait();
                return authService.Redirect(PreAuthUrlFilter(ctx, preAuthUrl));
            }

            try
            {
                string payload = $"grant_type=authorization_code&code={code}&client_id={ApplicationId}&client_secret={ApplicationPassword}";
                string contents = await AccessTokenUrl.PostStringToUrlAsync(payload).ConfigAwait();

                var authInfo = JsonObject.Parse(contents);

                //Yandex does not throw exception, but returns error property in JSON response
                // http://api.yandex.ru/oauth/doc/dg/reference/obtain-access-token.xml
                string accessTokenError = authInfo.Get("error");

                if (!accessTokenError.IsNullOrEmpty())
                {
                    Log.Error($"Yandex access_token error callback. {authInfo}");
                    return authService.Redirect(session.ReferrerUrl.SetParam("f", "AccessTokenFailed"));
                }
                tokens.AccessTokenSecret = authInfo.Get("access_token");

                session.IsAuthenticated = true;

                return await OnAuthenticatedAsync(authService, session, tokens, authInfo.ToDictionary(), token).ConfigAwait()
                    ?? await authService.Redirect(SuccessRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("s", "1"))).SuccessAuthResultAsync(authService,session).ConfigAwait();
            }
            catch (WebException webException)
            {
                //just in case Yandex will start throwing exceptions 
                var statusCode = ((HttpWebResponse)webException.Response).StatusCode;
                if (statusCode == HttpStatusCode.BadRequest)
                {
                    return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", "AccessTokenFailed")));
                }
            }
            return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", "Unknown")));
        }

        protected override async Task LoadUserAuthInfoAsync(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo, CancellationToken token = default)
        {
            try
            {
                string json = await $"https://login.yandex.ru/info?format=json&oauth_token={tokens.AccessTokenSecret}".GetJsonFromUrlAsync().ConfigAwait();
                JsonObject obj = JsonObject.Parse(json);

                tokens.UserId = obj.Get("id");
                tokens.UserName = obj.Get("display_name");
                tokens.DisplayName = obj.Get("real_name");
                tokens.FirstName = obj.Get("first_name");
                tokens.LastName = obj.Get("last_name");
                tokens.Email = obj.Get("default_email");
                tokens.BirthDateRaw = obj.Get("birthday");
                userSession.UserAuthName = tokens.Email;

                LoadUserOAuthProvider(userSession, tokens);
            }
            catch (Exception ex)
            {
                Log.Error($"Could not retrieve Yandex user info for '{tokens.DisplayName}'", ex);
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
