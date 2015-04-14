using System;
using System.Net;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    public interface IAuthHttpGateway
    {
        string DownloadTwitterUserInfo(OAuthAccessToken oauthToken, string twitterUserId);
        string DownloadFacebookUserInfo(string facebookCode, params string[] fields);
        string DownloadYammerUserInfo(string yammerUserId);
    }

    public class AuthHttpGateway : IAuthHttpGateway
    {
        public static string TwitterUserUrl = "https://api.twitter.com/1.1/users/lookup.json?user_id={0}";

        public static string FacebookUserUrl = "https://graph.facebook.com/v2.0/me?access_token={0}";

        public static string YammerUserUrl = "https://www.yammer.com/api/v1/users/{0}.json";

        public string DownloadTwitterUserInfo(OAuthAccessToken oauthToken, string twitterUserId)
        {
            twitterUserId.ThrowIfNullOrEmpty("twitterUserId");

            var url = TwitterUserUrl.Fmt(twitterUserId);
            var json = GetJsonFromOAuthUrl(oauthToken, url);
            return json;
        }

        public static string GetJsonFromOAuthUrl(OAuthAccessToken oauthToken, string url)
        {
            var uri = new Uri(url);
            var webReq = (HttpWebRequest)WebRequest.Create(uri);
            webReq.Accept = MimeTypes.Json;
            if (oauthToken.AccessToken != null)
            {
                webReq.Headers[HttpRequestHeader.Authorization] = OAuthAuthorizer.AuthorizeRequest(
                    oauthToken.OAuthProvider, oauthToken.AccessToken, oauthToken.AccessTokenSecret, HttpMethods.Get, uri, null);
            }

            using (var webRes = webReq.GetResponse())
                return webRes.ReadToEnd();
        }

        public string DownloadFacebookUserInfo(string facebookCode, params string[] fields)
        {
            facebookCode.ThrowIfNullOrEmpty("facebookCode");

            var url = FacebookUserUrl.Fmt(facebookCode);
            if (fields.Length > 0)
            {
                url = url.AddQueryParam("fields", string.Join(",", fields));
            }

            var json = url.GetStringFromUrl();
            return json;
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

    public class OAuthAccessToken
    {
        public OAuthProvider OAuthProvider { get; set; }
        public string AccessToken { get; set; }
        public string AccessTokenSecret { get; set; }
    }

}
