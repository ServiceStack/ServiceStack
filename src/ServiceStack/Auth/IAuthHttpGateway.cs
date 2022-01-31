using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    public interface IAuthHttpGateway
    {
        bool VerifyTwitterAccessToken(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret, out string userId, out string email);

        Task<AuthId> VerifyTwitterAccessTokenAsync(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret, CancellationToken token = default);
        string DownloadTwitterUserInfo(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret, string twitterUserId);
        Task<string> DownloadTwitterUserInfoAsync(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret, string twitterUserId, CancellationToken token = default);

        bool VerifyFacebookAccessToken(string appId, string accessToken);

        Task<bool> VerifyFacebookAccessTokenAsync(string appId, string accessToken, CancellationToken token = default);
        string DownloadFacebookUserInfo(string facebookCode, params string[] fields);
        Task<string> DownloadFacebookUserInfoAsync(string facebookCode, string[] fields, CancellationToken token = default);

        string DownloadGithubUserInfo(string accessToken);
        Task<string> DownloadGithubUserInfoAsync(string accessToken, CancellationToken token = default);
        string DownloadGithubUserEmailsInfo(string accessToken);
        Task<string> DownloadGithubUserEmailsInfoAsync(string accessToken, CancellationToken token = default);
        string DownloadGoogleUserInfo(string accessToken);
        Task<string> DownloadGoogleUserInfoAsync(string accessToken, CancellationToken token = default);
        string DownloadMicrosoftUserInfo(string accessToken);
        Task<string> DownloadMicrosoftUserInfoAsync(string accessToken, CancellationToken token = default);
        string CreateMicrosoftPhotoUrl(string accessToken, string savePhotoSize=null);
        Task<string> CreateMicrosoftPhotoUrlAsync(string accessToken, string savePhotoSize = null, CancellationToken token = default);
        string DownloadYammerUserInfo(string yammerUserId);
        Task<string> DownloadYammerUserInfoAsync(string yammerUserId);
    }

    public class AuthId
    {
        public string UserId { get; set; }
        public string Email { get; set; }
    }

    public class AuthHttpGateway : IAuthHttpGateway
    {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(AuthHttpGateway));

        public static string TwitterUserUrl = "https://api.twitter.com/1.1/users/lookup.json?user_id={0}";
        public static string TwitterVerifyCredentialsUrl = "https://api.twitter.com/1.1/account/verify_credentials.json?include_email=true";

        public static string FacebookUserUrl = "https://graph.facebook.com/v2.8/me?access_token={0}";
        public static string FacebookVerifyTokenUrl = "https://graph.facebook.com/v2.8/app?access_token={0}";

        public static string YammerUserUrl = "https://www.yammer.com/api/v1/users/{0}.json";

        public static string GithubUserUrl = "https://api.github.com/user";
        public static string GithubUserEmailsUrl = "https://api.github.com/user/emails";

        public string DownloadTwitterUserInfo(string consumerKey, string consumerSecret,
            string accessToken, string accessTokenSecret, string twitterUserId)
        {
            twitterUserId.ThrowIfNullOrEmpty(nameof(twitterUserId));
            return GetJsonFromOAuthUrl(consumerKey, consumerSecret, accessToken, accessTokenSecret,
                TwitterUserUrl.Fmt(twitterUserId));
        }

        public async Task<string> DownloadTwitterUserInfoAsync(string consumerKey, string consumerSecret,
            string accessToken, string accessTokenSecret, string twitterUserId, CancellationToken token=default)
        {
            twitterUserId.ThrowIfNullOrEmpty(nameof(twitterUserId));
            return await GetJsonFromOAuthUrlAsync(consumerKey, consumerSecret, accessToken, accessTokenSecret,
                TwitterUserUrl.Fmt(twitterUserId), token: token).ConfigAwait();
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

        public async Task<AuthId> VerifyTwitterAccessTokenAsync(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret, CancellationToken token=default)
        {
            try
            {
                var json = await GetJsonFromOAuthUrlAsync(consumerKey, consumerSecret, accessToken, accessTokenSecret, TwitterVerifyCredentialsUrl, token: token).ConfigAwait();
                var obj = JsonObject.Parse(json);
                var userId = obj.Get("id_str");
                var email = obj.Get("email");
                if (!string.IsNullOrEmpty(userId))
                    return new AuthId { UserId = userId, Email = email };
            }
            catch
            {
            }
            return null;
        }

        public static string GetJsonFromOAuthUrl(
            string consumerKey, string consumerSecret,
            string accessToken, string accessTokenSecret, 
            string url, string data = null)
        {
            var uri = new Uri(url);
            var webReq = WebRequest.CreateHttp(uri);
            webReq.Accept = MimeTypes.Json;

            if (accessToken != null)
            {
                webReq.Headers[HttpRequestHeader.Authorization] = OAuthAuthorizer.AuthorizeRequest(
                    consumerKey, consumerSecret, accessToken, accessTokenSecret, HttpMethods.Get, uri, data);
            }

            using var webRes = PclExport.Instance.GetResponse(webReq);
            return webRes.ReadToEnd();
        }

        public static async Task<string> GetJsonFromOAuthUrlAsync(
            string consumerKey, string consumerSecret,
            string accessToken, string accessTokenSecret, 
            string url, string data = null, CancellationToken token=default)
        {
            var uri = new Uri(url);
            var webReq = WebRequest.CreateHttp(uri);
            webReq.Accept = MimeTypes.Json;

            if (accessToken != null)
            {
                webReq.Headers[HttpRequestHeader.Authorization] = OAuthAuthorizer.AuthorizeRequest(
                    consumerKey, consumerSecret, accessToken, accessTokenSecret, HttpMethods.Get, uri, data);
            }

            using var webRes = await webReq.GetResponseAsync();
            using var stream = webRes.GetResponseStream();
            return await stream.ReadToEndAsync(HttpUtils.UseEncoding).ConfigAwait();
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

        public async Task<bool> VerifyFacebookAccessTokenAsync(string appId, string accessToken, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(accessToken))
                return false;

            try
            {
                var url = FacebookVerifyTokenUrl.Fmt(accessToken);
                var json = await url.GetJsonFromUrlAsync(token: token).ConfigAwait();

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

        public async Task<string> DownloadFacebookUserInfoAsync(string facebookCode, string[] fields, CancellationToken token=default)
        {
            facebookCode.ThrowIfNullOrEmpty("facebookCode");

            var url = FacebookUserUrl.Fmt(facebookCode);
            if (fields.Length > 0)
            {
                url = url.AddQueryParam("fields", string.Join(",", fields));
            }

            var json = await url.GetJsonFromUrlAsync(token: token).ConfigAwait();
            return json;
        }

        public string GetJsonFromGitHub(string url, string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
                throw new ArgumentNullException(nameof(accessToken));

            var json = url.GetJsonFromUrl(
                requestFilter: req => req.With(c => {
                    c.UserAgent = ServiceClientBase.DefaultUserAgent;
                    c.Authorization = new("token", accessToken);
                }));

            return json;
        }

        public async Task<string> GetJsonFromGitHubAsync(string url, string accessToken, CancellationToken token=default)
        {
            if (string.IsNullOrEmpty(accessToken))
                throw new ArgumentNullException(nameof(accessToken));

            var json = await url.GetJsonFromUrlAsync(
                requestFilter: req => req.With(c => {
                    c.UserAgent = ServiceClientBase.DefaultUserAgent;
                    c.Authorization = new("token", accessToken);
                }));

            return json;
        }

        public string DownloadGithubUserInfo(string accessToken) =>
            GetJsonFromGitHub(GithubUserUrl, accessToken);

        public Task<string> DownloadGithubUserInfoAsync(string accessToken, CancellationToken token = default) =>
            GetJsonFromGitHubAsync(GithubUserUrl, accessToken, token);

        public string DownloadGithubUserEmailsInfo(string accessToken) =>
            GetJsonFromGitHub(GithubUserEmailsUrl, accessToken);

        public Task<string> DownloadGithubUserEmailsInfoAsync(string accessToken, CancellationToken token=default) =>
            GetJsonFromGitHubAsync(GithubUserEmailsUrl, accessToken, token);
        
        public string DownloadGoogleUserInfo(string accessToken)
        {
            var json = GoogleAuthProvider.DefaultUserProfileUrl
                .AddQueryParam("access_token", accessToken)
                .GetJsonFromUrl();

            return json;
        }

        public async Task<string> DownloadGoogleUserInfoAsync(string accessToken, CancellationToken token=default)
        {
            var json = await GoogleAuthProvider.DefaultUserProfileUrl
                .AddQueryParam("access_token", accessToken)
                .GetJsonFromUrlAsync(token: token).ConfigAwait();

            return json;
        }

        public string DownloadMicrosoftUserInfo(string accessToken)
        {
            var json = MicrosoftGraphAuthProvider.DefaultUserProfileUrl
                .GetJsonFromUrl(requestFilter:req => req.AddBearerToken(accessToken));
            return json;
        }

        public async Task<string> DownloadMicrosoftUserInfoAsync(string accessToken, CancellationToken token=default)
        {
            var json = await MicrosoftGraphAuthProvider.DefaultUserProfileUrl
                .GetJsonFromUrlAsync(requestFilter:req => req.AddBearerToken(accessToken), token: token).ConfigAwait();
            return json;
        }

        public string CreateMicrosoftPhotoUrl(string accessToken, string savePhotoSize=null)
        {
            try
            {
                using var imageStream = MicrosoftGraphAuthProvider.PhotoUrl(savePhotoSize)
                    .GetStreamFromUrl(requestFilter:req => req.AddBearerToken(accessToken));
                var base64 = Convert.ToBase64String(imageStream.ReadFully());
                return "data:image/jpg;base64," + base64;
            }
            catch (Exception ex)
            {
                Log.Warn($"Could not retrieve '{MicrosoftGraphAuthProvider.Name}' photo", ex);
                return null;
            }
        }

        public async Task<string> CreateMicrosoftPhotoUrlAsync(string accessToken, string savePhotoSize=null, CancellationToken token=default)
        {
            try
            {
                using var imageStream = await MicrosoftGraphAuthProvider.PhotoUrl(savePhotoSize)
                    .GetStreamFromUrlAsync(requestFilter:req => req.AddBearerToken(accessToken), token: token);
                var base64 = Convert.ToBase64String(await imageStream.ReadFullyAsync(token));
                return "data:image/jpg;base64," + base64;
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

        public async Task<string> DownloadYammerUserInfoAsync(string yammerUserId)
        {
            yammerUserId.ThrowIfNullOrEmpty("yammerUserId");

            var url = YammerUserUrl.Fmt(yammerUserId);
            var json = await url.GetStringFromUrlAsync();
            return json;
        }

    }
}
