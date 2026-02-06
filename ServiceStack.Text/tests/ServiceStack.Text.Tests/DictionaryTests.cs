using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ServiceStack.Text.Tests.DynamicModels.DataModel;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class DictionaryTests
        : TestBase
    {
        [OneTimeSetUp]
        public void SetUp()
        {
#if IOS
			JsConfig.RegisterTypeForAot<Dictionary<string, int>> ();
			JsConfig.RegisterTypeForAot<KeyValuePair<int, string>> ();

			JsConfig.RegisterTypeForAot<KeyValuePair<string, int>> ();
			JsConfig.RegisterTypeForAot<Dictionary<string, int>> ();

			JsConfig.RegisterTypeForAot<KeyValuePair<string, Dictionary<string, int>>> ();
			JsConfig.RegisterTypeForAot<Dictionary<string, Dictionary<string, int>>> ();

			JsConfig.RegisterTypeForAot<KeyValuePair<int, Dictionary<string, int>>> ();
			JsConfig.RegisterTypeForAot<Dictionary<int, Dictionary<string, int>>> ();


#endif
        }

        [TearDown]
        public void TearDown()
        {
            JsConfig.Reset();
        }

        [Test]
        public void Can_serialize_one_level_dictionary()
        {
            var map = new Dictionary<string, int>
              {
                {"One", 1}, {"Two", 2}, {"Three", 3},
              };

            Serialize(map);
        }

        [Test]
        public void Can_serialize_empty_map()
        {
            var emptyMap = new Dictionary<string, int>();

            Serialize(emptyMap);
        }

        [Test]
        public void Can_serialize_empty_string_map()
        {
            var emptyMap = new Dictionary<string, string>();

            Serialize(emptyMap);
        }

        [Test]
        public void Can_serialize_two_level_dictionary()
        {
            var map = new Dictionary<string, Dictionary<string, int>>
                  {
                    {"map1", new Dictionary<string, int>
                         {
                            {"One", 1}, {"Two", 2}, {"Three", 3},
                         }
                    },
                    {"map2", new Dictionary<string, int>
                         {
                            {"Four", 4}, {"Five", 5}, {"Six", 6},
                         }
                    },
                  };

            Serialize(map);
        }

        [Test]
        public void Can_serialize_two_level_dictionary_with_int_key()
        {
            var map = new Dictionary<int, Dictionary<string, int>>
                  {
                    {1, new Dictionary<string, int>
                         {
                            {"One", 1}, {"Two", 2}, {"Three", 3},
                         }
                    },
                    {2, new Dictionary<string, int>
                         {
                            {"Four", 4}, {"Five", 5}, {"Six", 6},
                         }
                    },
                  };

            Serialize(map);
        }

        [Test]
        public void Can_deserialize_two_level_dictionary_with_array()
        {
            JsConfig.TryToParsePrimitiveTypeValues = true;
            JsConfig.ConvertObjectTypesIntoStringDictionary = true;
            var original = new Dictionary<string, StrictType[]>
                  {
                    {"array",
                        new [] {
                            new StrictType { Name = "First" },
                            new StrictType { Name = "Second" },
                            new StrictType { Name = "Third" },
                        }
                    },
                  };
            var json = JsonSerializer.SerializeToString(original);
            var deserialized = JsonSerializer.DeserializeFromString<Dictionary<string, object>>(json);

            Console.WriteLine(json);

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized["array"], Is.Not.Null);
            Assert.That(((List<object>)deserialized["array"]).Count, Is.EqualTo(3));
            Assert.That(((List<object>)deserialized["array"])[0].ToJson(), Is.EqualTo("{\"Name\":\"First\"}"));
            Assert.That(((List<object>)deserialized["array"])[1].ToJson(), Is.EqualTo("{\"Name\":\"Second\"}"));
            Assert.That(((List<object>)deserialized["array"])[2].ToJson(), Is.EqualTo("{\"Name\":\"Third\"}"));
        }

        [Test]
        public void Can_deserialize_dictionary_with_special_characters_in_strings()
        {
            JsConfig.TryToParsePrimitiveTypeValues = true;
            JsConfig.ConvertObjectTypesIntoStringDictionary = true;

            var original = new Dictionary<string, string>
                  {
                    {"embeddedtypecharacters", "{{body}}"},
                    {"embeddedlistcharacters", "[stuff]"},
                    {"ShortDateTimeFormat", "yyyy-MM-dd"},
                    {"DefaultDateTimeFormat", "dd/MM/yyyy HH:mm:ss"},
                    {"DefaultDateTimeFormatWithFraction", "dd/MM/yyyy HH:mm:ss.fff"},
                    {"XsdDateTimeFormat", "yyyy-MM-ddTHH:mm:ss.fffffffZ"},
                    {"XsdDateTimeFormat3F", "yyyy-MM-ddTHH:mm:ss.fffZ"},
                    {"XsdDateTimeFormatSeconds", "yyyy-MM-ddTHH:mm:ssZ"},
                    {"ShouldBeAZeroInAString", "0"},
                    {"ShouldBeAPositiveIntegerInAString", "12345"},
                    {"ShouldBeANegativeIntegerInAString", "-12345"},
                  };
            var json = JsonSerializer.SerializeToString(original);
            var deserialized = JsonSerializer.DeserializeFromString<Dictionary<string, object>>(json);

            Console.WriteLine(json);

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized["embeddedtypecharacters"], Is.Not.Null);
            Assert.That(deserialized["embeddedtypecharacters"], Is.EqualTo("{{body}}"));
            Assert.That(deserialized["embeddedlistcharacters"], Is.EqualTo("[stuff]"));
            Assert.That(deserialized["ShortDateTimeFormat"], Is.EqualTo("yyyy-MM-dd"));
            Assert.That(deserialized["DefaultDateTimeFormat"], Is.EqualTo("dd/MM/yyyy HH:mm:ss"));
            Assert.That(deserialized["DefaultDateTimeFormatWithFraction"], Is.EqualTo("dd/MM/yyyy HH:mm:ss.fff"));
            Assert.That(deserialized["XsdDateTimeFormat"], Is.EqualTo("yyyy-MM-ddTHH:mm:ss.fffffffZ"));
            Assert.That(deserialized["XsdDateTimeFormat3F"], Is.EqualTo("yyyy-MM-ddTHH:mm:ss.fffZ"));
            Assert.That(deserialized["XsdDateTimeFormatSeconds"], Is.EqualTo("yyyy-MM-ddTHH:mm:ssZ"));
            Assert.That(deserialized["ShouldBeAZeroInAString"], Is.EqualTo("0"));
            Assert.That(deserialized["ShouldBeAZeroInAString"], Is.InstanceOf<string>());
            Assert.That(deserialized["ShouldBeAPositiveIntegerInAString"], Is.EqualTo("12345"));
            Assert.That(deserialized["ShouldBeAPositiveIntegerInAString"], Is.InstanceOf<string>());
            Assert.That(deserialized["ShouldBeANegativeIntegerInAString"], Is.EqualTo("-12345"));
        }

        private static Dictionary<string, object> SetupDict()
        {
            return new Dictionary<string, object> {
                { "a", "text" },
                { "b", 32 },
                { "c", false },
                { "d", new[] {1, 2, 3} },
                { "e", 1m },
                { "f", 1.1m },
            };
        }

        public class MixType
        {
            public string a { get; set; }
            public int b { get; set; }
            public bool c { get; set; }
            public int[] d { get; set; }
        }

        private static void AssertDict(Dictionary<string, object> dict)
        {
            Assert.AreEqual("text", dict["a"]);
            Assert.AreEqual(32, dict["b"]);
            Assert.AreEqual(false, dict["c"]);
        }

        //[Test]
        //public void Test_JsonNet()
        //{
        //    var dict = SetupDict();
        //    var json = JsonConvert.SerializeObject(dict);
        //    var deserializedDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        //    AssertDict(deserializedDict);
        //}

        [Test]
        public void Test_ServiceStack_Text_TypeSerializer()
        {
            JsConfig.TryToParsePrimitiveTypeValues = true;
            JsConfig.ConvertObjectTypesIntoStringDictionary = true;

            var dict = SetupDict();
            var json = TypeSerializer.SerializeToString(dict);
            var deserializedDict = TypeSerializer.DeserializeFromString<Dictionary<string, object>>(json);
            AssertDict(deserializedDict);
        }

        [Test]
        public void Test_ServiceStack_Text_JsonSerializer()
        {
            JsConfig.TryToParsePrimitiveTypeValues = true;
            JsConfig.ConvertObjectTypesIntoStringDictionary = true;

            var dict = SetupDict();
            var json = JsonSerializer.SerializeToString(dict);
            var deserializedDict = JsonSerializer.DeserializeFromString<Dictionary<string, object>>(json);
            AssertDict(deserializedDict);
        }

        [Test]
        public void Test_ServiceStack_Text_JsonSerializer_Array_Value_Deserializes_Correctly()
        {
            JsConfig.TryToParsePrimitiveTypeValues = true;
            JsConfig.ConvertObjectTypesIntoStringDictionary = true;

            var dict = SetupDict();
            var json = JsonSerializer.SerializeToString(dict);
            var deserializedDict = JsonSerializer.DeserializeFromString<Dictionary<string, object>>(json);
            Assert.AreEqual("text", deserializedDict["a"]);
            Assert.AreEqual(new List<int> { 1, 2, 3 }, deserializedDict["d"]);
        }

        [Test]
        public void Deserializes_to_decimal_by_default()
        {
            JsConfig.TryToParsePrimitiveTypeValues = true;

            var dict = SetupDict();
            var json = JsonSerializer.SerializeToString(dict);
            var deserializedDict = JsonSerializer.DeserializeFromString<IDictionary<string, object>>(json);
            Assert.That(deserializedDict["f"], Is.TypeOf<decimal>());
            Assert.That(deserializedDict["f"], Is.EqualTo(1.1m));
        }

        class NumericType
        {
            public NumericType(decimal max, Type type)
                : this(0, max, type) { }

            public NumericType(decimal min, decimal max, Type type)
            {
                Min = min;
                Max = max;
                Type = type;
            }

            public decimal Min { get; private set; }
            public decimal Max { get; private set; }
            public Type Type { get; private set; }
        }

        [Test]
        public void Deserializes_signed_bytes_into_to_best_fit_numeric()
        {
            JsConfig.TryToParsePrimitiveTypeValues = true;
            JsConfig.TryToParseNumericType = true;

            var deserializedDict = JsonSerializer.DeserializeFromString<IDictionary<string, object>>("{\"min\":-128,\"max\":127}");
            Assert.That(deserializedDict["min"], Is.TypeOf<sbyte>());
            Assert.That(deserializedDict["min"], Is.EqualTo(sbyte.MinValue));
            //it seemed strange having zero return as a signed byte
            Assert.That(deserializedDict["max"], Is.TypeOf<byte>());
            Assert.That(deserializedDict["max"], Is.EqualTo(sbyte.MaxValue));
        }

