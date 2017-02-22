using Funq;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class CompressData : IReturn<CompressData>
    {
        public string String { get; set; }
        public byte[] Bytes { get; set; }
    }

    public class CompressString : IReturn<string>
    {
        public string String { get; set; }
    }

    public class CompressBytes : IReturn<byte[]>
    {
        public byte[] Bytes { get; set; }
    }

    [CompressResponse]
    public class CompressedServices : Service
    {
        public object Any(CompressData request) => request;
        public object Any(CompressString request) => request.String;
        public object Any(CompressBytes request) => request.Bytes;
    }

    public class CompressResponseTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() 
                : base(nameof(CompressResponseTests), typeof(CompressedServices).GetAssembly()) {}

            public override void Configure(Container container) {}
        }

        private ServiceStackHost appHost;

        public CompressResponseTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Does_compress_RequestDto_responses()
        {
            var client = new JsonServiceClient(Config.ListeningOn);
            var response = client.Post(new CompressData
            {
                String = "Hello",
                Bytes = "World".ToUtf8Bytes()
            });

            Assert.That(response.String, Is.EqualTo("Hello"));
            Assert.That(response.Bytes, Is.EqualTo("World".ToUtf8Bytes()));
        }

        [Test]
        public void Does_compress_raw_String_responses()
        {
            var client = new JsonServiceClient(Config.ListeningOn);
            var response = client.Post(new CompressString
            {
                String = "foo",
            });

            Assert.That(response, Is.EqualTo("foo"));
        }

        [Test]
        public void Does_compress_raw_Bytes_responses()
        {
            var client = new JsonServiceClient(Config.ListeningOn);
            var response = client.Post(new CompressBytes
            {
                Bytes = "foo".ToUtf8Bytes(),
            });

            Assert.That(response, Is.EquivalentTo("foo".ToUtf8Bytes()));
        }
    }
}