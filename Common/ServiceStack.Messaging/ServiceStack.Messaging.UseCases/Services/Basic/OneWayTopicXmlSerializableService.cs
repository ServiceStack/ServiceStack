using System.Collections.Generic;

namespace ServiceStack.Messaging.UseCases.Services.Basic
{
    public class OneWayTopicXmlSerializableService : SimpleService
    {
        private readonly IGatewayListener listener;
        private readonly List<ITextMessage> messagesReceived;

        public OneWayTopicXmlSerializableService(IMessagingFactory messagingFactory, string queueUri)
        {
            messagesReceived = new List<ITextMessage>();
            IDestination destination = new Destination(DestinationType.Topic, queueUri);
            listener = messagingFactory.CreateConnection(queueUri).CreateListener(destination);
            listener.MessageReceived += new MessageReceivedHandler(listener_MessageReceived);
        }

        public List<ITextMessage> MessagesReceived
        {
            get { return messagesReceived; }
        }

        void listener_MessageReceived(object source, MessageEventArgs e)
        {
            messagesReceived.Add(e.Message);
        }

        #region IReplyService Members

        public void Start()
        {
            listener.Start();
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            listener.Dispose();
        }

        #endregion

    }
}