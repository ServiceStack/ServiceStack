using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [Route("/request/{Id}")]
    public class UniqueRequest
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
        public string Get(UniqueRequest uniqueRequest)
        {
            return uniqueRequest.Id;
        }

        public object Any(Collections request)
        {
            return request;
        }
    }

    [TestFixture]
    public class UniqueRequestTests
    {
        private const string BaseUri = Config.ServiceStackBaseUri;

        [Test]
        [Ignore("ASP.NET does not allow invalid chars see http://stackoverflow.com/questions/13691829/path-parameters-w-url-unfriendly-characters")]
        public void Can_handle_encoded_chars()
        {
            var response = BaseUri.CombineWith("request/123%20456").GetStringFromUrl();
            Assert.That(response, Is.EqualTo("123%20456"));
            response = BaseUri.CombineWith("request/123%7C456").GetStringFromUrl();
            Assert.That(response, Is.EqualTo("123%7C456"));
        }

        [Test]
        public void Can_handle_collections_with_ServiceClient()
        {
            var client = new JsonServiceClient(BaseUri);
            var request = new Collections
            {
                Ids = new[] { 1, 2, 3 },
                Names = new List<string> { "A", "B", "C" },
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
