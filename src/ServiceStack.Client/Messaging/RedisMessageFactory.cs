//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using ServiceStack.Redis;

namespace ServiceStack.Messaging
{
    public class RedisMessageFactory : IMessageFactory
    {
        private readonly IRedisClientsManager clientsManager;

        public RedisMessageFactory(IRedisClientsManager clientsManager)
        {
            this.clientsManager = clientsManager;
        }

        public IMessageQueueClient CreateMessageQueueClient()
        {
            return new RedisMessageQueueClient(clientsManager);
        }

        public IMessageProducer CreateMessageProducer()
        {
            return new RedisMessageProducer(clientsManager);
        }

        public void Dispose()
        {
        }
    }
}