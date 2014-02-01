using Funq;
using NUnit.Framework;
using ServiceStack.Host.Handlers;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class AppHostListenerEchoRequestTests
    {
        public class AppHost : AppHostHttpListenerBase
        {
            public AppHost()
                : base("Echo AppHost", typeof(AppHost).Assembly) { }

            public override void Configure(Container container) {}
        }

        [Route("/echo")]
        [Route("/echo/{PathInfoParam}")]
        public class Echo : IReturn<Echo>
        {
            public string Param { get; set; }
            public string PathInfoParam { get; set; }
        }

        public class EchoService : Service
        {
            public Echo Any(Echo request)
            {
                return request;
            }

            public RequestInfoResponse Any(RequestInfo request)
            {
                var requestInfo = RequestInfoHandler.GetRequestInfo(base.Request);
                return requestInfo;
            }
        }

        private AppHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new AppHost();
            appHost.Init();
            appHost.Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Does_url_decode_raw_QueryString()
        {
            var testEncoding = "test://?&% encoding";
            var url = "{0}echo?Param={1}".Fmt(Config.AbsoluteBaseUri, testEncoding.UrlEncode());
            Assert.That(url, Is.StringEnding("/echo?Param=test%3a%2f%2f%3f%26%25+encoding"));

            var json = url.GetJsonFromUrl();
            var response = json.FromJson<Echo>();
            Assert.That(response.Param, Is.EqualTo(testEncoding));
        }

        [Test]
        public void Does_url_decode_raw_PathInfo()
        {
            var testEncoding = "test encoding";
            var url = "{0}echo/{1}".Fmt(Config.AbsoluteBaseUri, testEncoding.UrlEncode());
            Assert.That(url, Is.StringEnding("/echo/test+encoding"));

            var json = url.GetJsonFromUrl();
            var response = json.FromJson<Echo>();
            Assert.That(response.PathInfoParam, Is.EqualTo(testEncoding));
        }

        [Test]
        public void Does_url_transparently_decode_QueryString()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);
            var request = new Echo { Param = "test://?&% encoding" };
            var response = client.Get(request);
            Assert.That(response.Param, Is.EqualTo(request.Param));
        }

        [Test]
        public void Does_url_transparently_decode_PathInfo()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);
            var request = new Echo { PathInfoParam = "test%2Fpath:?&% encoding" };
            var response = client.Get(request);
            Assert.That(response.PathInfoParam, Is.EqualTo(request.PathInfoParam));
        }

        [Test]
        public void Does_url_transparently_decode_RequestBody()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);
            var request = new Echo { Param = "test://?&% encoding" };
            var response = client.Post(request);
            Assert.That(response.Param, Is.EqualTo(request.Param));
        }

    }
}