using System;
using System.Linq;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface
{
	/// <summary>
	/// Enable the authentication feature and configure the AuthService.
	/// </summary>
	public class AuthFeature
	{
		public const string AdminRole = "Admin";
		public static bool AddUserIdHttpHeader = true;
		
		public static TimeSpan? GetDefaultSessionExpiry()
		{
			var authProvider = AuthService.AuthProviders.FirstOrDefault() as AuthProvider;
			return authProvider == null ? null : authProvider.SessionExpiry;
		}

		public static void Init(IAppHost appHost, Func<IAuthSession> sessionFactory, params IAuthProvider[] authProviders)
		{
			AuthService.Init(appHost, sessionFactory, authProviders);
		}
	}
}