using System;
using System.Collections.Generic;
using System.Xml;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Configuration.Tests.Support;
using ServiceStack.ServiceClient.Web;
using ServiceStack.SpringFactory.Support;

namespace ServiceStack.SpringFactory.Tests
{
	[TestFixture]
	public class FactoryRefTests
	{
		const string objectsConfigXml = 
			@"<objects>
			<object name=""Soap11ServiceClient"" type=""ServiceStack.ServiceClient.Web.Soap11ServiceClient, ServiceStack.ServiceClient.Web"">
				<constructor-arg value=""http://mock.org/service.svc""/>
			</object>
			<object name=""Soap12ServiceClient"" type=""ServiceStack.ServiceClient.Web.Soap12ServiceClient, ServiceStack.ServiceClient.Web"">
				<constructor-arg value=""http://mock.org/service.svc""/>
			</object>
			<object name=""XmlServiceClient"" type=""ServiceStack.ServiceClient.Web.XmlServiceClient, ServiceStack.ServiceClient.Web"">
				<constructor-arg value=""http://mock.org/service.svc""/>
			</object>
			<object name=""TestGateway"" type=""ServiceStack.Configuration.Tests.Support.TestGateway, ServiceStack.Configuration.Tests"">
				<constructor-arg ref=""XmlServiceClient"" />
			</object>
			<object name=""NestedTestGateway"" type=""ServiceStack.Configuration.Tests.Support.NestedTestGateway, ServiceStack.Configuration.Tests"">
				<constructor-arg ref=""TestGateway"" />
			</object>
			<object name=""TestGatewayPropertyInjection"" type=""ServiceStack.Configuration.Tests.Support.TestGatewayPropertyInjection, ServiceStack.Configuration.Tests"">
				<property name=""ServiceClient"" ref=""XmlServiceClient"" />
			</object>
			<object name=""NestedTestGatewayPropertyInjection"" type=""ServiceStack.Configuration.Tests.Support.NestedTestGateway, ServiceStack.Configuration.Tests"">
				<property name=""TestGateway"" ref=""TestGatewayPropertyInjection"" />
			</object>
			<object name=""TestGatewayWithRefThatDoesNotExist"" type=""ServiceStack.Configuration.Tests.Support.TestGatewayPropertyInjection, ServiceStack.Configuration.Tests"">
				<property name=""ServiceClient"" ref=""RefDoesNotExist"" />
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
		public void TestGateway_with_ref_constructor()
		{
			var gateway = factory.Create<ITestGateway>("TestGateway");
			Assert.That(gateway, Is.Not.Null);
			var xmlServiceClient = (XmlServiceClient)gateway.ServiceClient;
			Assert.That(xmlServiceClient.BaseUri, Is.EqualTo("http://mock.org/service.svc"));
		}

		[Test]
		public void TestGateway_with_ref_property_injection()
		{
			var gateway = factory.Create<ITestGateway>("TestGatewayPropertyInjection");
			Assert.That(gateway, Is.Not.Null);
			var xmlServiceClient = (XmlServiceClient)gateway.ServiceClient;
			Assert.That(xmlServiceClient.BaseUri, Is.EqualTo("http://mock.org/service.svc"));
		}

		[Test]
		public void NestedTestGateway_with_ref_constructor()
		{
			var gateway = factory.Create<NestedTestGateway>("NestedTestGateway");
			Assert.That(gateway, Is.Not.Null);
			Assert.That(gateway.TestGateway, Is.Not.Null);
			var xmlServiceClient = (XmlServiceClient)gateway.TestGateway.ServiceClient;
			Assert.That(xmlServiceClient.BaseUri, Is.EqualTo("http://mock.org/service.svc"));
		}

		[Test]
		public void NestedTestGateway_with_ref_property_injection()
		{
			var gateway = factory.Create<NestedTestGateway>("NestedTestGatewayPropertyInjection");
			Assert.That(gateway, Is.Not.Null);
			Assert.That(gateway.TestGateway, Is.Not.Null);
			var xmlServiceClient = (XmlServiceClient)gateway.TestGateway.ServiceClient;
			Assert.That(xmlServiceClient.BaseUri, Is.EqualTo("http://mock.org/service.svc"));
		}

		[Test]
		[ExpectedException(typeof(TypeLoadException))]
		public void TestGateway_ref_does_not_exist()
		{
			var gateway = factory.Create<ITestGateway>("TestGatewayWithRefThatDoesNotExist");
			Assert.Fail("Should throw an exception");
		}
	}
}