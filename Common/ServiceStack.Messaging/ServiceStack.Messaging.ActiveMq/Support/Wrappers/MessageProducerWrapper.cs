using System;

namespace Bbc.Ww.Services.Messaging.ActiveMq.Support.Wrappers
{
    public class MessageProducerWrapper : IOneWayClient
    {
        private readonly SessionWrapper session;
        private readonly NMS.IMessageProducer producer;
        private readonly FailoverSettings failoverSettings;
        private string uri;

        public MessageProducerWrapper(SessionWrapper session, NMS.IMessageProducer producer)
        {
            this.session = session;
            this.producer = producer;
            failoverSettings = new FailoverSettings();
        }

        public void SendOneWay(ITextMessage message)
        {
            TextMessageWrapper message = (TextMessageWrapper)session.CreateTextMessage(message);
            producer.Send(message.NmsMessage);
        }

        public void SendOneWay(string messageText, MessageProperties messageProperties)
        {
            TextMessageWrapper message = (TextMessageWrapper)session.CreateTextMessage(messageText);
            message.NmsMessage.NMSCorrelationID = messageProperties.CorrelationId;
            message.NmsMessage.Properties[NmsProperties.SessionId] = messageProperties.SessionId;
            message.NmsMessage.NMSPersistent = messageProperties.Persist;
            producer.Send(message.NmsMessage);
        }

        public DestinationType DestinationType
        {
            get { throw new NotImplementedException("DestinationType"); }
        }

        public FailoverSettings FailoverSettings
        {
            get { return failoverSettings; }
        }

        public string Uri
        {
            get { return uri; }
            set { uri = value; }
        }

        public void Dispose()
        {
            producer.Dispose();
        }
    }
}