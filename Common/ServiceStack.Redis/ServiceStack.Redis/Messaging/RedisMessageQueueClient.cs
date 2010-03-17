using System;
using ServiceStack.Messaging;

namespace ServiceStack.Redis.Messaging
{
	public class RedisMessageQueueClient
		: IMessageQueueClient
	{
		private readonly IRedisClientsManager clientsManager;

		public RedisMessageQueueClient(IRedisClientsManager clientsManager)
		{
			this.clientsManager = clientsManager;
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
			Publish<T>(new Message<T>(messageBody));
		}

		public void Publish<T>(IMessage<T> message)
		{
			var messageBytes = message.ToBytes();
			Publish(message.ToInQueueName(), messageBytes);
		}

		public void Publish(string queueName, byte[] messageBytes)
		{
			this.ReadWriteClient.LPush(queueName, messageBytes);
		}

		public void Notify(string queueName, byte[] messageBytes)
		{
			this.ReadWriteClient.LPush(queueName, messageBytes);
			this.ReadWriteClient.LTrim(queueName, 0, 1000);
		}

		public byte[] Get(string queueName, TimeSpan? timeOut)
		{
			throw new NotImplementedException();
		}

		public byte[] GetAsync(string queueName)
		{
			return this.ReadOnlyClient.RPop(queueName);
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