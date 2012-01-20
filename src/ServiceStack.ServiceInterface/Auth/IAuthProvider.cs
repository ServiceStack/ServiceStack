using System.Collections.Generic;

namespace ServiceStack.ServiceInterface.Auth
{
	public interface IAuthProvider
	{
		IAuthHttpGateway AuthHttpGateway { get; set; }
		string AuthRealm { get; set; }
		string Provider { get; set; }
		string CallbackUrl { get; set; }
		string ConsumerKey { get; set; }
		string ConsumerSecret { get; set; }
		string RequestTokenUrl { get; set; }
		string AuthorizeUrl { get; set; }
		string AccessTokenUrl { get; set; }

		/// <summary>
		/// Useful OAuth utilities
		/// </summary>
		OAuthAuthorizer OAuthUtils { get; set; }

		/// <summary>
		/// Remove the Users Session
		/// </summary>
		/// <param name="service"></param>
		/// <param name="request"></param>
		/// <returns></returns>
		object Logout(IServiceBase service, Auth request);

		/// <summary>
		/// The entry point for all AuthProvider providers. Runs inside the AuthService so exceptions are treated normally.
		/// Overridable so you can provide your own Auth implementation.
		/// </summary>
		object Authenticate(IServiceBase authService, IAuthSession session, Auth request);

		void OnSaveUserAuth(IServiceBase authService, IAuthSession session);
		void OnAuthenticated(IServiceBase authService, IAuthSession session, IOAuthTokens tokens, Dictionary<string, string> authInfo);
		bool IsAuthorized(IAuthSession session, IOAuthTokens tokens, Auth request = null);
        void LoadUserOAuthProvider(IAuthSession userSession, IOAuthTokens tokens);
	}
}