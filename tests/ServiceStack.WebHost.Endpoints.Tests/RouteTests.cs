using System;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Formats;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class RouteTests
    {
        private RouteAppHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new RouteAppHost();
            appHost.Init();
            appHost.Start(Config.AbsoluteBaseUri);
        }

        [OneTimeTearDown]
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
                    Assert.That(httpRes.MatchesContentType(MimeTypes.Html));
                });

            Assert.That(response, Does.StartWith("<!doctype html>"));
        }

        [Test]
        public void Can_download_original_route_with_Accept_json()
        {
            var response = Config.AbsoluteBaseUri.CombineWith("/custom/foo")
                .GetStringFromUrl(
                    requestFilter: req => req.With(c => c.Accept = MimeTypes.Json),
                    responseFilter: httpRes =>
                    {
                        Assert.That(httpRes.MatchesContentType(MimeTypes.Json));
                    });

            Assert.That(response.ToLower(), Is.EqualTo("{\"data\":\"foo\"}"));
        }

        [Test]
        public void Can_download_original_route_with_trailing_slash_and_Accept_json()
        {
            var response = Config.AbsoluteBaseUri.CombineWith("/custom/foo/")
                .GetStringFromUrl(
                    requestFilter: req => req.With(c => c.Accept = MimeTypes.Json),
                    responseFilter: httpRes =>
                    {
                        Assert.That(httpRes.MatchesContentType(MimeTypes.Json));
                    });

            Assert.That(response.ToLower(), Is.EqualTo("{\"data\":\"foo\"}"));
        }

        [Test]
        public void Can_download_original_route_with_json_extension()
        {
            var response = Config.AbsoluteBaseUri.CombineWith("/custom/foo.json")
                .GetStringFromUrl(responseFilter: httpRes =>
                {
                    Assert.That(httpRes.MatchesContentType(MimeTypes.Json));
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
                        Assert.That(httpRes.MatchesContentType(MimeTypes.Json));
                    });

            Assert.That(response.ToLower(), Is.EqualTo("{\"data\":\"foo\"}"));
        }

        [Test]
        public void Can_download_original_route_with_xml_extension()
        {
            var response = Config.AbsoluteBaseUri.CombineWith("/custom/foo.xml")
                .GetStringFromUrl(responseFilter: httpRes =>
                {
                    Assert.That(httpRes.MatchesContentType(MimeTypes.Xml));
                });

            Assert.That(response, Is.EqualTo("<?xml version=\"1.0\" encoding=\"utf-8\"?><CustomRoute xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.servicestack.net/types\"><Data>foo</Data></CustomRoute>"));
        }

        [Test]
        public void Can_download_original_route_with_html_extension()
        {
            var response = Config.AbsoluteBaseUri.CombineWith("/custom/foo.html")
                .GetStringFromUrl(responseFilter: httpRes =>
                {
                    Assert.That(httpRes.MatchesContentType(MimeTypes.Html));
                });

            Assert.That(response, Does.StartWith("<!doctype html>"));
        }

        [Test]
        public void Can_download_original_route_with_csv_extension()
        {
            var response = Config.AbsoluteBaseUri.CombineWith("/custom/foo.csv")
                .GetStringFromUrl(responseFilter: httpRes =>
                {
                    Assert.That(httpRes.MatchesContentType(MimeTypes.Csv));
                });

            Assert.That(response, Is.EqualTo("Data\r\nfoo\r\n"));
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
        public void Does_encode_QueryString()
        {
            var msg = "Field with comma, to demonstrate. ";
            var response = Config.AbsoluteBaseUri.CombineWith("/custom")
                .PostToUrl(new CustomRoute { Data = msg }, 
                    requestFilter:req => req.With(c => c.Accept = MimeTypes.Json));

            response.Print();

            var dto = response.FromJson<CustomRoute>();
            Assert.That(dto.Data, Is.EqualTo(msg));

            response = Config.AbsoluteBaseUri.CombineWith("/custom")
                .PostToUrl(new CustomRoute { Data = msg }.ToStringDictionary(), 
                    requestFilter:req => req.With(c => c.Accept = MimeTypes.Json));

            response.Print();

            dto = response.FromJson<CustomRoute>();
            Assert.That(dto.Data, Is.EqualTo(msg));
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

        [Test]
        public void Does_populate_version_when_using_Version_Abbreviation()
        {
            var response = Config.AbsoluteBaseUri.CombineWith("/versioned-request?v=1")
                .GetJsonFromUrl()
                .FromJson<RequestWithVersion>();

            Assert.That(response.Version, Is.EqualTo(1));

            response = Config.AbsoluteBaseUri.CombineWith("/versioned-request/1?v=2")
                .GetJsonFromUrl()
                .FromJson<RequestWithVersion>();

            Assert.That(response.Version, Is.EqualTo(2));

            response = Config.AbsoluteBaseUri.CombineWith("/versioned-request/1?v=4&Version=3")
                .GetJsonFromUrl()
                .FromJson<RequestWithVersion>();

            Assert.That(response.Version, Is.EqualTo(3));
        }

        [Test]
        public async Task Does_send_version_using_JsonServiceClient()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);
            var response = client.Send(new RequestWithVersion { Version = 1 });
            Assert.That(response.Version, Is.EqualTo(1));

            response = await client.SendAsync(new RequestWithVersion { Version = 1 });
            Assert.That(response.Version, Is.EqualTo(1));
            
            client.Version = 1;
            response = client.Send(new RequestWithVersion());
            Assert.That(response.Version, Is.EqualTo(1));
            
            response = await client.SendAsync(new RequestWithVersion());
            Assert.That(response.Version, Is.EqualTo(1));
        }

        [Test]
        public async Task Does_send_version_using_JsonHttpClient()
        {
            var client = new JsonHttpClient(Config.AbsoluteBaseUri);
            var response = client.Send(new RequestWithVersion { Version = 1 });
            Assert.That(response.Version, Is.EqualTo(1));

            response = await client.SendAsync(new RequestWithVersion { Version = 1 });
            Assert.That(response.Version, Is.EqualTo(1));
            
            client.Version = 1;
            response = client.Send(new RequestWithVersion());
            Assert.That(response.Version, Is.EqualTo(1));
            
            response = await client.SendAsync(new RequestWithVersion());
            Assert.That(response.Version, Is.EqualTo(1));
        }

        [Test]
        public async Task Can_POST_to_IdWithAlias_with_JsonServiceClient_async()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var response = await client.PostAsync(new IdWithAlias { Id = 1 });
            Assert.That(response.Id, Is.EqualTo(1));
        }

        [Test]
        public async Task Can_POST_to_IdWithAlias_with_JsonHttpClient_async()
        {
            var client = new JsonHttpClient(Config.AbsoluteBaseUri);

            var response = await client.PostAsync(new IdWithAlias { Id = 1 });
            Assert.That(response.Id, Is.EqualTo(1));
        }

        [Test]
        public void Can_create_request_DTO_from_URL()
        {
            Assert.That(HostContext.Metadata.CreateRequestFromUrl("/hello") is Hello);

            var request = (Hello)HostContext.Metadata.CreateRequestFromUrl("/hello/foo");
            Assert.That(request.Name, Is.EqualTo("foo"));
            
            request = (Hello)HostContext.Metadata.CreateRequestFromUrl("http://domain.org/hello/foo");
            Assert.That(request.Name, Is.EqualTo("foo"));

            request = (Hello)HostContext.Metadata.CreateRequestFromUrl("https://www.sub.domain.org/hello/foo");
            Assert.That(request.Name, Is.EqualTo("foo"));

            request = (Hello)HostContext.Metadata.CreateRequestFromUrl("/hello?name=bar");
            Assert.That(request.Name, Is.EqualTo("bar"));

            var responseType = HostContext.Metadata.GetResponseTypeByRequest(request.GetType());
            Assert.That(responseType, Is.EqualTo(typeof(HelloResponse)));
            
            request = (Hello)HostContext.Metadata.CreateRequestFromUrl("/hello?name=gateway");
            var response = (HelloResponse)HostContext.AppHost.GetServiceGateway(new BasicRequest()).Send(responseType, request);
            Assert.That(response.Result, Is.EqualTo("Hello, gateway"));
        }
    }

    public class RouteAppHost : AppHostHttpListenerBase
    {
        public RouteAppHost() : base(nameof(BufferedRequestTests), typeof(CustomRouteService).Assembly) { }

        public override void Configure(Container container)
        {
            SetConfig(new HostConfig
            {
                AllowRouteContentTypeExtensions = true
            });

            Plugins.Add(new CsvFormat()); //required to allow .csv

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

    [Route("/versioned-request")]
    [Route("/versioned-request/{Id}")]
    public class RequestWithVersion : IReturn<RequestWithVersion>, IHasVersion
    {
        public int Id { get; set; }
        public int Version { get; set; }
    }

    [Route("/thing/{Id}/point", "POST")]
    [DataContract]
    public class IdWithAlias : IReturn<IdWithAlias>
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }
    }


    public class CustomRouteService : IService
    {
        public object Any(CustomRoute request) => request;
        public object Any(CustomRouteDot request) => request;
        public object Any(GetPngPic request) => request;
        public object Any(RequestWithVersion request) => request;
        public object Any(IdWithAlias request) => request;
    }

    [TestFixture]
    public class ModifiedRouteTests
    {
        private ModifiedRouteAppHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new ModifiedRouteAppHost();
            appHost.Init();
            appHost.Start(Config.AbsoluteBaseUri);
//            appHost.Start(Config.AnyHostBaseUrl); //go through fiddler
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Does_URL_Decode_PathInfo()
        {
            var client = new JsonServiceClient(Config.ListeningOn);
//            var client = new JsonServiceClient("http://test.servicestack.net");

            var pathInfo = "ern::Closer2U::Userprofile::1c7e9ead-c7d9-46f8-a0cc-2777c4373ac4";
            var response = client.Get(new CustomRoute {
                Data = pathInfo 
            });
            
            Assert.That(response.Data, Is.EqualTo(pathInfo));
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
            catch (Exception ex)
            {
                Assert.That(ex.GetStatus(), Is.EqualTo(HttpStatusCode.NotFound));
            }

            var response = Config.AbsoluteBaseUri.CombineWith("/prefix/modified/foo.csv")
                .GetStringFromUrl();

            Assert.That(response, Is.EqualTo("Data\r\nfoo\r\n"));
        }
    }

    public class ModifiedRouteAppHost : AppHostHttpListenerBase
    {
        public ModifiedRouteAppHost() : base(nameof(BufferedRequestTests), typeof(CustomRouteService).Assembly) { }

        public override void Configure(Container container)
        {
        }

        public override RouteAttribute[] GetRouteAttributes(System.Type requestType)
        {
            var routes = base.GetRouteAttributes(requestType);
            if (requestType != typeof(ModifiedRoute)) return routes;

            routes.Each(x => x.Path = "/prefix" + x.Path);
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
            public InvalidRoutesAppHost() : base(nameof(InvalidRoutesAppHost), typeof(InvalidRoutesAppHost).Assembly) { }

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
                    Assert.Fail("Should throw");
                }
                catch (Exception ex)
                {
                    Assert.That(ex.GetStatus(), Is.EqualTo(HttpStatusCode.MethodNotAllowed));
                }
            }
        }
    }

    [Route("/routeinfo/{Path*}")]
    public class GetRouteInfo
    {
        public string Path { get; set; }
    }

    public class GetRouteInfoResponse
    {
        public string BaseUrl { get; set; }
        public string ResolvedUrl { get; set; }
    }

    public class RouteInfoService : Service
    {
        public object Any(GetRouteInfo request)
        {
            return new GetRouteInfoResponse
            {
                BaseUrl = base.Request.GetBaseUrl(),
                ResolvedUrl = base.Request.ResolveAbsoluteUrl("~/resolved")
            };
        }
    }

    class RouteInfoAppHost : AppSelfHostBase
    {
        public RouteInfoAppHost() : base(typeof(RouteInfoAppHost).Name, typeof(RouteInfoAppHost).Assembly) { }
        public override void Configure(Container container)
        {
            CatchAllHandlers.Add((httpMethod, pathInfo, filePath) =>
            {
                if (pathInfo.StartsWith("/swagger-ui"))
                {
                    return new CustomResponseHandler((req, res) => 
                        new GetRouteInfoResponse
                        {
                            BaseUrl = req.GetBaseUrl(),
                            ResolvedUrl = req.ResolveAbsoluteUrl("~/resolved")
                        });
                }
                return null;
            });
        }
    }

    public class RouteInfoPathTests
    {
        [Test]
        public void RootPath_returns_BaseUrl()
        {
            var url = Config.ServiceStackBaseUri;
            using (var appHost = new RouteInfoAppHost()
                .Init()
                .Start(url + "/"))
            {
                var response = url.CombineWith("/routeinfo").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
                Assert.That(response.BaseUrl, Is.EqualTo(url));
                Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));

                response = url.CombineWith("/routeinfo/dir").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
                Assert.That(response.BaseUrl, Is.EqualTo(url));
                Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));

                response = url.CombineWith("/routeinfo/dir/sub").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
                Assert.That(response.BaseUrl, Is.EqualTo(url));
                Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));

                response = url.CombineWith("/swagger-ui").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
                Assert.That(response.BaseUrl, Is.EqualTo(url));
                Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));

                response = url.CombineWith("/swagger-ui/").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
                Assert.That(response.BaseUrl, Is.EqualTo(url));
                Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));
            }
        }

        [Test]
        public void ApiPath_returns_BaseUrl()
        {
            var url = Config.AbsoluteBaseUri.AppendPath("api");
            using (var appHost = new RouteInfoAppHost()
                .Init()
                .Start(url + "/"))
            {
                var response = url.CombineWith("/routeinfo").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
                Assert.That(response.BaseUrl, Is.EqualTo(url));
                Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));

                response = url.CombineWith("/routeinfo/dir").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
                Assert.That(response.BaseUrl, Is.EqualTo(url));
                Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));

                response = url.CombineWith("/routeinfo/dir/sub").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
                Assert.That(response.BaseUrl, Is.EqualTo(url));
                Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));

                response = url.CombineWith("/swagger-ui").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
                Assert.That(response.BaseUrl, Is.EqualTo(url));
                Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));

                response = url.CombineWith("/swagger-ui/").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
                Assert.That(response.BaseUrl, Is.EqualTo(url));
                Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));
            }
        }

        [Test]
        public void ApiV1Path_returns_BaseUrl()
        {
            var url = Config.AbsoluteBaseUri.AppendPath("api").AppendPath("v1");
            using (var appHost = new RouteInfoAppHost()
                .Init()
                .Start(url + "/"))
            {
                var response = url.CombineWith("/routeinfo").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
                Assert.That(response.BaseUrl, Is.EqualTo(url));
                Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));

                response = url.CombineWith("/routeinfo/dir").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
                Assert.That(response.BaseUrl, Is.EqualTo(url));
                Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));

                response = url.CombineWith("/routeinfo/dir/sub").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
                Assert.That(response.BaseUrl, Is.EqualTo(url));
                Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));

                response = url.CombineWith("/swagger-ui").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
                Assert.That(response.BaseUrl, Is.EqualTo(url));
                Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));

                response = url.CombineWith("/swagger-ui/").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
                Assert.That(response.BaseUrl, Is.EqualTo(url));
                Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));
            }
        }

        [Test]
        public void Does_NormalizePathInfo()
        {
            Assert.That(ServiceStackHost.NormalizePathInfo("/api/a", "api"), Is.EqualTo("/a"));
            Assert.That(ServiceStackHost.NormalizePathInfo("/api/a/b", "api"), Is.EqualTo("/a/b"));
            Assert.That(ServiceStackHost.NormalizePathInfo("/a", "api"), Is.EqualTo("/a"));
            Assert.That(ServiceStackHost.NormalizePathInfo("/a/b", "api"), Is.EqualTo("/a/b"));
            Assert.That(ServiceStackHost.NormalizePathInfo("/apikeys", "api"), Is.EqualTo("/apikeys"));
            Assert.That(ServiceStackHost.NormalizePathInfo("/apikeys/a", "api"), Is.EqualTo("/apikeys/a"));
            Assert.That(ServiceStackHost.NormalizePathInfo("/apikeys/a/b", "api"), Is.EqualTo("/apikeys/a/b"));
        }

    }
}
