using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Text;
using System.Collections.Generic;
#if NETCORE
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
#else
using System;
using System.IO;
using Microsoft.ServiceBus.Messaging;
#endif

namespace ServiceStack.Azure.Messaging;

class ServiceBusMqWorker
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(ServiceBusMqWorker));

    public IMessageHandler MessageHandler { get; }
    public IMessageQueueClient MqClient { get; }
    public string QueueName { get; }

#if NETCORE
    private readonly ServiceBusMqMessageFactory factory;

    public ServiceBusMqWorker(IMessageHandler messageHandler,
        IMessageQueueClient mqClient,
        string queueName,
        ServiceBusMqMessageFactory factory)
    {
        MessageHandler = messageHandler;
        MqClient = mqClient;
        QueueName = queueName;
        this.factory = factory;
    }

    public async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        var msg = args.Message;
        var strMessage = msg.Body.ToArray().FromMessageBody();
        IMessage iMessage = (IMessage)JsonSerializer.DeserializeFromString(strMessage, typeof(IMessage));
        if (iMessage != null)
        {
            // Null strings round-trip as "" via ServiceStack.Text serialization; normalize back to null
            // so ProcessMessage uses the default response queue instead of treating "" as a replyTo.
            if (string.IsNullOrEmpty(iMessage.ReplyTo))
                iMessage.ReplyTo = null;
            iMessage.Meta = new Dictionary<string, string>
            {
                [ServiceBusMqClient.LockTokenMeta] = msg.LockToken,
                [ServiceBusMqClient.QueueNameMeta] = QueueName
            };
            // No-op: Ack() must not block on CompleteMessageAsync inside the processor callback
            // (deadlocks the AMQP pump). We await the real completion below after ProcessMessage.
            factory.pendingAcks[msg.LockToken] = () => Task.CompletedTask;
        }

        MessageHandler.ProcessMessage(MqClient, iMessage);
        await args.CompleteMessageAsync(msg).ConfigureAwait(false);
    }
#else
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

    public IMessageHandlerStats GetStats() => MessageHandler.GetStats();
}
