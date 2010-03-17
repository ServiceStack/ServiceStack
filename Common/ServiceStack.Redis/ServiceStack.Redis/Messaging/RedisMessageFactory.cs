using System;
using ServiceStack.Messaging;

namespace ServiceStack.Redis.Messaging
{
	public class RedisMessageFactory
		: IMessageFactory
	{
		private IRedisClientsManager manager;
		private RedisMessageService service;

		public RedisMessageFactory(int retryAttempts, TimeSpan? requestTimeOut,
			IRedisClientsManager clientsManager)			
		{
			this.manager = clientsManager ?? new BasicRedisClientManager();
			service = new RedisMessageService(retryAttempts, requestTimeOut, this.manager);
		}

		public IMessageProducer CreateMessageProducer()
		{
			return new RedisMessageProducer(this.manager);
		}

		public IMessageService CreateMessageService()
		{
			return service;
		}

		public void Dispose()
		{
			if (this.service != null)
			{
				this.service.Dispose();
				this.service = null;
			}

			if (this.manager != null)
			{
				this.manager.Dispose();
				this.manager = null;
			}
		}
	}
}