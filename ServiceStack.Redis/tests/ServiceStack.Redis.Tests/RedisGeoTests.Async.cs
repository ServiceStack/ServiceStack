using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Async")]
    [Ignore("CI requires redis-server v3.2.0")]
    public class RedisGeoTestsAsync
    {
        private readonly IRedisClientAsync redis;

        public RedisGeoTestsAsync()
        {
            redis = new RedisClient(TestConfig.GeoHost);
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            if (redis is object)
            {
                await redis.DisposeAsync();
            }
        }

        [Test]
        public async Task Can_AddGeoMember_and_GetGeoCoordinates()
        {
            await redis.FlushDbAsync();
            var count = await redis.AddGeoMemberAsync("Sicily", 13.361389, 38.115556, "Palermo");
            Assert.That(count, Is.EqualTo(1));
            var results = await redis.GetGeoCoordinatesAsync("Sicily", new[] { "Palermo" });

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Longitude, Is.EqualTo(13.361389).Within(.1));
            Assert.That(results[0].Latitude, Is.EqualTo(38.115556).Within(.1));
            Assert.That(results[0].Member, Is.EqualTo("Palermo"));
        }

        [Test]
        public async Task GetGeoCoordinates_on_NonExistingMember_returns_no_results()
        {
            await redis.FlushDbAsync();
            var count = await redis.AddGeoMemberAsync("Sicily", 13.361389, 38.115556, "Palermo");
            var results = await redis.GetGeoCoordinatesAsync("Sicily", new[] { "NonExistingMember" });
            Assert.That(results.Count, Is.EqualTo(0));

            results = await redis.GetGeoCoordinatesAsync("Sicily", new[] { "Palermo", "NonExistingMember" });
            Assert.That(results.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task Can_AddGeoMembers_and_GetGeoCoordinates_multiple()
        {
            await redis.FlushDbAsync();
            var count = await redis.AddGeoMembersAsync("Sicily", new[] {
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania")
            });
            Assert.That(count, Is.EqualTo(2));

            var results = await redis.GetGeoCoordinatesAsync("Sicily", new[] { "Palermo", "Catania" });

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].Longitude, Is.EqualTo(13.361389).Within(.1));
            Assert.That(results[0].Latitude, Is.EqualTo(38.115556).Within(.1));
            Assert.That(results[0].Member, Is.EqualTo("Palermo"));

            Assert.That(results[1].Longitude, Is.EqualTo(15.087269).Within(.1));
            Assert.That(results[1].Latitude, Is.EqualTo(37.502669).Within(.1));
            Assert.That(results[1].Member, Is.EqualTo("Catania"));
        }

        [Test]
        public async Task Can_CalculateDistanceBetweenGeoMembers()
        {
            await redis.FlushDbAsync();
            await redis.AddGeoMembersAsync("Sicily", new[] {
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania")
            });

            var distance = await redis.CalculateDistanceBetweenGeoMembersAsync("Sicily", "Palermo", "Catania");
            Assert.That(distance, Is.EqualTo(166274.15156960039).Within(.1));
        }

        [Test]
        public async Task CalculateDistanceBetweenGeoMembers_on_NonExistingMember_returns_NaN()
        {
            await redis.FlushDbAsync();
            await redis.AddGeoMembersAsync("Sicily", new[] {
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania")
            });

            var distance = await redis.CalculateDistanceBetweenGeoMembersAsync("Sicily", "Palermo", "NonExistingMember");
            Assert.That(distance, Is.EqualTo(double.NaN));
        }

        [Test]
        public async Task Can_GetGeohashes()
        {
            await redis.FlushDbAsync();
            await redis.AddGeoMembersAsync("Sicily", new[] {
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania")
            });

            var hashes = await redis.GetGeohashesAsync("Sicily", new[] { "Palermo", "Catania" });
            Assert.That(hashes[0], Is.EqualTo("sqc8b49rny0"));
            Assert.That(hashes[1], Is.EqualTo("sqdtr74hyu0"));

            hashes = await redis.GetGeohashesAsync("Sicily", new[] { "Palermo", "NonExistingMember", "Catania" });
            Assert.That(hashes[0], Is.EqualTo("sqc8b49rny0"));
            Assert.That(hashes[1], Is.Null);
            Assert.That(hashes[2], Is.EqualTo("sqdtr74hyu0"));
        }

        [Test]
        public async Task Can_FindGeoMembersInRadius()
        {
            await redis.FlushDbAsync();
            await redis.AddGeoMembersAsync("Sicily", new[] {
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania")
            });

            var results = await redis.FindGeoMembersInRadiusAsync("Sicily", 15, 37, 200, RedisGeoUnit.Kilometers);

            Assert.That(results.Length, Is.EqualTo(2));
            Assert.That(results[0], Is.EqualTo("Palermo"));
            Assert.That(results[1], Is.EqualTo("Catania"));
        }

        //[Test] // method does not exist on IRedisClient/IRedisClientAsync
        //public async Task Can_GeoRadiusByMember()
        //{
        //    await redis.FlushDbAsync();
        //    await redis.AddGeoMembersAsync("Sicily", new[] {
        //        new RedisGeo(13.583333, 37.316667, "Agrigento"),
        //        new RedisGeo(13.361389, 38.115556, "Palermo"),
        //        new RedisGeo(15.087269, 37.502669, "Catania")
        //    });

        //    var results = await redis.GeoRadiusByMemberAsync("Sicily", "Agrigento", 100, RedisGeoUnit.Kilometers);

        //    Assert.That(results.Count, Is.EqualTo(2));
        //    Assert.That(results[0].Member, Is.EqualTo("Agrigento"));
        //    Assert.That(results[0].Unit, Is.Null);
        //    Assert.That(results[1].Member, Is.EqualTo("Palermo"));
        //    Assert.That(results[1].Unit, Is.Null);
        //}

        [Test]
        public async Task Can_FindGeoResultsInRadius()
        {
            await redis.FlushDbAsync();
            await redis.AddGeoMembersAsync("Sicily", new[] {
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania")
            });

            var results = await redis.FindGeoResultsInRadiusAsync("Sicily", 15, 37, 200, RedisGeoUnit.Kilometers);

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
        public async Task Can_FindGeoResultsInRadius_by_Member()
        {
            await redis.FlushDbAsync();
            await redis.AddGeoMembersAsync("Sicily", new[] {
                new RedisGeo(13.583333, 37.316667, "Agrigento"),
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania")
            });

            var results = await redis.FindGeoResultsInRadiusAsync("Sicily", "Agrigento", 100, RedisGeoUnit.Kilometers);

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
        public async Task Can_GeoRadius_WithCoord_WithDist_WithHash_Count_and_Asc()
        {
            await redis.FlushDbAsync();
            await redis.AddGeoMembersAsync("Sicily", new[] {
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania")
            });

            var results = await redis.FindGeoResultsInRadiusAsync("Sicily", 15, 37, 200, RedisGeoUnit.Kilometers,
                 count: 1, sortByNearest: false);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Member, Is.EqualTo("Palermo"));
            Assert.That(results[0].Unit, Is.EqualTo(RedisGeoUnit.Kilometers));
            Assert.That(results[0].Longitude, Is.EqualTo(13.361389).Within(.1));
            Assert.That(results[0].Latitude, Is.EqualTo(38.115556).Within(.1));
            Assert.That(results[0].Distance, Is.EqualTo(190.4424).Within(.1));
            Assert.That(results[0].Hash, Is.EqualTo(3479099956230698));

            results = await redis.FindGeoResultsInRadiusAsync("Sicily", 15, 37, 200, RedisGeoUnit.Kilometers,
                 count: 1, sortByNearest: true);

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