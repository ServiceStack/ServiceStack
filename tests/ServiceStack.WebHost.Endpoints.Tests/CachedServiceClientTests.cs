using System;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class CachedJsonServiceClientTests : CachedServiceClientTests
    {
        protected override ICachedServiceClient GetCachedServiceClient()
        {
            return new CachedServiceClient(new JsonServiceClient(Config.ListeningOn));
        }
    }

    public class CachedJsonHttpClientTests : CachedServiceClientTests
    {
        protected override ICachedServiceClient GetCachedServiceClient()
        {
            return new CachedHttpClient(new JsonHttpClient(Config.ListeningOn));
        }
    }

    [TestFixture]
    public abstract class CachedServiceClientTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(typeof(CacheServerFeatureTests).Name, typeof(CacheEtagServices).Assembly)
            { }

            public override void Configure(Container container) { }
        }

        private readonly ServiceStackHost appHost;
        protected CachedServiceClientTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        protected abstract ICachedServiceClient GetCachedServiceClient();

        [Test]
        public void CachedServiceClient_does_return_cached_ETag_Requests_when_MustRevalidate()
        {
            var client = GetCachedServiceClient();

            var request = new SetCache { ETag = "etag", CacheControl = CacheControl.MustRevalidate };

            var response = client.Get(request);
            Assert.That(client.CacheHits, Is.EqualTo(0));
            Assert.That(response, Is.EqualTo(request));

            response = client.Get(request);
            Assert.That(client.CacheHits, Is.EqualTo(1));
            Assert.That(response, Is.EqualTo(request));
        }

        [Test]
        public async Task CachedServiceClient_does_return_cached_ETag_Requests_Async()
        {
            var client = GetCachedServiceClient();

            var request = new SetCache { ETag = "etag", CacheControl = CacheControl.MustRevalidate };

            var response = await client.GetAsync(request);
            Assert.That(client.CacheHits, Is.EqualTo(0));
            Assert.That(response, Is.EqualTo(request));

            response = await client.GetAsync(request);
            Assert.That(client.CacheHits, Is.EqualTo(1));
            Assert.That(response, Is.EqualTo(request));
        }

        [Test]
        public void CachedServiceClient_does_return_cached_ETag_Requests_using_URL()
        {
            var client = GetCachedServiceClient();

            var requestUrl = Config.ListeningOn.CombineWith("set-cache?etag=etag");

            var response = client.Get<SetCache>(requestUrl);
            Assert.That(client.CacheHits, Is.EqualTo(0));
            Assert.That(response.ETag, Is.EqualTo("etag"));

            response = client.Get<SetCache>(requestUrl);
            Assert.That(client.CacheHits, Is.EqualTo(1));
            Assert.That(response.ETag, Is.EqualTo("etag"));
        }

        [Test]
        public void CachedServiceClient_does_return_cached_LastModified_Requests()
        {
            var client = GetCachedServiceClient();

            var request = new SetCache { LastModified = new DateTime(2016, 1, 1, 0, 0, 0) };

            var response = client.Get(request);
            Assert.That(client.CacheHits, Is.EqualTo(0));
            Assert.That(response, Is.EqualTo(request));

            response = client.Get(request);
            Assert.That(client.CacheHits, Is.EqualTo(1));
            Assert.That(response, Is.EqualTo(request));
        }

        [Test]
        public void CachedServiceClient_does_return_cached_LastModified_Requests_using_URL()
        {
            var client = GetCachedServiceClient();

            var requestUrl = Config.ListeningOn.CombineWith("set-cache?lastModified=2016-01-01");

            var response = client.Get<SetCache>(requestUrl);
            Assert.That(client.CacheHits, Is.EqualTo(0));
            Assert.That(response.LastModified, Is.EqualTo(new DateTime(2016, 1, 1, 0, 0, 0)));

            response = client.Get<SetCache>(requestUrl);
            Assert.That(client.CacheHits, Is.EqualTo(1));
            Assert.That(response.LastModified, Is.EqualTo(new DateTime(2016, 1, 1, 0, 0, 0)));
        }

        [Test]
        public async Task CachedServiceClient_does_return_cached_LastModified_Requests_using_URL_Async()
        {
            var client = GetCachedServiceClient();

            var requestUrl = Config.ListeningOn.CombineWith("set-cache?lastModified=2016-01-01");

            var response = await client.GetAsync<SetCache>(requestUrl);
            Assert.That(client.CacheHits, Is.EqualTo(0));
            Assert.That(response.LastModified, Is.EqualTo(new DateTime(2016, 1, 1, 0, 0, 0)));

            response = await client.GetAsync<SetCache>(requestUrl);
            Assert.That(client.CacheHits, Is.EqualTo(1));
            Assert.That(response.LastModified, Is.EqualTo(new DateTime(2016, 1, 1, 0, 0, 0)));
        }

        [Test]
        public void CachedServiceClient_does_return_cached_ToOptimizedResults()
        {
            var client = GetCachedServiceClient();

            var request = new CachedRequest { Age = TimeSpan.FromHours(1) };
            var response = client.Get(request);
            Assert.That(client.NotModifiedHits, Is.EqualTo(0));
            Assert.That(response, Is.EqualTo(request));

            response = client.Get(request);
            Assert.That(client.NotModifiedHits, Is.EqualTo(1));
            Assert.That(response, Is.EqualTo(request));
        }

        [Test]
        public async Task CachedServiceClient_does_return_cached_ToOptimizedResults_Async()
        {
            var client = GetCachedServiceClient();

            var request = new CachedRequest { Age = TimeSpan.FromHours(1) };
            var response = await client.GetAsync(request);
            Assert.That(client.NotModifiedHits, Is.EqualTo(0));
            Assert.That(response, Is.EqualTo(request));

            response = await client.GetAsync(request);
            Assert.That(client.NotModifiedHits, Is.EqualTo(1));
            Assert.That(response, Is.EqualTo(request));
        }

        [Test]
        public void CachedServiceClient_does_return_cached_after_FailedResponse()
        {
            var client = GetCachedServiceClient();
            FailsAfterOnce.Count = 0;

            var request = new FailsAfterOnce { ETag = "etag", MaxAge = TimeSpan.FromSeconds(0) };
            var response = client.Get(request);
            Assert.That(client.ErrorFallbackHits, Is.EqualTo(0));
            Assert.That(response, Is.EqualTo(request));

            response = client.Get(request);
            Assert.That(client.ErrorFallbackHits, Is.EqualTo(1));
            Assert.That(response, Is.EqualTo(request));
        }

        [Test]
        public async Task CachedServiceClient_does_return_cached_after_FailedResponse_Async()
        {
            var client = GetCachedServiceClient();
            FailsAfterOnce.Count = 0;

            var request = new FailsAfterOnce { ETag = "etag", MaxAge = TimeSpan.FromSeconds(0) };
            var response = await client.GetAsync(request);
            Assert.That(client.ErrorFallbackHits, Is.EqualTo(0));
            Assert.That(response, Is.EqualTo(request));

            response = await client.GetAsync(request);
            Assert.That(client.ErrorFallbackHits, Is.EqualTo(1));
            Assert.That(response, Is.EqualTo(request));
        }

        [Test]
        public void CachedServiceClient_does_not_return_NoCache_after_FailedResponse()
        {
            var client = GetCachedServiceClient();
            FailsAfterOnce.Count = 0;

            var request = new FailsAfterOnce { ETag = "etag", CacheControl = CacheControl.NoCache };
            var response = client.Get(request);
            Assert.That(client.ErrorFallbackHits, Is.EqualTo(0));
            Assert.That(response, Is.EqualTo(request));

            try
            {
                client.Get(request);
                Assert.Fail("Should throw");
            }
            catch (Exception) {}
        }
    }
}