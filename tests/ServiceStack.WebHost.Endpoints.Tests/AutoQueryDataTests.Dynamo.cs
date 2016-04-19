using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2;
using Funq;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class AutoQueryDataDynamoTests : AutoQueryDataTests
    {
        public override ServiceStackHost CreateAppHost()
        {
            return new AutoQueryDataDynamoAppHost();
        }

        [Test]
        public void Does_perform_QUERY_operation_when_querying_hash_key()
        {
            var response = client.Get(new QueryDataRockstarAlbums
            {
                RockstarId = 3,
                Genre = "Grunge",
            });

            Assert.That(response.Results.Count, Is.EqualTo(4));
            Assert.That(response.Results.Map(x => x.RockstarId).Distinct(),
                Is.EquivalentTo(new[] { 3 }));
        }

        [Test]
        public void Does_perform_SCAN_operation_when_not_querying_hash_key()
        {
            var response = client.Get(new QueryDataRockstarAlbums
            {
                Genre = "Grunge",
            });

            Assert.That(response.Results.Count, Is.EqualTo(5));
            Assert.That(response.Results.Map(x => x.RockstarId).Distinct(),
                Is.EquivalentTo(new[] { 3, 5 }));
        }

        [Test]
        public void Can_query_on_ForeignKey_and_RockstarAlbumGenreIndex()
        {
            QueryResponse<RockstarAlbumGenreGlobalIndex> response;
            response = client.Get(new QueryDataRockstarAlbumGenreIndex { Genre = "Grunge" }); //Hash
            Assert.That(response.Results.Count, Is.EqualTo(5));
            Assert.That(response.Total, Is.EqualTo(5));

            response = client.Get(new QueryDataRockstarAlbumGenreIndex { Genre = "Grunge", Id = 3 }); //Hash + Range
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results[0].Name, Is.EqualTo("Nevermind"));

            //Hash + Range BETWEEN
            response = client.Get(new QueryDataRockstarAlbumGenreIndex { Genre = "Grunge", IdBetween = new[] { 2, 3 } });
            Assert.That(response.Results.Count, Is.EqualTo(2));
            Assert.That(response.Total, Is.EqualTo(2));

            //Hash + Range BETWEEN + Filter
            response = client.Get(new QueryDataRockstarAlbumGenreIndex
            {
                Genre = "Grunge",
                IdBetween = new[] { 2, 3 },
                Name = "Nevermind"
            });
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results[0].Id, Is.EqualTo(3));

            response.PrintDump();
        }

        [Test]
        public void Can_query_on_ForeignKey_and_RockstarAlbumGenreIndex_Mapped()
        {
            QueryResponse<RockstarAlbum> response;
            response = client.Get(new QueryDataRockstarAlbumGenreIndexMapped { Genre = "Grunge" }); //Hash
            Assert.That(response.Results.Count, Is.EqualTo(5));
            Assert.That(response.Total, Is.EqualTo(5));

            response = client.Get(new QueryDataRockstarAlbumGenreIndexMapped { Genre = "Grunge", Id = 3 }); //Hash + Range
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results[0].Name, Is.EqualTo("Nevermind"));

            //Hash + Range BETWEEN
            response = client.Get(new QueryDataRockstarAlbumGenreIndexMapped { Genre = "Grunge", IdBetween = new[] { 2, 3 } });
            Assert.That(response.Results.Count, Is.EqualTo(2));
            Assert.That(response.Total, Is.EqualTo(2));

            //Hash + Range BETWEEN + Filter
            response = client.Get(new QueryDataRockstarAlbumGenreIndexMapped
            {
                Genre = "Grunge",
                IdBetween = new[] { 2, 3 },
                Name = "Nevermind"
            });
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results[0].Id, Is.EqualTo(3));

            response.PrintDump();
        }

        [Test]
        public void Can_query_MovieTitleIndex_Ratings()
        {
            var response = client.Get(new QueryDataMovieTitleIndex { Ratings = new[] { "G", "PG-13" } });
            Assert.That(response.Results.Count, Is.EqualTo(5));

            var url = Config.ListeningOn + "moviesdataindex/search?ratings=G,PG-13";
            response = url.AsJsonInto<MovieTitleIndex>();
            Assert.That(response.Results.Count, Is.EqualTo(5));

            response = client.Get(new QueryDataMovieTitleIndex
            {
                Ids = new[] { 1, 2 },
                ImdbIds = new[] { "tt0071562", "tt0060196" },
                Ratings = new[] { "G", "PG-13" }
            });
            Assert.That(response.Results.Count, Is.EqualTo(9));

            url = Config.ListeningOn + "moviesdataindex?ratings=G,PG-13&ids=1,2&imdbIds=tt0071562,tt0060196";
            response = url.AsJsonInto<MovieTitleIndex>();
            Assert.That(response.Results.Count, Is.EqualTo(9));
        }

        [Test]
        public void Does_implicitly_OrderBy_PrimaryKey_when_limits_is_specified_SearchDataMovieTitleIndex()
        {
            var movies = client.Get(new SearchDataMovieTitleIndex { Take = 100 });
            var ids = movies.Results.Map(x => x.Id);
            var orderedIds = ids.OrderBy(x => x);
            Assert.That(ids, Is.EqualTo(orderedIds));

            var rockstars = client.Get(new SearchDataMovieTitleIndex { Take = 100 });
            ids = rockstars.Results.Map(x => x.Id);
            orderedIds = ids.OrderBy(x => x);
            Assert.That(ids, Is.EqualTo(orderedIds));
        }

        [Test]
        public void Can_OrderBy_queries_SearchDataMovieTitleIndex()
        {
            var movies = client.Get(new SearchDataMovieTitleIndex { Take = 100, OrderBy = "ImdbId" });
            var ids = movies.Results.Map(x => x.ImdbId);
            var orderedIds = ids.OrderBy(x => x).ToList();
            Assert.That(ids, Is.EqualTo(orderedIds));

            movies = client.Get(new SearchDataMovieTitleIndex { Take = 100, OrderBy = "Rating,ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderBy(x => x.Rating).ThenBy(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));

            movies = client.Get(new SearchDataMovieTitleIndex { Take = 100, OrderByDesc = "ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = ids.OrderByDescending(x => x).ToList();
            Assert.That(ids, Is.EqualTo(orderedIds));

            movies = client.Get(new SearchDataMovieTitleIndex { Take = 100, OrderByDesc = "Rating,ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderByDescending(x => x.Rating)
                .ThenByDescending(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));

            movies = client.Get(new SearchDataMovieTitleIndex { Take = 100, OrderBy = "Rating,-ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderBy(x => x.Rating)
                .ThenByDescending(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));

            movies = client.Get(new SearchDataMovieTitleIndex { Take = 100, OrderByDesc = "Rating,-ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderByDescending(x => x.Rating)
                .ThenBy(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));

            var url = Config.ListeningOn + "moviesdata/search?take=100&orderBy=Rating,ImdbId";
            movies = url.AsJsonInto<MovieTitleIndex>();
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderBy(x => x.Rating).ThenBy(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));

            url = Config.ListeningOn + "moviesdata/search?take=100&orderByDesc=Rating,ImdbId";
            movies = url.AsJsonInto<MovieTitleIndex>();
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderByDescending(x => x.Rating)
                .ThenByDescending(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));
        }

        [Test]
        public void Cached_DynamoQuery_does_cached_duplicate_requests_when_MaxAge()
        {
            var request = new QueryCacheMaxAgeDataRockstars();
            var client = new CachedServiceClient(new JsonServiceClient(Config.ListeningOn));

            var response = client.Get(request);
            Assert.That(client.CacheHits, Is.EqualTo(0));
            Assert.That(response.Results.Count, Is.EqualTo(Rockstars.Count));

            response = client.Get(request);
            Assert.That(client.CacheHits, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(Rockstars.Count));

            response = client.Get(new QueryCacheMaxAgeDataRockstars { Age = 27 });
            Assert.That(client.CacheHits, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(Rockstars.Count(x => x.Age == 27)));
        }

        [Test]
        public void Cached_DynamoQuery_does_cached_duplicate_requests_when_MaxAge_Custom_Cached()
        {
            var request = new CustomQueryCacheMaxAgeDataRockstars();
            var client = new CachedServiceClient(new JsonServiceClient(Config.ListeningOn));

            var response = client.Get(request);
            Assert.That(client.CacheHits, Is.EqualTo(0));
            Assert.That(response.Results.Count, Is.EqualTo(Rockstars.Count));

            response = client.Get(request);
            Assert.That(client.CacheHits, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(Rockstars.Count));

            response = client.Get(new CustomQueryCacheMaxAgeDataRockstars { Age = 27 });
            Assert.That(client.CacheHits, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(Rockstars.Count(x => x.Age == 27)));
        }

        [Test]
        public void Cached_DynamoQuery_does_return_NotModified_when_MustRevalidate()
        {
            var request = new QueryCacheMustRevalidateDataRockstars();
            var client = new CachedServiceClient(new JsonServiceClient(Config.ListeningOn));

            var response = client.Get(request);
            Assert.That(client.NotModifiedHits, Is.EqualTo(0));
            Assert.That(response.Results.Count, Is.EqualTo(Rockstars.Count));

            response = client.Get(request);
            Assert.That(client.NotModifiedHits, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(Rockstars.Count));

            response = client.Get(new QueryCacheMustRevalidateDataRockstars { Age = 27 });
            Assert.That(client.CacheHits, Is.EqualTo(0));
            Assert.That(client.NotModifiedHits, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(Rockstars.Count(x => x.Age == 27)));
        }

        [Test]
        public void Does_throw_on_SCAN_when_not_allowScans()
        {
            var results = client.Get(new QueryScannedTable { Id = 1 });

            Assert.Throws<WebServiceException>(() => 
                client.Get(new QueryScannedTable { Name = "foo" })
            );
        }
    }

    public class AutoQueryDataDynamoAppHost : AutoQueryDataAppHost
    {
        public override void Configure(Container container)
        {
            base.Configure(container);

            container.Register(c => new PocoDynamo(
                new AmazonDynamoDBClient("keyId", "key", new AmazonDynamoDBConfig
                {
                    ServiceURL = "http://localhost:8000",
                }))
                .RegisterTable<Rockstar>()
                .RegisterTable<RockstarAlbum>()
                .RegisterTable<Adhoc>()
                .RegisterTable<Movie>()
                .RegisterTable<AllFields>()
                .RegisterTable<PagingTest>()
                .RegisterTable<ScannedTable>()
            );

            var dynamo = container.Resolve<IPocoDynamo>();
            //dynamo.DeleteAllTables();
            dynamo.InitSchema();
            dynamo.PutItems(SeedRockstars);
            dynamo.PutItems(SeedAlbums);
            dynamo.PutItems(SeedAdhoc);
            dynamo.PutItems(SeedMovies);
            dynamo.PutItems(SeedAllFields);
            dynamo.PutItems(SeedPagingTest);

            var feature = this.GetPlugin<AutoQueryDataFeature>();
            feature.AddDataSource(ctx => ctx.DynamoDbSource<Rockstar>());
            feature.AddDataSource(ctx => ctx.DynamoDbSource<RockstarAlbum>());
            feature.AddDataSource(ctx => ctx.DynamoDbSource<RockstarAlbumGenreGlobalIndex>());
            feature.AddDataSource(ctx => ctx.DynamoDbSource<Adhoc>());
            feature.AddDataSource(ctx => ctx.DynamoDbSource<Movie>());
            feature.AddDataSource(ctx => ctx.DynamoDbSource<MovieTitleIndex>());
            feature.AddDataSource(ctx => ctx.DynamoDbSource<AllFields>());
            feature.AddDataSource(ctx => ctx.DynamoDbSource<PagingTest>());
            feature.AddDataSource(ctx => ctx.DynamoDbSource<ScannedTable>(allowScans: false));
        }
    }

    public class ScannedTable
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class QueryScannedTable : QueryData<ScannedTable>
    {
        public int? Id { get; set; }
        public string Name { get; set; }
    }

    [Route("/moviesdataindex/search")]
    [QueryData(QueryTerm.And)] //Default
    public class SearchDataMovieTitleIndex : QueryData<MovieTitleIndex> { }

    [Route("/moviesdataindex")]
    [QueryData(QueryTerm.Or)]
    public class QueryDataMovieTitleIndex : QueryData<MovieTitleIndex>
    {
        public int[] Ids { get; set; }
        public string[] ImdbIds { get; set; }
        public string[] Ratings { get; set; }
    }

    public class MovieTitleIndex : IGlobalIndex<Movie>
    {
        [HashKey]
        public string Title { get; set; }

        [RangeKey]
        public decimal Score { get; set; }

        public int Id { get; set; }
        public string ImdbId { get; set; }
        public string Rating { get; set; }
        public string Director { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string TagLine { get; set; }
        public List<string> Genres { get; set; }
    }

    [Route("/querydata/rockstaralbumindex")]
    public class QueryDataRockstarAlbumGenreIndex : QueryData<RockstarAlbumGenreGlobalIndex>
    {
        public int? Id { get; set; }
        public int? RockstarId { get; set; }
        public string Name { get; set; }
        public string Genre { get; set; }
        public int[] IdBetween { get; set; }
    }

    [Route("/querydata/rockstaralbumindex/mapped")]
    public class QueryDataRockstarAlbumGenreIndexMapped : QueryData<RockstarAlbumGenreGlobalIndex, RockstarAlbum>
    {
        public int? Id { get; set; }
        public int? RockstarId { get; set; }
        public string Name { get; set; }
        public string Genre { get; set; }
        public int[] IdBetween { get; set; }
    }

    public class RockstarAlbumGenreGlobalIndex : IGlobalIndex<RockstarAlbum>
    {
        [HashKey]
        public string Genre { get; set; }

        [RangeKey]
        public int Id { get; set; }

        public string Name { get; set; }
        public int RockstarId { get; set; }
    }

    [CacheResponse(Duration = 10, MaxAge = 10)]
    [Route("/querydata/cachemaxage/rockstars")]
    public class QueryCacheMaxAgeDataRockstars : QueryData<Rockstar>
    {
        public int? Age { get; set; }
    }

    [CacheResponse(Duration = 10, MaxAge = 0, CacheControl = CacheControl.MustRevalidate)]
    [Route("/querydata/cachemustrevalidate/rockstars")]
    public class QueryCacheMustRevalidateDataRockstars : QueryData<Rockstar>
    {
        public int? Age { get; set; }
    }

    [Route("/custom/querydata/cachemaxage/rockstars")]
    public class CustomQueryCacheMaxAgeDataRockstars : QueryData<Rockstar>
    {
        public int? Age { get; set; }
    }

    [CacheResponse(Duration = 10, MaxAge = 10)]
    public class MyCachedAutoQueryServices : Service
    {
        public IAutoQueryData AutoQuery { get; set; }

        public object Any(CustomQueryCacheMaxAgeDataRockstars query)
        {
            return AutoQuery.Execute(query, AutoQuery.CreateQuery(query, Request));
        }
    }
}