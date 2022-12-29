using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Integration")]
    public class RedisPersistenceProviderTests
    {
        [Test]
        public void Can_Store_and_GetById_ModelWithIdAndName()
        {
            using (var redis = new RedisClient(TestConfig.SingleHost))
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
            using (var redis = new RedisClient(TestConfig.SingleHost))
            {
                var ids = new[] { 1, 2, 3, 4, 5 };
                var tos = ids.Map(ModelWithIdAndName.Create);

                redis.StoreAll(tos);

                var froms = redis.GetByIds<ModelWithIdAndName>(ids);
                var fromIds = froms.Map(x => x.Id);

                Assert.That(fromIds, Is.EquivalentTo(ids));
            }
        }

        [Test]
        public void Can_Delete_ModelWithIdAndName()
        {
            using (var redis = new RedisClient(TestConfig.SingleHost))
            {
                var ids = new List<int> { 1, 2, 3, 4, 5 };
                var tos = ids.ConvertAll(ModelWithIdAndName.Create);

                redis.StoreAll(tos);

                var deleteIds = new List<int> { 2, 4 };

                redis.DeleteByIds<ModelWithIdAndName>(deleteIds);

                var froms = redis.GetByIds<ModelWithIdAndName>(ids);
                var fromIds = froms.Map(x => x.Id);

                var expectedIds = ids.Where(x => !deleteIds.Contains(x)).ToList();

                Assert.That(fromIds, Is.EquivalentTo(expectedIds));
            }
        }

    }

}