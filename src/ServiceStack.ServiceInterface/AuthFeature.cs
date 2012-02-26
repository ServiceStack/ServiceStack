using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface
{
	/// <summary>
	/// Enable the authentication feature and configure the AuthService.
	/// </summary>
	public class AuthFeature : IPlugin
	{
		public const string AdminRole = "Admin";
		public static bool AddUserIdHttpHeader = true;

		private readonly Func<IAuthSession> sessionFactory;
		private readonly IAuthProvider[] authProviders;

		public List<Type> RegisterServices { get; set; }
		public List<IPlugin> RegisterPlugins { get; set; }
		
		public bool IncludeAssignRoleServices
		{
			set
			{
				if (!value)
				{
					RegisterServices.RemoveAll(x =>
						x == typeof(AssignRolesService)
						|| x == typeof(UnAssignRolesService));
				}
			}
		}

		public AuthFeature(Func<IAuthSession> sessionFactory, IAuthProvider[] authProviders)
		{
			this.sessionFactory = sessionFactory;
			this.authProviders = authProviders;

			RegisterServices = new List<Type> {
				typeof(AuthService),
				typeof(AssignRolesService),                      
				typeof(UnAssignRolesService),        
			};
			RegisterPlugins = new List<IPlugin> {
				new SessionFeature()                          
			};
		}

		public void Register(IAppHost appHost)
		{
			AuthService.Init(appHost, sessionFactory, authProviders);

			var unitTest = appHost == null;
			if (unitTest) return;

			RegisterServices.ForEach(x => appHost.RegisterService(x));
			RegisterPlugins.ForEach(x => appHost.LoadPlugin(x));
		}

		public static TimeSpan? GetDefaultSessionExpiry()
		{
			var authProvider = AuthService.AuthProviders.FirstOrDefault() as AuthProvider;
			return authProvider == null ? null : authProvider.SessionExpiry;
		}
	}
}