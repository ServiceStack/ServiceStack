//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace ServiceStack.Messaging
{
    public class RedisMessageProducer
        : IMessageProducer, IOneWayClient
    {
        private readonly IRedisClientsManager clientsManager;
        private readonly IMessageByteSerializer messageByteSerializer;
        private readonly Action onPublishedCallback;

        public RedisMessageProducer(IRedisClientsManager clientsManager)
            : this(clientsManager, new ServiceStackTextMessageByteSerializer(), null) { }

        public RedisMessageProducer(IRedisClientsManager clientsManager, IMessageByteSerializer messageByteSerializer)
            : this(clientsManager, messageByteSerializer, null) { }

        public RedisMessageProducer(IRedisClientsManager clientsManager, Action onPublishedCallback)
            : this(clientsManager, new ServiceStackTextMessageByteSerializer(), onPublishedCallback)
        {
        }
        
        public RedisMessageProducer(IRedisClientsManager clientsManager, IMessageByteSerializer messageByteSerializer, Action onPublishedCallback)
        {
            this.clientsManager = clientsManager;
            this.messageByteSerializer = messageByteSerializer;
            this.onPublishedCallback = onPublishedCallback;
        }

        private IRedisNativeClient readWriteClient;
        public IRedisNativeClient ReadWriteClient
        {
            get
            {
                this.readWriteClient ??= (IRedisNativeClient)clientsManager.GetClient();
                return readWriteClient;
            }
        }

        public void Publish<T>(T messageBody)
        {
            if (messageBody is IMessage message)
            {
                Diagnostics.ServiceStack.InitMessage(message);
                Publish(message.ToInQueueName(), message);
            }
            else
            {
                Publish(new Message<T>(messageBody));
            }
        }

        public void Publish<T>(IMessage<T> message)
        {
            Publish(message.ToInQueueName(), message);
        }

        public void SendOneWay(object requestDto)
        {
            Publish(MessageFactory.Create(requestDto));
        }

        public void SendOneWay(string queueName, object requestDto)
        {
            Publish(queueName, MessageFactory.Create(requestDto));
        }

        public void SendAllOneWay(IEnumerable<object> requests)
        {
            if (requests == null) return;
            foreach (var request in requests)
            {
                SendOneWay(request);
            }
        }

        public void Publish(string queueName, IMessage message)
        {
            var messageBytes = messageByteSerializer.ToBytes(message);
            this.ReadWriteClient.LPush(queueName, messageBytes);
            this.ReadWriteClient.Publish(QueueNames.TopicIn, queueName.ToUtf8Bytes());

            onPublishedCallback?.Invoke();
        }

        public void Dispose()
        {
            readWriteClient?.Dispose();
        }
    }
}
