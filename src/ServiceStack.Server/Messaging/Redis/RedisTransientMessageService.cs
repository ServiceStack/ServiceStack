//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;

namespace ServiceStack.Messaging.Redis
{
	public class RedisTransientMessageService
		: TransientMessageServiceBase
	{
		private readonly RedisTransientMessageFactory messageFactory;

		public RedisTransientMessageService(int retryAttempts, TimeSpan? requestTimeOut,
			RedisTransientMessageFactory messageFactory)
			: base(retryAttempts, requestTimeOut)
		{
			messageFactory.ThrowIfNull("messageFactory");
			this.messageFactory = messageFactory;
		}

		public override IMessageFactory MessageFactory => messageFactory;
	}

}