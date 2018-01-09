//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace ServiceStack.Messaging
{
    public class RedisMessageQueueClient
        : IMessageQueueClient, IOneWayClient
    {
        private readonly Action onPublishedCallback;
        private readonly IRedisClientsManager clientsManager;

        public int MaxSuccessQueueSize { get; set; }

        public RedisMessageQueueClient(IRedisClientsManager clientsManager)
            : this(clientsManager, null) { }

        public RedisMessageQueueClient(
            IRedisClientsManager clientsManager, Action onPublishedCallback)
        {
            this.onPublishedCallback = onPublishedCallback;
            this.clientsManager = clientsManager;
            this.MaxSuccessQueueSize = 100;
        }

        private IRedisNativeClient readWriteClient;
        public IRedisNativeClient ReadWriteClient => 
            readWriteClient ?? (readWriteClient = (IRedisNativeClient) clientsManager.GetClient());

        private IRedisNativeClient readOnlyClient;
        public IRedisNativeClient ReadOnlyClient => 
            readOnlyClient ?? (readOnlyClient = (IRedisNativeClient) clientsManager.GetReadOnlyClient());

        public void Publish<T>(T messageBody)
        {
            if (messageBody is IMessage message)
            {
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
            var messageBytes = message.ToBytes();
            this.ReadWriteClient.LPush(queueName, messageBytes);
            this.ReadWriteClient.Publish(QueueNames.TopicIn, queueName.ToUtf8Bytes());

            onPublishedCallback?.Invoke();
        }

        public void Notify(string queueName, IMessage message)
        {
            var messageBytes = message.ToBytes();
            this.ReadWriteClient.LPush(queueName, messageBytes);
            this.ReadWriteClient.LTrim(queueName, 0, this.MaxSuccessQueueSize);
            this.ReadWriteClient.Publish(QueueNames.TopicOut, queueName.ToUtf8Bytes());
        }

        public IMessage<T> Get<T>(string queueName, TimeSpan? timeOut = null)
        {
            var unblockingKeyAndValue = this.ReadWriteClient.BRPop(queueName, (int)timeOut.GetValueOrDefault().TotalSeconds);
            var messageBytes = unblockingKeyAndValue.Length != 2
                ? null
                : unblockingKeyAndValue[1];

            return messageBytes.ToMessage<T>();
        }

        public IMessage<T> GetAsync<T>(string queueName)
        {
            var messageBytes = this.ReadWriteClient.RPop(queueName);
            return messageBytes.ToMessage<T>();
        }

        public void Ack(IMessage message)
        {
            //NOOP message is removed at time of Get()
        }

        public void Nak(IMessage message, bool requeue, Exception exception = null)
        {
            var queueName = requeue
                ? message.ToInQueueName()
                : message.ToDlqQueueName();

            Publish(queueName, message);
        }

        public IMessage<T> CreateMessage<T>(object mqResponse)
        {
            return (IMessage<T>)mqResponse;
        }

        public string GetTempQueueName()
        {
            return QueueNames.GetTempQueueName();
        }

        public void Dispose()
        {
            this.readOnlyClient?.Dispose();
            this.readWriteClient?.Dispose();
        }
    }
}