using System;

namespace ServiceStack.CacheAccess
{
	public interface ICacheTextManager 
		: IHasCacheClient, ICacheClearable
	{
		string ContentType { get; }

		string ResolveText<T>(string cacheKey, Func<T> createCacheFn)
			where T : class;

		string ResolveText<T>(string cacheKey, TimeSpan expiresIn, Func<T> createCacheFn)
			where T : class;
	}
}