using System;
using ServiceStack.Messaging;
using ServiceStack.Text;
#if NETCORE
using Azure.Messaging.ServiceBus;
#else
using Microsoft.ServiceBus.Messaging;
#endif

namespace ServiceStack.Azure.Messaging;

public class ServiceBusMqMessageProducer : IMessageProducer
{
    protected readonly ServiceBusMqMessageFactory? parentFactory;

#if NETCORE
    public Action<ServiceBusMessage, IMessage>? PublishMessageFilter { get; set; }
#else
    public Action<BrokeredMessage, IMessage>? PublishMessageFilter { get; set; }
#endif

    protected internal ServiceBusMqMessageProducer(ServiceBusMqMessageFactory parentFactory)
    {
        this.parentFactory = parentFactory;
    }

    public virtual void Dispose()
    {
        StopClients();
    }

    public void StopClients()
    {
    }

    public void Publish<T>(T messageBody)
    {
        if (messageBody is IMessage message)
        {
            Diagnostics.ServiceStack.Init(message);
            Publish(message.ToInQueueName(), message);
        }
        else
        {
            Publish(new Message<T>(messageBody));
        }
    }

    public void Publish<T>(IMessage<T> message)
    {
        Publish(message.ToInQueueName(), message);
    }

    public virtual void Publish(string queueName, IMessage message)
    {
        queueName = queueName.SafeQueueName()!;
        message.ReplyTo = message.ReplyTo.SafeQueueName();

        using (JsConfig.With(new Text.Config { IncludeTypeInfo = true }))
        {
            var msgBody = JsonSerializer.SerializeToString(message, typeof(IMessage));
#if NETCORE
            var sender = parentFactory!.GetOrCreateSender(queueName);
            var msg = new ServiceBusMessage(BinaryData.FromBytes(msgBody.ToUtf8Bytes()))
            {
                MessageId = message.Id.ToString()
            };
            sender.SendMessageAsync(ApplyFilter(msg, message)).GetAwaiter().GetResult();
#else
            var sbClient = parentFactory!.GetOrCreateClient(queueName);
            var msg = new BrokeredMessage(msgBody) { MessageId = message.Id.ToString() };
            sbClient.Send(ApplyFilter(msg, message));
#endif
        }
    }

#if NETCORE
    public ServiceBusMessage ApplyFilter(ServiceBusMessage azureMessage, IMessage message)
    {
        PublishMessageFilter?.Invoke(azureMessage, message);
        return azureMessage;
    }
#else
    public BrokeredMessage ApplyFilter(BrokeredMessage azureMessage, IMessage message)
    {
        PublishMessageFilter?.Invoke(azureMessage, message);
        return azureMessage;
    }
#endif
}
