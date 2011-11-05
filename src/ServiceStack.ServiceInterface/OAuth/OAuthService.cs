using System;
using System.Linq;
using System.Net;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface.OAuth
{
	public class OAuth
	{
		public string provider { get; set; }
		public string State { get; set; }
		public string oauth_token { get; set; }
		public string oauth_verifier { get; set; }
	}
	
	public class OAuthResponse
	{
		public OAuthResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}
		
		public ResponseStatus ResponseStatus { get; set; }
	}

	public class OAuthService : RestServiceBase<OAuth>
	{
		public static string DefaultOAuthRealm { get; private set; }
		public static OAuthConfig[] OAuthConfigs { get; private set; }
		public static Func<IOAuthSession> SessionFactory { get; private set; }
		
		public static string GetSessionKey(string sessionId)
		{
			return IdUtils.CreateUrn<IOAuthSession>(sessionId);
		}

		public static void Register(IAppHost appHost, Func<IOAuthSession> sessionFactory, params OAuthConfig[] oAuthConfigs)
		{
			if (oAuthConfigs.Length == 0)
				throw new ArgumentNullException("oAuthConfigs");
			
			DefaultOAuthRealm = oAuthConfigs[0].OAuthRealm;

			OAuthConfigs = oAuthConfigs;
			SessionFactory = sessionFactory;
			appHost.RegisterService<OAuthService>();
			
			SessionFeature.Register(appHost);

			appHost.RequestFilters.Add((req, res, dto) => {
				var requiresAuth = dto.GetType().FirstAttribute<AuthenticateAttribute>();
				if (requiresAuth != null)
				{
					var oAuthConfig = OAuthConfigs.FirstOrDefault(x => x.Provider == requiresAuth.Provider);
					var oAuthRealm = oAuthConfig != null ? oAuthConfig.OAuthRealm : DefaultOAuthRealm;

					var sessionId = req.GetItemOrCookie("ss-psession");
					using (var cache = appHost.GetCacheClient())
					{
						var session = sessionId != null ? cache.GetSession(sessionId) : null;
						if (session == null || !session.IsAuthorized())
						{
							res.StatusCode = (int)HttpStatusCode.Unauthorized;
							res.AddHeader(HttpHeaders.WwwAuthenticate, "OAuth realm=\"{0}\"".Fmt(oAuthRealm));
							res.Close();
							return;
						}
					}
				}
			});
		}

		public override object OnGet(OAuth request)
		{
			var provider = request.provider ?? OAuthConfigs[0].Provider;

			var oAuthConfig = OAuthConfigs.FirstOrDefault(x => x.Provider == provider);
			if (oAuthConfig == null)
				throw HttpError.NotFound("No configuration was added for OAuth provider '{0}'");

			var session = this.GetSession();
			
			if (session.ReferrerUrl.IsNullOrEmpty())
				session.ReferrerUrl = base.RequestContext.GetHeader("Referer") ?? oAuthConfig.CallbackUrl;

			if (oAuthConfig.CallbackUrl.IsNullOrEmpty())
				oAuthConfig.CallbackUrl = session.ReferrerUrl;

			var oAuth = new OAuthAuthorizer(oAuthConfig);
			
			if (!session.IsAuthorized())
			{
				var tokens = session.ProviderOAuthAccess.FirstOrDefault(x => x.Provider == provider);
				if (tokens == null)
					session.ProviderOAuthAccess.Add(tokens = new OAuthTokens());

				if (!tokens.RequestToken.IsNullOrEmpty() && !request.oauth_token.IsNullOrEmpty())
				{
					oAuth.RequestToken = tokens.RequestToken;
					oAuth.RequestTokenSecret = tokens.RequestTokenSecret;
					oAuth.AuthorizationToken = request.oauth_token;
					oAuth.AuthorizationVerifier = request.oauth_verifier;
					
					if (oAuth.AcquireAccessToken())
					{
						tokens.OAuthToken = oAuth.AccessToken;
						tokens.AccessToken = oAuth.AccessTokenSecret;
						session.OnAuthenticated(this, provider, oAuth.AuthInfo);
						this.SaveSession(session);
						
						//Haz access!
						return this.Redirect(session.ReferrerUrl.AddQueryParam("s", "1"));
					}
					
					//No Joy :(
					return this.Redirect(session.ReferrerUrl.AddQueryParam("f", "AccessTokenFailed"));
				}
				if (oAuth.AcquireRequestToken())
				{
					tokens.RequestToken = oAuth.RequestToken;
					tokens.RequestTokenSecret = oAuth.RequestTokenSecret;
					this.SaveSession(session);
					
					//Redirect to OAuth provider to approve access
					return this.Redirect(oAuthConfig.AuthorizeUrl
						.AddQueryParam("oauth_token", tokens.RequestToken)
						.AddQueryParam("oauth_callback", session.ReferrerUrl));					
				}

				return this.Redirect(session.ReferrerUrl.AddQueryParam("f", "RequestTokenFailed"));
			}
			
			//Already Authenticated
			return this.Redirect(session.ReferrerUrl.AddQueryParam("s", "0"));
		}

	}
}

