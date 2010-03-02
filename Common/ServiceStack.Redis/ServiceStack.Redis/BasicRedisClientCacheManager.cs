using System;
using System.Collections.Generic;
using ServiceStack.CacheAccess;

namespace ServiceStack.Redis
{
	/// <summary>
	/// BasicRedisClientManager for ICacheClient
	/// 
	/// For more interoperabilty I'm also implementing the ICacheClient on
	/// this cache client manager which has the affect of calling 
	/// GetClientCache() for all write operations and GetReadOnlyClientCache() 
	/// for the read ones.
	/// 
	/// This works well for master-slave replication scenarios where you have 
	/// 1 master that replicates to multiple read slaves.
	/// </summary>
	public class BasicRedisClientCacheManager
		: BasicRedisClientManager,
		  IRedisClientCacheManager, ICacheClient
	{
		public const int DefaultCacheDb = 9;

		public BasicRedisClientCacheManager()
		{
		}

		public BasicRedisClientCacheManager(params string[] readWriteHosts)
			: this(readWriteHosts, readWriteHosts)
		{
		}

		public BasicRedisClientCacheManager(
			IEnumerable<string> readWriteHosts, IEnumerable<string> readOnlyHosts)
			: base(readWriteHosts, readOnlyHosts, DefaultCacheDb)
		{
		}

		public BasicRedisClientCacheManager(
			IEnumerable<string> readWriteHosts, 
			IEnumerable<string> readOnlyHosts, 
			int initialDb)
			: base(readWriteHosts, readOnlyHosts, initialDb)
		{
		}

		protected override void OnStart()
		{
			RedisClientFactory = RedisCacheClientFactory.Instance;
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

		private ICacheClient ConfigureRedisClient(IRedisClient client)
		{
			//Provide automatic partitioning of 'Redis Caches' from normal persisted data 
			//which is on DB '0' by default.
			var notUserSpecified = this.Db == RedisNativeClient.DefaultDb;
			if (notUserSpecified)
			{
				client.Db = DefaultCacheDb;
			}
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

		public bool Add<T>(string key, T value)
		{
			using (var client = GetClientCache())
			{
				return client.Add(key, value);
			}
		}

		public bool Set<T>(string key, T value)
		{
			using (var client = GetClientCache())
			{
				return client.Set(key, value);
			}
		}

		public bool Replace<T>(string key, T value)
		{
			using (var client = GetClientCache())
			{
				return client.Replace(key, value);
			}
		}

		public bool Add<T>(string key, T value, DateTime expiresAt)
		{
			using (var client = GetClientCache())
			{
				return client.Add(key, value, expiresAt);
			}
		}

		public bool Set<T>(string key, T value, DateTime expiresAt)
		{
			using (var client = GetClientCache())
			{
				return client.Set(key, value, expiresAt);
			}
		}

		public bool Replace<T>(string key, T value, DateTime expiresAt)
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

		public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
		{
			using (var client = GetReadOnlyClientCache())
			{
				return client.GetAll<T>(keys);
			}
		}

		#endregion
	}

}