using System.Net;
using System.Text;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.Host.Handlers;
#if !NETCORE
using ServiceStack.Host.HttpListener;
#endif
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class AppHostListenerEchoRequestTests
    {
        public class AppHost : AppHostHttpListenerBase
        {
            public AppHost()
                : base("Echo AppHost", typeof(AppHost).Assembly)
            {
            }

            public override void Configure(Container container) { }

#if !NETCORE
            public override ListenerRequest CreateRequest(HttpListenerContext httpContext, string operationName)
            {
                var req = new ListenerRequest(httpContext, operationName, RequestAttributes.None)
                {
                    ContentEncoding = Encoding.UTF8
                };
                req.RequestAttributes = req.GetAttributes();
                return req;
            }
#endif
        }

        [Route("/echo")]
        [Route("/echo/{PathInfoParam}")]
        public class Echo : IReturn<Echo>
        {
            public string Param { get; set; }
            public string PathInfoParam { get; set; }
        }

        [Route("/customhtml")]
        public class CustomHtml {}

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

            public object Any(CustomHtml request)
            {
                return @"<!DOCTYPE html>
<html>
    <head>
        <title></title>
        <meta http-equiv='Content-Type' content='text/html; charset=utf-8'>
</head>
<body>
    <form action='/echo' method='POST'>
        <input name='Force' value='English' />
        <input name='Param' id='Param'/>
        <input type='submit' value='Send'/>
    </form>
</body>
</html>";
            }
        }

        private AppHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new AppHost();
            appHost.Init();
            appHost.Start(Config.AbsoluteBaseUri);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Does_url_decode_raw_QueryString()
        {
            var testEncoding = "test://?&% encoding";
            var url = "{0}echo?Param={1}".Fmt(Config.AbsoluteBaseUri, testEncoding.UrlEncode());
            Assert.That(url, Does.EndWith("/echo?Param=test%3a%2f%2f%3f%26%25+encoding"));

            var json = url.GetJsonFromUrl();
            var response = json.FromJson<Echo>();
            Assert.That(response.Param, Is.EqualTo(testEncoding));
        }

        [Test]
        public void Does_url_decode_raw_PathInfo()
        {
            var testEncoding = "test encoding";
            var url = "{0}echo/{1}".Fmt(Config.AbsoluteBaseUri, testEncoding.UrlEncode());
            Assert.That(url, Does.EndWith("/echo/test+encoding"));

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

        [Test]
        public void Can_force_default_UTF8_encoding()
        {
            const string param = "привіт";

            var json = Config.AbsoluteBaseUri.CombineWith("/echo").PostStringToUrl(
                requestBody: "Param=" + param.UrlEncode(),
                contentType: MimeTypes.FormUrlEncoded, accept: MimeTypes.Json);

            var value = JsonObject.Parse(json)["Param"]
                        ?? JsonObject.Parse(json)["param"];

            Assert.That(value, Is.EqualTo(param));
        }
    }

}