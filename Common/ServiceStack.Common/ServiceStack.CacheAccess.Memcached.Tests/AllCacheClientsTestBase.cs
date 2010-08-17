using NUnit.Framework;

namespace ServiceStack.CacheAccess.Memcached.Tests
{
	public abstract class AllCacheClientsTestBase
	{
		public class TestConfig
		{
			public const string SingleHost = "localhost";
			public static readonly string[] MasterHosts = new[] { "localhost" };
		}

		protected static void AssertGetAll(ICacheClient cacheClient)
		{
			AssertCacheClientMissingModelValuesAsKeysWithNullValues(cacheClient);
			AssertCacheClientIntModelValuesAsKeysWithNullValues(cacheClient);
		}

		public void AssertGetSetIntValue(ICacheClient cacheClient)
		{
			cacheClient.Set("test:intkey1", 1);
			var value = cacheClient.Get<int>("test:intkey1");
			Assert.That(value, Is.EqualTo(1));
		}

		public void AssertGetSetIntValue(IMemcachedClient cacheClient)
		{
			cacheClient.Set("test:intkey1", 1);
			var value = cacheClient.Get("test:intkey1");
			Assert.That(value, Is.EqualTo(1));
		}

		protected static void AssertCacheClientIntModelValuesAsKeysWithNullValues(
			ICacheClient cacheClient)
		{
			var allKeys = new[] { "test:intkey1", "test:intkey2", "test:intkey3" };
			var expectedValues = new[] { 1, default(int), 3 };
			cacheClient.RemoveAll(allKeys);

			cacheClient.Set(allKeys[0], expectedValues[0]);
			cacheClient.Set(allKeys[2], expectedValues[2]);

			var keyValues = cacheClient.GetAll<int>(allKeys);
			Assert.That(keyValues, Has.Count.EqualTo(expectedValues.Length));

			for (var keyIndex = 0; keyIndex < expectedValues.Length; keyIndex++)
			{
				var key = allKeys[keyIndex];
				var keyValue = keyValues[key];
				Assert.That(keyValue, Is.EqualTo(expectedValues[keyIndex]));
			}
		}

		protected static void AssertCacheClientMissingModelValuesAsKeysWithNullValues(
			ICacheClient cacheClient)
		{
			var allKeys = new[] { "test:modelkey1", "test:modelkey2", "test:modelkey3" };
			var expectedValues = new[] { ModelWithIdAndName.Create(1), null, ModelWithIdAndName.Create(1) };
			cacheClient.Set(allKeys[0], expectedValues[0]);
			cacheClient.Set(allKeys[2], expectedValues[2]);

			var keyValues = cacheClient.GetAll<ModelWithIdAndName>(allKeys);
			Assert.That(keyValues, Has.Count.EqualTo(expectedValues.Length));

			for (var keyIndex = 0; keyIndex < expectedValues.Length; keyIndex++)
			{
				var key = allKeys[keyIndex];
				var keyValue = keyValues[key];

				ModelWithIdAndName.AssertIsEqual(keyValue, expectedValues[keyIndex]);
			}
		}
	}
}