using ServiceStack.Common;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Text;

namespace ServiceStack.ServiceInterface.Auth
{
    public interface IAuthHttpGateway
    {
        string DownloadTwitterUserInfo(string twitterUserId);
        string DownloadFacebookUserInfo(string facebookCode);
    }

    public class AuthHttpGateway : IAuthHttpGateway
    {
        public const string TwitterUserUrl = "http://api.twitter.com/1/users/lookup.json?user_id={0}";

        public const string FacebookUserUrl = "https://graph.facebook.com/me?access_token={0}";

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
    }
}