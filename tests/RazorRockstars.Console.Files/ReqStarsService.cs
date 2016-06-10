using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using System.Web.ModelBinding;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.Host;
using ServiceStack.Logging;
using ServiceStack.MsgPack;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Web;

namespace RazorRockstars.Console.Files
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

    [System.ComponentModel.Description("Description for ACodeGenTest")]
    public class ACodeGenTest
    {
        [ServiceStack.DataAnnotations.Description("Description for FirstField")]
        public int FirstField { get; set; }

        public List<string> SecondFields { get; set; }
    }

    [DataContract]
    public class ACodeGenTestResponse
    {
        [DataMember]
        [ServiceStack.DataAnnotations.Description("Description for FirstResult")]
        public int FirstResult { get; set; }

        [DataMember]
        [ApiMember(Description = "Description for SecondResult")]
        public int SecondResult { get; set; }
    }

    [Route("/reqstars/search", "GET")]
    [Route("/reqstars/aged/{Age}")]
    public class SearchReqstars : IReturn<ReqstarsResponse>
    {
        public int? Age { get; set; }
    }

    [Route("/reqstars", "GET")]
    public class AllReqstars : IReturn<List<Reqstar>> { }

    [Route("/reqstars/cached/{Aged}", "GET")]
    public class CachedAllReqstars : IReturn<ReqstarsResponse>
    {
        public int Aged { get; set; }        
    }

    public class ReqstarsResponse
    {
        public int Total { get; set; }
        public int? Aged { get; set; }
        public List<Reqstar> Results { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/reqstars/reset")]
    public class ResetReqstar : IReturnVoid { }

    [Route("/reqstars/{Id}", "GET")]
    public class GetReqstar : IReturn<Reqstar>
    {
        public int Id { get; set; }
    }

    [Route("/cached/reqstars/{Id}", "GET")]
    public class GetCachedReqstar : IReturn<Reqstar>
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
        public bool Alive { get; set; }

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

    [Route("/modelerror")]
    [Route("/modelerror/{StatusCode}")]
    [Route("/modelerror/{StatusCode}/{Message}")]
    public class ModelError : IReturn<ModelErrorResponse>
    {
        public int? StatusCode { get; set; }
        public string Message { get; set; }
    }

    public class ModelErrorResponse
    {
        public string Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class RoutelessReqstar : IReturn<RoutelessReqstar>
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class ReqstarsByNames : List<string> { }

    [Route("/richrequest")]
    public class RichRequest : IReturn<RichRequest>
    {
        public List<string> StringList { get; set; }
        public HashSet<string> StringSet { get; set; }
        public string[] StringArray { get; set; }
    }

    public class ReqstarsService : Service
    {
        public static Reqstar[] SeedData = new[] {
            new Reqstar(1, "Foo", "Bar", 20), 
            new Reqstar(2, "Something", "Else", 30), 
            new Reqstar(3, "Foo2", "Bar2", 20), 
        };

        public object Any(ACodeGenTest request)
        {
            return new ACodeGenTestResponse { FirstResult = request.FirstField };
        }

        [EnableCors]
        public void Options(Reqstar reqstar) { }

        public void Any(ResetReqstar request)
        {
            Db.DeleteAll<Reqstar>();
            Db.Insert(SeedData);
        }

        public ReqstarsResponse Get(SearchReqstars request)
        {
            if (request.Age.HasValue && request.Age <= 0)
                throw new ArgumentException("Invalid Age");

            return new ReqstarsResponse //matches ReqstarsResponse.cshtml razor view
            {
                Aged = request.Age,
                Total = Db.Scalar<int>("select count(*) from Reqstar"),
                Results = request.Age.HasValue
                    ? Db.Select<Reqstar>(q => q.Age == request.Age.Value)
                    : Db.Select<Reqstar>()
            };
        }

        public List<Reqstar> Any(AllReqstars request)
        {
            return Db.Select<Reqstar>();
        }

        public object Any(CachedAllReqstars request)
        {
            if (request.Aged <= 0)
                throw new ArgumentException("Invalid Age");

            var cacheKey = typeof(CachedAllReqstars).Name;
            return base.Request.ToOptimizedResultUsingCache(base.Cache, cacheKey, () => 
                new ReqstarsResponse {
                    Aged = request.Aged,
                    Total = Db.Scalar<int>("select count(*) from Reqstar"),
                    Results = Db.Select<Reqstar>(q => q.Age == request.Aged)
                });
        }

        [ClientCanSwapTemplates] //allow action-level filters
        public Reqstar Get(GetReqstar request)
        {
            return Db.SingleById<Reqstar>(request.Id);
        }

        public object Get(GetCachedReqstar request)
        {
            return Request.ToOptimizedResultUsingCache(Cache, request.GetType().Name, () =>
                Db.SingleById<Reqstar>(request.Id));
        }

        public object Post(Reqstar request)
        {
            if (!request.Age.HasValue)
                throw new ArgumentException("Age is required");

            Db.Insert(request.ConvertTo<Reqstar>());
            return Db.Select<Reqstar>();
        }

        public Reqstar Patch(UpdateReqstar request)
        {
            Db.Update<Reqstar>(request, x => x.Id == request.Id);
            return Db.SingleById<Reqstar>(request.Id);
        }

        public void Any(DeleteReqstar request)
        {
            Db.DeleteById<Reqstar>(request.Id);
        }

        public RoutelessReqstar Any(RoutelessReqstar request)
        {
            return request;
        }

        public List<Reqstar> Any(ReqstarsByNames request)
        {
            return Db.Select<Reqstar>(q => Sql.In(q.FirstName, request.ToArray()));
        }

        public MixedDataContractResponse Any(MixedDataContract request)
        {
            return new MixedDataContractResponse();
        }

        public InheritsResponse Any(Inherits request)
        {
            return new InheritsResponse();
        }

        public RichRequest Get(RichRequest request)
        {
            return request;
        }

        public object Any(ModelError request)
        {
            if (request.StatusCode.HasValue)
            {
                throw new HttpError(
                    request.StatusCode.Value,
                    typeof(ArgumentException).Name,
                    request.Message);
            }
            throw new ArgumentException(request.Message + " was triggered by client");
        }
    }


    [Route("/ignore/{ignore}")]
    public class IgnoreRoute1
    {
        public string Name { get; set;  }
    }
    [Route("/ignore/{ignore}/between")]
    public class IgnoreRoute2
    {
        public string Name { get; set; }
    }
    [Route("/ignore/{ignore}/and/{ignore}")]
    [Route("/ignore/{ignore}/with/{name}")]
    public class IgnoreRoute3
    {
        public string Name { get; set; }
    }
    [Route("/ignorewildcard/{ignore*}")]
    public class IgnoreWildcardRoute
    {
        public string Name { get; set; }
    }

    public class IgnoreService : Service
    {
        public object Get(IgnoreRoute1 request)
        {
            return request;
        }

        public object Get(IgnoreRoute2 request)
        {
            return request;
        }

        public object Get(IgnoreRoute3 request)
        {
            return request;
        }

        public object Get(IgnoreWildcardRoute request)
        {
            return request;
        }
    }

    [DataContract(Name = "MixName", Namespace = "http://mix.namespace.com")]
    public class MixedDataContract : IReturn<MixedDataContractResponse>
    {
        [DataMember(Name = "MixId", Order = 1, EmitDefaultValue = false, IsRequired = true)]
        public int Id { get; set; }
    }

    [DataContract(Name = "MixedDCResponse")]
    public class MixedDataContractResponse
    {
        [DataMember(Name = "MixTotal")]
        public int Total { get; set; }
        [DataMember(Order = 2)]
        public int? Aged { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public List<Rockstar> Results { get; set; }
        [DataMember(IsRequired = true)]
        public string Required { get; set; }

        [DataMember]
        public MixDataType MixDataType { get; set; }
    }

    public class MixDataType
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Inherits : InheritsBase
    {
        public string Name { get; set; }
    }

    public class InheritsBase
    {
        public int Id { get; set; }
    }

    public class InheritsResponse
    {
        public string Result { get; set; }
    }

    [Route("/filterattributes")]
    public class RequestDto : IReturn<ResponseDto> { }
    public class ResponseDto
    {
        public string Foo { get; set; }
    }

    [MyResponseFilter]
    [MyRequestFilter]
    public class MyService : Service
    {
        public object Get(RequestDto request)
        {
            return new ResponseDto { Foo = "Bar" };
        }
    }

    public class MyResponseFilterAttribute : ResponseFilterAttribute
    {
        public static int Called = 0;
        public override void Execute(IRequest req, IResponse res, object responseDto)
        {
            Called++;
            var x = responseDto;
        }
    }

    public class MyRequestFilterAttribute : RequestFilterAttribute
    {
        public static int Called = 0;
        public override void Execute(IRequest req, IResponse res, object responseDto)
        {
            Called++;
            var x = responseDto;
        }
    }

    [TestFixture]
    public class ReqStarsServiceTests
    {
        private const string ListeningOn = "http://*:2337/";
        public const string Host = "http://localhost:2337";

        private const string BaseUri = Host + "/";

        private AppHost appHost;

        private Stopwatch startedAt;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            LogManager.LogFactory = new ConsoleLogFactory();
            startedAt = Stopwatch.StartNew();
            appHost = new AppHost {
                EnableRazor = true, //Uncomment for faster tests!
            };
            appHost.Plugins.Add(new MsgPackFormat());
            //appHost.Plugins.Add(new NetSerializerFormat());
            //Fast
            appHost.Init();
            HostContext.Config.DebugMode = true;
            appHost.Start(ListeningOn);
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

        [Explicit("Debug Run")]
        [Test]
        public void RunFor10Mins()
        {
            Thread.Sleep(TimeSpan.FromMinutes(10));
        }

        [Explicit("Concurrent Run")]
        [Test]
        public void Concurrent_GetReqstar_JSON()
        {
            const int NoOfThreads = 100;

            var client = new JsonServiceClient(BaseUri);

            int completed = 0;
            Exception lastEx = null;
            NoOfThreads.TimesAsync(i => {
                try
                {
                    var response = client.Get(new GetReqstar { Id = 1 });
                    Assert.That(response.Id, Is.EqualTo(1));
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                }
                finally
                {
                    Interlocked.Increment(ref completed);
                }
            });

            while (completed < NoOfThreads)
                Thread.Sleep(100);

            if (lastEx != null)
                throw lastEx;
        }


        [Explicit("Concurrent Run")]
        [Test]
        public void Concurrent_GetReqstar_Razor()
        {
            const int NoOfThreads = 100;

            int completed = 0;
            Exception lastEx = null;
            string lastText = null;
            NoOfThreads.TimesAsync(i => {
                try
                {
                    lastText = "{0}/reqstars/1".Fmt(Host).GetStringFromUrl();
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                }
                finally
                {
                    Interlocked.Increment(ref completed);
                }
            });

            while (completed < NoOfThreads)
                Thread.Sleep(100);

            lastText.Print();

            if (lastEx != null)
                throw lastEx;
        }

        protected static IRestClient[] RestClients = 
		{
			new JsonServiceClient(BaseUri),
			new XmlServiceClient(BaseUri),
			new JsvServiceClient(BaseUri),
			new MsgPackServiceClient(BaseUri),
            //new NetSerializerServiceClient(BaseUri), 
		};

        protected static IServiceClient[] ServiceClients =
            RestClients.OfType<IServiceClient>().ToArray();



        [Test, TestCaseSource("RestClients")]
        public void Does_allow_sending_collections(IServiceClient client)
        {
            var results = client.Send<List<Reqstar>>(new ReqstarsByNames { "Foo", "Foo2" });
        }

        [Test, TestCaseSource("RestClients")]
        public void Does_execute_request_and_response_filter_attributes(IRestClient client)
        {
            MyRequestFilterAttribute.Called = MyResponseFilterAttribute.Called = 0;
            var response = client.Get(new RequestDto());
            Assert.That(response.Foo, Is.EqualTo("Bar"));
            Assert.That(MyRequestFilterAttribute.Called, Is.EqualTo(1));
            Assert.That(MyResponseFilterAttribute.Called, Is.EqualTo(1));
        }

        [Test]
        public void Can_Process_OPTIONS_request_with_Cors_ActionFilter()
        {
            var webReq = (HttpWebRequest)WebRequest.Create(Host + "/reqstars");
            webReq.Method = "OPTIONS";
            using (var r = webReq.GetResponse())
            {
                Assert.That(r.Headers[HttpHeaders.AllowOrigin], Is.EqualTo(CorsFeature.DefaultOrigin));
                Assert.That(r.Headers[HttpHeaders.AllowMethods], Is.EqualTo(CorsFeature.DefaultMethods));
                Assert.That(r.Headers[HttpHeaders.AllowHeaders], Is.EqualTo(CorsFeature.DefaultHeaders));

                var response = r.GetResponseStream().ReadFully();
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
            var html = "{0}/reqstars".Fmt(Host).GetStringFromUrl(accept: "text/html");
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
            var html = "{0}/reqstars/1".Fmt(Host).GetStringFromUrl(accept: "text/html");
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
            var response = client.Post<List<Reqstar>>("/reqstars", new Reqstar(4, "Just", "Created", 25));

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
                "/{0}/reply/RoutelessReqstar?id=1&firstName=Foo&lastName=Bar".Fmt(format)));
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
                "/{0}/reply/RoutelessReqstar".Fmt(format)));
            Assert.That(response.Id, Is.EqualTo(request.Id));
            Assert.That(response.FirstName, Is.EqualTo(request.FirstName));
            Assert.That(response.LastName, Is.EqualTo(request.LastName));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_GET_RichRequest_PrettyRestApi(IRestClient client)
        {
            var request = new RichRequest {
                StringArray = new[] { "a", "b", "c" },
                StringList = new List<string> { "d", "e", "f" },
                StringSet = new HashSet<string> { "g", "h", "i" },
            };

            Assert.That(request.ToUrl("GET"), Is.EqualTo("/richrequest?stringList=d,e,f&stringSet=g,h,i&stringArray=a,b,c".Replace(",","%2C")));

            var response = client.Get(request);

            Assert.That(response.StringArray, Is.EquivalentTo(request.StringArray));
            Assert.That(response.StringList, Is.EquivalentTo(request.StringList));
            Assert.That(response.StringSet, Is.EquivalentTo(request.StringSet));
        }

        [Test]
        public void Does_Cache_RazorPage()
        {
            var html = "{0}/reqstars/cached/10".Fmt(Host).GetStringFromUrl();
            Assert.That(html, Is.StringContaining("<h1>Counter:10</h1>"));
            html = "{0}/reqstars/cached/20".Fmt(Host).GetStringFromUrl();
            Assert.That(html, Is.StringContaining("<h1>Counter:10</h1>"));
        }

        [Test]
        public void Does_ignore_all_types_of_routes()
        {
            var response1 = "{0}/ignore/AnyThing?Name=foo".Fmt(Host).GetJsonFromUrl().FromJson<IgnoreRoute1>();
            Assert.That(response1.Name, Is.EqualTo("foo"));
            
            var response2 = "{0}/ignore/AnyThing/between?Name=foo".Fmt(Host).GetJsonFromUrl().FromJson<IgnoreRoute2>();
            Assert.That(response2.Name, Is.EqualTo("foo"));

            var response3 = "{0}/ignore/AnyThing/and/everything?Name=foo".Fmt(Host).GetJsonFromUrl().FromJson<IgnoreRoute3>();
            Assert.That(response3.Name, Is.EqualTo("foo"));
            response3 = "{0}/ignore/AnyThing/with/foo".Fmt(Host).GetJsonFromUrl().FromJson<IgnoreRoute3>();
            Assert.That(response3.Name, Is.EqualTo("foo"));
            
            var response4 = "{0}/ignorewildcard?Name=foo".Fmt(Host).GetJsonFromUrl().FromJson<IgnoreWildcardRoute>();
            Assert.That(response4.Name, Is.EqualTo("foo"));
            response4 = "{0}/ignorewildcard/a?Name=foo".Fmt(Host).GetJsonFromUrl().FromJson<IgnoreWildcardRoute>();
            Assert.That(response4.Name, Is.EqualTo("foo"));
            response4 = "{0}/ignorewildcard/a/b?Name=foo".Fmt(Host).GetJsonFromUrl().FromJson<IgnoreWildcardRoute>();
            Assert.That(response4.Name, Is.EqualTo("foo"));
        }

        [Test]
        public void Does_handle_ignored_routes()
        {
            var restPath = new RestPath(typeof(IgnoreRoute3), "/ignore/{ignore}/with/{name}");
            var pathComponents = RestPath.GetPathPartsForMatching("/ignore/AnyThing/with/foo");
            var score = restPath.MatchScore("GET", pathComponents);

            Assert.That(score, Is.GreaterThan(0));

            var request = (IgnoreRoute3) restPath.CreateRequest("/ignore/AnyThing/with/foo");
            Assert.That(request.Name, Is.EqualTo("foo"));
        }

        [Test]
        public void Does_RenderPartial_and_RenderAction()
        {
            var html = "{0}/Pages/PartialExamples".Fmt(Host)
                .GetStringFromUrl();

            Assert.That(html, Is.StringContaining("<!--view:PartialExamples.cshtml-->"));
            Assert.That(html, Is.StringContaining("<!--view:GetReqstar.cshtml-->"));
            Assert.That(html, Is.StringContaining("<!--view:CustomReqstar.cshtml-->"));
        }

        [Test]
        public void Does_render_cached_response()
        {
            var html1 = "{0}/cached/reqstars/1".Fmt(Host)
                .GetStringFromUrl();

            Assert.That(html1, Is.StringContaining("<!--view:GetCachedReqstar.cshtml-->"));

            var html2 = "{0}/cached/reqstars/1".Fmt(Host)
                .GetStringFromUrl();

            Assert.That(html2, Is.StringContaining("<!--view:GetCachedReqstar.cshtml-->"));

            Assert.That(html1, Is.EqualTo(html2));

            html2.Print();
        }
    }

}