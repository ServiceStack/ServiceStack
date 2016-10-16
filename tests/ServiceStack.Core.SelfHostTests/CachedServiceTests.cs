using System.Reflection;
using System.Runtime.Serialization;
using Funq;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using RazorRockstars;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.Core.SelfHostTests
{
    [DataContract]
    [Route("/cached/rockstars/gateway")]
    public class CachedRockstarsGateway : IGet, IReturn<RockstarsResponse> { }

    [DataContract]
    [Route("/cached/rockstars")]
    public class CachedRockstars : IGet, IReturn<RockstarsResponse> { }

    [CacheResponse(Duration = 60 * 60, MaxAge = 30 * 60)]
    public class CachedServices : Service
    {
        public object Get(CachedRockstarsGateway request) =>
            Gateway.Send(new SearchRockstars());

        public object Get(CachedRockstars request) =>
            new RockstarsResponse {
                Total = Db.Scalar<int>("select count(*) from Rockstar"),
                Results = Db.Select<Rockstar>()
            };
    }

    [TestFixture]
    public class CachedServiceTests
    {
        private ServiceStackHost appHost;

        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(CachedServiceTests), typeof(CachedServices).GetAssembly()) {}

            public override void Configure(Container container)
            {
                container.AddSingleton<IDbConnectionFactory>(
                    new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

                using (var db = container.GetService<IDbConnectionFactory>().Open())
                {
                    db.CreateTableIfNotExists<Rockstar>();
                    db.InsertAll(RockstarsService.SeedData);
                }
            }
        }

        public CachedServiceTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.AbsoluteBaseUri);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        protected virtual IServiceClient CreateClient() => 
            new JsonServiceClient(Config.AbsoluteBaseUri);

        [Test]
        public void Can_call_cached_Service()
        {
            appHost.GetCacheClient().FlushAll();

            var client = CreateClient();

            var response = client.Get(new CachedRockstars());
            Assert.That(response.Total, Is.EqualTo(RockstarsService.SeedData.Length));
            Assert.That(response.Results.Count, Is.EqualTo(RockstarsService.SeedData.Length));
        }

        [Test]
        public void Can_call_cached_Service_via_Gateway()
        {
            appHost.GetCacheClient().FlushAll();

            var client = CreateClient();

            var response = client.Get(new CachedRockstarsGateway());
            Assert.That(response.Total, Is.EqualTo(RockstarsService.SeedData.Length));
            Assert.That(response.Results.Count, Is.EqualTo(RockstarsService.SeedData.Length));
        }

        [Test]
        public void Does_return_same_response_from_multiple_cached_calls()
        {
            appHost.GetCacheClient().FlushAll();

            var url = Config.AbsoluteBaseUri.CombineWith(new CachedRockstars().ToGetUrl());
            var originalBytes = url.GetBytesFromUrl();

            for (var i = 0; i < 3; i++)
            {
                var fromCacheBytes = url.GetBytesFromUrl();

                Assert.That(fromCacheBytes, Is.EqualTo(originalBytes));
            }
        }
    }
}