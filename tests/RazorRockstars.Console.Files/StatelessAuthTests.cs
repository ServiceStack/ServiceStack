using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace RazorRockstars.Console.Files
{
    public class CustomUserSession : AuthUserSession
    {
        public override void OnAuthenticated(IServiceBase authService, 
            IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
        }

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

        ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            LogManager.LogFactory = new ConsoleLogFactory();
            appHost = new AppHost { EnableAuth = true }
                .Init()
                .Start("http://*:2337/");
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

        IServiceClient GetClient()
        {
            return new JsonServiceClient(ListeningOn);
        }

        IServiceClient GetClientWithUserPassword()
        {
            return new JsonServiceClient(ListeningOn) {
                UserName = Username,
                Password = Password
            };
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
        public void Authenticating_once_with_BasicAuth_does_not_establish_auth_session()
        {
            var client = (ServiceClientBase)GetClientWithUserPassword();
            client.AlwaysSendBasicAuthHeader = true;
            client.RequestFilter = req =>
                Assert.That(req.Headers[HttpHeaders.Authorization], Is.Not.Null);

            var request = new Secured { Name = "test" };
            var response = client.Send<SecuredResponse>(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            var nonBasicAuthClient = GetClient();
            nonBasicAuthClient.SetSessionId(client.GetSessionId());
            try
            {
                response = nonBasicAuthClient.Send<SecuredResponse>(request);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            }
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
        public void Can_access_Secured_ViewPage_with_Authentication()
        {
            Assert.That(ListeningOn.CombineWith("/secured").GetStringFromUrl(
                requestFilter: req => req.AddBasicAuth(Username, Password)),
                Is.StringContaining("<!--view:Secured.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/SecuredPage").GetStringFromUrl(
                requestFilter: req => req.AddBasicAuth(Username, Password)),
                Is.StringContaining("<!--page:SecuredPage.cshtml-->"));
        }

    }
}