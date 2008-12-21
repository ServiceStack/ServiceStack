using System;

namespace ServiceStack.Messaging.ActiveMq
{
    public class ActiveMqTextMessage 
    {
        private readonly ITextMessage message;
        private NMS.IDestination nmsTo;
        private NMS.IDestination nmsReplyTo;

        public ActiveMqTextMessage(ITextMessage message)
        {
            this.message = message;
        }

        public ITextMessage Message
        {
            get { return message; }
        }

        public NMS.IDestination NmsTo
        {
            get { return nmsTo; }
            set { nmsTo = value; }
        }

        public NMS.IDestination NmsReplyTo
        {
            get { return nmsReplyTo; }
            set { nmsReplyTo = value; }
        }

        public override string ToString()
        {
            string toType = nmsTo != null ? nmsTo.DestinationType.ToString() : string.Empty;
            string replyToType = nmsReplyTo != null ? nmsReplyTo.DestinationType.ToString() : string.Empty;
            return string.Format("To: [{0}] {1}, CorrelationId: {2}, ReplyTo: [{3}] {4}, Persist: {5}",
                toType, nmsTo, message.CorrelationId, replyToType, nmsReplyTo, message.Persist);
        }
    }
}