using System;
using System.Collections.Generic;
using ServiceStack.Logging;
using NMS;

namespace ServiceStack.Messaging.ActiveMq
{
    public class ActiveMqConnection : INmsConnectionManager
    {
        private const int MESSAGE_EXPIRATION_DAYS = 1;
        private const string ERROR_NOT_INMS_CONNECTION = "IMessagingFactory.CreateConnection did not return an instance of INmsConnection";
        private const string ERROR_DESTINATION = "Destination Type: {0} is not supported";

        private readonly ILog log;
        private NMS.IConnection nmsConnection;
        private readonly List<IDisposable> resources;

        private readonly IMessagingFactory factory;
        private readonly INmsConnectionFactory nmsConnectionFactory;
        private readonly string conectionString;
        private readonly FailoverSettings failoverSettings;

        public ActiveMqConnection(IMessagingFactory messagingFactory, INmsConnectionFactory nmsConnectionFactory, FailoverSettings failoverSettings)
        {
            if (messagingFactory == null)
            {
                throw new ArgumentNullException("messagingFactory");
            }
            this.factory = messagingFactory;
            this.nmsConnectionFactory = nmsConnectionFactory;
            this.conectionString = conectionString;
            this.failoverSettings = failoverSettings;
            log = LogManager.GetLogger(GetType());
            resources = new List<IDisposable>();
        }

        public void Start()
        {
            NmsConnection.Start();
        }

        private static void AssertValidDestination(IDestination destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }
            if (destination.DestinationType != DestinationType.Queue
                && destination.DestinationType != DestinationType.Topic)
            {
                throw new NotSupportedException(string.Format(ERROR_DESTINATION, destination.DestinationType));
            }
        }

        public IMessagingFactory Factory
        {
            get { return factory; }
        }

        public NMS.IConnection Reconnect(string connectionString)
        {
            if (nmsConnection != null)
            {
                Dispose();
            }
            INmsConnectionManager nmsConn = factory.CreateConnection(connectionString) as INmsConnectionManager;
            if (nmsConn == null)
            {
                throw new InvalidCastException(ERROR_NOT_INMS_CONNECTION);
            }
            return nmsConn.NmsConnection;
        }

        public NMS.IConnection NmsConnection
        {
            get
            {
                if (nmsConnection == null)
                {
                    nmsConnection = nmsConnectionFactory.CreateNmsConnection();
                }
                return nmsConnection;
            }
        }

        public IOneWayClient CreateClient(IDestination destination)
        {
            log.Debug("Creating new OneWayClient.");
            AssertValidDestination(destination);
            IOneWayClient client;
            switch (destination.DestinationType)
            {
                case DestinationType.Queue:
                    client = new ActiveMqQueueClient(this, destination);
                    break;
                case DestinationType.Topic:
                    client = new ActiveMqTopicClient(this, destination);
                    break;
                default:
                    throw new NotSupportedException(string.Format(ERROR_DESTINATION, destination.DestinationType));
            }
            if (failoverSettings != null)
            {
                client.FailoverSettings.Load(failoverSettings);
            }
            resources.Add(client);
            return client;
        }

        public IReplyClient CreateReplyClient(IDestination destination)
        {
            log.Debug("Creating new ReplyClient.");
            AssertValidDestination(destination);
            IReplyClient client;
            switch (destination.DestinationType)
            {
                case DestinationType.Queue:
                    client = new ActiveMqQueueClient(this, destination);
                    break;
                default:
                    throw new NotSupportedException(string.Format(ERROR_DESTINATION, destination.DestinationType));
            }
            if (failoverSettings != null)
            {
                client.FailoverSettings.Load(failoverSettings);
            }
            resources.Add(client);
            return client;
        }

        public IGatewayListener CreateListener(IDestination destination)
        {
            log.Debug("Creating new GatewayListener.");
            AssertValidDestination(destination);
            IGatewayListener gateway;
            switch (destination.DestinationType)
            {
                case DestinationType.Queue:
                    gateway = new ActiveMqQueueListener(this, destination);
                    break;
                case DestinationType.Topic:
                    gateway = new ActiveMqTopicListener(this, destination);
                    break;
                default:
                    throw new NotSupportedException(string.Format(ERROR_DESTINATION, destination.DestinationType));
            }
            if (failoverSettings != null)
            {
                gateway.FailoverSettings.Load(failoverSettings);
            }
            resources.Add(gateway);
            return gateway;
        }

        public IRegisteredListener CreateRegisteredListener(IDestination destination, string subscriberId)
        {
            log.Debug("Creating new RegisteredListener.");
            AssertValidDestination(destination);
            IRegisteredListener listener;
            switch (destination.DestinationType)
            {
                case DestinationType.Topic:
                    listener = new ActiveMqTopicListener(this, destination);
                    listener.SubscriberId = subscriberId;
                    break;
                default:
                    throw new NotSupportedException(string.Format(ERROR_DESTINATION, destination.DestinationType));
            }
            if (failoverSettings != null)
            {
                listener.FailoverSettings.Load(failoverSettings);
            }
            resources.Add(listener);
            return listener;
        }

        public ITextMessage CreateTextMessage(string text)
        {
            log.Debug("Creating new TextMessage.");
            ITextMessage textMessage = new TextMessage(text)
            {
                Persist = true,
                Expiration = TimeSpan.FromDays(MESSAGE_EXPIRATION_DAYS)
            };
            return textMessage;
        }

        public void Dispose()
        {
            foreach (IDisposable resource in resources)
            {
                try
                {
                    resource.Dispose();
                }
                catch (Exception ex)
                {
                    log.Error(string.Format("Error disposing of {0} resource: {1}", resource.GetType().Name, resource), ex);
                }
            }
            resources.Clear();

            try
            {
                nmsConnection.Dispose();
                nmsConnection = null;
            }
            catch (Exception ex)
            {
                log.Error("Error disposing of connection", ex);
            }

            log.DebugFormat("Disposed NMSConnection and {0} resource(s)", resources.Count);
        }
    }
}