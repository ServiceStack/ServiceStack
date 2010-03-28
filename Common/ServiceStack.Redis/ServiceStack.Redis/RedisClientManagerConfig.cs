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
	public class RedisClientManagerConfig
	{
		public RedisClientManagerConfig()
		{
			AutoStart = true; //Simplifies the most common use-case - registering in an IOC
		}

		public int? DefaultDb { get; set; }
		public int MaxReadPoolSize { get; set; }
		public int MaxWritePoolSize { get; set; }
		public bool AutoStart { get; set; }
	}
}