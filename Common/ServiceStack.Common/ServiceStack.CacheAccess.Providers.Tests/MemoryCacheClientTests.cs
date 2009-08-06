using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ServiceStack.CacheAccess.Providers.Tests
{
	[TestFixture]
	public class MemoryCacheClientTests : CacheClientTestBase
	{
		[TestFixtureSetUp]
		protected void SetUp()
		{
			this.cacheClient = new MemoryCacheClient();
		}

		[Test]
		public void MemoryCache_CacheAdd()
		{
			var cacheKey = Guid.NewGuid().ToString();

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