#if NETFX
        [Test]
        public void Deserializes_floats_into_to_best_fit_floating_point()
        {
            JsConfig.TryToParsePrimitiveTypeValues = true;
            JsConfig.TryToParseNumericType = true;
            JsConfig.ParsePrimitiveFloatingPointTypes = ParseAsType.Single | ParseAsType.Double;

            float floatValue = 1.1f;
            //TODO find a number that doesn't suck which throws in float.Parse() but not double.Parse()
            double Offset = Math.Pow(2, 1000);
            double doubleValue = double.MaxValue - Offset;
            var intValue = int.MaxValue;
            var longValue = long.MaxValue;

            float notFloat;
            Assert.That(!float.TryParse(doubleValue.ToString(), out notFloat));

            var toFloatValue = float.Parse(floatValue.ToString());
            Assert.AreEqual(toFloatValue, floatValue, 1);
            var toDoubleValue = double.Parse(doubleValue.ToString());
            Assert.AreEqual(toDoubleValue, doubleValue, Offset);

            var json = "{{\"float\":{0},\"double\":{1},\"int\":{2},\"long\":{3}}}"
                .Fmt(CultureInfo.InvariantCulture, floatValue, doubleValue, intValue, longValue);
            var map = JsonSerializer.DeserializeFromString<IDictionary<string, object>>(json);

            Assert.That(map["float"], Is.TypeOf<float>());
            Assert.That(map["float"], Is.EqualTo(floatValue));
 
            Assert.That(map["double"], Is.TypeOf<double>());
            Assert.AreEqual((double)map["double"], doubleValue, Offset);

            Assert.That(map["int"], Is.TypeOf<int>());
            Assert.That(map["int"], Is.EqualTo(intValue));

            Assert.That(map["long"], Is.TypeOf<long>());
            Assert.That(map["long"], Is.EqualTo(longValue));

            JsConfig.Reset();
        }
