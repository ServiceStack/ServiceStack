using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.Text.Json;


#if !IOS
using ServiceStack.Common.Tests.Models;
#endif

namespace ServiceStack.Text.Tests.JsonTests
{
    [TestFixture]
    public class BasicJsonTests
        : TestBase
    {
        public class JsonPrimitives
        {
            public int Int { get; set; }
            public long Long { get; set; }
            public float Float { get; set; }
            public double Double { get; set; }
            public bool Boolean { get; set; }
            public DateTime DateTime { get; set; }
            public string NullString { get; set; }
            public string EmptyString { get; set; }

            public static JsonPrimitives Create(int i)
            {
                return new JsonPrimitives
                {
                    Int = i,
                    Long = i,
                    Float = i,
                    Double = i,
                    Boolean = i % 2 == 0,
                    DateTime = DateTimeExtensions.FromUnixTimeMs(1),
                };
            }
        }

        public class NullableValueTypes
        {
            public int? Int { get; set; }
            public long? Long { get; set; }
            public decimal? Decimal { get; set; }
            public double? Double { get; set; }
            public bool? Boolean { get; set; }
            public DateTime? DateTime { get; set; }
        }

        [SetUp]
        public void Setup()
        {
        }

        [TearDown]
        public void TearDown()
        {
            JsConfig.Reset();
        }

        [Test]
        public void Can_parse_json_with_nullable_valuetypes()
        {
            var json = "{}";

            var item = JsonSerializer.DeserializeFromString<NullableValueTypes>(json);

            Assert.That(item.Int, Is.Null, "int");
            Assert.That(item.Long, Is.Null, "long");
            Assert.That(item.Decimal, Is.Null, "decimal");
            Assert.That(item.Double, Is.Null, "double");
            Assert.That(item.Boolean, Is.Null, "boolean");
            Assert.That(item.DateTime, Is.Null, "datetime");
        }

        [Test]
        public void Can_parse_json_with_nullable_valuetypes_that_has_included_null_values()
        {
            var json = "{\"Int\":null,\"Long\":null,\"Decimal\":null,\"Double\":null,\"Boolean\":null,\"DateTime\":null}";

            var item = JsonSerializer.DeserializeFromString<NullableValueTypes>(json);

            Assert.That(item.Int, Is.Null, "int");
            Assert.That(item.Long, Is.Null, "long");
            Assert.That(item.Decimal, Is.Null, "decimal");
            Assert.That(item.Double, Is.Null, "double");
            Assert.That(item.Boolean, Is.Null, "boolean");
            Assert.That(item.DateTime, Is.Null, "datetime");
        }

        [Test]
        public void Can_parse_json_with_nulls_or_empty_string_in_nullables()
        {
            const string json = "{\"Int\":null,\"Boolean\":\"\"}";
            var value = JsonSerializer.DeserializeFromString<NullableValueTypes>(json);

            Assert.That(value.Int, Is.EqualTo(null));
            Assert.That(value.Boolean, Is.EqualTo(null));
        }

        [Test]
        public void Can_parse_json_with_nullable_valuetypes_that_has_no_value_specified()
        {
            var json = "{\"Int\":,\"Long\":,\"Decimal\":,\"Double\":,\"Boolean\":,\"DateTime\":}";

            var item = JsonSerializer.DeserializeFromString<NullableValueTypes>(json);

            Assert.That(item.Int, Is.Null, "int");
            Assert.That(item.Long, Is.Null, "long");
            Assert.That(item.Decimal, Is.Null, "decimal");
            Assert.That(item.Double, Is.Null, "double");
            Assert.That(item.Boolean, Is.Null, "boolean");
            Assert.That(item.DateTime, Is.Null, "datetime");
        }

        [Test]
        public void Can_parse_mixed_list_nulls()
        {
            Assert.That(JsonSerializer.DeserializeFromString<List<string>>("[\"abc\",null,\"cde\",null]"),
                Is.EqualTo(new string[] { "abc", null, "cde", null }));
        }

        [Test]
        public void Can_parse_mixed_enumarable_nulls()
        {
            Assert.That(JsonSerializer.DeserializeFromString<IEnumerable<string>>("[\"abc\",null,\"cde\",null]"),
                Is.EqualTo(new string[] { "abc", null, "cde", null }));
        }

        [Test]
        public void Can_parse_mixed_enumarable_empty_strings()
        {
            Assert.That(JsonSerializer.DeserializeFromString<IEnumerable<string>>("[\"abc\",\"\",\"cde\",\"\"]"),
                Is.EqualTo(new string[] { "abc", "", "cde", "" }));
        }

        [Test]
        public void Can_handle_json_primitives()
        {
            var json = JsonSerializer.SerializeToString(JsonPrimitives.Create(1));
            Log(json);

            Assert.That(json, Is.EqualTo(
                "{\"Int\":1,\"Long\":1,\"Float\":1,\"Double\":1,\"Boolean\":false,\"DateTime\":\"\\/Date(1)\\/\"}"));
        }

        [Test]
        public void Can_parse_json_with_nulls()
        {
            const string json = "{\"Int\":1,\"NullString\":null,\"EmptyString\":\"\"}";
            var value = JsonSerializer.DeserializeFromString<JsonPrimitives>(json);

            Assert.That(value.Int, Is.EqualTo(1));
            Assert.That(value.NullString, Is.Null);
            Assert.That(value.EmptyString, Is.EqualTo(""));
        }

        [Test]
        public void Can_serialize_dictionary_of_int_int()
        {
            var json = JsonSerializer.SerializeToString<IntIntDictionary>(new IntIntDictionary() { Dictionary = { { 10, 100 }, { 20, 200 } } });
            const string expected = "{\"Dictionary\":{\"10\":100,\"20\":200}}";
            Assert.That(json, Is.EqualTo(expected));
        }

        private class IntIntDictionary
        {
            public IntIntDictionary()
            {
                Dictionary = new Dictionary<int, int>();
            }
            public IDictionary<int, int> Dictionary { get; set; }
        }

        [Test]
        public void Serialize_skips_null_values_by_default()
        {
            var o = new NullValueTester
            {
                Name = "Brandon",
                Type = "Programmer",
                SampleKey = 12,
                Nothing = (string)null,
                NullableDateTime = null
            };

            var s = JsonSerializer.SerializeToString(o);
            Assert.That(s, Is.EqualTo("{\"Name\":\"Brandon\",\"Type\":\"Programmer\",\"SampleKey\":12}"));
        }

        [Test]
        public void Serialize_can_include_null_values()
        {
            var o = new NullValueTester
            {
                Name = "Brandon",
                Type = "Programmer",
                SampleKey = 12,
                Nothing = null,
                NullClass = null,
                NullableDateTime = null,
            };

            JsConfig.IncludeNullValues = true;
            var s = JsonSerializer.SerializeToString(o);
            JsConfig.Reset();
            Assert.That(s, Is.EqualTo("{\"Name\":\"Brandon\",\"Type\":\"Programmer\",\"SampleKey\":12,\"Nothing\":null,\"NullClass\":null,\"NullableDateTime\":null}"));
        }

        private class NullClass
        {

        }

        [Test]
        public void Deserialize_sets_null_values()
        {
            var s = "{\"Name\":\"Brandon\",\"Type\":\"Programmer\",\"SampleKey\":12,\"Nothing\":null}";
            var o = JsonSerializer.DeserializeFromString<NullValueTester>(s);
            Assert.That(o.Name, Is.EqualTo("Brandon"));
            Assert.That(o.Type, Is.EqualTo("Programmer"));
            Assert.That(o.SampleKey, Is.EqualTo(12));
            Assert.That(o.Nothing, Is.Null);
        }

        [Test]
        public void Deserialize_ignores_omitted_values()
        {
            var s = "{\"Type\":\"Programmer\",\"SampleKey\":2}";
            var o = JsonSerializer.DeserializeFromString<NullValueTester>(s);
            Assert.That(o.Name, Is.EqualTo("Miguel"));
            Assert.That(o.Type, Is.EqualTo("Programmer"));
            Assert.That(o.SampleKey, Is.EqualTo(2));
            Assert.That(o.Nothing, Is.EqualTo("zilch"));
        }

        private class NullValueTester
        {
            public string Name
            {
                get;
                set;
            }

            public string Type
            {
                get;
                set;
            }

            public int SampleKey
            {
                get;
                set;
            }

            public string Nothing
            {
                get;
                set;
            }

            public NullClass NullClass { get; set; }

            public DateTime? NullableDateTime { get; set; }

            public NullValueTester()
            {
                Name = "Miguel";
                Type = "User";
                SampleKey = 1;
                Nothing = "zilch";
                NullableDateTime = new DateTime(2012, 01, 01);
            }
        }

#if !IOS
        [DataContract]
        class Person
        {
            [DataMember(Name = "MyID")]
            public int Id { get; set; }
            [DataMember]
            public string Name { get; set; }
        }

        [Test]
        public void Can_override_name()
        {
            var person = new Person
            {
                Id = 123,
                Name = "Abc"
            };

            Assert.That(TypeSerializer.SerializeToString(person), Is.EqualTo("{MyID:123,Name:Abc}"));
            Assert.That(JsonSerializer.SerializeToString(person), Is.EqualTo("{\"MyID\":123,\"Name\":\"Abc\"}"));
        }
#endif

        [Flags]
        public enum ExampleEnum : ulong
        {
            None = 0,
            One = 1,
            Two = 2,
            Four = 4,
            Eight = 8
        }

        [Test]
        public void Can_serialize_unsigned_flags_enum()
        {
            var anon = new
            {
                EnumProp1 = ExampleEnum.One | ExampleEnum.Two,
                EnumProp2 = ExampleEnum.Eight,
            };

            Assert.That(TypeSerializer.SerializeToString(anon), Is.EqualTo("{EnumProp1:3,EnumProp2:8}"));
            Assert.That(JsonSerializer.SerializeToString(anon), Is.EqualTo("{\"EnumProp1\":3,\"EnumProp2\":8}"));
        }

        public enum ExampleEnumWithoutFlagsAttribute : ulong
        {
            None = 0,
            One = 1,
            Two = 2
        }

        public class ClassWithEnumWithoutFlagsAttribute
        {
            public ExampleEnumWithoutFlagsAttribute EnumProp1 { get; set; }
            public ExampleEnumWithoutFlagsAttribute EnumProp2 { get; set; }
        }

        public class ClassWithNullableEnumWithoutFlagsAttribute
        {
            public ExampleEnumWithoutFlagsAttribute? EnumProp1 { get; set; }
        }

        [Test]
        public void Can_serialize_unsigned_enum_with_turned_on_TreatEnumAsInteger()
        {
            JsConfig.TreatEnumAsInteger = true;

            var anon = new ClassWithEnumWithoutFlagsAttribute
            {
                EnumProp1 = ExampleEnumWithoutFlagsAttribute.One,
                EnumProp2 = ExampleEnumWithoutFlagsAttribute.Two
            };

            Assert.That(JsonSerializer.SerializeToString(anon), Is.EqualTo("{\"EnumProp1\":1,\"EnumProp2\":2}"));
            Assert.That(TypeSerializer.SerializeToString(anon), Is.EqualTo("{EnumProp1:1,EnumProp2:2}"));
        }

        [Test]
        public void Can_serialize_nullable_enum_with_turned_on_TreatEnumAsInteger()
        {
            JsConfig.TreatEnumAsInteger = true;

            var anon = new ClassWithNullableEnumWithoutFlagsAttribute
            {
                EnumProp1 = ExampleEnumWithoutFlagsAttribute.One
            };

            Assert.That(JsonSerializer.SerializeToString(anon), Is.EqualTo("{\"EnumProp1\":1}"));
        }

        [Test]
        public void Can_deserialize_unsigned_enum_with_turned_on_TreatEnumAsInteger()
        {
            JsConfig.TreatEnumAsInteger = true;

            var s = "{\"EnumProp1\":1,\"EnumProp2\":2}";
            var o = JsonSerializer.DeserializeFromString<ClassWithEnumWithoutFlagsAttribute>(s);

            Assert.That(o.EnumProp1, Is.EqualTo(ExampleEnumWithoutFlagsAttribute.One));
            Assert.That(o.EnumProp2, Is.EqualTo(ExampleEnumWithoutFlagsAttribute.Two));
        }

        [Test]
        public void Can_serialize_object_array_with_nulls()
        {
            var objs = new[] { (object)"hello", (object)null };
            JsConfig.IncludeNullValues = false;

            Assert.That(objs.ToJson(), Is.EqualTo("[\"hello\",null]"));
        }


        [Test]
        public void Should_return_null_instance_for_empty_json()
        {
            var o = JsonSerializer.DeserializeFromString("", typeof(JsonPrimitives));
            Assert.IsNull(o);
        }

        [Test]
        public void Can_parse_empty_string_dictionary_with_leading_whitespace()
        {
            var serializer = new JsonSerializer<Dictionary<string, string>>();
            Assert.That(serializer.DeserializeFromString(" {}"), Is.Empty);
        }

        [Test]
        public void Can_parse_nonempty_string_dictionary_with_leading_whitespace()
        {
            var serializer = new JsonSerializer<Dictionary<string, string>>();
            var dictionary = serializer.DeserializeFromString(" {\"A\":\"N\",\"B\":\"O\"}");
            Assert.That(dictionary.Count, Is.EqualTo(2));
            Assert.That(dictionary["A"], Is.EqualTo("N"));
            Assert.That(dictionary["B"], Is.EqualTo("O"));
        }

        [Test]
        public void Can_parse_empty_dictionary_with_leading_whitespace()
        {
            var serializer = new JsonSerializer<Dictionary<int, double>>();
            Assert.That(serializer.DeserializeFromString(" {}"), Is.Empty);
        }

        [Test]
        public void Can_parse_nonempty_dictionary_with_leading_whitespace()
        {
            var serializer = new JsonSerializer<Dictionary<int, double>>();
            var dictionary = serializer.DeserializeFromString(" {\"1\":2.5,\"2\":5}");
            Assert.That(dictionary.Count, Is.EqualTo(2));
            Assert.That(dictionary[1], Is.EqualTo(2.5));
            Assert.That(dictionary[2], Is.EqualTo(5.0));
        }

        [Test]
        public void Can_parse_empty_hashtable_with_leading_whitespace()
        {
            var serializer = new JsonSerializer<Hashtable>();
            Assert.That(serializer.DeserializeFromString(" {}"), Is.Empty);
        }

        [Test]
        public void Can_parse_nonempty_hashtable_with_leading_whitespace()
        {
            var serializer = new JsonSerializer<Hashtable>();
            var hashtable = serializer.DeserializeFromString(" {\"A\":1,\"B\":2}");
            Assert.That(hashtable.Count, Is.EqualTo(2));
            Assert.That(hashtable["A"].ToString(), Is.EqualTo(1.ToString()));
            Assert.That(hashtable["B"].ToString(), Is.EqualTo(2.ToString()));
        }

        [Test]
        public void Can_parse_empty_json_object_with_leading_whitespace()
        {
            var serializer = new JsonSerializer<JsonObject>();
            Assert.That(serializer.DeserializeFromString(" {}"), Is.Empty);
        }

        [Test]
        public void Can_parse_nonempty_json_object_with_leading_whitespace()
        {
            var serializer = new JsonSerializer<JsonObject>();
            var jsonObject = serializer.DeserializeFromString(" {\"foo\":\"bar\"}");
            Assert.That(jsonObject, Is.Not.Empty);
            Assert.That(jsonObject["foo"], Is.EqualTo("bar"));
        }

        [Test]
        public void Can_parse_empty_key_value_pair_with_leading_whitespace()
        {
            var serializer = new JsonSerializer<KeyValuePair<string, string>>();
            Assert.That(serializer.DeserializeFromString(" {}"), Is.EqualTo(default(KeyValuePair<string, string>)));
        }

        [Test]
        public void Can_parse_nonempty_key_value_pair_with_leading_whitespace()
        {
            var serializer = new JsonSerializer<KeyValuePair<string, string>>();
            var keyValuePair = serializer.DeserializeFromString(" {\"Key\":\"foo\",\"Value\":\"bar\"}");
            Assert.That(keyValuePair, Is.EqualTo(new KeyValuePair<string, string>("foo", "bar")));
        }

        public class Foo
        {
            public string Bar { get; set; }
        }

        [Test]
        public void Can_parse_empty_object_with_leading_whitespace()
        {
            var serializer = new JsonSerializer<Foo>();
            var foo = serializer.DeserializeFromString(" {}");
            Assert.That(foo, Is.Not.Null);
            Assert.That(foo.Bar, Is.Null);
        }

        [Test]
        public void Can_parse_nonempty_object_with_leading_whitespace()
        {
            var serializer = new JsonSerializer<Foo>();
            var foo = serializer.DeserializeFromString(" {\"Bar\":\"baz\"}");
            Assert.That(foo, Is.Not.Null);
            Assert.That(foo.Bar, Is.EqualTo("baz"));
        }

        public class ModelWithDate
        {
            public DateTime? Date { get; set; }
        }

        [Test]
        public void Empty_string_converts_to_null_DateTime()
        {
            var json = "{\"Date\":\"\"}";
            var dto = json.FromJson<ModelWithDate>();

            Assert.That(dto.Date, Is.Null);
        }

        [DataContract]
        class ModelWithDataMemberField
        {
            public ModelWithDataMemberField() { }

            public ModelWithDataMemberField(string privateField, string privateProperty)
            {
                PrivateField = privateField;
                PrivateProperty = privateProperty;
            }

            [DataMember]
            public int Id;
            [DataMember]
            public string Name { get; set; }
            [DataMember]
            private string PrivateProperty { get; set; }
            [DataMember]
            private string PrivateField;

            public string GetPrivateProperty()
            {
                return PrivateProperty;
            }

            public string GetPrivateField()
            {
                return PrivateField;
            }
        }

        [Test]
        public void Explicit_DataMember_attribute_also_applies_to_public_fields()
        {
            var person = new ModelWithDataMemberField
            {
                Id = 1,
                Name = "A"
            };

            Assert.That(person.ToJsv().FromJsv<ModelWithDataMemberField>().Id, Is.EqualTo(1));
            Assert.That(person.ToJson().FromJson<ModelWithDataMemberField>().Id, Is.EqualTo(1));
        }

        [Test]
        public void Explicit_DataMember_attribute_serializers_private_properties_and_fields()
        {
            var person = new ModelWithDataMemberField("field", "property");

            Assert.That(person.ToJsv().FromJsv<ModelWithDataMemberField>().GetPrivateField(), Is.EqualTo("field"));
            Assert.That(person.ToJson().FromJson<ModelWithDataMemberField>().GetPrivateField(), Is.EqualTo("field"));

            Assert.That(person.ToJsv().FromJsv<ModelWithDataMemberField>().GetPrivateProperty(), Is.EqualTo("property"));
            Assert.That(person.ToJson().FromJson<ModelWithDataMemberField>().GetPrivateProperty(), Is.EqualTo("property"));
        }

        [Test]
        public void Can_include_null_values_for_adhoc_types()
        {
            Assert.That(new Foo().ToJson(), Is.EqualTo("{}"));

            JsConfig<Foo>.RawSerializeFn = obj =>
            {
                using (JsConfig.With(new Config { IncludeNullValues = true }))
                    return obj.ToJson();
            };

            Assert.That(new Foo().ToJson(), Is.EqualTo("{\"Bar\":null}"));

            JsConfig.Reset();
        }

        [Test]
        public void Can_run_FromJson_within_RawDeserializeFn()
        {
            JsConfig<Foo>.RawDeserializeFn = json =>
            {
                using (JsConfig.With(new Config { IncludeNullValues = true }))
                    return json.FromJson<Foo>();
            };

            var obj = "{\"Bar\":\"Bar\"}".FromJson<Foo>();

            Assert.That(obj.Bar, Is.EqualTo("Bar"));
        }

        [Test]
        public void Does_include_null_values_in_lists()
        {
            using (JsConfig.With(new Config { IncludeNullValues = true }))
            {
                var dto = new List<DateTime?>
                {
                    new DateTime(2000, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc),
                    null,
                    new DateTime(2000, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc),
                };

                var json = dto.ToJson();

                Assert.That(json, Is.EqualTo(@"[""\/Date(946684800000)\/"",null,""\/Date(978220800000)\/""]"));

                var fromJson = json.FromJson<List<DateTime?>>();

                Assert.That(fromJson.Count, Is.EqualTo(dto.Count));
            }
        }

        [Test]
        public void Can_deserialize_int_with_null_values()
        {
            var json = "{\"id\":null,\"name\":null}";
            var dto = json.FromJson<ModelWithIdAndName>();
            
            Assert.That(dto.Id, Is.EqualTo(default(int)));
            Assert.That(dto.Name, Is.Null);
        }
        
        public partial class ThrowValidation
        {
            public virtual int Age { get; set; }
            public virtual string Required { get; set; }
            public virtual string Email { get; set; }
        }

        [Test]
        public void Can_deserialize_ThrowValidation_with_null_values()
        {
            var json = "{\"version\":null,\"age\":null,\"required\":null,\"email\":\"invalidemail\"}";
            var dto = json.FromJson<ThrowValidation>();
            
            Assert.That(dto.Age, Is.EqualTo(default(int)));
            Assert.That(dto.Required, Is.Null);            
            Assert.That(dto.Email, Is.EqualTo("invalidemail"));            
        }

        class TestTrim
        {
            public string Description { get; set; }
        }

        [Test]
        public void Can_deserialize_custom_string_deserializer()
        {
            JsConfig<string>.DeSerializeFn = str => str.Trim();
            var json = "{\"description\":null}";
            var dto = json.FromJson<TestTrim>();
            Assert.That(dto.Description, Is.Null);
            JsConfig<string>.DeSerializeFn = null;
        }

        
    }
}
