using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Generic;

namespace ServiceStack.Redis.Tests.Generic
{
    [TestFixture, Category("Async")]
    public abstract class RedisPersistenceProviderTestsBaseAsync<T>
    {
        protected abstract IModelFactory<T> Factory { get; }

        private IRedisClientAsync client;
        private IRedisTypedClientAsync<T> redis;

        [SetUp]
        public async Task SetUp()
        {
            if (client is object)
            {
                await client.DisposeAsync();
                client = null;
            }
            client = new RedisClient(TestConfig.SingleHost).ForAsyncOnly();
            await client.FlushAllAsync();

            redis = client.As<T>();
        }

        [Test]
        public async Task Can_Store_and_GetById_ModelWithIdAndName()
        {
            const int modelId = 1;
            var to = Factory.CreateInstance(modelId);
            await redis.StoreAsync(to);

            var from = await redis.GetByIdAsync(to.GetId().ToString());

            Factory.AssertIsEqual(to, from);
        }

        [Test]
        public async Task Can_StoreAll_and_GetByIds_ModelWithIdAndName()
        {
            var tos = Factory.CreateList();
            var ids = tos.ConvertAll(x => x.GetId().ToString());

            await redis.StoreAllAsync(tos);

            var froms = await redis.GetByIdsAsync(ids);
            var fromIds = froms.Map(x => x.GetId().ToString());

            Assert.That(fromIds, Is.EquivalentTo(ids));
        }

        [Test]
        public async Task Can_Delete_ModelWithIdAndName()
        {
            var tos = Factory.CreateList();
            var ids = tos.ConvertAll(x => x.GetId().ToString());

            await redis.StoreAllAsync(tos);

            var deleteIds = new[] { ids[1], ids[3] };

            await redis.DeleteByIdsAsync(deleteIds);

            var froms = await redis.GetByIdsAsync(ids);
            var fromIds = froms.Map(x => x.GetId().ToString());

            var expectedIds = ids.Where(x => !deleteIds.Contains(x))
                .ToList().ConvertAll(x => x.ToString());

            Assert.That(fromIds, Is.EquivalentTo(expectedIds));
        }

        [Test]
        public async Task Can_DeleteAll()
        {
            var tos = Factory.CreateList();
            await redis.StoreAllAsync(tos);

            var all = await redis.GetAllAsync();

            Assert.That(all.Count, Is.EqualTo(tos.Count));

            await redis.DeleteAllAsync();

            all = await redis.GetAllAsync();

            Assert.That(all.Count, Is.EqualTo(0));
        }

    }
}