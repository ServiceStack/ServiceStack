using System;
using System.Collections.Generic;
using System.Threading;
using ServiceStack.Messaging;
using ServiceStack.Text;

namespace ServiceStack
{
    public static class MessageExtensions
    {
        public static string ToString(byte[] bytes)
        {
            return System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        private static Dictionary<Type, ToMessageDelegate> ToMessageFnCache = new Dictionary<Type, ToMessageDelegate>();
        internal static ToMessageDelegate GetToMessageFn(Type type)
        {
            ToMessageFnCache.TryGetValue(type, out var toMessageFn);

            if (toMessageFn != null) return toMessageFn;

            var genericType = typeof(MessageExtensions<>).MakeGenericType(type);
            var mi = genericType.GetStaticMethod("ConvertToMessage");
            toMessageFn = (ToMessageDelegate)mi.MakeDelegate(typeof(ToMessageDelegate));

            Dictionary<Type, ToMessageDelegate> snapshot, newCache;
            do
            {
                snapshot = ToMessageFnCache;
                newCache = new Dictionary<Type, ToMessageDelegate>(ToMessageFnCache) {
                    [type] = toMessageFn
                };

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref ToMessageFnCache, newCache, snapshot), snapshot));

            return toMessageFn;
        }

        public static IMessage ToMessage(this byte[] bytes, Type ofType)
        {
            if (bytes == null)
                return null;

            var msgFn = GetToMessageFn(ofType);
            var msg = msgFn(bytes);
            return msg;
        }

        public static Message<T> ToMessage<T>(this byte[] bytes)
        {
            if (bytes == null)
                return null;

            var messageText = ToString(bytes);
            return JsonSerializer.DeserializeFromString<Message<T>>(messageText);
        }

        public static byte[] ToBytes(this IMessage message)
        {
            var serializedMessage = JsonSerializer.SerializeToString((object)message);
            return System.Text.Encoding.UTF8.GetBytes(serializedMessage);
        }

        public static byte[] ToBytes<T>(this IMessage<T> message)
        {
            var serializedMessage = JsonSerializer.SerializeToString(message);
            return System.Text.Encoding.UTF8.GetBytes(serializedMessage);
        }

        public static string ToInQueueName(this IMessage message)
        {
            var queueName = message.Priority > 0
                ? new QueueNames(message.Body.GetType()).Priority
                : new QueueNames(message.Body.GetType()).In;

            return queueName;
        }

        public static string ToDlqQueueName(this IMessage message)
        {
            return new QueueNames(message.Body.GetType()).Dlq;
        }

        public static string ToInQueueName<T>(this IMessage<T> message)
        {
            return message.Priority > 0
                ? QueueNames<T>.Priority
                : QueueNames<T>.In;
        }

        public static IMessageQueueClient CreateMessageQueueClient(this IMessageService mqServer)
        {
            return mqServer.MessageFactory.CreateMessageQueueClient();
        }

        public static IMessageProducer CreateMessageProducer(this IMessageService mqServer)
        {
            return mqServer.MessageFactory.CreateMessageProducer();
        }
    }

    internal delegate IMessage ToMessageDelegate(object param);

    internal static class MessageExtensions<T>
    {
        public static IMessage ConvertToMessage(object oBytes)
        {
            var bytes = (byte[]) oBytes;
            return bytes.ToMessage<T>();
        }
    }
}