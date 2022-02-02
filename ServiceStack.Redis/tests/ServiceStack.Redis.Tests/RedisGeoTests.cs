using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    [Ignore("CI requires redis-server v3.2.0")]
    public class RedisGeoTests
    {
        private readonly RedisClient redis;

        public RedisGeoTests()
        {
            redis = new RedisClient(TestConfig.GeoHost);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            redis.Dispose();
        }

        [Test]
        public void Can_AddGeoMember_and_GetGeoCoordinates()
        {
            redis.FlushDb();
            var count = redis.AddGeoMember("Sicily", 13.361389, 38.115556, "Palermo");
            Assert.That(count, Is.EqualTo(1));
            var results = redis.GetGeoCoordinates("Sicily", "Palermo");

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Longitude, Is.EqualTo(13.361389).Within(.1));
            Assert.That(results[0].Latitude, Is.EqualTo(38.115556).Within(.1));
            Assert.That(results[0].Member, Is.EqualTo("Palermo"));
        }

        [Test]
        public void GetGeoCoordinates_on_NonExistingMember_returns_no_results()
        {
            redis.FlushDb();
            var count = redis.AddGeoMember("Sicily", 13.361389, 38.115556, "Palermo");
            var results = redis.GetGeoCoordinates("Sicily", "NonExistingMember");
            Assert.That(results.Count, Is.EqualTo(0));

            results = redis.GetGeoCoordinates("Sicily", "Palermo", "NonExistingMember");
            Assert.That(results.Count, Is.EqualTo(1));
        }

        [Test]
        public void Can_AddGeoMembers_and_GetGeoCoordinates_multiple()
        {
            redis.FlushDb();
            var count = redis.AddGeoMembers("Sicily", 
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania"));
            Assert.That(count, Is.EqualTo(2));

            var results = redis.GetGeoCoordinates("Sicily", "Palermo", "Catania");

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].Longitude, Is.EqualTo(13.361389).Within(.1));
            Assert.That(results[0].Latitude, Is.EqualTo(38.115556).Within(.1));
            Assert.That(results[0].Member, Is.EqualTo("Palermo"));

            Assert.That(results[1].Longitude, Is.EqualTo(15.087269).Within(.1));
            Assert.That(results[1].Latitude, Is.EqualTo(37.502669).Within(.1));
            Assert.That(results[1].Member, Is.EqualTo("Catania"));
        }

        [Test]
        public void Can_CalculateDistanceBetweenGeoMembers()
        {
            redis.FlushDb();
            redis.AddGeoMembers("Sicily",
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania"));

            var distance = redis.CalculateDistanceBetweenGeoMembers("Sicily", "Palermo", "Catania");
            Assert.That(distance, Is.EqualTo(166274.15156960039).Within(.1));
        }

        [Test]
        public void CalculateDistanceBetweenGeoMembers_on_NonExistingMember_returns_NaN()
        {
            redis.FlushDb();
            redis.AddGeoMembers("Sicily",
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania"));

            var distance = redis.CalculateDistanceBetweenGeoMembers("Sicily", "Palermo", "NonExistingMember");
            Assert.That(distance, Is.EqualTo(double.NaN));
        }

        [Test]
        public void Can_GetGeohashes()
        {
            redis.FlushDb();
            redis.AddGeoMembers("Sicily",
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania"));

            var hashes = redis.GetGeohashes("Sicily", "Palermo", "Catania");
            Assert.That(hashes[0], Is.EqualTo("sqc8b49rny0"));
            Assert.That(hashes[1], Is.EqualTo("sqdtr74hyu0"));

            hashes = redis.GetGeohashes("Sicily", "Palermo", "NonExistingMember", "Catania");
            Assert.That(hashes[0], Is.EqualTo("sqc8b49rny0"));
            Assert.That(hashes[1], Is.Null);
            Assert.That(hashes[2], Is.EqualTo("sqdtr74hyu0"));
        }

        [Test]
        public void Can_FindGeoMembersInRadius()
        {
            redis.FlushDb();
            redis.GeoAdd("Sicily",
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania"));

            var results = redis.FindGeoMembersInRadius("Sicily", 15, 37, 200, RedisGeoUnit.Kilometers);

            Assert.That(results.Length, Is.EqualTo(2));
            Assert.That(results[0], Is.EqualTo("Palermo"));
            Assert.That(results[1], Is.EqualTo("Catania"));
        }

        [Test]
        public void Can_GeoRadiusByMember()
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
        public void Can_FindGeoResultsInRadius()
        {
            redis.FlushDb();
            redis.GeoAdd("Sicily",
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania"));

            var results = redis.FindGeoResultsInRadius("Sicily", 15, 37, 200, RedisGeoUnit.Kilometers);

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
        public void Can_FindGeoResultsInRadius_by_Member()
        {
            redis.FlushDb();
            redis.GeoAdd("Sicily",
                new RedisGeo(13.583333, 37.316667, "Agrigento"),
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania"));

            var results = redis.FindGeoResultsInRadius("Sicily", "Agrigento", 100, RedisGeoUnit.Kilometers);

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

            var results = redis.FindGeoResultsInRadius("Sicily", 15, 37, 200, RedisGeoUnit.Kilometers,
                 count:1, sortByNearest:false);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Member, Is.EqualTo("Palermo"));
            Assert.That(results[0].Unit, Is.EqualTo(RedisGeoUnit.Kilometers));
            Assert.That(results[0].Longitude, Is.EqualTo(13.361389).Within(.1));
            Assert.That(results[0].Latitude, Is.EqualTo(38.115556).Within(.1));
            Assert.That(results[0].Distance, Is.EqualTo(190.4424).Within(.1));
            Assert.That(results[0].Hash, Is.EqualTo(3479099956230698));

            results = redis.FindGeoResultsInRadius("Sicily", 15, 37, 200, RedisGeoUnit.Kilometers,
                 count: 1, sortByNearest:true);

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