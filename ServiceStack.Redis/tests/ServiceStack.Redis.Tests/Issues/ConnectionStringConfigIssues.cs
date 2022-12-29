using NUnit.Framework;

namespace ServiceStack.Redis.Tests.Issues
{
    public class ConnectionStringConfigIssues
    {
        [Test]
        public void Can_use_password_with_equals()
        {
            var connString = "127.0.0.1?password=" + "p@55w0rd=".UrlEncode();

            var config = connString.ToRedisEndpoint();
            Assert.That(config.Password, Is.EqualTo("p@55w0rd="));
        }

        [Test, Ignore("Requires redis-server configured with 'requirepass p@55w0rd='")]
        public void Can_connect_to_redis_with_password_with_equals()
        {
            var connString = "127.0.0.1?password=" + "p@55w0rd=".UrlEncode();
            var redisManager = new PooledRedisClientManager(connString);
            using (var redis = redisManager.GetClient())
            {
                Assert.That(redis.Password, Is.EqualTo("p@55w0rd="));
            }
        }
    }
}