using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Logging;
using ServiceStack.WebHost.Endpoints.Metadata;
using ServiceStack.WebHost.Endpoints.Tests.Support.Operations;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class WsdlMetadataTests : TestBase
	{
		private static ILog log = LogManager.GetLogger(typeof(WsdlMetadataTests));

		[Test]
		public void Wsdl_state_is_correct()
		{
			var serviceOperations = new ServiceOperations(base.AllOperations);
			var wsdlGenerator = new Soap11WsdlMetadataHandler();
			var wsdlTemplate = wsdlGenerator.GetWsdlTemplate(serviceOperations, "http://w3c.org/types", false, false);

			Assert.That(wsdlTemplate.ReplyOperationNames, Is.EquivalentTo(serviceOperations.ReplyOperations.Names));
			Assert.That(wsdlTemplate.OneWayOperationNames, Is.EquivalentTo(serviceOperations.OneWayOperations.Names));
		}

		[Test]
		public void Xsd_output_does_not_contain_xml_declaration()
		{
			var xsd = new XsdGenerator {
				OperationTypes = new[] { typeof(GetCustomer), typeof(GetCustomerResponse), typeof(GetCustomers), typeof(GetCustomersResponse), typeof(StoreCustomer) },
				OptimizeForFlash = false,
				IncludeAllTypesInAssembly = false,
			}.ToString();
			
			Assert.That(!xsd.StartsWith("<?"));			
		}

	}
}