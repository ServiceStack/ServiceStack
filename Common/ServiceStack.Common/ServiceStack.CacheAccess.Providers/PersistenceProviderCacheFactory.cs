using ServiceStack.DataAccess;

namespace ServiceStack.CacheAccess.Providers
{
	public class PersistenceProviderCacheFactory : IPersistenceProviderCacheFactory
	{
		private readonly ICacheClient cacheClient;

		public PersistenceProviderCacheFactory(ICacheClient cacheClient)
		{
			this.cacheClient = cacheClient;
		}

		public IPersistenceProviderCache Create(IPersistenceProviderManager providerManager)
		{
			return new PersistenceProviderCache(this.cacheClient, providerManager);
		}

		public IPersistenceProviderCache Create(string conntectionString)
		{
			return new OrmLitePersistenceProviderCache(this.cacheClient, conntectionString);
		}
	}
}