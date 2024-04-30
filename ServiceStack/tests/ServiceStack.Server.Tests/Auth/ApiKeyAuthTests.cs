using System.Linq;
using System.Net;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.Server.Tests.Auth
{
    public class RequiresAuth : IReturn<RequiresAuth>, IHasBearerToken
    {
        public string Name { get; set; }
        public string BearerToken { get; set; }
    }

    [Authenticate]
    public class RequiresAuthService : Service
    {
        public static IApiKey LastApiKey;

        public object Any(RequiresAuth request)
        {
            LastApiKey = base.Request.GetApiKey();
            return request;
        }
    }

    [Route("/requires-auth")]
    public class RequiresAuthAction : IReturn<RequiresAuthAction>
    {
        public string Name { get; set; }
    }

    public class RequiresAuthActionService : Service
    {
        public static IApiKey LastApiKey;

        [Authenticate]
        public object Any(RequiresAuthAction request)
        {
            LastApiKey = base.Request.GetApiKey();
            return request;
        }
    }

    [TestFixture]
    public class ApiKeyAuthTests
    {
        class AppHost : AppSelfHostBase
        {
            public static IApiKey LastApiKey;

            public AppHost() : base(nameof(ApiKeyAuthTests), typeof(AppHost).Assembly) { }

            public override void Configure(Container container)
            {
                var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
                container.Register<IDbConnectionFactory>(dbFactory);

                container.Register<IAuthRepository>(c => 
                    new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()));
                container.Resolve<IAuthRepository>().InitSchema();

                Plugins.Add(new AuthFeature(() => new AuthUserSession(),
                    new IAuthProvider[] {
                        new ApiKeyAuthProvider(AppSettings) { RequireSecureConnection = false },
                    })
                {
                    IncludeRegistrationService = true,
                });

                GlobalRequestFilters.Add((req, res, dto) =>
                {
                    LastApiKey = req.GetApiKey();
                });
            }
        }

        public const string ListeningOn = "http://localhost:2337/";
        public const string Username = "user";
        public const string Password = "p@55word";
        private ServiceStackHost appHost;
        protected IManageApiKeys apiRepo;
        private string userId;
        private ApiKey liveKey;
        private ApiKey testKey;

        public ApiKeyAuthTests()
        {
            //System.Diagnostics.Debugger.Break();
            appHost = new AppHost()
               .Init()
               .Start("http://*:2337/");

            var client = new JsonServiceClient(ListeningOn);
            var response = client.Post(new Register
            {
                UserName = Username,
                Password = Password,
                Email = "as@if{0}.com",
                DisplayName = "DisplayName",
                FirstName = "FirstName",
                LastName = "LastName",
            });

            userId = response.UserId;
            apiRepo = (IManageApiKeys)appHost.Resolve<IAuthRepository>();
            var apiKeys = apiRepo.GetUserApiKeys(userId);
            liveKey = apiKeys.First(x => x.Environment == "live");
            testKey = apiKeys.First(x => x.Environment == "test");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Does_return_APIKey_for_ApiKey_request_in_GlobalRequestFilters()
        {
            AppHost.LastApiKey = null;
            RequiresAuthService.LastApiKey = null;

            var client = new JsonServiceClient(ListeningOn)
            {
                Credentials = new NetworkCredential(liveKey.Id, ""),
            };

            var request = new RequiresAuth { Name = "foo" };
            var response = client.Send(request);
            Assert.That(response.Name, Is.EqualTo(request.Name));

            Assert.That(AppHost.LastApiKey.Id, Is.EqualTo(liveKey.Id));
            Assert.That(RequiresAuthService.LastApiKey.Id, Is.EqualTo(liveKey.Id));
        }

        [Test]
        public void Does_return_APIKey_for_ApiKey_request_in_GlobalRequestFilters_Action()
        {
            RequiresAuthActionService.LastApiKey = null;

            var client = new JsonServiceClient(ListeningOn)
            {
                Credentials = new NetworkCredential(liveKey.Id, ""),
            };

            var request = new RequiresAuthAction { Name = "foo" };
            var response = client.Send(request);
            Assert.That(response.Name, Is.EqualTo(request.Name));

            Assert.That(RequiresAuthActionService.LastApiKey.Id, Is.EqualTo(liveKey.Id));
        }

        [Test]
        public void Does_allow_ApiKey_in_IHasBearerToken_RequestDto()
        {
            AppHost.LastApiKey = null;
            RequiresAuthService.LastApiKey = null;

            var client = new JsonServiceClient(ListeningOn);

            var request = new RequiresAuth { BearerToken = liveKey.Id, Name = "foo" };
            var response = client.Send(request);
            Assert.That(response.Name, Is.EqualTo(request.Name));

            Assert.That(AppHost.LastApiKey.Id, Is.EqualTo(liveKey.Id));
            Assert.That(RequiresAuthService.LastApiKey.Id, Is.EqualTo(liveKey.Id));
        }

        [Test]
        public void Does_not_allow_ApiKey_in_QueryString()
        {
            var url = ListeningOn.CombineWith("/requires-auth").AddQueryParam("apikey", liveKey.Id);

            try
            {
                var json = url.GetJsonFromUrl();
                Assert.Fail("Should throw");
            }
            catch (WebException ex)
            {
                Assert.That(ex.GetStatus().Value, Is.EqualTo(HttpStatusCode.Unauthorized));
            }
        }

        [Test]
        public void Does_allow_ApiKey_in_QueryString_when_AllowInHttpParams()
        {
            var apiKeyAuth = (ApiKeyAuthProvider)AuthenticateService.GetAuthProvider(ApiKeyAuthProvider.Name);
            apiKeyAuth.AllowInHttpParams = true;

            var url = ListeningOn.CombineWith("/requires-auth").AddQueryParam("apikey", liveKey.Id);
            var json = url.GetJsonFromUrl();

            Assert.That(json, Is.Not.Null);

            apiKeyAuth.AllowInHttpParams = false;
        }
    }
}