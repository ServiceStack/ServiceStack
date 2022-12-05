using ServiceStack.Messaging;

namespace ServiceStack
{
    public static class MessageExtensions
    {
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
}
