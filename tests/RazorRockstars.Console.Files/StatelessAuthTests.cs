using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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

    public class JsonHttpClientStatelessAuthTests : StatelessAuthTests
    {
        protected override IServiceClient GetClientWithUserPassword(bool alwaysSend = false)
        {
            return new JsonHttpClient(ListeningOn)
            {
                UserName = Username,
                Password = Password,
                AlwaysSendBasicAuthHeader = alwaysSend,
            };
        }

        protected override IServiceClient GetClientWithApiKey()
        {
            return new JsonHttpClient(ListeningOn)
            {
                Credentials = new NetworkCredential(ApiKey.Key, ""),
            };
        }

        protected override IServiceClient GetClientWithApiKeyBearerToken()
        {
            return new JsonHttpClient(ListeningOn)
            {
                BearerToken = ApiKey.Key,
            };
        }

        protected override IServiceClient GetClient()
        {
            return new JsonHttpClient(ListeningOn);
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

        public const string Username = "user";
        public const string Password = "p@55word";

        protected virtual IServiceClient GetClientWithUserPassword(bool alwaysSend = false)
        {
            return new JsonServiceClient(ListeningOn)
            {
                UserName = Username,
                Password = Password,
                AlwaysSendBasicAuthHeader = alwaysSend,
            };
        }

        protected virtual IServiceClient GetClientWithApiKey()
        {
            return new JsonServiceClient(ListeningOn)
            {
                Credentials = new NetworkCredential(ApiKey.Key, ""),
            };
        }

        protected virtual IServiceClient GetClientWithApiKeyBearerToken()
        {
            return new JsonServiceClient(ListeningOn)
            {
                BearerToken = ApiKey.Key,
            };
        }

        protected virtual IServiceClient GetClient()
        {
            return new JsonServiceClient(ListeningOn);
        }

        [Test]
        public void Authenticating_once_with_BasicAuth_does_not_establish_auth_session()
        {
            var client = GetClientWithUserPassword(alwaysSend:true);

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
        public async Task Authenticating_once_with_ApiKeyAuth_does_not_establish_auth_session_Async()
        {
            var client = GetClientWithApiKey();

            var request = new Secured { Name = "test" };
            var response = await client.SendAsync<SecuredResponse>(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            var newClient = GetClient();
            newClient.SetSessionId(client.GetSessionId());
            try
            {
                response = await newClient.SendAsync<SecuredResponse>(request);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            }
        }

        [Test]
        public void Authenticating_once_with_ApiKeyAuth_BearerToken_does_not_establish_auth_session()
        {
            var client = GetClientWithApiKeyBearerToken();

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
        public async Task Authenticating_once_with_ApiKeyAuth_BearerToken_does_not_establish_auth_session_Async()
        {
            var client = GetClientWithApiKeyBearerToken();

            var request = new Secured { Name = "test" };
            var response = await client.SendAsync<SecuredResponse>(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            var newClient = GetClient();
            newClient.SetSessionId(client.GetSessionId());
            try
            {
                response = await newClient.SendAsync<SecuredResponse>(request);
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
        public async Task Can_access_Secured_Pages_with_ApiKeyAuth_async()
        {
            Assert.That(await ListeningOn.CombineWith("/secured").GetStringFromUrlAsync(
                requestFilter: req => req.AddApiKeyAuth(ApiKey.Key)),
                Is.StringContaining("<!--view:Secured.cshtml-->"));

            Assert.That(await ListeningOn.CombineWith("/SecuredPage").GetStringFromUrlAsync(
                requestFilter: req => req.AddApiKeyAuth(ApiKey.Key)),
                Is.StringContaining("<!--page:SecuredPage.cshtml-->"));
        }

        [Test]
        public void Can_access_Secured_Pages_with_ApiKeyAuth_BearerToken()
        {
            Assert.That(ListeningOn.CombineWith("/secured").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(ApiKey.Key)),
                Is.StringContaining("<!--view:Secured.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/SecuredPage").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(ApiKey.Key)),
                Is.StringContaining("<!--page:SecuredPage.cshtml-->"));
        }

        [Test]
        public async Task Can_access_Secured_Pages_with_ApiKeyAuth_BearerToken_Async()
        {
            Assert.That(await ListeningOn.CombineWith("/secured").GetStringFromUrlAsync(
                requestFilter: req => req.AddBearerToken(ApiKey.Key)),
                Is.StringContaining("<!--view:Secured.cshtml-->"));

            Assert.That(await ListeningOn.CombineWith("/SecuredPage").GetStringFromUrlAsync(
                requestFilter: req => req.AddBearerToken(ApiKey.Key)),
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