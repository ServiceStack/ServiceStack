using System;
using ServiceStack.Logging;
using ServiceStack.Messaging.ActiveMq;

namespace ServiceStack.Messaging.Tests.Objects.Mock
{
    public class MockConnection : INmsConnectionManager
    {
        private ILog log;
        private const string ERROR_NOT_INMS_CONNECTION = "IMessagingFactory.CreateConnection did not return an instance of INmsConnection";
        private const string ERROR_DESTINATION = "Destination Type: {0} is not supported";

        private readonly MockMessagingFactory factory;
        private NMS.IConnection nmsConnection;

        public MockConnection(MockMessagingFactory messagingFactory, NMS.IConnection connection)
        {
            this.factory = messagingFactory;            
            this.nmsConnection = connection;
            log = LogManager.GetLogger(GetType());
        }

        public IMessagingFactory Factory
        {
            get { return factory; }
        }

        public NMS.IConnection Reconnect(string connectionString)
        {
            log.Debug("MOCK: Reconnecting...");
            if (nmsConnection != null)
            {
                nmsConnection.Dispose();
            }
            INmsConnectionManager nmsConn = factory.CreateConnection(connectionString) as INmsConnectionManager;
            if (nmsConn == null)
            {
                throw new InvalidCastException(ERROR_NOT_INMS_CONNECTION);
            }
            nmsConnection = nmsConn.NmsConnection;
            return nmsConn.NmsConnection;
        }

        public NMS.IConnection NmsConnection
        {
            get { return nmsConnection; }
        }

        public IOneWayClient CreateClient(IDestination destination)
        {
            log.Debug("MOCK: Creating new OneWayClient.");
            switch (destination.DestinationType)
            {
                case DestinationType.Queue:
                    return new ActiveMqQueueClient(this, destination);
                case DestinationType.Topic:
                    return new ActiveMqTopicClient(this, destination);
            }
            throw new NotSupportedException(string.Format(ERROR_DESTINATION, destination.DestinationType));
        }

        public IReplyClient CreateReplyClient(IDestination destination)
        {
            log.Debug("MOCK: Creating new ReplyClient.");
            switch (destination.DestinationType)
            {
                case DestinationType.Queue:
                    return new ActiveMqQueueClient(this, destination);
            }
            throw new NotSupportedException(string.Format(ERROR_DESTINATION, destination.DestinationType));
        }

        public IGatewayListener CreateListener(IDestination destination)
        {
            log.Debug("MOCK: Creating new GatewayListener.");
            switch (destination.DestinationType)
            {
                case DestinationType.Queue:
                    return new ActiveMqQueueListener(this, destination);
                case DestinationType.Topic:
                    return new ActiveMqTopicListener(this, destination);
            }
            throw new NotSupportedException(string.Format(ERROR_DESTINATION, destination.DestinationType));
        }

        public IRegisteredListener CreateRegisteredListener(IDestination destination, string subscriberId)
        {
            log.Debug("MOCK: Creating new RegisteredListener.");
            switch (destination.DestinationType)
            {
                case DestinationType.Topic:
                    IRegisteredListener listener = new ActiveMqTopicListener(this, destination);
                    listener.SubscriberId = subscriberId;
                    return listener;
            }
            throw new NotSupportedException(string.Format(ERROR_DESTINATION, destination.DestinationType));
        }

        public ITextMessage CreateTextMessage(string text)
        {
            log.Debug("MOCK: Creating new TextMessage.");
            ITextMessage textMessage = new TextMessage(text);
            textMessage.Persist = true;
            return textMessage;
        }

        public void Start()
        {
            log.Debug("MOCK: Connection started...");
            nmsConnection.Stop();
        }

        public void Dispose()
        {
            nmsConnection.Dispose();
        }
    }
}