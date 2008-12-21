using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Messaging.ActiveMq;
using NMS;

namespace ServiceStack.Messaging.Tests.Services.Basic
{
    public class ReplyQueueTextService : SimpleService, IListenerService
    {
        private readonly IGatewayListener listener;

        public ReplyQueueTextService(IMessagingFactory messagingFactory, string queueUri)
        {
            IDestination destination = new Destination(DestinationType.Queue, queueUri);
            listener = messagingFactory.CreateConnection(queueUri).CreateListener(destination);
            listener.MessageReceived += new MessageReceivedHandler(listener_MessageReceived);
        }

        void listener_MessageReceived(object source, MessageEventArgs e)
        {
            string response = Reverse(e.Message.Text);
            if (e.Message.ReplyTo == null)
            {
                throw new ApplicationException("No NMSReplyTo was specified");
            }

            ITextMessage replyMessage = listener.Connection.CreateTextMessage(response);
            replyMessage.CorrelationId = e.Message.CorrelationId;
            using (IOneWayClient client = listener.Connection.CreateClient(e.Message.ReplyTo))
            {
                client.SendOneWay(replyMessage);
            }
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
            listener.Connection.Dispose();
        }

        #endregion

    }
}
