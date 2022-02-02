using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    [Ignore("CI requires redis-server v3.2.0")]
    public class RedisGeoNativeClientTests
    {
        private readonly RedisNativeClient redis;

        public RedisGeoNativeClientTests()
        {
            redis = new RedisNativeClient(TestConfig.GeoHost);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            redis.Dispose();
        }

        [Test]
        public void Can_GeoAdd_and_GeoPos()
        {
            redis.FlushDb();
            var count = redis.GeoAdd("Sicily", 13.361389, 38.115556, "Palermo");
            Assert.That(count, Is.EqualTo(1));
            var results = redis.GeoPos("Sicily", "Palermo");

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Longitude, Is.EqualTo(13.361389).Within(.1));
            Assert.That(results[0].Latitude, Is.EqualTo(38.115556).Within(.1));
            Assert.That(results[0].Member, Is.EqualTo("Palermo"));
        }

        [Test]
        public void GeoPos_on_NonExistingMember_returns_no_results()
        {
            redis.FlushDb();
            var count = redis.GeoAdd("Sicily", 13.361389, 38.115556, "Palermo");
            var results = redis.GeoPos("Sicily", "NonExistingMember");
            Assert.That(results.Count, Is.EqualTo(0));

            results = redis.GeoPos("Sicily", "Palermo", "NonExistingMember");
            Assert.That(results.Count, Is.EqualTo(1));
        }

        [Test]
        public void Can_GeoAdd_and_GeoPos_multiple()
        {
            redis.FlushDb();
            var count = redis.GeoAdd("Sicily", 
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania"));
            Assert.That(count, Is.EqualTo(2));

            var results = redis.GeoPos("Sicily", "Palermo", "Catania");

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].Longitude, Is.EqualTo(13.361389).Within(.1));
            Assert.That(results[0].Latitude, Is.EqualTo(38.115556).Within(.1));
            Assert.That(results[0].Member, Is.EqualTo("Palermo"));

            Assert.That(results[1].Longitude, Is.EqualTo(15.087269).Within(.1));
            Assert.That(results[1].Latitude, Is.EqualTo(37.502669).Within(.1));
            Assert.That(results[1].Member, Is.EqualTo("Catania"));
        }

        [Test]
        public void Can_GeoDist()
        {
            redis.FlushDb();
            redis.GeoAdd("Sicily",
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania"));

            var distance = redis.GeoDist("Sicily", "Palermo", "Catania");
            Assert.That(distance, Is.EqualTo(166274.15156960039).Within(.1));
        }

        [Test]
        public void GeoDist_on_NonExistingMember_returns_NaN()
        {
            redis.FlushDb();
            redis.GeoAdd("Sicily",
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania"));

            var distance = redis.GeoDist("Sicily", "Palermo", "NonExistingMember");
            Assert.That(distance, Is.EqualTo(double.NaN));
        }

        [Test]
        public void Can_GeoHash()
        {
            redis.FlushDb();
            redis.GeoAdd("Sicily",
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania"));

            var hashes = redis.GeoHash("Sicily", "Palermo", "Catania");
            Assert.That(hashes[0], Is.EqualTo("sqc8b49rny0"));
            Assert.That(hashes[1], Is.EqualTo("sqdtr74hyu0"));

            hashes = redis.GeoHash("Sicily", "Palermo", "NonExistingMember", "Catania");
            Assert.That(hashes[0], Is.EqualTo("sqc8b49rny0"));
            Assert.That(hashes[1], Is.Null);
            Assert.That(hashes[2], Is.EqualTo("sqdtr74hyu0"));
        }

        [Test]
        public void Can_GeoRadius_default()
        {
            redis.FlushDb();
            redis.GeoAdd("Sicily",
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania"));

            var results = redis.GeoRadius("Sicily", 15, 37, 200, RedisGeoUnit.Kilometers);

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].Member, Is.EqualTo("Palermo"));
            Assert.That(results[0].Unit, Is.Null);
            Assert.That(results[1].Member, Is.EqualTo("Catania"));
            Assert.That(results[1].Unit, Is.Null);
        }

        [Test]
        public void Can_GeoRadiusByMember_default()
        {
            redis.FlushDb();
            redis.GeoAdd("Sicily",
                new RedisGeo(13.583333, 37.316667, "Agrigento"),
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania"));

            var results = redis.GeoRadiusByMember("Sicily", "Agrigento", 100, RedisGeoUnit.Kilometers);

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].Member, Is.EqualTo("Agrigento"));
            Assert.That(results[0].Unit, Is.Null);
            Assert.That(results[1].Member, Is.EqualTo("Palermo"));
            Assert.That(results[1].Unit, Is.Null);
        }

        [Test]
        public void Can_GeoRadius_WithCoord()
        {
            redis.FlushDb();
            redis.GeoAdd("Sicily",
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania"));

            var results = redis.GeoRadius("Sicily", 15, 37, 200, RedisGeoUnit.Kilometers, withCoords: true);

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].Member, Is.EqualTo("Palermo"));
            Assert.That(results[0].Unit, Is.EqualTo(RedisGeoUnit.Kilometers));
            Assert.That(results[0].Longitude, Is.EqualTo(13.361389).Within(.1));
            Assert.That(results[0].Latitude, Is.EqualTo(38.115556).Within(.1));

            Assert.That(results[1].Member, Is.EqualTo("Catania"));
            Assert.That(results[1].Unit, Is.EqualTo(RedisGeoUnit.Kilometers));
            Assert.That(results[1].Longitude, Is.EqualTo(15.087269).Within(.1));
            Assert.That(results[1].Latitude, Is.EqualTo(37.502669).Within(.1));
        }

        [Test]
        public void Can_GeoRadius_WithDist()
        {
            redis.FlushDb();
            redis.GeoAdd("Sicily",
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania"));

            var results = redis.GeoRadius("Sicily", 15, 37, 200, RedisGeoUnit.Kilometers, withDist: true);

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].Member, Is.EqualTo("Palermo"));
            Assert.That(results[0].Unit, Is.EqualTo(RedisGeoUnit.Kilometers));
            Assert.That(results[0].Distance, Is.EqualTo(190.4424).Within(.1));

            Assert.That(results[1].Member, Is.EqualTo("Catania"));
            Assert.That(results[1].Unit, Is.EqualTo(RedisGeoUnit.Kilometers));
            Assert.That(results[1].Distance, Is.EqualTo(56.4413).Within(.1));
        }

        [Test]
        public void Can_GeoRadius_WithCoord_WithDist_WithHash()
        {
            redis.FlushDb();
            redis.GeoAdd("Sicily",
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania"));

            var results = redis.GeoRadius("Sicily", 15, 37, 200, RedisGeoUnit.Kilometers,
                withCoords: true, withDist: true, withHash: true);

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].Member, Is.EqualTo("Palermo"));
            Assert.That(results[0].Unit, Is.EqualTo(RedisGeoUnit.Kilometers));
            Assert.That(results[0].Longitude, Is.EqualTo(13.361389).Within(.1));
            Assert.That(results[0].Latitude, Is.EqualTo(38.115556).Within(.1));
            Assert.That(results[0].Distance, Is.EqualTo(190.4424).Within(.1));
            Assert.That(results[0].Hash, Is.EqualTo(3479099956230698));

            Assert.That(results[1].Member, Is.EqualTo("Catania"));
            Assert.That(results[1].Unit, Is.EqualTo(RedisGeoUnit.Kilometers));
            Assert.That(results[1].Longitude, Is.EqualTo(15.087269).Within(.1));
            Assert.That(results[1].Latitude, Is.EqualTo(37.502669).Within(.1));
            Assert.That(results[1].Distance, Is.EqualTo(56.4413).Within(.1));
            Assert.That(results[1].Hash, Is.EqualTo(3479447370796909));
        }

        [Test]
        public void Can_GeoRadiusByMember_WithCoord_WithDist_WithHash()
        {
            redis.FlushDb();
            redis.GeoAdd("Sicily",
                new RedisGeo(13.583333, 37.316667, "Agrigento"),
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania"));

            var results = redis.GeoRadiusByMember("Sicily", "Agrigento", 100, RedisGeoUnit.Kilometers,
                withCoords: true, withDist: true, withHash: true);

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].Member, Is.EqualTo("Agrigento"));
            Assert.That(results[0].Unit, Is.EqualTo(RedisGeoUnit.Kilometers));
            Assert.That(results[0].Longitude, Is.EqualTo(13.583333).Within(.1));
            Assert.That(results[0].Latitude, Is.EqualTo(37.316667).Within(.1));
            Assert.That(results[0].Distance, Is.EqualTo(0));
            Assert.That(results[0].Hash, Is.EqualTo(3479030013248308));

            Assert.That(results[1].Member, Is.EqualTo("Palermo"));
            Assert.That(results[1].Unit, Is.EqualTo(RedisGeoUnit.Kilometers));
            Assert.That(results[1].Longitude, Is.EqualTo(13.361389).Within(.1));
            Assert.That(results[1].Latitude, Is.EqualTo(38.115556).Within(.1));
            Assert.That(results[1].Distance, Is.EqualTo(90.9778).Within(.1));
            Assert.That(results[1].Hash, Is.EqualTo(3479099956230698));
        }

        [Test]
        public void Can_GeoRadius_WithCoord_WithDist_WithHash_Count_and_Asc()
        {
            redis.FlushDb();
            redis.GeoAdd("Sicily",
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania"));

            var results = redis.GeoRadius("Sicily", 15, 37, 200, RedisGeoUnit.Kilometers,
                withCoords: true, withDist: true, withHash: true, count:1, asc:false);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Member, Is.EqualTo("Palermo"));
            Assert.That(results[0].Unit, Is.EqualTo(RedisGeoUnit.Kilometers));
            Assert.That(results[0].Longitude, Is.EqualTo(13.361389).Within(.1));
            Assert.That(results[0].Latitude, Is.EqualTo(38.115556).Within(.1));
            Assert.That(results[0].Distance, Is.EqualTo(190.4424).Within(.1));
            Assert.That(results[0].Hash, Is.EqualTo(3479099956230698));

             results = redis.GeoRadius("Sicily", 15, 37, 200, RedisGeoUnit.Kilometers,
                withCoords: true, withDist: true, withHash: true, count: 1, asc: true);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Member, Is.EqualTo("Catania"));
            Assert.That(results[0].Unit, Is.EqualTo(RedisGeoUnit.Kilometers));
            Assert.That(results[0].Longitude, Is.EqualTo(15.087269).Within(.1));
            Assert.That(results[0].Latitude, Is.EqualTo(37.502669).Within(.1));
            Assert.That(results[0].Distance, Is.EqualTo(56.4413).Within(.1));
            Assert.That(results[0].Hash, Is.EqualTo(3479447370796909));
        }
    }
}