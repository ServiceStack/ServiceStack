using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Caching;

namespace ServiceStack.Aws.DynamoDbTests
{
    [TestFixture]
    public class DynamoDbCustomCacheClientTests : DynamoTestBase
    {
        private ICacheClient cache;

        [OneTimeSetUp]
        public void OnOneTimeSetUp()
        {   
            cache = CreateCacheClient();
        }

        [Test]
        public void Can_set_get_and_remove()
        {
            cache.Set("Car", "Audi");
            var response = cache.Get<string>("Car");
            Assert.That(response, Is.EqualTo("Audi"));

            cache.Remove("Car");
            response = cache.Get<string>("Car");
            Assert.That(response, Is.EqualTo(default(string)));
        }

        [Test]
        public void Does_expire_key_with_local_time()
        {
            cache.Set("Car", "Audi", DateTime.Now.AddMilliseconds(10000));

            var response = cache.Get<string>("Car");
            Assert.That(response, Is.EqualTo("Audi"));

            cache.Set("Car", "Audi", DateTime.Now.AddMilliseconds(-10000));

            response = cache.Get<string>("Car");
            Assert.That(response, Is.EqualTo(default(string)));
        }

        [Test]
        public void Does_expire_key_with_utc_time()
        {
            cache.Set("Car", "Audi", DateTime.UtcNow.AddMilliseconds(10000));

            var response = cache.Get<string>("Car");
            Assert.That(response, Is.EqualTo("Audi"));

            cache.Set("Car", "Audi", DateTime.UtcNow.AddMilliseconds(-10000));

            response = cache.Get<string>("Car");
            Assert.That(response, Is.EqualTo(default(string)));
        }

        [Test]
        public void Can_use_batch_operations()
        {
            cache.SetAll(new Dictionary<string, string>
            {
                { "Car", "Audi" },
                { "Phone", "MotoX" }
            });

            var response = cache.GetAll<string>(new List<string> { "Car", "Phone" });
            Assert.That(response["Car"], Is.EqualTo("Audi"));
            Assert.That(response["Phone"], Is.EqualTo("MotoX"));

            var singleResponse = cache.Get<string>("Phone");
            Assert.That(singleResponse, Is.EqualTo("MotoX"));

            cache.RemoveAll(new List<string> { "Car", "Phone" });

            response = cache.GetAll<string>(new List<string> { "Car", "Phone" });
            Assert.That(response["Car"], Is.EqualTo(default(string)));
            Assert.That(response["Phone"], Is.EqualTo(default(string)));
        }

        [Test]
        public void Can_increment_and_decrement_values()
        {
            Assert.That(cache.Increment("incr:a", 2), Is.EqualTo(2));
            Assert.That(cache.Increment("incr:a", 3), Is.EqualTo(5));
            cache.Remove("incr:a");

            Assert.That(cache.Decrement("decr:a", 2), Is.EqualTo(-2));
            Assert.That(cache.Decrement("decr:a", 3), Is.EqualTo(-5));
            cache.Remove("decr:a");
        }

        [Test]
        public void Can_cache_multiple_items_in_parallel()
        {
            var fns = 10.Times(i => (Action)(() =>
            {
                cache.Set("concurrent-test", $"Data: {i}");
            }));

            Parallel.Invoke(fns.ToArray());

            var entry = cache.Get<string>("concurrent-test");
            Assert.That(entry, Does.StartWith("Data: "));

            cache.Remove("concurrent-test");
        }

        [Test]
        public void Does_flush_all()
        {
            3.Times(i => cache.Set("Car" + i, "Audi"));
            3.Times(i => Assert.That(cache.Get<string>("Car" + i), Is.EqualTo("Audi")));

            cache.FlushAll();

            3.Times(i => Assert.That(cache.Get<string>("Car" + i), Is.EqualTo(default(string))));
            3.Times(i => cache.Remove("Car" + i));
        }

        [Test]
        public void Can_flush_and_set_in_parallel()
        {
            //Ensure that no exception is thrown even while the cache is being flushed
            Parallel.Invoke(
                () => cache.FlushAll(),
                () =>
                {
                    5.Times(() =>
                    {
                        cache.Set("Car1", "Ford");
                        Thread.Sleep(75);
                    });
                },
                () =>
                {
                    5.Times(() =>
                    {
                        cache.Set("Car2", "Audi");
                        Thread.Sleep(50);
                    });
                });

            cache.RemoveAll(new List<string> { "Car1", "Car2", "Car3" });
        }

        class Car
        {
            public string Manufacturer { get; set; }
            public int Age { get; set; }
        }

        [Test]
        public void Can_cache_complex_entry()
        {
            var car = new Car { Manufacturer = "Audi", Age = 3 };
            cache.Set("Car", car);

            var response = cache.Get<Car>("Car");
            Assert.That(response.Manufacturer, Is.EqualTo(car.Manufacturer));
            Assert.That(response.Age, Is.EqualTo(car.Age));

            cache.Remove("Car");
        }

        [Test]
        public void Does_only_add_if_key_does_not_exist()
        {
            Assert.IsTrue(cache.Add("Car", "Audi"));
            Assert.That(cache.Get<string>("Car"), Is.EqualTo("Audi"));

            Assert.IsFalse(cache.Add("Car", "Ford"));
            Assert.That(cache.Get<string>("Car"), Is.EqualTo("Audi"));

            cache.Remove("Car");
        }

        [Test]
        public void Does_only_replace_if_key_exists()
        {
            Assert.IsFalse(cache.Replace("Car", "Audi"));
            Assert.That(cache.Get<string>("Car"), Is.EqualTo(default(string)));

            cache.Add("Car", "Ford");

            Assert.IsTrue(cache.Replace("Car", "Audi"));
            Assert.That(cache.Get<string>("Car"), Is.EqualTo("Audi"));

            cache.Remove("Car");
        }
    }
}
