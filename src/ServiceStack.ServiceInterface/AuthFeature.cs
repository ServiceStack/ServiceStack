using System;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface
{
	/// <summary>
	/// Enable the authentication feature and configure the AuthService.
	/// </summary>
	public class AuthFeature
	{
		public static void Init(IAppHost appHost, Func<IAuthSession> sessionFactory, params AuthProvider[] authProviders)
		{
			AuthService.Init(appHost, sessionFactory, authProviders);
		}
	}
}