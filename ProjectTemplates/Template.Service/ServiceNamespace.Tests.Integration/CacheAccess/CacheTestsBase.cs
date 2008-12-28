using System;
using System.Net;
using Ddn.CacheAccess.Memcached;
using Ddn.Common.Services.CacheAccess;
using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;

namespace @ServiceNamespace@.Tests.Integration.CacheAccess
{
	public class CacheTestsBase
	{
		public ICacheClient Cache { get; private set; }
		public ICacheClient ConfigCache { get; private set; }
		public MemcachedClient NativeCache { get; private set; }

		public CacheTestsBase()
		{
			this.NativeCache = new MemcachedClient();
			this.Cache = new DdnMemcachedClient(this.NativeCache);
			this.ConfigCache = new DdnMemcachedClient(CreateCacheWithConfig());
		}

		public MemcachedClient CreateCacheWithConfig()
		{
			// try to hit all lines in the config classes

			var mcc = new MemcachedClientConfiguration();

			mcc.Servers.Add(new IPEndPoint(IPAddress.Parse("172.20.0.98"), 11211));
			mcc.Servers.Add(new IPEndPoint(IPAddress.Parse("172.20.0.99"), 11211));

			//mcc.NodeLocator = typeof(DefaultNodeLocator);
			//mcc.KeyTransformer = typeof(SHA1KeyTransformer);
			//mcc.Transcoder = typeof(DefaultTranscoder);

			mcc.SocketPool.MinPoolSize = 10;
			mcc.SocketPool.MaxPoolSize = 100;
			mcc.SocketPool.ConnectionTimeout = new TimeSpan(0, 0, 10);
			mcc.SocketPool.DeadTimeout = new TimeSpan(0, 2, 0);

			return new MemcachedClient(mcc);
		}
	}
}