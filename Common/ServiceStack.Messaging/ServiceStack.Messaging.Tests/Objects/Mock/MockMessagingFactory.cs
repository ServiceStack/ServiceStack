using System;
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.Logging;

namespace ServiceStack.Messaging.Tests.Objects.Mock
{
    public class MockMessagingFactory : IMessagingFactory
    {
        private ILog log;
        private readonly List<MockNmsConnection> connections;
        readonly Dictionary<NMS.IDestination, List<MockNmsConsumer>> consumerMap;
        readonly Dictionary<NMS.IDestination, List<NMS.ITextMessage>> unsentMessages;
        private Type factoryConnectionType;
        private Type factoryProducerType;

        public MockMessagingFactory()
        {
            log = LogManager.GetLogger(GetType());
            this.connections = new List<MockNmsConnection>();
            consumerMap = new Dictionary<NMS.IDestination, List<MockNmsConsumer>>();
            unsentMessages = new Dictionary<NMS.IDestination, List<NMS.ITextMessage>>();
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

        public Dictionary<NMS.IDestination, List<MockNmsConsumer>> ConsumerMap
        {
            get { return consumerMap; }
        }

        public Dictionary<NMS.IDestination, List<NMS.ITextMessage>> UnsentMessages
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

        public IConnection CreateConnection(string connectionString)
        {
            log.Debug("MOCK: Creating new Connection.");
            MockNmsConnection mock = CreateConnectionFromType();
            mock.BrokerUri = connectionString;
            connections.Add(mock);
            return new MockConnection(this, mock);
        }

        public IConnection CreateConnection()
        {
            throw new NotImplementedException();
        }

        public IServiceHost CreateServiceHost(IGatewayListener listener, IServiceHostConfig config)
        {
            log.Debug("MOCK: Creating new ServiceHost.");
            return new MockServiceHost(listener, config);
        }
    }
}
