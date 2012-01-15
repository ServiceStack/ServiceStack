using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Common;
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

			this.oAuth = new OAuthAuthorizer(this);
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
		public OAuthAuthorizer oAuth { get; set; }

		/// <summary>
		/// Remove the Users Session
		/// </summary>
		/// <param name="service"></param>
		/// <param name="request"></param>
		/// <returns></returns>
		public virtual object Logout(IServiceBase service, Auth request)
		{
			service.RemoveSession();
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
			var tokens = Init(authService, session);

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
					OnAuthenticated(authService, session, tokens, oAuth.AuthInfo);
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
			if (oAuth.AcquireRequestToken())
			{
				tokens.RequestToken = oAuth.RequestToken;
				tokens.RequestTokenSecret = oAuth.RequestTokenSecret;
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
		/// <param name="service"></param>
		/// <param name="session"></param>
		/// <returns></returns>
		protected IOAuthTokens Init(IServiceBase service, IAuthSession session)
		{
			if (this.CallbackUrl.IsNullOrEmpty())
				this.CallbackUrl = service.RequestContext.AbsoluteUri;

			if (session.ReferrerUrl.IsNullOrEmpty())
				session.ReferrerUrl = service.RequestContext.GetHeader("Referer") ?? this.CallbackUrl;

			var tokens = session.ProviderOAuthAccess.FirstOrDefault(x => x.Provider == Provider);
			if (tokens == null)
				session.ProviderOAuthAccess.Add(tokens = new OAuthTokens { Provider = Provider });

			return tokens;
		}

		/// <summary>
		/// Saves the Auth Tokens for this request. Called in OnAuthenticated(). 
		/// Overrideable, the default behaviour is to call IUserAuthRepository.CreateOrMergeAuthSession().
		/// </summary>
		/// <param name="session"></param>
		/// <param name="provider"></param>
		/// <param name="tokens"></param>
		protected virtual void SaveUserAuth(IAuthSession session, IUserAuthRepository provider, IOAuthTokens tokens)
		{
			if (provider == null) return;
			session.UserAuthId = provider.CreateOrMergeAuthSession(session, tokens);
		}

		public virtual void OnSaveUserAuth(IServiceBase oAuthService, string userAuthId) { }

		public virtual void OnAuthenticated(IServiceBase oAuthService, IAuthSession session, IOAuthTokens tokens, Dictionary<string, string> authInfo)
		{
			var userSession = session as AuthUserSession;
			if (userSession != null)
			{
				LoadUserAuthInfo(userSession, tokens, authInfo);
			}

			var authProvider = oAuthService.TryResolve<IUserAuthRepository>();
			if (authProvider != null)
				authProvider.LoadUserAuth(session, tokens);

			authInfo.ForEach((x, y) => tokens.Items[x] = y);

			SaveUserAuth(userSession, oAuthService.TryResolve<IUserAuthRepository>(), tokens);
			OnSaveUserAuth(oAuthService, session.UserAuthId);
		}

		protected virtual void LoadUserAuthInfo(AuthUserSession userSession, IOAuthTokens tokens, Dictionary<string, string> authInfo) { }

		public virtual bool IsAuthorized(IAuthSession session, IOAuthTokens tokens)
		{
			return string.IsNullOrEmpty(tokens.AccessTokenSecret);
		}
	}

	public static class AuthConfigExtensions
	{
		public static bool IsAuthorizedSafe(this IAuthProvider authProvider, IAuthSession session, IOAuthTokens tokens)
		{
			return authProvider != null && authProvider.IsAuthorized(session, tokens);
		}
	}

}

