//
// http://code.google.com/p/servicestack/wiki/ServiceStackRedis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of Redis and ServiceStack: new BSD license.
//

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