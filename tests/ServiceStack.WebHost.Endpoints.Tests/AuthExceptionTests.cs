using System;
using System.Net;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.Endpoints.Tests
{
  public class ExceptionAuth
	{
		public string Name { get; set; }
	}

  public class ExceptionAuthResponse
	{
		public string Result { get; set; }

		public ResponseStatus ResponseStatus { get; set; }
	}

	[Authenticate]
  public class AuthExceptionService : ServiceBase<ExceptionAuth>
	{
    protected override object Run(ExceptionAuth request)
		{
      throw new ArgumentException("unicorn nuggets");
		}
	}

  public class AuthExceptionTests
	{
		private const string ListeningOn = "http://localhost:82/";

		private const string UserName = "user";
		private const string Password = "p@55word";

		public class AuthAppHostHttpListener
			: AppHostHttpListenerBase
		{
			public AuthAppHostHttpListener()
				: base("Auth exception Tests", typeof(CustomerService).Assembly) { }

			public override void Configure(Container container)
			{
				Plugins.Add(new AuthFeature(() => new AuthUserSession(),
					new AuthProvider[] {
						new CredentialsAuthProvider(), //HTML Form post of UserName/Password credentials
						new BasicAuthProvider(), //Sign-in with Basic Auth
					}));

				container.Register<ICacheClient>(new MemoryCacheClient());
				var userRep = new InMemoryAuthRepository();
				container.Register<IUserAuthRepository>(userRep);

				string hash;
				string salt;
				new SaltedHash().GetHashAndSaltString(Password, out hash, out salt);

				userRep.CreateUserAuth(new UserAuth {
					Id = 1,
					DisplayName = "DisplayName",
					Email = "as@if.com",
					UserName = UserName,
					FirstName = "FirstName",
					LastName = "LastName",
					PasswordHash = hash,
					Salt = salt,
				}, Password);
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

		IServiceClient GetClientWithUserPassword()
		{
			return new JsonServiceClient(ListeningOn) {
				UserName = UserName,
				Password = Password,
			};
		}

		[Test]
		public void Exceptions_thrown_are_received_by_client_when_AlwaysSendBasicAuthHeader_is_false()
		{
			try
			{
        var client = GetClientWithUserPassword();
        ((ServiceClientBase)client).AlwaysSendBasicAuthHeader = false;
				var request = new ExceptionAuth { Name = "test" };
        var response = client.Send<ExceptionAuthResponse>(request);

				Assert.Fail("Should have thrown");
			}
			catch (WebServiceException webEx)
			{
        Assert.That(webEx.ErrorMessage, Is.EqualTo("unicorn nuggets"));
				Console.WriteLine(webEx.ResponseDto.Dump());
			}
		}

    [Test]
    public void Exceptions_thrown_are_received_by_client_when_AlwaysSendBasicAuthHeader_is_true()
    {
      try
      {
        var client = GetClientWithUserPassword();
        ((ServiceClientBase)client).AlwaysSendBasicAuthHeader = true;
        var request = new ExceptionAuth { Name = "test" };
        var response = client.Send<ExceptionAuthResponse>(request);

        Assert.Fail("Should have thrown");
      }
      catch (WebServiceException webEx)
      {
        Assert.That(webEx.ErrorMessage, Is.EqualTo("unicorn nuggets"));
        Console.WriteLine(webEx.ResponseDto.Dump());
      }
    }
	}
}