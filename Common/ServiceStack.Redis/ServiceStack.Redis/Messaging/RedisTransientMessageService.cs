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