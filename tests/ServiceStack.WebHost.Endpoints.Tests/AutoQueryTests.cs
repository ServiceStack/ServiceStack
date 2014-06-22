using System.Linq;
using Funq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class AutoQueryAppHost : AppSelfHostBase
    {
        public AutoQueryAppHost()
            : base("AutoQuery", typeof(AutoQueryService).Assembly) {}

        public override void Configure(Container container)
        {
            container.Register<IDbConnectionFactory>(
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.DropAndCreateTable<Rockstar>();
                db.DropAndCreateTable<RockstarAlbum>();
                db.InsertAll(SeedData);
                db.InsertAll(SeedDataAlbum);                
            }

            Plugins.Add(new AutoQueryFeature { MaxLimit = 100 });
        }

        public static Rockstar[] SeedData = new[] {
            new Rockstar { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 },
            new Rockstar { Id = 2, FirstName = "Jim", LastName = "Morrison", Age = 27 },
            new Rockstar { Id = 3, FirstName = "Kurt", LastName = "Cobain", Age = 27 },
            new Rockstar { Id = 4, FirstName = "Elvis", LastName = "Presley", Age = 42 },
            new Rockstar { Id = 5, FirstName = "David", LastName = "Grohl", Age = 44 },
            new Rockstar { Id = 6, FirstName = "Eddie", LastName = "Vedder", Age = 48 },
            new Rockstar { Id = 7, FirstName = "Michael", LastName = "Jackson", Age = 50 },
        };

        public static RockstarAlbum[] SeedDataAlbum = new[] {
            new RockstarAlbum { Id = 1, RockstarId = 1, AlbumName = "Electric Ladyland" },    
            new RockstarAlbum { Id = 3, RockstarId = 3, AlbumName = "Never Mind" },    
            new RockstarAlbum { Id = 5, RockstarId = 5, AlbumName = "Foo Fighters" },    
            new RockstarAlbum { Id = 6, RockstarId = 6, AlbumName = "Into the Wild" },    
        };
    }

    public class QueryRockstars : QueryBase<Rockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryCustomRockstars : QueryBase<Rockstar, CustomRockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryRockstarAlbums : QueryBase<Rockstar, CustomRockstar>, IJoin<Rockstar, RockstarAlbum>
    {
        public int? Age { get; set; }
        public string AlbumName { get; set; }
    }

    public class QueryRockstarAlbumsLeftJoin : QueryBase<Rockstar, CustomRockstar>, ILeftJoin<Rockstar, RockstarAlbum>
    {
        public int? Age { get; set; }
        public string AlbumName { get; set; }
    }


    public class QueryOverridedRockstars : QueryBase<Rockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryOverridedCustomRockstars : QueryBase<Rockstar, CustomRockstar>
    {
        public int? Age { get; set; }
    }

    public class RockstarAlbum
    {
        public int Id { get; set; }
        public int RockstarId { get; set; }
        public string AlbumName { get; set; }
    }

    public class CustomRockstar
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public string AlbumName { get; set; }
    }

    public class AutoQueryService : Service
    {
        public IAutoQuery AutoQuery { get; set; }

        //Override with custom impl
        public object Any(QueryOverridedRockstars dto)
        {
            var q = AutoQuery.CreateQuery(dto, Request.GetRequestParams());
            q.Take(1);
            return AutoQuery.Execute(dto, q);
        }

        public object Any(QueryOverridedCustomRockstars dto)
        {
            var q = AutoQuery.CreateQuery(dto, Request.GetRequestParams());
            q.Take(1);
            return AutoQuery.Execute(dto, q);
        }
    }

    [TestFixture]
    public class AutoQueryTests
    {
        private readonly ServiceStackHost appHost;
        public IServiceClient client;

        private static readonly int TotalRockstars = AutoQueryAppHost.SeedData.Length;
        private static readonly int TotalAlbums = AutoQueryAppHost.SeedDataAlbum.Length;

        public AutoQueryTests()
        {
            appHost = new AutoQueryAppHost()
                .Init()
                .Start(Config.ListeningOn);

            client = new JsonServiceClient(Config.ListeningOn);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_execute_basic_query()
        {
            var response = client.Get(new QueryRockstars());

            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars));
        }

        [Test]
        public void Can_execute_overridden_basic_query()
        {
            var response = client.Get(new QueryOverridedRockstars());

            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(1));
        }

        [Test]
        public void Can_execute_explicit_equality_condition_on_overridden_CustomRockstar()
        {
            var response = client.Get(new QueryOverridedCustomRockstars { Age = 27 });

            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(response.Results.Count, Is.EqualTo(1));
        }

        [Test]
        public void Can_execute_basic_query_with_limits()
        {
            var response = client.Get(new QueryRockstars { Skip = 2 });
            Assert.That(response.Offset, Is.EqualTo(2));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars - 2));

            response = client.Get(new QueryRockstars { Take = 2 });
            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = client.Get(new QueryRockstars { Skip = 2, Take = 2 });
            Assert.That(response.Offset, Is.EqualTo(2));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(2));
        }

        [Test]
        public void Can_execute_explicit_equality_condition()
        {
            var response = client.Get(new QueryRockstars { Age = 27 });

            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_execute_explicit_equality_condition_on_CustomRockstar()
        {
            var response = client.Get(new QueryCustomRockstars { Age = 27 });

            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_execute_implicit_equality_condition()
        {
            var response = Config.ListeningOn.CombineWith("json/reply/QueryRockstars")
                .AddQueryParam("FirstName", "Jim")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Rockstar>>();

            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].LastName, Is.EqualTo("Morrison"));
        }

        [Test]
        public void Can_execute_query_with_JOIN_on_RockstarAlbums()
        {
            var response = client.Get(new QueryRockstarAlbums());
            response.PrintDump();
            Assert.That(response.Total, Is.EqualTo(TotalAlbums));
            Assert.That(response.Results.Count, Is.EqualTo(TotalAlbums));
            var albumNames = response.Results.Select(x => x.AlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Never Mind", "Foo Fighters", "Into the Wild"
            }));
        }

        [Test]
        public void Can_execute_query_with_LEFTJOIN_on_RockstarAlbums()
        {
            var response = client.Get(new QueryRockstarAlbumsLeftJoin());
            response.PrintDump();
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars));
            var albumNames = response.Results.Where(x => x.AlbumName != null).Select(x => x.AlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Never Mind", "Foo Fighters", "Into the Wild"
            }));
        }
    }
}