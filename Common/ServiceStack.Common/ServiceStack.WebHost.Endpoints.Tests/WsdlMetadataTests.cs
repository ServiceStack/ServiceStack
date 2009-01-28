using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.WebHost.Endpoints.Metadata;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class WsdlMetadataTests : TestBase
	{
		[Test]
		public void Wsdl_state_is_correct()
		{
			var serviceOperations = new ServiceOperations(base.AllOperations);
			var wsdlGenerator = new Soap11WsdlMetadataHandler();
			var wsdlTemplate = wsdlGenerator.GetWsdlTemplate(serviceOperations, "http://w3c.org/types", false, false);

			Assert.That(wsdlTemplate.ReplyOperationNames, Is.EquivalentTo(serviceOperations.ReplyOperations.Names));
			Assert.That(wsdlTemplate.OneWayOperationNames, Is.EquivalentTo(serviceOperations.OneWayOperations.Names));
		}
	}
}