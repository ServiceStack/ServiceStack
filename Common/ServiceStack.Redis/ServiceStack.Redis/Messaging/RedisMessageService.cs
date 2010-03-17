using System;
using ServiceStack.Messaging;

namespace ServiceStack.Redis.Messaging
{
	public class RedisMessageService
		: TransientMessageServiceBase
	{
		private readonly IRedisClientsManager manager;
		private readonly RedisMessageQueueClientFactory factory;

		public RedisMessageService()
			: this(DefaultRetryCount, null, null)
		{
		}

		public RedisMessageService(int retryAttempts, TimeSpan? requestTimeOut,
			IRedisClientsManager clientsManager)
			: base(retryAttempts, requestTimeOut)
		{
			this.manager = clientsManager ?? new PooledRedisClientManager();

			this.factory = new RedisMessageQueueClientFactory(manager);
		}

		public override IMessageQueueClientFactory MessageFactory
		{
			get { return this.factory; }
		}
	}

}