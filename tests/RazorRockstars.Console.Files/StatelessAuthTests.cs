using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.Logging;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace RazorRockstars.Console.Files
{
    public class CustomUserSession : AuthUserSession
    {
        public int Counter { get; set; }
    }

    [Route("/secured")]
    public class Secured : IReturn<SecuredResponse>
    {
        public string Name { get; set; }
    }

    public class SecuredResponse
    {
        public string Result { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    [Authenticate]
    public class SecureService : IService
    {
        public object Any(Secured request)
        {
            return new SecuredResponse { Result = request.Name };
        }
    }

    public class StatelessAuthTests
    {
        public const string ListeningOn = "http://localhost:2337/";

        protected readonly ServiceStackHost appHost;
        protected ApiKey ApiKey;

        public StatelessAuthTests()
        {
            LogManager.LogFactory = new ConsoleLogFactory();
            appHost = new AppHost { EnableAuth = true }
               .Init()
               .Start("http://*:2337/");

            var client = GetClient();
            client.Post(new Register {
                UserName = "user",
                Password = "p@55word",
                Email = "as@if{0}.com",
                DisplayName = "DisplayName",
                FirstName = "FirstName",
                LastName = "LastName",
            });

            using (var db = appHost.Resolve<IDbConnectionFactory>().OpenDbConnection())
            {
                ApiKey = db.Select<ApiKey>().First();
                ApiKey.PrintDump();
            }
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Ignore("Debug Run")]
        [Test]
        public void RunFor10Mins()
        {
            Process.Start(ListeningOn);
            Thread.Sleep(TimeSpan.FromMinutes(10));
        }

        const string Username = "user";
        const string Password = "p@55word";

        IServiceClient GetClientWithUserPassword()
        {
            return new JsonServiceClient(ListeningOn)
            {
                UserName = Username,
                Password = Password
            };
        }

        IServiceClient GetClientWithApiKey()
        {
            return new JsonServiceClient(ListeningOn)
            {
                Credentials = new NetworkCredential(ApiKey.Key, ""),
            };
        }

        protected IServiceClient GetClient()
        {
            return new JsonServiceClient(ListeningOn);
        }

        [Test]
        public void Authenticating_once_with_BasicAuth_does_not_establish_auth_session()
        {
            var client = (ServiceClientBase)GetClientWithUserPassword();
            client.AlwaysSendBasicAuthHeader = true;
            client.RequestFilter = req =>
                Assert.That(req.Headers[HttpHeaders.Authorization], Is.Not.Null);

            var request = new Secured { Name = "test" };
            var response = client.Send<SecuredResponse>(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            var newClient = GetClient();
            newClient.SetSessionId(client.GetSessionId());
            try
            {
                response = newClient.Send<SecuredResponse>(request);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            }
        }

        [Test]
        public void Authenticating_once_with_ApiKeyAuth_does_not_establish_auth_session()
        {
            var client = GetClientWithApiKey();

            var request = new Secured { Name = "test" };
            var response = client.Send<SecuredResponse>(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            var newClient = GetClient();
            newClient.SetSessionId(client.GetSessionId());
            try
            {
                response = newClient.Send<SecuredResponse>(request);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            }
        }

        [Test]
        public void Authenticating_once_with_CredentialsAuth_does_establish_auth_session()
        {
            var client = GetClient();
            client.Post(new Authenticate {
                provider = "credentials",
                UserName = Username,
                Password = Password,
            });

            var request = new Secured { Name = "test" };
            var response = client.Send<SecuredResponse>(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            var newClient = GetClient();
            newClient.SetSessionId(client.GetSessionId());
            response = newClient.Send<SecuredResponse>(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));
        }

        [Test]
        public void Can_not_access_Secured_Pages_without_Authentication()
        {
            Assert.That(ListeningOn.CombineWith("/secured").GetStringFromUrl(),
                Is.StringContaining("<!--page:Login.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/SecuredPage").GetStringFromUrl(),
                Is.StringContaining("<!--page:Login.cshtml-->"));
        }

        [Test]
        public void Can_access_Secured_Pages_with_BasicAuth()
        {
            Assert.That(ListeningOn.CombineWith("/secured").GetStringFromUrl(
                requestFilter: req => req.AddBasicAuth(Username, Password)),
                Is.StringContaining("<!--view:Secured.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/SecuredPage").GetStringFromUrl(
                requestFilter: req => req.AddBasicAuth(Username, Password)),
                Is.StringContaining("<!--page:SecuredPage.cshtml-->"));
        }

        [Test]
        public void Can_access_Secured_Pages_with_ApiKeyAuth()
        {
            Assert.That(ListeningOn.CombineWith("/secured").GetStringFromUrl(
                requestFilter: req => req.AddApiKeyAuth(ApiKey.Key)),
                Is.StringContaining("<!--view:Secured.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/SecuredPage").GetStringFromUrl(
                requestFilter: req => req.AddApiKeyAuth(ApiKey.Key)),
                Is.StringContaining("<!--page:SecuredPage.cshtml-->"));
        }

        [Test]
        public void Can_access_Secured_Pages_with_CredentialsAuth()
        {
            var client = GetClient();
            client.Post(new Authenticate {
                provider = "credentials",
                UserName = Username,
                Password = Password,
            });

            Assert.That(client.Get<string>("/secured?format=html"),
                Is.StringContaining("<!--view:Secured.cshtml-->"));

            Assert.That(client.Get<string>("/SecuredPage?format=html"),
                Is.StringContaining("<!--page:SecuredPage.cshtml-->"));
        }
    }
}