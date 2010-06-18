using System;
using NUnit.Framework;
using ServiceStack.Logging;

namespace ServiceStack.CacheAccess.Providers.Tests
{
	public abstract class CacheClientTestBase
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(CacheClientTestBase));

		protected ICacheClient cacheClient { get; set; }

		public void ClearCacheIfExists(string cacheKey)
		{
			var cacheResult = this.cacheClient.Get<CacheTest>(cacheKey);
			if (cacheResult != null)
			{
				log.WarnFormat("{0} already exists, removing it.", cacheKey);
				this.cacheClient.Remove(cacheKey);
			}
		}

		public void CacheAdd(string cacheKey)
		{
			ClearCacheIfExists(cacheKey);

			var result = new CacheTest(1);

			var state = this.cacheClient.Add(cacheKey, result);

			Assert.That(state, Is.True);

			var cacheResult = this.cacheClient.Get<CacheTest>(cacheKey);

			Assert.That(cacheResult, Is.Not.Null);
			Assert.That(cacheResult.Value, Is.EqualTo(1));

			result = new CacheTest(2);
			state = this.cacheClient.Add(cacheKey, cacheResult);

			Assert.That(state, Is.False);

			cacheResult = this.cacheClient.Get<CacheTest>(cacheKey);

			Assert.That(cacheResult, Is.Not.Null);
			Assert.That(cacheResult.Value, Is.EqualTo(1)); //should not have changed.
		}

		public void CacheSet(string cacheKey)
		{
			ClearCacheIfExists(cacheKey);

			var result = new CacheTest(1);

			var state = this.cacheClient.Set(cacheKey, result, DateTime.Now.AddMonths(1));

			Assert.That(state, Is.True);

			var cacheResult = this.cacheClient.Get<CacheTest>(cacheKey);

			Assert.That(cacheResult, Is.Not.Null);
			Assert.That(cacheResult.Value, Is.EqualTo(1));

			result = new CacheTest(2);
			state = this.cacheClient.Set(cacheKey, result);

			Assert.That(state, Is.True);

			cacheResult = this.cacheClient.Get<CacheTest>(cacheKey);

			Assert.That(cacheResult, Is.Not.Null);
			Assert.That(cacheResult.Value, Is.EqualTo(2)); //should not have changed.
		}



		public void TestEverySet(string cacheKey)
		{
			bool state = false;
			string methodCacheKey = null;
			var cacheValue = new CacheTest(1);

			methodCacheKey = cacheKey + "Add_Key_Value";
			state = this.cacheClient.Add(methodCacheKey, cacheValue);
			log.DebugFormat("this.cacheClient.Add({0}, {1}): {2}", methodCacheKey, cacheValue.Value, state);
			TestGets(methodCacheKey);

			methodCacheKey = cacheKey + "Add_Key_Value_Date";
			state = this.cacheClient.Add(methodCacheKey, cacheValue, DateTime.Now.AddDays(1));
			log.DebugFormat("this.cacheClient.Add({0}, {1}, 1Day): {2}", methodCacheKey, cacheValue.Value, state);
			TestGets(methodCacheKey);

			methodCacheKey = cacheKey + "Set_Key_Value";
			state = this.cacheClient.Set(methodCacheKey, cacheValue);
			log.DebugFormat("this.cacheClient.Add({0}, {1}, 1Day): {2}", methodCacheKey, cacheValue.Value, state);
			TestGets(methodCacheKey);

			methodCacheKey = cacheKey + "Set_Key_Value_Date";
			state = this.cacheClient.Set(methodCacheKey, cacheValue, DateTime.Now.AddDays(1));
			log.DebugFormat("this.cacheClient.Add({0}, {1}, 1Day): {2}", methodCacheKey, cacheValue.Value, state);
			TestGets(methodCacheKey);

			methodCacheKey = cacheKey + "Set_Key_Value_Date";
			state = this.cacheClient.Set(methodCacheKey, cacheValue, DateTime.Now.AddDays(1));
			log.DebugFormat("this.cacheClient.Add({0}, {1}, 1Day): {2}", methodCacheKey, cacheValue.Value, state);
			TestGets(methodCacheKey);
		}

		public void TestGets(string cacheKey)
		{
			var obj = this.cacheClient.Get<CacheTest>(cacheKey);
			log.DebugFormat("this.cacheClient.Get({0})              ===> {1}", cacheKey, obj != null);

			var result = this.cacheClient.Get<CacheTest>(cacheKey);
			log.DebugFormat("this.cacheClient.Get<CacheTest>({0})   ===> {1}", cacheKey, result != null);
		}
	}
}