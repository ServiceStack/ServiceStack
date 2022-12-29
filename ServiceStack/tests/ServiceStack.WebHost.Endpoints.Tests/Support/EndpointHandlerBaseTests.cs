using System;
using System.Collections;
using System.Linq;
using System.Net.NetworkInformation;
using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Support.Tests
{
    [TestFixture]
    public class EndpointHandlerBaseTests
    {
        private ServiceStackHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new BasicAppHost().Init();
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        class TestHandler : ServiceStackHandlerBase
        {
        }

        [Test, TestCaseSource(typeof(EndpointHandlerBaseTests), "EndpointExpectations")]
        public void GetEndpointAttributes_AcceptsUserHostAddressFormats(string format, RequestAttributes expected)
        {
            var handler = new TestHandler();
            var request = new BasicRequest {
                UserHostAddress = format,
                IsSecureConnection = false,
                Verb = "GET"
            };

            Assert.AreEqual(expected | RequestAttributes.HttpGet | RequestAttributes.InSecure, request.GetAttributes());
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
                    yield return new TestCaseData(address.ToString(), RequestAttributes.LocalSubnet);
                    yield return new TestCaseData("[" + address + "]:57", RequestAttributes.LocalSubnet);
                    // HttpListener Format w/Port
                    yield return new TestCaseData("[{0}]:8080".Fmt(address), RequestAttributes.LocalSubnet);
                }

                yield return new TestCaseData("fe80::100:7f:fffe%10", RequestAttributes.LocalSubnet);
                yield return new TestCaseData("[fe80::100:7f:fffe%10]:57", RequestAttributes.LocalSubnet);
                yield return new TestCaseData("[fe80::100:7f:fffe%10]:8080", RequestAttributes.LocalSubnet);

                //ipv6 loopback
                yield return new TestCaseData("::1", RequestAttributes.Localhost);
                yield return new TestCaseData("[::1]:83", RequestAttributes.Localhost);

                //ipv4
                yield return new TestCaseData("192.168.100.2", RequestAttributes.External);
                yield return new TestCaseData("192.168.100.2:47", RequestAttributes.External);

                //ipv4 loopback
                yield return new TestCaseData("127.0.0.1", RequestAttributes.Localhost);
                yield return new TestCaseData("127.0.0.1:20", RequestAttributes.Localhost);

                //ipv4 in X-FORWARDED-FOR HTTP Header format
                yield return new TestCaseData("192.168.100.2, 192.168.0.1", RequestAttributes.External);
                yield return new TestCaseData("192.168.100.2, 192.168.0.1, 10.1.1.1", RequestAttributes.External);
            }
        }
    }
}