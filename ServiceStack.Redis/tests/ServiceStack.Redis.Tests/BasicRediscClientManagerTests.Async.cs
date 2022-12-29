using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests
{
    public class BasicRediscClientManagerTestsAsync
        : RedisClientTestsBaseAsync
    {
        [Test]
        public async Task Can_select_db()
        {
            var redisManager = new BasicRedisClientManager("127.0.0.1");

            await using (var client = await redisManager.GetClientAsync())
            {
                await client.SelectAsync(2);
                await client.SetAsync("db", 2);
            }

            await using (var client = await redisManager.GetClientAsync())
            {
                await client.SelectAsync(3);
                await client.SetAsync("db", 3);
            }

            await using (var client = await redisManager.GetClientAsync())
            {
                await client.SelectAsync(2);
                //((RedisClient)client).ChangeDb(2);
                var db = await client.GetAsync<int>("db");
                Assert.That(db, Is.EqualTo(2));
            }

            redisManager = new BasicRedisClientManager("127.0.0.1?db=3");
            await using (var client = await redisManager.GetClientAsync())
            {
                var db = await client.GetAsync<int>("db");
                Assert.That(db, Is.EqualTo(3));
            }
        }
    }
}