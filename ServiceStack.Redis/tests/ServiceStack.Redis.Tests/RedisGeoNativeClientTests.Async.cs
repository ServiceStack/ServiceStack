using NUnit.Framework;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Async")]
    [Ignore("CI requires redis-server v3.2.0")]
    public class RedisGeoNativeClientTestsAsync
    {
        private readonly IRedisNativeClientAsync redis;

        public RedisGeoNativeClientTestsAsync()
        {
            redis = new RedisNativeClient(TestConfig.GeoHost);
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await redis.DisposeAsync();
        }

        [Test]
        public async Task Can_GeoAdd_and_GeoPos()
        {
            await redis.FlushDbAsync();
            var count = await redis.GeoAddAsync("Sicily", 13.361389, 38.115556, "Palermo");
            Assert.That(count, Is.EqualTo(1));
            var results = await redis.GeoPosAsync("Sicily", new[] { "Palermo" });

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Longitude, Is.EqualTo(13.361389).Within(.1));
            Assert.That(results[0].Latitude, Is.EqualTo(38.115556).Within(.1));
            Assert.That(results[0].Member, Is.EqualTo("Palermo"));
        }

        [Test]
        public async Task GeoPos_on_NonExistingMember_returns_no_results()
        {
            await redis.FlushDbAsync();
            var count = await redis.GeoAddAsync("Sicily", 13.361389, 38.115556, "Palermo");
            var results = await redis.GeoPosAsync("Sicily", new[] { "NonExistingMember" });
            Assert.That(results.Count, Is.EqualTo(0));

            results = await redis.GeoPosAsync("Sicily", new[] { "Palermo", "NonExistingMember" });
            Assert.That(results.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task Can_GeoAdd_and_GeoPos_multiple()
        {
            await redis.FlushDbAsync();
            var count = await redis.GeoAddAsync("Sicily", new[] {
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania")
            });
            Assert.That(count, Is.EqualTo(2));

            var results = await redis.GeoPosAsync("Sicily", new[] { "Palermo", "Catania" });

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].Longitude, Is.EqualTo(13.361389).Within(.1));
            Assert.That(results[0].Latitude, Is.EqualTo(38.115556).Within(.1));
            Assert.That(results[0].Member, Is.EqualTo("Palermo"));

            Assert.That(results[1].Longitude, Is.EqualTo(15.087269).Within(.1));
            Assert.That(results[1].Latitude, Is.EqualTo(37.502669).Within(.1));
            Assert.That(results[1].Member, Is.EqualTo("Catania"));
        }

        [Test]
        public async Task Can_GeoDist()
        {
            await redis.FlushDbAsync();
            await redis.GeoAddAsync("Sicily", new[] {
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania")
            });

            var distance = await redis.GeoDistAsync("Sicily", "Palermo", "Catania");
            Assert.That(distance, Is.EqualTo(166274.15156960039).Within(.1));
        }

        [Test]
        public async Task GeoDist_on_NonExistingMember_returns_NaN()
        {
            await redis.FlushDbAsync();
            await redis.GeoAddAsync("Sicily", new[] {
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania")
            });

            var distance = await redis.GeoDistAsync("Sicily", "Palermo", "NonExistingMember");
            Assert.That(distance, Is.EqualTo(double.NaN));
        }

        [Test]
        public async Task Can_GeoHash()
        {
            await redis.FlushDbAsync();
            await redis.GeoAddAsync("Sicily", new[] {
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania")
            });

            var hashes = await redis.GeoHashAsync("Sicily", new[] { "Palermo", "Catania" });
            Assert.That(hashes[0], Is.EqualTo("sqc8b49rny0"));
            Assert.That(hashes[1], Is.EqualTo("sqdtr74hyu0"));

            hashes = await redis.GeoHashAsync("Sicily", new[] { "Palermo", "NonExistingMember", "Catania" });
            Assert.That(hashes[0], Is.EqualTo("sqc8b49rny0"));
            Assert.That(hashes[1], Is.Null);
            Assert.That(hashes[2], Is.EqualTo("sqdtr74hyu0"));
        }

        [Test]
        public async Task Can_GeoRadius_default()
        {
            await redis.FlushDbAsync();
            await redis.GeoAddAsync("Sicily", new[] {
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania")
            });

            var results = await redis.GeoRadiusAsync("Sicily", 15, 37, 200, RedisGeoUnit.Kilometers);

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].Member, Is.EqualTo("Palermo"));
            Assert.That(results[0].Unit, Is.Null);
            Assert.That(results[1].Member, Is.EqualTo("Catania"));
            Assert.That(results[1].Unit, Is.Null);
        }

        [Test]
        public async Task Can_GeoRadiusByMember_default()
        {
            await redis.FlushDbAsync();
            await redis.GeoAddAsync("Sicily", new[] {
                new RedisGeo(13.583333, 37.316667, "Agrigento"),
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania")
            });

            var results = await redis.GeoRadiusByMemberAsync("Sicily", "Agrigento", 100, RedisGeoUnit.Kilometers);

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].Member, Is.EqualTo("Agrigento"));
            Assert.That(results[0].Unit, Is.Null);
            Assert.That(results[1].Member, Is.EqualTo("Palermo"));
            Assert.That(results[1].Unit, Is.Null);
        }

        [Test]
        public async Task Can_GeoRadius_WithCoord()
        {
            await redis.FlushDbAsync();
            await redis.GeoAddAsync("Sicily", new[] {
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania")
            });

            var results = await redis.GeoRadiusAsync("Sicily", 15, 37, 200, RedisGeoUnit.Kilometers, withCoords: true);

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
        public async Task Can_GeoRadius_WithDist()
        {
            await redis.FlushDbAsync();
            await redis.GeoAddAsync("Sicily", new[] {
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania")
            });

            var results = await redis.GeoRadiusAsync("Sicily", 15, 37, 200, RedisGeoUnit.Kilometers, withDist: true);

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].Member, Is.EqualTo("Palermo"));
            Assert.That(results[0].Unit, Is.EqualTo(RedisGeoUnit.Kilometers));
            Assert.That(results[0].Distance, Is.EqualTo(190.4424).Within(.1));

            Assert.That(results[1].Member, Is.EqualTo("Catania"));
            Assert.That(results[1].Unit, Is.EqualTo(RedisGeoUnit.Kilometers));
            Assert.That(results[1].Distance, Is.EqualTo(56.4413).Within(.1));
        }

        [Test]
        public async Task Can_GeoRadius_WithCoord_WithDist_WithHash()
        {
            await redis.FlushDbAsync();
            await redis.GeoAddAsync("Sicily", new[] {
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania")
            });

            var results = await redis.GeoRadiusAsync("Sicily", 15, 37, 200, RedisGeoUnit.Kilometers,
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
        public async Task Can_GeoRadiusByMember_WithCoord_WithDist_WithHash()
        {
            await redis.FlushDbAsync();
            await redis.GeoAddAsync("Sicily", new[] {
                new RedisGeo(13.583333, 37.316667, "Agrigento"),
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania")
            });

            var results = await redis.GeoRadiusByMemberAsync("Sicily", "Agrigento", 100, RedisGeoUnit.Kilometers,
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
        public async Task Can_GeoRadius_WithCoord_WithDist_WithHash_Count_and_Asc()
        {
            await redis.FlushDbAsync();
            await redis.GeoAddAsync("Sicily", new[] {
                new RedisGeo(13.361389, 38.115556, "Palermo"),
                new RedisGeo(15.087269, 37.502669, "Catania")
            });

            var results = await redis.GeoRadiusAsync("Sicily", 15, 37, 200, RedisGeoUnit.Kilometers,
                withCoords: true, withDist: true, withHash: true, count:1, asc:false);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Member, Is.EqualTo("Palermo"));
            Assert.That(results[0].Unit, Is.EqualTo(RedisGeoUnit.Kilometers));
            Assert.That(results[0].Longitude, Is.EqualTo(13.361389).Within(.1));
            Assert.That(results[0].Latitude, Is.EqualTo(38.115556).Within(.1));
            Assert.That(results[0].Distance, Is.EqualTo(190.4424).Within(.1));
            Assert.That(results[0].Hash, Is.EqualTo(3479099956230698));

             results = await redis.GeoRadiusAsync("Sicily", 15, 37, 200, RedisGeoUnit.Kilometers,
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