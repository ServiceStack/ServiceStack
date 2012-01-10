using System;
using System.Net;
using Funq;
using NUnit.Framework;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	public class Secured
	{
		public string Name { get; set; }
	}

	public class SecuredResponse
	{
		public string Result { get; set; }

		public ResponseStatus ResponseStatus { get; set; }
	}

	[Authenticate]
	public class SecuredService : ServiceBase<Secured>
	{
		protected override object Run(Secured request)
		{
			return new SecuredResponse { Result = request.Name };
		}
	}

	public class CustomUserSession : AuthUserSession
	{
		public override bool TryAuthenticate(IServiceBase oAuthService,
			string userName, string password)
		{
			return userName == "user" && password == "p@55word";
		}
	}

	public class AuthTests
	{
		private const string ListeningOn = "http://localhost:82/";

		public class AuthAppHostHttpListener
			: AppHostHttpListenerBase
		{

			public AuthAppHostHttpListener()
				: base("Validation Tests", typeof(CustomerService).Assembly) { }

			public override void Configure(Container container)
			{
				AuthFeature.Init(this, () => new CustomUserSession(),
					new AuthConfig[] {
						new CredentialsAuthConfig(), //HTML Form post of UserName/Password credentials
						new BasicAuthConfig(), //Sign-in with Basic Auth
					});

				container.Register<ICacheClient>(new MemoryCacheClient());
				container.Register<IUserAuthRepository>(new InMemoryAuthRepository());
			}
		}

		AuthAppHostHttpListener appHost;

		[TestFixtureSetUp]
		public void OnTestFixtureSetUp()
		{
			appHost = new AuthAppHostHttpListener();
			appHost.Init();
			appHost.Start(ListeningOn);
		}

		[TestFixtureTearDown]
		public void OnTestFixtureTearDown()
		{
			appHost.Dispose();
		}

		IServiceClient GetClient()
		{
			return new JsonServiceClient(ListeningOn);
		}

		IServiceClient GetClientWithUserPassword()
		{
			return new JsonServiceClient(ListeningOn) {
				UserName = "user",
				Password = "p@55word",
			};
		}

		[Test]
		public void No_Credentials_throws_UnAuthorized()
		{
			try
			{
				var client = GetClient();
				var request = new Secured { Name = "test" };
				var response = client.Send<SecureResponse>(request);

				Assert.Fail("Shouldn't be allowed");
			}
			catch (WebServiceException webEx)
			{
				Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
				Console.WriteLine(webEx.ResponseDto.Dump());
			}
		}

		[Test]
		public void Does_work_with_Credentials()
		{
			try
			{
				var client = GetClientWithUserPassword();
				var request = new Secured { Name = "test" };
				var response = client.Send<SecureResponse>(request);
				Assert.That(response.Result, Is.EqualTo(request.Name));
			}
			catch (WebServiceException webEx)
			{
				Assert.Fail(webEx.Message);
			}
		}

		[Test]
		public void Manually_authenticating_holds()
		{
			try
			{
				var client = GetClient();

				var authResponse = client.Send<AuthResponse>(new Auth {
					provider = CredentialsAuthConfig.Name,
					UserName = "user",
					Password = "p@55word",
				});

				Console.WriteLine(authResponse.Dump());

				var request = new Secured { Name = "test" };
				var response = client.Send<SecureResponse>(request);
				Assert.That(response.Result, Is.EqualTo(request.Name));
			}
			catch (WebServiceException webEx)
			{
				Assert.Fail(webEx.Message);
			}
		}

	}
}