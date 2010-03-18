using System;
using ServiceStack.Common.Extensions;
using ServiceStack.Messaging;

namespace ServiceStack.Redis.Messaging
{
	public class RedisTransientMessageService
		: TransientMessageServiceBase
	{
		private readonly RedisMessageQueueClientFactory factory;

		public RedisTransientMessageService(int retryAttempts, TimeSpan? requestTimeOut,
			RedisTransientMessageFactory messageFactory)
			: base(retryAttempts, requestTimeOut)
		{
			messageFactory.ThrowIfNull("messageFactory");

			this.factory = new RedisMessageQueueClientFactory(
				messageFactory.ClientsManager, messageFactory.OnMessagePublished);
		}

		public override IMessageQueueClientFactory MessageFactory
		{
			get { return this.factory; }
		}
	}

}