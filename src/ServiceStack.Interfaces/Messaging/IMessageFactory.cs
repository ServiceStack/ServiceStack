using System;

namespace ServiceStack.Messaging
{
    public interface IMessageFactory : IMessageQueueClientFactory
    {
        IMessageProducer CreateMessageProducer();
    }

    public interface IMessageFactory<out TMessageProducer, out TMessageQueueClient> : IMessageFactory
        where TMessageProducer : IMessageProducer
        where TMessageQueueClient : IMessageQueueClient
    {
        new TMessageProducer CreateMessageProducer();
        new TMessageQueueClient CreateMessageQueueClient();
    }
}