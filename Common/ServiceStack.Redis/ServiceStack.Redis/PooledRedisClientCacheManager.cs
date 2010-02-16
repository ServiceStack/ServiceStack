using System.Collections.Generic;
using ServiceStack.CacheAccess;

namespace ServiceStack.Redis
{
	public class PooledRedisClientCacheManager
		: PooledRedisClientManager, IRedisClientCacheManager
	{
		public const int DefaultCacheDb = 9;

		public int Db { get; set; }

		public PooledRedisClientCacheManager()
		{
			Init();
		}

		public PooledRedisClientCacheManager(params string[] readWriteHosts)
			: base(readWriteHosts)
		{
			Init();
		}

		public PooledRedisClientCacheManager(IEnumerable<string> writeHosts, IEnumerable<string> readHosts)
			: base(writeHosts, readHosts)
		{
			Init();
		}

		private void Init()
		{
			RedisClientFactory = RedisCacheClientFactory.Instance;
			this.Db = DefaultCacheDb;
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
			//Provide automatic partitioning of 'Redis Caches' from normal persisted data.
			client.Db = DefaultCacheDb;
			return (RedisCacheClient)client;
		}
	}

}