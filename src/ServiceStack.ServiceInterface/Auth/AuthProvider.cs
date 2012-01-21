using System;
using System.Collections.Generic;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.ServiceInterface.Auth
{
    public abstract class AuthProvider : IAuthProvider
    {
		protected static readonly ILog Log = LogManager.GetLogger(typeof(AuthProvider));

		public string AuthRealm { get; set; }
		public string Provider { get; set; }
		public string CallbackUrl { get; set; }

    	protected AuthProvider() {}

    	protected AuthProvider(IResourceManager appSettings, string authRealm, string oAuthProvider)
		{
			this.AuthRealm = appSettings.Get("OAuthRealm", authRealm);

			this.Provider = oAuthProvider;
			this.CallbackUrl = appSettings.GetString("oauth.{0}.CallbackUrl".Fmt(oAuthProvider));
		}

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

			foreach (var oAuthToken in session.ProviderOAuthAccess)
			{
				var authProvider = AuthService.GetAuthProvider(oAuthToken.Provider);
				if (authProvider == null) continue;
				var userAuthProvider = authProvider as OAuthProvider;
				if (userAuthProvider != null)
				{
					userAuthProvider.LoadUserOAuthProvider(session, oAuthToken);
				}
			}

			authRepo.SaveUserAuth(session);
		}

		public virtual void OnSaveUserAuth(IServiceBase authService, IAuthSession session) { }

		public virtual void OnAuthenticated(IServiceBase authService, IAuthSession session, IOAuthTokens tokens, Dictionary<string, string> authInfo)
		{
			var userSession = session as AuthUserSession;
			if (userSession != null)
			{
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
			authService.SaveSession(session);
			session.OnAuthenticated(authService, session, tokens, authInfo);
		}

		protected virtual void LoadUserAuthInfo(AuthUserSession userSession, IOAuthTokens tokens, Dictionary<string, string> authInfo) { }

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

		public abstract bool IsAuthorized(IAuthSession session, IOAuthTokens tokens, Auth request = null);
    	public abstract object Authenticate(IServiceBase authService, IAuthSession session, Auth request);
    }

	public static class AuthConfigExtensions
    {
        public static bool IsAuthorizedSafe(this IAuthProvider authProvider, IAuthSession session, IOAuthTokens tokens)
        {
            return authProvider != null && authProvider.IsAuthorized(session, tokens);
        }
    }

}

