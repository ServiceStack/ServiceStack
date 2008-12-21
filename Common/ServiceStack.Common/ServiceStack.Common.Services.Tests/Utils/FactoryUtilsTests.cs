using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel;
using System.Xml;
using ServiceStack.Common.Services.Config;
using ServiceStack.Common.Services.Support.Config;
using ServiceStack.Common.Services.Tests.Support;
using ServiceStack.Common.Services.Utils;
using ServiceStack.Common.Wcf;
using ServiceStack.Messaging;
using NUnit.Framework;

namespace ServiceStack.Common.Services.Tests.Utils
{
    [TestFixture]
    public class FactoryUtilsTests : TestBase
    {
        const string objectsConfigXml = "<objects>"
            + "<object name=\"BasicWebServiceClient\" type=\"ServiceStack.Common.Wcf.WebServiceClient, ServiceStack.Common.Wcf\">"
            + "  <property name=\"UseBasicHttpBinding\" value=\"true\"/>"
            + "  <property name=\"Uri\" value=\"http://mock.org/service.svc\"/>"
            + "</object>"
            + "<object name=\"WsWebServiceClient\" type=\"ServiceStack.Common.Wcf.WebServiceClient, ServiceStack.Common.Wcf\">"
            + "  <property name=\"UseBasicHttpBinding\" value=\"false\"/>"
            + "  <property name=\"Uri\" value=\"http://mock.org/service.svc\"/>"
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
            + "<object name=\"PropertyDoesNotExist\" type=\"ServiceStack.Common.Wcf.WebServiceClient, ServiceStack.Common.Wcf\">"
            + "  <property name=\"PropertyDoesNotExist\" value=\"false\"/>"
            + "</object>"
            + "<object name=\"PropertyNotCreatableFromString\" type=\"ServiceStack.Common.Wcf.WebServiceClient, ServiceStack.Common.Wcf\">"
            + "  <property name=\"Binding\" value=\"stringValue\"/>"
            + "</object>"
            + "<object name=\"ConstructorIndex\" type=\"ServiceStack.Messaging.Destination, ServiceStack.Messaging\">"
            + "  <constructor-arg value=\"tcp://wwvis7020:61616/test.topic\" index=\"1\"/>"
            + "  <constructor-arg value=\"Topic\" index=\"0\"/>"
            + "</object>"
            + "</objects>";

        const string invalidConstructorIndexConfigXml = "<objects>"
            + "<object name=\"InvalidConstructorIndex\" type=\"ServiceStack.Messaging.Destination, ServiceStack.Messaging\">"
            + "  <constructor-arg value=\"Topic\" index=\"1\"/>"
            + "  <constructor-arg value=\"tcp://wwvis7020:61616/test.topic\" index=\"2\"/>"
            + "</object>"
            + "</objects>";

        private const string BROKER_URI = "tcp://wwvis7020:61616";

        private IObjectFactory factory;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            var doc = new XmlDocument();
            doc.LoadXml(objectsConfigXml);
            var configHandler = new ObjectsConfigurationSectionHandler();
            var objectConfigTypes = (Dictionary<string, ObjectConfigurationType>)configHandler.Create(null, null, doc.DocumentElement);
            factory = FactoryUtils.CreateObjectFactoryFromConfig(objectConfigTypes);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        [Test]
        public void Create_BasicWebServiceClientTest()
        {
            var client = factory.Create<WebServiceClient>("BasicWebServiceClient");
            Assert.IsNotNull(client);
            Assert.AreEqual(typeof(BasicHttpBinding), client.Binding.GetType());
            Assert.AreEqual("http://mock.org/service.svc", client.Uri);
        }

        [Test]
        public void Create_WsWebServiceClientTest()
        {
            var client = factory.Create<WebServiceClient>("WsWebServiceClient");
            Assert.IsNotNull(client);
            Assert.AreEqual(typeof(WSHttpBinding), client.Binding.GetType());
            Assert.AreEqual("http://mock.org/service.svc", client.Uri);
        }

