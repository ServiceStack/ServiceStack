using System.Collections.Generic;
using System.IO;
using Funq;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Text;
#if !NETCORE_SUPPORT
using ServiceStack.MiniProfiler.UI;
#endif

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/request/{Id}")]
    public class ById
    {
        public string Id { get; set; }
    }

    [Route("/collections")]
    public class Collections : IReturn<Collections>
    {
        public int[] Ids { get; set; }
        public List<string> Names { get; set; }
    }

    public class UniqueRequestService : IService
    {
        public string Get(ById byId)
        {
            return byId.Id;
        }

        public object Any(Collections request)
        {
            return request;
        }
    }

    public class UniqueRequestAppHost : AppHostHttpListenerBase
    {
        public UniqueRequestAppHost() : base("Unique Request Tests", typeof(UniqueRequestService).GetAssembly()) {}
        public override void Configure(Container container) {}
    }

    [TestFixture]
    public class UniqueRequestTests
    {
        private const string BaseUri = "http://localhost:8001";
        private UniqueRequestAppHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new UniqueRequestAppHost();
            appHost.Init();
            appHost.Start(BaseUri + "/");
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_handle_encoded_chars()
        {
            var response = BaseUri.CombineWith("request/123%20456").GetStringFromUrl();
            Assert.That(response, Is.EqualTo("123 456"));
            response = BaseUri.CombineWith("request/123%7C456").GetStringFromUrl();
            Assert.That(response, Is.EqualTo("123|456"));
        }

        [Test]
        public void Can_handle_collections_with_ServiceClient()
        {
            var client = new JsonServiceClient(BaseUri);
            var request = new Collections {
                Ids = new[] {1, 2, 3}, 
                Names = new List<string> {"A", "B", "C"},
            };
            var response = client.Get(request);

            Assert.That(response.Ids, Is.EquivalentTo(request.Ids));
            Assert.That(response.Names, Is.EquivalentTo(request.Names));
        }

        [Test]
        public void Can_handle_collections_with_HttpClient()
        {
            var url = BaseUri.CombineWith("collections")
                .AddQueryParam("Ids", "1,2,3")
                .AddQueryParam("Names", "A,B,C");

            var response = url.GetJsonFromUrl()
                .FromJson<Collections>();

            Assert.That(response.Ids, Is.EquivalentTo(new[] { 1, 2, 3 }));
            Assert.That(response.Names, Is.EquivalentTo(new List<string> { "A", "B", "C" }));

            url = BaseUri.CombineWith("collections")
                .AddQueryParam("Ids", "1")
                .AddQueryParam("Ids", "2")
                .AddQueryParam("Ids", "3")
                .AddQueryParam("Names", "A")
                .AddQueryParam("Names", "B")
                .AddQueryParam("Names", "C");

            response = url.GetJsonFromUrl()
                .FromJson<Collections>();

            Assert.That(response.Ids, Is.EquivalentTo(new[] { 1, 2, 3 }));
            Assert.That(response.Names, Is.EquivalentTo(new List<string> { "A", "B", "C" }));
        }

        [Test]
        public void Can_handle_collections_with_HttpClient_on_predefined_route()
        {
            var url = BaseUri.CombineWith("json/reply/Collections")
                .AddQueryParam("Ids", "1,2,3")
                .AddQueryParam("Names", "A,B,C");

            var response = url.GetJsonFromUrl()
                .FromJson<Collections>();

            Assert.That(response.Ids, Is.EquivalentTo(new[] { 1, 2, 3 }));
            Assert.That(response.Names, Is.EquivalentTo(new List<string> { "A", "B", "C" }));

            url = BaseUri.CombineWith("json/reply/Collections")
                .AddQueryParam("Ids", "1")
                .AddQueryParam("Ids", "2")
                .AddQueryParam("Ids", "3")
                .AddQueryParam("Names", "A")
                .AddQueryParam("Names", "B")
                .AddQueryParam("Names", "C");

            response = url.GetJsonFromUrl()
                .FromJson<Collections>();

            Assert.That(response.Ids, Is.EquivalentTo(new[] { 1, 2, 3 }));
            Assert.That(response.Names, Is.EquivalentTo(new List<string> { "A", "B", "C" }));
        }
    }
}
