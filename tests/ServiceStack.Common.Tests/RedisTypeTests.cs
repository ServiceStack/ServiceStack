using NUnit.Framework;
using ServiceStack.Redis;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class RedisTypeTests
    {
        [Test]
        public void Can_parse_RedisGeo()
        {
            var palermo = new RedisGeo(13.361389, 38.115556, "Palermo");
            var geoString = palermo.ToString();
            Assert.That(geoString, Is.EqualTo("13.361389 38.115556 Palermo"));
            Assert.That(new RedisGeo(geoString), Is.EqualTo(palermo));
        }
    }
}