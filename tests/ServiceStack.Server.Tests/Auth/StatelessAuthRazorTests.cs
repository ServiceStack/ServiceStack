#if !NETSTANDARD1_6

using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Data;

namespace ServiceStack.Server.Tests.Auth
{
    public class OrmLiteStatelessAuthRazorTests : StatelessAuthRazorTests
    {
    }

    public abstract class StatelessAuthRazorTests
    {
        public const string ListeningOn = "http://localhost:2337/";

        protected readonly ServiceStackHost appHost;
        protected string ApiKey;
        public const string Username = "user";
        public const string Password = "p@55word";

        protected StatelessAuthRazorTests()
        {
            appHost = CreateAppHost()
               .Init()
               .Start("http://*:2337/");

            var client = GetClient();
            var response = client.Post(new Register
            {
                UserName = "user",
                Password = "p@55word",
                Email = "as@if{0}.com",
                DisplayName = "DisplayName",
                FirstName = "FirstName",
                LastName = "LastName",
            });

            var userId = response.UserId;
            var apiRepo = (IManageApiKeys)appHost.Resolve<IAuthRepository>();
            var user1Client = GetClientWithUserPassword(alwaysSend: true);
            ApiKey = user1Client.Get(new GetApiKeys { Environment = "live" }).Results[0].Key;
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        protected virtual ServiceStackHost CreateAppHost() =>
            new AppHost {
                Use = container => container.Register<IAuthRepository>(c =>
                    new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()))
            };

        protected virtual IServiceClient GetClient() => new JsonServiceClient(ListeningOn);

        protected virtual IServiceClient GetClientWithUserPassword(bool alwaysSend = false, string userName = null) => 
            new JsonServiceClient(ListeningOn) {
                UserName = userName ?? Username,
                Password = Password,
                AlwaysSendBasicAuthHeader = alwaysSend,
            };

        [Test]
        public void Can_not_access_Secured_Pages_without_Authentication()
        {
            Assert.That(ListeningOn.CombineWith("/secured").GetStringFromUrl(),
                Is.StringContaining("<!--page:Login.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/SecuredPage").GetStringFromUrl(),
                Is.StringContaining("<!--page:Login.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/TestSessionPage").GetStringFromUrl(),
                Is.StringContaining("IsAuthenticated:False"));

            Assert.That(ListeningOn.CombineWith("/test/session/view").GetStringFromUrl(),
                Is.StringContaining("IsAuthenticated:False"));
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

            Assert.That(ListeningOn.CombineWith("/test/session").GetJsonFromUrl(
                requestFilter: req => req.AddBasicAuth(Username, Password)),
                Is.StringContaining("\"IsAuthenticated\":true"));

            Assert.That(ListeningOn.CombineWith("/TestSessionPage").GetStringFromUrl(
                requestFilter: req => req.AddBasicAuth(Username, Password)),
                Is.StringContaining("IsAuthenticated:True"));

            Assert.That(ListeningOn.CombineWith("/test/session/view").GetStringFromUrl(
                requestFilter: req => req.AddBasicAuth(Username, Password)),
                Is.StringContaining("IsAuthenticated:True"));
        }

        [Test]
        public void Can_access_Secured_Pages_with_JWT()
        {
            var client = GetClientWithUserPassword(alwaysSend: true);
            var authResponse = client.Send(new Authenticate());

            Assert.That(ListeningOn.CombineWith("/secured").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(authResponse.BearerToken)),
                Is.StringContaining("<!--view:Secured.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/SecuredPage").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(authResponse.BearerToken)),
                Is.StringContaining("<!--page:SecuredPage.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/test/session").GetJsonFromUrl(
                requestFilter: req => req.AddBearerToken(authResponse.BearerToken)),
                Is.StringContaining("\"IsAuthenticated\":true"));

