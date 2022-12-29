using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Integration"), Category("Async")]
    public class RedisPersistenceProviderTestsAsync
    {
        [Test]
        public async Task Can_Store_and_GetById_ModelWithIdAndName()
        {
            await using IRedisClientAsync redis = new RedisClient(TestConfig.SingleHost);
            const int modelId = 1;
            var to = ModelWithIdAndName.Create(modelId);
            await redis.StoreAsync(to);

            var from = await redis.GetByIdAsync<ModelWithIdAndName>(modelId);

            ModelWithIdAndName.AssertIsEqual(to, from);
        }

        [Test]
        public async Task Can_StoreAll_and_GetByIds_ModelWithIdAndName()
        {
            await using IRedisClientAsync redis = new RedisClient(TestConfig.SingleHost);
            
            var ids = new[] { 1, 2, 3, 4, 5 };
            var tos = ids.Map(ModelWithIdAndName.Create);

            await redis.StoreAllAsync(tos);

            var froms = await redis.GetByIdsAsync<ModelWithIdAndName>(ids);
            var fromIds = froms.Map(x => x.Id);

            Assert.That(fromIds, Is.EquivalentTo(ids));
        }

        [Test]
        public async Task Can_Delete_ModelWithIdAndName()
        {
            await using IRedisClientAsync redis = new RedisClient(TestConfig.SingleHost);
            var ids = new List<int> { 1, 2, 3, 4, 5 };
            var tos = ids.ConvertAll(ModelWithIdAndName.Create);

            await redis.StoreAllAsync(tos);

            var deleteIds = new List<int> { 2, 4 };

            await redis.DeleteByIdsAsync<ModelWithIdAndName>(deleteIds);

            var froms = await redis.GetByIdsAsync<ModelWithIdAndName>(ids);
            var fromIds = froms.Map(x => x.Id);

            var expectedIds = ids.Where(x => !deleteIds.Contains(x)).ToList();

            Assert.That(fromIds, Is.EquivalentTo(expectedIds));
        }

    }

}