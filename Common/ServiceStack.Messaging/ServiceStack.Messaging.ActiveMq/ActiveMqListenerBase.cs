using System;
using System.Diagnostics;
using System.Net;
using ServiceStack.Logging;
using ServiceStack.Messaging.ActiveMq.Support.Host;
using ServiceStack.Messaging.ActiveMq.Support.Utils;
using ServiceStack.Messaging.ActiveMq.Support.Wrappers;

namespace ServiceStack.Messaging.ActiveMq
{
    /// <summary>
    /// Helper class to listen to incoming messages on the queue or topics provided.
    /// each messages received will raise the MessageReceived event.
    /// </summary>
    public abstract class ActiveMqListenerBase : IActiveMqListener
    {
        //Send a keep-alive message to keep the connection open
        private static readonly string ServiceHostAdvisoryTopic = "ServiceHostAdvisory";
        private const string ADVISORY_MESSAGE = "<ServiceHostAdvisory DestinationUri=\"{0}\" ProcessName=\"{1}\" IpAddress=\"{2}\" Time=\"{3}\" />";

        private readonly ILog log;
        private bool isDisposed;
        private int maximumRedeliveryCount;
        private DestinationUri destinationUri;
        private NMS.AcknowledgementMode acknowledgementMode;
        private readonly INmsConnectionManager nmsConnectionManager;
        private NMS.ISession session;
        private NMS.IMessage lastMessage;
        private NMS.IMessageConsumer consumer;
        private string deadLetterQueue;
        private string connectionId;
        private readonly FailoverSettings failoverSettings;
        private readonly RetryCounter retryCounter;
        
        public event MessageReceivedHandler MessageReceived;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveMqListenerBase"/> class.
        /// </summary>
        /// <param name="nmsConnectionManager">The NMS connection manager.</param>
        /// <param name="destination">The destination.</param>
        public ActiveMqListenerBase(INmsConnectionManager nmsConnectionManager, IDestination destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }
            log = LogManager.GetLogger(GetType());
            isDisposed = false;
            acknowledgementMode = NMS.AcknowledgementMode.Transactional;
            maximumRedeliveryCount = 0;
            failoverSettings = new FailoverSettings();
            retryCounter = new RetryCounter(failoverSettings);
            this.nmsConnectionManager = nmsConnectionManager;
            this.destinationUri = new DestinationUri(destination.Uri);
        }

        /// <summary>
        /// Gets the type of the destination. e.g. topic or queue.
        /// </summary>
        /// <value>The type of the destination.</value>
        public abstract DestinationType DestinationType { get; }

        #region IGatewayListener Members

        /// <summary>
        /// Gets or sets the destination URI.
        /// </summary>
        /// <value>The destination URI.</value>
        public DestinationUri DestinationUri
        {
            get { return destinationUri; }
            set { destinationUri = value; }
        }

        /// <summary>
        /// Destination Uri of service endpoint
        /// </summary>
        /// <value></value>
        public IDestination Destination
        {
            get { return new Destination(DestinationType, DestinationUri.Uri); }
            set { destinationUri = new DestinationUri(value.Uri); }
        }

        /// <summary>
        /// Gets the failover settings.
        /// </summary>
        /// <value>The failover settings.</value>
        public FailoverSettings FailoverSettings
        {
            get { return failoverSettings; }
        }

        #endregion

        /// <summary>
        /// Gets or sets the acknowledgement mode.
        /// </summary>
        /// <value>The acknowledgement mode.</value>
        public NMS.AcknowledgementMode AcknowledgementMode
        {
            get { return acknowledgementMode; }
            set { acknowledgementMode = value; }
        }

        /// <summary>
        /// Gets or sets the connection id.
        /// </summary>
        /// <value>The connection id.</value>
        public string ConnectionId
        {
            get { return connectionId; }
            set { connectionId = value; }
        }

        /// <summary>
        /// Gets or sets the dead letter queue. Poision messages (i.e. messages that have failed the MaximumRedeliveryCount)
        /// will be sent to the dead letter queue.
        /// </summary>
        /// <value>The dead letter queue.</value>
        public string DeadLetterQueue
        {
            get { return deadLetterQueue; }
            set { deadLetterQueue = value; }
        }

        /// <summary>
        /// Gets or sets the maximum redelivery count. The number of redelivery attempts that will be made for failed messages.
        /// </summary>
        /// <value>The maximum redelivery count.</value>
        public int MaximumRedeliveryCount
        {
            get { return maximumRedeliveryCount; }
            set { maximumRedeliveryCount = value; }
        }

        /// <summary>
        /// Gets the ActiveMQ session of this instance.
        /// </summary>
        /// <value>The session.</value>
        public NMS.ISession Session
        {
            get
            {
                if (session == null)
                {
                    OnConnect();
                }
                return session;
            }
        }

        /// <summary>
        /// Gets the ActiveMQ connection of this instance.
        /// </summary>
        /// <value>The session.</value>
        public virtual IConnection Connection
        {
            get
            {
                if (nmsConnectionManager.NmsConnection == null)
                {
                    OnConnect();
                }
                return nmsConnectionManager;
            }
        }

