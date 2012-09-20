using System.Data;
using System.Diagnostics;
using System.Linq;
using Alternate.ExpressLike.Controller.Proposal;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.OrmLite;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;

namespace RazorRockstars.Console.Files
{
    //Proposal 2: Keeping ServiceStack's message-based semantics
    //Inspired by Ivan's proposal: http://korneliuk.blogspot.com/2012/08/servicestack-reusing-dtos.html

    [TestFixture]
    public class ReqStarsServiceTests
    {
        private const string ListeningOn = "http://*:1337/";
        public const string Host = "http://localhost:1337";

        //private const string ListeningOn = "http://*:1337/subdir/subdir2/";
        //private const string Host = "http://localhost:1337/subdir/subdir2";

        private const string BaseUri = Host + "/";

        JsonServiceClient client;

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
            db.Insert(Reqstar.SeedData);
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
            try
            {
                var response = client.Get<ReqstarsResponse>("/reqstars");
                Assert.That(response.Results.Count, Is.EqualTo(Reqstar.SeedData.Length));
            }
            catch (System.Exception ex)
            {
                ex.Message.Print();
                throw;
            }
        }

        [Test]
        public void Can_GET_SearchReqstars_aged_20()
        {
            var response = client.Get<ReqstarsResponse>("/reqstars/aged/20");
            Assert.That(response.Results.Count,
                Is.EqualTo(Reqstar.SeedData.Count(x => x.Age == 20)));
        }

        [Test]
        public void Can_DELETE_Reqstar()
        {
            var response = client.Delete<EmptyResponse>("/reqstars/1/delete");

            var reqstarsLeft = db.Select<Reqstar>();

            Assert.That(reqstarsLeft.Count,
                Is.EqualTo(Reqstar.SeedData.Length - 1));
        }

        [Test]
        public void Can_CREATE_Reqstar()
        {
            var response = client.Post<ReqstarsResponse>("/reqstars",
                new Reqstar(4, "Just", "Created", 25));

            Assert.That(response.Results.Count,
                Is.EqualTo(Reqstar.SeedData.Length + 1));
        }

        //TODO: FIX
        //[Test]
        //public void Can_GET_ResetReqstars()
        //{
        //    db.DeleteAll<Reqstar>();

        //    var response = client.Get<EmptyResponse>("/reqstars/reset");

        //    var reqstarsLeft = db.Select<Reqstar>();

        //    Assert.That(reqstarsLeft.Count, Is.EqualTo(Reqstar.SeedData));
        //}
    }

    [Route("/reqstars", "GET")]
    [Route("/reqstars/aged/{Age}")]
    public class SearchReqstars : IReturn<ReqstarsResponse>
    {
        public int Id { get; set; }
        public int? Age { get; set; }
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

    //[Authenticate]
    public class ReqStarsService : Service
    {
        public object Get(SearchReqstars request)
        {
            return new ReqstarsResponse //matches ReqstarsResponse.cshtml razor view
            {
                Aged = request.Age,
                Total = Db.GetScalar<int>("select count(*) from Reqstar"),
                Results = request.Age.HasValue
                    ? Db.Select<Reqstar>(q => q.Age == request.Age.Value)
                    : Db.Select<Reqstar>()
            };
        }

        [ClientCanSwapTemplates] //aka action-level filters
        public object Get(GetReqstar request)
        {
            return Db.Id<Reqstar>(request.Id);
        }

        public object Post(Reqstar request)
        {
            Db.Insert(request.TranslateTo<Reqstar>());
            return Get(new SearchReqstars());
        }

        public void Any(DeleteReqstar request)
        {
            Db.DeleteById<Reqstar>(request.Id);
        }

        public void Any(ResetReqstar request)
        {
            Db.DeleteAll<Reqstar>();
            Db.Insert(Reqstar.SeedData);
        }
    }

}