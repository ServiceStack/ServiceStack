using System;
using System.Net;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    public interface IAuthHttpGateway
    {
        bool VerifyTwitterAccessToken(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret, out string userId, out string email);
        string DownloadTwitterUserInfo(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret, string twitterUserId);

        bool VerifyFacebookAccessToken(string appId, string accessToken);
        string DownloadFacebookUserInfo(string facebookCode, params string[] fields);

        string DownloadGithubUserInfo(string accessToken);
        string DownloadGithubUserEmailsInfo(string accessToken);
        string DownloadGoogleUserInfo(string accessToken);
        string DownloadMicrosoftUserInfo(string accessToken);
        string CreateMicrosoftPhotoUrl(string accessToken, string savePhotoSize=null);
        string DownloadYammerUserInfo(string yammerUserId);
    }

    public class AuthHttpGateway : IAuthHttpGateway
    {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(AuthHttpGateway));

        public static string TwitterUserUrl = "https://api.twitter.com/1.1/users/lookup.json?user_id={0}";
        public static string TwitterVerifyCredentialsUrl = "https://api.twitter.com/1.1/account/verify_credentials.json?include_email=true";

        public static string FacebookUserUrl = "https://graph.facebook.com/v2.8/me?access_token={0}";
        public static string FacebookVerifyTokenUrl = "https://graph.facebook.com/v2.8/app?access_token={0}";

        public static string YammerUserUrl = "https://www.yammer.com/api/v1/users/{0}.json";

        public static string GithubUserUrl = "https://api.github.com/user?access_token={0}";
        public static string GithubUserEmailsUrl = "https://api.github.com/user/emails?access_token={0}";

        public string DownloadTwitterUserInfo(string consumerKey, string consumerSecret,
            string accessToken, string accessTokenSecret, string twitterUserId)
        {
            twitterUserId.ThrowIfNullOrEmpty(nameof(twitterUserId));
            return GetJsonFromOAuthUrl(consumerKey, consumerSecret, accessToken, accessTokenSecret,
                TwitterUserUrl.Fmt(twitterUserId));
        }

        public bool VerifyTwitterAccessToken(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret, 
            out string userId, out string email)
        {
            try
            {
                var json = GetJsonFromOAuthUrl(consumerKey, consumerSecret, accessToken, accessTokenSecret, TwitterVerifyCredentialsUrl);
                var obj = JsonObject.Parse(json);
                userId = obj.Get("id_str");
                email = obj.Get("email");
                return !string.IsNullOrEmpty(userId);
            }
            catch
            {
                userId = null;
                email = null;
                return false;
            }
        }

        public static string GetJsonFromOAuthUrl(
            string consumerKey, string consumerSecret,
            string accessToken, string accessTokenSecret, 
            string url, string data = null)
        {
            var uri = new Uri(url);
            var webReq = (HttpWebRequest)WebRequest.Create(uri);
            webReq.Accept = MimeTypes.Json;

            if (accessToken != null)
            {
                webReq.Headers[HttpRequestHeader.Authorization] = OAuthAuthorizer.AuthorizeRequest(
                    consumerKey, consumerSecret, accessToken, accessTokenSecret, HttpMethods.Get, uri, data);
            }

            using (var webRes = PclExport.Instance.GetResponse(webReq))
                return webRes.ReadToEnd();
        }

        public bool VerifyFacebookAccessToken(string appId, string accessToken)
        {
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(accessToken))
                return false;

            try
            {
                var url = FacebookVerifyTokenUrl.Fmt(accessToken);
                var json = url.GetJsonFromUrl();

                var obj = JsonObject.Parse(json);
                var tokenAppId = obj.Get("id");

                return tokenAppId == appId;
            }
            catch
            {
                return false;
            }
        }

        public string DownloadFacebookUserInfo(string facebookCode, params string[] fields)
        {
            facebookCode.ThrowIfNullOrEmpty("facebookCode");

            var url = FacebookUserUrl.Fmt(facebookCode);
            if (fields.Length > 0)
            {
                url = url.AddQueryParam("fields", string.Join(",", fields));
            }

            var json = url.GetJsonFromUrl();
            return json;
        }

        public string DownloadGithubUserInfo(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
                throw new ArgumentNullException(nameof(accessToken));

            var url = GithubUserUrl.Fmt(accessToken);

            var json = url.GetJsonFromUrl(
                httpReq => PclExport.Instance.SetUserAgent(httpReq, ServiceClientBase.DefaultUserAgent));

            return json;
        }

        public string DownloadGithubUserEmailsInfo(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
                throw new ArgumentNullException(nameof(accessToken));

            var url = GithubUserEmailsUrl.Fmt(accessToken);

            var json = url.GetJsonFromUrl(
                httpReq => PclExport.Instance.SetUserAgent(httpReq, ServiceClientBase.DefaultUserAgent));

            return json;
        }

        public string DownloadGoogleUserInfo(string accessToken)
        {
            var json = GoogleAuthProvider.DefaultUserProfileUrl
                .AddQueryParam("access_token", accessToken)
                .GetJsonFromUrl();

            return json;
        }

        public string DownloadMicrosoftUserInfo(string accessToken)
        {
            var json = MicrosoftGraphAuthProvider.DefaultUserProfileUrl
                .GetJsonFromUrl(requestFilter:req => req.AddBearerToken(accessToken));
            return json;
        }

        public string CreateMicrosoftPhotoUrl(string accessToken, string savePhotoSize=null)
        {
            try 
            { 
                using (var origStream = MicrosoftGraphAuthProvider.PhotoUrl
                    .GetStreamFromUrl(requestFilter:req => req.AddBearerToken(accessToken)))
                using (var origImage = System.Drawing.Image.FromStream(origStream))
                {
                    var parts = savePhotoSize?.Split('x');
                    var width = origImage.Width;
                    var height = origImage.Height;

                    if (parts != null && parts.Length > 0)
                        int.TryParse(parts[0], out width);

                    if (parts != null && parts.Length > 1)
                        int.TryParse(parts[1], out height);

                    using (var resizedImage = origImage.ResizeToPng(width, height))
                    {
                        var base64 = Convert.ToBase64String(resizedImage.GetBuffer(), 0, (int) resizedImage.Length);
                        return "data:image/png;base64," + base64;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Could not retrieve '{MicrosoftGraphAuthProvider.Name}' photo", ex);
                return null;
            }
        }

        /// <summary>
        /// Download Yammer User Info given its ID.
        /// </summary>
        /// <param name="yammerUserId">
        /// The Yammer User ID.
        /// </param>
        /// <returns>
        /// The User info in JSON format.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Yammer provides a method to retrieve current user information via
        /// "https://www.yammer.com/api/v1/users/current.json".
        /// </para>
        /// <para>
        /// However, to ensure consistency with the rest of the Auth codebase,
        /// the explicit URL will be used, where [:id] denotes the User ID: 
        /// "https://www.yammer.com/api/v1/users/[:id].json"
        /// </para>
        /// <para>
        /// Refer to: https://developer.yammer.com/restapi/ for full documentation.
        /// </para>
        /// </remarks>
        public string DownloadYammerUserInfo(string yammerUserId)
        {
            yammerUserId.ThrowIfNullOrEmpty("yammerUserId");

            var url = YammerUserUrl.Fmt(yammerUserId);
            var json = url.GetStringFromUrl();
            return json;
        }
    }
}