        public abstract void Start();

        /// <summary>
        /// Handles exceptions thrown by the active ActiveMQ connection.
        /// </summary>
        /// <param name="exception">The exception.</param>
        void connection_ExceptionListener(Exception exception)
        {
            log.WarnFormat("{0} Exception was thrown: {1}", exception.GetType().Name, exception.Message);
            if (exception is NMS.NMSException)
            {
                log.WarnFormat("Shutting down: " + destinationUri.Uri);
                retryCounter.Retry();
                Reconnect();
                OnConnect();
            }
        }

        /// <summary>
        /// Starts this instance. listening on the queue or topic indicated by the Uri
        /// </summary>
        protected virtual void OnConnect()
        {
            Exception lastException = null;
            retryCounter.Reset();
            do
            {
                try
                {
                    isDisposed = false;
                    NMS.IConnection connection = nmsConnectionManager.NmsConnection ?? nmsConnectionManager.Reconnect(DestinationUri.Host);
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        nmsConnectionManager.NmsConnection.ClientId = connectionId;
                    }
                    connection.ExceptionListener += new NMS.ExceptionListener(connection_ExceptionListener);
                    session = connection.CreateSession(acknowledgementMode);
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    log.WarnFormat("Error trying to create connection. Error: {0}", ex.Message);
                    Reconnect();
                }
            } while (retryCounter.Retry());
            log.Error("Error trying to create connection", lastException);
            throw new ApplicationException("Error trying to create connection", lastException);
        }

        /// <summary>
        /// Used when a reconnection to the broker is required. A retry counter is provided to determine
        /// which failover broker to connect to.
        /// </summary>
        private void Reconnect()
        {
            try
            {
                if (failoverSettings.BrokerUris.Count > 0)
                {
                    destinationUri.Host = GetNextFailoverHost();
                }
                Dispose();
                nmsConnectionManager.Reconnect(destinationUri.Host);
            }
            catch (Exception ex)
            {
                log.Warn("Error while reconnecting", ex);
            }
        }

        /// <summary>
        /// Gets the next failover host.
        /// </summary>
        /// <returns></returns>
        private string GetNextFailoverHost()
        {
            int failoverIndex = retryCounter.TotalRetryAttempts;
            //int failoverIndex = totalRetryAttempts > 0 ? (totalRetryAttempts - 1) : 0;
            int nextFailoverIndex = failoverIndex % failoverSettings.BrokerUris.Count;
            return failoverSettings.BrokerUris[nextFailoverIndex];
        }

        /// <summary>
        /// If this listening session is transactional it will commit the receipt of the message.
        /// </summary>
        public virtual void Commit()
        {
            if (acknowledgementMode == NMS.AcknowledgementMode.Transactional)
            {
                Session.Commit();
            }
        }

        /// <summary>
        /// If this listening session is transactional it will rollback the receipt of the message.
        /// </summary>
        public virtual void Rollback()
        {
            if (acknowledgementMode == NMS.AcknowledgementMode.Transactional)
            {
                if (GetNmsxDeliveryCount(lastMessage) > maximumRedeliveryCount)
                {
                    if (!string.IsNullOrEmpty(deadLetterQueue))
                    {
                        log.WarnFormat("Poison message found. Sending to dead letter queue: {0}", deadLetterQueue);
                        NMS.IDestination dlq = Session.GetQueue(deadLetterQueue);
                        using (NMS.IMessageProducer producer = Session.CreateProducer(dlq))
                        {
                            producer.Send(lastMessage);
                        }
                        Session.Commit();
                    }
                    else
                    {
                        log.ErrorFormat("Poison message found. No dead letter queue configured. logging message contents: \n{0}\n\n", 
                            lastMessage.ToString());
                    }
                }
                else
                {
                    Session.Rollback();
                }
            }
        }

        /// <summary>
        /// Registers the durable consumer.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <param name="durableSubscriberId">The durable subscriber id.</param>
        public void RegisterDurableConsumer(NMS.ITopic destination, string durableSubscriberId)
        {
            //http://publib.boulder.ibm.com/infocenter/wsadhelp/v5r1m2/index.jsp?topic=/com.sun.j2ee.api.doc/javax/jms/MessageConsumer.html
            // and http://activemq.apache.org/stomp.html
            //Specifies a JMS Selector using SQL 92 syntax as specified in the JMS 1.1 specificiation. 
            //This allows a filter to be applied to each message as part of the subscription.
            //As we want the consumer to receive all messages we pass in a 'true' expression
            const string CONSUMER_SELECTOR = "2 > 1";

            //only consume messages that orginated locally? Default is to consume all messages.
            const bool NO_LOCAL = false;

            log.DebugFormat("Registering [{0}] durable subscriber on: {1}, durableSubsriberId: [{2}]", 
                destination.DestinationType, destination, durableSubscriberId);
            consumer = Session.CreateDurableConsumer(destination, durableSubscriberId, CONSUMER_SELECTOR, NO_LOCAL);
            StartConsumer(consumer);
        }

        /// <summary>
        /// Registers the consumer.
        /// </summary>
        /// <param name="destination">The destination.</param>
        public void RegisterConsumer(NMS.IDestination destination)
        {
            log.DebugFormat("Registering [{0}] listener on: {1}", destination.DestinationType, destination);
            consumer = Session.CreateConsumer(destination);
            StartConsumer(consumer);
        }

        /// <summary>
        /// Starts the messageConsumer.
        /// </summary>
        /// <param name="messageConsumer">The consumer.</param>
        private void StartConsumer(NMS.IMessageConsumer messageConsumer)
        {
            ActiveMQ.MessageConsumer activeMqConsumer = messageConsumer as ActiveMQ.MessageConsumer;
            if (activeMqConsumer != null)
            {
                activeMqConsumer.MaximumRedeliveryCount = MaximumRedeliveryCount;
            }
            messageConsumer.Listener += new NMS.MessageListener(OnMessage);
            nmsConnectionManager.NmsConnection.Start();
            log.DebugFormat("ActiveMqListener started lisenting on {0}", destinationUri.Uri);
        }

        /// <summary>
        /// Called when a message is received.
        /// </summary>
        /// <param name="message">The message.</param>
        public virtual void OnMessage(NMS.IMessage message)
        {
            string toType = message.NMSDestination != null ? message.NMSDestination.DestinationType.ToString() : null;
            log.DebugFormat("Received Message << To [{0}] {1}, CorrelationId: [{2}], ReplyTo: {3}",
                toType, message.NMSDestination, message.NMSCorrelationID, message.NMSReplyTo);
            OnMessageReceived(message);
        }

        /// <summary>
        /// Called when a message is received.
        /// </summary>
        /// <param name="message">The message.</param>
        protected virtual void OnMessageReceived(NMS.IMessage message)
        {
            try
            {
                lastMessage = message;
                if (MessageReceived != null)
                {
                    log.DebugFormat("Dispatching Message to MessageReceived event subscribers");
                    MessageReceived(this, new MessageEventArgs(
                        new TextMessageWrapper(message, DestinationUri.Host)));
                }
                else
                {
                    log.DebugFormat("Message was received but there are no MessageReceived event subsribers");
                }
                Commit();
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Error OnMessageReceived: {0}. Rolling back...", ex.Message);
                Rollback();
                throw;
            }
        }

        /// <summary>
        /// Gets the NMSX delivery count.
        /// When run in unit tests message is not an ActiveMQMessage.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        private static int GetNmsxDeliveryCount(NMS.IMessage message)
        {
            ActiveMQ.Commands.ActiveMQMessage amqMessage = message as ActiveMQ.Commands.ActiveMQMessage;
            if (amqMessage != null)
            {
                return amqMessage.NMSXDeliveryCount;
            }
            int retVal = ReflectionUtils.GetPropertyValue<int>(message, NmsProperties.DeliveryCount);
            return retVal;
        }


        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="ActiveMqListenerBase"/> is reclaimed by garbage collection.
        /// </summary>
        ~ActiveMqListenerBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// Implemented using the disposable pattern.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }

            isDisposed = true;
            if (consumer != null)
            {
                try
                {
                    consumer.Dispose();
                }
                catch (Exception ex)
                {
                    log.Warn("Exception while disposing of consumer", ex);
                }
            }
            if (session != null)
            {
                try
                {
                    session.Dispose();
                }
                catch (Exception ex)
                {
                    log.Warn("Exception while disposing of session", ex);
                }
            }
            consumer = null;
            session = null;
        }

        /// <summary>
        /// Asserts the ServiceHost is connected to the broker.
        /// TODO: replace implementation with a lighter-weight ping message which justs keeps the connection alive.
        /// Fix was added to fix bug: https://issues.apache.org/activemq/browse/AMQNET-78
        /// </summary>
        public void AssertConnected()
        {
            try
            {
                string advisoryUri = new DestinationUri(destinationUri.Host, ServiceHostAdvisoryTopic).Uri;
                IDestination advisoryDestination = new Destination(DestinationType.Topic, advisoryUri);
                using (IOneWayClient client = Connection.CreateClient(advisoryDestination))
                {
                    IPHostEntry IPHost = Dns.GetHostEntry(Dns.GetHostName());
                    string exeName = Process.GetCurrentProcess().MainModule.ModuleName;
                    string advisoryMessage = string.Format(ADVISORY_MESSAGE, destinationUri.Uri, exeName,
                        IPHost.AddressList[0], DateTime.Now.ToUniversalTime());
                    log.DebugFormat("Sending advisory message: {0}", advisoryMessage);
                    client.SendOneWay(Connection.CreateTextMessage(advisoryMessage));
                }
            }
            catch (Exception ex)
            {
                log.WarnFormat("AssertConnected failed! {0} Exception was thrown: {1}", ex.GetType().Name, ex.Message);
                log.WarnFormat("Shutting down: " + destinationUri.Uri);
                Reconnect();
            }
        }
    }
}