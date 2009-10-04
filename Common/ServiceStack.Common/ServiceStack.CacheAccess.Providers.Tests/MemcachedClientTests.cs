using System;
using NUnit.Framework;
using ServiceStack.CacheAccess.Memcached;
using ServiceStack.Configuration;

namespace ServiceStack.CacheAccess.Providers.Tests
{
	[Ignore("Integration test which requires Memcached installed")]
	[TestFixture]
	public class MemcachedClientTests : CacheClientTestBase
	{
		[TestFixtureSetUp]
		protected void SetUp()
		{
			var memcachedServers = ConfigUtils.GetListFromAppSetting("MemcachedServers");
			//this.cacheClient = new MemcachedClientCache(memcachedServers);
		}

		[Ignore("Debug output only, not really a test")][Test]
		public void MemoryCache_test_everything()
		{
			const string cacheKey = "testEvery";

            TestEverySet(cacheKey);
		}

		[Test]
		public void MemoryCache_CacheAdd()
		{
			const string cacheKey = "testCacheKey";

			CacheAdd(cacheKey);
		}

		[Test]
		public void MemoryCache_CacheSet()
		{
			var cacheKey = Guid.NewGuid().ToString();

			CacheSet(cacheKey);
		}
	}
}