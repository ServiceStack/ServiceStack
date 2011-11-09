using System.Net;
using System.Web;
using ServiceStack.Common;
using ServiceStack.Configuration;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel;
using ServiceStack.Text;

namespace ServiceStack.ServiceInterface.Auth
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
		public static string PreAuthUrl = "https://www.facebook.com/dialog/oauth";

		public string AppId { get; set; }
		public string AppSecret { get; set; }
		public string[] Permissions { get; set; }

		public FacebookOAuthConfig(IResourceManager appSettings)
			: base(appSettings, Realm, Name, "AppId", "AppSecret")
		{
			this.AppId = appSettings.GetString("oauth.facebook.AppId");
			this.AppSecret = appSettings.GetString("oauth.facebook.AppSecret");
			this.Permissions = appSettings.Get("oauth.facebook.Permissions", new string[0]);
		}

		public override object Authenticate(IServiceBase service, Auth request, IOAuthSession session, IOAuthTokens tokens, OAuthAuthorizer oAuth)
		{
			var code = service.RequestContext.Get<IHttpRequest>().QueryString["code"];
			var isPreAuthCallback = !code.IsNullOrEmpty();
			if (!isPreAuthCallback)
			{
				var preAuthUrl = PreAuthUrl + "?client_id={0}&redirect_uri={1}&scope={2}"
					.Fmt(AppId, this.CallbackUrl.UrlEncode(), string.Join(",", Permissions));
				return service.Redirect(preAuthUrl);
			}

			var accessTokenUrl = this.AccessTokenUrl + "?client_id={0}&redirect_uri={1}&client_secret={2}&code={3}"
				.Fmt(AppId, this.CallbackUrl.UrlEncode(), AppSecret, code);

			try
			{
				var contents = accessTokenUrl.DownloadUrl();
				var authInfo = HttpUtility.ParseQueryString(contents);
				tokens.AccessTokenSecret = authInfo["access_token"];
				service.SaveSession(session);
				session.OnAuthenticated(service, tokens, authInfo.ToDictionary());

				//Haz access!
				return service.Redirect(session.ReferrerUrl.AddQueryParam("s", "1"));
			}
			catch (WebException we)
			{
				var statusCode = ((HttpWebResponse)we.Response).StatusCode;
				if (statusCode == HttpStatusCode.BadRequest)
				{
					return service.Redirect(session.ReferrerUrl.AddQueryParam("f", "AccessTokenFailed"));
				}
			}

			//Shouldn't get here
			return service.Redirect(session.ReferrerUrl.AddQueryParam("f", "Unknown"));
		}
	}


	public class OAuthConfig
	{
		public OAuthConfig() { }

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

		public virtual object Authenticate(IServiceBase service, Auth request, IOAuthSession session,
			IOAuthTokens tokens, OAuthAuthorizer oAuth)
		{
			//Default oAuth logic based on Twitter's oAuth workflow
			if (!tokens.RequestToken.IsNullOrEmpty() && !request.oauth_token.IsNullOrEmpty())
			{
				oAuth.RequestToken = tokens.RequestToken;
				oAuth.RequestTokenSecret = tokens.RequestTokenSecret;
				oAuth.AuthorizationToken = request.oauth_token;
				oAuth.AuthorizationVerifier = request.oauth_verifier;

				if (oAuth.AcquireAccessToken())
				{
					tokens.AccessToken = oAuth.AccessToken;
					tokens.AccessTokenSecret = oAuth.AccessTokenSecret;
					session.OnAuthenticated(service, tokens, oAuth.AuthInfo);
					service.SaveSession(session);

					//Haz access!
					return service.Redirect(session.ReferrerUrl.AddQueryParam("s", "1"));
				}

				//No Joy :(
				return service.Redirect(session.ReferrerUrl.AddQueryParam("f", "AccessTokenFailed"));
			}
			if (oAuth.AcquireRequestToken())
			{
				tokens.RequestToken = oAuth.RequestToken;
				tokens.RequestTokenSecret = oAuth.RequestTokenSecret;
				service.SaveSession(session);

				//Redirect to OAuth provider to approve access
				return service.Redirect(this.AuthorizeUrl
					.AddQueryParam("oauth_token", tokens.RequestToken)
					.AddQueryParam("oauth_callback", session.ReferrerUrl));
			}

			return service.Redirect(session.ReferrerUrl.AddQueryParam("f", "RequestTokenFailed"));
		}
	}
}

