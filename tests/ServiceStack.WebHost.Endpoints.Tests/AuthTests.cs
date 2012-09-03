using System;
using System.IO;
using System.Net;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/secured")]
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
	public class SecuredService : RestServiceBase<Secured>
	{
		public override object OnPost(Secured request)
		{
			return new SecuredResponse { Result = request.Name };
		}

        public override object OnGet(Secured request)
        {
            throw new ArgumentException("unicorn nuggets");
        }
	}

    [Route("/securedfileupload")]
    public class SecuredFileUpload
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
    }

    [Authenticate]
    public class SecuredFileUploadService : RestServiceBase<SecuredFileUpload>
    {
        public override object OnPost(SecuredFileUpload request)
        {
            var file = this.RequestContext.Files[0];
            return new FileUploadResponse
            {
                FileName = file.FileName,
                ContentLength = file.ContentLength,
                ContentType = file.ContentType,
                Contents = new StreamReader(file.InputStream).ReadToEnd(),
                CustomerId = request.CustomerId,
                CustomerName = request.CustomerName
            };
        }
    }


	public class RequiresRole
	{
		public string Name { get; set; }
	}

	public class RequiresRoleResponse
	{
		public string Result { get; set; }

		public ResponseStatus ResponseStatus { get; set; }
	}

	[RequiredRole("TheRole")]
	public class RequiresRoleService : ServiceBase<RequiresRole>
	{
		protected override object Run(RequiresRole request)
		{
			return new RequiresRoleResponse { Result = request.Name };
		}
	}

	public class CustomUserSession : AuthUserSession
	{
		public override void OnAuthenticated(IServiceBase authService, IAuthSession session, IOAuthTokens tokens, System.Collections.Generic.Dictionary<string, string> authInfo)
		{
			if (!session.Roles.Contains("TheRole"))
				session.Roles.Add("TheRole");

			authService.RequestContext.Get<IHttpRequest>().SaveSession(session);
		}
	}

	public class AuthTests
	{
		private const string ListeningOn = "http://localhost:82/";

		private const string UserName = "user";
		private const string Password = "p@55word";

		public class AuthAppHostHttpListener
			: AppHostHttpListenerBase
		{
			public AuthAppHostHttpListener()
				: base("Validation Tests", typeof(CustomerService).Assembly) { }

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

		private static void FailOnAsyncError<T>(T response, Exception ex)
		{
			Assert.Fail(ex.Message);
		}

		IServiceClient GetClient()
		{
			return new JsonServiceClient(ListeningOn);
		}

		IServiceClient GetClientWithUserPassword()
		{
			return new JsonServiceClient(ListeningOn) {
				UserName = UserName,
				Password = Password,
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
        public void PostFile_with_no_Credentials_throws_UnAuthorized()
        {
            try
            {
                var client = GetClient();
                var uploadFile = new FileInfo("~/TestExistingDir/upload.html".MapProjectPath());
                client.PostFile<FileUploadResponse>(ListeningOn + "/securedfileupload", uploadFile, MimeTypes.GetMimeType(uploadFile.Name));

                Assert.Fail("Shouldn't be allowed");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
                Console.WriteLine(webEx.ResponseDto.Dump());
            }
        }

        [Test]
        public void PostFile_does_work_with_BasicAuth()
        {
            var client = GetClientWithUserPassword();
            var uploadFile = new FileInfo("~/TestExistingDir/upload.html".MapProjectPath());

            var expectedContents = new StreamReader(uploadFile.OpenRead()).ReadToEnd();
            var response = client.PostFile<FileUploadResponse>(ListeningOn + "/securedfileupload", uploadFile, MimeTypes.GetMimeType(uploadFile.Name));
            Assert.That(response.FileName, Is.EqualTo(uploadFile.Name));
            Assert.That(response.ContentLength, Is.EqualTo(uploadFile.Length));
            Assert.That(response.Contents, Is.EqualTo(expectedContents));
        }

        [Test]
        public void PostFileWithRequest_does_work_with_BasicAuth()
        {
            var client = GetClientWithUserPassword();
            var request = new SecuredFileUpload { CustomerId = 123, CustomerName = "Foo" };
            var uploadFile = new FileInfo("~/TestExistingDir/upload.html".MapProjectPath());

            var expectedContents = new StreamReader(uploadFile.OpenRead()).ReadToEnd();
            var response = client.PostFileWithRequest<FileUploadResponse>(ListeningOn + "/securedfileupload", uploadFile, request);
            Assert.That(response.FileName, Is.EqualTo(uploadFile.Name));
            Assert.That(response.ContentLength, Is.EqualTo(uploadFile.Length));
            Assert.That(response.Contents, Is.EqualTo(expectedContents));
            Assert.That(response.CustomerName, Is.EqualTo("Foo"));
            Assert.That(response.CustomerId, Is.EqualTo(123));
        }

		[Test]
		public void Does_work_with_BasicAuth()
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
		public void Does_always_send_BasicAuth()
		{
			try
			{
				var client = (ServiceClientBase)GetClientWithUserPassword();
				client.AlwaysSendBasicAuthHeader = true;
				client.LocalHttpWebRequestFilter = req => {
						bool hasAuthentication = false;
						foreach (var key in req.Headers.Keys)
						{
							if (key.ToString() == "Authorization")
								hasAuthentication = true;
						}
						Assert.IsTrue(hasAuthentication);
					};

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
		public void Does_work_with_CredentailsAuth()
		{
			try
			{
				var client = GetClient();

				var authResponse = client.Send<AuthResponse>(new Auth {
					provider = CredentialsAuthProvider.Name,
					UserName = "user",
					Password = "p@55word",
					RememberMe = true,
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

		[Test]
		public void Does_work_with_CredentailsAuth_Async()
		{
			var client = GetClient();

			var request = new Secured { Name = "test" };
			SecureResponse response = null;

			client.SendAsync<AuthResponse>(new Auth {
				provider = CredentialsAuthProvider.Name,
				UserName = "user",
				Password = "p@55word",
				RememberMe = true,
			}, authResponse => {
				Console.WriteLine(authResponse.Dump());
				client.SendAsync<SecureResponse>(request, r => response = r, FailOnAsyncError);

			}, FailOnAsyncError);

			Thread.Sleep(TimeSpan.FromSeconds(1));
			Assert.That(response.Result, Is.EqualTo(request.Name));
		}
		
		[Test]
		public void Can_call_RequiredRole_service_with_BasicAuth()
		{
			try
			{
				var client = GetClientWithUserPassword();
				var request = new RequiresRole { Name = "test" };
				var response = client.Send<RequiresRoleResponse>(request);
				Assert.That(response.Result, Is.EqualTo(request.Name));
			}
			catch (WebServiceException webEx)
			{
				Assert.Fail(webEx.Message);
			}
		}
		
		
		[Test]
		public void Does_work_with_CredentailsAuth_Multiple_Times()
		{
			try
			{
				var client = GetClient();

				var authResponse = client.Send<AuthResponse>(new Auth {
					provider = CredentialsAuthProvider.Name,
					UserName = "user",
					Password = "p@55word",
					RememberMe = true,
				});

				Console.WriteLine(authResponse.Dump());

				for(int i =0; i<500; i++){
				var request = new Secured { Name = "test" };
				var response = client.Send<SecureResponse>(request);
				Assert.That(response.Result, Is.EqualTo(request.Name));
					Console.WriteLine("loop : {0}",i);
				}
			}
			catch (WebServiceException webEx)
			{
				Assert.Fail(webEx.Message);
			}
		}

        [Test]
        public void Exceptions_thrown_are_received_by_client_when_AlwaysSendBasicAuthHeader_is_false()
        {
            try
            {
                var client = (IRestClient)GetClientWithUserPassword();
                ((ServiceClientBase)client).AlwaysSendBasicAuthHeader = false;
                var response = client.Get<SecuredResponse>("/secured");

                Assert.Fail("Should have thrown");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.ErrorMessage, Is.EqualTo("unicorn nuggets"));
            }
        }

        [Test]
        public void Exceptions_thrown_are_received_by_client_when_AlwaysSendBasicAuthHeader_is_true()
        {
            try
            {
                var client = (IRestClient)GetClientWithUserPassword();
                ((ServiceClientBase)client).AlwaysSendBasicAuthHeader = true;
                var response = client.Get<SecuredResponse>("/secured");

                Assert.Fail("Should have thrown");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.ErrorMessage, Is.EqualTo("unicorn nuggets"));
            }
        }
	}
}