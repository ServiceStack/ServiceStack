using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using Funq;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Web;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class GzipJsonServiceClientTests
    {
        private readonly ServiceStackHost appHost;

        public GzipJsonServiceClientTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(nameof(GzipJsonServiceClientTests), typeof(HelloService).GetAssembly()) { }

            public override void Configure(Container container) {}
        }

        [Test]
        public void Can_send_GZip_client_request()
        {
            var client = new JsonServiceClient(Config.ListeningOn)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = client.Post(new Hello { Name = "GZIP" });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP"));
        }

        [Test]
        public void Can_send_Deflate_client_request()
        {
            var client = new JsonServiceClient(Config.ListeningOn)
            {
                RequestCompressionType = CompressionTypes.Deflate,
            };
            var response = client.Post(new Hello { Name = "Deflate" });
            Assert.That(response.Result, Is.EqualTo("Hello, Deflate"));
        }

        [Test]
        public void Can_send_GZip_client_request_HttpClient()
        {
            var client = new JsonHttpClient(Config.ListeningOn)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = client.Post(new Hello { Name = "GZIP" });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP"));
        }

        [Test]
        public void Can_send_Deflate_client_request_HttpClient()
        {
            var client = new JsonHttpClient(Config.ListeningOn)
            {
                RequestCompressionType = CompressionTypes.Deflate,
            };
            var response = client.Post(new Hello { Name = "Deflate" });
            Assert.That(response.Result, Is.EqualTo("Hello, Deflate"));
        }

        [Explicit, Test]
        public void Can_send_gzip_client_request_ASPNET()
        {
            var client = new JsonServiceClient(Config.AspNetServiceStackBaseUri)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = client.Post(new Hello { Name = "GZIP" });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP"));
        }
    }
}