using ServiceStack.DataAccess;

namespace ServiceStack.CacheAccess.Providers
{
	public class PersistenceProviderCache : BasicPersistenceProviderCacheBase
	{
		protected IPersistenceProviderManager ProviderManager { get; set; }

		public PersistenceProviderCache(ICacheClient cacheClient, IPersistenceProviderManager providerManager)
			: base(cacheClient)
		{
			this.ProviderManager = providerManager;
		}

		public override IBasicPersistenceProvider GetBasicPersistenceProvider()
		{
			return this.ProviderManager.GetProvider();
		}
	}
}