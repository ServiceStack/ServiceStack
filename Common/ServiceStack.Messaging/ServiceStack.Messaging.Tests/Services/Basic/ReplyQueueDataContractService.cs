using System;
using ServiceStack.Common.Services.Serialization;
using ServiceStack.Messaging.Tests.Objects.Serializable;

namespace ServiceStack.Messaging.Tests.Services.Basic
{
    public class ReplyQueueDataContractService : SimpleService, IListenerService
    {
        private readonly IGatewayListener listener;

        public ReplyQueueDataContractService(IMessagingFactory messagingFactory, string queueUri)
        {
            IDestination destination = new Destination(DestinationType.Queue, queueUri);
            listener = messagingFactory.CreateConnection(queueUri).CreateListener(destination);
            listener.MessageReceived += new MessageReceivedHandler(listener_MessageReceived);
        }

        void listener_MessageReceived(object source, MessageEventArgs e)
        {
            if (e.Message.ReplyTo == null)
            {
                throw new ApplicationException("No NMSReplyTo was specified");
            }

            DataContractObject request = new DataContractDeserializer().Parse<DataContractObject>(e.Message.Text);
            DataContractObject response = new DataContractObject();
            response.Value = Reverse(request.Value);
            string responseXml = new DataContractSerializer().Parse(response);

            ITextMessage replyMessage = listener.Connection.CreateTextMessage(responseXml);
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
