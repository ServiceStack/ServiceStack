using System;
using ServiceStack.ServiceHost;

namespace ServiceStack.CacheAccess.Providers
{
	public static class CacheTextManagerFactory
	{
		public static ICacheTextManager Create(EndpointAttributes endpointAttributes, ICacheClient cacheClient)
		{
			var isXml = (endpointAttributes & EndpointAttributes.Xml)
						== EndpointAttributes.Xml;

			if (isXml) return new XmlCacheManager(cacheClient);

			var isJson = (endpointAttributes & EndpointAttributes.Json)
						== EndpointAttributes.Json;

			if (isJson) return new JsonCacheManager(cacheClient);

			var isJsv = (endpointAttributes & EndpointAttributes.Jsv)
						== EndpointAttributes.Jsv;

			if (isJsv) return new JsvCacheManager(cacheClient);

			throw new NotSupportedException("Only Xml, Json and Jsv CachTextManagers are supported");
		}
	}
}