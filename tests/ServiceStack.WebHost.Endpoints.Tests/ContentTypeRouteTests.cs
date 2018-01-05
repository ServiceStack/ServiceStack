using Funq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [ExcludeMetadata]
    [Route("/content/{Id}")]
    public class ContentRoute
    {
        public int Id { get; set; }
    }

    public class ContentRouteService : Service
    {
        public object Any(ContentRoute request)
        {
            return request;
        }

        public object GetJson(ContentRoute request)
        {
            request.Id++;
            return request;
        }

        public object AnyHtml(ContentRoute request)
        {
            return $@"
<html>
<body>
    <h1>AnyHtml {request.Id}</h1>
</body>
</html>";
        }

        public object GetHtml(ContentRoute request)
        {
            return $@"
<html>
<body>
    <h1>GetHtml {request.Id}</h1>
</body>
</html>";
        }
    }

    public class ContentTypeRouteTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(nameof(ContentTypeRouteTests), typeof(ContentRouteService).Assembly) { }

            public override void Configure(Container container) {}
        }

        private readonly ServiceStackHost appHost;
        public ContentTypeRouteTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown] public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void GET_Html_Request_calls_GetHtml()
        {
            var html = Config.ListeningOn.CombineWith("/content/1")
                .GetStringFromUrl(accept: MimeTypes.Html);

            Assert.That(html, Does.Contain("<h1>GetHtml 1</h1>"));
        }

        [Test]
        public void POST_Html_Request_calls_AnyHtml()
        {
            var html = Config.ListeningOn.CombineWith("/content/1")
                .PostStringToUrl(accept: MimeTypes.Html, requestBody: "");

            Assert.That(html, Does.Contain("<h1>AnyHtml 1</h1>"));
        }

        [Test]
        public void GET_JSON_Request_calls_GetJson()
        {
            var client = new JsonServiceClient(Config.ListeningOn);

            var response = client.Get<ContentRoute>(new ContentRoute { Id = 1 });
            Assert.That(response.Id, Is.EqualTo(1 + 1));
        }

        [Test]
        public void POST_JSON_Request_calls_Any()
        {
            var client = new JsonServiceClient(Config.ListeningOn);

            var response = client.Post<ContentRoute>(new ContentRoute { Id = 1 });
            Assert.That(response.Id, Is.EqualTo(1));
        }

    }
}