using System;
using System.Collections.Generic;
using System.Configuration;
using System.Xml;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.ServiceClient.Web;
using ServiceStack.SpringFactory.Support;

namespace ServiceStack.SpringFactory.Tests
{
	[TestFixture]
	public class FactoryUtilsTests 
	{
		const string ObjectsConfigXml = 
			"<objects>"
			+ "<object name=\"Soap11ServiceClient\" type=\"ServiceStack.ServiceClient.Web.Soap11ServiceClient, ServiceStack.ServiceClient.Web\">"
			+ "  <constructor-arg value=\"http://mock.org/service.svc\"/>"
			+ "</object>"
			+ "<object name=\"Soap12ServiceClient\" type=\"ServiceStack.ServiceClient.Web.Soap12ServiceClient, ServiceStack.ServiceClient.Web\">"
			+ "  <constructor-arg value=\"http://mock.org/service.svc\"/>"
			+ "</object>"
			+ "<object name=\"XmlServiceClient\" type=\"ServiceStack.ServiceClient.Web.XmlServiceClient, ServiceStack.ServiceClient.Web\">"
			+ "  <constructor-arg value=\"http://mock.org/service.svc\"/>"
			+ "</object>"
			+ "<object name=\"JsonServiceClient\" type=\"ServiceStack.ServiceClient.Web.JsonServiceClient, ServiceStack.ServiceClient.Web\">"
			+ "  <constructor-arg value=\"http://mock.org/service.svc\"/>"
			+ "</object>"
			+ "<object name=\"BasicActiveMqMessagingFactory\" type=\"ServiceStack.Messaging.ActiveMq.ActiveMqMessagingFactory, ServiceStack.Messaging.ActiveMq\">"
			+ "</object>"
			+ "<object name=\"ConfiguredActiveMqMessagingFactory\" type=\"ServiceStack.Messaging.ActiveMq.ActiveMqMessagingFactory, ServiceStack.Messaging.ActiveMq\">"
			+ "  <property name=\"ConnectionString\" value=\"tcp://wwvis7020:61616\"/>"
			+ "</object>"
			+ "<object name=\"ActiveMqMessagingFactoryWithFailoverSettings\" type=\"ServiceStack.Messaging.ActiveMq.ActiveMqMessagingFactory, ServiceStack.Messaging.ActiveMq\">"
			+ "  <property name=\"ConnectionString\" value=\"tcp://wwvis7020:61616\"/>"
			+ "  <property name=\"FailoverUri\" value=\"failover://(tcp://wwvis7020:61616,tcp://remotehost:61616)?initialReconnectDelay=100\"/>"
			+ "</object>"
			+ "<object name=\"DestinationTopic\" type=\"ServiceStack.Messaging.Destination, ServiceStack.Messaging\">"
			+ "  <constructor-arg value=\"Topic\"/>"
			+ "  <constructor-arg value=\"tcp://wwvis7020:61616/test.topic\"/>"
			+ "</object>"
			+ "<object name=\"EventLogger\" type=\"ServiceStack.Services.Logging.EventLog.EventLogger, ServiceStack.Services.Logging.EventLog\">"
			+ "  <constructor-arg value=\"ServiceStack.Services.Common.UnitTests.Utils\"/>"
			+ "  <constructor-arg value=\"Application\"/>"
			+ "</object>"
			+ "<object name=\"TypeNotExist\" type=\"ServiceStack.Services.Common.Client.TypeNotExist, ServiceStack.Services.Common\">"
			+ "</object>"
			+ "<object name=\"PropertyDoesNotExist\" type=\"ServiceStack.ServiceClient.Web.WcfServiceClient, ServiceStack.ServiceClient.Web\">"
			+ "  <property name=\"PropertyDoesNotExist\" value=\"false\"/>"
			+ "</object>"
			+ "<object name=\"PropertyNotCreatableFromString\" type=\"ServiceStack.Configuration.Tests.Support.TestType, ServiceStack.Configuration.Tests\">"
			+ "  <property name=\"CantBeCreatedFromString\" value=\"stringValue\"/>"
			+ "</object>"
			+ "<object name=\"ConstructorIndex\" type=\"ServiceStack.Messaging.Destination, ServiceStack.Messaging\">"
			+ "  <constructor-arg value=\"tcp://wwvis7020:61616/test.topic\" index=\"1\"/>"
			+ "  <constructor-arg value=\"Topic\" index=\"0\"/>"
			+ "</object>"
			+ "</objects>";

