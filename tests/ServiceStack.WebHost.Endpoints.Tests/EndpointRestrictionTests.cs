using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class EndpointRestrictionTests
        : ServiceHostTestBase
    {
        //Localhost and LocalSubnet is always included with the Internal flag
        private const int EndpointAttributeCount = 17;
        private static readonly List<RequestAttributes> AllAttributes = (EndpointAttributeCount).Times().Map<RequestAttributes>(x => (RequestAttributes)(1 << (int)x));

        ServiceStackHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new TestAppHost().Init();
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        public void ShouldAllowAccessWhen<TRequestDto>(RequestAttributes withScenario)
        {
            ShouldNotThrow<UnauthorizedAccessException>(() => appHost.ExecuteService(typeof(TRequestDto).New(), withScenario));
        }

        public void ShouldDenyAccessWhen<TRequestDto>(RequestAttributes withScenario)
        {
            ShouldThrow<UnauthorizedAccessException>(() => 
                appHost.ExecuteService(typeof(TRequestDto).New(), withScenario));
        }

        public void ShouldDenyAccessForAllOtherScenarios<TRequestDto>(params RequestAttributes[] notIncluding)
        {
            ShouldDenyAccessForOtherScenarios<TRequestDto>(AllAttributes.Where(x => !notIncluding.Contains(x)).ToList());
        }

        public void ShouldDenyAccessForOtherNetworkAccessScenarios<TRequestDto>(params RequestAttributes[] notIncluding)
        {
            var scenarios = new List<RequestAttributes> { RequestAttributes.Localhost, RequestAttributes.LocalSubnet, RequestAttributes.External };
            ShouldDenyAccessForOtherScenarios<TRequestDto>(scenarios.Where(x => !notIncluding.Contains(x)).ToList());
        }

        public void ShouldDenyAccessForOtherHttpRequestTypesScenarios<TRequestDto>(params RequestAttributes[] notIncluding)
        {
            var scenarios = new List<RequestAttributes>
            {
                RequestAttributes.HttpHead,
                RequestAttributes.HttpGet,
                RequestAttributes.HttpPost,
                RequestAttributes.HttpPut,
                RequestAttributes.HttpDelete,
                RequestAttributes.HttpPatch,
                RequestAttributes.HttpOptions,
                RequestAttributes.HttpOther
            };
            ShouldDenyAccessForOtherScenarios<TRequestDto>(scenarios.Where(x => !notIncluding.Contains(x)).ToList());
        }

        private void ShouldDenyAccessForOtherScenarios<TRequestDto>(IEnumerable<RequestAttributes> otherScenarios)
        {
            var requestDto = typeof(TRequestDto).New();
            foreach (var otherScenario in otherScenarios)
            {
                try
                {
                    ShouldThrow<UnauthorizedAccessException>(() => appHost.ExecuteService(requestDto, otherScenario));
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to throw on: " + otherScenario, ex);
                }
            }
        }


        [Test]
        public void InternalRestriction_allows_calls_from_Localhost_or_LocalSubnet()
        {
            ShouldAllowAccessWhen<InternalRestriction>(RequestAttributes.Localhost);
            ShouldAllowAccessWhen<InternalRestriction>(RequestAttributes.LocalSubnet);
            ShouldDenyAccessForOtherNetworkAccessScenarios<InternalRestriction>(RequestAttributes.Localhost, RequestAttributes.LocalSubnet);
        }

        [Test]
        public void LocalhostRestriction_allows_calls_from_localhost()
        {
            ShouldAllowAccessWhen<LocalhostRestriction>(RequestAttributes.Localhost);
            ShouldAllowAccessWhen<LocalhostRestrictionOnService>(RequestAttributes.Localhost);
            ShouldDenyAccessForOtherNetworkAccessScenarios<LocalhostRestriction>(RequestAttributes.Localhost);
            ShouldDenyAccessForOtherNetworkAccessScenarios<LocalhostRestrictionOnService>(RequestAttributes.Localhost);
        }

        [Test]
        public void LocalSubnetRestriction_allows_calls_from_LocalSubnet()
        {
            ShouldAllowAccessWhen<LocalSubnetRestriction>(RequestAttributes.LocalSubnet);
            ShouldDenyAccessForOtherNetworkAccessScenarios<LocalSubnetRestriction>(RequestAttributes.LocalSubnet);
        }

        [Test]
        public void LocalSubnetRestriction_does_not_allow_calls_from_Localhost()
        {
            ShouldDenyAccessWhen<LocalSubnetRestriction>(RequestAttributes.Localhost);
            ShouldDenyAccessWhen<LocalSubnetRestriction>(RequestAttributes.External);
        }

        [Test]
        public void InternalRestriction_allows_calls_from_Localhost_and_LocalSubnet()
        {
            ShouldAllowAccessWhen<InProcessRestriction>(RequestAttributes.InProcess);
            ShouldAllowAccessWhen<InternalRestriction>(RequestAttributes.Localhost);
            ShouldAllowAccessWhen<InternalRestriction>(RequestAttributes.LocalSubnet);
            ShouldDenyAccessWhen<LocalSubnetRestriction>(RequestAttributes.External);
        }

        [Test]
        public void InProcessRestriction_does_not_allow_any_other_NetworkAccess()
        {
            ShouldAllowAccessWhen<InProcessRestriction>(RequestAttributes.InProcess);
            ShouldDenyAccessWhen<InProcessRestriction>(RequestAttributes.Localhost);
            ShouldDenyAccessWhen<InProcessRestriction>(RequestAttributes.LocalSubnet);
            ShouldDenyAccessWhen<InProcessRestriction>(RequestAttributes.External);
        }

        [Test]
        public void SecureLocalSubnetRestriction_does_not_allow_partial_success()
        {
            ShouldDenyAccessWhen<SecureLocalSubnetRestriction>(RequestAttributes.Localhost);
            ShouldDenyAccessWhen<SecureLocalSubnetRestriction>(RequestAttributes.InSecure | RequestAttributes.LocalSubnet);
            ShouldDenyAccessWhen<SecureLocalSubnetRestriction>(RequestAttributes.InSecure);
            ShouldDenyAccessWhen<SecureLocalSubnetRestriction>(RequestAttributes.Secure | RequestAttributes.Localhost);
            ShouldAllowAccessWhen<SecureLocalSubnetRestriction>(RequestAttributes.Secure | RequestAttributes.LocalSubnet);

            ShouldDenyAccessWhen<SecureLocalSubnetRestriction>(RequestAttributes.Secure | RequestAttributes.External);
            ShouldDenyAccessForOtherNetworkAccessScenarios<SecureLocalSubnetRestriction>(RequestAttributes.LocalSubnet);
        }

        [Test]
        public void HttpPostXmlAndSecureLocalSubnetRestriction_does_not_allow_partial_success()
        {
            ShouldDenyAccessForOtherNetworkAccessScenarios<HttpPostXmlAndSecureLocalSubnetRestriction>(RequestAttributes.LocalSubnet);
            ShouldDenyAccessForOtherHttpRequestTypesScenarios<HttpPostXmlAndSecureLocalSubnetRestriction>(RequestAttributes.HttpPost);

            ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(RequestAttributes.Localhost);
            ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(RequestAttributes.HttpPost | RequestAttributes.Json | RequestAttributes.Secure | RequestAttributes.LocalSubnet);
            ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(RequestAttributes.HttpPost | RequestAttributes.Xml | RequestAttributes.Secure | RequestAttributes.Localhost);

            ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(RequestAttributes.LocalSubnet | RequestAttributes.Secure | RequestAttributes.HttpHead);
            ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(RequestAttributes.HttpPost | RequestAttributes.Xml | RequestAttributes.InSecure);

            ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(RequestAttributes.HttpPost | RequestAttributes.Json | RequestAttributes.Secure | RequestAttributes.LocalSubnet);
            ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(RequestAttributes.HttpPost | RequestAttributes.Xml | RequestAttributes.Secure | RequestAttributes.Localhost);

            ShouldAllowAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(RequestAttributes.HttpPost | RequestAttributes.Xml | RequestAttributes.Secure | RequestAttributes.LocalSubnet);
        }

        [Test]
        public void HttpPostXmlOrSecureLocalSubnetRestriction_does_allow_partial_success()
        {
            ShouldDenyAccessForOtherNetworkAccessScenarios<HttpPostXmlAndSecureLocalSubnetRestriction>(RequestAttributes.LocalSubnet);

            ShouldDenyAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(RequestAttributes.Localhost | RequestAttributes.HttpPut);
            ShouldAllowAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(RequestAttributes.HttpPost | RequestAttributes.Secure | RequestAttributes.LocalSubnet);
            ShouldDenyAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(RequestAttributes.HttpPost | RequestAttributes.Json | RequestAttributes.Secure | RequestAttributes.Localhost);

            ShouldAllowAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(RequestAttributes.Secure | RequestAttributes.LocalSubnet);
            ShouldAllowAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(RequestAttributes.HttpPost | RequestAttributes.Xml);

            ShouldAllowAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(RequestAttributes.HttpPost | RequestAttributes.Json | RequestAttributes.Secure | RequestAttributes.LocalSubnet);
            ShouldAllowAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(RequestAttributes.HttpPost | RequestAttributes.Xml | RequestAttributes.Secure | RequestAttributes.Localhost);

            ShouldAllowAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(RequestAttributes.HttpPost | RequestAttributes.Xml | RequestAttributes.Secure | RequestAttributes.LocalSubnet);
        }

        [Test]
        public void Can_access_from_insecure_dev_environment()
        {
            ShouldAllowAccessWhen<InSecureDevEnvironmentRestriction>(RequestAttributes.Localhost | RequestAttributes.InSecure | RequestAttributes.HttpPost);
            ShouldAllowAccessWhen<InSecureDevEnvironmentRestriction>(RequestAttributes.LocalSubnet | RequestAttributes.InSecure | RequestAttributes.HttpPost);
            ShouldAllowAccessWhen<InSecureDevEnvironmentRestriction>(RequestAttributes.LocalSubnet | RequestAttributes.InSecure | RequestAttributes.HttpPost | RequestAttributes.Reply);
            ShouldAllowAccessWhen<InSecureDevEnvironmentRestriction>(RequestAttributes.LocalSubnet | RequestAttributes.InSecure | RequestAttributes.HttpPost | RequestAttributes.OneWay);
        }

        [Test]
        public void Can_access_from_secure_dev_environment()
        {
            ShouldAllowAccessWhen<SecureDevEnvironmentRestriction>(RequestAttributes.Localhost | RequestAttributes.Secure | RequestAttributes.HttpPost);
            ShouldAllowAccessWhen<SecureDevEnvironmentRestriction>(RequestAttributes.LocalSubnet | RequestAttributes.Secure | RequestAttributes.HttpPost);
            ShouldAllowAccessWhen<SecureDevEnvironmentRestriction>(RequestAttributes.LocalSubnet | RequestAttributes.Secure | RequestAttributes.HttpPost | RequestAttributes.Reply);
            ShouldAllowAccessWhen<SecureDevEnvironmentRestriction>(RequestAttributes.LocalSubnet | RequestAttributes.Secure | RequestAttributes.HttpPost | RequestAttributes.OneWay);
        }

        [Test]
        public void Can_access_from_insecure_live_environment()
        {
            ShouldAllowAccessWhen<InSecureLiveEnvironmentRestriction>(RequestAttributes.External | RequestAttributes.InSecure | RequestAttributes.HttpPost);
            ShouldAllowAccessWhen<InSecureLiveEnvironmentRestriction>(RequestAttributes.External | RequestAttributes.InSecure | RequestAttributes.HttpPost | RequestAttributes.Reply);
            ShouldAllowAccessWhen<InSecureLiveEnvironmentRestriction>(RequestAttributes.External | RequestAttributes.InSecure | RequestAttributes.HttpPost | RequestAttributes.OneWay);
        }

        [Test]
        public void Can_access_from_secure_live_environment()
        {
            ShouldAllowAccessWhen<SecureLiveEnvironmentRestriction>(RequestAttributes.External | RequestAttributes.Secure | RequestAttributes.HttpPost);
            ShouldAllowAccessWhen<SecureLiveEnvironmentRestriction>(RequestAttributes.External | RequestAttributes.Secure | RequestAttributes.HttpPost | RequestAttributes.Reply);
            ShouldAllowAccessWhen<SecureLiveEnvironmentRestriction>(RequestAttributes.External | RequestAttributes.Secure | RequestAttributes.HttpPost | RequestAttributes.OneWay);
        }

        [Test]
        public void Can_access_MessageQueueRestriction_from_MQ()
        {
            ShouldAllowAccessWhen<MessageQueueRestriction>(RequestAttributes.Localhost | RequestAttributes.MessageQueue | RequestAttributes.HttpPost);
        }

        [Test]
        public void Can_not_access_MessageQueueRestriction_from_HTTP()
        {
            ShouldDenyAccessWhen<MessageQueueRestriction>(RequestAttributes.Localhost | RequestAttributes.Http | RequestAttributes.HttpPost);
        }

        [Ignore("TODO: Ignore reason")]
        [Test]
        public void Print_enum_results()
        {
            PrintEnumResult(RequestAttributes.InternalNetworkAccess, RequestAttributes.Secure);
            PrintEnumResult(RequestAttributes.InternalNetworkAccess, RequestAttributes.Secure | RequestAttributes.External);
            PrintEnumResult(RequestAttributes.InternalNetworkAccess, RequestAttributes.Secure | RequestAttributes.Localhost);
            PrintEnumResult(RequestAttributes.InternalNetworkAccess, RequestAttributes.Localhost);

            PrintEnumResult(RequestAttributes.Localhost, RequestAttributes.Secure | RequestAttributes.External);
            PrintEnumResult(RequestAttributes.Localhost, RequestAttributes.Secure | RequestAttributes.InternalNetworkAccess);
            PrintEnumResult(RequestAttributes.Localhost, RequestAttributes.LocalSubnet);
            PrintEnumResult(RequestAttributes.Localhost, RequestAttributes.Secure);
        }

        public void PrintEnumResult(RequestAttributes actual, RequestAttributes required)
        {
            $"({actual} | {required}): {actual | required}".Print();
            $"({actual} & {required}): {actual & required}".Print();
            $"({actual} ^ {required}): {actual ^ required}".Print();
            "".Print();
        }

        [Test]
        public void Enum_masks_are_correct()
        {
            const RequestAttributes network = RequestAttributes.Localhost | RequestAttributes.LocalSubnet | RequestAttributes.External;
            Assert.That((network.ToAllowedFlagsSet() & network) == network);

            const RequestAttributes security = RequestAttributes.Secure | RequestAttributes.InSecure;
            Assert.That((security.ToAllowedFlagsSet() & security) == security);

            const RequestAttributes method =
                RequestAttributes.HttpHead | RequestAttributes.HttpGet | RequestAttributes.HttpPost |
                RequestAttributes.HttpPut | RequestAttributes.HttpDelete | RequestAttributes.HttpPatch |
                RequestAttributes.HttpOptions | RequestAttributes.HttpOther;
            Assert.That((method.ToAllowedFlagsSet() & method) == method);

            const RequestAttributes call = RequestAttributes.OneWay | RequestAttributes.Reply;
            Assert.That((call.ToAllowedFlagsSet() & call) == call);

            const RequestAttributes format =
                RequestAttributes.Soap11 | RequestAttributes.Soap12 | RequestAttributes.Xml | RequestAttributes.Json |
                RequestAttributes.Jsv | RequestAttributes.ProtoBuf | RequestAttributes.Csv | RequestAttributes.Html |
                RequestAttributes.Wire | RequestAttributes.MsgPack | RequestAttributes.FormatOther;
            Assert.That((format.ToAllowedFlagsSet() & format) == format);

            const RequestAttributes endpoint =
                RequestAttributes.Http | RequestAttributes.MessageQueue | RequestAttributes.Tcp |
                RequestAttributes.EndpointOther;
            Assert.That((endpoint.ToAllowedFlagsSet() & endpoint) == endpoint);
        }
    }

}