#if NETFRAMEWORK
using System;
using System.Runtime.Serialization;
using System.Web.Routing;
using NUnit.Framework;
using ServiceStack.Html;

namespace ServiceStack.Text.Tests.JsonTests
{
    [TestFixture]
    public class RouteValueDictionaryTests : TestBase
    {
        [Test, Ignore("Has both: ICollection<KeyValuePair> and IDictionary, test should be fixed or redesigned.")]
        public void Does_deserialize_RouteValueDictionary()
        {
            var item = new TestObject
            {
                PropA = "foo",
                Values = new RouteValueDictionary { { "something", "lies here" } }
            };

            var jsonSerialized = JsonSerializer.SerializeToString(item);
            var typeSerialized = TypeSerializer.SerializeToString(item);

            var jsonResult = JsonSerializer.DeserializeFromString<TestObject>(jsonSerialized);
            var typeResult = TypeSerializer.DeserializeFromString<TestObject>(typeSerialized);

            Assert.AreEqual(item.PropA, jsonResult.PropA);
            Assert.NotNull(jsonResult.Values);
            Assert.AreEqual(item.Values.Count, jsonResult.Values.Count);
            Assert.AreEqual(item.Values["something"], jsonResult.Values["something"]);

            Assert.AreEqual(item.PropA, typeResult.PropA);
            Assert.NotNull(typeResult.Values);
            Assert.AreEqual(item.Values.Count, typeResult.Values.Count);
            Assert.AreEqual(item.Values["something"], typeResult.Values["something"]);
        }

        [DataContract]
        class TestObject
        {
            [DataMember]
            public string PropA { get; set; }
            [DataMember]
            public RouteValueDictionary Values { get; set; }
        }
    }
}
#endif
