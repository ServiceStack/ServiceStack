using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.WebHost.Endpoints.Tests.Support;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;
using ServiceStack.WebHost.Endpoints.Tests.Support.Operations;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;
using ServiceStack.WebHost.Endpoints.Tests.Support.Types;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class ServiceClientTests
        : ServiceClientTestBase
    {
        /// <summary>
        /// These tests require admin privillages
        /// </summary>
        /// <returns></returns>
        public override AppHostHttpListenerBase CreateListener()
        {
            return new TestAppHostHttpListener();
        }

        private JsonServiceClient client;

        [SetUp]
        public void SetUp()
        {
            client = new JsonServiceClient(BaseUrl);
        }

        [Test]
        public void Can_GetCustomers()
        {
            var request = new GetCustomer { CustomerId = 5 };

            Send<GetCustomerResponse>(request,
                response => Assert.That(response.Customer.Id, Is.EqualTo(request.CustomerId)));
        }

        [Test]
        public void Does_add_HttpHeaders_for_Get_Sync()
        {
            client.Headers.Add("Foo", "Bar");

            var response = client.Get(new EchoRequestInfo());

            Assert.That(response.Headers["Foo"], Is.EqualTo("Bar"));
        }

        [Test]
        public void Does_allow_sending_Cached_Response()
        {
            var cache = new Dictionary<string, object>();

            client.ResultsFilter = (type, method, uri, request) =>
            {
                var cacheKey = "{0} {1}".Fmt(method, uri);
                object entry;
                cache.TryGetValue(cacheKey, out entry);
                return entry;
            };
            client.ResultsFilterResponse = (webRes, res, method, uri, request) =>
            {
                Assert.That(webRes, Is.Not.Null);
                var cacheKey = "{0} {1}".Fmt(method, uri);
                cache[cacheKey] = res;
            };

            var response1 = client.Send(new GetCustomer { CustomerId = 5 });
            var response2 = client.Send(new GetCustomer { CustomerId = 5 });
            Assert.That(response1.Created, Is.EqualTo(response2.Created));
        }

        [Test]
        public async Task Does_allow_sending_Cached_Response_Async()
        {
            var cache = new Dictionary<string, object>();

            client.ResultsFilter = (type, method, uri, request) =>
            {
                var cacheKey = "{0} {1}".Fmt(method, uri);
                object entry;
                cache.TryGetValue(cacheKey, out entry);
                return entry;
            };
            client.ResultsFilterResponse = (webRes, res, method, uri, request) =>
            {
                Assert.That(webRes, Is.Not.Null);
                var cacheKey = "{0} {1}".Fmt(method, uri);
                cache[cacheKey] = res;
            };

            var response1 = await client.SendAsync(new GetCustomer { CustomerId = 5 });
            var response2 = await client.SendAsync(new GetCustomer { CustomerId = 5 });
            Assert.That(response1.Created, Is.EqualTo(response2.Created));
        }

        [Test]
        public async Task Does_add_HttpHeaders_for_Get_Async()
        {
            client.Headers.Add("Foo", "Bar");

            var response = await client.GetAsync(new EchoRequestInfo());

            Assert.That(response.Headers["Foo"], Is.EqualTo("Bar"));
        }

        [Test]
        public async Task Does_add_HttpHeaders_in_RequestFilter_for_Get_Async()
        {
            client.RequestFilter = req => req.Headers.Add("Foo", "Bar");

            var response = await client.GetAsync(new EchoRequestInfo());

            Assert.That(response.Headers["Foo"], Is.EqualTo("Bar"));
        }

        [Test]
        public async Task Can_call_return_void()
        {
            client.Post(new ReturnsVoid { Message = "Foo" });
            Assert.That(TestAsyncService.ReturnVoidMessage, Is.EqualTo("Foo"));

            await client.PostAsync(new ReturnsVoid { Message = "Foo" });
            Assert.That(TestAsyncService.ReturnVoidMessage, Is.EqualTo("Foo"));

            using (client.Post<HttpWebResponse>(new ReturnsVoid { Message = "Bar" })) { }
            Assert.That(TestAsyncService.ReturnVoidMessage, Is.EqualTo("Bar"));
        }

        [Test]
        public async Task Can_call_return_HttpWebResponse()
        {
            client.Post(new ReturnsWebResponse { Message = "Foo" });
            Assert.That(TestAsyncService.ReturnWebResponseMessage, Is.EqualTo("Foo"));

            await client.PostAsync(new ReturnsWebResponse { Message = "Foo" });
            Assert.That(TestAsyncService.ReturnWebResponseMessage, Is.EqualTo("Foo"));

            using (client.Post<HttpWebResponse>(new ReturnsWebResponse { Message = "Bar" })) { }
            Assert.That(TestAsyncService.ReturnWebResponseMessage, Is.EqualTo("Bar"));
        }
    }

}