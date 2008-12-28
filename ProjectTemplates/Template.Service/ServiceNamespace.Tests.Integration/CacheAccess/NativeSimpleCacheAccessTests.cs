using System;
using Enyim.Caching.Memcached;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace @ServiceNamespace@.Tests.Integration.CacheAccess
{
	[TestFixture]
	public class NativeSimpleCacheAccessTests : CacheTestsBase
	{

		[Test]
		public void AddAndGetSimpleObject()
		{
			var simpleObject = new SimpleObject { Id = Guid.NewGuid() };
			NativeCache.Store(StoreMode.Add, simpleObject.Id.ToString(), simpleObject, DateTime.Now.AddHours(1));

			var cachedObject = Cache.Get<SimpleObject>(simpleObject.Id.ToString());

			Assert.That(cachedObject, Is.Not.Null);
			Assert.That(cachedObject.Id, Is.EqualTo(simpleObject.Id));
		}

		[Test]
		public void SetAndGetSimpleObject()
		{
			var simpleObject = new SimpleObject { Id = Guid.NewGuid() };
			NativeCache.Store(StoreMode.Set, simpleObject.Id.ToString(), simpleObject, DateTime.Now.AddHours(1));

			var cachedObject = Cache.Get<SimpleObject>(simpleObject.Id.ToString());

			Assert.That(cachedObject, Is.Not.Null);
			Assert.That(cachedObject.Id, Is.EqualTo(simpleObject.Id));
		}

		[Test]
		public void ReplaceAndGetSimpleObject()
		{
			var simpleObject = new SimpleObject { Id = Guid.NewGuid() };
			NativeCache.Store(StoreMode.Replace, simpleObject.Id.ToString(), simpleObject, DateTime.Now.AddHours(1));

			var cachedObject = Cache.Get<SimpleObject>(simpleObject.Id.ToString());

			Assert.That(cachedObject, Is.Null);
		}
	}
}