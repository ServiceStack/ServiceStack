using System;

namespace ServiceStack.CacheAccess
{
	public interface ICacheManager
		: ICacheClearable
	{
		T Resolve<T>(string cacheKey, Func<T> createCacheFn)
			where T : class;
	}
}