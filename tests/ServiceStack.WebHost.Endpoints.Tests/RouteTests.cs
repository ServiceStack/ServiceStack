using System;
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

            Assert.That(response.ToLower(), Is.EqualTo("{\"data\":\"foo\"}"));
        }

        [Test]
        public void Can_process_plaintext_as_JSON()
        {
            var response = Config.AbsoluteBaseUri.CombineWith("/custom")
                .PostStringToUrl("{\"data\":\"foo\"}", 
                    contentType:MimeTypes.PlainText,
                    responseFilter: httpRes => 
                    {
                        httpRes.ContentType.Print();
                        Assert.That(httpRes.ContentType.MatchesContentType(MimeTypes.Json));
                    });

            Assert.That(response.ToLower(), Is.EqualTo("{\"data\":\"foo\"}"));
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

            var lf = System.Environment.NewLine;
            Assert.That(response, Is.EqualTo("Data{0}foo{0}".Fmt(lf)));
        }

        [Test]
        public void Does_encode_route_with_backslash()
        {
            var request = new CustomRoute { Data = "D\\SN" };
            Assert.That(request.ToUrl(), Is.EqualTo("/custom/D%5CSN"));
            Assert.That(request.ToUrl().UrlDecode(), Is.EqualTo("/custom/D\\SN"));

            //HttpListener and ASP.NET hosts doesn't support `\` or %5C in urls
            //var client = new JsonServiceClient(Config.AbsoluteBaseUri);
            //var response = client.Get(request);
            //Assert.That(response.Data, Is.EqualTo("D\\SN"));
        }

        [Test]
        public void Can_download_route_with_dot_seperator()
        {
            var response = Config.AbsoluteBaseUri.CombineWith("/customdot/id.data")
                .GetJsonFromUrl()
                .FromJson<CustomRouteDot>();

            Assert.That(response.Id, Is.EqualTo("id"));
            Assert.That(response.Data, Is.EqualTo("data"));
        }

        [Test]
        public void Can_download_route_with_dot_seperator_and_extension()
        {
            var response = Config.AbsoluteBaseUri.CombineWith("/pics/100x100/1.png")
                .GetJsonFromUrl()
                .FromJson<GetPngPic>();

            Assert.That(response.Size, Is.EqualTo("100x100"));
            Assert.That(response.Id, Is.EqualTo("1"));
        }

        [Test]
        public void Can_download_route_with_dot_seperator_and_extension_with_jsonserviceclient()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);
            var request = new GetPngPic {
                Id = "1",
                Size = "100x100",
            };

            Assert.That(request.ToGetUrl(), Is.EqualTo("/pics/100x100/1.png"));

            var response = client.Get<GetPngPic>(request);

            Assert.That(response.Size, Is.EqualTo("100x100"));
            Assert.That(response.Id, Is.EqualTo("1"));
        }
    }

    public class RouteAppHost : AppHostHttpListenerBase
    {
        public RouteAppHost() : base(typeof(BufferedRequestTests).Name, typeof(CustomRouteService).Assembly) { }

        public override void Configure(Container container)
        {
            SetConfig(new HostConfig
            {
                AllowRouteContentTypeExtensions = true
            });

            Plugins.Add(new CsvFormat()); //required to allow .csv

            Plugins.RemoveAll(x => x is MarkdownFormat);

            ContentTypes.Register(MimeTypes.PlainText,
                (req, o, stream) => JsonSerializer.SerializeToStream(o.GetType(), stream),
                JsonSerializer.DeserializeFromStream);

            PreRequestFilters.Add((req, res) => {
                if (req.ContentType.MatchesContentType(MimeTypes.PlainText))
                    req.ResponseContentType = MimeTypes.Json;
            });
        }
    }

    [Route("/custom")]
    [Route("/custom/{Data}")]
    public class CustomRoute : IReturn<CustomRoute>
    {
        public string Data { get; set; }
    }

    [Route("/customdot/{Id}.{Data}")]
    public class CustomRouteDot : IReturn<CustomRouteDot>
    {
        public string Id { get; set; }
        public string Data { get; set; }
    }

    [Route("/pics/{Size}/{Id}.png", "GET")]
    public class GetPngPic
    {
        public string Id { get; set; }

        public string Size { get; set; }
    }

    public class CustomRouteService : IService
    {
        public object Any(CustomRoute request)
        {
            return request;
        }

        public object Any(CustomRouteDot request)
        {
            return request;
        }

        public object Any(GetPngPic request)
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
                var notFound = Config.AbsoluteBaseUri.CombineWith("/modified/foo.csv")
                    .GetStringFromUrl();
                Assert.Fail("Existing route should be modified");
            }
            catch (WebException ex)
            {
                Assert.That(ex.GetStatus(), Is.EqualTo(HttpStatusCode.NotFound));
            }

            var response = Config.AbsoluteBaseUri.CombineWith("/api/modified/foo.csv")
                .GetStringFromUrl();

            var lf = System.Environment.NewLine;
            Assert.That(response, Is.EqualTo("Data{0}foo{0}".Fmt(lf)));

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

    [TestFixture]
    public class InvalidRouteTests
    {
        public class UnknownRoute { }

        class InvalidRoutesAppHost : AppSelfHostBase
        {
            public InvalidRoutesAppHost() : base(typeof(InvalidRoutesAppHost).Name, typeof(InvalidRoutesAppHost).Assembly) { }

            public override void Configure(Container container)
            {
                Routes.Add<UnknownRoute>("/unknownroute");
            }
        }

        [Test]
        public void Throws_error_when_registering_route_for_unknown_Service()
        {
            using (var appHost = new InvalidRoutesAppHost()
                .Init()
                .Start(Config.AbsoluteBaseUri))
            {
                try
                {
                    var json = Config.AbsoluteBaseUri.CombineWith("/unknownroute").GetJsonFromUrl();
                    json.PrintDump();
                    Assert.Fail("Should throw");
                }
                catch (WebException ex)
                {
                    Assert.That(ex.GetStatus(), Is.EqualTo(HttpStatusCode.MethodNotAllowed));
                }
            }
        }
    }
}
