using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NMS;

namespace Bbc.Ww.Services.Messaging.Tests.Objects.Mock
{
    public class MockNmsConnectionFactory : IConnectionFactory
    {
        private readonly List<MockNmsConnection> connections;
        Dictionary<IDestination, MockNmsConsumer> consumerMap;
        Dictionary<IDestination, List<ITextMessage>> unsentMessages;
        private Type factoryConnectionType;
        private Type factoryProducerType;

        public MockNmsConnectionFactory()
        {
            this.connections = new List<MockNmsConnection>();
            consumerMap = new Dictionary<IDestination, MockNmsConsumer>();
            unsentMessages = new Dictionary<IDestination, List<ITextMessage>>();
            factoryConnectionType = typeof (MockNmsConnection);
            factoryProducerType = typeof (MockNmsProducer);
        }

        public Type FactoryConnectionType
        {
            get { return factoryConnectionType; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("FactoryConnectionType");
                }
                Type superType = value;
                while (superType != typeof(MockNmsConnection))
                {
                    if (superType.BaseType == null)
                    {
                        throw new ArgumentException("FactoryConnectionType must be of type MockNmsConnection");
                    }
                    superType = superType.BaseType;
                }
                factoryConnectionType = value;
            }
        }

        public Type FactoryProducerType
        {
            get { return factoryProducerType; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("FactoryProducerType");
                }
                Type superType = value;
                while (superType != typeof(MockNmsProducer))
                {
                    if (superType.BaseType == null)
                    {
                        throw new ArgumentException("FactoryProducerType must be of type MockNmsProducer");
                    }
                    superType = superType.BaseType;
                }
                factoryProducerType = value;
            }
        }

        public Dictionary<IDestination, MockNmsConsumer> ConsumerMap
        {
            get { return consumerMap; }
        }

        public Dictionary<IDestination, List<ITextMessage>> UnsentMessages
        {
            get { return unsentMessages; }
        }

        public List<MockNmsConnection> Connections
        {
            get { return connections; }
        }

        public IEnumerable<MockNmsSession> Sessions
        {
            get
            {
                foreach (MockNmsConnection connection in connections)
                {
                    foreach (MockNmsSession session in connection.Sessions)
                    {
                        yield return session;
                    }
                }
            }
        }

        public IEnumerable<MockNmsConsumer> Consumers
        {
            get
            {
                foreach (MockNmsSession session in Sessions)
                {
                    foreach (MockNmsConsumer consumer in session.Consumers)
                    {
                        yield return consumer;
                    }
                }
            }
        }

        public IEnumerable<MockNmsProducer> Producers
        {
            get
            {
                foreach (MockNmsSession session in Sessions)
                {
                    foreach (MockNmsProducer producer in session.Producers)
                    {
                        yield return producer;
                    }
                }
            }
        }

        public IEnumerable<MockSentInfo> SentMessages
        {
            get
            {
                foreach (MockNmsProducer producer in Producers)
                {
                    foreach (MockSentInfo sentInfo in producer.SentMessages)
                    {
                        yield return sentInfo;
                    }
                }
            }
        }

        #region IMessagingFactory Members

        public IConnection CreateConnection(string userName, string password)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        private MockNmsConnection CreateConnectionFromType()
        {
            ConstructorInfo ci = factoryConnectionType.GetConstructor(new Type[] {GetType()});
            MockNmsConnection mockNmsConnection = (MockNmsConnection)ci.Invoke(new object[] { this });
            return mockNmsConnection;
        }

        #endregion

        #region IConnectionFactory Members

        public IConnection CreateConnection(string brokerUri)
        {
            MockNmsConnection mock = CreateConnectionFromType();
            mock.BrokerUri = brokerUri;
            connections.Add(mock);
            return mock;
        }

        #endregion
    }
}
