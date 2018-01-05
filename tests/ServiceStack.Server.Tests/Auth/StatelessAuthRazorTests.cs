﻿using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Data;

namespace ServiceStack.Server.Tests.Auth
{
#if NETCORE_SUPPORT
    [Ignore("Not working on .NET Core")]
#endif
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

        [OneTimeTearDown]
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
                Does.Contain("<!--page:Login.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/SecuredPage").GetStringFromUrl(),
                Does.Contain("<!--page:Login.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/TestSessionPage").GetStringFromUrl(),
                Does.Contain("IsAuthenticated:False"));

            Assert.That(ListeningOn.CombineWith("/test/session/view").GetStringFromUrl(),
                Does.Contain("IsAuthenticated:False"));
        }

        [Test]
        public void Can_access_Secured_Pages_with_BasicAuth()
        {
            Assert.That(ListeningOn.CombineWith("/secured").GetStringFromUrl(
                requestFilter: req => req.AddBasicAuth(Username, Password)),
                Does.Contain("<!--view:Secured.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/SecuredPage").GetStringFromUrl(
                requestFilter: req => req.AddBasicAuth(Username, Password)),
                Does.Contain("<!--page:SecuredPage.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/test/session").GetJsonFromUrl(
                requestFilter: req => req.AddBasicAuth(Username, Password)),
                Does.Contain("\"IsAuthenticated\":true"));

            Assert.That(ListeningOn.CombineWith("/TestSessionPage").GetStringFromUrl(
                requestFilter: req => req.AddBasicAuth(Username, Password)),
                Does.Contain("IsAuthenticated:True"));

            Assert.That(ListeningOn.CombineWith("/test/session/view").GetStringFromUrl(
                requestFilter: req => req.AddBasicAuth(Username, Password)),
                Does.Contain("IsAuthenticated:True"));
        }

        [Test]
        public void Can_access_Secured_Pages_with_JWT()
        {
            var client = GetClientWithUserPassword(alwaysSend: true);
            var authResponse = client.Send(new Authenticate());

            Assert.That(ListeningOn.CombineWith("/secured").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(authResponse.BearerToken)),
                Does.Contain("<!--view:Secured.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/SecuredPage").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(authResponse.BearerToken)),
                Does.Contain("<!--page:SecuredPage.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/test/session").GetJsonFromUrl(
                requestFilter: req => req.AddBearerToken(authResponse.BearerToken)),
                Does.Contain("\"IsAuthenticated\":true"));

            Assert.That(ListeningOn.CombineWith("/TestSessionPage").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(authResponse.BearerToken)),
                Does.Contain("IsAuthenticated:True"));

            Assert.That(ListeningOn.CombineWith("/test/session/view").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(authResponse.BearerToken)),
                Does.Contain("IsAuthenticated:True"));
        }

        [Test]
        public void Can_access_Secured_Pages_with_ApiKeyAuth()
        {
            Assert.That(ListeningOn.CombineWith("/secured").GetStringFromUrl(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Does.Contain("<!--view:Secured.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/SecuredPage").GetStringFromUrl(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Does.Contain("<!--page:SecuredPage.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/test/session").GetJsonFromUrl(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Does.Contain("\"IsAuthenticated\":true"));

            Assert.That(ListeningOn.CombineWith("/TestSessionPage").GetStringFromUrl(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Does.Contain("IsAuthenticated:True"));

            Assert.That(ListeningOn.CombineWith("/test/session/view").GetStringFromUrl(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Does.Contain("IsAuthenticated:True"));
        }

        [Test]
        public async Task Can_access_Secured_Pages_with_ApiKeyAuth_async()
        {
            Assert.That(await ListeningOn.CombineWith("/secured").GetStringFromUrlAsync(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Does.Contain("<!--view:Secured.cshtml-->"));

            Assert.That(await ListeningOn.CombineWith("/SecuredPage").GetStringFromUrlAsync(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Does.Contain("<!--page:SecuredPage.cshtml-->"));

            Assert.That(await ListeningOn.CombineWith("/test/session").GetJsonFromUrlAsync(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Does.Contain("\"IsAuthenticated\":true"));

            Assert.That(await ListeningOn.CombineWith("/TestSessionPage").GetStringFromUrlAsync(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Does.Contain("IsAuthenticated:True"));

            Assert.That(await ListeningOn.CombineWith("/test/session/view").GetStringFromUrlAsync(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Does.Contain("IsAuthenticated:True"));
        }

        [Test]
        public void Can_access_Secured_Pages_with_ApiKeyAuth_BearerToken()
        {
            Assert.That(ListeningOn.CombineWith("/secured").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(ApiKey)),
                Does.Contain("<!--view:Secured.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/SecuredPage").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(ApiKey)),
                Does.Contain("<!--page:SecuredPage.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/test/session").GetJsonFromUrl(
                requestFilter: req => req.AddApiKeyAuth(ApiKey)),
                Does.Contain("\"IsAuthenticated\":true"));

            Assert.That(ListeningOn.CombineWith("/TestSessionPage").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(ApiKey)),
                Does.Contain("IsAuthenticated:True"));

            Assert.That(ListeningOn.CombineWith("/test/session/view").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(ApiKey)),
                Does.Contain("IsAuthenticated:True"));
        }

        [Test]
        public async Task Can_access_Secured_Pages_with_ApiKeyAuth_BearerToken_Async()
        {
            Assert.That(await ListeningOn.CombineWith("/secured").GetStringFromUrlAsync(
                requestFilter: req => req.AddBearerToken(ApiKey)),
                Does.Contain("<!--view:Secured.cshtml-->"));

            Assert.That(await ListeningOn.CombineWith("/SecuredPage").GetStringFromUrlAsync(
                requestFilter: req => req.AddBearerToken(ApiKey)),
                Does.Contain("<!--page:SecuredPage.cshtml-->"));

            Assert.That(await ListeningOn.CombineWith("/test/session").GetJsonFromUrlAsync(
                requestFilter: req => req.AddBearerToken(ApiKey)),
                Does.Contain("\"IsAuthenticated\":true"));

            Assert.That(await ListeningOn.CombineWith("/TestSessionPage").GetStringFromUrlAsync(
                requestFilter: req => req.AddBearerToken(ApiKey)),
                Does.Contain("IsAuthenticated:True"));

            Assert.That(await ListeningOn.CombineWith("/test/session/view").GetStringFromUrlAsync(
                requestFilter: req => req.AddBearerToken(ApiKey)),
                Does.Contain("IsAuthenticated:True"));
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
                Does.Contain("<!--view:Secured.cshtml-->"));

            Assert.That(client.Get<string>("/SecuredPage?format=html"),
                Does.Contain("<!--page:SecuredPage.cshtml-->"));

            Assert.That(client.Get(new TestSession()).IsAuthenticated);

            Assert.That(client.Get<string>("/test/session"),
                Does.Contain("\"IsAuthenticated\":true"));

            Assert.That(client.Get<string>("/TestSessionPage"),
                Does.Contain("IsAuthenticated:True"));
        }
    }
}