		const string InvalidConstructorIndexConfigXml = 
			"<objects>"
			+ "<object name=\"InvalidConstructorIndex\" type=\"ServiceStack.Messaging.Destination, ServiceStack.Messaging\">"
			+ "  <constructor-arg value=\"Topic\" index=\"1\"/>"
			+ "  <constructor-arg value=\"tcp://wwvis7020:61616/test.topic\" index=\"2\"/>"
			+ "</object>"
			+ "</objects>";

		private IObjectFactory factory;

		[SetUp]
		public void SetUp()
		{
			var doc = new XmlDocument();
			doc.LoadXml(ObjectsConfigXml);
			var configHandler = new ObjectsConfigurationSectionHandler();
			var objectConfigTypes = (Dictionary<string, ObjectConfigurationType>)configHandler.Create(null, null, doc.DocumentElement);
			factory = FactoryUtils.CreateObjectFactoryFromConfig(objectConfigTypes);
		}

		[Test]
		public void Create_BasicWebServiceClientTest()
		{
			var client = factory.Create<Soap11ServiceClient>("Soap11ServiceClient");
			Assert.IsNotNull(client);
			Assert.AreEqual("http://mock.org/service.svc", client.Uri);
		}

		[Test]
		public void Create_WsWebServiceClientTest()
		{
			var client = factory.Create<Soap12ServiceClient>("Soap12ServiceClient");
			Assert.IsNotNull(client);
			Assert.AreEqual("http://mock.org/service.svc", client.Uri);
		}

		[Test]
		public void Create_XmlServiceClientTest()
		{
			var client = factory.Create<XmlServiceClient>("XmlServiceClient");
			Assert.IsNotNull(client);
			Assert.AreEqual("http://mock.org/service.svc", client.BaseUri);
		}

		[Test]
		public void Create_JsonServiceClientTest()
		{
			var client = factory.Create<JsonServiceClient>("JsonServiceClient");
			Assert.IsNotNull(client);
			Assert.AreEqual("http://mock.org/service.svc", client.BaseUri);
		}

		[Test]
		public void Contains_DefinedObjectTest()
		{
			Assert.IsTrue(factory.Contains("Soap11ServiceClient"));
		}

		[Test]
		public void Contains_UndefinedObjectTest()
		{
			Assert.IsFalse(factory.Contains("UndefinedObject"));
		}

		[Test]
		[ExpectedException(typeof(ConfigurationErrorsException))]
		public void Create_IDestinationInvalidConstructorIndexTest()
		{
			var doc = new XmlDocument();
			doc.LoadXml(InvalidConstructorIndexConfigXml);
			var configHandler = new ObjectsConfigurationSectionHandler();
			var objectConfigTypes = (Dictionary<string, ObjectConfigurationType>)configHandler.Create(null, null, doc.DocumentElement);
			factory = FactoryUtils.CreateObjectFactoryFromConfig(objectConfigTypes);
			Assert.Fail();
		}

		[Test]
		public void ObjectKeyNotExistTest()
		{
			var factoryInstance = factory.Create<object>("ObjectKeyNotExist");
			Assert.That(factoryInstance, Is.Null);
		}

		[Test]
		[ExpectedException(typeof(TypeLoadException))]
		public void TypeNotExistTest()
		{
			object factoryInstance = factory.Create<object>("TypeNotExist");
			Assert.Fail();
		}

		[Test]
		[ExpectedException(typeof(TypeLoadException))]
		public void PropertyDoesNotExistTest()
		{
			var factoryInstance = factory.Create<object>("PropertyDoesNotExist");
			Assert.Fail();
		}

		[Test]
		[ExpectedException(typeof(TypeLoadException))]
		public void PropertyNotCreatableFromStringTest()
		{
			var factoryInstance = factory.Create<object>("PropertyNotCreatableFromString");
			Assert.Fail();
		}
	}
}