using System;
using NUnit.Framework;
using ServiceStack.Server;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class EndpointHandlerBaseTests
    {
        public IHttpRequest CreateRequest(string userHostAddress)
        {
            var httpReq = new MockHttpRequest("test", HttpMethods.Get, MimeTypes.Json, "/", null, null, null)
            {
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

        [Flags]
        enum A : int { B = 0, C = 2, D = 4 }

        [Test]
        public void Can_parse_int_enums()
        {
            var result = A.B | A.C;
            Assert.That(result.Has(A.C));
            Assert.That(!result.Has(A.D));
        }
    }
}