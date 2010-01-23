using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Redis.Tests
{
	[TestFixture]
	public class RedisPersistenceProviderTests
	{
		[Test]
		public void Can_Store_and_GetById_ModelWithIdAndName()
		{
			using (var redis = new RedisPersistenceProvider())
			{
				const int modelId = 1;
				var to = ModelWithIdAndName.Create(modelId);
				redis.Store(to);

				var from = redis.GetById<ModelWithIdAndName>(modelId);

				ModelWithIdAndName.AssertIsEqual(to, from);
			}
		}

		[Test]
		public void Can_StoreAll_and_GetByIds_ModelWithIdAndName()
		{
			using (var redis = new RedisPersistenceProvider())
			{
				var ids = new[] { 1, 2, 3, 4, 5 };
				var tos = ids.ConvertAll(x => ModelWithIdAndName.Create(x));

				redis.StoreAll(tos);

				var froms = redis.GetByIds<ModelWithIdAndName>(ids);
				var fromIds = froms.ConvertAll(x => x.Id);

				Assert.That(fromIds, Is.EquivalentTo(ids));
			}
		}

		[Test]
		public void Can_Delete_ModelWithIdAndName()
		{
			using (var redis = new RedisPersistenceProvider())
			{
				var ids = new List<int> { 1, 2, 3, 4, 5 };
				var tos = ids.ConvertAll(x => ModelWithIdAndName.Create(x));

				redis.StoreAll(tos);

				var deleteIds = new List<int> { 2, 4 };

				redis.DeleteByIds<ModelWithIdAndName>(deleteIds);

				var froms = redis.GetByIds<ModelWithIdAndName>(ids);
				var fromIds = froms.ConvertAll(x => x.Id);

				var expectedIds = ids.Where(x => !deleteIds.Contains(x)).ToList();

				Assert.That(fromIds, Is.EquivalentTo(expectedIds));
			}
		}

	}

}