using ServiceStack.Messaging;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
#if NETCORE
using Azure.Messaging.ServiceBus;
#else
using System.IO;
using Microsoft.ServiceBus.Messaging;
#endif

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
#if NETCORE
        if (parentFactory!.pendingAcks.TryRemove(lockToken, out var complete))
            complete().GetAwaiter().GetResult();
#else
        var queueName = message.Meta[QueueNameMeta];
        var sbClient = parentFactory!.GetOrCreateClient(queueName);
        try
        {
            sbClient.Complete(Guid.Parse(lockToken));
        }
        catch (Exception)
        {
            throw;
        }
#endif
    }

    public IMessage<T>? CreateMessage<T>(object mqResponse)
    {
        if (mqResponse is IMessage)
            return (IMessage<T>)mqResponse;

#if NETCORE
        if (mqResponse is not ServiceBusReceivedMessage msg)
            return null;
        var msgBody = msg.Body.ToArray().FromMessageBody();
#else
        if (!(mqResponse is BrokeredMessage msg))
            return null;
        var msgBody = msg.GetBody<Stream>().FromMessageBody();
#endif

        var iMessage = (IMessage<T>)JsonSerializer.DeserializeFromString<IMessage>(msgBody);
        return iMessage;
    }

    public override void Dispose()
    {
    }

    public IMessage<T> Get<T>(string queueName, TimeSpan? timeout = null)
    {
        queueName = queueName.SafeQueueName()!;

#if NETCORE
        var receiver = parentFactory!.GetOrCreateReceiver(queueName);
        ServiceBusReceivedMessage? msg;
        string? lockToken = null;

        // ReceiveAndDelete mode: messages are auto-removed on receipt; no explicit Ack needed.
        // Loop discards stale/unrecognised messages and retries within the remaining timeout.
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(60));
        while (true)
        {
            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero) { msg = null; break; }
            msg = receiver.ReceiveMessageAsync(remaining).GetAwaiter().GetResult();
            if (msg == null) break;
            if (CreateMessage<T>(msg) != null) { lockToken = msg.LockToken; break; }
            // Unrecognised format: already deleted by ReceiveAndDelete; just retry
            msg = null;
        }
#else
        var sbClient = parentFactory!.GetOrCreateClient(queueName);
        string? lockToken = null;
        var msg = timeout.HasValue
            ? sbClient.Receive(timeout.Value)
            : sbClient.Receive();
        if (msg != null)
            lockToken = msg.LockToken.ToString();
#endif

        var iMessage = CreateMessage<T>(msg);
        if (iMessage != null)
        {
            iMessage.Meta = new Dictionary<string, string>
            {
                [LockTokenMeta] = lockToken!,
                [QueueNameMeta] = queueName
            };
        }

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
