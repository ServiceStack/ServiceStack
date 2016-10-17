using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Funq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class AutoQueryDataMemoryTests : AutoQueryDataTests
    {
        public override ServiceStackHost CreateAppHost()
        {
            return new AutoQueryDataAppHost();
        }
    }

    public class AutoQueryDataAppHost : AppSelfHostBase
    {
        public AutoQueryDataAppHost()
            : base("AutoQuerData", typeof(AutoQueryService).GetAssembly())
        { }

        public override void Configure(Container container)
        {
            Plugins.Add(new AutoQueryDataFeature
                {
                    MaxLimit = 100,
                    ResponseFilters = {
                        ctx => {
                            var executedCmds = new List<Command>();
                            var supportedFns = new Dictionary<string, Func<int, int, int>>(StringComparer.OrdinalIgnoreCase)
                            {
                                {"ADD",      (a,b) => a + b },
                                {"MULTIPLY", (a,b) => a * b },
                                {"DIVIDE",   (a,b) => a / b },
                                {"SUBTRACT", (a,b) => a - b },
                            };
                            foreach (var cmd in ctx.Commands)
                            {
                                Func<int, int, int> fn;
                                if (!supportedFns.TryGetValue(cmd.Name, out fn)) continue;
                                var label = !string.IsNullOrWhiteSpace(cmd.Suffix) ? cmd.Suffix.Trim() : cmd.ToString();
                                ctx.Response.Meta[label] = fn(int.Parse(cmd.Args[0]), int.Parse(cmd.Args[1])).ToString();
                                executedCmds.Add(cmd);
                            }
                            ctx.Commands.RemoveAll(executedCmds.Contains);
                        }
                    }
                }
                .AddDataSource(ctx => ctx.MemorySource(SeedRockstars))
                .AddDataSource(ctx => ctx.MemorySource(SeedAlbums))
                .AddDataSource(ctx => ctx.MemorySource(SeedGenres))
                .AddDataSource(ctx => ctx.MemorySource(SeedAdhoc))
                .AddDataSource(ctx => ctx.MemorySource(SeedMovies))
                .AddDataSource(ctx => ctx.MemorySource(SeedAllFields))
                .AddDataSource(ctx => ctx.MemorySource(SeedPagingTest))
                .RegisterQueryFilter<QueryDataRockstarsFilter>((q, dto, req) =>
                    q.And<Rockstar>(x => x.LastName, new EndsWithCondition(), "son")
                )
                .RegisterQueryFilter<QueryDataCustomRockstarsFilter>((q, dto, req) =>
                    q.And<Rockstar>(x => x.LastName, new EndsWithCondition(), "son")
                )
                .RegisterQueryFilter<IFilterRockstars>((q, dto, req) =>
                    q.And<Rockstar>(x => x.LastName, new EndsWithCondition(), "son")
                )
            );
        }

        public static Rockstar[] SeedRockstars = new[] {
            new Rockstar { Id = 1, FirstName = "Jimi", LastName = "Hendrix", LivingStatus = LivingStatus.Dead, Age = 27, DateOfBirth = new DateTime(1942, 11, 27), DateDied = new DateTime(1970, 09, 18), },
            new Rockstar { Id = 2, FirstName = "Jim", LastName = "Morrison", Age = 27, LivingStatus = LivingStatus.Dead, DateOfBirth = new DateTime(1943, 12, 08), DateDied = new DateTime(1971, 07, 03),  },
            new Rockstar { Id = 3, FirstName = "Kurt", LastName = "Cobain", Age = 27, LivingStatus = LivingStatus.Dead, DateOfBirth = new DateTime(1967, 02, 20), DateDied = new DateTime(1994, 04, 05), },
            new Rockstar { Id = 4, FirstName = "Elvis", LastName = "Presley", Age = 42, LivingStatus = LivingStatus.Dead, DateOfBirth = new DateTime(1935, 01, 08), DateDied = new DateTime(1977, 08, 16), },
            new Rockstar { Id = 5, FirstName = "David", LastName = "Grohl", Age = 44, LivingStatus = LivingStatus.Alive, DateOfBirth = new DateTime(1969, 01, 14), },
            new Rockstar { Id = 6, FirstName = "Eddie", LastName = "Vedder", Age = 48, LivingStatus = LivingStatus.Alive, DateOfBirth = new DateTime(1964, 12, 23), },
            new Rockstar { Id = 7, FirstName = "Michael", LastName = "Jackson", Age = 50, LivingStatus = LivingStatus.Dead, DateOfBirth = new DateTime(1958, 08, 29), DateDied = new DateTime(2009, 06, 05), },
        };

        public static RockstarAlbum[] SeedAlbums = new[] {
            new RockstarAlbum { Id = 1, RockstarId = 1, Name = "Electric Ladyland", Genre = "Funk" },
            new RockstarAlbum { Id = 2, RockstarId = 3, Name = "Bleach", Genre = "Grunge" },
            new RockstarAlbum { Id = 3, RockstarId = 3, Name = "Nevermind", Genre = "Grunge" },
            new RockstarAlbum { Id = 4, RockstarId = 3, Name = "In Utero", Genre = "Grunge" },
            new RockstarAlbum { Id = 5, RockstarId = 3, Name = "Incesticide", Genre = "Grunge" },
            new RockstarAlbum { Id = 6, RockstarId = 3, Name = "MTV Unplugged in New York", Genre = "Acoustic" },
            new RockstarAlbum { Id = 7, RockstarId = 5, Name = "Foo Fighters", Genre = "Grunge" },
            new RockstarAlbum { Id = 8, RockstarId = 6, Name = "Into the Wild", Genre = "Folk" },
        };

        public static RockstarGenre[] SeedGenres = new[] {
            new RockstarGenre { RockstarId = 1, Name = "Rock" },
            new RockstarGenre { RockstarId = 3, Name = "Grunge" },
            new RockstarGenre { RockstarId = 5, Name = "Alternative Rock" },
            new RockstarGenre { RockstarId = 6, Name = "Folk Rock" },
        };

        public static Adhoc[] SeedAdhoc = SeedRockstars.Map(x => new Adhoc
        {
            Id = x.Id,
            FirstName = x.FirstName,
            LastName = x.LastName,
        }).ToArray();

        public static Movie[] SeedMovies = new[] {
            new Movie { Id = 1, ImdbId = "tt0111161", Title = "The Shawshank Redemption", Score = 9.2m, Director = "Frank Darabont", ReleaseDate = new DateTime(1995,2,17), TagLine = "Fear can hold you prisoner. Hope can set you free.", Genres = new List<string>{"Crime","Drama"}, Rating = "R", },
            new Movie { Id = 2, ImdbId = "tt0068646", Title = "The Godfather", Score = 9.2m, Director = "Francis Ford Coppola", ReleaseDate = new DateTime(1972,3,24), TagLine = "An offer you can't refuse.", Genres = new List<string> {"Crime","Drama", "Thriller"}, Rating = "R", },
            new Movie { Id = 3, ImdbId = "tt1375666", Title = "Inception", Score = 9.2m, Director = "Christopher Nolan", ReleaseDate = new DateTime(2010,7,16), TagLine = "Your mind is the scene of the crime", Genres = new List<string>{"Action", "Mystery", "Sci-Fi", "Thriller"}, Rating = "PG-13", },
            new Movie { Id = 4, ImdbId = "tt0071562", Title = "The Godfather: Part II", Score = 9.0m, Director = "Francis Ford Coppola", ReleaseDate = new DateTime(1974,12,20), Genres = new List<string> {"Crime","Drama", "Thriller"}, Rating = "R", },
            new Movie { Id = 5, ImdbId = "tt0060196", Title = "The Good, the Bad and the Ugly", Score = 9.0m, Director = "Sergio Leone", ReleaseDate = new DateTime(1967,12,29), TagLine = "They formed an alliance of hate to steal a fortune in dead man's gold", Genres = new List<string>{"Adventure","Western"}, Rating = "R", },
            new Movie { Id = 6, ImdbId = "tt0114709", Title = "Toy Story", Score = 8.3m, Director = "John Lasseter", ReleaseDate = new DateTime(1995,11,22), TagLine = "A cowboy doll is profoundly threatened and jealous when a new spaceman figure supplants him as top toy in a boy's room.", Genres = new List<string>{"Animation","Adventure","Comedy"}, Rating = "G", },
            new Movie { Id = 7, ImdbId = "tt2294629", Title = "Frozen", Score = 7.8m, Director = "Chris Buck", ReleaseDate = new DateTime(2013,11,27), TagLine = "Fearless optimist Anna teams up with Kristoff in an epic journey, encountering Everest-like conditions, and a hilarious snowman named Olaf", Genres = new List<string>{"Animation","Adventure","Comedy"}, Rating = "PG", },
            new Movie { Id = 8, ImdbId = "tt1453405", Title = "Monsters University", Score = 7.4m, Director = "Dan Scanlon", ReleaseDate = new DateTime(2013,06,21), TagLine = "A look at the relationship between Mike and Sulley during their days at Monsters University -- when they weren't necessarily the best of friends.", Genres = new List<string>{"Animation","Adventure","Comedy"}, Rating = "G", },
            new Movie { Id = 9, ImdbId = "tt0468569", Title = "The Dark Knight", Score = 9.0m, Director = "Christopher Nolan", ReleaseDate = new DateTime(2008,07,18), TagLine = "When Batman, Gordon and Harvey Dent launch an assault on the mob, they let the clown out of the box, the Joker, bent on turning Gotham on itself and bringing any heroes down to his level.", Genres = new List<string>{"Action","Crime","Drama"}, Rating = "PG-13", },
            new Movie { Id = 10, ImdbId = "tt0109830", Title = "Forrest Gump", Score = 8.8m, Director = "Robert Zemeckis", ReleaseDate = new DateTime(1996,07,06), TagLine = "Forrest Gump, while not intelligent, has accidentally been present at many historic moments, but his true love, Jenny Curran, eludes him.", Genres = new List<string>{"Drama","Romance"}, Rating = "PG-13", },
        };

        public static AllFields[] SeedAllFields = new[] {
            new AllFields
            {
                Id = 1,
                NullableId = 2,
                Byte = 3,
                DateTime = new DateTime(2001, 01, 01),
                NullableDateTime = new DateTime(2002, 02, 02),
                Decimal = 4,
                Double = 5.5,
                Float = 6.6f,
                Guid = new Guid("3EE6865A-4149-4940-B7A2-F952E0FEFC5E"),
                NullableGuid = new Guid("7A2FDDD8-4BB0-4735-8230-A6AC79088489"),
                Long = 7,
                Short = 8,
                String = "string",
                TimeSpan = TimeSpan.FromHours(1),
                NullableTimeSpan = TimeSpan.FromDays(1),
                UInt = 9,
                ULong = 10,
                UShort = 11,
            }
        };

        public static PagingTest[] SeedPagingTest = 250.Times(i => new PagingTest { Id = i, Name = "Name" + i, Value = i % 2 }).ToArray();
    }

    [Route("/querydata/rockstars")]
    public class QueryDataRockstars : QueryData<Rockstar>
    {
        public int? Age { get; set; }
    }

    [Route("/querydata/rockstaralbums")]
    public class QueryDataRockstarAlbums : QueryData<RockstarAlbum>
    {
        public int? Id { get; set; }
        public int? RockstarId { get; set; }
        public string Name { get; set; }
        public string Genre { get; set; }
        public int[] IdBetween { get; set; }
    }

    [Route("/querydata/pagingtest")]
    public class QueryDataPagingTest : QueryData<PagingTest>
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public int? Value { get; set; }
    }

    public class QueryDataRockstarsConventions : QueryData<Rockstar>
    {
        public DateTime? DateOfBirthGreaterThan { get; set; }
        public DateTime? DateDiedLessThan { get; set; }
        public int[] Ids { get; set; }
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

    public class QueryDataCustomRockstars : QueryData<Rockstar, CustomRockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryDataOverridedRockstars : QueryData<Rockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryDataOverridedCustomRockstars : QueryData<Rockstar, CustomRockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryDataFieldRockstars : QueryData<Rockstar>
    {
        public string FirstName { get; set; } //default to 'AND FirstName = {Value}'

        public string[] FirstNames { get; set; } //Collections default to 'FirstName IN ({Values})

        [QueryDataField(Condition = ">=")]
        public int? Age { get; set; }

        [QueryDataField(Condition = "Like", Field = "FirstName")]
        public string FirstNameCaseInsensitive { get; set; }

        [QueryDataField(Condition = "StartsWith", Field = "FirstName")]
        public string FirstNameStartsWith { get; set; }

        [QueryDataField(Condition = "EndsWith", Field = "LastName")]
        public string LastNameEndsWith { get; set; }

        [QueryDataField(Condition = "Between", Field = "FirstName")]
        public string[] FirstNameBetween { get; set; }

        [QueryDataField(Term = QueryTerm.Or, Condition = "=", Field = "LastName")]
        public string OrLastName { get; set; }
    }

    public class QueryDataFieldRockstarsDynamic : QueryData<Rockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryDataRockstarsFilter : QueryData<Rockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryDataCustomRockstarsFilter : QueryData<Rockstar, CustomRockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryDataRockstarsIFilter : QueryData<Rockstar>, IFilterRockstars
    {
        public int? Age { get; set; }
    }

    [QueryData(QueryTerm.Or)]
    [Route("/OrDataRockstars")]
    public class QueryDataOrRockstars : QueryData<Rockstar>
    {
        public int? Age { get; set; }
        public string FirstName { get; set; }
    }

    [Route("/OrDataRockstarsFields")]
    public class QueryDataOrRockstarsFields : QueryData<Rockstar>
    {
        [QueryDataField(Term = QueryTerm.Or)]
        public string FirstName { get; set; }

        [QueryDataField(Term = QueryTerm.Or)]
        public string LastName { get; set; }
    }

    [QueryData(QueryTerm.Or)]
    public class QueryDataGetRockstars : QueryData<Rockstar>
    {
        public int[] Ids { get; set; }
        public List<int> Ages { get; set; }
        public List<string> FirstNames { get; set; }
        public int[] IdsBetween { get; set; }
    }

    [QueryData(QueryTerm.Or)]
    public class QueryDataGetRockstarsDynamic : QueryData<Rockstar> { }

    [DataContract]
    [Route("/adhocdata-rockstars")]
    public class QueryDataAdhocRockstars : QueryData<Rockstar>
    {
        [DataMember(Name = "first_name")]
        public string FirstName { get; set; }
    }

    [DataContract]
    [Route("/adhocdata")]
    public class QueryDataAdhoc : QueryData<Adhoc> { }

    [Route("/moviesdata/search")]
    [QueryData(QueryTerm.And)] //Default
    public class SearchDataMovies : QueryData<Movie> { }

    [Route("/moviesdata")]
    [QueryData(QueryTerm.Or)]
    public class QueryDataMovies : QueryData<Movie>
    {
        public int[] Ids { get; set; }
        public string[] ImdbIds { get; set; }
        public string[] Ratings { get; set; }
    }

    public class StreamDataMovies : QueryData<Movie>
    {
        public string[] Ratings { get; set; }
    }

    public class QueryDataUnknownRockstars : QueryData<Rockstar>
    {
        public int UnknownInt { get; set; }
        public string UnknownProperty { get; set; }

    }

    public class QueryDataAllFields : QueryData<AllFields>
    {
        public virtual Guid Guid { get; set; }
    }

    public class AutoQueryDataService : Service
    {
        public IAutoQueryData AutoQuery { get; set; }

        //Override with custom impl
        public object Any(QueryDataOverridedRockstars dto)
        {
            var q = AutoQuery.CreateQuery(dto, Request.GetRequestParams(), Request);
            q.Take(1);
            return AutoQuery.Execute(dto, q);
        }

        public object Any(QueryDataOverridedCustomRockstars dto)
        {
            var q = AutoQuery.CreateQuery(dto, Request.GetRequestParams(), Request);
            q.Take(1);
            return AutoQuery.Execute(dto, q);
        }
    }

    [TestFixture]
    public abstract class AutoQueryDataTests
    {
        public readonly ServiceStackHost appHost;
        protected readonly IServiceClient client;

        private static readonly int TotalRockstars = AutoQueryAppHost.SeedRockstars.Length;
        private static readonly int TotalAlbums = AutoQueryAppHost.SeedAlbums.Length;

        public abstract ServiceStackHost CreateAppHost();

        public AutoQueryDataTests()
        {
            appHost = CreateAppHost()
                .Init()
                .Start(Config.ListeningOn);

            client = new JsonServiceClient(Config.ListeningOn);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        public List<Rockstar> Rockstars
        {
            get { return AutoQueryDataAppHost.SeedRockstars.ToList(); }
        }

        public List<PagingTest> PagingTests
        {
            get { return AutoQueryDataAppHost.SeedPagingTest.ToList(); }
        }

        public bool IsDynamoDb
        {
            get 
            {
                return appHost is AutoQueryDataDynamoAppHost;
            }
        }

        [Test]
        public void Can_execute_basic_query()
        {
            var response = client.Get(new QueryDataRockstars());

            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars));
        }

        [Test]
        public void Can_execute_overridden_basic_query()
        {
            var response = client.Get(new QueryDataOverridedRockstars());

            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(1));
        }

        [Test]
        public void Can_execute_AdhocRockstars_query()
        {
            var request = new QueryDataAdhocRockstars { FirstName = "Jimi" };

            Assert.That(request.ToGetUrl(), Is.EqualTo("/adhocdata-rockstars?first_name=Jimi"));

            var response = client.Get(request);

            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo(request.FirstName));
        }

        [Test]
        public void Can_execute_Adhoc_query_alias()
        {
            var response = Config.ListeningOn.CombineWith("adhocdata")
                .AddQueryParam("first_name", "Jimi")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Adhoc>>();

            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo("Jimi"));
        }

        [Test]
        public void Can_execute_Adhoc_query_convention()
        {
            var response = Config.ListeningOn.CombineWith("adhocdata")
                .AddQueryParam("last_name", "Hendrix")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Adhoc>>();
            Assert.That(response.Results.Count, Is.EqualTo(7));

            JsConfig.EmitLowercaseUnderscoreNames = true;
            response = Config.ListeningOn.CombineWith("adhocdata")
                .AddQueryParam("last_name", "Hendrix")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Adhoc>>();
            JsConfig.Reset();

            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo("Jimi"));
        }

        [Test]
        public void Can_execute_explicit_equality_condition_on_overridden_CustomRockstar()
        {
            var response = client.Get(new QueryDataOverridedCustomRockstars { Age = 27 });

            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(response.Results.Count, Is.EqualTo(1));
        }

        [Test]
        public void Can_execute_basic_query_with_limits()
        {
            var response = client.Get(new QueryDataRockstars { Skip = 2 });
            Assert.That(response.Offset, Is.EqualTo(2));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars - 2));

            response = client.Get(new QueryDataRockstars { Take = 2 });
            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = client.Get(new QueryDataRockstars { Skip = 2, Take = 2 });
            Assert.That(response.Offset, Is.EqualTo(2));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(2));
        }

        [Test]
        public void Can_execute_explicit_equality_condition()
        {
            var response = client.Get(new QueryDataRockstars { Age = 27 });

            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_execute_explicit_equality_condition_on_CustomRockstar()
        {
            var response = client.Get(new QueryDataCustomRockstars { Age = 27 });

            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_execute_implicit_equality_condition()
        {
            var response = Config.ListeningOn.CombineWith("json/reply/QueryDataRockstars")
                .AddQueryParam("FirstName", "Jim")
                .AddQueryParam("LivingStatus", "Dead")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Rockstar>>();

            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].LastName, Is.EqualTo("Morrison"));
        }

        [Test]
        public void Can_execute_multiple_conditions_with_same_param_name()
        {
            var response = Config.ListeningOn.CombineWith("json/reply/QueryDataRockstars")
                .AddQueryParam("FirstName", "Jim")
                .AddQueryParam("FirstName", "Jim")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Rockstar>>();

            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].LastName, Is.EqualTo("Morrison"));

            response = Config.ListeningOn.CombineWith("json/reply/QueryDataRockstars")
                .AddQueryParam("FirstNameStartsWith", "Jim")
                .AddQueryParam("FirstNameStartsWith", "Jimi")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Rockstar>>();

            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].LastName, Is.EqualTo("Hendrix"));
        }

        [Test]
        public void Can_execute_implicit_IsNull_condition()
        {
            var response = Config.ListeningOn.CombineWith("json/reply/QueryDataRockstars?DateDied=")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Rockstar>>();

            Assert.That(response.Total, Is.EqualTo(2));
            Assert.That(response.Results.Count, Is.EqualTo(2));
        }

        [Test]
        public void Can_execute_custom_QueryFields()
        {
            QueryResponse<Rockstar> response;
            response = client.Get(new QueryDataFieldRockstars { FirstName = "Jim" });
            Assert.That(response.Results.Count, Is.EqualTo(1));

            response = client.Get(new QueryDataFieldRockstars { FirstNames = new[] { "Jim", "Kurt" } });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = client.Get(new QueryDataFieldRockstars { FirstNameCaseInsensitive = "jim" });
            Assert.That(response.Results.Count, Is.EqualTo(1));

            response = client.Get(new QueryDataFieldRockstars { FirstNameStartsWith = "Jim" });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = client.Get(new QueryDataFieldRockstars { LastNameEndsWith = "son" });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = client.Get(new QueryDataFieldRockstars { FirstNameBetween = new[] { "A", "F" } });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            try
            {
                response = client.Get(new QueryDataFieldRockstars
                {
                    LastNameEndsWith = "son",
                    OrLastName = "Hendrix"
                });
                Assert.That(response.Results.Count, Is.EqualTo(3));
            }
            catch (Exception ex)
            {
                if (!IsDynamoDb) //DynamoDb doesn't support EndsWith
                    throw;
            }

            response = client.Get(new QueryDataFieldRockstars
            {
                FirstNameStartsWith = "Jim",
                OrLastName = "Presley"
            });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = client.Get(new QueryDataFieldRockstars { Age = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(4));
        }

        [Test]
        public void Can_execute_combination_of_QueryFields()
        {
            QueryResponse<Rockstar> response;

            response = client.Get(new QueryDataFieldRockstars
            {
                FirstNameStartsWith = "Jim",
                LastNameEndsWith = "son",
            });
            Assert.That(response.Results.Count, Is.EqualTo(1));

            response = client.Get(new QueryDataFieldRockstars
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

            response = client.Get(new QueryDataFieldRockstars
            {
                FirstNameStartsWith = "Jim'\"",
            });
            Assert.That(response.Results.Count, Is.EqualTo(0));
        }

        [Test]
        public void Does_allow_adding_attributes_dynamically()
        {
            typeof(QueryDataFieldRockstarsDynamic)
                .GetProperty("Age")
                .AddAttributes(new QueryDataFieldAttribute { Condition = ">=" });

            var response = client.Get(new QueryDataFieldRockstarsDynamic { Age = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(4));
        }

        [Test]
        public void Does_execute_typed_QueryFilters()
        {
            // QueryFilter appends additional: x => x.LastName.EndsWith("son")
            var response = client.Get(new QueryDataRockstarsFilter { Age = 27 });
            Assert.That(response.Results.Count, Is.EqualTo(1));

            var custom = client.Get(new QueryDataCustomRockstarsFilter { Age = 27 });
            Assert.That(custom.Results.Count, Is.EqualTo(1));

            response = client.Get(new QueryDataRockstarsIFilter { Age = 27 });
            Assert.That(response.Results.Count, Is.EqualTo(1));
        }

        [Test]
        public void Can_execute_OR_QueryFilters()
        {
            var response = client.Get(new QueryDataOrRockstars { Age = 42, FirstName = "Jim" });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = Config.ListeningOn.CombineWith("OrDataRockstars")
                .AddQueryParam("Age", "27")
                .AddQueryParam("FirstName", "Kurt")
                .AddQueryParam("LastName", "Hendrix")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Rockstar>>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_execute_OR_QueryFilters_Fields()
        {
            var response = client.Get(new QueryDataOrRockstarsFields
            {
                FirstName = "Jim",
                LastName = "Vedder",
            });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = Config.ListeningOn.CombineWith("OrDataRockstarsFields")
                .AddQueryParam("FirstName", "Kurt")
                .AddQueryParam("LastName", "Hendrix")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Rockstar>>();
            Assert.That(response.Results.Count, Is.EqualTo(2));
        }

        [Test]
        public void Can_execute_implicit_conventions()
        {
            var baseUrl = Config.ListeningOn.CombineWith("json/reply/QueryDataRockstars");

            var response = baseUrl.AddQueryParam("AgeOlderThan", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = baseUrl.AddQueryParam("AgeGreaterThanOrEqualTo", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(4));

            response = baseUrl.AddQueryParam("AgeGreaterThan", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = baseUrl.AddQueryParam("GreaterThanAge", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = baseUrl.AddQueryParam("AgeNotEqualTo", 27).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(4));

            response = baseUrl.AddQueryParam(">Age", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(4));
            response = baseUrl.AddQueryParam("Age>", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = baseUrl.AddQueryParam("<Age", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = baseUrl.AddQueryParam("Age<", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(4));
            response = baseUrl.AddQueryParam("Age!", "27").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(4));

            response = baseUrl.AddQueryParam("FirstNameStartsWith", "Jim").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(2));
            response = baseUrl.AddQueryParam("LastNameEndsWith", "son").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(2));
            response = baseUrl.AddQueryParam("LastNameContains", "e").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_execute_Explicit_conventions()
        {
            QueryResponse<Rockstar> response;
            response = client.Get(new QueryDataRockstarsConventions { Ids = new[] { 1, 2, 3 } });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = client.Get(new QueryDataRockstarsConventions { AgeOlderThan = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = client.Get(new QueryDataRockstarsConventions { AgeGreaterThanOrEqualTo = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(4));

            response = client.Get(new QueryDataRockstarsConventions { AgeGreaterThan = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = client.Get(new QueryDataRockstarsConventions { GreaterThanAge = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = client.Get(new QueryDataRockstarsConventions { FirstNameStartsWith = "Jim" });
            Assert.That(response.Results.Count, Is.EqualTo(2));
            response = client.Get(new QueryDataRockstarsConventions { LastNameEndsWith = "son" });
            Assert.That(response.Results.Count, Is.EqualTo(2));
            response = client.Get(new QueryDataRockstarsConventions { LastNameContains = "e" });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = client.Get(new QueryDataRockstarsConventions { DateOfBirthGreaterThan = new DateTime(1960, 01, 01) });
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = client.Get(new QueryDataRockstarsConventions { DateDiedLessThan = new DateTime(1980, 01, 01) });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_execute_In_OR_Queries()
        {
            QueryResponse<Rockstar> response;
            response = client.Get(new QueryDataGetRockstars());
            Assert.That(response.Results.Count, Is.EqualTo(0));

            response = client.Get(new QueryDataGetRockstars { Ids = new[] { 1, 2, 3 } });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = client.Get(new QueryDataGetRockstars { Ages = new[] { 42, 44 }.ToList() });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = client.Get(new QueryDataGetRockstars { FirstNames = new[] { "Jim", "Kurt" }.ToList() });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = client.Get(new QueryDataGetRockstars { IdsBetween = new[] { 1, 3 } });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_execute_In_OR_Queries_with_implicit_conventions()
        {
            var baseUrl = Config.ListeningOn.CombineWith("json/reply/QueryDataGetRockstarsDynamic");

            QueryResponse<Rockstar> response;
            response = baseUrl.AddQueryParam("Ids", "1,2,3").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = baseUrl.AddQueryParam("Ages", "42, 44").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = baseUrl.AddQueryParam("FirstNames", "Jim,Kurt").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = baseUrl.AddQueryParam("IdsBetween", "1,3").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_query_Movie_Ratings()
        {
            var response = client.Get(new QueryDataMovies { Ratings = new[] { "G", "PG-13" } });
            Assert.That(response.Results.Count, Is.EqualTo(5));

            var url = Config.ListeningOn + "moviesdata?ratings=G,PG-13";
            response = url.AsJsonInto<Movie>();
            Assert.That(response.Results.Count, Is.EqualTo(5));

            response = client.Get(new QueryDataMovies
            {
                Ids = new[] { 1, 2 },
                ImdbIds = new[] { "tt0071562", "tt0060196" },
                Ratings = new[] { "G", "PG-13" }
            });
            Assert.That(response.Results.Count, Is.EqualTo(9));

            url = Config.ListeningOn + "moviesdata?ratings=G,PG-13&ids=1,2&imdbIds=tt0071562,tt0060196";
            response = url.AsJsonInto<Movie>();
            Assert.That(response.Results.Count, Is.EqualTo(9));
        }

        [Test]
        public void Can_StreamMovies()
        {
            var results = client.GetLazy(new StreamDataMovies()).ToList();
            Assert.That(results.Count, Is.EqualTo(10));

            results = client.GetLazy(new StreamDataMovies { Ratings = new[] { "G", "PG-13" } }).ToList();
            Assert.That(results.Count, Is.EqualTo(5));
        }

        [Test]
        public void Does_implicitly_OrderBy_PrimaryKey_when_limits_is_specified()
        {
            var movies = client.Get(new SearchDataMovies { Take = 100 });
            var ids = movies.Results.Map(x => x.Id);
            var orderedIds = ids.OrderBy(x => x);
            Assert.That(ids, Is.EqualTo(orderedIds));

            var rockstars = client.Get(new SearchDataMovies { Take = 100 });
            ids = rockstars.Results.Map(x => x.Id);
            orderedIds = ids.OrderBy(x => x);
            Assert.That(ids, Is.EqualTo(orderedIds));
        }

        [Test]
        public void Can_OrderBy_queries()
        {
            var movies = client.Get(new SearchDataMovies { Take = 100, OrderBy = "ImdbId" });
            var ids = movies.Results.Map(x => x.ImdbId);
            var orderedIds = ids.OrderBy(x => x).ToList();
            Assert.That(ids, Is.EqualTo(orderedIds));

            movies = client.Get(new SearchDataMovies { Take = 100, OrderBy = "Rating,ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderBy(x => x.Rating).ThenBy(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));

            movies = client.Get(new SearchDataMovies { Take = 100, OrderByDesc = "ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = ids.OrderByDescending(x => x).ToList();
            Assert.That(ids, Is.EqualTo(orderedIds));

            movies = client.Get(new SearchDataMovies { Take = 100, OrderByDesc = "Rating,ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderByDescending(x => x.Rating)
                .ThenByDescending(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));

            movies = client.Get(new SearchDataMovies { Take = 100, OrderBy = "Rating,-ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderBy(x => x.Rating)
                .ThenByDescending(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));

            movies = client.Get(new SearchDataMovies { Take = 100, OrderByDesc = "Rating,-ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderByDescending(x => x.Rating)
                .ThenBy(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));

            var url = Config.ListeningOn + "moviesdata/search?take=100&orderBy=Rating,ImdbId";
            movies = url.AsJsonInto<Movie>();
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderBy(x => x.Rating).ThenBy(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));

            url = Config.ListeningOn + "moviesdata/search?take=100&orderByDesc=Rating,ImdbId";
            movies = url.AsJsonInto<Movie>();
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderByDescending(x => x.Rating)
                .ThenByDescending(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));
        }

        [Test]
        public void Can_consume_as_CSV()
        {
            var url = Config.ListeningOn + "moviesdata/search.csv?ratings=G,PG-13";
            var csv = url.GetStringFromUrl();
            var headers = csv.SplitOnFirst('\n')[0].Trim();
            Assert.That(headers, Is.EqualTo("Id,ImdbId,Title,Rating,Score,Director,ReleaseDate,TagLine,Genres"));
            csv.Print();

            url = Config.ListeningOn + "querydata/rockstars.csv?Age=27";
            csv = url.GetStringFromUrl();
            headers = csv.SplitOnFirst('\n')[0].Trim();
            Assert.That(headers, Is.EqualTo("Id,FirstName,LastName,Age,DateOfBirth,DateDied,LivingStatus"));
            csv.Print();
        }

        [Test]
        public void Does_not_query_Ignored_properties()
        {
            var response = client.Get(new QueryDataUnknownRockstars
            {
                UnknownProperty = "Foo",
                UnknownInt = 1,
            });

            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars));
        }

        [Test]
        public void Can_Query_AllFields_Guid()
        {
            var guid = new Guid("3EE6865A-4149-4940-B7A2-F952E0FEFC5E");
            var response = client.Get(new QueryDataAllFields
            {
                Guid = guid
            });

            Assert.That(response.Results.Count, Is.EqualTo(1));

            Assert.That(response.Results[0].Guid, Is.EqualTo(guid));
        }

        [Test]
        public void Does_populate_Total()
        {
            var response = client.Get(new QueryDataRockstars());
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));
            Assert.That(response.Meta, Is.Null);

            response = client.Get(new QueryDataRockstars { Include = "COUNT" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));

            response = client.Get(new QueryDataRockstars { Include = "COUNT(*)" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));

            response = client.Get(new QueryDataRockstars { Include = "COUNT(DISTINCT LivingStatus)" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));

            response = client.Get(new QueryDataRockstars { Include = "Count(*), Min(Age), Max(Age), Sum(Id)" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));

            response = client.Get(new QueryDataRockstars { Age = 27, Include = "Count(*), Min(Age), Max(Age), Sum(Id)" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count(x => x.Age == 27)));
        }

        [Test]
        public void Can_Include_Aggregates_in_AutoQuery()
        {
            QueryResponse<Rockstar> response;
            response = client.Get(new QueryDataRockstars { Include = "COUNT" });
            Assert.That(response.Meta["COUNT(*)"], Is.EqualTo(Rockstars.Count.ToString()));

            response = client.Get(new QueryDataRockstars { Include = "COUNT(*)" });
            Assert.That(response.Meta["COUNT(*)"], Is.EqualTo(Rockstars.Count.ToString()));

            response = client.Get(new QueryDataRockstars { Include = "COUNT(DISTINCT LivingStatus)" });
            Assert.That(response.Meta["COUNT(DISTINCT LivingStatus)"], Is.EqualTo("2"));

            response = client.Get(new QueryDataRockstars { Include = "MIN(Age)" });
            Assert.That(response.Meta["MIN(Age)"], Is.EqualTo(Rockstars.Map(x => x.Age).Min().ToString()));

            response = client.Get(new QueryDataRockstars { Include = "Count(*), Min(Age), Max(Age), Sum(Id), Avg(Age), First(Id), Last(Id)", OrderBy = "Id" });
            Assert.That(response.Meta["Count(*)"], Is.EqualTo(Rockstars.Count.ToString()));
            Assert.That(response.Meta["Min(Age)"], Is.EqualTo(Rockstars.Map(x => x.Age).Min().ToString()));
            Assert.That(response.Meta["Max(Age)"], Is.EqualTo(Rockstars.Map(x => x.Age).Max().ToString()));
            Assert.That(response.Meta["Sum(Id)"], Is.EqualTo(Rockstars.Map(x => x.Id).Sum().ToString()));
            Assert.That(response.Meta["Avg(Age)"], Is.EqualTo(Rockstars.Average(x => x.Age).ToString()));
            Assert.That(response.Meta["First(Id)"], Is.EqualTo(Rockstars.First().Id.ToString()));
            Assert.That(response.Meta["Last(Id)"], Is.EqualTo(Rockstars.Last().Id.ToString()));

            response = client.Get(new QueryDataRockstars { Age = 27, Include = "Count(*), Min(Age), Max(Age), Sum(Id), Avg(Age), First(Id), Last(Id)", OrderBy = "Id" });
            var rockstars27 = Rockstars.Where(x => x.Age == 27).ToList();
            Assert.That(response.Meta["Count(*)"], Is.EqualTo(rockstars27.Count.ToString()));
            Assert.That(response.Meta["Min(Age)"], Is.EqualTo(rockstars27.Map(x => x.Age).Min().ToString()));
            Assert.That(response.Meta["Max(Age)"], Is.EqualTo(rockstars27.Map(x => x.Age).Max().ToString()));
            Assert.That(response.Meta["Sum(Id)"], Is.EqualTo(rockstars27.Map(x => x.Id).Sum().ToString()));
            Assert.That(response.Meta["Avg(Age)"], Is.EqualTo(rockstars27.Average(x => x.Age).ToString()));
            Assert.That(response.Meta["First(Id)"], Is.EqualTo(rockstars27.First().Id.ToString()));
            Assert.That(response.Meta["Last(Id)"], Is.EqualTo(rockstars27.Last().Id.ToString()));
        }

        [Test]
        public void Does_ignore_unknown_aggregate_commands()
        {
            var response = client.Get(new QueryDataRockstars { Include = "FOO(1)" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));
            Assert.That(response.Meta, Is.Null);

            response = client.Get(new QueryDataRockstars { Include = "FOO(1), Min(Age), Bar('a') alias, Count(*), Baz(1,'foo')" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));
            Assert.That(response.Meta["Min(Age)"], Is.EqualTo(Rockstars.Map(x => x.Age).Min().ToString()));
            Assert.That(response.Meta["Count(*)"], Is.EqualTo(Rockstars.Count.ToString()));
        }

        [Test]
        public void Can_Include_Aggregates_in_AutoQuery_with_Aliases()
        {
            var response = client.Get(new QueryDataRockstars { Include = "COUNT(*) Count" });
            Assert.That(response.Meta["Count"], Is.EqualTo(Rockstars.Count.ToString()));

            response = client.Get(new QueryDataRockstars { Include = "COUNT(DISTINCT LivingStatus) as UniqueStatus" });
            Assert.That(response.Meta["UniqueStatus"], Is.EqualTo("2"));

            response = client.Get(new QueryDataRockstars { Include = "MIN(Age) MinAge" });
            Assert.That(response.Meta["MinAge"], Is.EqualTo(Rockstars.Map(x => x.Age).Min().ToString()));

            response = client.Get(new QueryDataRockstars { Include = "Count(*) count, Min(Age) min, Max(Age) max, Sum(Id) sum" });
            Assert.That(response.Meta["count"], Is.EqualTo(Rockstars.Count.ToString()));
            Assert.That(response.Meta["min"], Is.EqualTo(Rockstars.Map(x => x.Age).Min().ToString()));
            Assert.That(response.Meta["max"], Is.EqualTo(Rockstars.Map(x => x.Age).Max().ToString()));
            Assert.That(response.Meta["sum"], Is.EqualTo(Rockstars.Map(x => x.Id).Sum().ToString()));
        }

        [Test]
        public void Can_execute_custom_aggregate_functions()
        {
            var response = client.Get(new QueryDataRockstars
            {
                Include = "ADD(6,2), Multiply(6,2) SixTimesTwo, Subtract(6,2), divide(6,2) TheDivide"
            });
            Assert.That(response.Meta["ADD(6,2)"], Is.EqualTo("8"));
            Assert.That(response.Meta["SixTimesTwo"], Is.EqualTo("12"));
            Assert.That(response.Meta["Subtract(6,2)"], Is.EqualTo("4"));
            Assert.That(response.Meta["TheDivide"], Is.EqualTo("3"));
        }

        [Test]
        public void Can_select_partial_list_of_fields()
        {
            var response = Config.ListeningOn.CombineWith("json/reply/QueryDataRockstars")
                .AddQueryParam("Age", "27")
                .AddQueryParam("Fields", "Id,FirstName,Age")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Rockstar>>();

            Assert.That(response.Results.All(x => x.Id > 0));
            Assert.That(response.Results.All(x => x.FirstName != null));
            Assert.That(response.Results.All(x => x.LastName == null));
            Assert.That(response.Results.Any(x => x.Age > 0));
            Assert.That(response.Results.All(x => x.DateDied == null));
            Assert.That(response.Results.All(x => x.DateOfBirth == default(DateTime).ToLocalTime()));
        }

        [Test]
        public void Can_select_partial_list_of_fields_case_insensitive()
        {
            var response = Config.ListeningOn.CombineWith("json/reply/QueryDataRockstars")
                .AddQueryParam("Age", "27")
                .AddQueryParam("Fields", "id,firstname,age")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Rockstar>>();

            response.PrintDump();

            Assert.That(response.Results.All(x => x.Id > 0));
            Assert.That(response.Results.All(x => x.FirstName != null));
            Assert.That(response.Results.All(x => x.LastName == null));
            Assert.That(response.Results.Any(x => x.Age > 0));
            Assert.That(response.Results.All(x => x.DateDied == null));
            Assert.That(response.Results.All(x => x.DateOfBirth == default(DateTime).ToLocalTime()));
        }

        [Test]
        public void Does_return_MaxLimit_results()
        {
            QueryResponse<PagingTest> response;
            response = client.Get(new QueryDataPagingTest());
            Assert.That(response.Results.Count, Is.EqualTo(100));
            Assert.That(response.Total, Is.EqualTo(PagingTests.Count));

            response = client.Get(new QueryDataPagingTest { Skip = 200 });
            Assert.That(response.Results.Count, Is.EqualTo(PagingTests.Skip(200).Count()));
            Assert.That(response.Total, Is.EqualTo(PagingTests.Count));

            response = client.Get(new QueryDataPagingTest { Value = 1 });
            Assert.That(response.Results.Count, Is.EqualTo(100));
            Assert.That(response.Total, Is.EqualTo(PagingTests.Count(x => x.Value == 1)));
        }

        [Test]
        public void Can_query_on_ForeignKey_and_Index()
        {
            QueryResponse<RockstarAlbum> response;
            response = client.Get(new QueryDataRockstarAlbums { RockstarId = 3 }); //Hash
            Assert.That(response.Results.Count, Is.EqualTo(5));
            Assert.That(response.Total, Is.EqualTo(5));

            response = client.Get(new QueryDataRockstarAlbums { RockstarId = 3, Id = 3 }); //Hash + Range
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results[0].Name, Is.EqualTo("Nevermind"));

            //Hash + Range BETWEEN
            response = client.Get(new QueryDataRockstarAlbums { RockstarId = 3, IdBetween = new[] { 2, 3 } });
            Assert.That(response.Results.Count, Is.EqualTo(2));
            Assert.That(response.Total, Is.EqualTo(2));

            //Hash + Range BETWEEN + Filter
            response = client.Get(new QueryDataRockstarAlbums
            {
                RockstarId = 3,
                IdBetween = new[] { 2, 3 },
                Name = "Nevermind"
            });
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results[0].Id, Is.EqualTo(3));

            //Hash + LocalSecondaryIndex
            response = client.Get(new QueryDataRockstarAlbums { RockstarId = 3, Genre = "Grunge" });
            Assert.That(response.Results.Count, Is.EqualTo(4));
            Assert.That(response.Total, Is.EqualTo(4));

            response.PrintDump();
        }
    }
}