using System;
using System.Collections.Generic;

namespace ServiceStack.CacheAccess.Providers
{
	public class CacheManager : ICacheManager
	{
		public CacheManager(ICacheClient cacheClient)
		{
			this.CacheClient = cacheClient;
		}

		public virtual T Resolve<T>(string cacheKey, Func<T> createCacheFn)
			where T : class
		{
			var result = this.CacheClient.Get<T>(cacheKey);
			if (result != null) return result;

			var cacheValue = createCacheFn();

			this.CacheClient.Set(cacheKey, cacheValue);

			return cacheValue;
		}

		public virtual void Clear(IEnumerable<string> cacheKeys)
		{
			this.CacheClient.RemoveAll(cacheKeys);
		}

		public virtual void Clear(params string[] cacheKeys)
		{
			this.CacheClient.RemoveAll(cacheKeys);
		}

		public ICacheClient CacheClient
		{
			get; private set;
		}
	}

}