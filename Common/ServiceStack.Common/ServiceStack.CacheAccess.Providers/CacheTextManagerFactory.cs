using System;
using ServiceStack.Service;

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

			throw new NotSupportedException("Only Xml and Json CachTextManagers are supported");
		}
	}
}