using System;
using ServiceStack.Messaging;

namespace ServiceStack.Redis.Messaging
{
	public class RedisMessageQueueClientFactory
		: IMessageQueueClientFactory
	{
		private readonly IRedisClientsManager manager;

		public RedisMessageQueueClientFactory(IRedisClientsManager manager)
		{
			this.manager = manager;
		}

		public IMessageQueueClient CreateMessageQueueClient()
		{
			return new RedisMessageQueueClient(this.manager);
		}

		public void Dispose()
		{
		}
	}
}