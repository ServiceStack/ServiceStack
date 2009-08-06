using System;
using System.Collections.Generic;

namespace ServiceStack.CacheAccess
{
	public interface ICacheManager
	{
		T Resolve<T>(string cacheKey, Func<T> createCacheFn) where T : class;

		void Clear(IEnumerable<string> cacheKeys);

		void Clear(params string[] cacheKeys);
	}
}