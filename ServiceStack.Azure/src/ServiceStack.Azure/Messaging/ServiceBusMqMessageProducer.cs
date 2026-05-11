using System;
using ServiceStack.Messaging;
using ServiceStack.Text;
using Azure.Messaging.ServiceBus;

namespace ServiceStack.Azure.Messaging;

public class ServiceBusMqMessageProducer : IMessageProducer
{
    protected readonly ServiceBusMqMessageFactory? parentFactory;
    public Action<ServiceBusMessage, IMessage>? PublishMessageFilter { get; set; }

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
            var sender = parentFactory!.GetOrCreateSender(queueName);
            var msg = new ServiceBusMessage(BinaryData.FromBytes(msgBody.ToUtf8Bytes()))
            {
                MessageId = message.Id.ToString()
            };
            sender.SendMessageAsync(ApplyFilter(msg, message)).GetAwaiter().GetResult();
        }
    }

    public ServiceBusMessage ApplyFilter(ServiceBusMessage azureMessage, IMessage message)
    {
        PublishMessageFilter?.Invoke(azureMessage, message);
        return azureMessage;
    }
}
