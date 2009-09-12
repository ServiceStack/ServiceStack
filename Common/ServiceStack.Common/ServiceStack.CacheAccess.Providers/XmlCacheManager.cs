using System;
using ServiceStack.Common.Web;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.CacheAccess.Providers
{
	public class XmlCacheManager 
		: CacheManager, ICacheTextManager
	{
		public XmlCacheManager(ICacheClient cacheClient)
			: base(cacheClient) { }

		public string ResolveText<T>(string cacheKey, Func<T> createCacheFn)
			where T : class
		{
			cacheKey = cacheKey + MimeTypes.GetExtension(MimeTypes.Xml);

			var result = this.cacheClient.Get<string>(cacheKey);
			if (result != null) return result;

			var cacheValue = createCacheFn();

			var cacheValueXml = DataContractSerializer.Instance.Parse(cacheValue);

			this.cacheClient.Set(cacheKey, cacheValueXml);

			return cacheValueXml;
		}
	}

}