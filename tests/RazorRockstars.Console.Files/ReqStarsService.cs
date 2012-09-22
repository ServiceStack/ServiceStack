using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.OrmLite;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;

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
    /// Any Views rendered is based on Returned DTO type, see: http://razor.servicestack.net/#unified-stack
    
    [Route("/reqstars", "GET")]
    [Route("/reqstars/aged/{Age}")]
    public class SearchReqstars : IReturn<ReqstarsResponse>
    {
        public int? Age { get; set; }
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

    
    public class ReqstarsService : Service
    {
        public static Reqstar[] SeedData = new[] {
            new Reqstar(1, "Foo", "Bar", 20), 
            new Reqstar(2, "Something", "Else", 30), 
            new Reqstar(3, "Foo2", "Bar2", 20), 
        };

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

        public void Any(DeleteReqstar request)
        {
            Db.DeleteById<Reqstar>(request.Id);
        }
    }
    

    [TestFixture]
    public class ReqStarsServiceTests
    {
        private const string ListeningOn = "http://*:1337/";
        public const string Host = "http://localhost:1337";

        private const string BaseUri = Host + "/";

        JsonServiceClient client;
        //XmlServiceClient client;
        //JsvServiceClient client;

        private AppHost appHost;

        private Stopwatch startedAt;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            LogManager.LogFactory = new ConsoleLogFactory();
            startedAt = Stopwatch.StartNew();
            appHost = new AppHost();
            appHost.Init();
            appHost.Start(ListeningOn);

            client = new JsonServiceClient(BaseUri);
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
        
        public class EmptyResponse { }


        [Test]
        public void Can_GET_SearchReqstars()
        {
            var response = client.Get<ReqstarsResponse>("/reqstars");
            Assert.That(response.Results.Count, Is.EqualTo(ReqstarsService.SeedData.Length));
        }

        [Test]
        public void Disallows_GET_SearchReqstars_PrettyTypedApi()
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

        [Test]
        public void Can_GET_SearchReqstars_PrettyRestApi()
        {
            var request = new SearchReqstars();
            var response = client.Get(request);

            Assert.That(request.ToUrl("GET"), Is.EqualTo("/reqstars"));
            Assert.That(response.Results.Count, Is.EqualTo(ReqstarsService.SeedData.Length));
        }

        [Test]
        public void Invalid_GET_SearchReqstars_throws_typed_Response_PrettyRestApi()
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

                Assert.That(webEx.ResponseStatus, Is.Not.Null);
                Assert.That(webEx.ResponseDto, Is.Not.Null);

                var typedError = webEx.ResponseDto as ReqstarsResponse;
                Assert.That(typedError, Is.Not.Null);
                Assert.That(typedError.ResponseStatus.ErrorCode, Is.EqualTo("ArgumentException"));
                Assert.That(typedError.ResponseStatus.Message, Is.EqualTo("Invalid Age"));
            }
        }


        [Test]
        public void Can_GET_SearchReqstars_aged_20()
        {
            var response = client.Get<ReqstarsResponse>("/reqstars/aged/20");
            Assert.That(response.Results.Count,
                Is.EqualTo(ReqstarsService.SeedData.Count(x => x.Age == 20)));
        }

        [Test]
        public void Disallows_GET_SearchReqstars_aged_20_PrettyTypedApi()
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

        [Test]
        public void Can_GET_SearchReqstars_aged_20_PrettyRestApi()
        {
            var request = new SearchReqstars { Age = 20 };
            var response = client.Get(request);

            Assert.That(request.ToUrl("GET"), Is.EqualTo("/reqstars/aged/20"));
            Assert.That(response.Results.Count,
                Is.EqualTo(ReqstarsService.SeedData.Count(x => x.Age == 20)));
        }


        [Test]
        public void Can_DELETE_Reqstar()
        {
            var response = client.Delete<EmptyResponse>("/reqstars/1/delete");

            var reqstarsLeft = db.Select<Reqstar>();

            Assert.That(reqstarsLeft.Count,
                Is.EqualTo(ReqstarsService.SeedData.Length - 1));
        }

        [Test]
        public void Can_DELETE_Reqstar_PrettyTypedApi()
        {
            client.Send(new DeleteReqstar { Id = 1 });

            var reqstarsLeft = db.Select<Reqstar>();

            Assert.That(reqstarsLeft.Count,
                Is.EqualTo(ReqstarsService.SeedData.Length - 1));
        }

        [Test]
        public void Can_DELETE_Reqstar_PrettyRestApi()
        {
            client.Delete(new DeleteReqstar { Id = 1 });

            var reqstarsLeft = db.Select<Reqstar>();

            Assert.That(reqstarsLeft.Count,
                Is.EqualTo(ReqstarsService.SeedData.Length - 1));
        }


        [Test]
        public void Can_CREATE_Reqstar()
        {
            var response = client.Post<List<Reqstar>>("/reqstars",
                new Reqstar(4, "Just", "Created", 25));

            Assert.That(response.Count,
                Is.EqualTo(ReqstarsService.SeedData.Length + 1));
        }

        [Test]
        public void Can_CREATE_Reqstar_PrettyTypedApi()
        {
            var response = client.Send(new Reqstar(4, "Just", "Created", 25));

            Assert.That(response.Count,
                Is.EqualTo(ReqstarsService.SeedData.Length + 1));
        }

        [Test]
        public void Can_CREATE_Reqstar_PrettyRestApi()
        {
            var response = client.Post(new Reqstar(4, "Just", "Created", 25));

            Assert.That(response.Count,
                Is.EqualTo(ReqstarsService.SeedData.Length + 1));
        }

        [Test]
        public void Fails_to_CREATE_Empty_Reqstar_PrettyRestApi()
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

                Assert.That(webEx.ResponseStatus, Is.Not.Null);

                var responseDto = webEx.ResponseDto as ErrorResponse;
                Assert.That(responseDto, Is.Not.Null);
                Assert.That(responseDto.ResponseStatus.ErrorCode, Is.EqualTo("ArgumentException"));
                Assert.That(responseDto.ResponseStatus.Message, Is.EqualTo("Age is required"));
            }
        }

        [Test]
        public void Can_GET_ResetReqstars()
        {
            db.DeleteAll<Reqstar>();

            var response = client.Get<EmptyResponse>("/reqstars/reset");

            var reqstarsLeft = db.Select<Reqstar>();

            Assert.That(reqstarsLeft.Count, Is.EqualTo(ReqstarsService.SeedData.Length));
        }

        [Test]
        public void Can_SEND_ResetReqstars_PrettyTypedApi()
        {
            db.DeleteAll<Reqstar>();

            client.Send(new ResetReqstar());

            var reqstarsLeft = db.Select<Reqstar>();

            Assert.That(reqstarsLeft.Count, Is.EqualTo(ReqstarsService.SeedData.Length));
        }

        [Test]
        public void Can_GET_ResetReqstars_PrettyRestApi()
        {
            db.DeleteAll<Reqstar>();

            client.Get(new ResetReqstar());

            var reqstarsLeft = db.Select<Reqstar>();

            Assert.That(reqstarsLeft.Count, Is.EqualTo(ReqstarsService.SeedData.Length));
        }
    }
}