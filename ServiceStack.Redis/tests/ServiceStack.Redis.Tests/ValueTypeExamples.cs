using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Redis.Generic;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Integration")]
    public class ValueTypeExamples
    {
        [SetUp]
        public void SetUp()
        {
            using (var redisClient = new RedisClient(TestConfig.SingleHost))
            {
                redisClient.FlushAll();
            }
        }

        [Test]
        public void Working_with_int_values()
        {
            const string intKey = "intkey";
            const int intValue = 1;

            //STORING AN INT USING THE BASIC CLIENT
            using (var redisClient = new RedisClient(TestConfig.SingleHost))
            {
                redisClient.SetValue(intKey, intValue.ToString());
                string strGetIntValue = redisClient.GetValue(intKey);
                int toIntValue = int.Parse(strGetIntValue);

                Assert.That(toIntValue, Is.EqualTo(intValue));
            }

            //STORING AN INT USING THE GENERIC CLIENT
            using (var redisClient = new RedisClient(TestConfig.SingleHost))
            {
                //Create a generic client that treats all values as ints:
                IRedisTypedClient<int> intRedis = redisClient.As<int>();

                intRedis.SetValue(intKey, intValue);
                var toIntValue = intRedis.GetValue(intKey);

                Assert.That(toIntValue, Is.EqualTo(intValue));
            }
        }

        [Test]
        public void Working_with_int_list_values()
        {
            const string intListKey = "intListKey";
            var intValues = new List<int> { 2, 4, 6, 8 };

            //STORING INTS INTO A LIST USING THE BASIC CLIENT
            using (var redisClient = new RedisClient(TestConfig.SingleHost))
            {
                IList<string> strList = redisClient.Lists[intListKey];

                //storing all int values in the redis list 'intListKey' as strings
                intValues.ForEach(x => strList.Add(x.ToString()));

                //retrieve all values again as strings
                List<string> strListValues = strList.ToList();

                //convert back to list of ints
                List<int> toIntValues = strListValues.ConvertAll(x => int.Parse(x));

                Assert.That(toIntValues, Is.EqualTo(intValues));

                //delete all items in the list
                strList.Clear();
            }

            //STORING INTS INTO A LIST USING THE GENERIC CLIENT
            using (var redisClient = new RedisClient(TestConfig.SingleHost))
            {
                //Create a generic client that treats all values as ints:
                IRedisTypedClient<int> intRedis = redisClient.As<int>();

                IRedisList<int> intList = intRedis.Lists[intListKey];

                //storing all int values in the redis list 'intListKey' as ints
                intValues.ForEach(x => intList.Add(x));

                List<int> toIntListValues = intList.ToList();

                Assert.That(toIntListValues, Is.EqualTo(intValues));
            }
        }

        public class IntAndString
        {
            public int Id { get; set; }
            public string Letter { get; set; }
        }

        [Test]
        public void Working_with_Generic_types()
        {
            using (var redisClient = new RedisClient(TestConfig.SingleHost))
            {
                //Create a typed Redis client that treats all values as IntAndString:
                var typedRedis = redisClient.As<IntAndString>();

                var pocoValue = new IntAndString { Id = 1, Letter = "A" };
                typedRedis.SetValue("pocoKey", pocoValue);
                IntAndString toPocoValue = typedRedis.GetValue("pocoKey");

                Assert.That(toPocoValue.Id, Is.EqualTo(pocoValue.Id));
                Assert.That(toPocoValue.Letter, Is.EqualTo(pocoValue.Letter));

                var pocoListValues = new List<IntAndString> {
                    new IntAndString {Id = 2, Letter = "B"},
                    new IntAndString {Id = 3, Letter = "C"},
                    new IntAndString {Id = 4, Letter = "D"},
                    new IntAndString {Id = 5, Letter = "E"},
                };

                IRedisList<IntAndString> pocoList = typedRedis.Lists["pocoListKey"];

                //Adding all IntAndString objects into the redis list 'pocoListKey'
                pocoListValues.ForEach(x => pocoList.Add(x));

                List<IntAndString> toPocoListValues = pocoList.ToList();

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
}