using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/test")]
    public class DummyRequest : IReturn<MockResponse> { }

    public class DummyFallback : IReturn<MockResponse> { }

    [Route("/testsend")]
    public class DummySendGet : IReturn<MockResponse>, IGet { }

    public class MockResponse
    {
        public string Url { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class JsonServiceClientResolverTests : ServiceClientResolverTestsBase
    {
        protected override IServiceClient CreateClient(string baseUrl, UrlResolverDelegate urlResolver = null,
            TypedUrlResolverDelegate typedUrlResolver = null)
        {
            return new JsonServiceClient(baseUrl)
            {
                UrlResolver = urlResolver,
                TypedUrlResolver = typedUrlResolver,
                ResultsFilter = (type, method, uri, request) =>
                    new MockResponse { Url = uri }
            };
        }
    }

    public class JsonHttpClientResolverTests : ServiceClientResolverTestsBase
    {
        protected override IServiceClient CreateClient(string baseUrl, UrlResolverDelegate urlResolver = null,
            TypedUrlResolverDelegate typedUrlResolver = null)
        {
            return new JsonHttpClient(baseUrl)
            {
                UrlResolver = urlResolver,
                TypedUrlResolver = typedUrlResolver,
                ResultsFilter = (type, method, uri, request) =>
                    new MockResponse { Url = uri }
            };
        }
    }

    public abstract class ServiceClientResolverTestsBase
    {
        protected abstract IServiceClient CreateClient(string baseUrl,
            UrlResolverDelegate urlResolver = null, TypedUrlResolverDelegate typedUrlResolver = null);

        [Test]
        public void Can_Change_RawUrls_with_UrlResolver()
        {
            var client = CreateClient("http://example.org/api", urlResolver:
                (meta, httpMethod, url) => meta.BaseUri.Replace("example.org", "111.111.111.111").CombineWith(url));

            var response = client.Get<MockResponse>("/dummy");
            Assert.That(response.Url, Is.EqualTo("http://111.111.111.111/api/dummy"));

            response = client.Post<MockResponse>("/dummy", new DummyRequest());
            Assert.That(response.Url, Is.EqualTo("http://111.111.111.111/api/dummy"));

            response = client.Send(new DummyRequest());
            Assert.That(response.Url, Is.EqualTo("http://111.111.111.111/api/json/reply/DummyRequest"));
        }

        [Test]
        public async Task Can_Change_RawUrls_with_UrlResolver_Async()
        {
            var client = CreateClient("http://example.org/api", urlResolver:
                (meta, httpMethod, url) => meta.BaseUri.Replace("example.org", "111.111.111.111").CombineWith(url));

            var response = await client.DeleteAsync<MockResponse>("/dummy");
            Assert.That(response.Url, Is.EqualTo("http://111.111.111.111/api/dummy"));

            response = await client.PutAsync<MockResponse>("/dummy", new DummyRequest());
            Assert.That(response.Url, Is.EqualTo("http://111.111.111.111/api/dummy"));

            response = await client.SendAsync(new DummyRequest());
            Assert.That(response.Url, Is.EqualTo("http://111.111.111.111/api/json/reply/DummyRequest"));
        }

        [Test]
        public void Can_Change_Typed_Urls_with_TypedUrlResolver()
        {
            var client = CreateClient("http://example.org/api", typedUrlResolver:
                (meta, httpMethod, dto) => meta.BaseUri.Replace("example.org", dto.GetType().Name + ".example.org")
                        .CombineWith(dto.ToUrl(httpMethod, meta.Format)));

            var response = client.Get(new DummyRequest());
            Assert.That(response.Url, Is.EqualTo("http://DummyRequest.example.org/api/test"));

            response = client.Send(new DummySendGet());
            Assert.That(response.Url, Is.EqualTo("http://DummySendGet.example.org/api/testsend"));

            response = client.Get(new DummyFallback());
            Assert.That(response.Url, Is.EqualTo("http://DummyFallback.example.org/api/json/reply/DummyFallback"));

            response = client.Post(new DummyRequest());
            Assert.That(response.Url, Is.EqualTo("http://DummyRequest.example.org/api/test"));
        }

        [Test]
        public async Task Can_Change_Typed_Urls_with_TypedUrlResolver_Async()
        {
            var client = CreateClient("http://example.org/api", typedUrlResolver:
                (meta, httpMethod, dto) => meta.BaseUri.Replace("example.org", dto.GetType().Name + ".example.org")
                        .CombineWith(dto.ToUrl(httpMethod, meta.Format)));

            var response = await client.DeleteAsync(new DummyRequest());
            Assert.That(response.Url, Is.EqualTo("http://DummyRequest.example.org/api/test"));

            response = await client.SendAsync(new DummySendGet());
            Assert.That(response.Url, Is.EqualTo("http://DummySendGet.example.org/api/testsend"));

            response = await client.DeleteAsync(new DummyFallback());
            Assert.That(response.Url, Is.EqualTo("http://DummyFallback.example.org/api/json/reply/DummyFallback"));

            response = await client.PutAsync(new DummyRequest());
            Assert.That(response.Url, Is.EqualTo("http://DummyRequest.example.org/api/test"));
        }
    }

}