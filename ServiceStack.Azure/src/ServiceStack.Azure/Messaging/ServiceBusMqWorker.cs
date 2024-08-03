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

namespace ServiceStack.Azure.Messaging;

class ServiceBusMqWorker
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(ServiceBusMqWorker));

    public IMessageHandler MessageHandler { get; }
    public IMessageQueueClient MqClient { get; }
    public string QueueName { get; }
    public QueueClient Client { get; }

    public ServiceBusMqWorker(IMessageHandler messageHandler,
        IMessageQueueClient mqClient,
        string queueName,
        QueueClient sbClient)
    {
        MessageHandler = messageHandler;
        MqClient = mqClient;
        QueueName = queueName;
        Client = sbClient;
    }

    public IMessageHandlerStats GetStats() => MessageHandler.GetStats();

#if NETCORE
    public Task HandleMessageAsync(Microsoft.Azure.ServiceBus.Message msg, CancellationToken token)
    {
        var strMessage = msg.Body.FromMessageBody();
        IMessage iMessage = (IMessage)JsonSerializer.DeserializeFromString(strMessage, typeof(IMessage));
        if (iMessage != null)
        {
            iMessage.Meta = new Dictionary<string, string>
            {
                [ServiceBusMqClient.LockTokenMeta] = msg.SystemProperties.LockToken,
                [ServiceBusMqClient.QueueNameMeta] = QueueName
            };
        }

        MessageHandler.ProcessMessage(MqClient, iMessage);
        return Task.CompletedTask;
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
                    [ServiceBusMqClient.QueueNameMeta] = QueueName
                };
            }
            MessageHandler.ProcessMessage(MqClient, iMessage);
        }
        catch (Exception)
        {
            throw;
        }
    }
#endif
}