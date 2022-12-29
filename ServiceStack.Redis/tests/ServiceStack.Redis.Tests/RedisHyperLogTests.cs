using System;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Ignore("Integration")]
    public class RedisHyperLogTests
    {
        [Test]
        public void Can_Add_to_Hyperlog()
        {
            var redis = new RedisClient("10.0.0.14");
            redis.FlushAll();

            redis.AddToHyperLog("hyperlog", "a", "b", "c");
            redis.AddToHyperLog("hyperlog", "c", "d");

            var count = redis.CountHyperLog("hyperlog");

            Assert.That(count, Is.EqualTo(4));

            redis.AddToHyperLog("hyperlog2", "c", "d", "e", "f");

            redis.MergeHyperLogs("hypermerge", "hyperlog", "hyperlog2");

            var mergeCount = redis.CountHyperLog("hypermerge");

            Assert.That(mergeCount, Is.EqualTo(6));
        }

        [Test]
        public void Test_on_old_redisserver()
        {
            var redis = new RedisClient("10.0.0.14");
            //var redis = new RedisClient();
            redis.FlushAll();

            //redis.ExpireEntryIn("key", TimeSpan.FromDays(14));

            redis.Set("key", "value", TimeSpan.FromDays(14));

            var value = redis.Get("key");

            value.FromUtf8Bytes().Print();
        }
    }
}