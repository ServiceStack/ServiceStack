using System;
using System.Collections.Generic;
using System.Dynamic;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class ExpandoTests : TestBase
    {
        [Test]
        public void Can_serialize_one_level_expando()
        {
            dynamic map = new ExpandoObject();
            map.One = 1;
            map.Two = 2;
            map.Three = 3;

            Serialize(map);
        }

        [Test]
        public void Can_serialize_empty_map()
        {
            var emptyMap = new ExpandoObject();

            Serialize(emptyMap);
        }

        [Test]
        public void Can_serialize_two_level_expando()
        {
            dynamic map1 = new ExpandoObject();
            map1.One = 1;
            map1.Two = 2;
            map1.Three = 3;

            dynamic map2 = new ExpandoObject();
            map2.Four = 4;
            map2.Five = 5;
            map2.Six = 6;

            dynamic map = new ExpandoObject();
            map.map1 = map1;
            map.map2 = map2;

            Serialize(map);
        }

        private static ExpandoObject SetupMap()
        {
            dynamic map = new ExpandoObject();
            map.a = "text";
            map.b = 32;
            map.c = false;
            map.d = new[] { 1, 2, 3 };
            return map;
        }

        public class MixType
        {
            public string a { get; set; }
            public int b { get; set; }
            public bool c { get; set; }
            public int[] d { get; set; }
        }

        private static void AssertMap(dynamic map)
        {
            Assert.AreEqual("text", map.a);
            Assert.AreEqual(32, map.b);
            Assert.AreEqual(false, map.c);
            Assert.AreEqual(new List<int> { 1, 2, 3 }, map.d);
        }

        [Test]
        public void Can_deserialize_mixed_expando_into_strongtyped_map()
        {
            var mixedMap = SetupMap();

            var json = JsonSerializer.SerializeToString(mixedMap);
            Console.WriteLine("JSON:\n" + json);

            var mixedType = json.FromJson<MixType>();
            Assert.AreEqual("text", mixedType.a);
            Assert.AreEqual(32, mixedType.b);
            Assert.AreEqual(false, mixedType.c);
        }

        [Test]
        public void Can_deserialize_two_level_expando()
        {
            JsConfig.TryToParsePrimitiveTypeValues = true;
            var json = "{\"a\":\"text\",\"b\":32,\"Map\":{\"a\":\"text\",\"b\":32,\"c\":false,\"d\":[1,2,3]}}";
            dynamic expandoObject = TypeSerializer.DeserializeFromString<ExpandoObject>(json);
            Assert.AreEqual("text", expandoObject.a);
            Assert.AreEqual(32, expandoObject.b);
            AssertMap((ExpandoObject)expandoObject.Map);
            JsConfig.TryToParsePrimitiveTypeValues = false;
        }

        [Test]
        public void Can_serialise_null_values_from_expando_correctly()
        {
            JsConfig.IncludeNullValuesInDictionaries = true;
            dynamic expando = new ExpandoObject();
            expando.value = null;

            Serialize(expando, includeXml: false);

            var json = JsonSerializer.SerializeToString(expando);
            Log(json);

            Assert.That(json, Is.EqualTo("{\"value\":null}"));
            JsConfig.Reset();
        }

        [Test]
        public void Will_ignore_null_values_from_expando_correctly()
        {
            JsConfig.IncludeNullValues = false;
            dynamic expando = new ExpandoObject();
            expando.value = null;

            Serialize(expando, includeXml: false);

            var json = JsonSerializer.SerializeToString(expando);
            Log(json);

            Assert.That(json, Is.EqualTo("{}"));
            JsConfig.Reset();
        }

        public class FooSlash
        {
            public ExpandoObject Nested { get; set; }
            public string Bar { get; set; }
        }

        [Test]
        public void Can_serialize_ExpandoObject_with_end_slash()
        {
            dynamic nested = new ExpandoObject();
            nested.key = "value\"";

            var foo = new FooSlash
            {
                Nested = nested,
                Bar = "BarValue"
            };
            Serialize(foo);
        }

        [Test]
        public void Can_serialise_null_values_from_nested_expando_correctly()
        {
            JsConfig.IncludeNullValues = true;
            var foo = new FooSlash();
            var json = JsonSerializer.SerializeToString(foo);
            Assert.That(json, Is.EqualTo("{\"Nested\":null,\"Bar\":null}"));
            JsConfig.Reset();
        }

        [Test]
        public void Can_serialize_ExpandoObject_with_quotes()
        {
            dynamic dto = new ExpandoObject();
            dto.title = "\"test\"";

            dynamic to = Serialize(dto);
            Assert.That(to.title, Is.EqualTo(dto.title));
        }

        [Test]
        public void Can_Deserialize_Object_To_ExpandoObject()
        {
            JsConfig.TryToParsePrimitiveTypeValues = true;
            const string json = "{\"Id\":1}";
            dynamic d = json.ConvertTo<ExpandoObject>();
            Assert.That(d.Id, Is.Not.Null);
            Assert.That(d.Id, Is.EqualTo(1));
        }
    }
}