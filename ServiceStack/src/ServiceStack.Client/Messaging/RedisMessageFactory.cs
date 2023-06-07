//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using ServiceStack.Redis;
using ServiceStack.Text;

namespace ServiceStack.Messaging
{
    public class RedisMessageFactory : IMessageFactory
    {
        public static string RegisterAllowRuntimeTypeInTypes { get; set; } = typeof(ServiceStack.Messaging.Message).FullName; 
        
        private readonly IRedisClientsManager clientsManager;

        public RedisMessageFactory(IRedisClientsManager clientsManager)
        {
            this.clientsManager = clientsManager;

            if (RegisterAllowRuntimeTypeInTypes != null)
                JsConfig.AllowRuntimeTypeInTypes.Add(RegisterAllowRuntimeTypeInTypes);
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