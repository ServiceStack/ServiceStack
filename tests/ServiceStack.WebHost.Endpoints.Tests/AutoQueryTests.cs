using System.Linq;
using Funq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class AutoQueryAppHost : AppSelfHostBase
    {
        public AutoQueryAppHost()
            : base("AutoQuery", typeof(AutoQueryService).Assembly) { }

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

            var autoQuery = new AutoQueryFeature { MaxLimit = 100 }
                .RegisterQueryFilter<QueryRockstarsFilter, Rockstar>((req, q, dto) =>
                    q.And(x => x.LastName.EndsWith("son"))
                )
                .RegisterQueryFilter<QueryCustomRockstarsFilter, Rockstar>((req, q, dto) =>
                    q.And(x => x.LastName.EndsWith("son"))
                )
                .RegisterQueryFilter<IFilterRockstars, Rockstar>((req, q, dto) =>
                    q.And(x => x.LastName.EndsWith("son"))
                );

            Plugins.Add(autoQuery);
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
            new RockstarAlbum { RockstarId = 1, Name = "Electric Ladyland" },    
            new RockstarAlbum { RockstarId = 3, Name = "Nevermind" },    
            new RockstarAlbum { RockstarId = 5, Name = "Foo Fighters" },    
            new RockstarAlbum { RockstarId = 6, Name = "Into the Wild" },    
        };
    }

    public class QueryRockstars : QueryBase<Rockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryRockstarsConventions : QueryBase<Rockstar>
    {
        public int? AgeOlderThan { get; set; }
        public int? AgeGreaterThanOrEqualTo { get; set; }
        public int? AgeGreaterThan { get; set; }
        public int? GreaterThanAge { get; set; }
        public string FirstNameStartsWith { get; set; }
        public string LastNameEndsWith { get; set; }
        public string LastNameContains { get; set; }
        public string RockstarAlbumNameContains { get; set; }
        public int? RockstarIdAfter { get; set; }
        public int? RockstarIdOnOrAfter { get; set; }
    }

    public class QueryCustomRockstars : QueryBase<Rockstar, CustomRockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryRockstarAlbums : QueryBase<Rockstar, CustomRockstar>, IJoin<Rockstar, RockstarAlbum>
    {
        public int? Age { get; set; }
        public string RockstarAlbumName { get; set; }
    }

    public class QueryRockstarAlbumsImplicit : QueryBase<Rockstar, CustomRockstar>, IJoin<Rockstar, RockstarAlbum>
    {
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

    public class QueryFieldRockstars : QueryBase<Rockstar>
    {
        public string FirstName { get; set; } //default to 'AND FirstName = {Value}'

        [QueryField(Format = "UPPER({Field}) LIKE UPPER({Value})", Field = "FirstName")]
        public string FirstNameCaseInsensitive { get; set; }

        [QueryField(Format = "{Field} LIKE {Value}", Field = "FirstName", ValueFormat = "{0}%")]
        public string FirstNameStartsWith { get; set; }

        [QueryField(Format = "{Field} LIKE {Value}", Field = "LastName", ValueFormat = "%{0}")]
        public string LastNameEndsWith { get; set; }

        [QueryField(Type = QueryType.Or, Format = "UPPER({Field}) LIKE UPPER({Value})", Field = "LastName")]
        public string OrLastName { get; set; }

        [QueryField(Operand = ">=")]
        public int? Age { get; set; }
    }

    public class QueryFieldRockstarsDynamic : QueryBase<Rockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryRockstarsFilter : QueryBase<Rockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryCustomRockstarsFilter : QueryBase<Rockstar, CustomRockstar>
    {
        public int? Age { get; set; }
    }

    public interface IFilterRockstars { }
    public class QueryRockstarsIFilter : QueryBase<Rockstar>, IFilterRockstars
    {
        public int? Age { get; set; }
    }

    [Query(QueryType.Or)]
    [Route("/OrRockstars")]
    public class QueryOrRockstars : QueryBase<Rockstar>
    {
        public int? Age { get; set; }
        public string FirstName { get; set; }
    }

    public class RockstarAlbum
    {
        [AutoIncrement]
        public int Id { get; set; }
        public int RockstarId { get; set; }
        public string Name { get; set; }
    }

    public class CustomRockstar
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public string RockstarAlbumName { get; set; }
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
            Assert.That(response.Total, Is.EqualTo(TotalAlbums));
            Assert.That(response.Results.Count, Is.EqualTo(TotalAlbums));
            var albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Nevermind", "Foo Fighters", "Into the Wild"
            }));

            response = client.Get(new QueryRockstarAlbums { Age = 27 });
            Assert.That(response.Total, Is.EqualTo(2));
            Assert.That(response.Results.Count, Is.EqualTo(2));
            albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Nevermind"
            }));

            response = client.Get(new QueryRockstarAlbums { RockstarAlbumName = "Nevermind" });
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] { "Nevermind" }));
        }

        [Test]
        public void Can_execute_IMPLICIT_query_with_JOIN_on_RockstarAlbums()
        {
            var response = Config.ListeningOn.CombineWith("json/reply/QueryRockstarAlbumsImplicit")
                .AddQueryParam("Age", "27")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<CustomRockstar>>();
            Assert.That(response.Total, Is.EqualTo(2));
            Assert.That(response.Results.Count, Is.EqualTo(2));
            var albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Nevermind"
            }));

            response = Config.ListeningOn.CombineWith("json/reply/QueryRockstarAlbumsImplicit")
                .AddQueryParam("RockstarAlbumName", "Nevermind")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<CustomRockstar>>();
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] { "Nevermind" }));
        }

        [Test]
        public void Can_execute_query_with_LEFTJOIN_on_RockstarAlbums()
        {
            var response = client.Get(new QueryRockstarAlbumsLeftJoin());
            response.PrintDump();
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars));
            var albumNames = response.Results.Where(x => x.RockstarAlbumName != null).Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Nevermind", "Foo Fighters", "Into the Wild"
            }));
        }

        [Test]
        public void Can_execute_custom_QueryFields()
        {
            QueryResponse<Rockstar> response;
            response = client.Get(new QueryFieldRockstars { FirstName = "Jim" });
            Assert.That(response.Results.Count, Is.EqualTo(1));

            response = client.Get(new QueryFieldRockstars { FirstName = "jim" });
            Assert.That(response.Results.Count, Is.EqualTo(0));

            response = client.Get(new QueryFieldRockstars { FirstNameCaseInsensitive = "jim" });
            Assert.That(response.Results.Count, Is.EqualTo(1));

            response = client.Get(new QueryFieldRockstars { FirstNameStartsWith = "Jim" });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = client.Get(new QueryFieldRockstars { LastNameEndsWith = "son" });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = client.Get(new QueryFieldRockstars
            {
                LastNameEndsWith = "son",
                OrLastName = "Hendrix"
            });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = client.Get(new QueryFieldRockstars { Age = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(4));
        }

        [Test]
        public void Can_execute_combination_of_QueryFields()
        {
            QueryResponse<Rockstar> response;

            response = client.Get(new QueryFieldRockstars
            {
                FirstNameStartsWith = "Jim",
                LastNameEndsWith = "son",
            });
            Assert.That(response.Results.Count, Is.EqualTo(1));

            response = client.Get(new QueryFieldRockstars
            {
                FirstNameStartsWith = "Jim",
                OrLastName = "Cobain",
            });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Does_escape_values()
        {
            QueryResponse<Rockstar> response;

            response = client.Get(new QueryFieldRockstars
            {
                FirstNameStartsWith = "Jim'\"",
            });
            Assert.That(response.Results.Count, Is.EqualTo(0));
        }

        [Test]
        public void Does_allow_adding_attributes_dynamically()
        {
            typeof(QueryFieldRockstarsDynamic)
                .GetProperty("Age")
                .AddAttributes(new QueryFieldAttribute { Operand = ">=" });

            var response = client.Get(new QueryFieldRockstars { Age = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(4));
        }

        [Test]
        public void Does_execute_typed_QueryFilters()
        {
            // QueryFilter appends additional: x => x.LastName.EndsWith("son")
            var response = client.Get(new QueryRockstarsFilter { Age = 27 });
            Assert.That(response.Results.Count, Is.EqualTo(1));

            var custom = client.Get(new QueryCustomRockstarsFilter { Age = 27 });
            Assert.That(custom.Results.Count, Is.EqualTo(1));

            response = client.Get(new QueryRockstarsIFilter { Age = 27 });
            Assert.That(response.Results.Count, Is.EqualTo(1));
        }

        [Test]
        public void Can_execute_OR_QueryFilters()
        {
            var response = client.Get(new QueryOrRockstars { Age = 42, FirstName = "Jim" });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = Config.ListeningOn.CombineWith("OrRockstars")
                .AddQueryParam("Age", "27")
                .AddQueryParam("FirstName", "Kurt")
                .AddQueryParam("LastName", "Hendrix")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Rockstar>>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_execute_implicit_conventions()
        {
            var baseUrl = Config.ListeningOn.CombineWith("json/reply/QueryRockstars");

            var response = baseUrl.AddQueryParam("AgeOlderThan", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = baseUrl.AddQueryParam("AgeGreaterThanOrEqualTo", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(4));

            response = baseUrl.AddQueryParam("AgeGreaterThan", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = baseUrl.AddQueryParam("GreaterThanAge", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = baseUrl.AddQueryParam(">Age", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(4));
            response = baseUrl.AddQueryParam("Age>", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = baseUrl.AddQueryParam("<Age", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = baseUrl.AddQueryParam("Age<", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(4));

            response = baseUrl.AddQueryParam("FirstNameStartsWith", "jim").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(2));
            response = baseUrl.AddQueryParam("LastNameEndsWith", "son").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(2));
            response = baseUrl.AddQueryParam("LastNameContains", "e").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_execute_implicit_conventions_on_JOIN()
        {
            var baseUrl = Config.ListeningOn.CombineWith("json/reply/QueryRockstarAlbums");

            var response = baseUrl.AddQueryParam("RockstarAlbumNameContains", "n").AsJsonInto<CustomRockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = baseUrl.AddQueryParam(">RockstarId", "3").AsJsonInto<CustomRockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = baseUrl.AddQueryParam("RockstarId>", "3").AsJsonInto<CustomRockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(2));
        }

        [Test]
        public void Can_execute_Explicit_conventions()
        {
            var response = client.Get(new QueryRockstarsConventions { AgeOlderThan = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = client.Get(new QueryRockstarsConventions { AgeGreaterThanOrEqualTo = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(4));

            response = client.Get(new QueryRockstarsConventions { AgeGreaterThan = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = client.Get(new QueryRockstarsConventions { GreaterThanAge = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = client.Get(new QueryRockstarsConventions { FirstNameStartsWith = "Jim" });
            Assert.That(response.Results.Count, Is.EqualTo(2));
            response = client.Get(new QueryRockstarsConventions { LastNameEndsWith = "son" });
            Assert.That(response.Results.Count, Is.EqualTo(2));
            response = client.Get(new QueryRockstarsConventions { LastNameContains = "e" });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

    }

    public static class AutoQueryExtensions
    {
        public static QueryResponse<T> AsJsonInto<T>(this string url)
        {
            return url.GetJsonFromUrl()
                .FromJson<QueryResponse<T>>();
        }
    }
}

