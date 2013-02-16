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

using System;
using ServiceStack.CacheAccess;

namespace ServiceStack.Redis
{
	public interface IRedisClientCacheManager 
		: IDisposable
	{
		/// <summary>
		/// Returns a Read/Write client (The default) using the hosts defined in ReadWriteHosts
		/// </summary>
		/// <returns></returns>
		IRedisClient GetClient();

		/// <summary>
		/// Returns a ReadOnly client using the hosts defined in ReadOnlyHosts.
		/// </summary>
		/// <returns></returns>
		IRedisClient GetReadOnlyClient();

		/// <summary>
		/// Returns a Read/Write ICacheClient (The default) using the hosts defined in ReadWriteHosts
		/// </summary>
		/// <returns></returns>
		ICacheClient GetCacheClient();

		/// <summary>
		/// Returns a ReadOnly ICacheClient using the hosts defined in ReadOnlyHosts.
		/// </summary>
		/// <returns></returns>
		ICacheClient GetReadOnlyCacheClient();
	}
}