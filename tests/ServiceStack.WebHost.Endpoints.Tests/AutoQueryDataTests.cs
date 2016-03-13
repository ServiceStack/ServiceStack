using System;
using System.Runtime.Serialization;
using Funq;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class AutoQueryDataAppHost : AppSelfHostBase
    {
        public AutoQueryDataAppHost()
            : base("AutoQuerData", typeof(AutoQueryService).Assembly) {}

        public override void Configure(Container container)
        {
            Plugins.Add(new AutoQueryDataFeature()
                .AddDataSource(ctx => new QueryDataSource<Rockstar>(ctx, GetRockstars())));
        }

        public static Rockstar[] GetRockstars()
        {
            return new[] {
                new Rockstar { Id = 1, FirstName = "Jimi", LastName = "Hendrix", LivingStatus = LivingStatus.Dead, Age = 27, DateOfBirth = new DateTime(1942, 11, 27), DateDied = new DateTime(1970, 09, 18), },
                new Rockstar { Id = 2, FirstName = "Jim", LastName = "Morrison", Age = 27, LivingStatus = LivingStatus.Dead, DateOfBirth = new DateTime(1943, 12, 08), DateDied = new DateTime(1971, 07, 03),  },
                new Rockstar { Id = 3, FirstName = "Kurt", LastName = "Cobain", Age = 27, LivingStatus = LivingStatus.Dead, DateOfBirth = new DateTime(1967, 02, 20), DateDied = new DateTime(1994, 04, 05), },
                new Rockstar { Id = 4, FirstName = "Elvis", LastName = "Presley", Age = 42, LivingStatus = LivingStatus.Dead, DateOfBirth = new DateTime(1935, 01, 08), DateDied = new DateTime(1977, 08, 16), },
                new Rockstar { Id = 5, FirstName = "David", LastName = "Grohl", Age = 44, LivingStatus = LivingStatus.Alive, DateOfBirth = new DateTime(1969, 01, 14), },
                new Rockstar { Id = 6, FirstName = "Eddie", LastName = "Vedder", Age = 48, LivingStatus = LivingStatus.Alive, DateOfBirth = new DateTime(1964, 12, 23), },
                new Rockstar { Id = 7, FirstName = "Michael", LastName = "Jackson", Age = 50, LivingStatus = LivingStatus.Dead, DateOfBirth = new DateTime(1958, 08, 29), DateDied = new DateTime(2009, 06, 05), },
            };
        }
    }

    [Route("/querydata/rockstars")]
    public class QueryDataRockstars : QueryData<Rockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryDataOverridedRockstars : QueryData<Rockstar>
    {
        public int? Age { get; set; }
    }

    [DataContract]
    [Route("/adhocdata-rockstars")]
    public class QueryDataAdhocRockstars : QueryData<Rockstar>
    {
        [DataMember(Name = "first_name")]
        public string FirstName { get; set; }
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
    }

    [TestFixture]
    public class AutoQueryDataTests
    {
        private readonly ServiceStackHost appHost;
        public IServiceClient client;

        private static readonly int TotalRockstars = AutoQueryAppHost.SeedRockstars.Length;
        private static readonly int TotalAlbums = AutoQueryAppHost.SeedAlbums.Length;

        public AutoQueryDataTests()
        {
            appHost = new AutoQueryDataAppHost()
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
    }
}