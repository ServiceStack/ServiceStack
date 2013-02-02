using Funq;
using NUnit.Framework;
using ServiceStack.ServiceInterface.Admin;
using ServiceStack.Text;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class BufferedRequestTests
    {
        private BufferedRequestAppHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new BufferedRequestAppHost();
            appHost.Init();
            appHost.Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
            appHost = null;
        }

        [Test]
        public void BufferredRequest_allows_rereading_of_Request_InputStream()
        {
            appHost.LastRequestBody = null;
            appHost.UseBufferredStream = true;

            var client = new JsonServiceClient(Config.ServiceStackBaseUri);
            var request = new MyRequest { Data = "RequestData" };
            var response = client.Post(request);

            Assert.That(response.Data, Is.EqualTo(request.Data));
            Assert.That(appHost.LastRequestBody, Is.EqualTo(request.ToJson()));
        }

        [Test]
        public void Cannot_reread_Request_InputStream_without_bufferring()
        {
            appHost.LastRequestBody = null;
            appHost.UseBufferredStream = false;

            var client = new JsonServiceClient(Config.ServiceStackBaseUri);
            var request = new MyRequest { Data = "RequestData" };

            var response = client.Post(request);

            Assert.That(appHost.LastRequestBody, Is.EqualTo(request.ToJson()));
            Assert.That(response.Data, Is.Null);
        }

        [Test]
        public void Cannot_see_RequestBody_in_RequestLogger_without_bufferring()
        {
            appHost.LastRequestBody = null;
            appHost.UseBufferredStream = false;

            var client = new JsonServiceClient(Config.ServiceStackBaseUri);
            var request = new MyRequest { Data = "RequestData" };

            var response = client.Post(request);

            Assert.That(appHost.LastRequestBody, Is.EqualTo(request.ToJson()));
            Assert.That(response.Data, Is.Null);

            var requestLogger = appHost.TryResolve<IRequestLogger>();
            var lastEntry = requestLogger.GetLatestLogs(1);
            Assert.That(lastEntry[0].RequestBody, Is.Null);
        }
    }

    [TestFixture]
    public class BufferedRequestLoggerTests
    {
        private BufferedRequestAppHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new BufferedRequestAppHost { EnableRequestBodyTracking = true };
            appHost.Init();
            appHost.Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
            appHost = null;
        }

        [Test]
        public void Can_see_RequestBody_in_RequestLogger_when_EnableRequestBodyTracking()
        {
            appHost.LastRequestBody = null;
            appHost.UseBufferredStream = false;

            var client = new JsonServiceClient(Config.ServiceStackBaseUri);
            var request = new MyRequest { Data = "RequestData" };

            var response = client.Post(request);

            Assert.That(appHost.LastRequestBody, Is.EqualTo(request.ToJson()));
            Assert.That(response.Data, Is.EqualTo(request.Data));

            var requestLogger = appHost.TryResolve<IRequestLogger>();
            var lastEntry = requestLogger.GetLatestLogs(1);
            Assert.That(lastEntry[0].RequestBody, Is.EqualTo(request.ToJson()));
        }
    }

    public class BufferedRequestAppHost : AppHostHttpListenerBase
    {
        public BufferedRequestAppHost() : base(typeof(BufferedRequestTests).Name, typeof(MyService).Assembly) { }

        public string LastRequestBody { get; set; }
        public bool UseBufferredStream { get; set; }
        public bool EnableRequestBodyTracking { get; set; }

        public override void Configure(Container container)
        {
            PreRequestFilters.Add((httpReq, httpRes) => {
                if (UseBufferredStream)
                    httpReq.UseBufferedStream = UseBufferredStream;

                LastRequestBody = null;
                LastRequestBody = httpReq.GetRawBody();
            });

            Plugins.Add(new RequestLogsFeature { EnableRequestBodyTracking = EnableRequestBodyTracking });
        }
    }

    public class MyRequest : IReturn<MyRequest>
    {
        public string Data { get; set; }
    }

    public class MyService : IService
    {
        public object Any(MyRequest request)
        {
            return request;
        }
    }
}
