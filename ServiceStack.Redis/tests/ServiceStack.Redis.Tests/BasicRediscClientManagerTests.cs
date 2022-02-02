using NUnit.Framework;

namespace ServiceStack.Redis.Tests
{
    public class BasicRediscClientManagerTests
        : RedisClientTestsBase
    {
        [Test]
        public void Can_select_db()
        {
            var redisManager = new BasicRedisClientManager("127.0.0.1");

            using (var client = redisManager.GetClient())
            {
                client.Db = 2;
                client.Set("db", 2);
            }

            using (var client = redisManager.GetClient())
            {
                client.Db = 3;
                client.Set("db", 3);
            }

            using (var client = redisManager.GetClient())
            {
                client.Db = 2;
                //((RedisClient)client).ChangeDb(2);
                var db = client.Get<int>("db");
                Assert.That(db, Is.EqualTo(2));
            }

            redisManager = new BasicRedisClientManager("127.0.0.1?db=3");
            using (var client = redisManager.GetClient())
            {
                var db = client.Get<int>("db");
                Assert.That(db, Is.EqualTo(3));
            }
        }
    }
}