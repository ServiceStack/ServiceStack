using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Text.Tests.JsonTests
{
    [TestFixture]
    public class EscapedCharsTests
        : TestBase
    {
        public class NestedModel
        {
            public string Id { get; set; }

            public ModelWithIdAndName Model { get; set; }
        }

        [TearDown]
        public void TearDown()
        {
            JsConfig.Reset();
        }

        [Test]
        public void Can_deserialize_text_with_escaped_chars()
        {
            var model = new ModelWithIdAndName
            {
                Id = 1,
                Name = @"1 \ 2 \r 3 \n 4 \b 5 \f 6 """
            };

            SerializeAndCompare(model);
        }

        [Test]
        public void Can_short_circuit_string_with_no_escape_chars()
        {
            var model = new ModelWithIdAndName
            {
                Id = 1,
                Name = @"Simple string"
            };

            SerializeAndCompare(model);
        }

        [Test]
        public void Can_deserialize_json_with_whitespace()
        {
            var model = new ModelWithIdAndName
            {
                Id = 1,
                Name = @"Simple string"
            };

            const string json = "\t { \t \"Id\" \t : 1 , \t \"Name\" \t  : \t \"Simple string\" \t } \t ";

            var fromJson = JsonSerializer.DeserializeFromString<ModelWithIdAndName>(json);

            Assert.That(fromJson, Is.EqualTo(model));
        }

        public class Inner
        {
            public int Int { get; set; }
        }

        public class Program
        {
            public Inner[] Inner { get; set; }
        }

        [Test]
        public void Can_deserialize_inner_whitespace()
        {
            var fromJson = JsonSerializer.DeserializeFromString<Program>("{\"Inner\":[{\"Int\":0} , {\"Int\":1}\r\n]}");
            Assert.That(fromJson.Inner.Length, Is.EqualTo(2));
            Assert.That(fromJson.Inner[0].Int, Is.EqualTo(0));
            Assert.That(fromJson.Inner[1].Int, Is.EqualTo(1));

            var dto = new Program { Inner = new[] { new Inner { Int = 0 } } };
            Serialize(dto);
            var json = JsonSerializer.SerializeToString(dto);
            Assert.That(json, Is.EqualTo(@"{""Inner"":[{""Int"":0}]}"));
        }

        [Test]
        public void Can_deserialize_nested_json_with_whitespace()
        {
            var model = new NestedModel
            {
                Id = "Nested with space",
                Model = new ModelWithIdAndName
                {
                    Id = 1,
                    Name = @"Simple string"
                }
            };

            const string json = "\t { \"Id\" : \"Nested with space\" \n , \r \t \"Model\" \t : \n { \t \"Id\" \t : 1 , \t \"Name\" \t  : \t \"Simple string\" \t } \t } \n ";

            var fromJson = JsonSerializer.DeserializeFromString<NestedModel>(json);

            Assert.That(fromJson.Id, Is.EqualTo(model.Id));
            Assert.That(fromJson.Model, Is.EqualTo(model.Model));
        }


        public class ModelWithList
        {
            public ModelWithList()
            {
                this.StringList = new List<string>();
            }

            public int Id { get; set; }
            public List<string> StringList { get; set; }
            public string[] StringArray { get; set; }

            public bool Equals(ModelWithList other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return other.Id == Id && StringList.EquivalentTo(other.StringList);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != typeof(ModelWithList)) return false;
                return Equals((ModelWithList)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Id * 397) ^ (StringList != null ? StringList.GetHashCode() : 0);
                }
            }
        }

        [Test]
        public void Can_serialize_Model_with_array()
        {
            var model = new ModelWithList
            {
                Id = 1,
                StringArray = new[] { "One", "Two", "Three" }
            };

            SerializeAndCompare(model);
        }

        [Test]
        public void Can_serialize_Model_with_list()
        {
            var model = new ModelWithList
            {
                Id = 1,
                StringList = { "One", "Two", "Three" }
            };

            SerializeAndCompare(model);
        }

        [Test]
        public void Can_serialize_Model_with_array_of_escape_chars()
        {
            var model = new ModelWithList
            {
                Id = 1,
                StringArray = new[] { @"1 \ 2 \r 3 \n 4 \b 5 \f 6 """, @"1 \ 2 \r 3 \n 4 \b 5 \f 6 """ }
            };

            SerializeAndCompare(model);
        }

        [Test]
        public void Can_serialize_Model_with_list_of_escape_chars()
        {
            var model = new ModelWithList
            {
                Id = 1,
                StringList = { @"1 \ 2 \r 3 \n 4 \b 5 \f 6 """, @"1 \ 2 \r 3 \n 4 \b 5 \f 6 """ }
            };

            SerializeAndCompare(model);
        }

        [Test]
        public void Can_deserialize_json_list_with_whitespace()
        {
            var model = new ModelWithList
            {
                Id = 1,
                StringList = { " One ", " Two " }
            };

            Log(JsonSerializer.SerializeToString(model));

            const string json = "\t { \"Id\" : 1 , \n \"StringList\" \t : \n [ \t \" One \" \t , \t \" Two \" \t ] \n } \t ";

            var fromJson = JsonSerializer.DeserializeFromString<ModelWithList>(json);

            Assert.That(fromJson, Is.EqualTo(model));
        }

        [Test]
        public void Can_deserialize_basic_latin_unicode()
        {
            const string json = "{\"Id\":1,\"Name\":\"\\u0041 \\u0042 \\u0043 | \\u0031 \\u0032 \\u0033\"}";

            var model = new ModelWithIdAndName { Id = 1, Name = "A B C | 1 2 3" };

            var fromJson = JsonSerializer.DeserializeFromString<ModelWithIdAndName>(json);

            Assert.That(fromJson, Is.EqualTo(model));
        }

        [Test]
        public void Can_serialize_unicode_without_escape()
        {
            var model = new MyModel { Name = "JříАбвĀašū" };
            var toJson = JsonSerializer.SerializeToString(model);
            Assert.That(toJson, Is.EqualTo("{\"Name\":\"JříАбвĀašū\"}"));
        }

        [Test]
        public void Can_deserialize_unicode_without_escape()
        {
            var fromJson = JsonSerializer.DeserializeFromString<MyModel>("{\"Name\":\"JříАбвĀašū\"}");
            Assert.That(fromJson.Name, Is.EqualTo("JříАбвĀašū"));
        }

        [Test]
        public void Can_serialize_unicode_with_escape()
        {
            JsConfig.EscapeUnicode = true;
            var model = new MyModel { Name = "JříАбвĀašū" };
            var toJson = JsonSerializer.SerializeToString(model);
            Assert.That(toJson, Is.EqualTo("{\"Name\":\"J\\u0159\\u00ED\\u0410\\u0431\\u0432\\u0100a\\u0161\\u016B\"}"));
        }

        [Test]
        public void Can_deserialize_unicode_with_escape()
        {
            JsConfig.EscapeUnicode = true;
            var fromJson = JsonSerializer.DeserializeFromString<MyModel>("{\"Name\":\"J\\u0159\\u00ED\\u0410\\u0431\\u0432\\u0100a\\u0161\\u016B\"}");
            Assert.That(fromJson.Name, Is.EqualTo("JříАбвĀašū"));
        }

        [Test]
        public void Can_serialize_array_of_control_chars_and_unicode()
        {
            // we want to ensure control chars are escaped, but other unicode is fine to be serialized
            Assert.IsFalse(JsConfig.EscapeUnicode, "for this test, JsConfig.EscapeUnicode must be false");

            var array = new[] { ((char)0x18).ToString(), "Ω" };
            var json = JsonSerializer.SerializeToString(array);
            Assert.That(json, Is.EqualTo(@"[""\u0018"",""Ω""]"));
        }


        [Test]
        public void Can_serialize_windows_new_line()
        {
            const string expected = "\"Hi I\'m\\r\\nnew line :)\"";
            var text = "Hi I\'m\r\nnew line :)";

            var result = JsonSerializer.SerializeToString(text);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Can_serialize_object_with_escaped_chars()
        {
            const string expected = "{\"Id\":1,\"Name\":\"Hi I'm\\r\\nnew line :)\"}";
            var model = new ModelWithIdAndName
            {
                Id = 1,
                Name = "Hi I'm\r\nnew line :)"
            };

            var result = JsonSerializer.SerializeToString(model);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Can_deserialize_with_new_line()
        {
            var model = new ModelWithIdAndName
            {
                Id = 1,
                Name = "Hi I'm\r\nnew line :)"
            };

            const string json = "{\"Id\":1,\"Name\":\"Hi I'm\\r\\nnew line :)\"}";

            var fromJson = JsonSerializer.DeserializeFromString<ModelWithIdAndName>(json);

            Assert.That(fromJson, Is.EqualTo(model));
        }

        public class MyModel
        {
            public string Name { get; set; }
        }

        [Test]
        public void Can_serialize_string_with_new_line()
        {
            Assert.That("Line1\nLine2".ToJson(), Is.EqualTo("\"Line1\\nLine2\""));
            Assert.That(new MyModel { Name = "Line1\nLine2" }.ToJson(),
                Is.EqualTo("{\"Name\":\"Line1\\nLine2\"}"));
        }
    }
}