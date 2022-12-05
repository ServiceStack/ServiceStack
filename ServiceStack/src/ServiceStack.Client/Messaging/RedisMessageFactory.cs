//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using ServiceStack.Redis;

namespace ServiceStack.Messaging
{
    public class RedisMessageFactory : IMessageFactory
    {
        private readonly IRedisClientsManager clientsManager;
        private readonly IMessageByteSerializer messageByteSerializer;

        public RedisMessageFactory(IRedisClientsManager clientsManager)
            : this(clientsManager, new ServiceStackTextMessageByteSerializer())
        {
        }

        public RedisMessageFactory(IRedisClientsManager clientsManager, IMessageByteSerializer messageByteSerializer)
        {
            this.clientsManager = clientsManager;
            this.messageByteSerializer = messageByteSerializer;
        }

        public IMessageQueueClient CreateMessageQueueClient()
        {
            return new RedisMessageQueueClient(clientsManager, messageByteSerializer);
        }

        public IMessageProducer CreateMessageProducer()
        {
            return new RedisMessageProducer(clientsManager, messageByteSerializer);
        }

        public void Dispose()
        {
        }
    }
}
