using System;
using ServiceStack.Common.Services.Serialization;
using ServiceStack.Messaging.ActiveMq.Support.Wrappers;
using ServiceStack.Messaging.Tests.Objects.Mock;
using ServiceStack.Messaging.Tests.Objects.Serializable;
using ServiceStack.Messaging.Tests.Support;
using NUnit.Framework;
using Rhino.Mocks;

namespace ServiceStack.Messaging.Tests.UnitTests
{
    public class UnitTestCaseBase : TestCaseBase
    {
        protected const string SERVICE_HOSTS_CONFIG_TEST = "serviceHostsConfigTest";
        private ITextMessage textMessage;
        private MockMessagingFactory mockFactory;
        private MockRepository mock;

        [SetUp]
        public override void SetUp()
        {
            mockFactory = new MockMessagingFactory();
        }

        [TearDown]
        public override void TearDown()
        {
            mockFactory = null;
        }

        public override string[] FailoverUris
        {
            get
            {
                return new string[] { "tcp://localhost:61616", "tcp://wwvis7020:61616" };
            }
        }

        public MockRepository Mock
        {
            get
            {
                if (mock == null)
                {
                    mock = new MockRepository();
                }
                return mock;
            }
        }

        protected void ReplayAll()
        {
            Mock.ReplayAll();
        }

        protected void VerifyAll()
        {
            try
            {
                Mock.VerifyAll();
            }
            catch (InvalidOperationException ex)
            {
                throw;
                //ignore ex.
            }
        }

        public MockMessagingFactory MockFactory
        {
            get
            {
                return mockFactory;
            }
        }

        protected MockNmsConnection MockNmsConnection
        {
            get
            {
                if (MockFactory.Connections.Count == 0)
                {
                    throw new ArgumentOutOfRangeException("Connections");
                }
                return MockFactory.Connections[0];
            }
        }

        protected MockNmsSession MockNmsSession
        {
            get
            {
                if (MockNmsConnection.Sessions.Count == 0)
                {
                    throw new ArgumentOutOfRangeException("Sessions");
                }
                return MockNmsConnection.Sessions[0];
            }
        }

        protected MockNmsProducer MockNmsProducer
        {
            get
            {
                if (MockNmsConnection.Sessions.Count == 0)
                {
                    throw new ArgumentOutOfRangeException("Producers");
                }
                return MockNmsSession.Producers[0];
            }
        }

        protected MockNmsConsumer MockNmsConsumer
        {
            get
            {
                if (MockNmsConnection.Sessions.Count == 0)
                {
                    throw new ArgumentOutOfRangeException("Consumers");
                }
                return MockNmsSession.Consumers[0];
            }
        }

        protected static TimeSpan MockWaitTimeOut
        {
            get
            {
                return TimeSpan.FromSeconds(5);
            }
        }

        protected static TimeSpan MockWaitToReceiveMessage
        {
            get
            {
                return TimeSpan.FromSeconds(1);
            }
        }

        protected static XmlSerializableObject GetXmlSerializableObject(string xml)
        {
            return new XmlSerializableDeserializer().Parse<XmlSerializableObject>(xml);
        }

        protected void AssertNmsState(int connectionsNo, int sessionsNo, int producersNo, int consumersNo)
        {
            Assert.AreEqual(connectionsNo, MockFactory.Connections.Count);
            if (sessionsNo > 0)
            {
                Assert.AreEqual(sessionsNo, MockNmsConnection.Sessions.Count);
            }
            if (producersNo > 0)
            {
                Assert.AreEqual(producersNo, MockNmsSession.Producers.Count);
            }
            if (consumersNo > 0)
            {
                Assert.AreEqual(consumersNo, MockNmsSession.Consumers.Count);
            }
        }

        protected static void AssertAllResourcesDisposed(MockMessagingFactory connectionFactory)
        {
            foreach (MockNmsConnection connection in connectionFactory.Connections)
            {
                Assert.AreEqual(1, connection.DisposedNo);
            }
            foreach (MockNmsSession session in connectionFactory.Sessions)
            {
                Assert.AreEqual(1, session.DisposedNo);
            }
            foreach (MockNmsProducer producer in connectionFactory.Producers)
            {
                Assert.AreEqual(1, producer.DisposedNo);
            }
            foreach (MockNmsConsumer consumer in connectionFactory.Consumers)
            {
                Assert.AreEqual(1, consumer.DisposedNo);
            }
        }

        protected static void AssertDefaultMessageProperties(NMS.ITextMessage message)
        {
            Assert.AreEqual(false, message.NMSPersistent);
            Assert.AreEqual(TimeSpan.MaxValue, message.NMSExpiration);
        }

        protected static void AssertDefaultMessageProperties(ITextMessage message)
        {
            Assert.AreEqual(false, message.Persist);
            Assert.AreEqual(TimeSpan.MaxValue, message.Expiration);
        }

        protected void AssertCustomMessageProperties(NMS.ITextMessage message)
        {
            AssertCustomMessageProperties(message.NMSCorrelationID,
                message.Properties[NmsProperties.SessionId].ToString(),
                message.NMSPersistent,
                message.NMSExpiration);
        }

        protected void AssertCustomMessageProperties(ITextMessage message)
        {
            AssertCustomMessageProperties(message.CorrelationId,
                message.SessionId,
                message.Persist,
                message.Expiration);
        }

        protected void AssertCustomMessageProperties(string correlationId, string sessionId, bool persist, TimeSpan timeout)
        {
            Assert.AreEqual(TextMessage.CorrelationId, correlationId);
            Assert.AreEqual(TextMessage.SessionId, sessionId);
            Assert.AreEqual(TextMessage.Persist, persist);
            Assert.AreEqual(TextMessage.Expiration, timeout);
        }

        protected ITextMessage TextMessage
        {
            get
            {
                if (textMessage == null)
                {
                    textMessage = new TextMessage(TEXT_MESSAGE);
                    textMessage.CorrelationId = "Custom_CorrelationId";
                    textMessage.Persist = false;
                    textMessage.SessionId = "Custom_SessionId";
                    textMessage.Expiration = TimeSpan.FromSeconds(11);
                }
                return textMessage;
            }
        }

        public ITextMessage CreateTextMessage(string text)
        {
            ITextMessage message = new TextMessage(text, TextMessage);
            return message;
        }

        public ITextMessage CreateTextMessage(string text, string correlationId)
        {
            ITextMessage message = new TextMessage(text, TextMessage);
            message.CorrelationId = correlationId;
            return message;
        }

        protected IDestination DestinationQueue
        {
            get
            {
                return new Destination(DestinationType.Queue, DestinationUri);
            }
        }

        protected IDestination DestinationTopic
        {
            get
            {
                return new Destination(DestinationType.Topic, DestinationUri);
            }
        }

        protected string DlqDestinationUri
        {
            get
            {
                return string.Format("{0}/{1}", BROKER_URI, DlqDestinationName);
            }
        }

        protected string DlqDestinationName
        {
            get
            {
                return DestinationName + ".DLQ";
            }
        }

        protected string DestinationName
        {
            get
            {
                return GetType().Name;
            }
        }

        protected string DestinationUri
        {
            get
            {
                return string.Format("{0}/{1}", BROKER_URI, DestinationName);
            }
        }

        protected override IConnection CreateNewConnection()
        {
            return MockFactory.CreateConnection(DestinationUri);
        }
    }
}