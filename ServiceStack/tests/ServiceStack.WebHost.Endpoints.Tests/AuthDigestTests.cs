using System;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class AuthDigestTests
    {
        protected virtual string VirtualDirectory { get { return ""; } }
        protected virtual string ListeningOn { get { return "http://localhost:1337/"; } }
        protected virtual string WebHostUrl { get { return "http://mydomain.com"; } }

        public class AuthDigestAppHost : AuthAppHost
        {
            public AuthDigestAppHost(string webHostUrl, Action<Container> configureFn = null)
                : base(webHostUrl, configureFn)
            { }

            public override IAuthProvider[] GetAuthProviders()
            {
                return new[] { new DigestAuthProvider(AppSettings) };
            }
        }

        ServiceStackHost appHost;
        [OneTimeSetUp]
        public void OnTestFixtureSetUp() =>
            appHost = new AuthDigestAppHost(WebHostUrl, Configure)
                .Init()
                .Start(ListeningOn);

        [OneTimeTearDown]
        public void OnTestFixtureTearDown() => appHost.Dispose();

        public virtual void Configure(Container container) { }

        IServiceClient GetClientWithUserPassword()
        {
            return new JsonServiceClient(ListeningOn)
            {
                UserName = AuthTests.UserName,
                Password = AuthTests.Password
            };
        }

        [Test]
        public void Does_work_with_DigestAuth()
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
        public async Task Does_work_with_DigestAuth_Async()
        {
            try
            {
                var client = GetClientWithUserPassword();
                var request = new Secured { Name = "test" };
                var response = await client.SendAsync<SecureResponse>(request);
                Assert.That(response.Result, Is.EqualTo(request.Name));
            }
            catch (WebServiceException webEx)
            {
                Assert.Fail(webEx.Message);
            }
        }
    }
}