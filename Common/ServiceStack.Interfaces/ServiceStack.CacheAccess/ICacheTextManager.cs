using System;

namespace ServiceStack.CacheAccess
{
	public interface ICacheTextManager 
		: ICacheManager
	{
		string ResolveText<T>(string cacheKey, Func<T> createCacheFn) where T : class;
	}
}