        [Test]
        public void Create_IConnectionTest()
        {
            var messagingFactory = factory.Create<IMessagingFactory>("BasicActiveMqMessagingFactory");
            Assert.IsNotNull(messagingFactory);
            var connection = messagingFactory.CreateConnection(BROKER_URI);
            Assert.IsNotNull(connection);
        }

        [Test]
        public void Create_IDestinationTest()
        {
            var destination = factory.Create<IDestination>("DestinationTopic");
            Assert.IsNotNull(destination);
            Assert.AreEqual(DestinationType.Topic, destination.DestinationType);
            Assert.AreEqual("tcp://wwvis7020:61616/test.topic", destination.Uri);
        }

        [Test]
        public void Create_IGatewayClientTest()
        {
            var messagingFactory = factory.Create<IMessagingFactory>("BasicActiveMqMessagingFactory");
            using (IConnection connection = messagingFactory.CreateConnection(BROKER_URI))
            {
                var destination = factory.Create<IDestination>("DestinationTopic");
                using (IGatewayClient client = connection.CreateClient(destination))
                {
                    Assert.IsNotNull(client);
                }
            }
        }

        [Test]
        public void Create_IGatewayClientFromConfiguredFactoryTest()
        {
            var messagingFactory = factory.Create<IMessagingFactory>("ConfiguredActiveMqMessagingFactory");
            using (var connection = messagingFactory.CreateConnection())
            {
                var destination = factory.Create<IDestination>("DestinationTopic");
                using (var client = connection.CreateClient(destination))
                {
                    Assert.IsNotNull(client);
                }
            }
        }

        [Test]
        public void Create_IGatewayClientFromFactoryWithFailoverSettingsTest()
        {
            var messagingFactory = factory.Create<IMessagingFactory>("ActiveMqMessagingFactoryWithFailoverSettings");
            using (var connection = messagingFactory.CreateConnection())
            {
                var destination = factory.Create<IDestination>("DestinationTopic");
                using (var client = connection.CreateClient(destination))
                {
                    Assert.IsNotNull(client);
                    Assert.AreEqual(2, client.FailoverSettings.BrokerUris.Count);
                    Assert.AreEqual(client.FailoverSettings.BrokerUris[0], "tcp://wwvis7020:61616");
                    Assert.AreEqual(client.FailoverSettings.BrokerUris[1], "tcp://remotehost:61616");
                    Assert.AreEqual(client.FailoverSettings.InitialReconnectDelay, TimeSpan.FromMilliseconds(100));
                }
            }
        }

        [Test]
        public void Create_IDestinationConstructorIndexTest()
        {
            var destination = factory.Create<IDestination>("ConstructorIndex");
            Assert.IsNotNull(destination);
            Assert.AreEqual(DestinationType.Topic, destination.DestinationType);
            Assert.AreEqual("tcp://wwvis7020:61616/test.topic", destination.Uri);
        }

        [Test]
        public void Contains_DefinedObjectTest()
        {
            Assert.IsTrue(factory.Contains("BasicWebServiceClient"));
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
            doc.LoadXml(invalidConstructorIndexConfigXml);
            var configHandler = new ObjectsConfigurationSectionHandler();
            var objectConfigTypes = (Dictionary<string, ObjectConfigurationType>)configHandler.Create(null, null, doc.DocumentElement);
            factory = FactoryUtils.CreateObjectFactoryFromConfig(objectConfigTypes);
            Assert.Fail();
        }

        [Test]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void ObjectKeyNotExistTest()
        {
            var factoryInstance = factory.Create<object>("ObjectKeyNotExist");
            Assert.Fail();
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
        [ExpectedException(typeof(NotSupportedException))]
        public void PropertyNotCreatableFromStringTest()
        {
            var factoryInstance = factory.Create<object>("PropertyNotCreatableFromString");
            Assert.Fail();
        }
    }
}