using NUnit.Framework;
using ServiceStack.Redis.Generic;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Integration"), Category("Async")]
    public class ValueTypeExamplesAsync
    {
        [SetUp]
        public async Task SetUp()
        {
            await using var redisClient = new RedisClient(TestConfig.SingleHost).ForAsyncOnly();
            await redisClient.FlushAllAsync();
        }

        [Test]
        public async Task Working_with_int_values()
        {
            const string intKey = "intkey";
            const int intValue = 1;

            //STORING AN INT USING THE BASIC CLIENT
            await using (var redisClient = new RedisClient(TestConfig.SingleHost).ForAsyncOnly())
            {
                await redisClient.SetValueAsync(intKey, intValue.ToString());
                string strGetIntValue = await redisClient.GetValueAsync(intKey);
                int toIntValue = int.Parse(strGetIntValue);

                Assert.That(toIntValue, Is.EqualTo(intValue));
            }

            //STORING AN INT USING THE GENERIC CLIENT
            await using (var redisClient = new RedisClient(TestConfig.SingleHost).ForAsyncOnly())
            {
                //Create a generic client that treats all values as ints:
                IRedisTypedClientAsync<int> intRedis = redisClient.As<int>();

                await intRedis.SetValueAsync(intKey, intValue);
                var toIntValue = await intRedis.GetValueAsync(intKey);

                Assert.That(toIntValue, Is.EqualTo(intValue));
            }
        }

        [Test]
        public async Task Working_with_int_list_values()
        {
            const string intListKey = "intListKey";
            var intValues = new List<int> { 2, 4, 6, 8 };

            //STORING INTS INTO A LIST USING THE BASIC CLIENT
            await using (var redisClient = new RedisClient(TestConfig.SingleHost).ForAsyncOnly())
            {
                IRedisListAsync strList = redisClient.Lists[intListKey];

                //storing all int values in the redis list 'intListKey' as strings
                await intValues.ForEachAsync(async x => await strList.AddAsync(x.ToString()));

                //retrieve all values again as strings
                List<string> strListValues = await strList.ToListAsync();

                //convert back to list of ints
                List<int> toIntValues = strListValues.ConvertAll(x => int.Parse(x));

                Assert.That(toIntValues, Is.EqualTo(intValues));

                //delete all items in the list
                await strList.ClearAsync();
            }

            //STORING INTS INTO A LIST USING THE GENERIC CLIENT
            await using (var redisClient = new RedisClient(TestConfig.SingleHost).ForAsyncOnly())
            {
                //Create a generic client that treats all values as ints:
                IRedisTypedClientAsync<int> intRedis = redisClient.As<int>();

                IRedisListAsync<int> intList = intRedis.Lists[intListKey];

                //storing all int values in the redis list 'intListKey' as ints
                await intValues.ForEachAsync(async x => await intList.AddAsync(x));

                List<int> toIntListValues = await intList.ToListAsync();

                Assert.That(toIntListValues, Is.EqualTo(intValues));
            }
        }

        public class IntAndString
        {
            public int Id { get; set; }
            public string Letter { get; set; }
        }

        [Test]
        public async Task Working_with_Generic_types()
        {
            await using var redisClient = new RedisClient(TestConfig.SingleHost).ForAsyncOnly();
            //Create a typed Redis client that treats all values as IntAndString:
            var typedRedis = redisClient.As<IntAndString>();

            var pocoValue = new IntAndString { Id = 1, Letter = "A" };
            await typedRedis.SetValueAsync("pocoKey", pocoValue);
            IntAndString toPocoValue = await typedRedis.GetValueAsync("pocoKey");

            Assert.That(toPocoValue.Id, Is.EqualTo(pocoValue.Id));
            Assert.That(toPocoValue.Letter, Is.EqualTo(pocoValue.Letter));

            var pocoListValues = new List<IntAndString> {
                new IntAndString {Id = 2, Letter = "B"},
                new IntAndString {Id = 3, Letter = "C"},
                new IntAndString {Id = 4, Letter = "D"},
                new IntAndString {Id = 5, Letter = "E"},
            };

            IRedisListAsync<IntAndString> pocoList = typedRedis.Lists["pocoListKey"];

            //Adding all IntAndString objects into the redis list 'pocoListKey'
            await pocoListValues.ForEachAsync(async x => await pocoList.AddAsync(x));

            List<IntAndString> toPocoListValues = await pocoList.ToListAsync();

            for (var i = 0; i < pocoListValues.Count; i++)
            {
                pocoValue = pocoListValues[i];
                toPocoValue = toPocoListValues[i];
                Assert.That(toPocoValue.Id, Is.EqualTo(pocoValue.Id));
                Assert.That(toPocoValue.Letter, Is.EqualTo(pocoValue.Letter));
            }
        }

    }
}