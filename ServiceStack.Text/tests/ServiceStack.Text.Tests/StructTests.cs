using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class StructTests
    {
#if !NETCORE
        [Serializable]
#endif
        public class Foo
        {
            public string Name { get; set; }

            public Text Content1 { get; set; }

            public Text Content2 { get; set; }
        }

        public interface IText { }

#if !NETCORE
        [Serializable]
#endif
        public struct Text
        {
            private readonly string _value;

            public Text(string value)
            {
                _value = value;
            }

            public static Text Parse(string value)
            {
                return value == null ? null : new Text(value);
            }

            public static implicit operator Text(string value)
            {
                return new Text(value);
            }

            public static implicit operator string(Text item)
            {
                return item._value;
            }

            public override string ToString()
            {
                return _value;
            }
        }

        [OneTimeSetUp]
        public void SetUp()
        {
#if IOS
            JsConfig.RegisterTypeForAot<Text> ();
            JsConfig.RegisterTypeForAot<Foo> ();
            JsConfig.RegisterTypeForAot<PersonStatus> ();
            JsConfig.RegisterTypeForAot<Person> ();
            JsConfig.RegisterTypeForAot<TestDictionary> ();
            JsConfig.RegisterTypeForAot<KeyValuePair<string, string>> ();
            JsConfig.RegisterTypeForAot<Pair> ();
#endif
        }

        [Test]
        public void Test_structs()
        {
            JsConfig<Text>.SerializeFn = text => text.ToString();

            var dto = new Foo { Content1 = "My content", Name = "My name" };

            var json = JsonSerializer.SerializeToString(dto, dto.GetType());

            Assert.That(json, Is.EqualTo("{\"Name\":\"My name\",\"Content1\":\"My content\"}"));
        }

        [Test]
        public void Test_structs_with_double_quotes()
        {
            var dto = new Foo { Content1 = "My \"quoted\" content", Name = "My \"quoted\" name" };

            JsConfig<Text>.SerializeFn = text => text.ToString();
            JsConfig<Text>.DeSerializeFn = v => new Text(v);

            var json = JsonSerializer.SerializeToString(dto, dto.GetType());
            Assert.That(json, Is.EqualTo("{\"Name\":\"My \\\"quoted\\\" name\",\"Content1\":\"My \\\"quoted\\\" content\"}"));

            var foo = JsonSerializer.DeserializeFromString<Foo>(json);
            Assert.That(foo.Name, Is.EqualTo(dto.Name));
            Assert.That(foo.Content1, Is.EqualTo(dto.Content1));
        }

        public enum PersonStatus
        {
            None,
            ActiveAgent,
            InactiveAgent
        }

        public class Person
        {
            //A bunch of other properties
            public PersonStatus Status { get; set; }
        }


        [Test]
        public void Test_enum_overloads()
        {
            JsConfig<Person>.TextCase = TextCase.CamelCase;
            JsConfig.IncludeNullValues = true;
            JsConfig<PersonStatus>.SerializeFn = text => text.ToString().ToCamelCase();

            var dto = new Person { Status = PersonStatus.ActiveAgent };

            var json = JsonSerializer.SerializeToString(dto);

            Assert.That(json, Is.EqualTo("{\"status\":\"activeAgent\"}"));

            Console.WriteLine(json);

            JsConfig.Reset();
        }

        
        public class TestDictionary
        {
            public Dictionary<string, string> Dictionary { get; set; }
            public List<KeyValuePair<string, string>> KvpList { get; set; }
            public IEnumerable<KeyValuePair<string, string>> KvpEnumerable { get; set; }
        }

        public class Pair
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }

        [Test]
        public void Serializes_ListOfKvp_AsPocoList()
        {
            var map = new Dictionary<string, string> { { "foo", "bar" }, { "x", "y" } };

            var dto = new TestDictionary
            {
                Dictionary = map,
                KvpList = map.ToList(),
                KvpEnumerable = map,
            };

            var json = dto.ToJson();

            Console.WriteLine(json);

            Assert.That(json, Is.EqualTo("{\"Dictionary\":{\"foo\":\"bar\",\"x\":\"y\"},"
                + "\"KvpList\":[{\"Key\":\"foo\",\"Value\":\"bar\"},{\"Key\":\"x\",\"Value\":\"y\"}],"
                + "\"KvpEnumerable\":[{\"Key\":\"foo\",\"Value\":\"bar\"},{\"Key\":\"x\",\"Value\":\"y\"}]}"));
        }

        [Test]
        public void Should_deserialize_KeyValuePair_with_int_DateTime()
        {
            var t = "{\"Key\":99,\"Value\":\"\\/Date(1320098400000+0200)\\/\"}";
            var b = JsonSerializer.DeserializeFromString<KeyValuePair<int, DateTime>>(t);
            Assert.That(b, Is.Not.Null);
            Assert.That(b.Key, Is.EqualTo(99));
            Assert.That(b.Value, Is.EqualTo(new DateTime(2011, 11, 1)));
        }

        public class TestKeyValuePair
        {
            public KeyValuePair<int?, bool?> KeyValue { get; set; }
            public string Name { get; set; }
        }

        [Test]
        public void Should_deserialize_class_with_KeyValuePair_with_nullables()
        {
            var t = "{\"KeyValue\":{\"Value\":true},\"Name\":\"test\"}";
            var b = JsonSerializer.DeserializeFromString<TestKeyValuePair>(t);
            Assert.That(b.KeyValue.Key, Is.Null);
            Assert.That(b.KeyValue.Value, Is.EqualTo(true));
            Assert.That(b.Name, Is.EqualTo("test"));
        }

        [Test]
        public void Can_TreatValueAsRefType()
        {
            JsConfig<UserStruct>.TreatValueAsRefType = true;

            var dto = new UserStruct { Id = 1, Name = "foo" };

            Assert.That(dto.ToJson(),
                Is.EqualTo("{\"Id\":1,\"Name\":\"foo\"}"));

            Assert.That(dto.ToJsv(),
                Is.EqualTo("{Id:1,Name:foo}"));
            Assert.That(dto.ToXml(),
                Is.EqualTo("<?xml version=\"1.0\" encoding=\"utf-8\"?><UserStruct xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/ServiceStack.Text.Tests\"><Id>1</Id><Name>foo</Name></UserStruct>"));
            JsConfig.Reset();
        }

