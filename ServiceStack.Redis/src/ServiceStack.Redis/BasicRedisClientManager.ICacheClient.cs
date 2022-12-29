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

using System;
using System.Collections.Generic;
using ServiceStack.Caching;

namespace ServiceStack.Redis
{
	/// <summary>
	/// BasicRedisClientManager for ICacheClient
	/// 
	/// For more interoperability I'm also implementing the ICacheClient on
	/// this cache client manager which has the affect of calling 
	/// GetCacheClient() for all write operations and GetReadOnlyCacheClient() 
	/// for the read ones.
	/// 
	/// This works well for master-replica replication scenarios where you have 
	/// 1 master that replicates to multiple read replicas.
	/// </summary>
	public partial class BasicRedisClientManager
		: ICacheClient
	{
		public ICacheClient GetCacheClient() => 
			new RedisClientManagerCacheClient(this);

		public ICacheClient GetReadOnlyCacheClient() => 
			ConfigureRedisClient(this.GetReadOnlyClientImpl());

		private ICacheClient ConfigureRedisClient(IRedisClient client) => client;

		public bool Remove(string key)
		{
			using var client = GetReadOnlyCacheClient();
			return client.Remove(key);
		}

		public void RemoveAll(IEnumerable<string> keys)
		{
			using var client = GetCacheClient();
			client.RemoveAll(keys);
		}

		public T Get<T>(string key)
		{
			using var client = GetReadOnlyCacheClient();
			return client.Get<T>(key);
		}

		public long Increment(string key, uint amount)
		{
			using var client = GetCacheClient();
			return client.Increment(key, amount);
		}

		public long Decrement(string key, uint amount)
		{
			using var client = GetCacheClient();
			return client.Decrement(key, amount);
		}

		public bool Add<T>(string key, T value)
		{
			using var client = GetCacheClient();
			return client.Add(key, value);
		}

		public bool Set<T>(string key, T value)
		{
			using var client = GetCacheClient();
			return client.Set(key, value);
		}

		public bool Replace<T>(string key, T value)
		{
			using var client = GetCacheClient();
			return client.Replace(key, value);
		}

		public bool Add<T>(string key, T value, DateTime expiresAt)
		{
			using var client = GetCacheClient();
			return client.Add(key, value, expiresAt);
		}

		public bool Set<T>(string key, T value, DateTime expiresAt)
		{
			using var client = GetCacheClient();
			return client.Set(key, value, expiresAt);
		}

		public bool Replace<T>(string key, T value, DateTime expiresAt)
		{
			using var client = GetCacheClient();
			return client.Replace(key, value, expiresAt);
		}

		public bool Add<T>(string key, T value, TimeSpan expiresIn)
		{
			using var client = GetCacheClient();
			return client.Add(key, value, expiresIn);
		}

		public bool Set<T>(string key, T value, TimeSpan expiresIn)
		{
			using var client = GetCacheClient();
			return client.Set(key, value, expiresIn);
		}

		public bool Replace<T>(string key, T value, TimeSpan expiresIn)
		{
			using var client = GetCacheClient();
			return client.Replace(key, value, expiresIn);
		}

		public void FlushAll()
		{
			using var client = GetCacheClient();
			client.FlushAll();
		}

		public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
		{
			using var client = GetReadOnlyCacheClient();
			return client.GetAll<T>(keys);
		}
	}


}