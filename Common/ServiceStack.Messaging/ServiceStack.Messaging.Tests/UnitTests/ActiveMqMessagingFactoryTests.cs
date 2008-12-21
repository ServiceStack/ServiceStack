using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Messaging.ActiveMq;
using NUnit.Framework;

namespace ServiceStack.Messaging.Tests.UnitTests
{
    [TestFixture]
    public class ActiveMqMessagingFactoryTests : UnitTestCaseBase
    {
        [Test]
        public void CreateConnectionTest()
        {
            using (IConnection connection = MockFactory.CreateConnection(DestinationUri))
            {
                Assert.IsNotNull(connection);
            }
        }

        [Test]
        public void CreateConnection_WithArgumentTest()
        {
            IMessagingFactory factory = new ActiveMqMessagingFactory();
            using (IConnection connection = factory.CreateConnection(DestinationUri))
            {
                Assert.IsNotNull(connection);
            }
        }

        [Test]
        public void CreateConnection_WithPropertyTest()
        {
            ActiveMqMessagingFactory factory = new ActiveMqMessagingFactory();
            factory.ConnectionString = DestinationUri;
            using (IConnection connection = factory.CreateConnection())
            {
                Assert.IsNotNull(connection);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CreateConnection_WithoutPropertyTest()
        {
            ActiveMqMessagingFactory factory = new ActiveMqMessagingFactory();
            using (IConnection connection = factory.CreateConnection())
            {
                Assert.IsNotNull(connection);
            }
        }

        [Test]
        public void CreateServiceHostTest()
        {
            IMessagingFactory factory = new ActiveMqMessagingFactory();
            IGatewayListener mockListener = Mock.Stub<IGatewayListener>();
            IServiceHostConfig mockConf = Mock.Stub<IServiceHostConfig>();
            IServiceHost serviceHost = factory.CreateServiceHost(mockListener, mockConf);
            Assert.IsNotNull(serviceHost);
        }
    }
}
