using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ProtoBuf.Grpc.Client;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Extensions.Tests.Protoc
{
    public class ProtocAutoQueryTests
    {
        private readonly ServiceStackHost appHost;
        public GrpcServices.GrpcServicesClient client;

        private static readonly int TotalRockstars = AutoQueryAppHost.SeedRockstars.Length;
        private static readonly int TotalAlbums = AutoQueryAppHost.SeedAlbums.Length;

        public ProtocAutoQueryTests()
        {
            ConsoleLogFactory.Configure();
            appHost = new AutoQueryAppHost()
                .Init()
                .Start(TestsConfig.ListeningOn);

            client = ProtocTests.GetClient();
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        public List<Rockstar> Rockstars => AutoQueryAppHost.SeedRockstars.Map(x => x.ConvertTo<Rockstar>());

        public List<PagingTest> PagingTests => AutoQueryAppHost.SeedPagingTest.Map(x => x.ConvertTo<PagingTest>());

        /*
        [Test]
        public async Task Can_execute_basic_query()
        {
            var response = await client.GetQueryRockstarsAsync(new QueryRockstars {
                Include = "Total",
            });
            var req = new QueryBase {
                Include = "Total",
                QueryDbRockstar = new QueryDb_Rockstar {
                    QueryRockstars = new QueryRockstars()
                }
            };
            var response = await client.GetQueryRockstarsAsync(new QueryRockstars());
            
            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars));
        }
        
        [Test]
        public async Task Can_execute_basic_query_NamedRockstar()
        {
            var response = await client.GetAsync(new QueryNamedRockstars { Include = "Total" });
        
            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].LastName, Is.EqualTo("SQL Server"));
        }
        
        [Test]
        public async Task Can_execute_overridden_basic_query()
        {
            var response = await client.GetAsync(new QueryOverridedRockstars { Include = "Total" });
        
            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(1));
        }
        
        [Test]
        public async Task Can_execute_overridden_basic_query_with_case_insensitive_orderBy()
        {
            var response = await client.GetAsync(new QueryCaseInsensitiveOrderBy { Age = 27, OrderBy = "FirstName" });
        
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }
        
        [Test]
        public async Task Can_execute_AdhocRockstars_query()
        {
            var request = new QueryAdhocRockstars { FirstName = "Jimi", Include = "Total" };
        
            Assert.That(request.ToGetUrl(), Is.EqualTo("/adhoc-rockstars?first_name=Jimi&include=Total"));
        
            var response = await client.GetAsync(request);
        
            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo(request.FirstName));
        }
        
        [Test]
        public async Task Can_execute_explicit_equality_condition_on_overridden_CustomRockstar()
        {
            var response = await client.GetAsync(new QueryOverridedCustomRockstars { Age = 27, Include = "Total" });
        
            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(response.Results.Count, Is.EqualTo(1));
        }
        
        [Test]
        public async Task Can_execute_basic_query_with_limits()
        {
            var response = await client.GetAsync(new QueryRockstars { Skip = 2, Include = "Total" });
            Assert.That(response.Offset, Is.EqualTo(2));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars - 2));
        
            response = await client.GetAsync(new QueryRockstars { Take = 2, Include = "Total" });
            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(2));
        
            response = await client.GetAsync(new QueryRockstars { Skip = 2, Take = 2, Include = "Total" });
            Assert.That(response.Offset, Is.EqualTo(2));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(2));
        }
        
        [Test]
        public async Task Can_execute_explicit_equality_condition()
        {
            var response = await client.GetAsync(new QueryRockstars { Age = 27, Include = "Total" });
        
            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }
        
        [Test]
        public async Task Can_execute_explicit_equality_condition_on_CustomRockstar()
        {
            var response = await client.GetAsync(new QueryCustomRockstars { Age = 27, Include = "Total" });
        
            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }
        
        [Test]
        public async Task Can_execute_explicit_equality_condition_on_CustomRockstarSchema()
        {
            var response = await client.GetAsync(new QueryCustomRockstarsSchema { Age = 27, Include = "Total" });
        
            response.PrintDump();
        
            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(response.Results.Count, Is.EqualTo(3));
            Assert.That(response.Results[0].FirstName, Is.Not.Null);
            Assert.That(response.Results[0].LastName, Is.Not.Null);
            Assert.That(response.Results[0].Age, Is.EqualTo(27));
        }
        
        [Test]
        public async Task Can_execute_query_with_JOIN_on_RockstarAlbums()
        {
            var response = await client.GetAsync(new QueryJoinedRockstarAlbums { Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(TotalAlbums));
            Assert.That(response.Results.Count, Is.EqualTo(TotalAlbums));
            var albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Bleach", "Nevermind", "In Utero", "Incesticide",
                "MTV Unplugged in New York", "Foo Fighters", "Into the Wild",
            }));
        
            response = await client.GetAsync(new QueryJoinedRockstarAlbums { Age = 27, Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(6));
            Assert.That(response.Results.Count, Is.EqualTo(6));
            albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Bleach", "Nevermind", "In Utero", "Incesticide",
                "MTV Unplugged in New York",
            }));
        
            response = await client.GetAsync(new QueryJoinedRockstarAlbums { RockstarAlbumName = "Nevermind", Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] { "Nevermind" }));
        }
        
        [Test]
        public async Task Can_execute_query_with_JOIN_on_RockstarAlbums_and_CustomSelectRockstar()
        {
            var response = await client.GetAsync(new QueryJoinedRockstarAlbumsCustomSelect { Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(TotalAlbums));
            Assert.That(response.Results.Count, Is.EqualTo(TotalAlbums));
            var ages = response.Results.Select(x => x.Age);
            Assert.That(ages.Contains(27 * 2));
            
            var customRes = await client.GetAsync(new QueryJoinedRockstarAlbumsCustomSelectResponse { Include = "Total" });
            Assert.That(customRes.Total, Is.EqualTo(TotalAlbums));
            Assert.That(customRes.Results.Count, Is.EqualTo(TotalAlbums));
            ages = customRes.Results.Select(x => x.Age);
            Assert.That(ages.Contains(27 * 2));
        }
        
        [Test]
        public async Task Can_execute_query_with_multiple_JOINs_on_Rockstar_Albums_and_Genres()
        {
            var response = await client.GetAsync(new QueryMultiJoinRockstar { Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(TotalAlbums));
            Assert.That(response.Results.Count, Is.EqualTo(TotalAlbums));
            var albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Bleach", "Nevermind", "In Utero", "Incesticide",
                "MTV Unplugged in New York", "Foo Fighters", "Into the Wild",
            }));
        
            var genreNames = response.Results.Select(x => x.RockstarGenreName).Distinct();
            Assert.That(genreNames, Is.EquivalentTo(new[] {
                "Rock", "Grunge", "Alternative Rock", "Folk Rock"
            }));
        
            response = await client.GetAsync(new QueryMultiJoinRockstar { RockstarAlbumName = "Nevermind", Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] { "Nevermind" }));
        
            response = await client.GetAsync(new QueryMultiJoinRockstar { RockstarGenreName = "Folk Rock", Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            albumNames = response.Results.Select(x => x.RockstarGenreName);
            Assert.That(albumNames, Is.EquivalentTo(new[] { "Folk Rock" }));
        }
        
        [Test]
        public async Task Can_execute_query_with_LEFTJOIN_on_RockstarAlbums()
        {
            var response = await client.GetAsync(new QueryRockstarAlbumsLeftJoin { IdNotEqualTo = 3, Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(TotalRockstars - 1));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars - 1));
            var albumNames = response.Results.Where(x => x.RockstarAlbumName != null).Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Foo Fighters", "Into the Wild"
            }));
        }
        
        [Test]
        public async Task Can_execute_query_with_custom_LEFTJOIN_on_RockstarAlbums()
        {
            var response = await client.GetAsync(new QueryRockstarAlbumsCustomLeftJoin { IdNotEqualTo = 3, Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(TotalRockstars - 1));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars - 1));
            var albumNames = response.Results.Where(x => x.RockstarAlbumName != null).Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Foo Fighters", "Into the Wild"
            }));
        }
        
        [Test]
        public async Task Can_execute_custom_QueryFields()
        {
            QueryResponse<Rockstar> response;
            response = await client.GetAsync(new QueryFieldRockstars { FirstName = "Jim" });
            Assert.That(response.Results.Count, Is.EqualTo(1));
        
            response = await client.GetAsync(new QueryFieldRockstars { FirstNames = new[] { "Jim","Kurt" } });
            Assert.That(response.Results.Count, Is.EqualTo(2));
        
            response = await client.GetAsync(new QueryFieldRockstars { FirstNameCaseInsensitive = "jim" });
            Assert.That(response.Results.Count, Is.EqualTo(1));
        
            response = await client.GetAsync(new QueryFieldRockstars { FirstNameStartsWith = "Jim" });
            Assert.That(response.Results.Count, Is.EqualTo(2));
        
            response = await client.GetAsync(new QueryFieldRockstars { LastNameEndsWith = "son" });
            Assert.That(response.Results.Count, Is.EqualTo(2));
        
            response = await client.GetAsync(new QueryFieldRockstars { FirstNameBetween = new[] {"A","F"} });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        
            response = await client.GetAsync(new QueryFieldRockstars
            {
                LastNameEndsWith = "son",
                OrLastName = "Hendrix"
            });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        
            response = await client.GetAsync(new QueryFieldRockstars
            {
                FirstNameStartsWith = "Jim",
                OrLastName = "Presley"
            });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        
            response = await client.GetAsync(new QueryFieldRockstars { Age = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(4));
        }
        
        [Test]
        public async Task Can_execute_combination_of_QueryFields()
        {
            QueryResponse<Rockstar> response;
        
            response = await client.GetAsync(new QueryFieldRockstars
            {
                FirstNameStartsWith = "Jim",
                LastNameEndsWith = "son",
            });
            Assert.That(response.Results.Count, Is.EqualTo(1));
        
            response = await client.GetAsync(new QueryFieldRockstars
            {
                FirstNameStartsWith = "Jim",
                OrLastName = "Cobain",
            });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }
        
        [Test]
        public async Task Does_escape_values()
        {
            QueryResponse<Rockstar> response;
        
            response = await client.GetAsync(new QueryFieldRockstars
            {
                FirstNameStartsWith = "Jim'\"",
            });
            Assert.That(response.Results?.Count ?? 0, Is.EqualTo(0));
        }
        
        [Test]
        public async Task Does_use_custom_model_to_select_columns()
        {
            var response = await client.GetAsync(new QueryRockstarAlias { RockstarAlbumName = "Nevermind" });
        
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].RockstarId, Is.EqualTo(3));
            Assert.That(response.Results[0].FirstName, Is.EqualTo("Kurt"));
            Assert.That(response.Results[0].RockstarAlbumName, Is.EqualTo("Nevermind"));
        }
        
        [Test]
        public async Task Does_allow_adding_attributes_dynamically()
        {
            typeof(QueryFieldRockstarsDynamic)
                .GetProperty("Age")
                .AddAttributes(new QueryDbFieldAttribute { Operand = ">=" });
        
            var response = await client.GetAsync(new QueryFieldRockstarsDynamic { Age = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(4));
        }
        
        [Test]
        public async Task Does_execute_typed_QueryFilters()
        {
            // QueryFilter appends additional: x => x.LastName.EndsWith("son")
            var response = await client.GetAsync(new QueryRockstarsFilter { Age = 27 });
            Assert.That(response.Results.Count, Is.EqualTo(1));
        
            var custom = await client.GetAsync(new QueryCustomRockstarsFilter { Age = 27 });
            Assert.That(custom.Results.Count, Is.EqualTo(1));
        
            response = await client.GetAsync(new QueryRockstarsIFilter { Age = 27 });
            Assert.That(response.Results.Count, Is.EqualTo(1));
        }
        
        [Test]
        public async Task Can_execute_OR_QueryFilters()
        {
            var response = await client.GetAsync(new QueryOrRockstars { Age = 42, FirstName = "Jim" });
            Assert.That(response.Results.Count, Is.EqualTo(2));
        }
        
        [Test]
        public async Task Does_retain_implicit_convention_when_not_overriding_template_or_ValueFormat()
        {
            var response = await client.GetAsync(new QueryFieldsImplicitConventions { FirstNameContains = "im" });
            Assert.That(response.Results.Count, Is.EqualTo(2));
        
            response = await client.GetAsync(new QueryFieldsImplicitConventions { LastNameEndsWith = "son" });
            Assert.That(response.Results.Count, Is.EqualTo(2));
        }
        
        [Test]
        public async Task Can_execute_OR_QueryFilters_Fields()
        {
            var response = await client.GetAsync(new QueryOrRockstarsFields
            {
                FirstName = "Jim",
                LastName = "Vedder",
            });
            Assert.That(response.Results.Count, Is.EqualTo(2));
        }
        
        [Test]
        public async Task Can_execute_Explicit_conventions()
        {
            var response = await client.GetAsync(new QueryRockstarsConventions { Ids = new[] {1, 2, 3} });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        
            response = await client.GetAsync(new QueryRockstarsConventions { AgeOlderThan = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        
            response = await client.GetAsync(new QueryRockstarsConventions { AgeGreaterThanOrEqualTo = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(4));
        
            response = await client.GetAsync(new QueryRockstarsConventions { AgeGreaterThan = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = await client.GetAsync(new QueryRockstarsConventions { GreaterThanAge = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        
            response = await client.GetAsync(new QueryRockstarsConventions { FirstNameStartsWith = "Jim" });
            Assert.That(response.Results.Count, Is.EqualTo(2));
            response = await client.GetAsync(new QueryRockstarsConventions { LastNameEndsWith = "son" });
            Assert.That(response.Results.Count, Is.EqualTo(2));
            response = await client.GetAsync(new QueryRockstarsConventions { LastNameContains = "e" });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        
            response = await client.GetAsync(new QueryRockstarsConventions { DateOfBirthGreaterThan = new DateTime(1960, 01, 01) });
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = await client.GetAsync(new QueryRockstarsConventions { DateDiedLessThan = new DateTime(1980, 01, 01) });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }
        
        [Test]
        public async Task Can_execute_In_OR_Queries()
        {
            QueryResponse<Rockstar> response;
            response = await client.GetAsync(new QueryGetRockstars());
            Assert.That(response.Results?.Count ?? 0, Is.EqualTo(0));
        
            response = await client.GetAsync(new QueryGetRockstars { Ids = new[] { 1, 2, 3 } });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        
            response = await client.GetAsync(new QueryGetRockstars { Ages = new[] { 42, 44 }.ToList() });
            Assert.That(response.Results.Count, Is.EqualTo(2));
        
            response = await client.GetAsync(new QueryGetRockstars { FirstNames = new[] { "Jim", "Kurt" }.ToList() });
            Assert.That(response.Results.Count, Is.EqualTo(2));
        
            response = await client.GetAsync(new QueryGetRockstars { IdsBetween = new[] { 1, 3 } });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }
        
        [Test]
        public async Task Does_ignore_empty_collection_filters_by_default()
        {
            QueryResponse<Rockstar> response;
            response = await client.GetAsync(new QueryRockstarFilters());
            Assert.That(response.Results.Count, Is.EqualTo(AutoQueryAppHost.SeedRockstars.Length));
        
            response = await client.GetAsync(new QueryRockstarFilters
            {
                Ids = new int[] {},
                Ages = new List<int>(),
                FirstNames = new List<string>(),
                IdsBetween = new int[] {},               
            });
            Assert.That(response.Results.Count, Is.EqualTo(AutoQueryAppHost.SeedRockstars.Length));
        }
        
        [Test]
        public async Task Can_query_Movie_Ratings()
        {
            var response = await client.GetAsync(new QueryMovies { Ratings = new[] {"G","PG-13"} });
            Assert.That(response.Results.Count, Is.EqualTo(5));
        
            response = await client.GetAsync(new QueryMovies {
                Ids = new[] { 1, 2 },
                ImdbIds = new[] { "tt0071562", "tt0060196" },
                Ratings = new[] { "G", "PG-13" }
            });
            Assert.That(response.Results.Count, Is.EqualTo(9));
        }
        
        [Test]
        public async Task Does_implicitly_OrderBy_PrimaryKey_when_limits_is_specified()
        {
            var movies = await client.GetAsync(new SearchMovies { Take = 100 });
            var ids = movies.Results.Map(x => x.Id);
            var orderedIds = ids.OrderBy(x => x);
            Assert.That(ids, Is.EqualTo(orderedIds));
        
            var rockstars = await client.GetAsync(new SearchMovies { Take = 100 });
            ids = rockstars.Results.Map(x => x.Id);
            orderedIds = ids.OrderBy(x => x);
            Assert.That(ids, Is.EqualTo(orderedIds));
        }
        
        [Test]
        public async Task Can_OrderBy_queries()
        {
            var movies = await client.GetAsync(new SearchMovies { Take = 100, OrderBy = "ImdbId" });
            var ids = movies.Results.Map(x => x.ImdbId);
            var orderedIds = ids.OrderBy(x => x).ToList();
            Assert.That(ids, Is.EqualTo(orderedIds));
        
            movies = await client.GetAsync(new SearchMovies { Take = 100, OrderBy = "Rating,ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderBy(x => x.Rating).ThenBy(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));
        
            movies = await client.GetAsync(new SearchMovies { Take = 100, OrderByDesc = "ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = ids.OrderByDescending(x => x).ToList();
            Assert.That(ids, Is.EqualTo(orderedIds));
        
            movies = await client.GetAsync(new SearchMovies { Take = 100, OrderByDesc = "Rating,ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderByDescending(x => x.Rating)
                .ThenByDescending(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));
        
            movies = await client.GetAsync(new SearchMovies { Take = 100, OrderBy = "Rating,-ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderBy(x => x.Rating)
                .ThenByDescending(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));
        
            movies = await client.GetAsync(new SearchMovies { Take = 100, OrderByDesc = "Rating,-ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderByDescending(x => x.Rating)
                .ThenBy(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));
        }
        
        [Test]
        public async Task Does_not_query_Ignored_properties()
        {
            var response = await client.GetAsync(new QueryUnknownRockstars {
                UnknownProperty = "Foo",
                UnknownInt = 1,
                Include = "Total"
            });
        
            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars));
        }
        
        [Test]
        public async Task Can_Query_Rockstars_with_References()
        {
            var response = await client.GetAsync(new QueryRockstarsWithReferences {
                Age = 27
            });
         
            Assert.That(response.Results.Count, Is.EqualTo(3));
        
            var jimi = response.Results.First(x => x.FirstName == "Jimi");
            Assert.That(jimi.Albums.Count, Is.EqualTo(1));
            Assert.That(jimi.Albums[0].Name, Is.EqualTo("Electric Ladyland"));
        
            var jim = response.Results.First(x => x.FirstName == "Jim");
            Assert.That(jim.Albums, Is.Null);
        
            var kurt = response.Results.First(x => x.FirstName == "Kurt");
            Assert.That(kurt.Albums.Count, Is.EqualTo(5));
        
            response = await client.GetAsync(new QueryRockstarsWithReferences
            {
                Age = 27,
                Fields = "Id,FirstName,Age"
            });
            Assert.That(response.Results.Count, Is.EqualTo(3));
            Assert.That(response.Results.All(x => x.Id > 0));
            Assert.That(response.Results.All(x => x.LastName == null));
            Assert.That(response.Results.All(x => x.Albums == null));
        
            response = await client.GetAsync(new QueryRockstarsWithReferences
            {
                Age = 27,
                Fields = "Id,FirstName,Age,Albums"
            });
            Assert.That(response.Results.Where(x => x.FirstName != "Jim").All(x => x.Albums != null));
        }
        
        [Test]
        public async Task Can_Query_RockstarReference_without_References()
        {
            var response = await client.GetAsync(new QueryCustomRockstarsReferences
            {
                Age = 27
            });
            Assert.That(response.Results.Count, Is.EqualTo(3));
            Assert.That(response.Results.All(x => x.Albums == null));
        }
        
        [Test]
        public async Task Can_Query_AllFields_Guid()
        {
            var guid = new Guid("3EE6865A-4149-4940-B7A2-F952E0FEFC5E");
            var response = await client.GetAsync(new QueryAllFields {
                Guid = guid
            });
        
            Assert.That(response.Results.Count, Is.EqualTo(1));
        
            Assert.That(response.Results[0].Guid, Is.EqualTo(guid));
        }
        
        [Test]
        public async Task Does_populate_Total()
        {
            var response = await client.GetAsync(new QueryRockstars { Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));
            Assert.That(response.Meta, Is.Null);
        
            response = await client.GetAsync(new QueryRockstars { Include = "COUNT" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));
        
            response = await client.GetAsync(new QueryRockstars { Include = "COUNT(*)" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));
        
            response = await client.GetAsync(new QueryRockstars { Include = "COUNT(DISTINCT LivingStatus), Total" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));
        
            response = await client.GetAsync(new QueryRockstars { Include = "Count(*), Min(Age), Max(Age), Sum(Id)" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));
        
            response = await client.GetAsync(new QueryRockstars { Age = 27, Include = "Count(*), Min(Age), Max(Age), Sum(Id)" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count(x => x.Age == 27)));
        }
        
        [Test]
        public async Task Can_Include_Aggregates_in_AutoQuery()
        {
            var response = await client.GetAsync(new QueryRockstars { Include = "COUNT" });
            Assert.That(response.Meta["COUNT(*)"], Is.EqualTo(Rockstars.Count.ToString()));
        
            response = await client.GetAsync(new QueryRockstars { Include = "COUNT(*)" });
            Assert.That(response.Meta["COUNT(*)"], Is.EqualTo(Rockstars.Count.ToString()));
        
            response = await client.GetAsync(new QueryRockstars { Include = "COUNT(DISTINCT LivingStatus)" });
            Assert.That(response.Meta["COUNT(DISTINCT LivingStatus)"], Is.EqualTo("2"));
        
            response = await client.GetAsync(new QueryRockstars { Include = "MIN(Age)" });
            Assert.That(response.Meta["MIN(Age)"], Is.EqualTo(Rockstars.Map(x => x.Age).Min().ToString()));
        
            response = await client.GetAsync(new QueryRockstars { Include = "Count(*), Min(Age), Max(Age), Sum(Id), Avg(Age)", OrderBy = "Id" });
            Assert.That(response.Meta["Count(*)"], Is.EqualTo(Rockstars.Count.ToString()));
            Assert.That(response.Meta["Min(Age)"], Is.EqualTo(Rockstars.Map(x => x.Age).Min().ToString()));
            Assert.That(response.Meta["Max(Age)"], Is.EqualTo(Rockstars.Map(x => x.Age).Max().ToString()));
            Assert.That(response.Meta["Sum(Id)"], Is.EqualTo(Rockstars.Map(x => x.Id).Sum().ToString()));
            Assert.That(double.Parse(response.Meta["Avg(Age)"]), Is.EqualTo(Rockstars.Average(x => x.Age)).Within(1d));
            //Not supported by Sqlite
            //Assert.That(response.Meta["First(Id)"], Is.EqualTo(Rockstars.First().Id.ToString()));
            //Assert.That(response.Meta["Last(Id)"], Is.EqualTo(Rockstars.Last().Id.ToString()));
        
            response = await client.GetAsync(new QueryRockstars { Age = 27, Include = "Count(*), Min(Age), Max(Age), Sum(Id), Avg(Age)", OrderBy = "Id" });
            var rockstars27 = Rockstars.Where(x => x.Age == 27).ToList();
            Assert.That(response.Meta["Count(*)"], Is.EqualTo(rockstars27.Count.ToString()));
            Assert.That(response.Meta["Min(Age)"], Is.EqualTo(rockstars27.Map(x => x.Age).Min().ToString()));
            Assert.That(response.Meta["Max(Age)"], Is.EqualTo(rockstars27.Map(x => x.Age).Max().ToString()));
            Assert.That(response.Meta["Sum(Id)"], Is.EqualTo(rockstars27.Map(x => x.Id).Sum().ToString()));
            Assert.That(double.Parse(response.Meta["Avg(Age)"]), Is.EqualTo(rockstars27.Average(x => x.Age)).Within(1d));
            //Not supported by Sqlite
            //Assert.That(response.Meta["First(Id)"], Is.EqualTo(rockstars27.First().Id.ToString()));
            //Assert.That(response.Meta["Last(Id)"], Is.EqualTo(rockstars27.Last().Id.ToString()));
        }
        
        [Test]
        public async Task Does_ignore_unknown_aggregate_commands()
        {
            var response = await client.GetAsync(new QueryRockstars { Include = "FOO(1), Total" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));
            Assert.That(response.Meta, Is.Null);
        
            response = await client.GetAsync(new QueryRockstars { Include = "FOO(1), Min(Age), Bar('a') alias, Count(*), Baz(1,'foo')" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));
            Assert.That(response.Meta["Min(Age)"], Is.EqualTo(Rockstars.Map(x => x.Age).Min().ToString()));
            Assert.That(response.Meta["Count(*)"], Is.EqualTo(Rockstars.Count.ToString()));
        }
        
        [Test]
        public async Task Can_Include_Aggregates_in_AutoQuery_with_Aliases()
        {
            var response = await client.GetAsync(new QueryRockstars { Include = "COUNT(*) count" });
            Assert.That(response.Meta["count"], Is.EqualTo(Rockstars.Count.ToString()));
        
            response = await client.GetAsync(new QueryRockstars { Include = "COUNT(DISTINCT LivingStatus) as uniquestatus" });
            Assert.That(response.Meta["uniquestatus"], Is.EqualTo("2"));
        
            response = await client.GetAsync(new QueryRockstars { Include = "MIN(Age) minage" });
            Assert.That(response.Meta["minage"], Is.EqualTo(Rockstars.Map(x => x.Age).Min().ToString()));
        
            response = await client.GetAsync(new QueryRockstars { Include = "Count(*) count, Min(Age) min, Max(Age) max, Sum(Id) sum" });
            Assert.That(response.Meta["count"], Is.EqualTo(Rockstars.Count.ToString()));
            Assert.That(response.Meta["min"], Is.EqualTo(Rockstars.Map(x => x.Age).Min().ToString()));
            Assert.That(response.Meta["max"], Is.EqualTo(Rockstars.Map(x => x.Age).Max().ToString()));
            Assert.That(response.Meta["sum"], Is.EqualTo(Rockstars.Map(x => x.Id).Sum().ToString()));
        }
        
        [Test]
        public async Task Can_execute_custom_aggregate_functions()
        {
            var response = await client.GetAsync(new QueryRockstars {
                Include = "ADD(6,2), Multiply(6,2) SixTimesTwo, Subtract(6,2), divide(6,2) TheDivide"
            });
            Assert.That(response.Meta["ADD(6,2)"], Is.EqualTo("8"));
            Assert.That(response.Meta["SixTimesTwo"], Is.EqualTo("12"));
            Assert.That(response.Meta["Subtract(6,2)"], Is.EqualTo("4"));
            Assert.That(response.Meta["TheDivide"], Is.EqualTo("3"));
        }
        
        [Test]
        public async Task Sending_empty_ChangeDb_returns_default_info()
        {
            var response = await client.GetAsync(new ChangeDb());
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars));
        
            var aqResponse = await client.GetAsync(new QueryChangeDb());
            Assert.That(aqResponse.Results.Count, Is.EqualTo(TotalRockstars));
        }
        
        [Test]
        public async Task Can_ChangeDb_with_Named_Connection()
        {
            var response = await client.GetAsync(new ChangeDb { NamedConnection = AutoQueryAppHost.SqlServerNamedConnection });
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo("Microsoft"));
        
            var aqResponse = await client.GetAsync(new QueryChangeDb { NamedConnection = AutoQueryAppHost.SqlServerNamedConnection });
            Assert.That(aqResponse.Results.Count, Is.EqualTo(1));
            Assert.That(aqResponse.Results[0].FirstName, Is.EqualTo("Microsoft"));
        }
        
        [Test]
        public async Task Can_ChangeDb_with_ConnectionString()
        {
            var response = await client.GetAsync(new ChangeDb { ConnectionString = AutoQueryAppHost.SqliteFileConnString });
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo("Sqlite"));
        
            var aqResponse = await client.GetAsync(new QueryChangeDb { ConnectionString = AutoQueryAppHost.SqliteFileConnString });
            Assert.That(aqResponse.Results.Count, Is.EqualTo(1));
            Assert.That(aqResponse.Results[0].FirstName, Is.EqualTo("Sqlite"));
        }
        
        [Test]
        public async Task Can_ChangeDb_with_ConnectionString_and_Provider()
        {
            var response = await client.GetAsync(new ChangeDb
            {
                ConnectionString = AutoQueryAppHost.SqlServerConnString,
                ProviderName = AutoQueryAppHost.SqlServerProvider,
            });
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo("Microsoft"));
        
            var aqResponse = await client.GetAsync(new QueryChangeDb
            {
                ConnectionString = AutoQueryAppHost.SqlServerConnString,
                ProviderName = AutoQueryAppHost.SqlServerProvider,
            });
            Assert.That(aqResponse.Results.Count, Is.EqualTo(1));
            Assert.That(aqResponse.Results[0].FirstName, Is.EqualTo("Microsoft"));
        }
        
        [Test]
        public async Task Can_Change_Named_Connection_with_ConnectionInfoAttribute()
        {
            var response = await client.GetAsync(new ChangeConnectionInfo());
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo("Microsoft"));
        
            var aqResponse = await client.GetAsync(new QueryChangeConnectionInfo());
            Assert.That(aqResponse.Results.Count, Is.EqualTo(1));
            Assert.That(aqResponse.Results[0].FirstName, Is.EqualTo("Microsoft"));
        }
        
        [Test]
        public async Task Does_return_MaxLimit_results()
        {
            QueryResponse<PagingTest> response;
            response = await client.GetAsync(new QueryPagingTest { Include = "Total" });
            Assert.That(response.Results.Count, Is.EqualTo(100));
            Assert.That(response.Total, Is.EqualTo(PagingTests.Count));
        
            response = await client.GetAsync(new QueryPagingTest { Skip = 200, Include = "Total" });
            Assert.That(response.Results.Count, Is.EqualTo(PagingTests.Skip(200).Count()));
            Assert.That(response.Total, Is.EqualTo(PagingTests.Count));
        
            response = await client.GetAsync(new QueryPagingTest { Value = 1, Include = "Total" });
            Assert.That(response.Results.Count, Is.EqualTo(100));
            Assert.That(response.Total, Is.EqualTo(PagingTests.Count(x => x.Value == 1)));
        }
        
        [Test]
        public async Task Can_query_on_ForeignKey_and_Index()
        {
            QueryResponse<RockstarAlbum> response;
            response = await client.GetAsync(new QueryRockstarAlbums { RockstarId = 3, Include = "Total" }); //Hash
            Assert.That(response.Results.Count, Is.EqualTo(5));
            Assert.That(response.Total, Is.EqualTo(5));
        
            response = await client.GetAsync(new QueryRockstarAlbums { RockstarId = 3, Id = 3, Include = "Total" }); //Hash + Range
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results[0].Name, Is.EqualTo("Nevermind"));
        
            //Hash + Range BETWEEN
            response = await client.GetAsync(new QueryRockstarAlbums
            {
                RockstarId = 3,
                IdBetween = new[] { 2, 3 },
                Include = "Total"
            });
            Assert.That(response.Results.Count, Is.EqualTo(2));
            Assert.That(response.Total, Is.EqualTo(2));
        
            //Hash + Range BETWEEN + Filter
            response = await client.GetAsync(new QueryRockstarAlbums
            {
                RockstarId = 3,
                IdBetween = new[] { 2, 3 },
                Name = "Nevermind",
                Include = "Total"
            });
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results[0].Id, Is.EqualTo(3));
        
            //Hash + LocalSecondaryIndex
            response = await client.GetAsync(new QueryRockstarAlbums { RockstarId = 3, Genre = "Grunge", Include = "Total" });
            Assert.That(response.Results.Count, Is.EqualTo(4));
            Assert.That(response.Total, Is.EqualTo(4));
        
            response.PrintDump();
        }
        */
    }
}