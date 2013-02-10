using System.IO;
using Funq;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.MiniProfiler.UI;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/request/{Id}")]
    public class ById
    {
        public string Id { get; set; }
    }

    public class UniqueRequestService : IService
    {
        public string Get(ById byId)
        {
            return byId.Id;
        }
    }

    public class UniqueRequestAppHost : AppHostHttpListenerBase
    {
        public UniqueRequestAppHost() : base("Unique Request Tests", typeof(UniqueRequestService).Assembly) {}
        public override void Configure(Container container) {}
    }

    [TestFixture]
    public class UniqueRequestTests
    {
        private const string BaseUri = "http://localhost:8000";
        private UniqueRequestAppHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new UniqueRequestAppHost();
            appHost.Init();
            appHost.Start(BaseUri + "/");
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
            appHost = null;
        }

        [Test]
        public void Can_handle_encoded_chars()
        {
            var response = BaseUri.CombineWith("request/123%20456").GetStringFromUrl();
            Assert.That(response, Is.EqualTo("123 456"));
            response = BaseUri.CombineWith("request/123%7C456").GetStringFromUrl();
            Assert.That(response, Is.EqualTo("123|456"));
        }
    }
}
