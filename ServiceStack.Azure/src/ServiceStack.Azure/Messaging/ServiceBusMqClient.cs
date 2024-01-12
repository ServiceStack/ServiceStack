using ServiceStack.Messaging;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if NETCORE
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
#else
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
        var queueName = message.Meta[QueueNameMeta];

        var sbClient = parentFactory.GetOrCreateClient(queueName);
        try
        {
#if NETCORE
            sbClient.CompleteAsync(lockToken).GetAwaiter().GetResult();
#else
            sbClient.Complete(Guid.Parse(lockToken));
#endif
        }
        catch (Exception)
        {
            throw;
        }
    }

    public IMessage<T>? CreateMessage<T>(object mqResponse)
    {
        if (mqResponse is IMessage)
            return (IMessage<T>)mqResponse;

#if NETCORE
        if (mqResponse is not Microsoft.Azure.ServiceBus.Message msg)
            return null;
        var msgBody = msg.Body.FromMessageBody();
#else
        if (!(mqResponse is BrokeredMessage msg))
            return null;
        var msgBody = msg.GetBody<Stream>().FromMessageBody();
#endif

        var iMessage = (IMessage)JsonSerializer.DeserializeFromString(msgBody, typeof(IMessage));
        return (IMessage<T>)iMessage;
    }

    public override void Dispose()
    {
        // All dispose done in base class
    }

    public IMessage<T> Get<T>(string queueName, TimeSpan? timeout = default)
    {
        queueName = queueName.SafeQueueName()!;

        var sbClient = parentFactory.GetOrCreateClient(queueName);
        string? lockToken = null;

#if NETCORE
        var msg = Task.Run(() => sbClient.ReceiveAsync(timeout)).GetAwaiter().GetResult();
        if (msg != null)
            lockToken = msg.SystemProperties.LockToken;
#else
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

#if NETCORE
    private async Task<Microsoft.Azure.ServiceBus.Message> GetMessageFromReceiver(MessageReceiver messageReceiver, TimeSpan? timeout)
    {
        var msg = timeout.HasValue
            ? await messageReceiver.ReceiveAsync(timeout.Value)
            : await messageReceiver.ReceiveAsync();

        await messageReceiver.CompleteAsync(msg.SystemProperties.LockToken);
        return msg;
    }

    private async Task<Microsoft.Azure.ServiceBus.Message> GetMessageFromClient(QueueClient sbClient, TimeSpan? timeout)
    {
        var tcs = new TaskCompletionSource<Microsoft.Azure.ServiceBus.Message>();
        var task = tcs.Task;

        sbClient.RegisterMessageHandler(
            async (message, token) =>
            {
                tcs.SetResult(message);
                await sbClient.CompleteAsync(message.SystemProperties.LockToken);
            },
            (eventArgs) => Task.CompletedTask);

        if (timeout.HasValue)
        {
            await Task.WhenAny(task, Task.Delay((int)timeout.Value.TotalMilliseconds));

            if (!task.IsCompleted)
                throw new TimeoutException("Reached timeout while getting message from client");
        } else
        {
            await task;
        }

        return task.Result;
    }
#endif

    public IMessage<T> GetAsync<T>(string queueName) => Get<T>(queueName);

    public string GetTempQueueName()
    {
        throw new NotImplementedException();
    }

    public void Nak(IMessage message, bool requeue, Exception exception = null)
    {
        // If don't requeue, post message to DLQ
        var queueName = requeue
            ? message.ToInQueueName()
            : message.ToDlqQueueName();

        Publish(queueName, message);

        Ack(message);
    }

    public void Notify(string queueName, IMessage message)
    {
        if (parentFactory.MqServer.DisableNotifyMessages)
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