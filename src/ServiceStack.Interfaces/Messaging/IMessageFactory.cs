using System;

namespace ServiceStack.Messaging
{
    public interface IMessageFactory : IMessageQueueClientFactory
    {
        IMessageProducer CreateMessageProducer();
    }
}