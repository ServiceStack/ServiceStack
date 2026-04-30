using ServiceStack.Messaging;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using Azure.Messaging.ServiceBus;

namespace ServiceStack.Azure.Messaging;

public class ServiceBusMqClient : ServiceBusMqMessageProducer, IMessageQueueClient, IOneWayClient
{
    internal const string LockTokenMeta = "LockToken";
    internal const string QueueNameMeta = "QueueName";

    protected internal ServiceBusMqClient(ServiceBusMqMessageFactory parentFactory)
        : base(parentFactory)
    {
    }

    public void Ack(IMessage? message)
    {
        if (message == null)
            return;

        if (message.Meta == null || !message.Meta.ContainsKey(LockTokenMeta))
            throw new ArgumentException(LockTokenMeta);

        if (!message.Meta.ContainsKey(QueueNameMeta))
            throw new ArgumentException(QueueNameMeta);

        var lockToken = message.Meta[LockTokenMeta];
        if (parentFactory!.pendingAcks.TryRemove(lockToken, out var complete))
            complete().GetAwaiter().GetResult();
    }

    public IMessage<T>? CreateMessage<T>(object mqResponse)
    {
        if (mqResponse is IMessage)
            return (IMessage<T>)mqResponse;

        if (mqResponse is not ServiceBusReceivedMessage msg)
            return null;
        var msgBody = msg.Body.ToArray().FromMessageBody();

        var iMessage = (IMessage<T>)JsonSerializer.DeserializeFromString<IMessage>(msgBody);
        return iMessage;
    }

    public override void Dispose()
    {
    }

    public IMessage<T> Get<T>(string queueName, TimeSpan? timeout = null)
    {
        queueName = queueName.SafeQueueName()!;

        var receiver = parentFactory!.GetOrCreateReceiver(queueName);
        ServiceBusReceivedMessage? msg = null;
        string? lockToken = null;
        IMessage<T>? iMessage = null;

        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(60));
        while (true)
        {
            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero) break;
            var candidate = receiver.ReceiveMessageAsync(remaining).GetAwaiter().GetResult();
            if (candidate == null) break;
            iMessage = CreateMessage<T>(candidate);
            if (iMessage != null)
            {
                msg = candidate;
                lockToken = candidate.LockToken;
                break;
            }
        }

        if (iMessage == null) 
            return iMessage;
        
        parentFactory!.pendingAcks[lockToken!] = () => receiver.CompleteMessageAsync(msg);
        iMessage.Meta = new Dictionary<string, string>
        {
            [LockTokenMeta] = lockToken!,
            [QueueNameMeta] = queueName
        };

        return iMessage;
    }

    public IMessage<T> GetAsync<T>(string queueName) => Get<T>(queueName);

    public string GetTempQueueName()
    {
        throw new NotImplementedException();
    }

    public void Nak(IMessage message, bool requeue, Exception exception = null)
    {
        var queueName = requeue
            ? message.ToInQueueName()
            : message.ToDlqQueueName();

        Publish(queueName, message);
        Ack(message);
    }

    public void Notify(string queueName, IMessage message)
    {
        if (parentFactory is { MqServer.DisableNotifyMessages: true })
            return;

        Publish(queueName, message);
    }

    public void SendAllOneWay(IEnumerable<object>? requests)
    {
        if (requests == null) return;
        foreach (var request in requests)
        {
            SendOneWay(request);
        }
    }

    public void SendOneWay(object requestDto)
    {
        Publish(MessageFactory.Create(requestDto));
    }

    public void SendOneWay(string relativeOrAbsoluteUri, object requestDto)
    {
        throw new NotImplementedException();
    }
}
