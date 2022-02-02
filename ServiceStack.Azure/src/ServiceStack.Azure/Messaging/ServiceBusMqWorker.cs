using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if NETCORE
using Microsoft.Azure.ServiceBus;
#else
using Microsoft.ServiceBus.Messaging;
#endif

namespace ServiceStack.Azure.Messaging
{
    class ServiceBusMqWorker
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ServiceBusMqWorker));

        private readonly string queueName;
        private readonly IMessageQueueClient mqClient;
        private readonly ServiceBusMqMessageFactory mqMessageFactory;
        private readonly QueueClient sbClient;

        public ServiceBusMqWorker(ServiceBusMqMessageFactory mqMessageFactory, IMessageQueueClient mqClient, string queueName, QueueClient sbClient)
        {
            this.mqMessageFactory = mqMessageFactory;
            this.queueName = queueName;
            this.mqClient = mqClient;
            this.sbClient = sbClient;
        }

#if NETCORE
        public async Task HandleMessageAsync(Microsoft.Azure.ServiceBus.Message msg, CancellationToken token)
        {
            var strMessage = msg.Body.FromMessageBody();
            IMessage iMessage = (IMessage)JsonSerializer.DeserializeFromString(strMessage, typeof(IMessage));
            if (iMessage != null)
            {
                iMessage.Meta = new Dictionary<string, string>
                {
                    [ServiceBusMqClient.LockTokenMeta] = msg.SystemProperties.LockToken,
                    [ServiceBusMqClient.QueueNameMeta] = queueName
                };
            }

            Type msgType = iMessage.GetType().GetGenericArguments()[0];
            var messageHandlerFactory = mqMessageFactory.handlerMap[msgType];
            var messageHandler = messageHandlerFactory.CreateMessageHandler();

            messageHandler.ProcessMessage(mqClient, iMessage);
        }
#else
        public void HandleMessage(BrokeredMessage msg)
        {
            try
            {
                var strMessage = msg.GetBody<Stream>().FromMessageBody();
                IMessage iMessage = (IMessage)JsonSerializer.DeserializeFromString(strMessage, typeof(IMessage));
                if (iMessage != null)
                {
                    iMessage.Meta = new Dictionary<string, string>
                    {
                        [ServiceBusMqClient.LockTokenMeta] = msg.LockToken.ToString(),
                        [ServiceBusMqClient.QueueNameMeta] = queueName
                    };
                }
                Type msgType = iMessage.GetType().GetGenericArguments()[0];
                var messageHandlerFactory = mqMessageFactory.handlerMap[msgType];
                var messageHandler = messageHandlerFactory.CreateMessageHandler();

                messageHandler.ProcessMessage(mqClient, iMessage);
            }
            catch (Exception)
            {
                throw;
            }
        }
#endif
    }
}
