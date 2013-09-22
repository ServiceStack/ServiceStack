using ServiceStack.Common;
using ServiceStack.Clients;
using ServiceStack.Text;

namespace ServiceStack.ServiceInterface.Auth
{
    public interface IAuthHttpGateway
    {
        string DownloadTwitterUserInfo(string twitterUserId);
        string DownloadFacebookUserInfo(string facebookCode);
        string DownloadYammerUserInfo(string yammerUserId);
    }

    public class AuthHttpGateway : IAuthHttpGateway
    {
        public const string TwitterUserUrl = "http://api.twitter.com/1/users/lookup.json?user_id={0}";

        public const string FacebookUserUrl = "https://graph.facebook.com/me?access_token={0}";

        public const string YammerUserUrl = "https://www.yammer.com/api/v1/users/{0}.json";

        public string DownloadTwitterUserInfo(string twitterUserId)
        {
            twitterUserId.ThrowIfNullOrEmpty("twitterUserId");

            var url = TwitterUserUrl.Fmt(twitterUserId);
            var json = url.GetStringFromUrl();
            return json;
        }

        public string DownloadFacebookUserInfo(string facebookCode)
        {
            facebookCode.ThrowIfNullOrEmpty("facebookCode");

            var url = FacebookUserUrl.Fmt(facebookCode);
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
}