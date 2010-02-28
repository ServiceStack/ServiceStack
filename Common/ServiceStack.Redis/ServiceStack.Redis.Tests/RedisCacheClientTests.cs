using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Common.Utils;

namespace ServiceStack.Redis.Tests
{
	[TestFixture]
	public class RedisCacheClientTests
	{
		private ICacheClient cacheClient;

		[SetUp]
		public void OnBeforeEachTest()
		{
			if (cacheClient != null)
				cacheClient.Dispose();

			cacheClient = new RedisCacheClient(TestConfig.SingleHost);
			cacheClient.FlushAll();
		}

		[Test]
		public void Get_non_existant_value_returns_null()
		{
			var model = ModelWithIdAndName.Create(1);
			var cacheKey = model.CreateUrn();
			var existingModel = cacheClient.Get(cacheKey);
			Assert.That(existingModel, Is.Null);
		}


		[Test]
		public void Get_non_existant_generic_value_returns_null()
		{
			var model = ModelWithIdAndName.Create(1);
			var cacheKey = model.CreateUrn();
			var existingModel = cacheClient.Get<ModelWithIdAndName>(cacheKey);
			Assert.That(existingModel, Is.Null);
		}

		[Test]
		public void Can_store_and_get_model()
		{
			var model = ModelWithIdAndName.Create(1);
			var cacheKey = model.CreateUrn();
			cacheClient.Set(cacheKey, model);

			var existingModel = cacheClient.Get<ModelWithIdAndName>(cacheKey);
			ModelWithIdAndName.AssertIsEqual(existingModel, model);
		}

		[Test]
		public void Can_Set_and_Get_key_with_all_byte_values()
		{
			const string key = "bytesKey";

			var value = new byte[256];
			for (var i = 0; i < value.Length; i++)
			{
				value[i] = (byte)i;
			}

			cacheClient.Set(key, value);
			var resultValue = cacheClient.Get(key);

			Assert.That(resultValue, Is.EquivalentTo(value));
		}
	}
}