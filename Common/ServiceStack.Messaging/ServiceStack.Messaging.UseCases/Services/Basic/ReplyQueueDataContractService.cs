using System;
using ServiceStack.Common.Services.Serialization;
using ServiceStack.Messaging.UseCases.Objects.Serializable;

namespace ServiceStack.Messaging.UseCases.Services.Basic
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

            var request = new DataContractDeserializer().Parse<DataContractObject>(e.Message.Text);
            var response = new DataContractObject {Value = Reverse(request.Value)};
            var responseXml = new DataContractSerializer().Parse(response);

            var replyMessage = listener.Connection.CreateTextMessage(responseXml);
            replyMessage.CorrelationId = e.Message.CorrelationId;
            using (var client = listener.Connection.CreateClient(e.Message.ReplyTo))
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