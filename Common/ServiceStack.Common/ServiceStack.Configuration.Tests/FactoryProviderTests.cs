using System.Collections.Generic;
using System.Xml;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Configuration;
using ServiceStack.Configuration.Tests.Support;
using ServiceStack.Configuration.Tests.Support.Crypto;
using ServiceStack.DataAccess.Db4oProvider;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceInterface;
using ServiceStack.DataAccess;
using ServiceStack.Service;
using ServiceStack.SpringFactory.Support;

namespace ServiceStack.SpringFactory.Tests
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

		private IObjectFactory factoryConfig;

		[SetUp]
		public void SetUp()
		{
			var doc = new XmlDocument();
			doc.LoadXml(objectsConfigXml);
			var configHandler = new ObjectsConfigurationSectionHandler();
			var objectConfigTypes = (Dictionary<string, ObjectConfigurationType>)configHandler.Create(null, null, doc.DocumentElement);
			this.factoryConfig = FactoryUtils.CreateObjectFactoryFromConfig(objectConfigTypes);
		}

		[Test]
		public void A_config_provider_is_resolvable_by_type()
		{
			var provider = new FactoryProvider(factoryConfig);
			var resolvedGateway = provider.Resolve<TestGateway>();
			Assert.That(resolvedGateway, Is.Not.Null);
			var xmlServiceClient = (XmlServiceClient)resolvedGateway.ServiceClient;
			Assert.That(xmlServiceClient.BaseUri, Is.EqualTo("http://mock.org/service.svc"));
		}

		[Ignore("Needs to be implemented")]
		[Test]
		public void A_config_provider_is_resolvable_by_an_interface_type()
		{
			var provider = new FactoryProvider(factoryConfig);
			var resolvedGateway = provider.Resolve<ITestGateway>();
			Assert.That(resolvedGateway, Is.Not.Null);
			var xmlServiceClient = (XmlServiceClient)resolvedGateway.ServiceClient;
			Assert.That(xmlServiceClient.BaseUri, Is.EqualTo("http://mock.org/service.svc"));
		}

		[Test]
		public void A_registered_provider_is_resolvable_by_an_interface_type()
		{
			var gateway = new TestGateway(new XmlServiceClient("http://mock.org/service.svc"));
			var provider = new FactoryProvider(gateway);
			var resolvedGateway = provider.Resolve<ITestGateway>();
			Assert.That(resolvedGateway, Is.Not.Null);
			var xmlServiceClient = (XmlServiceClient)resolvedGateway.ServiceClient;
			Assert.That(xmlServiceClient.BaseUri, Is.EqualTo("http://mock.org/service.svc"));
		}

		[Test]
		public void A_registered_db4o_provider_manager_can_be_resolved()
		{
			var db4oProvider = new Db4OFileProviderManager("test.db4o");
			var factory = new FactoryProvider(this.factoryConfig);
			factory.Register(db4oProvider);
			var provider = factory.Resolve<IPersistenceProviderManager>();
		}

		[Test]
		public void A_RsaPrivateKey_can_be_created_and_configured_in_code()
		{
			var privateKey = new RsaPrivateKey(ConfigUtils.GetAppSetting("ServerPrivateKey"));
			var factory = new FactoryProvider(this.factoryConfig);
			factory.Register(privateKey);
			var resolvedPrivateKey = factory.Resolve<RsaPrivateKey>();
			Assert.That(resolvedPrivateKey, Is.Not.Null);
		}

		[Test]
		public void A_non_existant_provider_returns_null()
		{
			var factory = new FactoryProvider(this.factoryConfig);
			var config = factory.Resolve<IResourceManager>();
			Assert.That(config, Is.Null);
		}
	}
}