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

namespace ServiceStack.Redis
{
	public class RedisResponseException
		: RedisException
	{
		public RedisResponseException(string message)
			: base(message)
		{
		}

		public RedisResponseException(string message, string code) : base(message)
		{
			Code = code;
		}

		public string Code { get; private set; }
	}
}