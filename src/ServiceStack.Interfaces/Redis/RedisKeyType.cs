//
// https://github.com/mythz/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack.
//
// Licensed under the same terms of Redis and ServiceStack: new BSD license.
//

namespace ServiceStack.Redis
{
	public enum RedisKeyType
	{
		None, 
		String, 
		List, 
		Set,
		SortedSet,
		Hash
	}
}