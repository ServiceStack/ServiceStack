using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/testcontenttype")]
    public class TestContentType : IReturn<TestContentType>
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TestContentTypeService : Service
    {
        public object Any(TestContentType request)
        {
            return request;
        }
    }

    [TestFixture]
    public class ContentTypeTests
    {
        private const string ListeningOn = "http://localhost:1337/";

        ExampleAppHostHttpListener appHost;
        readonly JsonServiceClient client = new(ListeningOn);

        [OneTimeSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new ExampleAppHostHttpListener();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [OneTimeTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Does_return_JSON()
        {
            var json = ListeningOn.AppendPath("testcontenttype")
                .AddQueryParam("id", 1)
                .GetStringFromUrl(accept: MimeTypes.Json,
                    responseFilter: res =>
                        Assert.That(res.MatchesContentType(MimeTypes.Json)));

            var dto = json.FromJson<TestContentType>();
            Assert.That(dto.Id, Is.EqualTo(1));
        }

        [Test]
        public void Does_return_JSON_UpperCase()
        {
            var json = ListeningOn.AppendPath("testcontenttype")
                .AddQueryParam("id", 1)
                .GetStringFromUrl(accept: MimeTypes.Json.ToUpper(),
                    responseFilter: res =>
                        Assert.That(res.MatchesContentType(MimeTypes.Json)));

            var dto = json.FromJson<TestContentType>();
            Assert.That(dto.Id, Is.EqualTo(1));
        }

        [Test]
        public void Does_return_JSON_extension()
        {
            var json = ListeningOn.AppendPath("testcontenttype.json")
                .AddQueryParam("id", 1)
                .GetStringFromUrl(responseFilter: res =>
                        Assert.That(res.MatchesContentType(MimeTypes.Json)));

            var dto = json.FromJson<TestContentType>();
            Assert.That(dto.Id, Is.EqualTo(1));
        }

        [Test]
        public void Does_return_JSON_format()
        {
            var json = ListeningOn.AppendPath("testcontenttype")
                .AddQueryParam("id", 1)
                .AddQueryParam("format", "json")
                .GetStringFromUrl(responseFilter: res =>
                        Assert.That(res.MatchesContentType(MimeTypes.Json)));

            var dto = json.FromJson<TestContentType>();
            Assert.That(dto.Id, Is.EqualTo(1));
        }

        [Test]
        public void Can_call_JSON_Service_with_UTF8_BOM()
        {
            var dto = new TestContentType { Id = 1, Name = "Foo" };
            var json = dto.ToJson();
            var jsonBytes = json.ToUtf8Bytes();

            var bytes = new List<byte>(new byte[] { 0xEF, 0xBB, 0xBF });
            bytes.AddRange(jsonBytes);

            var mergedBytes = bytes.ToArray();

            var responseBytes = ListeningOn.AppendPath("testcontenttype")
                .PostBytesToUrl(mergedBytes, contentType: MimeTypes.Json);

            var responseJson = responseBytes.FromUtf8Bytes();
            var fromJson = responseJson.FromJson<TestContentType>();
            
            Assert.That(fromJson.Id, Is.EqualTo(dto.Id));
            Assert.That(fromJson.Name, Is.EqualTo(dto.Name));
        }

        [Test]
        public void Can_get_custom_json_format()
        {
            var json = ListeningOn.AppendPath("testcontenttype")
                .AddQueryParam("format", "x-custom+json")
                .GetStringFromUrl(responseFilter: res =>
                    Assert.That(res.MatchesContentType("application/x-custom+json")));

            Assert.That(json, Is.EqualTo("{\"custom\":\"json\"}"));
        }
    }
}