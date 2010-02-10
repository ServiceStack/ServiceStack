using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.CacheAccess;
using ServiceStack.Common;
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
			cacheClient = new RedisCacheClient();
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
	}
}