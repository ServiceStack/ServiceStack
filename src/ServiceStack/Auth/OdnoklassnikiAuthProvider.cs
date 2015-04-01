using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
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
        }

        public string ApplicationId { get; set; }
        public string PublicKey { get; set; }
        public string SecretKey { get; set; }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            IAuthTokens tokens = Init(authService, ref session, request);
            IRequest httpRequest = authService.Request;


            string error = httpRequest.QueryString["error"];

            bool hasError = !error.IsNullOrEmpty();
            if (hasError)
            {
                Log.Error("Odnoklassniki error callback. {0}".Fmt(httpRequest.QueryString));
                return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", error)));
            }

            string code = httpRequest.QueryString["code"];
            bool isPreAuthCallback = !code.IsNullOrEmpty();
            if (!isPreAuthCallback)
            {
                string preAuthUrl = PreAuthUrl + "?client_id={0}&redirect_uri={1}&response_type=code&layout=m"
                  .Fmt(ApplicationId, CallbackUrl.UrlEncode());

                authService.SaveSession(session, SessionExpiry);
                return authService.Redirect(PreAuthUrlFilter(this, preAuthUrl));
            }

            try
            {
                string payload = "client_id={0}&client_secret={1}&code={2}&redirect_uri={3}&grant_type=authorization_code"
                  .Fmt(ApplicationId, SecretKey, code, CallbackUrl.UrlEncode());

                string contents = AccessTokenUrlFilter(this, AccessTokenUrl).PostToUrl(payload, "*/*", RequestFilter);

                var authInfo = JsonObject.Parse(contents);

                //ok.ru does not throw exception, but returns error property in JSON response
                string accessTokenError = authInfo.Get("error");

                if (!accessTokenError.IsNullOrEmpty())
                {
                    Log.Error("Odnoklassniki access_token error callback. {0}".Fmt(authInfo.ToString()));
                    return authService.Redirect(session.ReferrerUrl.SetParam("f", "AccessTokenFailed"));
                }
                tokens.AccessTokenSecret = authInfo.Get("access_token");
                tokens.UserId = authInfo.Get("user_id");

                session.IsAuthenticated = true;

                return OnAuthenticated(authService, session, tokens, authInfo.ToDictionary())
                    ?? authService.Redirect(SuccessRedirectUrlFilter(this, session.ReferrerUrl.SetParam("s", "1")));
            }
            catch (WebException webException)
            {
                //just in case it starts throwing exceptions 
                HttpStatusCode statusCode = ((HttpWebResponse)webException.Response).StatusCode;
                if (statusCode == HttpStatusCode.BadRequest)
                {
                    return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", "AccessTokenFailed")));
                }
            }
            return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", "Unknown")));
        }

        protected virtual void RequestFilter(HttpWebRequest request)
        {
            request.UserAgent = ServiceClientBase.DefaultUserAgent;
        }

        protected override void LoadUserAuthInfo(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            try
            {
                //sig = md5( request_params_composed_string + md5(access_token + application_secret_key)  )

                string innerSignature = Encoding.UTF8.GetBytes(tokens.AccessTokenSecret + ConsumerSecret).ToMd5Hash();
                string signature = Encoding.UTF8.GetBytes("application_key={0}".Fmt(PublicKey) + innerSignature).ToMd5Hash();

                string payload = "access_token={0}&sig={1}&application_key={2}".Fmt(tokens.AccessTokenSecret, signature, PublicKey);

                string json = "http://api.odnoklassniki.ru/api/users/getCurrentUser".PostToUrl(payload, "*/*", RequestFilter);

                JsonObject obj = JsonObject.Parse(json);

                if (!obj.Get("error").IsNullOrEmpty())
                {
                    Log.Error("Could not retrieve Odnoklassniki user info for '{0}', Response:{1}".Fmt(tokens.DisplayName, json));
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
                Log.Error("Could not retrieve Odnoklassniki user info for '{0}'".Fmt(tokens.DisplayName), ex);
            }

            LoadUserOAuthProvider(userSession, tokens);
        }

        public override void LoadUserOAuthProvider(IAuthSession authSession, IAuthTokens tokens)
        {
            var userSession = authSession as AuthUserSession;
            if (userSession == null) return;

            userSession.DisplayName = tokens.DisplayName ?? userSession.DisplayName;
            userSession.FirstName = tokens.FirstName ?? userSession.DisplayName;
            userSession.LastName = tokens.LastName ?? userSession.LastName;
            userSession.BirthDateRaw = tokens.BirthDateRaw ?? userSession.BirthDateRaw;
            userSession.Language = tokens.Language ?? userSession.Language;
            userSession.Country = tokens.Country ?? userSession.Country;
            userSession.City = tokens.City ?? userSession.City;
            userSession.Gender = tokens.Gender ?? userSession.Gender;
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

