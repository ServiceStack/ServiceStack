using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [DataContract]
    [Route("/hellozip")]
    public class HelloZip : IReturn<HelloZipResponse>
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<string> Test { get; set; }
    }

    [DataContract]
    public class HelloZipResponse
    {
        [DataMember]
        public string Result { get; set; }
    }

    public class HelloZipService : IService
    {
        public object Any(HelloZip request)
        {
            return request.Test == null
                ? new HelloZipResponse { Result = $"Hello, {request.Name}" }
                : new HelloZipResponse { Result = $"Hello, {request.Name} ({request.Test?.Count})" };
        }
    }

    [TestFixture]
    public class ZipServiceClientTests
    {
        private readonly ServiceStackHost appHost;

        public ZipServiceClientTests()
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
                : base(nameof(ZipServiceClientTests), typeof(HelloZipService).Assembly) { }

            public override void Configure(Container container) {}
        }

        [Test]
        public void Can_send_GZip_client_request_list()
        {
            var client = new JsonServiceClient(Config.ListeningOn)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = client.Post(new HelloZip
            {
                Name = "GZIP",
                Test = new List<string> { "Test" }
            });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP (1)"));
        }

        [Test]
        public async Task Can_send_GZip_client_request_list_async()
        {
            var client = new JsonServiceClient(Config.ListeningOn)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = await client.PostAsync(new HelloZip
            {
                Name = "GZIP",
                Test = new List<string> { "Test" }
            });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP (1)"));
        }

        [Test]
        public void Can_send_GZip_client_request_list_HttpClient()
        {
            var client = new JsonHttpClient(Config.ListeningOn)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = client.Post(new HelloZip
            {
                Name = "GZIP",
                Test = new List<string> { "Test" }
            });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP (1)"));
        }

        [Test]
        public async Task Can_send_GZip_client_request_list_HttpClient_async()
        {
            var client = new JsonHttpClient(Config.ListeningOn)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = await client.PostAsync(new HelloZip
            {
                Name = "GZIP",
                Test = new List<string> { "Test" }
            });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP (1)"));
        }

        [Test]
        public void Can_send_GZip_client_request()
        {
            var client = new JsonServiceClient(Config.ListeningOn)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = client.Post(new HelloZip { Name = "GZIP" });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP"));
        }

        [Test]
        public void Can_send_Deflate_client_request()
        {
            var client = new JsonServiceClient(Config.ListeningOn)
            {
                RequestCompressionType = CompressionTypes.Deflate,
            };
            var response = client.Post(new HelloZip { Name = "Deflate" });
            Assert.That(response.Result, Is.EqualTo("Hello, Deflate"));
        }

        [Test]
        public void Can_send_GZip_client_request_HttpClient()
        {
            var client = new JsonHttpClient(Config.ListeningOn)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = client.Post(new HelloZip { Name = "GZIP" });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP"));
        }

        [Test]
        public void Can_send_Deflate_client_request_HttpClient()
        {
            var client = new JsonHttpClient(Config.ListeningOn)
            {
                RequestCompressionType = CompressionTypes.Deflate,
            };
            var response = client.Post(new HelloZip { Name = "Deflate" });
            Assert.That(response.Result, Is.EqualTo("Hello, Deflate"));
        }

        [Ignore("Integration Test"), Test]
        public void Can_send_gzip_client_request_ASPNET()
        {
            var client = new JsonServiceClient(Config.AspNetServiceStackBaseUri)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = client.Post(new HelloZip { Name = "GZIP" });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP"));
        }



        [TestCase(CompressionTypes.Deflate)]
        [TestCase(CompressionTypes.GZip)]
        public async Task Can_send_async_compressed_client_request(string compressionType)
        {
            var client = new JsonServiceClient(Config.ListeningOn)
            {
                RequestCompressionType = compressionType,
            };
            var response = await client.PostAsync(new HelloZip { Name = compressionType });
            Assert.That(response.Result, Is.EqualTo($"Hello, {compressionType}"));
        }
    }
}