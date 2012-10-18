using ServiceStack.DataAccess;

namespace ServiceStack.CacheAccess
{
	public interface IPersistenceProviderCacheFactory
	{
		IPersistenceProviderCache Create(IPersistenceProviderManager providerManager);
		
		IPersistenceProviderCache Create(string conntectionString);
	}
}