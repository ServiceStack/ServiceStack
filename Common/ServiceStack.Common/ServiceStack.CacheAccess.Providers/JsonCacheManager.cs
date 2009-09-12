using System;
using ServiceStack.Common.Web;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.CacheAccess.Providers
{
	public class JsonCacheManager 
		: CacheManager, ICacheTextManager
	{
		public JsonCacheManager(ICacheClient cacheClient)
			: base(cacheClient) { }

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
	}
}