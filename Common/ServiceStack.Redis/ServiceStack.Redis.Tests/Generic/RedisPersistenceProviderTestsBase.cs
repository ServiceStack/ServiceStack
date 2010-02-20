using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Common.Utils;
using ServiceStack.Redis.Generic;

namespace ServiceStack.Redis.Tests.Generic
{
	[TestFixture]
	public abstract class RedisPersistenceProviderTestsBase<T>
	{
		protected abstract IModelFactory<T> Factory { get; }

		private RedisClient client;
		private IRedisTypedClient<T> redis;

		[SetUp]
		public void SetUp()
		{
			if (client != null)
			{
				client.Dispose();
				client = null;
			}
			client = new RedisClient(TestConfig.SingleHost);
			client.FlushAll();

			redis = client.GetTypedClient<T>();
		}

		[Test]
		public void Can_Store_and_GetById_ModelWithIdAndName()
		{
			const int modelId = 1;
			var to = Factory.CreateInstance(modelId);
			redis.Store(to);

			var from = redis.GetById(to.GetId().ToString());

			Factory.AssertIsEqual(to, from);
		}

		[Test]
		public void Can_StoreAll_and_GetByIds_ModelWithIdAndName()
		{
			var tos = Factory.CreateList();
			var ids = tos.ConvertAll(x => x.GetId().ToString());

			redis.StoreAll(tos);

			var froms = redis.GetByIds(ids);
			var fromIds = froms.ConvertAll(x => x.GetId().ToString());

			Assert.That(fromIds, Is.EquivalentTo(ids));
		}

		[Test]
		public void Can_Delete_ModelWithIdAndName()
		{
			var tos = Factory.CreateList();
			var ids = tos.ConvertAll(x => x.GetId().ToString());

			redis.StoreAll(tos);

			var deleteIds = new[] { ids[1], ids[3] };

			redis.DeleteByIds(deleteIds);

			var froms = redis.GetByIds(ids);
			var fromIds = froms.ConvertAll(x => x.GetId().ToString());

			var expectedIds = ids.Where(x => !deleteIds.Contains(x))
				.ToList().ConvertAll(x => x.ToString());

			Assert.That(fromIds, Is.EquivalentTo(expectedIds));
		}

	}
}