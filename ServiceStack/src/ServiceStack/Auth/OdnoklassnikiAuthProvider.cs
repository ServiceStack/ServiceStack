using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    /// <summary>
    ///   Create Odnoklassniki App at: http://www.odnoklassniki.ru/devaccess
    ///   The Callback URL for your app should match the CallbackUrl provided.
    ///   
    ///   NB: They claim they use OAuth 2.0, but they in fact don't. 
    ///   http://apiok.ru/wiki/display/api/Authorization+OAuth+2.0
    /// </summary>
    public class OdnoklassnikiAuthProvider : OAuthProvider
    {
        public const string Name = "odnoklassniki";
        public static string Realm = "http://www.odnoklassniki.ru/oauth/";
        public static string PreAuthUrl = "http://www.odnoklassniki.ru/oauth/authorize";
        public static string TokenUrl = "http://api.odnoklassniki.ru/oauth/token.do";

        static OdnoklassnikiAuthProvider() {}

        public OdnoklassnikiAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name, "ApplicationId", "SecretKey")
        {
            ApplicationId = appSettings.GetString("oauth.Odnoklassniki.ApplicationId");
            PublicKey = appSettings.GetString("oauth.Odnoklassniki.PublicKey");
            SecretKey = appSettings.GetString("oauth.Odnoklassniki.SecretKey");
            AccessTokenUrl = TokenUrl;

            NavItem = new NavItem {
                Href = "/auth/" + Name,
                Label = "Sign In with Odnoklassniki",
                Id = "btn-" + Name,
                ClassName = "btn-social btn-odnoklassniki",
                IconClass = "fab svg-odnoklassniki",
            };
        }

        public string ApplicationId { get; set; }
        public string PublicKey { get; set; }
        public string SecretKey { get; set; }

        public override async Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token = default)
        {
            IAuthTokens tokens = Init(authService, ref session, request);
            var ctx = CreateAuthContext(authService, session, tokens);
            IRequest httpRequest = authService.Request;


            string error = httpRequest.QueryString["error"];

            bool hasError = !error.IsNullOrEmpty();
            if (hasError)
            {
                Log.Error($"Odnoklassniki error callback. {httpRequest.QueryString}");
                return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", error)));
            }

            string code = httpRequest.QueryString["code"];
            bool isPreAuthCallback = !code.IsNullOrEmpty();
            if (!isPreAuthCallback)
            {
                string preAuthUrl = $"{PreAuthUrl}?client_id={ApplicationId}&redirect_uri={CallbackUrl.UrlEncode()}&response_type=code&layout=m";

                await this.SaveSessionAsync(authService, session, SessionExpiry, token).ConfigAwait();
                return authService.Redirect(PreAuthUrlFilter(ctx, preAuthUrl));
            }

            try
            {
                string payload = $"client_id={ApplicationId}&client_secret={SecretKey}&code={code}&redirect_uri={CallbackUrl.UrlEncode()}&grant_type=authorization_code";

                string contents = await AccessTokenUrlFilter(ctx, AccessTokenUrl)
                    .PostToUrlAsync(formData:payload, requestFilter:req => 
                        req.With(c => {
                            c.UserAgent = ServiceClientBase.DefaultUserAgent;
                            c.Accept = "*/*";
                        }), 
                        token: token).ConfigAwait();

                var authInfo = JsonObject.Parse(contents);

                //ok.ru does not throw exception, but returns error property in JSON response
                string accessTokenError = authInfo.Get("error");

                if (!accessTokenError.IsNullOrEmpty())
                {
                    Log.Error($"Odnoklassniki access_token error callback. {authInfo}");
                    return authService.Redirect(session.ReferrerUrl.SetParam("f", "AccessTokenFailed"));
                }
                tokens.AccessTokenSecret = authInfo.Get("access_token");
                tokens.UserId = authInfo.Get("user_id");

                session.IsAuthenticated = true;

                return await OnAuthenticatedAsync(authService, session, tokens, authInfo.ToDictionary(), token).ConfigAwait()
                    ?? await authService.Redirect(SuccessRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("s", "1"))).SuccessAuthResultAsync(authService,session).ConfigAwait();
            }
            catch (Exception ex)
            {
                //just in case it starts throwing exceptions 
                var statusCode = ex.GetStatus();
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
                //sig = md5( request_params_composed_string + md5(access_token + application_secret_key)  )
                string innerSignature = Encoding.UTF8.GetBytes(tokens.AccessTokenSecret + ConsumerSecret).ToMd5Hash();
                string signature = Encoding.UTF8.GetBytes($"application_key={PublicKey}" + innerSignature).ToMd5Hash();

                string payload = $"access_token={tokens.AccessTokenSecret}&sig={signature}&application_key={PublicKey}";

                string json = await "http://api.odnoklassniki.ru/api/users/getCurrentUser"
                    .PostToUrlAsync(formData:payload, requestFilter:req => 
                            req.With(c => {
                                c.UserAgent = ServiceClientBase.DefaultUserAgent;
                                c.Accept = "*/*";
                            }), 
                        token: token).ConfigAwait();

                JsonObject obj = JsonObject.Parse(json);

                if (!obj.Get("error").IsNullOrEmpty())
                {
                    Log.Error($"Could not retrieve Odnoklassniki user info for '{tokens.DisplayName}', Response:{json}");
                    return;
                }

                //response fields info: http://apiok.ru/wiki/display/api/users.getCurrentUser+ru
                var location = JsonObject.Parse(obj.GetUnescaped("location"));

                tokens.UserId = obj.Get("uid");
                tokens.DisplayName = obj.Get("name");
                tokens.FirstName = obj.Get("first_name");
                tokens.LastName = obj.Get("last_name");
                tokens.BirthDateRaw = obj.Get("birthday");
                tokens.Language = obj.Get("locale");
                tokens.Country = location.Get("countryCode");
                tokens.City = location.Get("city");
                tokens.Gender = obj.Get("gender");

                if (SaveExtendedUserInfo)
                {
                    obj.Each(x => authInfo[x.Key] = x.Value);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Could not retrieve Odnoklassniki user info for '{tokens.DisplayName}'", ex);
            }

            await LoadUserOAuthProviderAsync(userSession, tokens).ConfigAwait();
        }

        public override Task LoadUserOAuthProviderAsync(IAuthSession authSession, IAuthTokens tokens)
        {
            if (authSession is AuthUserSession userSession)
            {
                userSession.DisplayName = tokens.DisplayName ?? userSession.DisplayName;
                userSession.FirstName = tokens.FirstName ?? userSession.DisplayName;
                userSession.LastName = tokens.LastName ?? userSession.LastName;
                userSession.BirthDateRaw = tokens.BirthDateRaw ?? userSession.BirthDateRaw;
                userSession.Language = tokens.Language ?? userSession.Language;
                userSession.Country = tokens.Country ?? userSession.Country;
                userSession.City = tokens.City ?? userSession.City;
                userSession.Gender = tokens.Gender ?? userSession.Gender;
            }
            return Task.CompletedTask;
        }
    }

    #region Test APP Registration Information Email
    /*
  From: "odnoklassniki.ru" <bezotveta@odnoklassniki.ru>
  To: rouslan@gmail.com
  Message-ID: <1462956089.11400671017663.JavaMail.root@srvk849.odnoklassniki.ru>
  Subject: =?KOI8-R?B?T2Rub2tsYXNzbmlraS5ydTog98HbxSDQ0g==?=
   =?KOI8-R?B?yczP1sXOycUg2sHSxcfJ09TSydLP18HOzw==?=
   
  Ваше приложение ServiceStack Test Login успешно зарегистрировано на Odnoklassniki.ru.

  Application ID: 1089990656.
  Публичный ключ приложения: CBABPIPBEBABABABA.
  Секретный ключ приложения:  B268138DF0BE3919FB1BBED8.
  Ссылка на приложение: http://www.odnoklassniki.ru/games/servicestacktestlogin
  Этот ключ необходим для изменения настроек приложения и для подписи/верификации запросов от Вашего приложения. Более подробная информация содержится в документации к програмному интерфейсу сайта Odnoklassniki.ru.
  --
  С уважением,
  Служба поддержки Odnoklassniki.ru
 
*/
    #endregion
}

