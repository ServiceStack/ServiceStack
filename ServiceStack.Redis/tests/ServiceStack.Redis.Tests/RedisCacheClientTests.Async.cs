using System;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Caching;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    [Category("Async")]
    public class RedisCacheClientTestsAsync
    {
        private ICacheClientAsync cacheClient;

        [SetUp]
        public async Task OnBeforeEachTest()
        {
            if (cacheClient is object)
                await cacheClient.DisposeAsync();

            cacheClient = new RedisClient(TestConfig.SingleHost);
            await cacheClient.FlushAllAsync();
        }

        [Test]
        public async Task Get_non_existant_value_returns_null()
        {
            var model = ModelWithIdAndName.Create(1);
            var cacheKey = model.CreateUrn();
            var existingModel = await cacheClient.GetAsync<ModelWithIdAndName>(cacheKey);
            Assert.That(existingModel, Is.Null);
        }

        [Test]
        public async Task Get_non_existant_generic_value_returns_null()
        {
            var model = ModelWithIdAndName.Create(1);
            var cacheKey = model.CreateUrn();
            var existingModel = await cacheClient.GetAsync<ModelWithIdAndName>(cacheKey);
            Assert.That(existingModel, Is.Null);
        }

        [Test]
        public async Task Can_store_and_get_model()
        {
            var model = ModelWithIdAndName.Create(1);
            var cacheKey = model.CreateUrn();
            await cacheClient.SetAsync(cacheKey, model);

            var existingModel = await cacheClient.GetAsync<ModelWithIdAndName>(cacheKey);
            ModelWithIdAndName.AssertIsEqual(existingModel, model);
        }

        [Test]
        public async Task Can_store_null_model()
        {
            await cacheClient.SetAsync<ModelWithIdAndName>("test-key", null);
        }

        [Test]
        public async Task Can_Set_and_Get_key_with_all_byte_values()
        {
            const string key = "bytesKey";

            var value = new byte[256];
            for (var i = 0; i < value.Length; i++)
            {
                value[i] = (byte)i;
            }

            await cacheClient.SetAsync(key, value);
            var resultValue = await cacheClient.GetAsync<byte[]>(key);

            Assert.That(resultValue, Is.EquivalentTo(value));
        }

        [Test]
        public async Task Can_Replace_By_Pattern()
        {
            var model = ModelWithIdAndName.Create(1);
            string modelKey = "model:" + model.CreateUrn();
            await cacheClient.AddAsync(modelKey, model);

            model = ModelWithIdAndName.Create(2);
            string modelKey2 = "xxmodelxx:" + model.CreateUrn();
            await cacheClient.AddAsync(modelKey2, model);

            string s = "this is a string";
            await cacheClient.AddAsync("string1", s);

            var removable = (IRemoveByPatternAsync)cacheClient;
            await removable.RemoveByPatternAsync("*model*");

            ModelWithIdAndName result = await cacheClient.GetAsync<ModelWithIdAndName>(modelKey);
            Assert.That(result, Is.Null);

            result = await cacheClient.GetAsync<ModelWithIdAndName>(modelKey2);
            Assert.That(result, Is.Null);

            string result2 = await cacheClient.GetAsync<string>("string1");
            Assert.That(result2, Is.EqualTo(s));

            await removable.RemoveByPatternAsync("string*");

            result2 = await cacheClient.GetAsync<string>("string1");
            Assert.That(result2, Is.Null);
        }

        [Test]
        public async Task Can_GetTimeToLive()
        {
            var model = ModelWithIdAndName.Create(1);
            string key = "model:" + model.CreateUrn();
            await cacheClient.AddAsync(key, model);

            var ttl = await cacheClient.GetTimeToLiveAsync(key);
            Assert.That(ttl, Is.EqualTo(TimeSpan.MaxValue));

            await cacheClient.SetAsync(key, model, expiresIn: TimeSpan.FromSeconds(10));
            ttl = await cacheClient.GetTimeToLiveAsync(key);
            Assert.That(ttl.Value, Is.GreaterThanOrEqualTo(TimeSpan.FromSeconds(9)));
            Assert.That(ttl.Value, Is.LessThanOrEqualTo(TimeSpan.FromSeconds(10)));

            await cacheClient.RemoveAsync(key);
            ttl = await cacheClient.GetTimeToLiveAsync(key);
            Assert.That(ttl, Is.Null);
        }

        [Test]
        public async Task Can_increment_and_reset_values()
        {
            await using var client = await new RedisManagerPool(TestConfig.SingleHost).GetCacheClientAsync();
            
            Assert.That(await client.IncrementAsync("incr:counter", 10), Is.EqualTo(10));
            await client.SetAsync("incr:counter", 0);
            Assert.That(await client.IncrementAsync("incr:counter", 10), Is.EqualTo(10));
        }
    }
}