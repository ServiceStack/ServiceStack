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

namespace ServiceStack.ServiceInterface.Auth
{
	public class Auth
	{
		public string provider { get; set; }
		public string State { get; set; }
		public string oauth_token { get; set; }
		public string oauth_verifier { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
	}

	public class AuthResponse
	{
		public AuthResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		public string SessionId { get; set; }

		public string UserName { get; set; }

		public ResponseStatus ResponseStatus { get; set; }
	}

	public class AuthService : RestServiceBase<Auth>
	{
		public const string BasicProvider = "basic";
		public const string CredentialsProvider = "credentials";

		public static string DefaultOAuthProvider { get; private set; }
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

			DefaultOAuthProvider = oAuthConfigs[0].Provider;
			DefaultOAuthRealm = oAuthConfigs[0].OAuthRealm;

			OAuthConfigs = oAuthConfigs;
			SessionFactory = sessionFactory;
			appHost.RegisterService<AuthService>();

			SessionFeature.Register(appHost);

			appHost.RequestFilters.Add((req, res, dto) => {
				var requiresAuth = dto.GetType().FirstAttribute<AuthenticateAttribute>();
				if (requiresAuth != null)
				{
					var oAuthConfig = OAuthConfigs.FirstOrDefault(x => x.Provider == requiresAuth.Provider)
						?? oAuthConfigs[0];

					var sessionId = req.GetItemOrCookie("ss-psession");
					using (var cache = appHost.GetCacheClient())
					{
						var session = sessionId != null ? cache.GetSession(sessionId) : null;
						if (session == null || !session.IsAuthorized(oAuthConfig.Provider))
						{
							res.StatusCode = (int)HttpStatusCode.Unauthorized;
							res.AddHeader(HttpHeaders.WwwAuthenticate, "OAuth realm=\"{0}\"".Fmt(oAuthConfig.OAuthRealm));
							res.Close();
							return;
						}
					}
				}
			});
		}

		public override object OnGet(Auth request)
		{
			var provider = request.provider ?? OAuthConfigs[0].Provider;

			var oAuthConfig = OAuthConfigs.FirstOrDefault(x => x.Provider == provider);
			if (oAuthConfig == null)
				throw HttpError.NotFound("No configuration was added for OAuth provider '{0}'");

			var session = this.GetSession();

			if (oAuthConfig.CallbackUrl.IsNullOrEmpty())
				oAuthConfig.CallbackUrl = base.RequestContext.AbsoluteUri;

			if (session.ReferrerUrl.IsNullOrEmpty())
				session.ReferrerUrl = base.RequestContext.GetHeader("Referer") ?? oAuthConfig.CallbackUrl;

			var oAuth = new OAuthAuthorizer(oAuthConfig);

			if (!session.IsAuthorized(provider))
			{
				var tokens = session.ProviderOAuthAccess.FirstOrDefault(x => x.Provider == provider);
				if (tokens == null)
					session.ProviderOAuthAccess.Add(tokens = new OAuthTokens { Provider = provider });

				return oAuthConfig.Authenticate(this, request, session, tokens, oAuth);
			}

			//Already Authenticated
			return this.Redirect(session.ReferrerUrl.AddQueryParam("s", "0"));
		}

		public override object OnPost(Auth request)
		{
			request.provider.ThrowIfNullOrEmpty("provider");

			if (request.provider != BasicProvider || request.provider != CredentialsProvider)
				throw new ArgumentException("Provider must be either 'basic' or 'credentials'");

			var session = this.GetSession();
			var userName = request.UserName;
			var password = request.Password;

			if (request.provider == BasicProvider)
			{
				var httpReq = base.RequestContext.Get<IHttpRequest>();
				var basicAuth = httpReq.GetBasicAuthUserAndPassword();
				if (basicAuth == null)
					throw HttpError.Unauthorized("Invalid BasicAuth credentials");

				userName = basicAuth.Value.Key;
				password = basicAuth.Value.Value;
			}

			if (session.TryAuthenticate(this, userName, password))
			{				
				return new AuthResponse {
					UserName = userName,
					SessionId = session.Id,
				};
			}

			throw HttpError.Unauthorized("Invalid UserName or Password");
		}
	}
}

