using System;
using ServiceStack.Logging;
using ServiceStack.Messaging.ActiveMq.Support.Async;
using ServiceStack.Messaging.ActiveMq.Support.Client;
using ServiceStack.Messaging.ActiveMq.Support.Converters;
using ServiceStack.Messaging.ActiveMq.Support.Wrappers;

namespace ServiceStack.Messaging.ActiveMq
{
    /// <summary>
    /// Client used to send messages to an ActiveMQ queue.
    /// </summary>
    public class ActiveMqQueueClient : ActiveMqClientBase, IReplyClient
    {
        private readonly ILog log;
        private readonly object semaphore;
        private NMS.ITemporaryQueue replyDestination;
        private NMS.IMessageConsumer consumer;
        private readonly MessageCorrelationCollection messageCollection;

        public ActiveMqQueueClient(INmsConnectionManager nmsConnectionManager, IDestination destination)
            : base(nmsConnectionManager, destination)
        {
            log = LogManager.GetLogger(GetType());
            semaphore = new object();
            messageCollection = new MessageCorrelationCollection();
        }

        public override DestinationType DestinationType
        {
            get { return Messaging.DestinationType.Queue; }
        }

        protected void AddReplyAsyncResult(string correlationId, IReplyAsyncResult asyncResult)
        {
            if (string.IsNullOrEmpty(correlationId))
            {
                throw new ArgumentNullException("correlationId");
            }
            messageCollection.MessageAsyncResults[correlationId] = asyncResult;
        }

        protected NMS.ITextMessage ReceiveMessage(string correlationId, TimeSpan timeout)
        {
            return (NMS.ITextMessage)messageCollection.Receive(correlationId, timeout);
        }

        #region IOneWayClient Members

        public virtual void SendOneWay(ITextMessage message)
        {
            ActiveMqTextMessage amqMessage = new ActiveMqTextMessage(message);
            amqMessage.NmsTo = GetNmsDestination(message);
            SendMessage(amqMessage);
        }

        #endregion

        #region IReplyClient Members

        public virtual IAsyncResult BeginSend(ITextMessage message)
        {
            lock (semaphore)
            {
                AssertConnected(); //required to set temporary queue for reply destination
                ActiveMqTextMessage amqMessage = new ActiveMqTextMessage(message);
                amqMessage.NmsTo = GetNmsDestination(message);
                if (message.ReplyTo != null)
                {
                    amqMessage.NmsReplyTo = DestinationConverter.ToNmsDestination(Session, message.ReplyTo);
                }
                else
                {
                    amqMessage.NmsReplyTo = replyDestination;
                }
                string correlationId = SendMessage(amqMessage);
                MessageReplyAsyncResult asyncResult = new MessageReplyAsyncResult(correlationId);
                AddReplyAsyncResult(correlationId, asyncResult);
                return asyncResult;
            }
        }

        public virtual ITextMessage EndSend(IAsyncResult asyncResult, TimeSpan timeout)
        {
            lock (semaphore)
            {
                MessageReplyAsyncResult replyAsyncResult = (MessageReplyAsyncResult)asyncResult;
                string correlationId = (string)replyAsyncResult.AsyncState;
                NMS.ITextMessage responseMessage = ReceiveMessage(correlationId, timeout);
                return new TextMessageWrapper(responseMessage, null);
            }
        }

        public virtual ITextMessage Send(ITextMessage message, TimeSpan timeout)
        {
            IAsyncResult asyncResult = BeginSend(message);
            return EndSend(asyncResult, timeout);
        }

        #endregion

        /// <summary>
        /// Connects this instance and registers the replyMessage listener.
        /// </summary>
        protected override void OnConnect()
        {
            base.OnConnect();
            RegisterListener();
        }

        /// <summary>
        /// Assert that a connection is established and that a replyDestination has been issued.
        /// </summary>
        protected void AssertConnected()
        {
            if (replyDestination == null)
            {
                OnConnect();
            }
        }

        /// <summary>
        /// Registers the listener associated with the reply queue.
        /// </summary>
        private void RegisterListener()
        {
            try
            {
                replyDestination = Session.CreateTemporaryQueue();
                log.DebugFormat("Registering [{0}] listener on: {1}", replyDestination.DestinationType, replyDestination);
                consumer = Session.CreateConsumer(replyDestination);
                consumer.Listener += new NMS.MessageListener(consumer_Listener);
            }
            catch (Exception ex)
            {
                log.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Collects all messages received into the messageCollection.
        /// </summary>
        /// <param name="message">The message.</param>
        void consumer_Listener(NMS.IMessage message)
        {
            log.DebugFormat("Received Message! To: {0}, CorrelationId: {1}, ReplyTo: {2}",
                message.NMSDestination, message.NMSCorrelationID, message.NMSReplyTo);
            messageCollection.AddMessage(message, message.NMSExpiration);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// Implemented using the disposable pattern.
        /// </summary>
        public override void Dispose()
        {
            try
            {
                if (consumer != null)
                {
                    consumer.Dispose();
                    consumer = null;
                }
            }
            catch (Exception ex) 
            {
                log.Error("Error while disposing",ex);
            }
            finally
            {
                base.Dispose();
            }
        }
    }
}