#if !NETCORE 
        [Serializable]
#endif
        protected struct DangerousText1
        {
            public static object Parse(string text)
            {
                return new DangerousText1();
            }
        }

#if !NETCORE
        [Serializable]
#endif
        protected struct DangerousText2
        {
            public static int Parse(string text)
            {
                return 42;
            }
        }

        [Test]
        public void StaticParseMethod_will_not_throw_on_standard_usage()
        {
            ParseStringDelegate ret = null;
            Assert.DoesNotThrow(() => ret = StaticParseMethod<Text>.Parse);
            Assert.IsNotNull(ret);
        }

        [Test]
        public void StaticParseMethod_will_not_throw_on_old_usage()
        {
            ParseStringDelegate ret = null;
            Assert.DoesNotThrow(() => ret = StaticParseMethod<DangerousText1>.Parse);
            Assert.IsNotNull(ret);
        }

        [Test]
        public void StaticParseMethod_will_not_throw_on_unstandard_usage()
        {
            ParseStringDelegate ret = null;
            Assert.DoesNotThrow(() => ret = StaticParseMethod<DangerousText2>.Parse);
            Assert.IsNull(ret);
        }

#if !NETCORE
        [Explicit("Ensure this test has proven to work, before adding it to the test suite")]
        [Test]
        [TestCase("en")]
        [TestCase("en-US")]
        [TestCase("de-CH")]
        [TestCase("de")]
        public void test_rect_different_cultures(string culture)
        {
            var currentCulture = CultureInfo.GetCultureInfo(culture);
            Thread.CurrentThread.CurrentCulture = currentCulture;
            Thread.CurrentThread.CurrentUICulture = currentCulture;
            var s = new JsonSerializer<Rect>();
            var r = new Rect(23, 34, 1024, 768);
            var interim = s.SerializeToString(r);
            var r2 = s.DeserializeFromString(interim);
            Assert.AreEqual(r, r2);
        }
#endif
    }

    public struct UserStruct
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public struct Rect : IFormattable
    {
        double x;
        double y;
        double width;
        double height;

        public Rect(double x, double y, double width, double height)
        {
            if (width < 0 || height < 0)
                throw new ArgumentException("width and height must be non-negative.");
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return "{0},{1},{2},{3}".Fmt(x, y, width, height);
        }

        public static Rect Parse(string input)
        {
            var parts = input.Split(',');
            return new Rect(
                double.Parse(parts[0]),
                double.Parse(parts[1]),
                double.Parse(parts[2]),
                double.Parse(parts[3])
            );
        }
    }
}