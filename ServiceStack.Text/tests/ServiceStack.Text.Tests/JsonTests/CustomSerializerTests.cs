using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using System.Runtime.Serialization;
using System.Threading;

namespace ServiceStack.Text.Tests.JsonTests
{
    public class CustomSerializerTests : TestBase
    {
        static CustomSerializerTests()
        {
            JsConfig<EntityWithValues>.RawSerializeFn = SerializeEntity;
            JsConfig<EntityWithValues>.RawDeserializeFn = DeserializeEntity;
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            JsConfig.Reset();
        }

        [Test]
        public void Can_serialize_Entity()
        {
            var originalEntity = new EntityWithValues { id = 5, Values = new Dictionary<string, string> { { "dog", "bark" }, { "cat", "meow" } } };
            JsonSerializeAndCompare(originalEntity);
        }

        [Test]
        public void Can_serialize_arrays_of_entities()
        {
            var originalEntities = new[] { new EntityWithValues { id = 5, Values = new Dictionary<string, string> { { "dog", "bark" } } }, new EntityWithValues { id = 6, Values = new Dictionary<string, string> { { "cat", "meow" } } } };
            JsonSerializeAndCompare(originalEntities);
        }

        public class EntityWithValues
        {
            private Dictionary<string, string> _values;

            public int id { get; set; }

            public Dictionary<string, string> Values
            {
                get { return _values ?? (_values = new Dictionary<string, string>()); }
                set { _values = value; }
            }

            public override int GetHashCode()
            {
                return this.id.GetHashCode() ^ this.Values.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as EntityWithValues);
            }

            public bool Equals(EntityWithValues other)
            {
                return ReferenceEquals(this, other)
                       || (this.id == other.id && DictionaryEquality(Values, other.Values));
            }

            private bool DictionaryEquality(Dictionary<string, string> first, Dictionary<string, string> second)
            {
                return first.Count == second.Count
                       && first.Keys.All(second.ContainsKey)
                       && first.Keys.All(key => first[key] == second[key]);
            }
        }

        private static string SerializeEntity(EntityWithValues entity)
        {
            var dictionary = entity.Values.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (entity.id > 0)
            {
                dictionary["id"] = entity.id.ToString(CultureInfo.InvariantCulture);
            }
            return JsonSerializer.SerializeToString(dictionary);
        }

        private static EntityWithValues DeserializeEntity(string value)
        {
            var dictionary = JsonSerializer.DeserializeFromString<Dictionary<string, string>>(value);
            if (dictionary == null) return null;
            var entity = new EntityWithValues();
            foreach (var pair in dictionary)
            {
                if (pair.Key == "id")
                {
                    if (!string.IsNullOrEmpty(pair.Value))
                    {
                        entity.id = int.Parse(pair.Value);
                    }
                }
                else
                {
                    entity.Values.Add(pair.Key, pair.Value);
                }
            }
            return entity;
        }

        [DataContract]
        private class Test1Base
        {
            public Test1Base(bool itb, bool itbm)
            {
                InTest1Base = itb; InTest1BaseM = itbm;
            }

            [DataMember]
            public bool InTest1BaseM { get; set; }

            public bool InTest1Base { get; set; }
        }

        [DataContract]
        private class Test1 : Test1Base
        {
            public Test1(bool it, bool itm, bool itb, bool itbm)
                : base(itb, itbm)
            {
                InTest1 = it; InTest1M = itm;
            }

            [DataMember]
            public bool InTest1M { get; set; }

            public bool InTest1 { get; set; }
        }

        [Test]
        public void Can_Serialize_With_Custom_Constructor()
        {
            bool hit = false;
            JsConfig.ModelFactory = type =>
            {
                if (typeof(Test1) == type)
                {
                    hit = true;
                    return () => new Test1(false, false, true, false);
                }
                return null;
            };

            var t1 = new Test1(true, true, true, true);

            var data = JsonSerializer.SerializeToString(t1);

            var t2 = JsonSerializer.DeserializeFromString<Test1>(data);

            Assert.IsTrue(hit);
            Assert.IsTrue(t2.InTest1BaseM);
            Assert.IsTrue(t2.InTest1M);
            Assert.IsTrue(t2.InTest1Base);
            Assert.IsFalse(t2.InTest1);
        }


        public class Dto
        {
            public string Name { get; set; }
        }

        public interface IHasVersion
        {
            int Version { get; set; }
        }

        public class DtoV1 : IHasVersion
        {
            public int Version { get; set; }
            public string Name { get; set; }

            public DtoV1()
            {
                Version = 1;
            }
        }

        [Test]
        public void Can_detect_dto_with_no_Version()
        {
            using (JsConfig.With(new Config { ModelFactory = type =>
            {
                if (typeof(IHasVersion).IsAssignableFrom(type))
                {
                    return () =>
                    {
                        var obj = (IHasVersion)type.CreateInstance();
                        obj.Version = 0;
                        return obj;
                    };
                }
                return type.CreateInstance;
            }}))
            {
                var dto = new Dto { Name = "Foo" };
                var fromDto = dto.ToJson().FromJson<DtoV1>();
                Assert.That(fromDto.Version, Is.EqualTo(0));
                Assert.That(fromDto.Name, Is.EqualTo("Foo"));

                var dto1 = new DtoV1 { Name = "Foo 1" };
                var fromDto1 = dto1.ToJson().FromJson<DtoV1>();
                Assert.That(fromDto1.Version, Is.EqualTo(1));
                Assert.That(fromDto1.Name, Is.EqualTo("Foo 1"));
            }
        }

