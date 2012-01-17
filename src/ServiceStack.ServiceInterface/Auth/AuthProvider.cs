using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.ServiceInterface.Auth
{
	public class AuthProvider : IAuthProvider
	{
		protected static readonly ILog Log = LogManager.GetLogger(typeof(AuthProvider));

		public AuthProvider() { }

		public AuthProvider(IResourceManager appSettings, string authRealm, string oAuthProvider)
			: this(appSettings, authRealm, oAuthProvider, "ConsumerKey", "ConsumerSecret") { }

		public AuthProvider(IResourceManager appSettings, string authRealm, string oAuthProvider,
			string consumerKeyName, string consumerSecretName)
		{
			this.AuthRealm = appSettings.Get("OAuthRealm", authRealm);

			this.Provider = oAuthProvider;
			this.CallbackUrl = appSettings.GetString("oauth.{0}.CallbackUrl".Fmt(oAuthProvider));
			this.ConsumerKey = appSettings.GetString("oauth.{0}.{1}".Fmt(oAuthProvider, consumerKeyName));
			this.ConsumerSecret = appSettings.GetString("oauth.{0}.{1}".Fmt(oAuthProvider, consumerSecretName));

			this.RequestTokenUrl = appSettings.Get("oauth.{0}.RequestTokenUrl", authRealm + "oauth/request_token");
			this.AuthorizeUrl = appSettings.Get("oauth.{0}.AuthorizeUrl", authRealm + "oauth/authorize");
			this.AccessTokenUrl = appSettings.Get("oauth.{0}.AccessTokenUrl", authRealm + "oauth/access_token");

			this.OAuthUtils = new OAuthAuthorizer(this);
			this.AuthHttpGateway = new AuthHttpGateway();
		}

		public IAuthHttpGateway AuthHttpGateway { get; set; }

		public string AuthRealm { get; set; }
		public string Provider { get; set; }
		public string CallbackUrl { get; set; }
		public string ConsumerKey { get; set; }
		public string ConsumerSecret { get; set; }
		public string RequestTokenUrl { get; set; }
		public string AuthorizeUrl { get; set; }
		public string AccessTokenUrl { get; set; }
		public OAuthAuthorizer OAuthUtils { get; set; }

		/// <summary>
		/// Remove the Users Session
		/// </summary>
		/// <param name="service"></param>
		/// <param name="request"></param>
		/// <returns></returns>
		public virtual object Logout(IServiceBase service, Auth request)
		{
			var session = service.GetSession();
			var referrerUrl = session.ReferrerUrl
				?? service.RequestContext.GetHeader("Referer")
				?? this.CallbackUrl;

			service.RemoveSession();

			if (service.RequestContext.ResponseContentType == ContentType.Html)
				return service.Redirect(referrerUrl.AddHashParam("s", "-1"));

			return new AuthResponse();
		}

		/// <summary>
		/// The entry point for all AuthProvider providers. Runs inside the AuthService so exceptions are treated normally.
		/// Overridable so you can provide your own Auth implementation.
		/// </summary>
		/// <param name="authService"></param>
		/// <param name="session"></param>
		/// <param name="request"></param>
		/// <returns></returns>
		public virtual object Authenticate(IServiceBase authService, IAuthSession session, Auth request)
		{
			var tokens = Init(authService, ref session, request);

			//Default OAuth logic based on Twitter's OAuth workflow
			if (!tokens.RequestToken.IsNullOrEmpty() && !request.oauth_token.IsNullOrEmpty())
			{
				OAuthUtils.RequestToken = tokens.RequestToken;
				OAuthUtils.RequestTokenSecret = tokens.RequestTokenSecret;
				OAuthUtils.AuthorizationToken = request.oauth_token;
				OAuthUtils.AuthorizationVerifier = request.oauth_verifier;

				if (OAuthUtils.AcquireAccessToken())
				{
					tokens.AccessToken = OAuthUtils.AccessToken;
					tokens.AccessTokenSecret = OAuthUtils.AccessTokenSecret;
					session.IsAuthenticated = true;
					OnAuthenticated(authService, session, tokens, OAuthUtils.AuthInfo);
					authService.SaveSession(session);

					//Haz access!
					return authService.Redirect(session.ReferrerUrl.AddHashParam("s", "1"));
				}

				//No Joy :(
				tokens.RequestToken = null;
				tokens.RequestTokenSecret = null;
				authService.SaveSession(session);
				return authService.Redirect(session.ReferrerUrl.AddHashParam("f", "AccessTokenFailed"));
			}
			if (OAuthUtils.AcquireRequestToken())
			{
				tokens.RequestToken = OAuthUtils.RequestToken;
				tokens.RequestTokenSecret = OAuthUtils.RequestTokenSecret;
				authService.SaveSession(session);

				//Redirect to OAuth provider to approve access
				return authService.Redirect(this.AuthorizeUrl
					.AddQueryParam("oauth_token", tokens.RequestToken)
					.AddQueryParam("oauth_callback", session.ReferrerUrl));
			}

			return authService.Redirect(session.ReferrerUrl.AddHashParam("f", "RequestTokenFailed"));
		}

		/// <summary>
		/// Sets the CallbackUrl and session.ReferrerUrl if not set and initializes the session tokens for this AuthProvider
		/// </summary>
		/// <param name="authService"></param>
		/// <param name="session"></param>
		/// <param name="request"> </param>
		/// <returns></returns>
		protected IOAuthTokens Init(IServiceBase authService, ref IAuthSession session, Auth request)
		{
			if (request != null && !LoginMatchesSession(session, request.UserName))
			{
				authService.RemoveSession();
				session = authService.GetSession();
			}

			if (this.CallbackUrl.IsNullOrEmpty())
				this.CallbackUrl = authService.RequestContext.AbsoluteUri;

			if (session.ReferrerUrl.IsNullOrEmpty())
				session.ReferrerUrl = authService.RequestContext.GetHeader("Referer") ?? this.CallbackUrl;

			var tokens = session.ProviderOAuthAccess.FirstOrDefault(x => x.Provider == Provider);
			if (tokens == null)
				session.ProviderOAuthAccess.Add(tokens = new OAuthTokens { Provider = Provider });

			return tokens;
		}

		/// <summary>
		/// Saves the Auth Tokens for this request. Called in OnAuthenticated(). 
		/// Overrideable, the default behaviour is to call IUserAuthRepository.CreateOrMergeAuthSession().
		/// </summary>
		protected virtual void SaveUserAuth(IServiceBase authService, IAuthSession session, IUserAuthRepository authRepo, IOAuthTokens tokens)
		{
			if (authRepo == null) return;
			if (tokens != null)
			{
				session.UserAuthId = authRepo.CreateOrMergeAuthSession(session, tokens);
			}
			authRepo.LoadUserAuth(session, tokens);
			authRepo.SaveUserAuth(session);
		}

		public virtual void OnSaveUserAuth(IServiceBase authService, IAuthSession session) { }

		public virtual void OnAuthenticated(IServiceBase authService, IAuthSession session, IOAuthTokens tokens, Dictionary<string, string> authInfo)
		{
			var userSession = session as AuthUserSession;
			if (userSession != null)
			{
				session.ProviderOAuthAccess.ForEach(x => LoadUserOAuthProvider(userSession, x));
				LoadUserAuthInfo(userSession, tokens, authInfo);
			}

			var authRepo = authService.TryResolve<IUserAuthRepository>();
			if (authRepo != null)
			{
				if (tokens != null)
				{
					authInfo.ForEach((x, y) => tokens.Items[x] = y);
				}
				SaveUserAuth(authService, userSession, authRepo, tokens);
			}

			OnSaveUserAuth(authService, session);
			session.OnAuthenticated(authService, session, tokens, authInfo);
		}

		protected virtual void LoadUserAuthInfo(AuthUserSession userSession, IOAuthTokens tokens, Dictionary<string, string> authInfo) { }

		public virtual bool IsAuthorized(IAuthSession session, IOAuthTokens tokens, Auth request=null)
		{
			if (request != null)
			{
				if (!LoginMatchesSession(session, request.UserName)) return false;
			}

			return tokens != null && !string.IsNullOrEmpty(tokens.AccessTokenSecret);
		}

		protected static bool LoginMatchesSession(IAuthSession session, string userName)
		{
			if (userName == null) return false;
			var isEmail = userName.Contains("@");
			if (isEmail)
			{
				if (!userName.EqualsIgnoreCase(session.Email))
					return false;
			}
			else
			{
				if (!userName.EqualsIgnoreCase(session.UserName))
					return false;
			}
			return true;
		}

		protected virtual void LoadUserOAuthProvider(AuthUserSession userSession, IOAuthTokens tokens){}

	}

	public static class AuthConfigExtensions
	{
		public static bool IsAuthorizedSafe(this IAuthProvider authProvider, IAuthSession session, IOAuthTokens tokens)
		{
			return authProvider != null && authProvider.IsAuthorized(session, tokens);
		}
	}

}

