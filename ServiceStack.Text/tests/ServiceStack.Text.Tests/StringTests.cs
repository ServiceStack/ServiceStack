using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using NUnit.Framework;
using ServiceStack.Text.Tests.Support;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class StringTests
    {
        [Test]
        public void SerializerTests()
        {
            string v = "This is a string";

            // serialize to JSON using ServiceStack
            string jsonString = JsonSerializer.SerializeToString(v);

            // serialize to JSON using BCL
            //var bclJsonString = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(v);
            var bclJsonString = BclJsonDataContractSerializer.Instance.Parse(v);

            string correctJSON = "\"This is a string\""; // this is what a modern browser will produce with JSON.stringify("This is a string");

            Assert.AreEqual(correctJSON, bclJsonString, "BCL serializes string correctly");
            Assert.AreEqual(correctJSON, jsonString, "ServiceStack serializes string correctly");
        }

        [Test]
        public void Deserializes_string_correctly()
        {
            const string original = "This is a string";
            var json = JsonSerializer.SerializeToString(original);
            var fromJson = JsonSerializer.DeserializeFromString<string>(json);
            var fromJsonType = JsonSerializer.DeserializeFromString(json, typeof(string));

            Assert.That(fromJson, Is.EqualTo(original));
            Assert.That(fromJsonType, Is.EqualTo(original));
        }

        [Test]
        public void Embedded_Quotes()
        {
            string v = @"I have ""embedded quotes"" inside me";

            // serialize to JSON using ServiceStack
            string jsonString = JsonSerializer.SerializeToString(v);

            // serialize to JSON using BCL
            //var bclJsonString = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(v);
            var bclJsonString = BclJsonDataContractSerializer.Instance.Parse(v);

            string correctJSON = @"""I have \""embedded quotes\"" inside me"""; // this is what a modern browser will produce with JSON.stringify("This is a string");

            Assert.AreEqual(correctJSON, bclJsonString, "BCL serializes string correctly");
            Assert.AreEqual(correctJSON, jsonString, "ServiceStack serializes string correctly");
        }

        [Test]
        public void RoundTripTest()
        {
            string json = "\"This is a string\"";
            string correctString = "This is a string"; // this is what a modern browser will produce from JSON.parse("\"This is a string\"");

            //var bclString = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<string>(json);
            var bclString = BclJsonDataContractDeserializer.Instance.Parse<string>(json);
            var ssString = ServiceStack.Text.JsonSerializer.DeserializeFromString<string>(json);

            Assert.AreEqual(correctString, bclString, "BCL deserializes correctly");
            Assert.AreEqual(correctString, ssString, "ServiceStack deserializes correctly");

            var ssJson = ServiceStack.Text.JsonSerializer.SerializeToString(ssString, typeof(string));
            //var bclJson = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(bclString);
            var bclJson = BclJsonDataContractSerializer.Instance.Parse(bclString);

            Assert.AreEqual(json, bclJson, "BCL round trips correctly");
            Assert.AreEqual(json, ssJson, "ServiceStack round trips correctly");
        }

        [Test]
        public void Deserializes_string_with_quotes_correctly()
        {
            const string original = "\"This is a string surrounded with quotes\"";
            var json = JsonSerializer.SerializeToString(original);
            var bclJson = BclJsonDataContractSerializer.Instance.Parse(original);
            Assert.That(json, Is.EqualTo(bclJson));

            var fromJson = JsonSerializer.DeserializeFromString<string>(json);
            var fromJsonBcl = BclJsonDataContractDeserializer.Instance.Parse<string>(json);
            Assert.That(fromJson, Is.EqualTo(fromJsonBcl));

            var fromJsonType = JsonSerializer.DeserializeFromString(json, typeof(string));

            "{0}||{1}".Print(json, fromJson);

            Assert.That(fromJson, Is.EqualTo(original));
            Assert.That(fromJsonType, Is.EqualTo(original));
        }

        public class Poco
        {
            public string Name { get; set; }
        }

        [Test]
        public void Deserializes_Poco_with_string_with_quotes_correctly()
        {
            var original = new Poco { Name = "\"This is a string surrounded with quotes\"" };
            var json = JsonSerializer.SerializeToString(original);
            var fromJson = JsonSerializer.DeserializeFromString<Poco>(json);
            var fromJsonType = (Poco)JsonSerializer.DeserializeFromString(json, typeof(Poco));

            "{0}||{1}".Print(json, fromJson);

            Assert.That(fromJson.Name, Is.EqualTo(original.Name));
            Assert.That(fromJsonType.Name, Is.EqualTo(original.Name));
        }

        [Test]
        public void Starting_with_quotes_inside_POCOs()
        {
            var dto = new Poco { Name = "\"starting with\" POCO" };

            var json = dto.ToJson();

            var fromDto = json.FromJson<Poco>();

            Assert.That(fromDto.Name, Is.EqualTo(dto.Name));
        }

        public class CharDataTypeTest
        {
            public char Code { get; set; }
        }

        [Test]
        public void Quotes_Inside_Char_Field_In_Poco()
        {
            var charDataTypeTest = new CharDataTypeTest { Code = '\"' };
            string jsonString = JsonSerializer.SerializeToString(charDataTypeTest);
            string correctJSON = @"{""Code"":""\""""}";  //should be {"Code":"\""} 
            Assert.That(jsonString, Is.EqualTo(correctJSON));
        }

        Movie dto = new Movie
        {
            ImdbId = "tt0111161",
            Title = "The Shawshank Redemption",
            Rating = 9.2m,
            Director = "Frank Darabont",
            ReleaseDate = new DateTime(1995, 2, 17),
            TagLine = "Fear can hold you prisoner. Hope can set you free.",
            Genres = new List<string> { "Crime", "Drama" },
        };

        [Test]
        public void Can_toXml()
        {
            var xml = dto.ToXml();
            var fromXml = xml.FromXml<Movie>();
            Assert.That(fromXml.ImdbId, Is.EqualTo(dto.ImdbId));
        }

        [Test]
        public void Can_toJson()
        {
            var json = dto.ToJson();
            var fromJson = json.FromJson<Movie>();
            Assert.That(fromJson.ImdbId, Is.EqualTo(dto.ImdbId));
        }

        [Test]
        public void Can_toJsv()
        {
            var jsv = dto.ToJsv();
            var fromJsv = jsv.FromJsv<Movie>();
            Assert.That(fromJsv.ImdbId, Is.EqualTo(dto.ImdbId));
        }

        public class OrderModel
        {
            public string OrderType { get; set; }
            public decimal Price { get; set; }
            public int Lot { get; set; }
        }

        [Test]
        public void Can_toJson_than_toXml()
        {
            var orderModel = new OrderModel
            {
                OrderType = "BUY",
                Price = 2400,
                Lot = 5
            };

            var json = orderModel.ToJson();
            var fromJson = json.FromJson<OrderModel>();
            Assert.That(fromJson.OrderType, Is.EqualTo(orderModel.OrderType));

            var xml = orderModel.ToXml();
            var fromXml = xml.FromXml<OrderModel>();
            Assert.That(fromXml.OrderType, Is.EqualTo(orderModel.OrderType));
        }

        [Test]
        public void Serializes_Poco_with_string_property()
        {
            var original = new Poco { Name = "\"This is a string surrounded with quotes\"" };
            var originalEmpty = new Poco { Name = "" };
            var originalNull = new Poco { Name = null };
            var jsv = TypeSerializer.SerializeToString<Poco>(original);
            var jsvEmpty = TypeSerializer.SerializeToString<Poco>(originalEmpty);
            var jsonNull = TypeSerializer.SerializeToString<Poco>(originalNull);
            var fromJsv = TypeSerializer.DeserializeFromString<Poco>(jsv);
            var fromJsvEmpty = TypeSerializer.DeserializeFromString<Poco>(jsvEmpty);
            var fromJsvNull = TypeSerializer.DeserializeFromString<Poco>(jsonNull);

            Assert.That(fromJsv.Name, Is.EqualTo(original.Name));
            Assert.That(fromJsvEmpty.Name, Is.EqualTo(String.Empty));
            Assert.That(fromJsvNull.Name, Is.EqualTo(null));
        }
    }

    [TestFixture]
    public class StringParsingTests
    {
        [TestCase("test", "test")]
        [TestCase("", "\"\"")]
        [TestCase("asdf asdf asdf ", "asdf asdf asdf ")]
        [TestCase("test\t\ttest", "test\\t\\ttest")]
        [TestCase("\t\ttesttest", "\\t\\ttesttest")]
        [TestCase("testtest\t\t", "testtest\\t\\t")]
        [TestCase("test\tt\test", "test\\tt\\test")]
        [TestCase("\ttest\ttest", "\\ttest\\ttest")]
        [TestCase("test\ttest\t", "test\\ttest\\t")]
        [TestCase("\\", "\\")]
        [TestCase("test\t", "test\t")]
        [TestCase("test\ttest\\", "test\\ttest\\")]
        [TestCase("test\ttest", "test\\ttest")]
        [TestCase("\ttesttest", "\\ttesttest")]
        [TestCase("testtest\t", "testtest\\t")]
        [TestCase("the word is \ab", "the word is \\ab")]
        [TestCase("the word is \u1ab9", "the word is \\u1ab9")]
        [TestCase("the word is \x00ff", "the word is \\x00ff")]
        [TestCase("the word is \x00", "the word is \\x00")]
        [TestCase("the word is \\x0", "the word is \\x0")]
        [TestCase("test tab \t", "test tab \\t")]
        [TestCase("test return \r", "test return \\r")]
        [TestCase("test bell \b", "test bell \\b")]
        [TestCase("test quote \"", "test quote \\\"")]
        [TestCase("\"", "\\\"")]
        [TestCase("\"double quote\"", "\\\"double quote\\\"")]
        [TestCase("\"triple quote\"", "\"\\\"triple quote\\\"\"")]
        [TestCase("\"double triple quote\" and \"double triple quote\"",
                  "\"\\\"double triple quote\\\" and \\\"double triple quote\\\"\"")]
        public void AssertUnescapes(string expected, string given)
        {
            Assert.AreEqual(expected, JsonSerializer.DeserializeFromString<string>(given));
        }
    }

    public class BclJsonDataContractSerializer
    {
        public static BclJsonDataContractSerializer Instance = new BclJsonDataContractSerializer();

        public string Parse(object obj)
        {
            if (obj == null) return null;
            var type = obj.GetType();
            try
            {
                using (var ms = new MemoryStream())
                {
                    var serializer = new DataContractJsonSerializer(type);
                    serializer.WriteObject(ms, obj);
                    ms.Position = 0;
                    using (var sr = new StreamReader(ms))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException("BclJsonDataContractSerializer: Error converting type: " + ex.Message, ex);
            }
        }

        public void SerializeToStream<T>(T value, Stream stream)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            serializer.WriteObject(stream, value);
        }

    }

    public class BclJsonDataContractDeserializer
    {
        public static BclJsonDataContractDeserializer Instance = new BclJsonDataContractDeserializer();

        public object Parse(string json, Type returnType)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    var bytes = Encoding.UTF8.GetBytes(json);
                    ms.Write(bytes, 0, bytes.Length);
                    ms.Position = 0;
                    var serializer = new DataContractJsonSerializer(returnType);
                    return serializer.ReadObject(ms);
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException("BclJsonDataContractDeserializer: Error converting to type: " + ex.Message, ex);
            }
        }

        public To Parse<To>(string json)
        {
            return (To)Parse(json, typeof(To));
        }
    }

}