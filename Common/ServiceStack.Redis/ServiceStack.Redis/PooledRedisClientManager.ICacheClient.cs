using System;
using System.Collections.Generic;
using ServiceStack.CacheAccess;

namespace ServiceStack.Redis
{
	/// <summary>
	/// For more interoperabilty I'm also implementing the ICacheClient on
	/// this cache client manager which has the affect of calling 
	/// GetCacheClient() for all write operations and GetReadOnlyCacheClient() 
	/// for the read ones.
	/// 
	/// This works well for master-slave replication scenarios where you have 
	/// 1 master that replicates to multiple read slaves.
	/// </summary>
	public partial class PooledRedisClientManager
		: IRedisClientCacheManager, ICacheClient
	{
		public const int DefaultCacheDb = 9;

		public ICacheClient GetCacheClient()
		{
			return ConfigureRedisClient(this.GetClient());
		}

		public ICacheClient GetReadOnlyCacheClient()
		{
			return ConfigureRedisClient(this.GetReadOnlyClient());
		}

		private ICacheClient ConfigureRedisClient(IRedisClient client)
		{
			//Provide automatic partitioning of 'Redis Caches' from normal persisted data 
			//which is on DB '0' by default.

			var notUserSpecified = this.Db == RedisNativeClient.DefaultDb;
			if (notUserSpecified)
			{
				client.Db = DefaultCacheDb;
			}
			return client;
		}


		#region Implementation of ICacheClient

		public bool Remove(string key)
		{
			using (var client = GetReadOnlyCacheClient())
			{
				return client.Remove(key);
			}
		}

		public void RemoveAll(IEnumerable<string> keys)
		{
			using (var client = GetCacheClient())
			{
				client.RemoveAll(keys);
			}
		}

		public T Get<T>(string key)
		{
			using (var client = GetReadOnlyCacheClient())
			{
				return client.Get<T>(key);
			}
		}

		public long Increment(string key, uint amount)
		{
			using (var client = GetCacheClient())
			{
				return client.Increment(key, amount);
			}
		}

		public long Decrement(string key, uint amount)
		{
			using (var client = GetCacheClient())
			{
				return client.Decrement(key, amount);
			}
		}

		public bool Add<T>(string key, T value)
		{
			using (var client = GetCacheClient())
			{
				return client.Add(key, value);
			}
		}

		public bool Set<T>(string key, T value)
		{
			using (var client = GetCacheClient())
			{
				return client.Set(key, value);
			}
		}

		public bool Replace<T>(string key, T value)
		{
			using (var client = GetCacheClient())
			{
				return client.Replace(key, value);
			}
		}

		public bool Add<T>(string key, T value, DateTime expiresAt)
		{
			using (var client = GetCacheClient())
			{
				return client.Add(key, value, expiresAt);
			}
		}

		public bool Set<T>(string key, T value, DateTime expiresAt)
		{
			using (var client = GetCacheClient())
			{
				return client.Set(key, value, expiresAt);
			}
		}

		public bool Replace<T>(string key, T value, DateTime expiresAt)
		{
			using (var client = GetCacheClient())
			{
				return client.Replace(key, value, expiresAt);
			}
		}

		public bool Add<T>(string key, T value, TimeSpan expiresIn)
		{
			using (var client = GetCacheClient())
			{
				return client.Set(key, value, expiresIn);
			}
		}

		public bool Set<T>(string key, T value, TimeSpan expiresIn)
		{
			using (var client = GetCacheClient())
			{
				return client.Set(key, value, expiresIn);
			}
		}

		public bool Replace<T>(string key, T value, TimeSpan expiresIn)
		{
			using (var client = GetCacheClient())
			{
				return client.Replace(key, value, expiresIn);
			}
		}

		public void FlushAll()
		{
			using (var client = GetCacheClient())
			{
				client.FlushAll();
			}
		}

		public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
		{
			using (var client = GetReadOnlyCacheClient())
			{
				return client.GetAll<T>(keys);
			}
		}

		public void SetAll<T>(IDictionary<string, T> values)
		{
			foreach (var entry in values)
			{
				Set(entry.Key, entry.Value);
			}
		}
		#endregion
	}

}