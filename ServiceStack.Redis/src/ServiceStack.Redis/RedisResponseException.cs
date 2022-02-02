//
// https://github.com/ServiceStack/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
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