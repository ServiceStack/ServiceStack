#if !NETCORE //TODO: find out why fails to build in .netcoreapp1.1

using System;
using NUnit.Framework;
using ServiceStack.Caching;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisCacheClientTests
    {
        private ICacheClientExtended cacheClient;

        [SetUp]
        public void OnBeforeEachTest()
        {
            if (cacheClient != null)
                cacheClient.Dispose();

            cacheClient = new RedisClient(TestConfig.SingleHost);
            cacheClient.FlushAll();
        }

        [Test]
        public void Get_non_existant_value_returns_null()
        {
            var model = ModelWithIdAndName.Create(1);
            var cacheKey = model.CreateUrn();
            var existingModel = cacheClient.Get<ModelWithIdAndName>(cacheKey);
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
        public void Can_store_null_model()
        {
            cacheClient.Set<ModelWithIdAndName>("test-key", null);
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
            var resultValue = cacheClient.Get<byte[]>(key);

            Assert.That(resultValue, Is.EquivalentTo(value));
        }

        [Test]
        public void Can_Replace_By_Pattern()
        {
            var model = ModelWithIdAndName.Create(1);
            string modelKey = "model:" + model.CreateUrn();
            cacheClient.Add(modelKey, model);

            model = ModelWithIdAndName.Create(2);
            string modelKey2 = "xxmodelxx:" + model.CreateUrn();
            cacheClient.Add(modelKey2, model);

            string s = "this is a string";
            cacheClient.Add("string1", s);

            cacheClient.RemoveByPattern("*model*");

            ModelWithIdAndName result = cacheClient.Get<ModelWithIdAndName>(modelKey);
            Assert.That(result, Is.Null);

            result = cacheClient.Get<ModelWithIdAndName>(modelKey2);
            Assert.That(result, Is.Null);

            string result2 = cacheClient.Get<string>("string1");
            Assert.That(result2, Is.EqualTo(s));

            cacheClient.RemoveByPattern("string*");

            result2 = cacheClient.Get<string>("string1");
            Assert.That(result2, Is.Null);
        }

        [Test]
        public void Can_GetTimeToLive()
        {
            var model = ModelWithIdAndName.Create(1);
            string key = "model:" + model.CreateUrn();
            cacheClient.Add(key, model);

            var ttl = cacheClient.GetTimeToLive(key);
            Assert.That(ttl, Is.EqualTo(TimeSpan.MaxValue));

            cacheClient.Set(key, model, expiresIn: TimeSpan.FromSeconds(10));
            ttl = cacheClient.GetTimeToLive(key);
            Assert.That(ttl.Value, Is.GreaterThanOrEqualTo(TimeSpan.FromSeconds(9)));
            Assert.That(ttl.Value, Is.LessThanOrEqualTo(TimeSpan.FromSeconds(10)));

            cacheClient.Remove(key);
            ttl = cacheClient.GetTimeToLive(key);
            Assert.That(ttl, Is.Null);
        }

        [Test]
        public void Can_increment_and_reset_values()
        {
            using (var client = new RedisManagerPool(TestConfig.SingleHost).GetCacheClient())
            {
                Assert.That(client.Increment("incr:counter", 10), Is.EqualTo(10));
                client.Set("incr:counter", 0);
                Assert.That(client.Increment("incr:counter", 10), Is.EqualTo(10));
            }
        }
    }
}

#endif