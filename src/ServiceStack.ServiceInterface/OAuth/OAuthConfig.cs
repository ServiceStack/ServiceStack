using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.ServiceInterface.OAuth
{
	public class TwitterOAuthConfig : OAuthConfig
	{
		public const string Name = "twitter";
		public static string Realm = "https://api.twitter.com/";

		public TwitterOAuthConfig(IResourceManager appSettings)
			: base(appSettings, Realm, Name)
		{
		}
	}

	public class FacebookOAuthConfig : OAuthConfig
	{
		public const string Name = "facebook";
		public static string Realm = "https://graph.facebook.com/";

		public FacebookOAuthConfig(IResourceManager appSettings)
			: base(appSettings, Realm, Name, "AppId", "AppSecret")
		{
		}
	}


	public class OAuthConfig
	{
		public OAuthConfig() {}

		public OAuthConfig(IResourceManager appSettings, string oAuthRealm, string oAuthProvider)
			: this(appSettings, oAuthRealm, oAuthProvider, "ConsumerKey", "ConsumerSecret") { }

		public OAuthConfig(IResourceManager appSettings, string oAuthRealm, string oAuthProvider,
			string consumerKeyName, string consumerSecretName)
		{
			oAuthRealm = appSettings.Get("OAuthRealm", oAuthRealm);

			this.Provider = oAuthProvider;
			this.CallbackUrl = appSettings.GetString("oauth.{0}.CallbackUrl".Fmt(oAuthProvider));
			this.ConsumerKey = appSettings.GetString("oauth.{0}.{1}".Fmt(oAuthProvider, consumerKeyName));
			this.ConsumerSecret = appSettings.GetString("oauth.{0}.{1}".Fmt(oAuthProvider, consumerSecretName));

			this.RequestTokenUrl = appSettings.Get("oauth.{0}.RequestTokenUrl", oAuthRealm + "oauth/request_token");
			this.AuthorizeUrl = appSettings.Get("oauth.{0}.AuthorizeUrl", oAuthRealm + "oauth/authorize");
			this.AccessTokenUrl = appSettings.Get("oauth.{0}.AccessTokenUrl", oAuthRealm + "oauth/access_token");
		}

		public string OAuthRealm { get; set; }
		public string Provider { get; set; }
		public string CallbackUrl { get; set; }
		public string ConsumerKey { get; set; }
		public string ConsumerSecret { get; set; }
		public string RequestTokenUrl { get; set; }
		public string AuthorizeUrl { get; set; }
		public string AccessTokenUrl { get; set; }
	}
}

