using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Common.Web;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.CacheAccess.Providers
{
	public class JsonCacheManager 
		: CacheManager, ICacheTextManager
	{
		public JsonCacheManager(ICacheClient cacheClient)
			: base(cacheClient) { }

		public ICacheClient CacheClient
		{
			get { return this.cacheClient; }
		}

		public string ContentType
		{
			get { return MimeTypes.Json; }
		}

		public string ResolveText<T>(string cacheKey, Func<T> createCacheFn)
			where T : class
		{
			cacheKey = cacheKey + MimeTypes.GetExtension(MimeTypes.Json);

			var result = this.cacheClient.Get<string>(cacheKey);
			if (result != null) return result;

			var cacheValue = createCacheFn();

			var cacheValueText = JsonDataContractSerializer.Instance.Parse(cacheValue);

			this.cacheClient.Set(cacheKey, cacheValueText);

			return cacheValueText;
		}

		public override void Clear(IEnumerable<string> cacheKeys)
		{
			this.Clear(cacheKeys.ToArray());
		}

		public override void Clear(params string[] cacheKeys)
		{
			var ext = MimeTypes.GetExtension(MimeTypes.Json);
			var xmlCacheKeys = cacheKeys.ToList().ConvertAll(x => x + ext);

			this.cacheClient.RemoveAll(xmlCacheKeys);
		}
	}
}