using ServiceStack.CacheAccess;
using ServiceStack.Redis;

namespace ServiceStack.ServiceInterface.Auth
{
	public class InMemoryAuthRepository : RedisAuthRepository
	{
		public class InMemoryRedisClientsManager : IRedisClientsManager
		{
			public IRedisClient GetClient()
			{
				throw new System.NotImplementedException();
			}

			public IRedisClient GetReadOnlyClient()
			{
				throw new System.NotImplementedException();
			}

			public ICacheClient GetCacheClient()
			{
				throw new System.NotImplementedException();
			}

			public ICacheClient GetReadOnlyCacheClient()
			{
				throw new System.NotImplementedException();
			}

			public void Dispose() {}
		}

		public InMemoryAuthRepository() : base(null) {}
	}
}