using System;

namespace ServiceStack.Messaging.ActiveMq.Support.Client
{
    internal class MessageInfo
    {
        private readonly NMS.IMessage message;
        private readonly DateTime dateCreated;
        private readonly TimeSpan messageExpiration;

        internal MessageInfo(NMS.IMessage message, TimeSpan messageExpiration)
        {
            dateCreated = DateTime.Now;
            this.message = message;
            this.messageExpiration = messageExpiration;
        }

        public bool IsExpired
        {
            get
            {
                return (DateTime.Now - dateCreated) > messageExpiration;
            }
        }

        public NMS.IMessage Message
        {
            get
            {
                return message;
            }
        }
    }
}