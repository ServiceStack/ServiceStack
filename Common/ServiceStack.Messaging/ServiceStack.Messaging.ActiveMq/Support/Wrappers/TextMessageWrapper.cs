using System;
using ServiceStack.Messaging.ActiveMq.Support.Utils;

namespace ServiceStack.Messaging.ActiveMq.Support.Wrappers
{
    /// <summary>
    /// 
    /// </summary>
    public class TextMessageWrapper : ITextMessage
    {
        private readonly string brokerUri;
        private readonly NMS.IMessage message;
        private string text;

        public TextMessageWrapper(NMS.IMessage message, string brokerUri)
        {
            this.message = message;
            this.brokerUri = brokerUri;
        }

        public string Text
        {
            get
            {
                if (text == null)
                {
                    text = MessageUtils.GetText(message);
                }
                return text;
            }
        }

        public string CorrelationId
        {
            get { return message.NMSCorrelationID; }
            set { message.NMSCorrelationID = value; }
        }

        public TimeSpan Expiration
        {
            get { return message.NMSExpiration; }
            set { message.NMSExpiration = value; }
        }

        public IDestination To
        {
            get
            {
                return (message.NMSDestination != null) 
                    ? new DestinationWrapper(message.NMSDestination, brokerUri) : null;
            }
            set { throw new NotImplementedException(); }
        }

        public IDestination ReplyTo
        {
            get
            {
                return (message.NMSReplyTo != null) 
                    ? new DestinationWrapper(message.NMSReplyTo, brokerUri) : null;
            }
            set { throw new NotImplementedException(); }
        }

        public string SessionId
        {
            get { return (string)message.Properties[NmsProperties.SessionId]; }
            set { message.Properties[NmsProperties.SessionId] = value; }
        }

        public DateTime TimeStamp
        {
            get { return message.NMSTimestamp; }
            set { throw new NotImplementedException(); }
        }

        public bool Persist
        {
            get { return message.NMSPersistent; }
            set { message.NMSPersistent = value; }
        }

        public override string ToString()
        {
            string toType = To != null ? To.DestinationType.ToString() : string.Empty;
            string replyToType = ReplyTo != null ? ReplyTo.DestinationType.ToString() : string.Empty;
            return string.Format("To: [{0}] {1}, CorrelationId: {2}, ReplyTo: [{3}] {4}, Persist: {5}",
                toType, To, CorrelationId, replyToType, ReplyTo, Persist);
        }
    }
}