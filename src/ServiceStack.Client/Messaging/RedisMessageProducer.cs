//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ServiceStack.Messaging
{
	public class RedisMessageProducer
		: IMessageProducer, IOneWayClient 
	{
		private readonly IRedisClientsManager clientsManager;
		private readonly Action onPublishedCallback;

		public RedisMessageProducer(IRedisClientsManager clientsManager)
			: this(clientsManager, null) {}

		public RedisMessageProducer(IRedisClientsManager clientsManager, Action onPublishedCallback)
		{
			this.clientsManager = clientsManager;
			this.onPublishedCallback = onPublishedCallback;
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
			if (readWriteClient != null)
			{
				readWriteClient.Dispose();
			}
		}
	}
}