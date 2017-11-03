using System.Net;
using Funq;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/matchroute/html", MatchRule = "AcceptsHtml")]
    public class MatchesHtml : IReturn<MatchesHtml>
    {
        public string Name { get; set; }
    }

    [Route("/matchroute/json", MatchRule = "AcceptsJson")]
    public class MatchesJson : IReturn<MatchesJson>
    {
        public string Name { get; set; }
    }

    public class MatchesCsv : IReturn<MatchesCsv>
    {
        public string Name { get; set; }
    }

    [Route("/matchlast/{Id}", MatchRule = @"LastInt")]
    public class MatchesLastInt
    {
        public int Id { get; set; }
    }

    [Route("/matchlast/{Slug}", MatchRule = @"!LastInt")]
    public class MatchesNotLastInt
    {
        public string Slug { get; set; }
    }

    [Route("/matchregex/{Id}", MatchRule = @"PathInfo =~ \/[0-9]+$")]
    public class MatchesId
    {
        public int Id { get; set; }
    }

    [Route("/matchregex/{Slug}", MatchRule = @"PathInfo =~ \/[^0-9]+$")]
    public class MatchesSlug
    {
        public string Slug { get; set; }
    }

    public class RouteMatchService : Service
    {
        public object Any(MatchesHtml request) => request;
        public object Any(MatchesJson request) => request;
        public object Any(MatchesCsv request) => request;

        public object Any(MatchesLastInt request) => request;
        public object Any(MatchesNotLastInt request) => request;

        public object Any(MatchesId request) => request;
        public object Any(MatchesSlug request) => request;
    }

    public class RouteMatchTests
    {
        private readonly ServiceStackHost appHost;
        public IServiceClient CreateClient() => new JsonServiceClient(Config.ListeningOn);

        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(RouteMatchTests), typeof(RouteMatchService).Assembly) { }

            public override void Configure(Container container)
            {
                SetConfig(new HostConfig
                {
                    RequestRules =
                    {
                        { "AcceptsCsv", httpReq => httpReq.Accept?.IndexOf(MimeTypes.Csv) >= 0 },
                    }
                });

                Routes.Add(typeof(MatchesCsv), "/matchroute/csv", null, null, null, matchRule:"AcceptsCsv");
            }
        }

        public RouteMatchTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Does_match_builtin_RequestRules_JSON()
        {
            var client = CreateClient();

            var response = client.Get(new MatchesJson { Name = "JSON" });
            Assert.That(response.Name, Is.EqualTo("JSON"));

            try
            {
                var html = Config.ListeningOn.AppendPath("matchroute/json").AddQueryParam("name", "JSON")
                    .GetStringFromUrl(accept: MimeTypes.Html);
                Assert.Fail("Should throw");
            }
            catch (WebException ex)
            {
                Assert.That(ex.ToStatusCode(), Is.EqualTo(404));
            }
        }

        [Test]
        public void Does_match_builtin_RequestRules_HTML()
        {
            var client = CreateClient();

            var html = Config.ListeningOn.AppendPath("matchroute/html").AddQueryParam("name", "HTML")
                .GetStringFromUrl(accept: MimeTypes.Html);
            Assert.That(html, Does.StartWith("<"));

            try
            {
                var response = client.Get(new MatchesHtml { Name = "HTML" });
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public void Does_match_new_RequestRules_CSV()
        {
            var client = CreateClient();

            var csv = Config.ListeningOn.AppendPath("matchroute/csv").AddQueryParam("name", "CSV")
                .GetStringFromUrl(accept: MimeTypes.Csv);
            Assert.That(csv.NormalizeNewLines(), Is.EqualTo("Name\nCSV"));

            try
            {
                var response = client.Get(new MatchesCsv { Name = "CSV" });
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public void Can_match_on_builtin_LastInt()
        {
            var json = Config.ListeningOn.AppendPath("matchlast/1")
                .GetJsonFromUrl();

            Assert.That(json.ToLower(), Is.EqualTo("{\"id\":1}"));
        }

        [Test]
        public void Can_match_on_builtin_not_LastInt()
        {
            var json = Config.ListeningOn.AppendPath("matchlast/name")
                .GetJsonFromUrl();

            Assert.That(json.ToLower(), Is.EqualTo("{\"slug\":\"name\"}"));
        }

        [Test]
        public void Can_match_on_regex_int_id()
        {
            var json = Config.ListeningOn.AppendPath("matchregex/1")
                .GetJsonFromUrl();

            Assert.That(json.ToLower(), Is.EqualTo("{\"id\":1}"));
        }

        [Test]
        public void Can_match_on_regex_slug()
        {
            var json = Config.ListeningOn.AppendPath("matchregex/name")
                .GetJsonFromUrl();

            Assert.That(json.ToLower(), Is.EqualTo("{\"slug\":\"name\"}"));
        }

    }
}