using System;
using System.Net;
using Funq;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/matches/html", Matches = "AcceptsHtml")]
    public class MatchesHtml : IReturn<MatchesHtml>
    {
        public string Name { get; set; }
    }

    [Route("/matches/json", Matches = "AcceptsJson")]
    public class MatchesJson : IReturn<MatchesJson>
    {
        public string Name { get; set; }
    }

    public class MatchesCsv : IReturn<MatchesCsv>
    {
        public string Name { get; set; }
    }

    [Route("/{SlugFirst}/matchrule")]
    public class MatchesNotFirstInt
    {
        public string SlugFirst { get; set; }
    }

    [Route("/{IdFirst}/matchrule", Matches = @"{int}/**")]
    public class MatchesFirstInt
    {
        public int IdFirst { get; set; }
    }

    [Route("/matchrule/{SlugLast}")]
    public class MatchesNotLastInt
    {
        public string SlugLast { get; set; }
    }

    [Route("/matchrule/{IdLast}", Matches = @"**/{int}")]
    public class MatchesLastInt
    {
        public int IdLast { get; set; }
    }

    [Route("/matchrule/{Slug2}/remaining/path")]
    public class MatchesSecondSlug
    {
        public string Slug2 { get; set; }
    }

    [Route("/matchrule/{Id2}/remaining/path", Matches = @"path/{int}/**")]
    public class MatchesSecondInt
    {
        public int Id2 { get; set; }
    }

    [Route("/remaining/path/{Slug2}/matchrule")]
    public class MatchesSecondLastSlug
    {
        public string Slug2 { get; set; }
    }

    [Route("/remaining/path/{Id2}/matchrule", Matches = @"**/{int}/path")]
    public class MatchesSecondLastInt
    {
        public int Id2 { get; set; }
    }

    [Route("/matchregex/{Slug}")]
    public class MatchesSlug
    {
        public string Slug { get; set; }
    }

    [Route("/matchregex/{Id}", Matches = @"PathInfo =~ \/[0-9]+$")]
    public class MatchesInt
    {
        public int Id { get; set; }
    }

    [Route("/matchexact/{Exact}", Matches = @"UserAgent = specific-client")]
    public class MatchesExactUserAgent
    {
        public string Exact { get; set; }
    }

    [Route("/matchexact/{Any}")]
    public class MatchesAnyUserAgent
    {
        public string Any { get; set; }
    }

    [Route("/matchsite/{Mobile}", Matches = "IsMobile")]
    public class MatchMobileSite
    {
        public string Mobile { get; set; }
    }

    [Route("/matchsite/{Desktop}")]
    public class MatchDesktopSite
    {
        public string Desktop { get; set; }
    }

    [Route("/matchauth/{Authenticated}", Matches = "IsAuthenticated")]
    public class MatchAuthenticated
    {
        public string Authenticated { get; set; }
    }

    [Route("/matchauth/{Unauthenticated}")]
    public class MatchUnauthenticated
    {
        public string Unauthenticated { get; set; }
    }

    public class RouteMatchService : Service
    {
        public object Any(MatchesHtml request) => request;
        public object Any(MatchesJson request) => request;
        public object Any(MatchesCsv request) => request;

        public object Any(MatchesNotFirstInt request) => request;
        public object Any(MatchesFirstInt request) => request;

        public object Any(MatchesNotLastInt request) => request;
        public object Any(MatchesLastInt request) => request;

        public object Any(MatchesSecondSlug request) => request;
        public object Any(MatchesSecondInt request) => request;

        public object Any(MatchesSecondLastSlug request) => request;
        public object Any(MatchesSecondLastInt request) => request;

        public object Any(MatchesSlug request) => request;
        public object Any(MatchesInt request) => request;

        public object Any(MatchesExactUserAgent request) => request;
        public object Any(MatchesAnyUserAgent request) => request;

        public object Any(MatchMobileSite request) => request;
        public object Any(MatchDesktopSite request) => request;

        public object Any(MatchAuthenticated request) => request;
        public object Any(MatchUnauthenticated request) => request;
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
                        { "CustomAcceptsCsv", httpReq => httpReq.Accept?.IndexOf(MimeTypes.Csv) >= 0 },
                    },
                    AdminAuthSecret = "secretz"
                });

                Routes.Add(typeof(MatchesCsv), "/matches/csv", null, null, null, matches:"CustomAcceptsCsv");
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
                var html = Config.ListeningOn.AppendPath("matches/json").AddQueryParam("name", "JSON")
                    .GetStringFromUrl(accept: MimeTypes.Html);
                Assert.Fail("Should throw");
            }
            catch (Exception ex)
            {
                Assert.That(ex.GetStatus(), Is.EqualTo(HttpStatusCode.NotFound));
            }
        }

        [Test]
        public void Does_match_builtin_RequestRules_HTML()
        {
            var client = CreateClient();

            var html = Config.ListeningOn.AppendPath("matches/html").AddQueryParam("name", "HTML")
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

            var csv = Config.ListeningOn.AppendPath("matches/csv").AddQueryParam("name", "CSV")
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
        public void Can_match_on_builtin_FirstInt()
        {
            var json = Config.ListeningOn.AppendPath("1/matchrule")
                .GetJsonFromUrl();

            Assert.That(json.ToLower(), Is.EqualTo("{\"idfirst\":1}"));
        }

        [Test]
        public void Can_match_on_builtin_NotFirstInt()
        {
            var json = Config.ListeningOn.AppendPath("name/matchrule")
                .GetJsonFromUrl();

            Assert.That(json.ToLower(), Is.EqualTo("{\"slugfirst\":\"name\"}"));
        }

        [Test]
        public void Can_match_on_builtin_LastInt()
        {
            var json = Config.ListeningOn.AppendPath("matchrule/1")
                .GetJsonFromUrl();

            Assert.That(json.ToLower(), Is.EqualTo("{\"idlast\":1}"));
        }

        [Test]
        public void Can_match_on_builtin_NotLastInt()
        {
            var json = Config.ListeningOn.AppendPath("matchrule/name")
                .GetJsonFromUrl();

            Assert.That(json.ToLower(), Is.EqualTo("{\"sluglast\":\"name\"}"));
        }

        [Test]
        public void Can_match_on_builtin_Second_Int()
        {
            var json = Config.ListeningOn.AppendPath("matchrule/1/remaining/path")
                .GetJsonFromUrl();

            Assert.That(json.ToLower(), Is.EqualTo("{\"id2\":1}"));
        }

        [Test]
        public void Can_match_on_builtin_Second_NotInt()
        {
            var json = Config.ListeningOn.AppendPath("matchrule/name/remaining/path")
                .GetJsonFromUrl();

            Assert.That(json.ToLower(), Is.EqualTo("{\"slug2\":\"name\"}"));
        }

        [Test]
        public void Can_match_on_builtin_SecondLast_Int()
        {
            var json = Config.ListeningOn.AppendPath("/remaining/path/1/matchrule")
                .GetJsonFromUrl();

            Assert.That(json.ToLower(), Is.EqualTo("{\"id2\":1}"));
        }

        [Test]
        public void Can_match_on_builtin_SecondLast_NotInt()
        {
            var json = Config.ListeningOn.AppendPath("/remaining/path/name/matchrule")
                .GetJsonFromUrl();

            Assert.That(json.ToLower(), Is.EqualTo("{\"slug2\":\"name\"}"));
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

        [Test]
        public void Can_match_on_exact_UserAgent()
        {
            var json = Config.ListeningOn.AppendPath("matchexact/specific-client")
                .GetJsonFromUrl(req => req.With(c => c.UserAgent = "specific-client"));

            Assert.That(json.ToLower(), Is.EqualTo("{\"exact\":\"specific-client\"}"));
        }

        [Test]
        public void Can_match_on_any_UserAgent()
        {
            var json = Config.ListeningOn.AppendPath("matchexact/any-client")
                .GetJsonFromUrl(req => req.With(c => c.UserAgent = "any-client"));

            Assert.That(json.ToLower(), Is.EqualTo("{\"any\":\"any-client\"}"));
        }

        [Test]
        public void Does_match_builtin_IsMobile_Rule_with_iPhone6UA()
        {
            const string iPhone6UA = "Mozilla/5.0 (iPhone; CPU iPhone OS 6_0 like Mac OS X) AppleWebKit/536.26 (KHTML, like Gecko) Version/6.0 Mobile/10A5376e Safari/8536.25";
            var json = Config.ListeningOn.AppendPath("matchsite/iPhone6")
                .GetJsonFromUrl(req => req.With(c => c.UserAgent = iPhone6UA));

            Assert.That(json.ToLower(), Is.EqualTo("{\"mobile\":\"iphone6\"}"));
        }

        [Test]
        public void Does_not_match_builtin_IsMobile_Rule_with_SafariUA()
        {
            const string SafariUA = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_6) AppleWebKit/601.7.7 (KHTML, like Gecko) Version/9.1.2 Safari/601.7.7";
            var json = Config.ListeningOn.AppendPath("matchsite/Safari")
                .GetJsonFromUrl(req => req.With(c => c.UserAgent = SafariUA));

            Assert.That(json.ToLower(), Is.EqualTo("{\"desktop\":\"safari\"}"));
        }

        [Test]
        public void Can_match_on_IsAuthenticated_using_AuthSecret()
        {
            var json = Config.ListeningOn.AppendPath("matchauth/secure")
                .AddQueryParam("authsecret","secretz")
                .GetJsonFromUrl();

            Assert.That(json.ToLower(), Is.EqualTo("{\"authenticated\":\"secure\"}"));
        }

        [Test]
        public void Does_not_match_on_IsAuthenticated_when_not_authenticated()
        {
            var json = Config.ListeningOn.AppendPath("matchauth/public")
                .GetJsonFromUrl();

            Assert.That(json.ToLower(), Is.EqualTo("{\"unauthenticated\":\"public\"}"));
        }

    }
}