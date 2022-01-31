using System.Reflection;
using Funq;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/custom-method/result")]
    public class CustomMethodResult : IReturn<CustomMethodResult>
    {
        public int Id { get; set; }
    }

    [Route("/custom-method/headers")]
    public class CustomMethodHeaders : IReturn<CustomMethodHeaders>
    {
        public int Id { get; set; }
    }

    public class CustomMethodService : Service
    {
        public object Head(CustomMethodResult request)
        {
            return new HttpResult {
                Headers = {
                    {"X-Method", "HEAD"},
                    {"X-Id", request.Id.ToString()},
                    {"Content-Length", "100"},
                    {"Content-Type", "video/mp4"},
                }
            };
        }

        public object Any(CustomMethodResult request) => request;

        public void Head(CustomMethodHeaders request)
        {
            Response.AddHeader("X-Method", "HEAD");
            Response.AddHeader("X-Id", request.Id.ToString());
            Response.AddHeader("Content-Type", "video/mp4");
            Response.SetContentLength(100);
        }

        public object Any(CustomMethodHeaders request) => request;
    }

    public class CustomHttpMethodTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(CustomHttpMethodTests), typeof(CustomMethodService).Assembly) { }

            public override void Configure(Container container) { }
        }

        private ServiceStackHost appHost;

        public CustomHttpMethodTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Does_execute_HEAD_Request_returning_custom_HttpResult()
        {
            var response = Config.ListeningOn.AppendPath("custom-method","result").AddQueryParam("id", 1)
                .SendStringToUrl(method: "HEAD",
                    responseFilter: res => {
                        Assert.That(res.GetHeader("X-Method"), Is.EqualTo("HEAD"));
                        Assert.That(res.GetHeader("X-Id"), Is.EqualTo("1"));
                        Assert.That(res.MatchesContentType("video/mp4"));
                        Assert.That(res.GetContentLength(), Is.EqualTo(100));
                    });

            Assert.That(response, Is.Empty);
        }

        [Test]
        public void Does_execute_HEAD_Request_writing_custom_headers()
        {
            var response = Config.ListeningOn.AppendPath("custom-method","headers").AddQueryParam("id", 1)
                .SendStringToUrl(method: "HEAD",
                    responseFilter: res => {
                        Assert.That(res.GetHeader("X-Method"), Is.EqualTo("HEAD"));
                        Assert.That(res.GetHeader("X-Id"), Is.EqualTo("1"));
                        Assert.That(res.MatchesContentType("video/mp4"));
                        Assert.That(res.GetContentLength(), Is.EqualTo(100));
                    });

            Assert.That(response, Is.Empty);
        }
    }
}