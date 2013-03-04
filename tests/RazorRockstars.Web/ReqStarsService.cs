using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.OrmLite;
using ServiceStack.Plugins.MsgPack;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Cors;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace RazorRockstars.Web
{
    /// New Proposal, keeping ServiceStack's message-based semantics:
    /// Inspired by Ivan's proposal: http://korneliuk.blogspot.com/2012/08/servicestack-reusing-dtos.html
    /// 
    /// To align with ServiceStack's message-based design, an "Action": 
    ///   - is public and only supports a single argument the typed Request DTO 
    ///   - method name matches a HTTP Method or "Any" which is used as a fallback (for all methods) if it exists
    ///   - only returns object or void
    ///
    /// Notes: 
    /// Content Negotiation built-in, i.e. by default each method/route is automatically available in every registered Content-Type (HTTP Only).
    /// New API are also available in ServiceStack's typed service clients (they're actually even more succinct :)
    /// Any Views rendered is based on Request or Returned DTO type, see: http://razor.servicestack.net/#unified-stack

    [Route("/reqstars/search", "GET")]
    [Route("/reqstars/aged/{Age}")]
    public class SearchReqstars : IReturn<ReqstarsResponse>
    {
        public int? Age { get; set; }
    }

    [Route("/reqstars", "GET")]
    public class AllReqstars : IReturn<List<Reqstar>> { }

    public class ReqstarsResponse
    {
        public int Total { get; set; }
        public int? Aged { get; set; }
        public List<Reqstar> Results { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/reqstars/reset")]
    public class ResetReqstar : IReturnVoid { }

    [Route("/viewstate")]
    public class ViewState { }
    public class ViewStateResponse { }

    [Route("/reqstars/{Id}", "GET")]
    public class GetReqstar : IReturn<Reqstar>
    {
        public int Id { get; set; }
    }

    [Route("/reqstars/{Id}/delete")]
    public class DeleteReqstar : IReturnVoid
    {
        public int Id { get; set; }
    }

    [Route("/reqstars")]
    public class Reqstar : IReturn<List<Reqstar>>
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }

        public Reqstar() { }
        public Reqstar(int id, string firstName, string lastName, int age)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Age = age;
        }
    }

    [Route("/reqstars/{Id}", "PATCH")]
    public class UpdateReqstar : IReturn<Reqstar>
    {
        public int Id { get; set; }
        public int Age { get; set; }

        public UpdateReqstar() { }
        public UpdateReqstar(int id, int age)
        {
            Id = id;
            Age = age;
        }
    }

    public class RoutelessReqstar : IReturn<RoutelessReqstar>
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    [Route("/throw")]
    [Route("/throw/{StatusCode}")]
    [Route("/throw/{StatusCode}/{Message}")]
    public class Throw
    {
        public int? StatusCode { get; set; }
        public string Message { get; set; }
    }

    public class ThrowResponse
    {
        public string Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    [Api("Service Description")]
    [Route("/annotated/{Name}", "GET", Summary = @"GET Summary", Notes = "GET Notes")]
    [Route("/annotated/{Name}", "POST", Summary = @"POST Summary", Notes = "POST Notes")]
    public class Annotated
    {
        [ApiMember(Name = "Name", Description = "Name Description",
                   ParameterType = "path", DataType = "string", IsRequired = true)]
        public string Name { get; set; }
    }

    [Route("/headers/{Text}")]
    public class Headers : IReturn<HttpWebResponse>
    {
        public string Text { get; set; }
    }

    [Route("/strings/{Text}")]
    public class Strings : IReturn<string>
    {
        public string Text { get; set; }
    }

    [Route("/bytes/{Text}")]
    public class Bytes : IReturn<byte[]>
    {
        public string Text { get; set; }
    }

    [Route("/streams/{Text}")]
    public class Streams : IReturn<Stream>
    {
        public string Text { get; set; }
    }

    public class ReqstarsService : Service
    {
        public static Reqstar[] SeedData = new[] {
            new Reqstar(1, "Foo", "Bar", 20), 
            new Reqstar(2, "Something", "Else", 30), 
            new Reqstar(3, "Foo2", "Bar2", 20), 
        };

        [EnableCors]
        public void Options(Reqstar reqstar) { }

        public void Any(ResetReqstar request)
        {
            Db.DeleteAll<Reqstar>();
            Db.Insert(SeedData);
        }

        public object Get(SearchReqstars request)
        {
            if (request.Age.HasValue && request.Age <= 0)
                throw new ArgumentException("Invalid Age");

            return new ReqstarsResponse //matches ReqstarsResponse.cshtml razor view
            {
                Aged = request.Age,
                Total = Db.GetScalar<int>("select count(*) from Reqstar"),
                Results = request.Age.HasValue
                    ? Db.Select<Reqstar>(q => q.Age == request.Age.Value)
                    : Db.Select<Reqstar>()
            };
        }

        public object Any(AllReqstars request)
        {
            return Db.Select<Reqstar>();
        }

        [ClientCanSwapTemplates] //allow action-level filters
        public object Get(GetReqstar request)
        {
            return Db.Id<Reqstar>(request.Id);
        }

        public object Post(Reqstar request)
        {
            if (!request.Age.HasValue)
                throw new ArgumentException("Age is required");

            Db.Insert(request.TranslateTo<Reqstar>());
            return Db.Select<Reqstar>();
        }

        public object Patch(UpdateReqstar request)
        {
            Db.Update<Reqstar>(request, x => x.Id == request.Id);
            return Db.Id<Reqstar>(request.Id);
        }

        public void Any(DeleteReqstar request)
        {
            Db.DeleteById<Reqstar>(request.Id);
        }

        public object Any(RoutelessReqstar request)
        {
            return request;
        }

        public object Any(Throw request)
        {
            if (request.StatusCode.HasValue)
            {
                throw new HttpError(
                    request.StatusCode.Value,
                    typeof(NotImplementedException).Name,
                    request.Message);
            }
            throw new NotImplementedException(request.Message + " is not implemented");
        }

        public ViewStateResponse Get(ViewState request)
        {
            return new ViewStateResponse();
        }

        public Annotated Any(Annotated request)
        {
            return request;
        }

        public void Any(Headers request)
        {
            base.Request.Headers["X-Response"] = request.Text;
        }

        public string Any(Strings request)
        {
            return "Hello, " + (request.Text ?? "World!");
        }

        public byte[] Any(Bytes request)
        {
            return Guid.Parse(request.Text).ToByteArray();
        }

        public byte[] Any(Streams request)
        {
            return Guid.Parse(request.Text).ToByteArray();
        }
    }


    [Explicit("Find out why dev server doesn't handle multiple requests")]
    [TestFixture]
    public class ReqStarsServiceTests
    {
        public const string Host = "http://localhost:1338";
        private const string BaseUri = Host + "/";

        private AppHost appHost;

        private Stopwatch startedAt;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            LogManager.LogFactory = new ConsoleLogFactory();
            startedAt = Stopwatch.StartNew();
            appHost = new AppHost {
                EnableRazor = false, //Uncomment for faster tests!
            };
            appHost.Plugins.Add(new MsgPackFormat());
            appHost.Init();
            EndpointHost.Config.DebugMode = true;
        }

        private IDbConnection db;

        [SetUp]
        public void SetUp()
        {
            db = appHost.TryResolve<IDbConnectionFactory>().OpenDbConnection();
            db.DropAndCreateTable<Reqstar>();
            db.Insert(ReqstarsService.SeedData);
        }

        [TearDown]
        public void TearDown()
        {
            db.Dispose();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            "Time Taken {0}ms".Fmt(startedAt.ElapsedMilliseconds).Print();
            appHost.Dispose();
        }

        protected static IRestClient[] RestClients = 
		{
			new JsonServiceClient(BaseUri),
			new XmlServiceClient(BaseUri),
			new JsvServiceClient(BaseUri),
			new MsgPackServiceClient(BaseUri),
		};

        protected static IServiceClient[] ServiceClients = 
            RestClients.OfType<IServiceClient>().ToArray();


        [Test]
        public void Can_Process_OPTIONS_request_with_Cors_ActionFilter()
        {
            var webReq = (HttpWebRequest)WebRequest.Create(Host + "/reqstars");
            webReq.Method = "OPTIONS";
            using (var webRes = webReq.GetResponse())
            {
                Assert.That(webRes.Headers["Access-Control-Allow-Origin"], Is.EqualTo("*"));
                Assert.That(webRes.Headers["Access-Control-Allow-Methods"], Is.EqualTo("GET, POST, PUT, DELETE, OPTIONS"));
                Assert.That(webRes.Headers["Access-Control-Allow-Headers"], Is.EqualTo("Content-Type"));

                var response = webRes.GetResponseStream().ReadFully();
                Assert.That(response.Length, Is.EqualTo(0));
            }
        }


        [Test, TestCaseSource("RestClients")]
        public void Can_GET_AllReqstars(IRestClient client)
        {
            var allReqstars = client.Get<List<Reqstar>>("/reqstars");
            Assert.That(allReqstars.Count, Is.EqualTo(ReqstarsService.SeedData.Length));
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Can_SEND_AllReqstars_PrettyTypedApi(IServiceClient client)
        {
            var allReqstars = client.Send(new AllReqstars());
            Assert.That(allReqstars.Count, Is.EqualTo(ReqstarsService.SeedData.Length));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_GET_AllReqstars_PrettyRestApi(IRestClient client)
        {
            var request = new AllReqstars();
            var response = client.Get(request);

            Assert.That(request.ToUrl("GET"), Is.EqualTo("/reqstars"));
            Assert.That(response.Count, Is.EqualTo(ReqstarsService.SeedData.Length));
        }

        [Test]
        public void Can_GET_AllReqstars_View()
        {
            var html = "{0}/reqstars".Fmt(Host).GetStringFromUrl(acceptContentType: "text/html");
            html.Print();
            Assert.That(html, Is.StringContaining("<!--view:AllReqstars.cshtml-->"));
            Assert.That(html, Is.StringContaining("<!--template:HtmlReport.cshtml-->"));
        }


        [Test, TestCaseSource("RestClients")]
        public void Can_GET_SearchReqstars(IRestClient client)
        {
            var response = client.Get<ReqstarsResponse>("/reqstars/search");
            Assert.That(response.Results.Count, Is.EqualTo(ReqstarsService.SeedData.Length));
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Disallows_SEND_SearchReqstars_PrettyTypedApi(IServiceClient client)
        {
            try
            {
                var response = client.Send(new SearchReqstars());
                Assert.Fail("POST's to SearchReqstars should not be allowed");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(405));
                Assert.That(webEx.StatusDescription, Is.EqualTo("Method Not Allowed"));
            }
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_GET_SearchReqstars_PrettyRestApi(IRestClient client)
        {
            var request = new SearchReqstars();
            var response = client.Get(request);

            Assert.That(request.ToUrl("GET"), Is.EqualTo("/reqstars/search"));
            Assert.That(response.Results.Count, Is.EqualTo(ReqstarsService.SeedData.Length));
        }

        [Test, TestCaseSource("RestClients")]
        public void Invalid_GET_SearchReqstars_throws_typed_Response_PrettyRestApi(IRestClient client)
        {
            try
            {
                var response = client.Get(new SearchReqstars { Age = -1 });
                Assert.Fail("POST's to SearchReqstars should not be allowed");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(400));
                Assert.That(webEx.StatusDescription, Is.EqualTo("ArgumentException"));

                Assert.That(webEx.ResponseStatus.ErrorCode, Is.EqualTo("ArgumentException"));
                Assert.That(webEx.ResponseStatus.Message, Is.EqualTo("Invalid Age"));

                Assert.That(webEx.ResponseDto as ReqstarsResponse, Is.Not.Null);
            }
        }


        [Test, TestCaseSource("RestClients")]
        public void Can_GET_SearchReqstars_aged_20(IRestClient client)
        {
            var response = client.Get<ReqstarsResponse>("/reqstars/aged/20");
            Assert.That(response.Results.Count,
                Is.EqualTo(ReqstarsService.SeedData.Count(x => x.Age == 20)));
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Disallows_SEND_SearchReqstars_aged_20_PrettyTypedApi(IServiceClient client)
        {
            try
            {
                var response = client.Send(new SearchReqstars { Age = 20 });
                Assert.Fail("POST's to SearchReqstars should not be allowed");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(405));
                Assert.That(webEx.StatusDescription, Is.EqualTo("Method Not Allowed"));
            }
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_GET_SearchReqstars_aged_20_PrettyRestApi(IRestClient client)
        {
            var request = new SearchReqstars { Age = 20 };
            var response = client.Get(request);

            Assert.That(request.ToUrl("GET"), Is.EqualTo("/reqstars/aged/20"));
            Assert.That(response.Results.Count,
                Is.EqualTo(ReqstarsService.SeedData.Count(x => x.Age == 20)));
        }


        [Test, TestCaseSource("RestClients")]
        public void Can_GET_GetReqstar(IRestClient client)
        {
            var response = client.Get<Reqstar>("/reqstars/1");
            Assert.That(response.FirstName,
                Is.EqualTo(ReqstarsService.SeedData[0].FirstName));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_GET_GetReqstar_PrettyRestApi(IRestClient client)
        {
            var request = new GetReqstar { Id = 1 };
            var response = client.Get(request);

            Assert.That(request.ToUrl("GET"), Is.EqualTo("/reqstars/1"));
            Assert.That(response.FirstName,
                Is.EqualTo(ReqstarsService.SeedData[0].FirstName));
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Disallows_SEND_GetReqstar_PrettyTypedApi(IServiceClient client)
        {
            try
            {
                var response = client.Send(new GetReqstar { Id = 1 });
                Assert.Fail("POST's to GetReqstar should not be allowed");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(405));
                Assert.That(webEx.StatusDescription, Is.EqualTo("Method Not Allowed"));
            }
        }

        [Test]
        public void Can_GET_GetReqstar_View()
        {
            var html = "{0}/reqstars/1".Fmt(Host).GetStringFromUrl(acceptContentType: "text/html");
            html.Print();
            Assert.That(html, Is.StringContaining("<!--view:GetReqstar.cshtml-->"));
            Assert.That(html, Is.StringContaining("<!--template:HtmlReport.cshtml-->"));
        }


        public class EmptyResponse { }

        [Test, TestCaseSource("RestClients")]
        public void Can_DELETE_Reqstar(IRestClient client)
        {
            var response = client.Delete<EmptyResponse>("/reqstars/1/delete");

            var reqstarsLeft = db.Select<Reqstar>();

            Assert.That(reqstarsLeft.Count,
                Is.EqualTo(ReqstarsService.SeedData.Length - 1));
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Can_DELETE_Reqstar_PrettyTypedApi(IServiceClient client)
        {
            client.Send(new DeleteReqstar { Id = 1 });

            var reqstarsLeft = db.Select<Reqstar>();

            Assert.That(reqstarsLeft.Count,
                Is.EqualTo(ReqstarsService.SeedData.Length - 1));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_DELETE_Reqstar_PrettyRestApi(IRestClient client)
        {
            client.Delete(new DeleteReqstar { Id = 1 });

            var reqstarsLeft = db.Select<Reqstar>();

            Assert.That(reqstarsLeft.Count,
                Is.EqualTo(ReqstarsService.SeedData.Length - 1));
        }


        [Test, TestCaseSource("RestClients")]
        public void Can_PATCH_UpdateReqstar(IRestClient client)
        {
            var response = client.Patch<Reqstar>("/reqstars/1", new UpdateReqstar(1, 18));
            Assert.That(response.Age, Is.EqualTo(18));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_PATCH_UpdateReqstar_PrettyRestApi(IRestClient client)
        {
            var response = client.Patch(new UpdateReqstar(1, 18));
            Assert.That(response.Age, Is.EqualTo(18));
        }


        [Test, TestCaseSource("RestClients")]
        public void Can_CREATE_Reqstar(IRestClient client)
        {
            var response = client.Post<List<Reqstar>>("/reqstars",
                new Reqstar(4, "Just", "Created", 25));

            Assert.That(response.Count,
                Is.EqualTo(ReqstarsService.SeedData.Length + 1));
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Can_CREATE_Reqstar_PrettyTypedApi(IServiceClient client)
        {
            var response = client.Send(new Reqstar(4, "Just", "Created", 25));

            Assert.That(response.Count,
                Is.EqualTo(ReqstarsService.SeedData.Length + 1));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_CREATE_Reqstar_PrettyRestApi(IRestClient client)
        {
            var response = client.Post(new Reqstar(4, "Just", "Created", 25));

            Assert.That(response.Count,
                Is.EqualTo(ReqstarsService.SeedData.Length + 1));
        }

        [Test, TestCaseSource("RestClients")]
        public void Fails_to_CREATE_Empty_Reqstar_PrettyRestApi(IRestClient client)
        {
            try
            {
                var response = client.Post(new Reqstar());
                Assert.Fail("Should've thrown 400 Bad Request Error");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(400));
                Assert.That(webEx.StatusDescription, Is.EqualTo("ArgumentException"));

                Assert.That(webEx.ResponseStatus.ErrorCode, Is.EqualTo("ArgumentException"));
                Assert.That(webEx.ResponseStatus.Message, Is.EqualTo("Age is required"));

                Assert.That(webEx.ResponseDto as ErrorResponse, Is.Not.Null);
            }
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_GET_ResetReqstars(IRestClient client)
        {
            db.DeleteAll<Reqstar>();

            var response = client.Get<EmptyResponse>("/reqstars/reset");

            var reqstarsLeft = db.Select<Reqstar>();

            Assert.That(reqstarsLeft.Count, Is.EqualTo(ReqstarsService.SeedData.Length));
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Can_SEND_ResetReqstars_PrettyTypedApi(IServiceClient client)
        {
            db.DeleteAll<Reqstar>();

            client.Send(new ResetReqstar());

            var reqstarsLeft = db.Select<Reqstar>();

            Assert.That(reqstarsLeft.Count, Is.EqualTo(ReqstarsService.SeedData.Length));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_GET_ResetReqstars_PrettyRestApi(IRestClient client)
        {
            db.DeleteAll<Reqstar>();

            client.Get(new ResetReqstar());

            var reqstarsLeft = db.Select<Reqstar>();

            Assert.That(reqstarsLeft.Count, Is.EqualTo(ReqstarsService.SeedData.Length));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_GET_RoutelessReqstar_PrettyRestApi(IRestClient client)
        {
            var request = new RoutelessReqstar {
                Id = 1,
                FirstName = "Foo",
                LastName = "Bar",
            };

            var response = client.Get(request);

            var format = ((ServiceClientBase)client).Format;
            Assert.That(request.ToUrl("GET", format), Is.EqualTo(
                "/{0}/syncreply/RoutelessReqstar?id=1&firstName=Foo&lastName=Bar".Fmt(format)));
            Assert.That(response.Id, Is.EqualTo(request.Id));
            Assert.That(response.FirstName, Is.EqualTo(request.FirstName));
            Assert.That(response.LastName, Is.EqualTo(request.LastName));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_POST_RoutelessReqstar_PrettyRestApi(IRestClient client)
        {
            var request = new RoutelessReqstar {
                Id = 1,
                FirstName = "Foo",
                LastName = "Bar",
            };

            var response = client.Post(request);

            var format = ((ServiceClientBase)client).Format;
            Assert.That(request.ToUrl("POST", format), Is.EqualTo(
                "/{0}/syncreply/RoutelessReqstar".Fmt(format)));
            Assert.That(response.Id, Is.EqualTo(request.Id));
            Assert.That(response.FirstName, Is.EqualTo(request.FirstName));
            Assert.That(response.LastName, Is.EqualTo(request.LastName));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_download_Headers_response(IRestClient client)
        {
            HttpWebResponse response = client.Get(new Headers { Text = "Test" });
            Assert.That(response.Headers["X-Response"], Is.EqualTo("Test"));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_download_Strings_response(IRestClient client)
        {
            string response = client.Get(new Strings { Text = "Test" });
            Assert.That(response, Is.EqualTo("Hello, Test"));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_download_Bytes_response(IRestClient client)
        {
            var guid = Guid.NewGuid();
            byte[] response = client.Get(new Bytes { Text = guid.ToString() });
            Assert.That(new Guid(response), Is.EqualTo(guid));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_download_Streams_response(IRestClient client)
        {
            var guid = Guid.NewGuid();
            Stream response = client.Get(new Streams { Text = guid.ToString() });
            using (response)
            {
                var bytes = response.ReadFully();
                Assert.That(new Guid(bytes), Is.EqualTo(guid));
            }
        }
    }
}