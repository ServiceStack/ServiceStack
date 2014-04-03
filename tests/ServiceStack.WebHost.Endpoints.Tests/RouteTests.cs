using System.Net;
using Funq;
using NUnit.Framework;
using ServiceStack.Formats;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class RouteTests
    {
        private RouteAppHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new RouteAppHost();
            appHost.Init();
            appHost.Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_download_original_route()
        {
            var response = Config.AbsoluteBaseUri.CombineWith("/custom/foo")
                .GetStringFromUrl(responseFilter: httpRes =>
                {
                    httpRes.ContentType.Print();
                    Assert.That(httpRes.ContentType.MatchesContentType(MimeTypes.Html));
                });

            Assert.That(response, Is.StringStarting("<!doctype html>"));
        }

        [Test]
        public void Can_download_original_route_with_json_extension()
        {
            var response = Config.AbsoluteBaseUri.CombineWith("/custom/foo.json")
                .GetStringFromUrl(responseFilter: httpRes =>
                {
                    httpRes.ContentType.Print();
                    Assert.That(httpRes.ContentType.MatchesContentType(MimeTypes.Json));
                });

            Assert.That(response.ToLower(), Is.EqualTo( "{\"data\":\"foo\"}"));
        }

        [Test]
        public void Can_download_original_route_with_xml_extension()
        {
            var response = Config.AbsoluteBaseUri.CombineWith("/custom/foo.xml")
                .GetStringFromUrl(responseFilter: httpRes =>
                {
                    httpRes.ContentType.Print();
                    Assert.That(httpRes.ContentType.MatchesContentType(MimeTypes.Xml));
                });

            Assert.That(response, Is.EqualTo("<?xml version=\"1.0\" encoding=\"utf-8\"?><CustomRoute xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.servicestack.net/types\"><Data>foo</Data></CustomRoute>"));
        }

        [Test]
        public void Can_download_original_route_with_html_extension()
        {
            var response = Config.AbsoluteBaseUri.CombineWith("/custom/foo.html")
                .GetStringFromUrl(responseFilter: httpRes =>
                {
                    httpRes.ContentType.Print();
                    Assert.That(httpRes.ContentType.MatchesContentType(MimeTypes.Html));
                });

            Assert.That(response, Is.StringStarting("<!doctype html>"));
        }

        [Test]
        public void Can_download_original_route_with_csv_extension()
        {
            var response = Config.AbsoluteBaseUri.CombineWith("/custom/foo.csv")
                .GetStringFromUrl(responseFilter: httpRes =>
                {
                    httpRes.ContentType.Print();
                    Assert.That(httpRes.ContentType.MatchesContentType(MimeTypes.Csv));
                });

            Assert.That(response, Is.EqualTo("Data\r\nfoo\r\n"));
        }
    }

    public class RouteAppHost : AppHostHttpListenerBase
    {
        public RouteAppHost() : base(typeof(BufferedRequestTests).Name, typeof(CustomRouteService).Assembly) { }

        public override void Configure(Container container)
        {
            SetConfig(new HostConfig {
                AllowRouteContentTypeExtensions = true
            });

            Plugins.Add(new CsvFormat()); //required to allow .csv
        }
    }

    [Route("/custom")]
    [Route("/custom/{Data}")]
    public class CustomRoute : IReturn<CustomRoute>
    {
        public string Data { get; set; }
    }

    public class CustomRouteService : IService
    {
        public object Any(CustomRoute request)
        {
            return request;
        }
    }

    [TestFixture]
    public class ModifiedRouteTests
    {
        private ModifiedRouteAppHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new ModifiedRouteAppHost();
            appHost.Init();
            appHost.Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_download_modified_routes()
        {
            try
            {
                var notFound = Config.AbsoluteBaseUri.CombineWith("/custom/foo.csv")
                    .GetStringFromUrl();
                Assert.Fail("Existing route should be modified");
            }
            catch (WebException ex)
            {
                Assert.That(ex.GetStatus(), Is.EqualTo(HttpStatusCode.NotFound));
            }

            var response = Config.AbsoluteBaseUri.CombineWith("/api/custom/foo.csv")
                .GetStringFromUrl();

            Assert.That(response, Is.EqualTo("Data\r\nfoo\r\n"));
        }
    }

    public class ModifiedRouteAppHost : AppHostHttpListenerBase
    {
        public ModifiedRouteAppHost() : base(typeof(BufferedRequestTests).Name, typeof(CustomRouteService).Assembly) { }

        public override void Configure(Container container)
        {
        }

        public override RouteAttribute[] GetRouteAttributes(System.Type requestType)
        {
            var routes = base.GetRouteAttributes(requestType);
            if (requestType != typeof(ModifiedRoute)) return routes;

            routes.Each(x => x.Path = "/api" + x.Path);
            return routes;
        }
    }

    [Route("/modified")]
    [Route("/modified/{Data}")]
    public class ModifiedRoute : IReturn<ModifiedRoute>
    {
        public string Data { get; set; }
    }

    public class ModifiedRouteService : IService
    {
        public object Any(ModifiedRoute request)
        {
            return request;
        }
    }
}
