using System;
using System.Collections.Generic;
using ServiceStack.Logging;

namespace ServiceStack.CacheAccess.Providers
{
	public class CacheManager : ICacheManager
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(CacheManager));

		public CacheManager(ICacheClient cacheClient)
		{
			this.CacheClient = cacheClient;
		}

		public Action<Exception> OnErrorFn { get; set; }

		public virtual T Resolve<T>(string cacheKey, Func<T> createCacheFn)
			where T : class
		{
			var result = this.CacheClient.Get<T>(cacheKey);
			if (result != null) return result;

			var cacheValue = createCacheFn();

			this.CacheClient.Set(cacheKey, cacheValue);

			return cacheValue;
		}

		public virtual T Resolve<T>(string cacheKey, TimeSpan expireIn, Func<T> createCacheFn)
			where T : class
		{
			try
			{
				var result = this.CacheClient.Get<T>(cacheKey);
				if (result != null) return result;
			}
			catch (Exception ex)
			{
				Log.Error("Error accessing ICacheClient", ex);
				if (OnErrorFn != null)
				{
					OnErrorFn(ex);
				}
				return createCacheFn();
			}

			var cacheValue = createCacheFn();

			this.CacheClient.Set(cacheKey, cacheValue, expireIn);

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
			get;
			private set;
		}
	}

}