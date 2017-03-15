using AutorestClient;
using AutorestClient.Models;
using NUnit.Framework;
using ServiceStack.OpenApi.Tests.Host;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ServiceStack.OpenApi.Tests
{
    [TestFixture]
    class AllTypesTests : GeneratedClientTestBase
    {
        [Test]
        public void Sleep()
        {
            Thread.Sleep(20000);
        }


        [Test]
        public void Can_post_all_types()
        {
            var client = new ServiceStackAutorestClient(new Uri(Config.AbsoluteBaseUri));

            HelloAllTypes helloAllTypes = new HelloAllTypes()
            {
                Name = "Hello",
                AllTypes = new AllTypes()
                {
                    ByteProperty = byte.MaxValue,
                    CharProperty = "n",
                    DateTime = DateTime.UtcNow,
                    DecimalProperty = 100.123456789,
                    DateTimeOffset = new DateTimeOffset(DateTime.UtcNow.AddDays(1)).ToString(),
                    DoubleProperty = 123.45678901,
                    FloatProperty = 456.312f,
                    Guid = Guid.NewGuid().ToString(),
                    Id = 1,
                    IntProperty = Int32.MaxValue,
                    IntStringMap = new Dictionary<string, string>() { { "1", "abc" }, { "2", "bcd" }, { "3", "cde" } },
                    KeyValuePair = new KeyValuePairStringString("key1", "value1"),
                    LongProperty = long.MaxValue,
                    NullableDateTime = DateTime.UtcNow,
                    NullableId = 2,
                    NullableTimeSpan = new TimeSpan(1, 0, 0).ToString(),
                    TimeSpan = new TimeSpan(2, 1, 3).ToString(),
                    ShortProperty = short.MaxValue,
                    StringProperty = "test string",
                    StringArray = new string[] { "string1", "string2", "string3" },
                    StringList = new List<string>() { "string4", "string5", "string6" },
                    StringMap = new Dictionary<string, string>() { { "ab", "abc" }, { "bc", "bcd" }, { "cd", "cde" } },
                    SubType = new SubType() { Id = 10, Name = "SubType name" },
                    UIntProperty = 123456,
                    ULongProperty = 1234567,
                    UShortProperty = UInt16.MaxValue,
                },
                AllCollectionTypes = new AllCollectionTypes()
                {
                    IntArray = new int[] { 1, 2, 3, 4 },
                    IntList = new List<int>() { 1, 2, 3, 4 },
                    PocoArray = new Poco[] { new Poco() { Name = "poco1" }, new Poco() { Name = "poco2" } },
                    PocoList = new List<Poco>() { new Poco() { Name = "poco1" }, new Poco() { Name = "poco2" } },
                    PocoLookup = new Dictionary<string, IList<Poco>>()
                    {
                        { "p1", new List<Poco>() { new Poco() { Name = "poco1" }, new Poco() { Name = "poco2" } }},
                        { "p2", new List<Poco>() { new Poco() { Name = "poco3" }, new Poco() { Name = "poco4" } }}
                    },
                    PocoLookupMap = new Dictionary<string, IList<IDictionary<string, Poco>>>()
                    {
                        { "pp1", new List<IDictionary<string, Poco>>()
                            {
                                new Dictionary<string, Poco>() {
                                    { "p11", new Poco() { Name = "poco1" } },
                                    { "p12", new Poco() { Name = "poco2" } }
                                },
                                new Dictionary<string, Poco>() {
                                    { "p13", new Poco() { Name = "poco3" } },
                                    { "p14", new Poco() { Name = "poco4" } }
                                }
                            }
                        },
                        { "pp2", new List<IDictionary<string, Poco>>()
                            {
                                new Dictionary<string, Poco>() {
                                    { "p21", new Poco() { Name = "poco1" } },
                                    { "p22", new Poco() { Name = "poco2" } }
                                },
                                new Dictionary<string, Poco>() {
                                    { "p23", new Poco() { Name = "poco3" } },
                                    { "p24", new Poco() { Name = "poco4" } }
                                }
                            }
                        }
                     },
                    StringArray = new string[] { "string1", "string2" },
                    StringList = new List<string>() { "string1", "string2" }
                }
            };
            
            var result = client.HelloAllTypes.Post("123", null, null, helloAllTypes);
        }

        [Test]
        public void Can_post_all_types_with_result()
        {
            var client = new ServiceStackAutorestClient(new Uri(Config.AbsoluteBaseUri));

            var dto = new HelloAllTypesWithResult()
            {
                Name = "Hello",
                AllTypes = new AllTypes()
                {
                    ByteProperty = byte.MaxValue,
                    CharProperty = "n",
                    DateTime = DateTime.UtcNow,
                    DecimalProperty = 100.123456789,
                    DateTimeOffset = new DateTimeOffset(DateTime.UtcNow.AddDays(1)).ToString(),
                    DoubleProperty = 123.45678901,
                    FloatProperty = 456.312f,
                    Guid = Guid.NewGuid().ToString(),
                    Id = 1,
                    IntProperty = Int32.MaxValue,
                    IntStringMap = new Dictionary<string, string>() { { "1", "abc" }, { "2", "bcd" }, { "3", "cde" } },
                    KeyValuePair = null, //new KeyValuePair<string, string>("key1", "value1").ToString(), KeyValuePair does not work yet
                    LongProperty = long.MaxValue,
                    NullableDateTime = DateTime.UtcNow,
                    NullableId = 2,
                    NullableTimeSpan = new TimeSpan(1, 0, 0).ToString(),
                    TimeSpan = new TimeSpan(2, 1, 3).ToString(),
                    ShortProperty = short.MaxValue,
                    StringProperty = "test string",
                    StringArray = new string[] { "string1", "string2", "string3" },
                    StringList = new List<string>() { "string4", "string5", "string6" },
                    StringMap = new Dictionary<string, string>() { { "ab", "abc" }, { "bc", "bcd" }, { "cd", "cde" } },
                    SubType = new SubType() { Id = 10, Name = "SubType name" },
                    UIntProperty = 123456,
                    ULongProperty = 1234567,
                    UShortProperty = UInt16.MaxValue,
                },
                AllCollectionTypes = new AllCollectionTypes()
                {
                    IntArray = new int[] { 1, 2, 3, 4 },
                    IntList = new List<int>() { 1, 2, 3, 4 },
                    PocoArray = new Poco[] { new Poco() { Name = "poco1" }, new Poco() { Name = "poco2" } },
                    PocoList = new List<Poco>() { new Poco() { Name = "poco1" }, new Poco() { Name = "poco2" } },
                    PocoLookup = new Dictionary<string, IList<Poco>>()
                    {
                        { "p1", new List<Poco>() { new Poco() { Name = "poco1" }, new Poco() { Name = "poco2" } }},
                        { "p2", new List<Poco>() { new Poco() { Name = "poco3" }, new Poco() { Name = "poco4" } }}
                    },
                    PocoLookupMap = new Dictionary<string, IList<IDictionary<string, Poco>>>()
                    {
                        { "pp1", new List<IDictionary<string, Poco>>()
                            {
                                new Dictionary<string, Poco>() {
                                    { "p11", new Poco() { Name = "poco1" } },
                                    { "p12", new Poco() { Name = "poco2" } }
                                },
                                new Dictionary<string, Poco>() {
                                    { "p13", new Poco() { Name = "poco3" } },
                                    { "p14", new Poco() { Name = "poco4" } }
                                }
                            }
                        },
                        { "pp2", new List<IDictionary<string, Poco>>()
                            {
                                new Dictionary<string, Poco>() {
                                    { "p21", new Poco() { Name = "poco1" } },
                                    { "p22", new Poco() { Name = "poco2" } }
                                },
                                new Dictionary<string, Poco>() {
                                    { "p23", new Poco() { Name = "poco3" } },
                                    { "p24", new Poco() { Name = "poco4" } }
                                }
                            }
                        }
                     },
                    StringArray = new string[] { "string1", "string2" },
                    StringList = new List<string>() { "string1", "string2" }
                }
            };

            var result = client.HelloAllTypesWithResult.Post(body: dto);

            Assert.That(result.Result, Is.EqualTo(dto.Name));
            Assert.That(result.AllTypes.ByteProperty, Is.EqualTo(dto.AllTypes.ByteProperty));
            Assert.That(result.AllTypes.CharProperty, Is.EqualTo(dto.AllTypes.CharProperty));
            Assert.That(result.AllTypes.DateTime, Is.EqualTo(dto.AllTypes.DateTime).Within(TimeSpan.FromSeconds(1)));
            //Assert.That(result.AllTypes.DateTimeOffset, Is.EqualTo(dto.AllTypes.DateTimeOffset));
            Assert.That(result.AllTypes.DecimalProperty, Is.EqualTo(dto.AllTypes.DecimalProperty));
            Assert.That(result.AllTypes.DoubleProperty, Is.EqualTo(dto.AllTypes.DoubleProperty));
            Assert.That(result.AllTypes.FloatProperty, Is.EqualTo(dto.AllTypes.FloatProperty).Within(0.0001));
            Assert.That(result.AllTypes.Guid, Is.EqualTo(dto.AllTypes.Guid.Replace("-", String.Empty)));
            Assert.That(result.AllTypes.Id, Is.EqualTo(dto.AllTypes.Id));
            Assert.That(result.AllTypes.IntProperty, Is.EqualTo(dto.AllTypes.IntProperty));
            Assert.That(result.AllTypes.IntStringMap, Is.EquivalentTo(dto.AllTypes.IntStringMap));

        }
    }
}
