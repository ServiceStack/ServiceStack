using System;
using System.Collections.Generic;
using ServiceStack.CacheAccess;

namespace ServiceStack.Redis
{
	/// <summary>
	/// For more interoperabilty I'm also implementing the ICacheClient on
	/// this cache client manager which has the affect of calling 
	/// GetClientCache() for all write operations and GetReadOnlyClientCache() 
	/// for the read ones.
	/// 
	/// This works well for master-slave replication scenarios where you have 
	/// 1 master that replicates to multiple read slaves.
	/// </summary>
	public class PooledRedisClientCacheManager
		: PooledRedisClientManager, IRedisClientCacheManager, ICacheClient
	{
		public const int DefaultCacheDb = 9;

		public PooledRedisClientCacheManager()
		{
		}

		public PooledRedisClientCacheManager(params string[] readWriteHosts)
			: base(readWriteHosts)
		{
		}

		public PooledRedisClientCacheManager(IEnumerable<string> readWriteHosts, IEnumerable<string> readOnlyHosts)
			: base(readWriteHosts, readOnlyHosts)
		{
		}

		public PooledRedisClientCacheManager(IEnumerable<string> readWriteHosts, IEnumerable<string> readOnlyHosts, RedisClientManagerConfig config) 
			: base(readWriteHosts, readOnlyHosts, config)
		{
		}

		protected override void  OnStart()
		{
			RedisClientFactory = RedisCacheClientFactory.Instance;
			this.Db = DefaultCacheDb;
			base.OnStart();
		}

		public ICacheClient GetClientCache()
		{
			return ConfigureRedisClient(base.GetClient());
		}

		public ICacheClient GetReadOnlyClientCache()
		{
			return ConfigureRedisClient(base.GetReadOnlyClient());
		}

		private static ICacheClient ConfigureRedisClient(IRedisClient client)
		{
			//Provide automatic partitioning of 'Redis Caches' from normal persisted data 
			//which is on DB '0' by default.
			client.Db = DefaultCacheDb;
			return (RedisCacheClient)client;
		}


		#region Implementation of ICacheClient

		public bool Remove(string key)
		{
			using (var client = GetReadOnlyClientCache())
			{
				return client.Remove(key);
			}
		}

		public void RemoveAll(IEnumerable<string> keys)
		{
			using (var client = GetClientCache())
			{
				client.RemoveAll(keys);
			}
		}

		public object Get(string key)
		{
			using (var client = GetReadOnlyClientCache())
			{
				return client.Get(key);
			}
		}

		public T Get<T>(string key)
		{
			using (var client = GetReadOnlyClientCache())
			{
				return client.Get<T>(key);
			}
		}

		public long Increment(string key, uint amount)
		{
			using (var client = GetClientCache())
			{
				return client.Increment(key, amount);
			}
		}

		public long Decrement(string key, uint amount)
		{
			using (var client = GetClientCache())
			{
				return client.Decrement(key, amount);
			}
		}

		public bool Add(string key, object value)
		{
			using (var client = GetClientCache())
			{
				return client.Add(key, value);
			}
		}

		public bool Set(string key, object value)
		{
			using (var client = GetClientCache())
			{
				return client.Set(key, value);
			}
		}

		public bool Replace(string key, object value)
		{
			using (var client = GetClientCache())
			{
				return client.Replace(key, value);
			}
		}

		public bool Add(string key, object value, DateTime expiresAt)
		{
			using (var client = GetClientCache())
			{
				return client.Add(key, value, expiresAt);
			}
		}

		public bool Set(string key, object value, DateTime expiresAt)
		{
			using (var client = GetClientCache())
			{
				return client.Set(key, value, expiresAt);
			}
		}

		public bool Replace(string key, object value, DateTime expiresAt)
		{
			using (var client = GetClientCache())
			{
				return client.Replace(key, value, expiresAt);
			}
		}

		public void FlushAll()
		{
			using (var client = GetClientCache())
			{
				client.FlushAll();
			}
		}

		public IDictionary<string, object> GetAll(IEnumerable<string> keys)
		{
			using (var client = GetReadOnlyClientCache())
			{
				return client.GetAll(keys);
			}
		}

		public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
		{
			using (var client = GetReadOnlyClientCache())
			{
				return client.GetAll<T>(keys);
			}
		}

		public bool CheckAndSet(string key, object value, ulong lastModifiedValue)
		{
			throw new NotImplementedException();
		}

		public bool CheckAndSet(string key, object value, ulong lastModifiedValue, DateTime expiresAt)
		{
			throw new NotImplementedException();
		}

		public T Get<T>(string key, out ulong ucas)
		{
			throw new NotImplementedException();
		}

		public IDictionary<string, object> GetAll(IEnumerable<string> keys, out IDictionary<string, ulong> lastModifiedValues)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

}