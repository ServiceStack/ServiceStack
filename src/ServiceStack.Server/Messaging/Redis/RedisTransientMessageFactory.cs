//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using ServiceStack.Redis;

namespace ServiceStack.Messaging.Redis
{
	/// <summary>
	/// Transient message queues are a one-pass message queue service that starts
	/// processing messages when Start() is called. Any subsequent Start() calls 
	/// while the service is running is ignored.
	/// 
	/// The transient service will continue to run until all messages have been 
	/// processed after which time it will shutdown all processing until Start() is called again.
	/// </summary>
	public class RedisTransientMessageFactory
		: IMessageFactory
	{
		public IRedisClientsManager ClientsManager { get; private set; }

		public RedisTransientMessageService MessageService { get; private set; }

		public RedisTransientMessageFactory(
			IRedisClientsManager clientsManager)
			: this(2, null, clientsManager)
		{
		}

		public RedisTransientMessageFactory(int retryAttempts, TimeSpan? requestTimeOut,
			IRedisClientsManager clientsManager)
		{
			this.ClientsManager = clientsManager ?? new BasicRedisClientManager();
			MessageService = new RedisTransientMessageService(
				retryAttempts, requestTimeOut, this);
		}

		public IMessageQueueClient CreateMessageQueueClient()
		{
			return new RedisMessageQueueClient(this.ClientsManager, OnMessagePublished);
		}

		public IMessageProducer CreateMessageProducer()
		{
			return new RedisMessageProducer(this.ClientsManager, OnMessagePublished);
		}

		public IMessageService CreateMessageService()
		{
			return MessageService;
		}


		public void OnMessagePublished()
		{
		    MessageService?.Start();
		}

		public void Dispose()
		{
			if (this.MessageService != null)
			{
				this.MessageService.Dispose();
				this.MessageService = null;
			}

			if (this.ClientsManager != null)
			{
				this.ClientsManager.Dispose();
				this.ClientsManager = null;
			}
		}

	}
}