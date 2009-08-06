using System;
using System.Collections.Generic;

namespace ServiceStack.CacheAccess.Providers
{
	public class CacheManager : ICacheManager
	{
		private readonly ICacheClient cacheClient;

		public CacheManager(ICacheClient cacheClient)
		{
			this.cacheClient = cacheClient;
		}

		public T Resolve<T>(string cacheKey, Func<T> createCacheFn)
			where T : class
		{
			var result = this.cacheClient.Get<T>(cacheKey);
			if (result != null) return result;

			var cacheValue = createCacheFn();

			this.cacheClient.Set(cacheKey, cacheValue);

			return cacheValue;
		}

		public void Clear(IEnumerable<string> cacheKeys)
		{
			this.cacheClient.RemoveAll(cacheKeys);
		}

		public void Clear(params string[] cacheKeys)
		{
			this.cacheClient.RemoveAll(cacheKeys);
		}
	}
}