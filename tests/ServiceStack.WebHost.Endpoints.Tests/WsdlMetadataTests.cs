using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.Metadata;
using ServiceStack.Testing;
using ServiceStack.WebHost.Endpoints.Tests.Support;
using ServiceStack.WebHost.Endpoints.Tests.Support.Operations;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class WsdlMetadataTests : MetadataTestBase
	{
		//private static ILog log = LogManager.GetLogger(typeof(WsdlMetadataTests));

		[Test]
		public void Wsdl_state_is_correct()
		{
            using (new BasicAppHost().Init())
            {
                var wsdlGenerator = new Soap11WsdlMetadataHandler();
                var xsdMetadata = new XsdMetadata(Metadata);
                var wsdlTemplate = wsdlGenerator.GetWsdlTemplate(xsdMetadata, "http://w3c.org/types", false, "http://w3c.org/types", "Service Name");

                Assert.That(wsdlTemplate.ReplyOperationNames, Is.EquivalentTo(xsdMetadata.GetReplyOperationNames(Format.Soap12)));
                Assert.That(wsdlTemplate.OneWayOperationNames, Is.EquivalentTo(xsdMetadata.GetOneWayOperationNames(Format.Soap12)));
            }
		}

		[Test]
		public void Xsd_output_does_not_contain_xml_declaration()
		{
			var xsd = new XsdGenerator {
				OperationTypes = new[] { typeof(GetCustomer), typeof(GetCustomerResponse), typeof(GetCustomers), typeof(GetCustomersResponse), typeof(StoreCustomer) },
				OptimizeForFlash = false,
			}.ToString();

			Assert.That(!xsd.StartsWith("<?"));
		}

	}
}