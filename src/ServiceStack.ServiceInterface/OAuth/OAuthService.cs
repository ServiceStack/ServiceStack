using System;
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
		public static OAuthConfig OAuthConfig { get; private set; }
		public static Func<IOAuthSession> SessionFactory { get; private set; }
		
		public static string GetSessionKey(string sessionId)
		{
			return IdUtils.CreateUrn<IOAuthSession>(sessionId);
		}

		public static void Register(IAppHost appHost, OAuthConfig config, Func<IOAuthSession> sessionFactory)
		{
			OAuthConfig = config;
			SessionFactory = sessionFactory;
			appHost.RegisterService<OAuthService>();
			
			SessionFeature.Register(appHost);

			appHost.RequestFilters.Add((req, res, dto) => {
				var requiresAuth = dto.GetType().GetCustomAttributes(typeof(AuthenticateAttribute), true).Length > 0;
				if (requiresAuth)
				{
					var sessionId = req.GetItemOrCookie("ss-psession");
					using (var cache = appHost.GetCacheClient())
					{
						var session = sessionId != null ? cache.GetSession(sessionId) : null;
						if (session == null || !session.IsAuthorized())
						{
							res.StatusCode = (int)HttpStatusCode.Unauthorized;
							res.AddHeader(HttpHeaders.WwwAuthenticate, "OAuth realm=\"{0}\"".Fmt(OAuthConfig.OAuthRealm));
							res.Close();
							return;
						}
					}
				}
			});
		}
		
		public override object OnGet(OAuth request)
		{
			var session = this.GetSession();
			
			if (session.ReferrerUrl.IsNullOrEmpty())
			{
				session.ReferrerUrl = base.RequestContext.GetHeader("Referer") ?? OAuthConfig.CallbackUrl;
			}
			
			var oAuth = new OAuthAuthorizer(OAuthConfig);
			
			if (!session.IsAuthorized())
			{
				if (!session.RequestToken.IsNullOrEmpty() && !request.oauth_token.IsNullOrEmpty())
				{
					oAuth.RequestToken = session.RequestToken;
					oAuth.RequestTokenSecret = session.RequestTokenSecret;
					oAuth.AuthorizationToken = request.oauth_token;
					oAuth.AuthorizationVerifier = request.oauth_verifier;
					
					if (oAuth.AcquireAccessToken())
					{
						session.OAuthToken = oAuth.AccessToken;
						session.AccessToken = oAuth.AccessTokenSecret;
						session.OnAuthenticated(this, oAuth.AuthInfo);
						this.SaveSession(session);
						
						//Haz access!
						return this.Redirect(session.ReferrerUrl.AddQueryParam("s", "1"));
					}
					
					//No Joy :(
					return this.Redirect(session.ReferrerUrl.AddQueryParam("f", "AccessTokenFailed"));
				}
				if (oAuth.AcquireRequestToken())
				{
					session.RequestToken = oAuth.RequestToken;
					session.RequestTokenSecret = oAuth.RequestTokenSecret;
					this.SaveSession(session);
					
					//Redirect to OAuth provider to approve access
					return this.Redirect(OAuthConfig.AuthorizeUrl
						.AddQueryParam("oauth_token", session.RequestToken)
						.AddQueryParam("oauth_callback", session.ReferrerUrl));					
				}

				return this.Redirect(session.ReferrerUrl.AddQueryParam("f", "RequestTokenFailed"));
			}
			
			//Already Authenticated
			return this.Redirect(session.ReferrerUrl.AddQueryParam("s", "0"));
		}
	}
}

