using System;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisPasswordTests
    {
        [Ignore("Integration")]
        [Test]
        public void Can_connect_to_Replicas_and_Masters_with_Password()
        {
            var factory = new PooledRedisClientManager(
                readWriteHosts: new[] {"pass@10.0.0.59:6379"},
                readOnlyHosts: new[] {"pass@10.0.0.59:6380"});

            using var readWrite = factory.GetClient();
            using var readOnly = factory.GetReadOnlyClient();
            readWrite.SetValue("Foo", "Bar");
            var value = readOnly.GetValue("Foo");

            Assert.That(value, Is.EqualTo("Bar"));
        }

        [Test]
        public void Passwords_are_not_leaked_in_exception_messages()
        {
            const string password = "yesterdayspassword";

            Assert.Throws<RedisResponseException>(() => {
                    try
                    {
                        var connString = password + "@" + TestConfig.SingleHost + "?RetryTimeout=2000";
                        // redis will throw when using password and it's not configured
                        var factory = new PooledRedisClientManager(connString); 
                        using var redis = factory.GetClient();
                        redis.SetValue("Foo", "Bar");
                    }
                    catch (RedisResponseException ex)
                    {
                        Assert.That(ex.Message, Is.Not.Contains(password));
                        throw;
                    }
                    catch (TimeoutException tex)
                    {
                        Assert.That(tex.InnerException.Message, Is.Not.Contains(password));
                        throw tex.InnerException;
                    }
                },
                "Expected an exception after Redis AUTH command; try using a password that doesn't match.");
        }
    }
}