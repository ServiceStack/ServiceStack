//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using ServiceStack.Redis;

namespace ServiceStack.Messaging
{
    public class RedisMessageQueueClientFactory
        : IMessageQueueClientFactory
    {
        private readonly Action onPublishedCallback;
        private readonly IMessageByteSerializer messageByteSerializer;
        private readonly IRedisClientsManager clientsManager;

        public RedisMessageQueueClientFactory(IRedisClientsManager clientsManager, Action onPublishedCallback)
            : this(clientsManager, new ServiceStackTextMessageByteSerializer(), onPublishedCallback)
        {
        }
        
        public RedisMessageQueueClientFactory(
            IRedisClientsManager clientsManager, IMessageByteSerializer messageByteSerializer, Action onPublishedCallback)
        {
            this.onPublishedCallback = onPublishedCallback;
            this.messageByteSerializer = messageByteSerializer;
            this.clientsManager = clientsManager;
        }

        public IMessageQueueClient CreateMessageQueueClient()
        {
            return new RedisMessageQueueClient(
                this.clientsManager, this.messageByteSerializer, this.onPublishedCallback);
        }

        public void Dispose()
        {
        }
    }
}
