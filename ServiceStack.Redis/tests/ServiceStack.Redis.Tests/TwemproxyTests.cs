using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Ignore("Integration")]
    public class TwemproxyTests
    {
        [Test]
        public void Can_connect_to_twemproxy()
        {
            var redis = new RedisClient("10.0.0.14", 22121)
            {
                //ServerVersionNumber = 2611
            };
            //var redis = new RedisClient("10.0.0.14");
            redis.SetValue("foo", "bar");
            var foo = redis.GetValue("foo");

            Assert.That(foo, Is.EqualTo("bar"));
        }
    }
}