            Assert.That(ListeningOn.CombineWith("/TestSessionPage").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(authResponse.BearerToken)),
                Is.StringContaining("IsAuthenticated:True"));

            Assert.That(ListeningOn.CombineWith("/test/session/view").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(authResponse.BearerToken)),
                Is.StringContaining("IsAuthenticated:True"));
        }

        [Test]
        public void Can_access_Secured_Pages_with_ApiKeyAuth()
        {
            Assert.That(ListeningOn.CombineWith("/secured").GetStringFromUrl(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Is.StringContaining("<!--view:Secured.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/SecuredPage").GetStringFromUrl(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Is.StringContaining("<!--page:SecuredPage.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/test/session").GetJsonFromUrl(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Is.StringContaining("\"IsAuthenticated\":true"));

            Assert.That(ListeningOn.CombineWith("/TestSessionPage").GetStringFromUrl(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Is.StringContaining("IsAuthenticated:True"));

            Assert.That(ListeningOn.CombineWith("/test/session/view").GetStringFromUrl(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Is.StringContaining("IsAuthenticated:True"));
        }

        [Test]
        public async Task Can_access_Secured_Pages_with_ApiKeyAuth_async()
        {
            Assert.That(await ListeningOn.CombineWith("/secured").GetStringFromUrlAsync(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Is.StringContaining("<!--view:Secured.cshtml-->"));

            Assert.That(await ListeningOn.CombineWith("/SecuredPage").GetStringFromUrlAsync(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Is.StringContaining("<!--page:SecuredPage.cshtml-->"));

            Assert.That(await ListeningOn.CombineWith("/test/session").GetJsonFromUrlAsync(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Is.StringContaining("\"IsAuthenticated\":true"));

            Assert.That(await ListeningOn.CombineWith("/TestSessionPage").GetStringFromUrlAsync(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Is.StringContaining("IsAuthenticated:True"));

            Assert.That(await ListeningOn.CombineWith("/test/session/view").GetStringFromUrlAsync(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Is.StringContaining("IsAuthenticated:True"));
        }

        [Test]
        public void Can_access_Secured_Pages_with_ApiKeyAuth_BearerToken()
        {
            Assert.That(ListeningOn.CombineWith("/secured").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(ApiKey)),
                Is.StringContaining("<!--view:Secured.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/SecuredPage").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(ApiKey)),
                Is.StringContaining("<!--page:SecuredPage.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/test/session").GetJsonFromUrl(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Is.StringContaining("\"IsAuthenticated\":true"));

            Assert.That(ListeningOn.CombineWith("/TestSessionPage").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(ApiKey)),
                Is.StringContaining("IsAuthenticated:True"));

            Assert.That(ListeningOn.CombineWith("/test/session/view").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(ApiKey)),
                Is.StringContaining("IsAuthenticated:True"));
        }

        [Test]
        public async Task Can_access_Secured_Pages_with_ApiKeyAuth_BearerToken_Async()
        {
            Assert.That(await ListeningOn.CombineWith("/secured").GetStringFromUrlAsync(
                requestFilter: req => req.AddBearerToken(ApiKey)),
                Is.StringContaining("<!--view:Secured.cshtml-->"));

            Assert.That(await ListeningOn.CombineWith("/SecuredPage").GetStringFromUrlAsync(
                requestFilter: req => req.AddBearerToken(ApiKey)),
                Is.StringContaining("<!--page:SecuredPage.cshtml-->"));

            Assert.That(await ListeningOn.CombineWith("/test/session").GetJsonFromUrlAsync(
                requestFilter: req => req.AddBearerToken(ApiKey)),
                Is.StringContaining("\"IsAuthenticated\":true"));

            Assert.That(await ListeningOn.CombineWith("/TestSessionPage").GetStringFromUrlAsync(
                requestFilter: req => req.AddBearerToken(ApiKey)),
                Is.StringContaining("IsAuthenticated:True"));

            Assert.That(await ListeningOn.CombineWith("/test/session/view").GetStringFromUrlAsync(
                requestFilter: req => req.AddBearerToken(ApiKey)),
                Is.StringContaining("IsAuthenticated:True"));
        }

        [Test]
        public void Can_access_Secured_Pages_with_CredentialsAuth()
        {
            var client = GetClient();
            client.Post(new Authenticate
            {
                provider = "credentials",
                UserName = Username,
                Password = Password,
            });

            Assert.That(client.Get<string>("/secured?format=html"),
                Is.StringContaining("<!--view:Secured.cshtml-->"));

            Assert.That(client.Get<string>("/SecuredPage?format=html"),
                Is.StringContaining("<!--page:SecuredPage.cshtml-->"));

            Assert.That(client.Get(new TestSession()).IsAuthenticated);

            Assert.That(client.Get<string>("/test/session"),
                Is.StringContaining("\"IsAuthenticated\":true"));

            Assert.That(client.Get<string>("/TestSessionPage"),
                Is.StringContaining("IsAuthenticated:True"));
        }
    }
}

#endif
