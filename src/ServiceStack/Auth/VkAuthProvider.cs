using System;
using System.Collections.Generic;
using System.Net;
using ServiceStack.Configuration;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    /// <summary>
    ///   Create VK App at: http://vk.com/editapp?act=create
    ///   The Callback URL for your app should match the CallbackUrl provided.
    /// </summary>
    public class VkAuthProvider : OAuthProvider
    {
        public const string Name = "vkcom";
        public static string Realm = "https://oauth.VK.ru/";
        public static string PreAuthUrl = "https://oauth.vk.com/authorize";
        public static string TokenUrl = "https://oauth.vk.com/access_token";

        static VkAuthProvider()
        {
        }

        public VkAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name, "ApplicationId", "SecureKey")
        {
            ApplicationId = appSettings.GetString("oauth.vkcom.ApplicationId");
            SecureKey = appSettings.GetString("oauth.vkcom.SecureKey");
            Scope = appSettings.GetString("oauth.vkcom.Scope");
            ApiVersion = appSettings.GetString("oauth.vkcom.ApiVersion");

            AccessTokenUrl = TokenUrl;
        }

        public string ApplicationId { get; set; }

        public string SecureKey { get; set; }
        public string Scope { get; set; }
        public string ApiVersion { get; set; }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            IAuthTokens tokens = Init(authService, ref session, request);
            IRequest httpRequest = authService.Request;

            string error = httpRequest.QueryString["error_reason"]
                           ?? httpRequest.QueryString["error_description"]
                           ?? httpRequest.QueryString["error"];

            bool hasError = !error.IsNullOrEmpty();
            if (hasError)
            {
                Log.Error("VK error callback. {0}".Fmt(httpRequest.QueryString));
                return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", error)));
            }

            string code = httpRequest.QueryString["code"];
            bool isPreAuthCallback = !code.IsNullOrEmpty();
            if (!isPreAuthCallback)
            {
                string preAuthUrl = PreAuthUrl + "?client_id={0}&scope={1}&redirect_uri={2}&response_type=code&v={3}"
                  .Fmt(ApplicationId, Scope, CallbackUrl.UrlEncode(), ApiVersion);

                authService.SaveSession(session, SessionExpiry);
                return authService.Redirect(PreAuthUrlFilter(this, preAuthUrl));
            }

            try
            {
                code = EnsureLatestCode(code);

                string accessTokeUrl = AccessTokenUrl + "?client_id={0}&client_secret={1}&code={2}&redirect_uri={3}"
                  .Fmt(ApplicationId, SecureKey, code, CallbackUrl.UrlEncode());

                string contents = AccessTokenUrlFilter(this, accessTokeUrl).GetStringFromUrl("*/*", RequestFilter);

                var authInfo = JsonObject.Parse(contents);

                //VK does not throw exception, but returns error property in JSON response
                string accessTokenError = authInfo.Get("error") ?? authInfo.Get("error_description");

                if (!accessTokenError.IsNullOrEmpty())
                {
                    Log.Error("VK access_token error callback. {0}".Fmt(authInfo.ToString()));
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
                //just in case VK will start throwing exceptions 
                HttpStatusCode statusCode = ((HttpWebResponse)webException.Response).StatusCode;
                if (statusCode == HttpStatusCode.BadRequest)
                {
                    return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", "AccessTokenFailed")));
                }
            }
            return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.SetParam("f", "Unknown")));
        }

        /// <summary>
        /// If previous attemts failes, the subsequential calls 
        /// build up code value like "code1,code2,code3"
        /// so we need the last one only
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private string EnsureLatestCode(string code)
        {
            int idx = code.LastIndexOf(",", StringComparison.InvariantCulture);
            if (idx > 0)
            {
                code = code.Substring(idx);
            }
            return code;
        }

        protected virtual void RequestFilter(HttpWebRequest request)
        {
            request.UserAgent = ServiceClientBase.DefaultUserAgent;
        }

        protected override void LoadUserAuthInfo(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            try
            {
                string json = "https://api.vk.com/method/users.get?user_ids={0}&fields=screen_name,bdate,city,country,timezone&oauth_token={0}"
                  .Fmt(tokens.UserId, tokens.AccessTokenSecret).GetJsonFromUrl();

                var obj = json.ArrayObjects()[0].GetUnescaped("response").ArrayObjects()[0];

                tokens.UserName = obj.Get("screen_name");
                tokens.DisplayName = obj.Get("screen_name");
                tokens.FirstName = obj.Get("first_name");
                tokens.LastName = obj.Get("last_name");
                tokens.BirthDateRaw = obj.Get("bdate");
                tokens.TimeZone = obj.Get("timezone");

                if (SaveExtendedUserInfo)
                {
                    obj.Each(x => authInfo[x.Key] = x.Value);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Could not retrieve VK user info for '{0}'".Fmt(tokens.DisplayName), ex);
            }

            LoadUserOAuthProvider(userSession, tokens);
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
        }
    }
}