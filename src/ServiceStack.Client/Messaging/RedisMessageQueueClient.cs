//Copyright (c) Service Stack LLC. All Rights Reserved.
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
        public IRedisNativeClient ReadWriteClient
        {
            get
            {
                if (this.readWriteClient == null)
                {
                    this.readWriteClient = (IRedisNativeClient)clientsManager.GetClient();
                }
                return readWriteClient;
            }
        }

        private IRedisNativeClient readOnlyClient;
        public IRedisNativeClient ReadOnlyClient
        {
            get
            {
                if (this.readOnlyClient == null)
                {
                    this.readOnlyClient = (IRedisNativeClient)clientsManager.GetReadOnlyClient();
                }
                return readOnlyClient;
            }
        }

        public void Publish<T>(T messageBody)
        {
            var message = messageBody as IMessage;
            if (message != null)
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
            using (__requestAccess())
            {
                var messageBytes = message.ToBytes();
                this.ReadWriteClient.LPush(queueName, messageBytes);
                this.ReadWriteClient.Publish(QueueNames.TopicIn, queueName.ToUtf8Bytes());

                if (onPublishedCallback != null)
                {
                    onPublishedCallback();
                }
            }
        }

        public void Notify(string queueName, IMessage message)
        {
            using (__requestAccess())
            {
                var messageBytes = message.ToBytes();
                this.ReadWriteClient.LPush(queueName, messageBytes);
                this.ReadWriteClient.LTrim(queueName, 0, this.MaxSuccessQueueSize);
                this.ReadWriteClient.Publish(QueueNames.TopicOut, queueName.ToUtf8Bytes());
            }
        }

        public IMessage<T> Get<T>(string queueName, TimeSpan? timeOut = null)
        {
            using (__requestAccess())
            {
                var unblockingKeyAndValue = this.ReadOnlyClient.BRPop(queueName, (int)timeOut.GetValueOrDefault().TotalSeconds);
                var messageBytes = unblockingKeyAndValue.Length != 2
                    ? null
                    : unblockingKeyAndValue[1];

                return messageBytes.ToMessage<T>();
            }
        }

        public IMessage<T> GetAsync<T>(string queueName)
        {
            using (__requestAccess())
            {
                var messageBytes = this.ReadOnlyClient.RPop(queueName);
                return messageBytes.ToMessage<T>();
            }
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

        private class AccessToken
        {
            private string token;
            internal static readonly AccessToken __accessToken =
                new AccessToken("lUjBZNG56eE9yd3FQdVFSTy9qeGl5dlI5RmZwamc4U05udl000");
            private AccessToken(string token)
            {
                this.token = token;
            }
        }

        protected IDisposable __requestAccess()
        {
            return LicenseUtils.RequestAccess(AccessToken.__accessToken, LicenseFeature.Client, LicenseFeature.Text);
        }

        public void Dispose()
        {
            if (this.readOnlyClient != null)
            {
                this.readOnlyClient.Dispose();
            }
            if (this.readWriteClient != null)
            {
                this.readWriteClient.Dispose();
            }
        }
    }
}