using System;
using ServiceStack.Common.Web;

namespace ServiceStack.CacheAccess.Providers
{
	public class CompressableFileAndCacheTextManagerFactory
		: ICompressableCacheTextManagerFactory
	{
		private readonly string basePath;
		private readonly ICacheClient cacheClient;

		public CompressableFileAndCacheTextManagerFactory(string basePath, ICacheClient cacheClient)
		{
			this.basePath = basePath;
			this.cacheClient = cacheClient;
		}

		private FileAndCacheTextManager xmlCacheManager;
		public FileAndCacheTextManager XmlCacheManager
		{
			get
			{
				if (xmlCacheManager == null)
				{
					xmlCacheManager = new FileAndCacheTextManager(this.basePath, new XmlCacheManager(cacheClient));
				}
				return xmlCacheManager;
			}
		}

		private FileAndCacheTextManager jsonCacheManager;
		public FileAndCacheTextManager JsonCacheManager
		{
			get
			{
				if (jsonCacheManager == null)
				{
					jsonCacheManager = new FileAndCacheTextManager(this.basePath, new JsonCacheManager(cacheClient));
				}
				return jsonCacheManager;
			}
		}

		public ICompressableCacheTextManager Resolve(string contentType)
		{
			switch (contentType)
			{
				case MimeTypes.Xml:
					return this.XmlCacheManager;
				
				case MimeTypes.Json:
					return this.JsonCacheManager;

				default:
					throw new NotSupportedException(contentType);
			}
		}
	}
}