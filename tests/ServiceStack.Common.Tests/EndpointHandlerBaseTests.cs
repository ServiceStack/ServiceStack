using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class EndpointHandlerBaseTests
    {
        public IHttpRequest CreateRequest(string userHostAddress)
        {
            var httpReq = new MockHttpRequest("test", HttpMethods.Get, ContentType.Json, "/", null, null, null) {
                UserHostAddress = userHostAddress
            };
            return httpReq;
        }

        [Test]
        public void Can_parse_Ips()
        {
            var result = CreateRequest("204.2.145.235").GetAttributes();

            Assert.That(result.Has(EndpointAttributes.External));
            Assert.That(result.Has(EndpointAttributes.HttpGet));
            Assert.That(result.Has(EndpointAttributes.InSecure));
        }
    }
}