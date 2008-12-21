using System;
using ServiceStack.Messaging.ActiveMq;
using ServiceStack.Messaging.ActiveMq.Support.Wrappers;
using NUnit.Framework;

namespace ServiceStack.Messaging.Tests.UnitTests
{
    [TestFixture]
    public class ActiveMqConnectionTests : UnitTestCaseBase
    {
        private ActiveMqConnection connection;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            connection = new ActiveMqConnection(new ActiveMqMessagingFactory(), new NmsConnectionFactoryWrapper(BROKER_URI), null);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            connection.Dispose();
        }

        public IConnection Connection
        {
            get { return connection; }
        }

        [Test]
        public void CreateOneWayQueueTest()
        {
            using (IOneWayClient client = Connection.CreateClient(DestinationQueue))
            {
                Assert.AreEqual(DestinationType.Queue, client.Destination.DestinationType);
            }
        }

        [Test]
        public void CreateOneWayTopicTest()
        {
            using (IOneWayClient client = Connection.CreateClient(DestinationTopic))
            {
                Assert.AreEqual(DestinationType.Topic, client.Destination.DestinationType);
            }
        }

        [Test]
        public void CreateReplyQueueTest()
        {
            using (IReplyClient client = Connection.CreateReplyClient(DestinationQueue))
            {
                Assert.AreEqual(DestinationType.Queue, client.Destination.DestinationType);
            }
        }

        [Test]
        public void CreateListenerTest()
        {
            using (IGatewayListener listener = Connection.CreateListener(DestinationQueue))
            {
                Assert.IsNotNull(listener);
            }
        }

        [Test]
        public void CreateRegisteredListenerTest()
        {
            using (IRegisteredListener listener = Connection.CreateRegisteredListener(DestinationTopic, "subscriberId"))
            {
                Assert.IsNotNull(listener);
            }
        }

        [Test]
        public void CreateTextMessageTest()
        {
            ITextMessage message = Connection.CreateTextMessage(TEXT_MESSAGE);
            Assert.IsNotNull(message);
            Assert.AreEqual(true, message.Persist);
            Assert.IsTrue(message.Expiration <= TimeSpan.FromDays(7));
        }
    }
}