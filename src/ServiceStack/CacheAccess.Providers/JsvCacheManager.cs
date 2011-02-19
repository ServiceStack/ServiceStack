using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Common.Web;
using ServiceStack.Text;

namespace ServiceStack.CacheAccess.Providers
{
	public class JsvCacheManager 
		: CacheManager, ICacheTextManager
	{
		public JsvCacheManager(ICacheClient cacheClient)
			: base(cacheClient) { }

		public string ContentType
		{
			get { return MimeTypes.Jsv; }
		}

		public string Resolve<T>(string cacheKey, TimeSpan? expiresIn, Func<T> createCacheFn) where T : class
		{
			cacheKey = cacheKey + MimeTypes.GetExtension(MimeTypes.Jsv);

			var result = this.CacheClient.Get<string>(cacheKey);
			if (result != null) return result;

			var cacheValue = createCacheFn();

			var cacheValueText = TypeSerializer.SerializeToString(cacheValue);

			if (expiresIn.HasValue)
				this.CacheClient.Set(cacheKey, cacheValueText, expiresIn.Value);
			else
				this.CacheClient.Set(cacheKey, cacheValueText);

			return cacheValueText;
		}

		public string ResolveText<T>(string cacheKey, TimeSpan expiresIn, Func<T> createCacheFn) where T : class
		{
			return Resolve(cacheKey, expiresIn, createCacheFn);
		}

		public string ResolveText<T>(string cacheKey, Func<T> createCacheFn)
			where T : class
		{
			return Resolve(cacheKey, null, createCacheFn);
		}

		public override void Clear(IEnumerable<string> cacheKeys)
		{
			this.Clear(cacheKeys.ToArray());
		}

		public override void Clear(params string[] cacheKeys)
		{
			var ext = MimeTypes.GetExtension(MimeTypes.Jsv);
			var cacheKeyWithExts = cacheKeys.ToList().ConvertAll(x => x + ext);

			this.CacheClient.RemoveAll(cacheKeyWithExts);
		}
	}
}