        public class ErrorPoco
        {
            public string ErrorCode { get; set; }
            public string ErrorDescription { get; set; }
        }

        [Test]
        public void Can_deserialize_json_with_underscores()
        {
            var json = "{\"error_code\":\"anErrorCode\",\"error_description\",\"the description\"}";

            var dto = json.FromJson<ErrorPoco>();

            Assert.That(dto.ErrorCode, Is.Null);

            using (JsConfig.With(new Config { PropertyConvention = PropertyConvention.Lenient }))
            {
                dto = json.FromJson<ErrorPoco>();

                Assert.That(dto.ErrorCode, Is.EqualTo("anErrorCode"));
                Assert.That(dto.ErrorDescription, Is.EqualTo("the description"));

                dto.PrintDump();
            }
        }
    }

    public class CustomSerailizerValueTypeTests
    {
        [Ignore("Needs to clear dirty static element caches from other tests"), Test]
        public void Can_serialize_custom_doubles()
        {
            JsConfig<double>.IncludeDefaultValue = true;
            JsConfig<double>.RawSerializeFn = d =>
                double.IsPositiveInfinity(d) ?
                  "\"+Inf\""
                : double.IsNegativeInfinity(d) ?
                 "\"-Inf\""
                : double.IsNaN(d) ?
                  "\"NaN\""
                : d.ToString();

            var doubles = new[] { 0.0, 1.0, double.NegativeInfinity, double.NaN, double.PositiveInfinity };

            Assert.That(doubles.ToJson(), Is.EqualTo("[0,1,\"-Inf\",\"NaN\",\"+Inf\"]"));

            Assert.That(new KeyValuePair<double, double>(0, 1).ToJson(),
                Is.EqualTo("{\"Key\":0,\"Value\":1}"));

            JsConfig.Reset();
        }

        public class ModelInt
        {
            public int Int { get; set; }
        }

        [Test]
        public void Can_serialize_custom_ints()
        {
            //JsConfig<int>.IncludeDefaultValue = true;
            JsConfig<int>.RawSerializeFn = i =>
                i == 0 ? "-1" : i.ToString();

            var dto = new ModelInt { Int = 0 };

            using (JsConfig.With(new Config { IncludeNullValues = true }))
            {
                Assert.That(dto.ToJson(), Is.EqualTo("{\"Int\":-1}"));
            }

            JsConfig.Reset();
        }

        public class ModelDecimal
        {
            public decimal Decimal { get; set; }
        }

        [Test]
        public void Can_customize_JSON_decimal()
        {
            JsConfig<decimal>.RawSerializeFn = d =>
                d.ToString(new CultureInfo("nl-NL"));

            var dto = new ModelDecimal { Decimal = 1.33m };

            Assert.That(dto.ToCsv(), Is.EqualTo("Decimal\r\n\"1,33\"\r\n"));
            Assert.That(dto.ToJsv(), Is.EqualTo("{Decimal:1,33}"));
            Assert.That(dto.ToJson(), Is.EqualTo("{\"Decimal\":1,33}"));
        }

        public class FormatAttribute : Attribute
        {
            string Format;

            public FormatAttribute(string format)
            {
                Format = format;
            }
        }

        public class DcStatus
        {
            [Format("{0:0.0} V")]
            public double Voltage { get; set; }

            [Format("{0:0.000} A")]
            public double Current { get; set; }

            [Format("{0:0} W")]
            public double Power => Voltage * Current;

            public string ToJson()
            {
                return new Dictionary<string, string>
                {
                    { "Voltage", string.Format(CultureInfo.InvariantCulture, "{0:0.0} V", Voltage)},
                    { "Current", string.Format(CultureInfo.InvariantCulture, "{0:0.000} A", Current)}, // Use $"{Current:0.000} A" if you don't care about culture
                    { "Power", $"{Power:0} W"},
                }.ToJson();
            }
        }

        public class DcStatusRawFn
        {
            [Format("{0:0.0} V")]
            public double Voltage { get; set; }

            [Format("{0:0.000} A")]
            public double Current { get; set; }

            [Format("{0:0} W")]
            public double Power => Voltage * Current;
        }

        [Test]
        public void Can_deserialize_using_CustomFormat()
        {
            var test = new DcStatus { Voltage = 10, Current = 1.2 };
            Assert.That(test.ToJson(), Is.EqualTo("{\"Voltage\":\"10.0 V\",\"Current\":\"1.200 A\",\"Power\":\"12 W\"}"));

            JsConfig<DcStatusRawFn>.RawSerializeFn = o => new Dictionary<string, string> {
                { "Voltage", string.Format(CultureInfo.InvariantCulture, "{0:0.0} V", o.Voltage)},
                { "Current", string.Format(CultureInfo.InvariantCulture, "{0:0.000} A", o.Current)},
                { "Power", $"{o.Power:0} W"},
            }.ToJson();

            var test2 = new DcStatusRawFn { Voltage = 10, Current = 1.2 };
            Assert.That(test2.ToJson(), Is.EqualTo("{\"Voltage\":\"10.0 V\",\"Current\":\"1.200 A\",\"Power\":\"12 W\"}"));

            JsConfig.Reset();
        }
    }

}