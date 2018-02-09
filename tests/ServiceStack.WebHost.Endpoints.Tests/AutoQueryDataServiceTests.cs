using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support;

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
            var response = client.Get(new GetAllRockstarGenresData { Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(AutoQueryDataAppHost.SeedGenres.Length));
            Assert.That(response.Results.Count, Is.EqualTo(AutoQueryDataAppHost.SeedGenres.Length));

            response = client.Get(new GetAllRockstarGenresData { Name = "Grunge" });
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].RockstarId, Is.EqualTo(3));
        }

        [Test]
        public void Does_Cache_third_party_api_ServiceSource()
        {
            GetGithubRepos.ApiCalls = 0;
            QueryResponse<GithubRepo> response;

            response = client.Get(new QueryGitHubRepos { Organization = "ServiceStack" });
            Assert.That(response.Results.Count, Is.GreaterThan(20));
            Assert.That(GetGithubRepos.ApiCalls, Is.EqualTo(1));

            response = client.Get(new QueryGitHubRepos { Organization = "ServiceStack",
                NameStartsWith = "ServiceStack",
                Fields = "Name,Description,Homepage,Language,Watchers_Count",
                OrderByDesc = "Watchers_Count"
            });
            Assert.That(response.Results.Count, Is.LessThan(20));
            Assert.That(response.Results.All(x => x.Id == 0));
            Assert.That(response.Results.All(x => x.Name != null));
            Assert.That(response.Results[0].Watchers_Count, Is.GreaterThan(3000));
            Assert.That(GetGithubRepos.ApiCalls, Is.EqualTo(1));

            response = client.Get(new QueryGitHubRepos { User = "mythz" });
            Assert.That(response.Results.Count, Is.GreaterThan(20));
            Assert.That(GetGithubRepos.ApiCalls, Is.EqualTo(2));

            response = client.Get(new QueryGitHubRepos { User = "mythz",
                DescriptionContains = "101 LINQ Samples",
                Take = 4,
                OrderBy = "Name",
            });
            Assert.That(response.Results.Count, Is.EqualTo(4));
            Assert.That(GetGithubRepos.ApiCalls, Is.EqualTo(2));
        }

        [Test]
        public void Does_Cache_MemorySource()
        {
            QueryResponse<GithubContributor> response;

            response = client.Get(new QueryServiceStackContributors
            {
                Repo = "ServiceStack",
                Take = 20,
                Include = "Total"
            });
            Assert.That(response.Total, Is.GreaterThan(20));
            Assert.That(response.Results.Count, Is.EqualTo(20));
            Assert.That(HostContext.LocalCache.Get<List<GithubContributor>>("aqd:" + typeof(GithubContributor).Name) != null);

            response = client.Get(new QueryServiceStackContributors
            {
                Repo = "ServiceStack",
                ContributionsAbove = 10,
                Fields = "Login,Contributions"
            });
            Assert.That(response.Results.Count, Is.GreaterThan(10));
            Assert.That(response.Results.All(c => c.Contributions > 10));
            Assert.That(response.Results.All(c => c.Login != null));
            Assert.That(response.Results.All(c => c.Id == 0));
            Assert.That(response.Results.All(c => c.Type == null));
        }
    }

    public class AutoQueryDataServiceAppHost : AutoQueryDataAppHost
    {
        public override void Configure(Container container)
        {
#if NET45
            //https://githubengineering.com/crypto-removal-notice/
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
#endif

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
                db.DropAndCreateTable<RockstarGenre>();
                db.InsertAll(SeedRockstars);
                db.InsertAll(SeedAlbums);
                db.InsertAll(SeedAdhoc);
                db.InsertAll(SeedMovies);
                db.InsertAll(SeedAllFields);
                db.InsertAll(SeedPagingTest);
                db.InsertAll(SeedGenres);
            }

            var feature = this.GetPlugin<AutoQueryDataFeature>();
            feature.AddDataSource(ctx => ctx.ServiceSource<Rockstar>(new GetAllRockstarData()));
            feature.AddDataSource(ctx => ctx.ServiceSource<RockstarAlbum>(ctx.Dto.ConvertTo<GetAllRockstarAlbumsData>()));
            feature.AddDataSource(ctx => ctx.ServiceSource<Adhoc>(new GetAllAdhocData()));
            feature.AddDataSource(ctx => ctx.ServiceSource<Movie>(new GetAllMoviesData()));
            feature.AddDataSource(ctx => ctx.ServiceSource<AllFields>(new GetAllFieldsData()));
            feature.AddDataSource(ctx => ctx.ServiceSource<PagingTest>(new GetAllPagingTestData()));
            feature.AddDataSource(ctx => ctx.ServiceSource<PagingTest>(new GetAllPagingTestData()));
            feature.AddDataSource(ctx => ctx.ServiceSource<GithubRepo>(ctx.Dto.ConvertTo<GetGithubRepos>(), 
                HostContext.Cache, TimeSpan.FromMinutes(1)));
            feature.AddDataSource(ctx => ctx.MemorySource(
                () => "https://api.github.com/repos/ServiceStack/{0}/contributors"
                         .Fmt(ctx.Request.GetParam("repo"))
                    .GetJsonFromUrl(req => req.SetUserAgent("AutoQuery")).FromJson<List<GithubContributor>>(),
                HostContext.LocalCache, TimeSpan.FromMinutes(1)));
        }
    }

    //No IReturn<T> -> List<Movie>
    public class GetAllRockstarData {}

    //IReturn<T> -> List<RockstarAlbum>
    public class GetAllRockstarAlbumsData : IReturn<List<RockstarAlbum>>
    {
        public int? Id { get; set; }
        public int? RockstarId { get; set; }
        public string Name { get; set; }
        public string Genre { get; set; }
        public int[] IdBetween { get; set; }
    }

    //Response DTO
    public class GetAllAdhocData : IReturn<GetAllAdhocDataResponse> { }
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
    public class GetAllPagingTestData { }

    public class QueryGitHubRepos : QueryData<GithubRepo>
    {
        public string User { get; set; }
        public string Organization { get; set; }

        public string NameStartsWith { get; set; }
        public string DescriptionContains { get; set; }
        public int? Watchers_Count { get; set; }
    }

    public class GetGithubRepos : IReturn<List<GithubRepo>>
    {
        public static int ApiCalls = 0;

        public string User { get; set; }
        public string Organization { get; set; }
    }

    public class QueryServiceStackContributors : QueryData<GithubContributor>
    {
        public string Repo { get; set; }
        public int? ContributionsAbove { get; set; }
    }

    public class DataQueryServices : Service
    {
        public object Any(GetAllRockstarData request)
        {
            return Db.Select<Rockstar>().AsTaskResult();
        }

        public object Any(GetAllRockstarAlbumsData request)
        {
            var q = Db.From<RockstarAlbum>();

            if (request.IdBetween != null)
                q.Where(x => x.Id >= request.IdBetween[0] && x.Id <= request.IdBetween[1]);

            if (request.Name != null)
                q.Where(x => x.Name == request.Name);

            var results = Db.Select(q);
            return results;
        }

        public object Any(GetAllAdhocData request)
        {
            return new GetAllAdhocDataResponse
            {
                Results = Db.Select<Adhoc>()
            };
        }

        public object Any(GetAllMoviesData request)
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

        public Task<List<PagingTest>> Get(GetAllPagingTestData request)
        {
            return Task.FromResult(Db.Select<PagingTest>());
        }

        public object Get(GetGithubRepos request)
        {
            if (request.User == null && request.Organization == null)
                throw new ArgumentNullException("User");

            var url = request.User != null
                ? "https://api.github.com/users/{0}/repos".Fmt(request.User)
                : "https://api.github.com/orgs/{0}/repos".Fmt(request.Organization);

            Interlocked.Increment(ref GetGithubRepos.ApiCalls);

            return url.GetJsonFromUrl(requestFilter:req => req.SetUserAgent(GetType().Name))
                .FromJson<List<GithubRepo>>();
        }
    }

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