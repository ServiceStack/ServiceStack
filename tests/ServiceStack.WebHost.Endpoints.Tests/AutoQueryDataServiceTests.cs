using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class AutoQueryDataServiceTests : AutoQueryDataTests
    {
        public override ServiceStackHost CreateAppHost()
        {
            return new AutoQueryDataServiceAppHost();
        }

        [Test]
        public void Can_call_overidden_AutoQueryData_Service_with_custom_MemorySource()
        {
            var response = client.Get(new GetAllRockstarGenresData());
            Assert.That(response.Total, Is.EqualTo(AutoQueryDataAppHost.SeedGenres.Length));
            Assert.That(response.Results.Count, Is.EqualTo(AutoQueryDataAppHost.SeedGenres.Length));

            response = client.Get(new GetAllRockstarGenresData { Name = "Grunge" });
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].RockstarId, Is.EqualTo(3));
        }
    }

    public class AutoQueryDataServiceAppHost : AutoQueryDataAppHost
    {
        public override void Configure(Container container)
        {
            base.Configure(container);

            var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
            container.Register<IDbConnectionFactory>(dbFactory);

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.DropAndCreateTable<Rockstar>();
                db.DropAndCreateTable<RockstarAlbum>();
                db.DropAndCreateTable<Adhoc>();
                db.DropAndCreateTable<Movie>();
                db.DropAndCreateTable<AllFields>();
                db.DropAndCreateTable<PagingTest>();
                db.InsertAll(SeedRockstars);
                db.InsertAll(SeedAlbums);
                db.InsertAll(SeedAdhoc);
                db.InsertAll(SeedMovies);
                db.InsertAll(SeedAllFields);
                db.InsertAll(SeedPagingTest);
            }

            var feature = this.GetPlugin<AutoQueryDataFeature>();
            feature.AddDataSource(ctx => ctx.ServiceSource<Rockstar>(new GetAllRockstarData()));
            feature.AddDataSource(ctx => ctx.ServiceSource<RockstarAlbum>(new GetAllRockstarAlbumsData()));
            feature.AddDataSource(ctx => ctx.ServiceSource<Adhoc>(new GetAllAdhocData()));
            feature.AddDataSource(ctx => ctx.ServiceSource<Movie>(new GetAllMoviesData()));
            feature.AddDataSource(ctx => ctx.ServiceSource<AllFields>(new GetAllFieldsData()));
            feature.AddDataSource(ctx => ctx.ServiceSource<PagingTest>(new GetAllPagingTestData()));
        }
    }

    //No IReturn<T> -> List<Movie>
    public class GetAllRockstarData {}

    //IReturn<T> -> List<RockstarAlbum>
    public class GetAllRockstarAlbumsData : IReturn<List<RockstarAlbum>> {}

    //Response DTO
    public class GetAllAdhocData : IReturn<GetAllAdhocDataResponse> {}
    public class GetAllAdhocDataResponse
    {
        public DateTime Created { get; set; }
        public List<Adhoc> Results { get; set; } 
    }

    //GET No IReturn<T> Task Response
    public class GetAllMoviesData {}

    //Response DTO Task Response
    public class GetAllFieldsData : IReturn<GetAllFieldsDataResponse> { }
    public class GetAllFieldsDataResponse
    {
        public DateTime Created { get; set; }
        public List<AllFields> Results { get; set; } 
    }

    //GET 
    public class GetAllPagingTestData {}

    public class DataQueryServices : Service
    {
        public object Any(GetAllRockstarData request)
        {
            return Db.Select<Rockstar>();
        }

        public object Any(GetAllRockstarAlbumsData request)
        {
            return Db.Select<RockstarAlbum>();
        }

        public object Any(GetAllAdhocData request)
        {
            return new GetAllAdhocDataResponse
            {
                Results = Db.Select<Adhoc>()
            };
        }

        public Task Any(GetAllMoviesData request)
        {
            return Task.FromResult(Db.Select<Movie>());
        }

        public Task Get(GetAllFieldsData request)
        {
            return Task.FromResult(new GetAllFieldsDataResponse
            {
                Created = DateTime.UtcNow,
                Results = Db.Select<AllFields>(),
            });
        }

        public List<PagingTest> Get(GetAllPagingTestData request)
    public class GetAllRockstarGenresData : QueryData<RockstarGenre>
    {
        public string Name { get; set; }
    }

    public class CustomDataQueryServices : Service
    {
        public IAutoQueryData AutoQuery { get; set; }

        public object Any(GetAllRockstarGenresData requestDto)
        {
            var memorySource = new MemoryDataSource<RockstarGenre>(Db.Select<RockstarGenre>(), requestDto, Request);
            var q = AutoQuery.CreateQuery(requestDto, Request, memorySource);
            return AutoQuery.Execute(requestDto, q);
        }
    }
}