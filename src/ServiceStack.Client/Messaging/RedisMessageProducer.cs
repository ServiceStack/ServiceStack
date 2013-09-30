//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ServiceStack.Messaging
{
	public class RedisMessageProducer
		: IMessageProducer 
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
            if (typeof(IMessage<T>).IsAssignableFrom(typeof(T)))
                Publish((IMessage<T>)messageBody);
            else
                Publish((IMessage<T>)new Message<T>(messageBody));
        }

		public void Publish<T>(IMessage<T> message)
		{
			var messageBytes = message.ToBytes();
			this.ReadWriteClient.LPush(message.ToInQueueName(), messageBytes);
			this.ReadWriteClient.Publish(QueueNames.TopicIn, message.ToInQueueName().ToUtf8Bytes());
			
			if (onPublishedCallback != null)
			{
				onPublishedCallback();
			}
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