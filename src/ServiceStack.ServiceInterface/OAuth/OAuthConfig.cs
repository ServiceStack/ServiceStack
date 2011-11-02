using ServiceStack.Configuration;

namespace ServiceStack.ServiceInterface.OAuth
{
	public class OAuthConfig
	{
		public static string OAuthRealm = "https://api.twitter.com/";
		
		public OAuthConfig() {}
		
		public OAuthConfig(IResourceManager appSettings)
		{
			OAuthRealm = appSettings.Get("OAuthRealm", OAuthRealm);
			
			this.CallbackUrl     = appSettings.GetString("CallbackUrl");
			this.ConsumerKey     = appSettings.GetString("ConsumerKey");
			this.ConsumerSecret  = appSettings.GetString("ConsumerSecret");
			this.RequestTokenUrl = appSettings.Get("RequestTokenUrl", OAuthRealm + "oauth/request_token");
			this.AuthorizeUrl    = appSettings.Get("AuthorizeUrl", OAuthRealm + "oauth/authorize");
			this.AccessTokenUrl  = appSettings.Get("AccessTokenUrl", OAuthRealm + "oauth/access_token");
		}
		
		public string CallbackUrl { get; set; }
		public string ConsumerKey { get; set; }
		public string ConsumerSecret { get; set; }
		public string RequestTokenUrl { get; set; }
		public string AuthorizeUrl { get; set; }
		public string AccessTokenUrl { get; set; }
		
	}
}

