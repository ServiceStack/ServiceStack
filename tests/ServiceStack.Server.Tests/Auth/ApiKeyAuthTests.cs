using System.Linq;
using System.Net;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.Server.Tests.Auth
{
    public class RequiresAuth : IReturn<RequiresAuth>
    {
        public string Name { get; set; }
    }
    
    [Authenticate]
    public class RequiresAuthService : Service
    {
        public object Any(RequiresAuth request) => request;
    }

    [TestFixture]
    public class ApiKeyAuthTests
    {
        class AppHost : AppSelfHostBase
        {
            public static ApiKey LastApiKey;

            public AppHost() : base(nameof(ApiKeyAuthTests), typeof(AppHost).GetAssembly()) { }

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

            var client = new JsonServiceClient(ListeningOn)
            {
                Credentials = new NetworkCredential(liveKey.Id, ""),
            };

            var request = new RequiresAuth { Name = "foo" };
            var response = client.Send(request);
            Assert.That(response.Name, Is.EqualTo(request.Name));

            Assert.That(AppHost.LastApiKey.Id, Is.EqualTo(liveKey.Id));
        }
    }
}