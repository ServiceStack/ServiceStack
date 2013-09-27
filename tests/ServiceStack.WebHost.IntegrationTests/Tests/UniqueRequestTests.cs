using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [Route("/request/{Id}")]
    public class UniqueRequest
    {
        public string Id { get; set; }
    }

    public class UniqueRequestService : IService
    {
        public string Get(UniqueRequest uniqueRequest)
        {
            return uniqueRequest.Id;
        }
    }

    [TestFixture]
    public class UniqueRequestTests
    {
        private const string BaseUri = Config.AbsoluteBaseUri;

        [Test]
        [Explicit("ASP.NET does not allow invalid chars see http://stackoverflow.com/questions/13691829/path-parameters-w-url-unfriendly-characters")]
        public void Can_handle_encoded_chars()
        {
            var response = BaseUri.CombineWith("request/123%20456").GetStringFromUrl();
            Assert.That(response, Is.EqualTo("123%20456"));
            response = BaseUri.CombineWith("request/123%7C456").GetStringFromUrl();
            Assert.That(response, Is.EqualTo("123%7C456"));
        }

    }
}
