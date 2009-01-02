using System.Collections.Generic;
using System.Xml;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Configuration.Support;
using ServiceStack.Configuration.Tests.Support;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceInterface;
using ServiceStack.Service;

namespace ServiceStack.Configuration.Tests
{
	[TestFixture]
	public class FactoryProviderTests
	{
		const string objectsConfigXml = 
		@"<objects>
			<object name=""XmlServiceClient"" type=""ServiceStack.ServiceClient.Web.XmlServiceClient, ServiceStack.ServiceClient.Web"">
				<constructor-arg value=""http://mock.org/service.svc""/>
			</object>
			<object name=""TestGateway"" type=""ServiceStack.Configuration.Tests.Support.TestGateway, ServiceStack.Configuration.Tests"">
				<constructor-arg ref=""XmlServiceClient"" />
			</object>
		</objects>";

		private IObjectFactory factory;

		[SetUp]
		public void SetUp()
		{
			var doc = new XmlDocument();
			doc.LoadXml(objectsConfigXml);
			var configHandler = new ObjectsConfigurationSectionHandler();
			var objectConfigTypes = (Dictionary<string, ObjectConfigurationType>)configHandler.Create(null, null, doc.DocumentElement);
			factory = FactoryUtils.CreateObjectFactoryFromConfig(objectConfigTypes);
		}

		[Test]
		public void A_registered_provider_is_resolvable_by_an_interface_type()
		{
			var gateway = new TestGateway(new XmlServiceClient("http://mock.org/service.svc"));
			var provider = new FactoryProvider(null, gateway);
			var resolvedGateway = provider.Resolve<ITestGateway>();
			Assert.That(resolvedGateway, Is.Not.Null);
			var xmlServiceClient = (XmlServiceClient) resolvedGateway.ServiceClient;
			Assert.That(xmlServiceClient.BaseUri, Is.EqualTo("http://mock.org/service.svc"));
		}
	}
}