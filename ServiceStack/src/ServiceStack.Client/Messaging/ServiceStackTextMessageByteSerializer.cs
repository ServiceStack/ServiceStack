using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ServiceStack.Messaging;

public class ServiceStackTextMessageByteSerializer : IMessageByteSerializer
{
    internal delegate IMessage ToMessageDelegate(IMessageByteSerializer serializer, object param);

    private static IDictionary<Type, ToMessageDelegate> ToMessageFnCache = new Dictionary<Type, ToMessageDelegate>();

    internal ToMessageDelegate GetToMessageFn(Type type)
    {
        ToMessageFnCache.TryGetValue(type, out var toMessageFn);

        if (toMessageFn != null) return toMessageFn;

        var method = GetType().GetMethods()
            .Single(x => x.Name == nameof(ToMessage) && x.IsGenericMethod)
            .MakeGenericMethod(type);

        toMessageFn = (ToMessageDelegate) method.MakeDelegate(typeof(ToMessageDelegate));

        IDictionary<Type, ToMessageDelegate> snapshot, newCache;
        do
        {
            snapshot = ToMessageFnCache;
            newCache = new Dictionary<Type, ToMessageDelegate>(ToMessageFnCache)
            {
                [type] = toMessageFn
            };
        } while (!ReferenceEquals(
                     Interlocked.CompareExchange(ref ToMessageFnCache, newCache, snapshot), snapshot));

        return toMessageFn;
    }

    public string ToString(byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
    }

    public IMessage ToMessage(byte[] bytes, Type ofType)
    {
        if (bytes == null)
            return null;

        var msgFn = GetToMessageFn(ofType);
        var msg = msgFn(this, bytes);
        return msg;
    }

    public Message<T> ToMessage<T>(byte[] bytes)
    {
        if (bytes == null)
            return null;

        var messageText = ToString(bytes);
        return JsonSerializer.DeserializeFromString<Message<T>>(messageText);
    }

    public byte[] ToBytes(IMessage message)
    {
        var serializedMessage = JsonSerializer.SerializeToString((object) message);
        return Encoding.UTF8.GetBytes(serializedMessage);
    }

    public byte[] ToBytes<T>(IMessage<T> message)
    {
        var serializedMessage = JsonSerializer.SerializeToString(message);
        return Encoding.UTF8.GetBytes(serializedMessage);
    }
}
