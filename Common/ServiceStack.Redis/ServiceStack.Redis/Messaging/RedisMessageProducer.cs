using System;
using ServiceStack.Messaging;

namespace ServiceStack.Redis.Messaging
{
	public class RedisMessageProducer
		: IMessageProducer 
	{
		private readonly IRedisClientsManager clientsManager;
		private readonly Action onPublishedCallback;

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
			Publish((IMessage<T>)new Message<T>(messageBody));
		}

		public void Publish<T>(IMessage<T> message)
		{
			var messageBytes = message.ToBytes();
			this.ReadWriteClient.LPush(message.ToInQueueName(), messageBytes);
			
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