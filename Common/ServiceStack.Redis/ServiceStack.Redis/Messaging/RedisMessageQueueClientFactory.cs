using System;
using ServiceStack.Messaging;

namespace ServiceStack.Redis.Messaging
{
	public class RedisMessageQueueClientFactory
		: IMessageQueueClientFactory
	{
		private readonly Action onPublishedCallback;
		private readonly IRedisClientsManager clientsManager;

		public RedisMessageQueueClientFactory(
			IRedisClientsManager clientsManager, Action onPublishedCallback)
		{
			this.onPublishedCallback = onPublishedCallback;
			this.clientsManager = clientsManager;
		}

		public IMessageQueueClient CreateMessageQueueClient()
		{
			return new RedisMessageQueueClient(
				this.clientsManager, this.onPublishedCallback);
		}

		public void Dispose()
		{
		}
	}
}