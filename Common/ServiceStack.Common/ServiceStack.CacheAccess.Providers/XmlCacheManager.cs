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

		public ICacheClient CacheClient
		{
			get { return this.cacheClient; }
		}

		public string ContentType
		{
			get { return MimeTypes.Xml; }
		}

		public string ResolveText<T>(string cacheKey, Func<T> createCacheFn)
			where T : class
		{
			var contentTypeCacheKey = cacheKey + MimeTypes.GetExtension(MimeTypes.Xml);

			var result = this.cacheClient.Get<string>(contentTypeCacheKey);
			if (result != null) return result;

			var cacheValue = createCacheFn();

			var cacheValueXml = DataContractSerializer.Instance.Parse(cacheValue);

			this.cacheClient.Set(contentTypeCacheKey, cacheValueXml);

			return cacheValueXml;
		}
	}

}