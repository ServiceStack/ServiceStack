using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Common.Web;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.CacheAccess.Providers
{
	public class XmlCacheManager  
		: CacheManager, ICacheTextManager
	{
		public XmlCacheManager(ICacheClient cacheClient)
			: base(cacheClient) { }

		public string ContentType
		{
			get { return MimeTypes.Xml; }
		}

		public string ResolveText<T>(string cacheKey, Func<T> createCacheFn)
			where T : class
		{
			var contentTypeCacheKey = cacheKey + MimeTypes.GetExtension(MimeTypes.Xml);

			var result = this.CacheClient.Get<string>(contentTypeCacheKey);
			if (result != null) return result;

			var cacheValue = createCacheFn();

			var cacheValueXml = DataContractSerializer.Instance.Parse(cacheValue);

			this.CacheClient.Set(contentTypeCacheKey, cacheValueXml);

			return cacheValueXml;
		}

		public override void Clear(IEnumerable<string> cacheKeys)
		{
			this.Clear(cacheKeys.ToArray());
		}

		public override void Clear(params string[] cacheKeys)
		{
			var ext = MimeTypes.GetExtension(MimeTypes.Xml);
			var xmlCacheKeys = cacheKeys.ToList().ConvertAll(x => x + ext);

			this.CacheClient.RemoveAll(xmlCacheKeys);
		}
	}

}