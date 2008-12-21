using System;
using ServiceStack.Logging;
using ServiceStack.Messaging.ActiveMq.Support.Converters;
using ServiceStack.Messaging.ActiveMq.Support.Logging;
using ServiceStack.Messaging.ActiveMq.Support.Utils;
using ServiceStack.Messaging.ActiveMq.Support.Wrappers;

namespace ServiceStack.Messaging.ActiveMq
{
    /// <summary>
    /// Base ActiveMq client class used to send messages to a queue or topic
    /// </summary>
    public abstract class ActiveMqClientBase : IGatewayClient
    {
        private const string ERROR_NO_DESTINATION_SPECIFIED = "No destination was specified on IMessage.To or IOneWayClient.Uri";
        private const string ERROR_NOT_INMS_CONNECTION = "IMessagingFactory.CreateConnection did not return an instance of INmsConnection";

        private readonly ILog log;
        private bool isDisposed;
        private readonly IDestination destination;
        private readonly INmsConnectionManager nmsConnectionManager;
        private NMS.ISession session;
        private NMS.IMessageProducer producer;
        private DestinationUri destinationUri;
        private FailoverSettings failoverSettings;
        private readonly RetryCounter retryCounter;
        private readonly RetryCounter sendMessageRetryCounter;

        public ActiveMqClientBase(INmsConnectionManager nmsConnectionManager, IDestination destination)
        { 
            log = LogManager.GetLogger(GetType());
            ActiveMQ.Tracer.Trace = new ActiveMqTracer();
            isDisposed = false;
            failoverSettings = new FailoverSettings();
            retryCounter = new RetryCounter(failoverSettings);
            sendMessageRetryCounter = new RetryCounter(failoverSettings);
            this.nmsConnectionManager = nmsConnectionManager;
            this.destination = destination;
            if (this.destination != null)
            {
                DestinationUri = new DestinationUri(destination.Uri);
            }
        }

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
        /// Gets or sets the destination.
        /// </summary>
        /// <value>The destination.</value>
        public IDestination Destination
        {
            get { return new Destination(DestinationType, DestinationUri.Uri); }
            set { destinationUri = new DestinationUri(value.Uri); }
        }

        /// <summary>
        /// Gets the type of the destination. e.g. topic or queue.
        /// </summary>
        /// <value>The type of the destination.</value>
        public abstract DestinationType DestinationType { get; }

        /// <summary>
        /// Gets the failover settings.
        /// </summary>
        /// <value>The failover settings.</value>
        public FailoverSettings FailoverSettings
        {
            get
            {
                return failoverSettings;
            }
            protected set
            {
                failoverSettings = value;
            }
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <value>The connection.</value>
        public IConnection Connection
        {
            get { return nmsConnectionManager; }
        }

        /// <summary>
        /// Gets the ActiveMq session.
        /// </summary>
        /// <value>The session.</value>
        protected NMS.ISession Session
        {
            get
            {
                if (nmsConnectionManager.NmsConnection == null || session == null)
                {
                    OnConnect();
                }
                return session;
            }
        }

        /// <summary>
        /// Helper message used to send text to the specified destination.
        /// </summary>
        /// <param name="amqMessage">The amq message.</param>
        public string SendMessage(ActiveMqTextMessage amqMessage)
        {
            Exception lastException = null;
            sendMessageRetryCounter.Reset();
            do
            {
                try
                {
                    if (string.IsNullOrEmpty(amqMessage.Message.CorrelationId))
                    {
                        amqMessage.Message.CorrelationId = Guid.NewGuid().ToString();
                    }
                    NMS.IMessage message = MessageUtils.CreateNmsMessage(Session, amqMessage.Message.Text);
                    message.NMSCorrelationID = amqMessage.Message.CorrelationId;
                    message.NMSPersistent = amqMessage.Message.Persist;
                    message.NMSExpiration = amqMessage.Message.Expiration;
                    if (!string.IsNullOrEmpty(amqMessage.Message.SessionId))
                    {
                        message.Properties[NmsProperties.SessionId] = amqMessage.Message.SessionId;
                    }
                    if (amqMessage.NmsReplyTo != null)
                    {
                        message.NMSReplyTo = amqMessage.NmsReplyTo;
                    }
                    log.DebugFormat("Sending Message >> {0}", amqMessage);
                    producer.Send(amqMessage.NmsTo, message);
                    return message.NMSCorrelationID;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    log.WarnFormat("Error trying to Send Message. Error: {0}", ex.Message);
                    Reconnect();
                }
            } while (sendMessageRetryCounter.Retry());

            log.Error("Error trying to Send Message.", lastException);
            throw new ApplicationException("Error trying to Send Message.", lastException);
        }

        /// <summary>
        /// Creates a connection to the broker.
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
                    connection.ExceptionListener += new NMS.ExceptionListener(connection_ExceptionListener);
                    session = connection.CreateSession();
                    producer = session.CreateProducer();
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    log.WarnFormat("Error trying to connect to broker. Error: {0}", ex.Message);
                    Reconnect();
                }
            } while (retryCounter.Retry());
            throw new ApplicationException("Error trying to connect to broker", lastException);
        }

        /// <summary>
        /// Handles any exceptions caught by the connection manager.
        /// </summary>
        /// <param name="exception">The exception.</param>
        void connection_ExceptionListener(Exception exception)
        {
            log.WarnFormat("{0} Exception was thrown: {1}", exception.GetType().Name, exception.Message);
            if (exception is NMS.NMSException)
            {
                log.WarnFormat("Shutting down: " + destinationUri.Uri);
                Reconnect(); 
                OnConnect();
            }
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
                log.Error("Error on Reconnect", ex);
            }
        }

        /// <summary>
        /// Gets the next failover host.
        /// </summary>
        /// <returns></returns>
        private string GetNextFailoverHost()
        {
            int failoverIndex = retryCounter.TotalRetryAttempts + sendMessageRetryCounter.TotalRetryAttempts;
            int nextFailoverIndex = failoverIndex % failoverSettings.BrokerUris.Count;
            return failoverSettings.BrokerUris[nextFailoverIndex];
        }

        /// <summary>
        /// Gets the NMS destination.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        protected NMS.IDestination GetNmsDestination(IMessage message)
        {
            IDestination useDestination = message.To ?? destination;
            if (useDestination == null)
            {
                throw new ArgumentException("message", ERROR_NO_DESTINATION_SPECIFIED);
            }
            return DestinationConverter.ToNmsDestination(Session, useDestination);
        }


        #region Disposable 
        ~ActiveMqClientBase()
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
            if (producer != null)
            {
                try
                {
                    producer.Dispose();
                }
                catch (Exception ex)
                {
                    log.WarnFormat("Error disposing of producer", ex);
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
                    log.WarnFormat("Error disposing of session", ex);
                }
            }
            producer = null;
            session = null;
        }
        #endregion
    }
}