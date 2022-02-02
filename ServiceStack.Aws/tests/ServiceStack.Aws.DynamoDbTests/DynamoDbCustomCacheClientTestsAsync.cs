using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Caching;

namespace ServiceStack.Aws.DynamoDbTests
{
    [TestFixture]
    public class DynamoDbCustomCacheClientTestsAsync : DynamoTestBase
    {
        private ICacheClientAsync cache;

        [OneTimeSetUp]
        public async Task OnOneTimeSetUp()
        {   
            cache = (ICacheClientAsync)CreateCacheClient();
        }

        [Test]
        public async Task Can_set_get_and_remove()
        {
            await cache.SetAsync("Car", "Audi");
            var response = await cache.GetAsync<string>("Car");
            Assert.That(response, Is.EqualTo("Audi"));

            await cache.RemoveAsync("Car");
            response = await cache.GetAsync<string>("Car");
            Assert.That(response, Is.EqualTo(default(string)));
        }

        [Test]
        public async Task Does_expire_key_with_local_time()
        {
            await cache.SetAsync("Car", "Audi", DateTime.Now.AddMilliseconds(10000));

            var response = await cache.GetAsync<string>("Car");
            Assert.That(response, Is.EqualTo("Audi"));

            await cache.SetAsync("Car", "Audi", DateTime.Now.AddMilliseconds(-10000));

            response = await cache.GetAsync<string>("Car");
            Assert.That(response, Is.EqualTo(default(string)));
        }

        [Test]
        public async Task Does_expire_key_with_utc_time()
        {
            await cache.SetAsync("Car", "Audi", DateTime.UtcNow.AddMilliseconds(10000));

            var response = await cache.GetAsync<string>("Car");
            Assert.That(response, Is.EqualTo("Audi"));

            await cache.SetAsync("Car", "Audi", DateTime.UtcNow.AddMilliseconds(-10000));

            response = await cache.GetAsync<string>("Car");
            Assert.That(response, Is.EqualTo(default(string)));
        }

        [Test]
        public async Task Can_use_batch_operations()
        {
            await cache.SetAllAsync(new Dictionary<string, string>
            {
                { "Car", "Audi" },
                { "Phone", "MotoX" }
            });

            var response = await cache.GetAllAsync<string>(new List<string> { "Car", "Phone" });
            Assert.That(response["Car"], Is.EqualTo("Audi"));
            Assert.That(response["Phone"], Is.EqualTo("MotoX"));

            var singleResponse = await cache.GetAsync<string>("Phone");
            Assert.That(singleResponse, Is.EqualTo("MotoX"));

            await cache.RemoveAllAsync(new List<string> { "Car", "Phone" });

            response = await cache.GetAllAsync<string>(new List<string> { "Car", "Phone" });
            Assert.That(response["Car"], Is.EqualTo(default(string)));
            Assert.That(response["Phone"], Is.EqualTo(default(string)));
        }

        [Test]
        public async Task Can_increment_and_decrement_values()
        {
            Assert.That(await cache.IncrementAsync("incr:a", 2), Is.EqualTo(2));
            Assert.That(await cache.IncrementAsync("incr:a", 3), Is.EqualTo(5));
            await cache.RemoveAsync("incr:a");

            Assert.That(await cache.DecrementAsync("decr:a", 2), Is.EqualTo(-2));
            Assert.That(await cache.DecrementAsync("decr:a", 3), Is.EqualTo(-5));
            await cache.RemoveAsync("decr:a");
        }

        [Test]
        public async Task Can_cache_multiple_items_in_parallel()
        {
            Parallel.For(0, 10, i => {
                cache.SetAsync("concurrent-test", $"Data: {i}");
            });

            var entry = await cache.GetAsync<string>("concurrent-test");
            Assert.That(entry, Does.StartWith("Data: "));

            await cache.RemoveAsync("concurrent-test");
        }

        [Test]
        public async Task Does_flush_all()
        {
            await 3.TimesAsync(async i => await cache.SetAsync("Car" + i, "Audi"));
            await 3.TimesAsync(async i => Assert.That(await cache.GetAsync<string>("Car" + i), Is.EqualTo("Audi")));

            await cache.FlushAllAsync();

            await 3.TimesAsync(async i => Assert.That(await cache.GetAsync<string>("Car" + i), Is.EqualTo(default(string))));
            await 3.TimesAsync(async i => await cache.RemoveAsync("Car" + i));
        }

        [Test]
        public async Task Can_flush_and_set_in_parallel()
        {
            //Ensure that no exception is thrown even while the cache is being flushed
            Parallel.Invoke(
                async () => await cache.FlushAllAsync(),
                async () =>
                {
                    await 5.TimesAsync(async i =>
                    {
                        await cache.SetAsync("Car1", "Ford");
                        await Task.Delay(75);
                    });
                },
                async () =>
                {
                    await 5.TimesAsync(async i =>
                    {
                        await cache.SetAsync("Car2", "Audi");
                        await Task.Delay(50);
                    });
                });

            await Task.Delay(100);
            await cache.RemoveAllAsync(new List<string> { "Car1", "Car2", "Car3" });
        }

        class Car
        {
            public string Manufacturer { get; set; }
            public int Age { get; set; }
        }

        [Test]
        public async Task Can_cache_complex_entry()
        {
            var car = new Car { Manufacturer = "Audi", Age = 3 };
            await cache.SetAsync("Car", car);

            var response = await cache.GetAsync<Car>("Car");
            Assert.That(response.Manufacturer, Is.EqualTo(car.Manufacturer));
            Assert.That(response.Age, Is.EqualTo(car.Age));

            await cache.RemoveAsync("Car");
        }

        [Test]
        public async Task Does_only_add_if_key_does_not_exist()
        {
            Assert.IsTrue(await cache.AddAsync("Car", "Audi"));
            Assert.That(await cache.GetAsync<string>("Car"), Is.EqualTo("Audi"));

            Assert.IsFalse(await cache.AddAsync("Car", "Ford"));
            Assert.That(await cache.GetAsync<string>("Car"), Is.EqualTo("Audi"));

            await cache.RemoveAsync("Car");
        }

        [Test]
        public async Task Does_only_replace_if_key_exists()
        {
            Assert.IsFalse(await cache.ReplaceAsync("Car", "Audi"));
            Assert.That(await cache.GetAsync<string>("Car"), Is.EqualTo(default(string)));

            await cache.AddAsync("Car", "Ford");

            Assert.IsTrue(await cache.ReplaceAsync("Car", "Audi"));
            Assert.That(await cache.GetAsync<string>("Car"), Is.EqualTo("Audi"));

            await cache.RemoveAsync("Car");
        }
    }
}
