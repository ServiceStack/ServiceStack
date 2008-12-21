using System;
using ServiceStack.Messaging.ActiveMq;

namespace ServiceStack.Messaging.ActiveMq
{
    public class ActiveMqTopicClient : ActiveMqClientBase, IOneWayClient
    {
        public ActiveMqTopicClient(INmsConnectionManager nmsConnectionManager, IDestination destination)
            : base(nmsConnectionManager, destination) {}

        public override DestinationType DestinationType
        {
            get { return DestinationType.Topic; }
        }

        #region IOneWayClient Members

        public virtual void SendOneWay(ITextMessage message)
        {
            ActiveMqTextMessage amqMessage = new ActiveMqTextMessage(message);
            DestinationUri destination = message.To != null ? new DestinationUri(message.To.Uri) : DestinationUri;
            amqMessage.NmsTo = Session.GetTopic(destination.Name);
            SendMessage(amqMessage);
        }
        #endregion
    }
}