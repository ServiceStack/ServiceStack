using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class EndpointRestrictionTests
		: ServiceHostTestBase
	{

		//Localhost and LocalSubnet is always included with the Internal flag
		private const EndpointAttributes Localhost = EndpointAttributes.Localhost | EndpointAttributes.Internal;
		private const EndpointAttributes LocalSubnet = EndpointAttributes.LocalSubnet | EndpointAttributes.Internal;
		private static readonly List<EndpointAttributes> AllAttributes = new List<EndpointAttributes>((EndpointAttributes[])Enum.GetValues(typeof(EndpointAttributes)));

		TestAppHost appHost;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			appHost = CreateAppHost();
		}

		public void ShouldAllowAccessWhen<TRequestDto>(EndpointAttributes withScenario)
			where TRequestDto : new()
		{
			ShouldNotThrow<UnauthorizedAccessException>(() => appHost.ExecuteService(new TRequestDto(), withScenario));
		}

		public void ShouldDenyAccessWhen<TRequestDto>(EndpointAttributes withScenario)
			where TRequestDto : new()
		{
			ShouldThrow<UnauthorizedAccessException>(() => appHost.ExecuteService(new TRequestDto(), withScenario));
		}

		public void ShouldDenyAccessForAllOtherScenarios<TRequestDto>(params EndpointAttributes[] notIncluding)
			where TRequestDto : new()
		{
			var otherScenarios = AllAttributes.Where(x => notIncluding.Contains(x)).ToList();

			var requestDto = new TRequestDto();
			otherScenarios.ForEach(scenario =>
				ShouldThrow<UnauthorizedAccessException>(() => appHost.ExecuteService(requestDto, scenario)));
		}



		[Test]
		public void InternalRestriction_allows_calls_from_Localhost_or_LocalSubnet()
		{
			ShouldAllowAccessWhen<InternalRestriction>(Localhost);
			ShouldAllowAccessWhen<InternalRestriction>(LocalSubnet);
			ShouldDenyAccessForAllOtherScenarios<InternalRestriction>(Localhost, LocalSubnet);
		}

		[Test]
		public void LocalhostRestriction_allows_calls_from_localhost()
		{
			ShouldAllowAccessWhen<LocalhostRestriction>(Localhost);
			ShouldDenyAccessForAllOtherScenarios<LocalhostRestriction>(Localhost);
		}

		[Test]
		public void LocalSubnetRestriction_allows_calls_from_LocalSubnet()
		{
			ShouldAllowAccessWhen<LocalSubnetRestriction>(LocalSubnet);
			ShouldDenyAccessForAllOtherScenarios<LocalSubnetRestriction>(LocalSubnet);
		}

		[Test]
		public void LocalSubnetRestriction_does_not_allow_calls_from_Localhost()
		{
			ShouldDenyAccessWhen<LocalSubnetRestriction>(Localhost);
		}

		[Test]
		public void InternalRestriction_allows_calls_from_Localhost_and_LocalSubnet()
		{
			ShouldAllowAccessWhen<InternalRestriction>(Localhost);
			ShouldAllowAccessWhen<InternalRestriction>(LocalSubnet);
		}

		[Test]
		public void SecureLocalSubnetRestriction_does_not_allow_partial_success()
		{
			ShouldDenyAccessWhen<SecureLocalSubnetRestriction>(Localhost);
			ShouldDenyAccessWhen<SecureLocalSubnetRestriction>(LocalSubnet);
			ShouldDenyAccessWhen<SecureLocalSubnetRestriction>(EndpointAttributes.Secure | Localhost);
			ShouldAllowAccessWhen<SecureLocalSubnetRestriction>(EndpointAttributes.Secure | LocalSubnet);
			
			ShouldDenyAccessWhen<SecureLocalSubnetRestriction>(EndpointAttributes.Secure | EndpointAttributes.Internal);
			ShouldDenyAccessForAllOtherScenarios<SecureLocalSubnetRestriction>();
		}

		[Test]
		public void HttpPostXmlAndSecureLocalSubnetRestriction_does_not_allow_partial_success()
		{
			ShouldDenyAccessForAllOtherScenarios<HttpPostXmlAndSecureLocalSubnetRestriction>();

			ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(Localhost);
			ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Secure | LocalSubnet);
			ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Json | EndpointAttributes.Secure | Localhost);

			ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(EndpointAttributes.Secure | LocalSubnet);
			ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Xml);

			ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Json | EndpointAttributes.Secure | LocalSubnet);
			ShouldDenyAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Xml | EndpointAttributes.Secure | Localhost);

			ShouldAllowAccessWhen<HttpPostXmlAndSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Xml | EndpointAttributes.Secure | LocalSubnet);
		}

		[Test]
		public void HttpPostXmlOrSecureLocalSubnetRestriction_does_allow_partial_success()
		{
			ShouldDenyAccessForAllOtherScenarios<HttpPostXmlOrSecureLocalSubnetRestriction>();

			ShouldDenyAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(Localhost);
			ShouldAllowAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Secure | LocalSubnet);
			ShouldDenyAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Json | EndpointAttributes.Secure | Localhost);

			ShouldAllowAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(EndpointAttributes.Secure | LocalSubnet);
			ShouldAllowAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Xml);

			ShouldAllowAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Json | EndpointAttributes.Secure | LocalSubnet);
			ShouldAllowAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Xml | EndpointAttributes.Secure | Localhost);

			ShouldAllowAccessWhen<HttpPostXmlOrSecureLocalSubnetRestriction>(EndpointAttributes.HttpPost | EndpointAttributes.Xml | EndpointAttributes.Secure | LocalSubnet);
		}


		[Ignore][Test]
		public void Print_enum_results()
		{
			PrintEnumResult(EndpointAttributes.Internal, EndpointAttributes.Secure);
			PrintEnumResult(EndpointAttributes.Internal, EndpointAttributes.Secure | EndpointAttributes.External);
			PrintEnumResult(EndpointAttributes.Internal, EndpointAttributes.Secure | EndpointAttributes.Localhost);
			PrintEnumResult(EndpointAttributes.Internal, EndpointAttributes.Localhost);

			PrintEnumResult(EndpointAttributes.Localhost, EndpointAttributes.Secure | EndpointAttributes.External);
			PrintEnumResult(EndpointAttributes.Localhost, EndpointAttributes.Secure | EndpointAttributes.Internal);
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