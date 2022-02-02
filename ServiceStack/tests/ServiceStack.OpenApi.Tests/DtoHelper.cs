using AutorestClient.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace ServiceStack.OpenApi.Tests
{
    class DtoHelper
    {
        public static AllTypes GetAllTypes()
        {
            return new AllTypes
            {
                ByteProperty = byte.MaxValue,
                CharProperty = "n",
                DateTimeProperty = DateTime.UtcNow,
                DecimalProperty = 100.123456789,
                DateTimeOffsetProperty = new DateTimeOffset(DateTime.UtcNow.AddDays(1)).ToString(),
                DoubleProperty = 123.45678901,
                FloatProperty = 456.312f,
                GuidProperty = Guid.NewGuid().ToString(),
                Id = 1,
                IntProperty = int.MaxValue,
                IntStringMap = new Dictionary<string, string> { { "1", "abc" }, { "2", "bcd" }, { "3", "cde" } },
                KeyValuePairProperty = new KeyValuePairStringString("key1", "value1"),
                LongProperty = long.MaxValue,
                NullableDateTime = DateTime.UtcNow,
                NullableId = 2,
                NullableTimeSpan = new TimeSpan(1, 0, 0).ToString(),
                TimeSpanProperty = new TimeSpan(2, 1, 3).ToString(),
                ShortProperty = short.MaxValue,
                StringProperty = "test string",
                StringArray = new string[] { "string1", "string2", "string3" },
                StringList = new List<string>() { "string4", "string5", "string6" },
                StringMap = new Dictionary<string, string>() { { "ab", "abc" }, { "bc", "bcd" }, { "cd", "cde" } },
                SubType = new SubType() { Id = 10, Name = "SubType name" },
                UIntProperty = 123456,
                ULongProperty = 1234567,
                UShortProperty = UInt16.MaxValue,
            };
        }

        public static AllCollectionTypes GetAllCollectionTypes()
        {
            return new AllCollectionTypes
            {
                IntArray = new[] { 1, 2, 3, 4 },
                IntList = new List<int>{ 1, 2, 3, 4 },
                PocoArray = new[] { new Poco{ Name = "poco1" }, new Poco{ Name = "poco2" } },
                PocoList = new List<Poco>{ new Poco{ Name = "poco1" }, new Poco{ Name = "poco2" } },
                PocoLookup = new Dictionary<string, IList<Poco>>
                    {
                        { "p1", new List<Poco>{ new Poco{ Name = "poco1" }, new Poco{ Name = "poco2" } }},
                        { "p2", new List<Poco>{ new Poco{ Name = "poco3" }, new Poco{ Name = "poco4" } }}
                    },
                PocoLookupMap = new Dictionary<string, IList<IDictionary<string, Poco>>>()
                    {
                        { "pp1", new List<IDictionary<string, Poco>>
                            {
                                new Dictionary<string, Poco>{
                                    { "p11", new Poco{ Name = "poco1" } },
                                    { "p12", new Poco{ Name = "poco2" } }
                                },
                                new Dictionary<string, Poco>{
                                    { "p13", new Poco{ Name = "poco3" } },
                                    { "p14", new Poco{ Name = "poco4" } }
                                }
                            }
                        },
                        { "pp2", new List<IDictionary<string, Poco>>()
                            {
                                new Dictionary<string, Poco>{
                                    { "p21", new Poco{ Name = "poco1" } },
                                    { "p22", new Poco{ Name = "poco2" } }
                                },
                                new Dictionary<string, Poco>{
                                    { "p23", new Poco{ Name = "poco3" } },
                                    { "p24", new Poco{ Name = "poco4" } }
                                }
                            }
                        }
                     },
                StringArray = new string[] { "string1", "string2" },
                StringList = new List<string>{ "string1", "string2" }
            };
        }

        public static void AssertAllTypes(AllTypes actual, AllTypes expected)
        {
            Assert.That(actual.ByteProperty, Is.EqualTo(expected.ByteProperty));
            Assert.That(actual.CharProperty, Is.EqualTo(expected.CharProperty));
            Assert.That(actual.DateTimeProperty, Is.EqualTo(expected.DateTimeProperty).Within(TimeSpan.FromSeconds(1)));
            //Assert.That(actual.DateTimeOffset, Is.EqualTo(expected.DateTimeOffset));
            Assert.That(actual.DecimalProperty, Is.EqualTo(expected.DecimalProperty));
            Assert.That(actual.DoubleProperty, Is.EqualTo(expected.DoubleProperty));
            Assert.That(actual.FloatProperty, Is.EqualTo(expected.FloatProperty).Within(0.0001));
            Assert.That(actual.GuidProperty, Is.EqualTo(expected.GuidProperty.Replace("-", String.Empty)));
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.IntProperty, Is.EqualTo(expected.IntProperty));
            Assert.That(actual.IntStringMap, Is.EquivalentTo(expected.IntStringMap));
            Assert.That(actual.KeyValuePairProperty.Key, Is.EquivalentTo(expected.KeyValuePairProperty.Key));
            Assert.That(actual.KeyValuePairProperty.Value, Is.EquivalentTo(expected.KeyValuePairProperty.Value));
            Assert.That(actual.LongProperty, Is.EqualTo(expected.LongProperty));
            Assert.That(actual.NullableDateTime, Is.EqualTo(expected.NullableDateTime).Within(TimeSpan.FromSeconds(1)));
            Assert.That(actual.NullableId, Is.EqualTo(expected.NullableId));
            //Assert.That(actual.NullableTimeSpan, Is.EqualTo(expected.NullableTimeSpan));
            Assert.That(actual.ShortProperty, Is.EqualTo(expected.ShortProperty));
            Assert.That(actual.StringArray, Is.EquivalentTo(expected.StringArray));
            Assert.That(actual.StringList, Is.EquivalentTo(expected.StringList));
            Assert.That(actual.StringMap, Is.EquivalentTo(expected.StringMap));
            Assert.That(actual.StringProperty, Is.EqualTo(expected.StringProperty));
            Assert.That(actual.SubType.Id, Is.EqualTo(expected.SubType.Id));
            Assert.That(actual.SubType.Name, Is.EqualTo(expected.SubType.Name));
            //Assert.That(actual.TimeSpan, Is.EqualTo(expected.TimeSpan));
            Assert.That(actual.UIntProperty, Is.EqualTo(expected.UIntProperty));
            Assert.That(actual.ULongProperty, Is.EqualTo(expected.ULongProperty));
            Assert.That(actual.UShortProperty, Is.EqualTo(expected.UShortProperty));
        }

        public static void AssertAllCollectionTypes(AllCollectionTypes actual, AllCollectionTypes expected)
        {
            Assert.That(actual.IntArray, Is.EqualTo(expected.IntArray));
            Assert.That(actual.IntList, Is.EqualTo(expected.IntList));
            AssertListPoco(actual.PocoArray, expected.PocoArray);
            AssertListPoco(actual.PocoList, expected.PocoList);

            Assert.That(actual.PocoLookup.Count, Is.EqualTo(expected.PocoLookup.Count));
            foreach (var key in actual.PocoLookup.Keys)
                AssertListPoco(actual.PocoLookup[key], expected.PocoLookup[key]);

            Assert.That(actual.PocoLookupMap.Count, Is.EqualTo(expected.PocoLookupMap.Count));

            foreach(var key in actual.PocoLookupMap.Keys)
            {
                var actualList = actual.PocoLookupMap[key];
                var expectedList = expected.PocoLookupMap[key];

                Assert.That(actualList.Count, Is.EqualTo(expectedList.Count));
                for(int i = 0; i < actualList.Count; i++)
                {
                    Assert.That(actualList[i].Count, Is.EqualTo(expectedList[i].Count));

                    foreach (var key2 in actualList[i].Keys)
                    {
                        Assert.That(actualList[i][key2].Name, Is.EqualTo(expectedList[i][key2].Name));
                    }
                }
            }
        }

        public static void AssertListPoco(IList<Poco> actual, IList<Poco> expected)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));

            for (int i = 0; i < actual.Count; i++)
                Assert.That(actual[i].Name, Is.EqualTo(expected[i].Name));
        }
    }
}
