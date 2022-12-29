using System.Globalization;
using Funq;
using NUnit.Framework;
using ServiceStack.Common.Tests;
using ServiceStack.Host;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.Issues
{
    [Route("/test/{Id}")]
    public class LocalizationTest : IReturn<LocalizationTest>
    {
        public string Id { get; set; }
        public string Data { get; set; }
    }

    public class LocalizationTestService : Service
    {
        public object Any(LocalizationTest request) => request;
    }

    public class LocalizationIssues
    {
        public class AppHost : AppSelfHostBase
        {
            public AppHost() : base("Test", typeof(AppHost).Assembly) { }
            public override void Configure(Container container) { }
        }

        private ServiceStackHost appHost;

        public LocalizationIssues()
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            appHost.Dispose();
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        [Test]
        public void Can_resolve_routes_in_Turkish_Culture()
        {
            var restPath = new RestPath(typeof(LocalizationTest), "/test/{Id}");

            foreach (var varName in new[] {"id", "ID", "Id", "iD"})
            {
                //$"IsVariable({varName}) = {restPath.IsVariable(varName)}".Print();
                Assert.That(restPath.IsVariable(varName));
            }

            var request = restPath.CreateRequest("/test/3");
        }

        [Test]
        public void Can_call_Service_in_Turkish_Culture()
        {
            var client = new JsonServiceClient(Config.ListeningOn);
            var request = new LocalizationTest { Id = "foo" };
            var response = client.Get(request);
            Assert.That(response.Id, Is.EqualTo(request.Id));
        }
    }
}