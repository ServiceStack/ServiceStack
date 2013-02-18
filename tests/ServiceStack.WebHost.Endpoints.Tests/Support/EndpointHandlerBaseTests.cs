using System;
using System.Collections;
using System.Linq;
using System.Net.NetworkInformation;
using Moq;
using NUnit.Framework;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.WebHost.Endpoints.Support.Tests
{
    [TestFixture]
    public class EndpointHandlerBaseTests
    {
        class TestHandler : EndpointHandlerBase
        {
            public override object CreateRequest(ServiceHost.IHttpRequest request, string operationName)
            {
                throw new NotImplementedException();
            }

            public override object GetResponse(IHttpRequest httpReq, IHttpResponse httpRes, object request)
            {
                throw new NotImplementedException();
            }
        }

        [Test, TestCaseSource(typeof(EndpointHandlerBaseTests), "EndpointExpectations")]
        public void GetEndpointAttributes_AcceptsUserHostAddressFormats(string format, EndpointAttributes expected)
        {
            var handler = new TestHandler();
            var request = new Mock<IHttpRequest>();
            request.Expect(req => req.UserHostAddress).Returns(format);
            request.Expect(req => req.IsSecureConnection).Returns(false);
            request.Expect(req => req.HttpMethod).Returns("GET");

            Assert.AreEqual(expected | EndpointAttributes.HttpGet | EndpointAttributes.InSecure, request.Object.GetAttributes());
        }

        public static IEnumerable EndpointExpectations
        {
            get
            {
                var ipv6Addresses = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(nic => nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .SelectMany(nic => nic.GetIPProperties()
                        .UnicastAddresses.Select(unicast => unicast.Address))
                        .Where(address => address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6).ToList();

                //this covers all the different flavors of ipv6 address -- scoped, link local, etc
                foreach (var address in ipv6Addresses)
                {
                    yield return new TestCaseData(address.ToString(), EndpointAttributes.LocalSubnet);
                    yield return new TestCaseData("[" + address + "]:57", EndpointAttributes.LocalSubnet);
                    // HttpListener Format w/Port
                    yield return new TestCaseData("[{0}]:8080".Fmt(address), EndpointAttributes.LocalSubnet);
                }

                yield return new TestCaseData("fe80::100:7f:fffe%10", EndpointAttributes.LocalSubnet);
                yield return new TestCaseData("[fe80::100:7f:fffe%10]:57", EndpointAttributes.LocalSubnet);
                yield return new TestCaseData("[fe80::100:7f:fffe%10]:8080", EndpointAttributes.LocalSubnet);

                //ipv6 loopback
                yield return new TestCaseData("::1", EndpointAttributes.Localhost);
                yield return new TestCaseData("[::1]:83", EndpointAttributes.Localhost);

                //ipv4
                yield return new TestCaseData("192.168.100.2", EndpointAttributes.External);
                yield return new TestCaseData("192.168.100.2:47", EndpointAttributes.External);

                //ipv4 loopback
                yield return new TestCaseData("127.0.0.1", EndpointAttributes.Localhost);
                yield return new TestCaseData("127.0.0.1:20", EndpointAttributes.Localhost);

                //ipv4 in X-FORWARDED-FOR HTTP Header format
                yield return new TestCaseData("192.168.100.2, 192.168.0.1", EndpointAttributes.External);
                yield return new TestCaseData("192.168.100.2, 192.168.0.1, 10.1.1.1", EndpointAttributes.External);
            }
        }
    }
}