#endif

        [Test]
        public void Deserializes_signed_types_into_to_best_fit_numeric()
        {
            var unsignedTypes = new[]
                {
                    new NumericType(Int16.MinValue,Int16.MaxValue, typeof (Int16)),
                    new NumericType(Int32.MinValue,Int32.MaxValue, typeof (Int32)),
                    new NumericType(Int64.MinValue,Int64.MaxValue, typeof (Int64)),
                };

            JsConfig.TryToParsePrimitiveTypeValues = true;
            JsConfig.TryToParseNumericType = true;


            foreach (var signedType in unsignedTypes)
            {
                var dict = new Dictionary<string, object>
                {
                    {"min",signedType.Min},
                    {"max",signedType.Max},
                };

                var json = JsonSerializer.SerializeToString(dict);
                var deserializedDict = JsonSerializer.DeserializeFromString<IDictionary<string, object>>(json);
                Assert.That(deserializedDict["min"], Is.TypeOf(signedType.Type));
                Assert.That(deserializedDict["min"], Is.EqualTo(signedType.Min));
                Assert.That(deserializedDict["max"], Is.TypeOf(signedType.Type));
                Assert.That(deserializedDict["max"], Is.EqualTo(signedType.Max));

            }
        }

        [Test]
        public void Deserializes_unsigned_types_into_to_best_fit_numeric()
        {
            var unsignedTypes = new[]
                {
                    new NumericType(byte.MinValue,byte.MaxValue, typeof (byte)),
                    new NumericType(UInt16.MaxValue, typeof (UInt16)),
                    new NumericType(UInt32.MaxValue, typeof (UInt32)),
                    new NumericType(UInt64.MaxValue, typeof (UInt64)),
                };

            JsConfig.TryToParsePrimitiveTypeValues = JsConfig.TryToParseNumericType = JsConfig.TryParseIntoBestFit = true;

            foreach (var unsignedType in unsignedTypes)
            {
                var dict = new Dictionary<string, object>
                {
                    {"min",unsignedType.Min},
                    {"max",unsignedType.Max},
                };

                var json = JsonSerializer.SerializeToString(dict);
                var deserializedDict = JsonSerializer.DeserializeFromString<IDictionary<string, object>>(json);
                Assert.That(deserializedDict["min"], Is.EqualTo(0));
                Assert.That(deserializedDict["min"], Is.TypeOf<byte>());
                Assert.That(deserializedDict["max"], Is.TypeOf(unsignedType.Type));
                Assert.That(deserializedDict["max"], Is.EqualTo(unsignedType.Max));
            }

            JsConfig.Reset();
        }

        [Test]
        public void Can_deserialize_mixed_dictionary_into_strongtyped_map()
        {
            var mixedMap = SetupDict();

            var json = JsonSerializer.SerializeToString(mixedMap);
            Console.WriteLine("JSON:\n" + json);

            var mixedType = json.FromJson<MixType>();
            Assert.AreEqual("text", mixedType.a);
            Assert.AreEqual(32, mixedType.b);
            Assert.AreEqual(false, mixedType.c);
            Assert.AreEqual(new[] { 1, 2, 3 }, mixedType.d);
        }

        [Test]
        public void Can_serialise_null_values_from_dictionary_correctly()
        {
            JsConfig.IncludeNullValues = false;
            JsConfig.IncludeNullValuesInDictionaries = true;
            var dictionary = new Dictionary<string, object> { { "value", null } };

            Serialize(dictionary, includeXml: false);

            var json = JsonSerializer.SerializeToString(dictionary);
            Log(json);

            Assert.That(json, Is.EqualTo("{\"value\":null}"));
            JsConfig.Reset();
        }

        [Test]
        public void Will_ignore_null_values_from_dictionary_correctly()
        {
            JsConfig.IncludeNullValues = true;
            JsConfig.IncludeNullValuesInDictionaries = false;
            var dictionary = new Dictionary<string, string> { { "value", null } };

            Serialize(dictionary, includeXml: false);

            var json = JsonSerializer.SerializeToString(dictionary);
            Log(json);

            Assert.That(json, Is.EqualTo("{}"));
            JsConfig.Reset();
        }

        public class FooSlash
        {
            public Dictionary<string, string> Nested { get; set; }
            public string Bar { get; set; }
        }

        [Test]
        public void Can_serialize_Dictionary_with_end_slash()
        {
            var foo = new FooSlash
            {
                Nested = new Dictionary<string, string> { { "key", "value\"" } },
                Bar = "BarValue"
            };
            Serialize(foo);
        }

        [Test]
        public void Can_serialise_null_values_from_nested_dictionary_correctly()
        {
            JsConfig.IncludeNullValues = true;
            var foo = new FooSlash();
            var json = JsonSerializer.SerializeToString(foo);
            Assert.That(json, Is.EqualTo("{\"Nested\":null,\"Bar\":null}"));
            JsConfig.Reset();
        }

        [Test]
        public void Can_serialize_Dictionary_with_quotes()
        {
            var dto = new Dictionary<string, string> { { "title", "\"test\"" } };
            var to = Serialize(dto);

            Assert.That(to["title"], Is.EqualTo(dto["title"]));
        }

        [Test]
        public void Can_serialize_Dictionary_with_escaped_symbols_in_key()
        {
            var dto = new Dictionary<string, string> { { @"a\fb", "\"test\"" } };
            var to = Serialize(dto);

            Assert.That(to.Keys.ToArray()[0], Is.EqualTo(@"a\fb"));
        }

        [Test]
        public void Can_serialize_Dictionary_with_escaped_symbols_in_key_and_binary_value()
        {
            var dto = new Dictionary<string, byte[]> { { @"a\fb", new byte[] { 1 } } };
            var to = Serialize(dto);

            Assert.That(to.Keys.ToArray()[0], Is.EqualTo(@"a\fb"));
        }

        [Test]
        public void Can_serialize_Dictionary_with_int_key_and_string_with_quote()
        {
            var dto = new Dictionary<int, string> { { 1, @"a""b" } };
            var to = Serialize(dto);

            Assert.That(to.Keys.ToArray()[0], Is.EqualTo(1));
            Assert.That(to[1], Is.EqualTo(@"a""b"));
        }

        [Test]
        public void Can_serialize_string_byte_Dictionary_with_UTF8()
        {
            var dto = new Dictionary<string, byte[]> { { "aфаž\"a", new byte[] { 1 } } };
            var to = Serialize(dto);

            Assert.That(to.Keys.ToArray()[0], Is.EqualTo("aфаž\"a"));
        }

        [Test]
        public void Can_serialize_string_string_Dictionary_with_UTF8()
        {
            var dto = new Dictionary<string, string> { { "aфаž\"a", "abc" } };
            var to = Serialize(dto);

            Assert.That(to.Keys.ToArray()[0], Is.EqualTo("aфаž\"a"));
        }

        [Test]
        public void Can_Deserialize_Object_To_Dictionary()
        {
            const string json = "{\"Id\":1}";
            var d = json.ConvertTo<Dictionary<string, string>>();
            Assert.That(d.ContainsKey("Id"));
            Assert.That(d["Id"], Is.EqualTo("1"));
        }

        [Test]
        public void Nongeneric_implementors_of_IDictionary_K_V_Should_serialize_like_Dictionary_K_V()
        {
            dynamic expando = new System.Dynamic.ExpandoObject();
            expando.Property = "Value";
            IDictionary<string, object> dict = expando;
            Assert.AreEqual(dict.Dump(), new Dictionary<string, object>(dict).Dump());
        }

        [Test]
        public void Can_serialize_OrderedDictionary()
        {
            var dto = new OrderedDictionary { { "A", 1 }, { "B", "2" }, { "C", true } };
            var to = Serialize(dto, includeXml: false);

            Assert.That(to["A"], Is.EqualTo(1));
            Assert.That(to["B"], Is.EqualTo("2"));
            Assert.That(to["C"], Is.EqualTo(true));
        }

        [Test]
        public void Can_deserialize_OrderedDictionary()
        {
            var original = new OrderedDictionary {
                {"Key1", "Value1"},
                {"Key2", 2},
                {3, "Value3"},
                {"Key4", false}
              };
            var json = JsonSerializer.SerializeToString(original);
            var deserialized = JsonSerializer.DeserializeFromString<OrderedDictionary>(json);

            json.Print();

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized["Key1"], Is.EqualTo("Value1"));
            Assert.That(deserialized["Key2"], Is.EqualTo(2));
            Assert.That(deserialized[2], Is.EqualTo("Value3"));
            Assert.That(deserialized["Key4"], Is.EqualTo(false));
        }

        [Test]
        public void Can_deserialize_OrderedDictionary_subclass()
        {
            var original = new OrderedDictionarySub {
                {"Key1", "Value1"},
                {"Key2", 2},
                {3, "Value3"},
                {"Key4", false}
              };
            var json = JsonSerializer.SerializeToString(original);
            var deserialized = JsonSerializer.DeserializeFromString<OrderedDictionarySub>(json);

            json.Print();

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized["Key1"], Is.EqualTo("Value1"));
            Assert.That(deserialized["Key2"], Is.EqualTo(2));
            Assert.That(deserialized[2], Is.EqualTo("Value3"));
            Assert.That(deserialized["Key4"], Is.EqualTo(false));
        }

        [Test]
        public void Can_recover_from_exceptions_when_serializing_dictionary_keys()
        {
            var before = JsConfig<int>.SerializeFn;
            try
            {
                JsConfig<int>.SerializeFn = v =>
                {
                    throw new Exception("Boom!");
                };
                var target = new Dictionary<int, string>
                {
                    { 1, "1" },
                };
                Assert.Throws<Exception>(() => JsonSerializer.SerializeToString(target));
            }
            finally
            {
                JsConfig<int>.SerializeFn = before;
            }
            var json = JsonSerializer.SerializeToString(new ModelWithDictionary());

            json.Print();

            Assert.That(json.StartsWith("{"));
        }

        private class ModelWithDictionary
        {
            public Dictionary<string, string> Value { get; set; }
        }

        [Test]
        public void Do_not_convert_Guids()
        {
            JsConfig.DateHandler = DateHandler.ISO8601;
            JsConfig.AlwaysUseUtc = true;
            JsConfig.TryToParsePrimitiveTypeValues = true;  // needed for datetime

            var isGuidRegex =
              new Regex(@"^(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}$", RegexOptions.Compiled);

            JsConfig.ParsePrimitiveFn = s => isGuidRegex.IsMatch(s) ? s : null;

            var original = new Dictionary<string, object>
               {
                   {"GuidString", "6A3F0923-A4B8-4026-9982-5C79128EA128"},
                   {"DateTime", DateTime.UtcNow}
               };

            var json = JsonSerializer.SerializeToString(original);

            json.Print();

            var deserialized = JsonSerializer.DeserializeFromString<Dictionary<string, object>>(json);

            Assert.That(deserialized["GuidString"], Is.EqualTo("6A3F0923-A4B8-4026-9982-5C79128EA128"));
            Assert.That(deserialized["DateTime"], Is.AssignableTo(typeof(DateTime)));

            JsConfig.Reset();
        }
    }

    public class OrderedDictionarySub : OrderedDictionary { }
}