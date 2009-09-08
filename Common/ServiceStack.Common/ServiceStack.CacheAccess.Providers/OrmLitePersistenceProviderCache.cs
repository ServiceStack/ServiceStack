using System.Data;
using ServiceStack.DataAccess;
using ServiceStack.OrmLite;

namespace ServiceStack.CacheAccess.Providers
{
	public class OrmLitePersistenceProviderCache : BasicPersistenceProviderCacheBase
	{
		private readonly OrmLiteBasicPersistenceProvider provider;

		public OrmLitePersistenceProviderCache(ICacheClient cacheClient, string connectionString) 
			: base(cacheClient)
		{
			provider = new OrmLiteBasicPersistenceProvider(connectionString);
		}

		public OrmLitePersistenceProviderCache(ICacheClient cacheClient, IDbConnection dbConnection)
			: base(cacheClient)
		{
			provider = new OrmLiteBasicPersistenceProvider(dbConnection);
		}

		public override IBasicPersistenceProvider GetBasicPersistenceProvider()
		{
			return provider;
		}
	}
}