using Funq;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Tests;

namespace ServiceStack.WebHostApp
{
	public class AppHost
		: AppHostBase
	{
		public AppHost()
				: base("Validation Tests", typeof(SecuredService).Assembly) { }

			public override void Configure(Container container)
			{
				Plugins.Add(new AuthFeature(() => new CustomUserSession(),
					new AuthProvider[] {
						new CredentialsAuthProvider(), //HTML Form post of UserName/Password credentials
						new BasicAuthProvider(), //Sign-in with Basic Auth
					}));

				container.Register<ICacheClient>(new MemoryCacheClient());
				var userRep = new InMemoryAuthRepository();
				container.Register<IUserAuthRepository>(userRep);

				string hash;
				string salt;
				new SaltedHash().GetHashAndSaltString("test1", out hash, out salt);

				userRep.CreateUserAuth(new UserAuth {
					Id = 1,
					DisplayName = "DisplayName",
					Email = "as@if.com",
					UserName = "test1",
					FirstName = "FirstName",
					LastName = "LastName",
					PasswordHash = hash,
					Salt = salt,
				}, "test1");
			}
	}
}

