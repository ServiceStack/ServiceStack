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
        private static readonly List<EndpointAttributes> AllAttributes = (EndpointAttributeCount).Times().Map<EndpointAttributes>(x => (EndpointAttributes)(1 << (int)x));

		TestAppHost appHost;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			appHost = CreateAppHost();
		}

		public void ShouldAllowAccessWhen<TRequestDto>(EndpointAttributes withScenario)
		{
            ShouldNotThrow<UnauthorizedAccessException>(() => appHost.ExecuteService(typeof(TRequestDto).New(), withScenario));
		}

		public void ShouldDenyAccessWhen<TRequestDto>(EndpointAttributes withScenario)
		{
            ShouldThrow<UnauthorizedAccessException>(() => appHost.ExecuteService(typeof(TRequestDto).New(), withScenario));
		}

		public void ShouldDenyAccessForAllOtherScenarios<TRequestDto>(params EndpointAttributes[] notIncluding)
		{
			ShouldDenyAccessForOtherScenarios<TRequestDto>(AllAttributes.Where(x => !notIncluding.Contains(x)).ToList());
		}

		public void ShouldDenyAccessForOtherNetworkAccessScenarios<TRequestDto>(params EndpointAttributes[] notIncluding)
		{
			var scenarios = new List<EndpointAttributes> { EndpointAttributes.Localhost, EndpointAttributes.LocalSubnet, EndpointAttributes.External };
			ShouldDenyAccessForOtherScenarios<TRequestDto>(scenarios.Where(x => !notIncluding.Contains(x)).ToList());
		}

		public void ShouldDenyAccessForOtherHttpRequestTypesScenarios<TRequestDto>(params EndpointAttributes[] notIncluding)
		{
			var scenarios = new List<EndpointAttributes> { EndpointAttributes.HttpHead, EndpointAttributes.HttpGet, 
				EndpointAttributes.HttpPost, EndpointAttributes.HttpPut, EndpointAttributes.HttpDelete };
			ShouldDenyAccessForOtherScenarios<TRequestDto>(scenarios.Where(x => !notIncluding.Contains(x)).ToList());
		}

		private void ShouldDenyAccessForOtherScenarios<TRequestDto>(IEnumerable<EndpointAttributes> otherScenarios)
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
			ShouldAllowAccessWhen<InternalRestriction>(EndpointAttributes.Localhost);
			ShouldAllowAccessWhen<InternalRestriction>(EndpointAttributes.LocalSubnet);
			ShouldDenyAccessForOtherNetworkAccessScenarios<InternalRestriction>(EndpointAttributes.Localhost, EndpointAttributes.LocalSubnet);
		}

		[Test]
		public void LocalhostRestriction_allows_calls_from_localhost()
		{
			ShouldAllowAccessWhen<LocalhostRestriction>(EndpointAttributes.Localhost);
			ShouldDenyAccessForOtherNetworkAccessScenarios<LocalhostRestriction>(EndpointAttributes.Localhost);
		}

		[Test]
		public void LocalSubnetRestriction_allows_calls_from_LocalSubnet()
		{
			ShouldAllowAccessWhen<LocalSubnetRestriction>(EndpointAttributes.LocalSubnet);
			ShouldDenyAccessForOtherNetworkAccessScenarios<LocalSubnetRestriction>(EndpointAttributes.LocalSubnet);
		}

		[Test]
		public void LocalSubnetRestriction_does_not_allow_calls_from_Localhost()
		{
			ShouldDenyAccessWhen<LocalSubnetRestriction>(EndpointAttributes.Localhost);
			ShouldDenyAccessWhen<LocalSubnetRestriction>(EndpointAttributes.External);
		}

		[Test]
		public void InternalRestriction_allows_calls_from_Localhost_and_LocalSubnet()
		{
			ShouldAllowAccessWhen<InternalRestriction>(EndpointAttributes.Localhost);
			ShouldAllowAccessWhen<InternalRestriction>(EndpointAttributes.LocalSubnet);
			ShouldDenyAccessWhen<LocalSubnetRestriction>(EndpointAttributes.External);
		}

		[Test]
		public void SecureLocalSubnetRestriction_does_not_allow_partial_success()
		{
			ShouldDenyAccessWhen<SecureLocalSubnetRestriction>(EndpointAttributes.Localhost);
			ShouldDenyAccessWhen<SecureLocalSubnetRestriction>(EndpointAttributes.InSecure | EndpointAttributes.LocalSubnet);
			ShouldDenyAccessWhen<SecureLocalSubnetRestriction>(EndpointAttributes.InSecure);
			ShouldDenyAccessWhen<SecureLocalSubnetRestriction>(EndpointAttributes.Secure | EndpointAttributes.Localhost);
			ShouldAllowAccessWhen<SecureLocalSubnetRestriction>(EndpointAttributes.Secure | EndpointAttributes.LocalSubnet);

			ShouldDenyAccessWhen<SecureLocalSubnetRestriction>(EndpointAttributes.Secure | EndpointAttributes.InternalNetworkAccess);
			ShouldDenyAccessForOtherNetworkAccessScenarios<SecureLocalSubnetRestriction>(EndpointAttributes.LocalSubnet);
		}

		[Test]
		public void HttpPostXmlAndSecureLocalSubnetRestriction_does_not_allow_partial_success()
		{
			ShouldDenyAccessForOtherNetworkAccessScenarios<HttpPostXmlAndSecureLocalSubnetRestriction>(EndpointAttributes.LocalSubnet);
			ShouldDenyAccessForOtherHttpRequestTypesScenarios<HttpPostXmlAndSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost);

			ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(EndpointAttributes.Localhost);
			ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Json | EndpointAttributes.Secure | EndpointAttributes.LocalSubnet);
			ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Xml | EndpointAttributes.Secure | EndpointAttributes.Localhost);

			ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(EndpointAttributes.LocalSubnet | EndpointAttributes.Secure | EndpointAttributes.HttpHead);
			ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Xml | EndpointAttributes.InSecure );

			ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Json | EndpointAttributes.Secure | EndpointAttributes.LocalSubnet);
			ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Xml | EndpointAttributes.Secure | EndpointAttributes.Localhost);

			ShouldAllowAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Xml | EndpointAttributes.Secure | EndpointAttributes.LocalSubnet);
		}

		[Test]
		public void HttpPostXmlOrSecureLocalSubnetRestriction_does_allow_partial_success()
		{
			ShouldDenyAccessForOtherNetworkAccessScenarios<HttpPostXmlAndSecureLocalSubnetRestriction>(EndpointAttributes.LocalSubnet);

			ShouldDenyAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(EndpointAttributes.Localhost | EndpointAttributes.HttpPut);
			ShouldAllowAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Secure | EndpointAttributes.LocalSubnet);
			ShouldDenyAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Json | EndpointAttributes.Secure | EndpointAttributes.Localhost);

			ShouldAllowAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(EndpointAttributes.Secure | EndpointAttributes.LocalSubnet);
			ShouldAllowAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Xml);

			ShouldAllowAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Json | EndpointAttributes.Secure | EndpointAttributes.LocalSubnet);
			ShouldAllowAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Xml | EndpointAttributes.Secure | EndpointAttributes.Localhost);

			ShouldAllowAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Xml | EndpointAttributes.Secure | EndpointAttributes.LocalSubnet);
		}

		[Test]
		public void Can_access_from_insecure_dev_environment()
		{
			ShouldAllowAccessWhen<InSecureDevEnvironmentRestriction>(EndpointAttributes.Localhost | EndpointAttributes.InSecure | EndpointAttributes.HttpPost);
			ShouldAllowAccessWhen<InSecureDevEnvironmentRestriction>(EndpointAttributes.LocalSubnet | EndpointAttributes.InSecure | EndpointAttributes.HttpPost);
			ShouldAllowAccessWhen<InSecureDevEnvironmentRestriction>(EndpointAttributes.LocalSubnet | EndpointAttributes.InSecure | EndpointAttributes.HttpPost | EndpointAttributes.Reply);
			ShouldAllowAccessWhen<InSecureDevEnvironmentRestriction>(EndpointAttributes.LocalSubnet | EndpointAttributes.InSecure | EndpointAttributes.HttpPost | EndpointAttributes.OneWay);
		}

		[Test]
		public void Can_access_from_secure_dev_environment()
		{
			ShouldAllowAccessWhen<SecureDevEnvironmentRestriction>(EndpointAttributes.Localhost | EndpointAttributes.Secure | EndpointAttributes.HttpPost);
			ShouldAllowAccessWhen<SecureDevEnvironmentRestriction>(EndpointAttributes.LocalSubnet | EndpointAttributes.Secure | EndpointAttributes.HttpPost);
			ShouldAllowAccessWhen<SecureDevEnvironmentRestriction>(EndpointAttributes.LocalSubnet | EndpointAttributes.Secure | EndpointAttributes.HttpPost | EndpointAttributes.Reply);
			ShouldAllowAccessWhen<SecureDevEnvironmentRestriction>(EndpointAttributes.LocalSubnet | EndpointAttributes.Secure | EndpointAttributes.HttpPost | EndpointAttributes.OneWay);
		}

		[Test]
		public void Can_access_from_insecure_live_environment()
		{
			ShouldAllowAccessWhen<InSecureLiveEnvironmentRestriction>(EndpointAttributes.External | EndpointAttributes.InSecure | EndpointAttributes.HttpPost);
			ShouldAllowAccessWhen<InSecureLiveEnvironmentRestriction>(EndpointAttributes.External | EndpointAttributes.InSecure | EndpointAttributes.HttpPost | EndpointAttributes.Reply);
			ShouldAllowAccessWhen<InSecureLiveEnvironmentRestriction>(EndpointAttributes.External | EndpointAttributes.InSecure | EndpointAttributes.HttpPost | EndpointAttributes.OneWay);
		}

		[Test]
		public void Can_access_from_secure_live_environment()
		{
			ShouldAllowAccessWhen<SecureLiveEnvironmentRestriction>(EndpointAttributes.External | EndpointAttributes.Secure | EndpointAttributes.HttpPost);
			ShouldAllowAccessWhen<SecureLiveEnvironmentRestriction>(EndpointAttributes.External | EndpointAttributes.Secure | EndpointAttributes.HttpPost | EndpointAttributes.Reply);
			ShouldAllowAccessWhen<SecureLiveEnvironmentRestriction>(EndpointAttributes.External | EndpointAttributes.Secure | EndpointAttributes.HttpPost | EndpointAttributes.OneWay);
		}


		[Ignore]
		[Test]
		public void Print_enum_results()
		{
			PrintEnumResult(EndpointAttributes.InternalNetworkAccess, EndpointAttributes.Secure);
			PrintEnumResult(EndpointAttributes.InternalNetworkAccess, EndpointAttributes.Secure | EndpointAttributes.External);
			PrintEnumResult(EndpointAttributes.InternalNetworkAccess, EndpointAttributes.Secure | EndpointAttributes.Localhost);
			PrintEnumResult(EndpointAttributes.InternalNetworkAccess, EndpointAttributes.Localhost);

			PrintEnumResult(EndpointAttributes.Localhost, EndpointAttributes.Secure | EndpointAttributes.External);
			PrintEnumResult(EndpointAttributes.Localhost, EndpointAttributes.Secure | EndpointAttributes.InternalNetworkAccess);
			PrintEnumResult(EndpointAttributes.Localhost, EndpointAttributes.LocalSubnet);
			PrintEnumResult(EndpointAttributes.Localhost, EndpointAttributes.Secure);
		}

		public void PrintEnumResult(EndpointAttributes actual, EndpointAttributes required)
		{
			Console.WriteLine(string.Format("({0} | {1}): {2}", actual, required, (actual | required)));
			Console.WriteLine(string.Format("({0} & {1}): {2}", actual, required, (actual & required)));
			Console.WriteLine(string.Format("({0} ^ {1}): {2}", actual, required, (actual ^ required)));
			Console.WriteLine();
		}

	}

}