using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class JsonObjectTests : TestBase
    {
        private const string JsonCentroid = @"{""place"":{ ""woeid"":12345, ""placeTypeName"":""St\\a\/te"" } }";

        [Test]
        public void Can_dynamically_parse_JSON_with_escape_chars()
        {
            var placeTypeName = JsonObject.Parse(JsonCentroid).Object("place").Get("placeTypeName");
            Assert.That(placeTypeName, Is.EqualTo("St\\a/te"));

            placeTypeName = JsonObject.Parse(JsonCentroid).Object("place").Get<string>("placeTypeName");
            Assert.That(placeTypeName, Is.EqualTo("St\\a/te"));
        }

        private const string JsonEscapedByteArray = @"{""universalId"":""09o4bFTeBq3hTKhoJVCkzSLRG\/o1SktTPqxgZ3L3Xss=""}";

        [Test]
        public void Can_dynamically_parse_JSON_with_escape_byte_array()
        {
            var parsed = JsonObject.Parse(JsonEscapedByteArray).Get<byte[]>("universalId");
            Assert.That(parsed, Is.EqualTo(new byte[] {
                0xd3, 0xda, 0x38, 0x6c, 0x54, 0xde, 0x06, 0xad,
                0xe1, 0x4c, 0xa8, 0x68, 0x25, 0x50, 0xa4, 0xcd,
                0x22, 0xd1, 0x1b, 0xfa, 0x35, 0x4a, 0x4b, 0x53,
                0x3e, 0xac, 0x60, 0x67, 0x72, 0xf7, 0x5e, 0xcb}));
        }

        [Test]
        public void Does_escape_string_access()
        {
            string test = "\"quoted string\"";
            var json = JsonSerializer.SerializeToString(new { a = test });
            var jsonObject = JsonObject.Parse(json);

            var actual = jsonObject["a"];
            Assert.That(actual, Is.EqualTo(test));
            Assert.That(jsonObject.Get("a"), Is.EqualTo(test));
            Assert.That(jsonObject.Get<string>("a"), Is.EqualTo(test));

            Assert.That(jsonObject.GetUnescaped("a"), Is.EqualTo(test.Replace("\"", "\\\"")));
        }

        [Test]
        public void Does_encode_unicode()
        {
            string test = "<\"I get this : 􏰁􏰂􏰃􏰄􏰂􏰅􏰆􏰇􏰈􏰀􏰉􏰊􏰇􏰋􏰆􏰌􏰀􏰆􏰊􏰀􏰍􏰄􏰎􏰆􏰏􏰐􏰑􏰑􏰆􏰒􏰆􏰂􏰊􏰀";
            var obj = new { test };
            using (var mem = new System.IO.MemoryStream())
            {
                ServiceStack.Text.JsonSerializer.SerializeToStream(obj, obj.GetType(), mem);

                var encoded = System.Text.Encoding.UTF8.GetString(mem.ToArray());

                var copy1 = JsonObject.Parse(encoded);

                Assert.That(test, Is.EqualTo(copy1["test"]));

                System.Diagnostics.Debug.WriteLine(copy1["test"]);
            }
        }

        [Test]
        public void Does_encode_large_strings()
        {
            char[] testChars = new char[32769];
            for (int i = 0; i < testChars.Length; i++)
                testChars[i] = (char)i;

            string test = new string(testChars);

            var obj = new { test };
            using (var mem = new System.IO.MemoryStream())
            {
                ServiceStack.Text.JsonSerializer.SerializeToStream(obj, obj.GetType(), mem);

                var encoded = System.Text.Encoding.UTF8.GetString(mem.ToArray());

                var copy1 = JsonObject.Parse(encoded);

                Assert.That(test, Is.EqualTo(copy1["test"]));

                System.Diagnostics.Debug.WriteLine(copy1["test"]);
            }
        }
        
        public interface IFoo
        {
            string Property { get; }
        }

        public class FooA : IFoo
        {
            public string Property { get; set; } = "A";
        }

        [Test]
        public void Can_consistently_serialize_stream()
        {
            var item = new FooA();
            var result1 = SerializeToStream1(item);
            var result2 = SerializeToStream2(item);
            var result3 = SerializeToStream1(item);
            var result4 = SerializeToStream2(item);

            Assert.That(result1, Is.EqualTo(result2));
            Assert.That(result3, Is.EqualTo(result4));
        }
        
        // Serialize using TypeSerializer.SerializeToStream<T>(T, Stream)
        public static string SerializeToStream1(IFoo item)
        {
            using var stream = new MemoryStream();
            TypeSerializer.SerializeToStream(item, stream);
            return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
        }

        // Serialize using TypeSerializer.SerializeToStream(object, Type, Stream)
        public static string SerializeToStream2(IFoo item)
        {
            using var stream = new MemoryStream();
            TypeSerializer.SerializeToStream(item, item.GetType(), stream);
            return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
        }

        [Test]
        public void Can_parse_Twitter_response()
        {
            var json = @"[{""is_translator"":false,""geo_enabled"":false,""profile_background_color"":""000000"",""protected"":false,""default_profile"":false,""profile_background_tile"":false,""created_at"":""Sun Nov 23 17:42:51 +0000 2008"",""name"":""Demis Bellot TW"",""profile_background_image_url_https"":""https:\/\/si0.twimg.com\/profile_background_images\/192991651\/twitter-bg.jpg"",""profile_sidebar_fill_color"":""2A372F"",""listed_count"":36,""notifications"":null,""utc_offset"":0,""friends_count"":267,""description"":""StackExchangarista, JavaScript, C#, Web & Mobile developer. Creator of the ServiceStack.NET projects. "",""following"":null,""verified"":false,""profile_sidebar_border_color"":""D9D082"",""followers_count"":796,""profile_image_url"":""http:\/\/a2.twimg.com\/profile_images\/1598852740\/avatar_normal.png"",""contributors_enabled"":false,""profile_image_url_https"":""https:\/\/si0.twimg.com\/profile_images\/1598852740\/avatar_normal.png"",""status"":{""possibly_sensitive"":false,""place"":null,""retweet_count"":37,""in_reply_to_screen_name"":null,""created_at"":""Mon Nov 07 02:34:23 +0000 2011"",""retweeted"":false,""in_reply_to_status_id_str"":null,""in_reply_to_user_id_str"":null,""contributors"":null,""id_str"":""133371690876022785"",""retweeted_status"":{""possibly_sensitive"":false,""place"":null,""retweet_count"":37,""in_reply_to_screen_name"":null,""created_at"":""Mon Nov 07 02:32:15 +0000 2011"",""retweeted"":false,""in_reply_to_status_id_str"":null,""in_reply_to_user_id_str"":null,""contributors"":null,""id_str"":""133371151551447041"",""in_reply_to_user_id"":null,""in_reply_to_status_id"":null,""source"":""\u003Ca href=\""http:\/\/www.arstechnica.com\"" rel=\""nofollow\""\u003EArs auto-tweeter\u003C\/a\u003E"",""geo"":null,""favorited"":false,""id"":133371151551447041,""coordinates"":null,""truncated"":false,""text"":""Google: Microsoft uses patents when products \""stop succeeding\"": http:\/\/t.co\/50QFc1uJ by @binarybits""},""in_reply_to_user_id"":null,""in_reply_to_status_id"":null,""source"":""web"",""geo"":null,""favorited"":false,""id"":133371690876022785,""coordinates"":null,""truncated"":false,""text"":""RT @arstechnica: Google: Microsoft uses patents when products \""stop succeeding\"": http:\/\/t.co\/50QFc1uJ by @binarybits""},""profile_use_background_image"":true,""favourites_count"":238,""location"":""New York"",""id_str"":""17575623"",""default_profile_image"":false,""show_all_inline_media"":false,""profile_text_color"":""ABB8AF"",""screen_name"":""demisbellot"",""statuses_count"":9638,""profile_background_image_url"":""http:\/\/a0.twimg.com\/profile_background_images\/192991651\/twitter-bg.jpg"",""url"":""http:\/\/www.servicestack.net\/mythz_blog\/"",""time_zone"":""London"",""profile_link_color"":""43594A"",""id"":17575623,""follow_request_sent"":null,""lang"":""en""}]";
            var objs = JsonObject.ParseArray(json);
            var obj = objs[0];

            Assert.That(obj.Get("name"), Is.EqualTo("Demis Bellot TW"));
        }

        [Test]
        public void Can_parse_ArrayObjects()
        {
            var data = new { key = new[] { "value1", "value2" } };
            var json = data.ToJson();

            Assert.That(json, Is.EqualTo(@"{""key"":[""value1"",""value2""]}"));

            var value = JsonObject.Parse(json);
            var dataObjects = value.Get<string[]>("key");

            Assert.That(dataObjects[0], Is.EqualTo("value1"));
            Assert.That(dataObjects[1], Is.EqualTo("value2"));
        }

        [Test]
        public void Can_deserialize_JsonArray()
        {
            var json = @"
                {
                    ""projects"":[
                        {
                            ""name"": ""Project1""
                        },
                        {
                            ""name"": ""Project2""
                        },
                        {
                            ""name"": ""Project3""
                        }
                    ]
                }";

            var projects = JsonObject.Parse(json).ArrayObjects("projects");

            var proj3Name = projects[2].Get("name");

            Assert.That(proj3Name, Is.EqualTo("Project3"));
        }

        [Test]
        public void Can_deserialize_JSON_Object()
        {
            var json = "{\"http://SomeUrl.com/\":{\"http://otherUrl.org/schema#name\":[{\"value\":\"val1\",\"type\":\"val2\"}]}}";

            var obj = JsonObject.Parse(json)
                .Object("http://SomeUrl.com/");

            var items = obj.ArrayObjects("http://otherUrl.org/schema#name")[0];

            Assert.That(items["value"], Is.EqualTo("val1"));
            Assert.That(items["type"], Is.EqualTo("val2"));
        }

        public class Customer
        {
            public static List<object> Setters = new List<object>();

            private string name;
            private int age;
            private string address;

            public string Name
            {
                get { return name; }
                set { name = value; Setters.Add(value); }
            }

            public int Age
            {
                get { return age; }
                set { age = value; Setters.Add(value); }
            }

            public string Address
            {
                get { return address; }
                set { address = value; Setters.Add(value); }
            }
        }

        [Test]
        public void Only_sets_Setters_with_JSON()
        {
            var dto = "{\"Name\":\"Foo\"}".FromJson<Customer>();

            Assert.That(dto.Name, Is.EqualTo("Foo"));
            Assert.That(Customer.Setters.Count, Is.EqualTo(1));
            Assert.That(Customer.Setters[0], Is.EqualTo(dto.Name));
        }

        public class TypeObject
        {
            public string Prop1 { get; set; }
            public int Prop2 { get; set; }
            public bool Prop3 { get; set; }
            public double Prop4 { get; set; }
            public string[] Prop5 { get; set; }
            public Dictionary<string, string> Prop6 { get; set; }
        }

        [Test]
        public void Can_parse_dynamic_json()
        {
            var json = @"{
              ""prop1"": ""text string"",
              ""prop2"": 33,
              ""prop3"": true,
              ""prop4"": 6.3,
              ""prop5"": [ ""A"", ""B"", ""C"" ],
              ""prop6"": { ""A"" : ""a"" }
            }";

            var typeObj = json.FromJson<TypeObject>();

            Assert.That(typeObj.Prop1, Is.EqualTo("text string"));
            Assert.That(typeObj.Prop2, Is.EqualTo(33));
            Assert.That(typeObj.Prop3, Is.EqualTo(true));
            Assert.That(typeObj.Prop4, Is.EqualTo(6.3d));
            Assert.That(typeObj.Prop5, Is.EquivalentTo(new[] { "A", "B", "C" }));
            Assert.That(typeObj.Prop6, Is.EquivalentTo(new Dictionary<string, string> { { "A", "a" } }));

            var obj = JsonObject.Parse(json);

            var o = new TypeObject
            {
                Prop1 = obj["prop1"],
                Prop2 = obj.Get<int>("prop2"),
                Prop3 = obj.Get<bool>("prop3"),
                Prop4 = obj.Get<double>("prop4"),
                Prop5 = obj.Get<string[]>("prop5"),
                Prop6 = obj.Object("prop6"),
            };

            Assert.That(o.Prop1, Is.EqualTo("text string"));
            Assert.That(o.Prop2, Is.EqualTo(33));
            Assert.That(o.Prop3, Is.EqualTo(true));
            Assert.That(o.Prop4, Is.EqualTo(6.3d));
            Assert.That(o.Prop5, Is.EquivalentTo(new[] { "A", "B", "C" }));
            Assert.That(o.Prop6, Is.EquivalentTo(new Dictionary<string, string> { { "A", "a" } }));
        }

        [Test]
        public void Can_deserialize_array_string_in_Map()
        {
            var json = "{\"name\":\"foo\",\"roles\":[\"Role1\",\"Role 2\"]}";
            var obj = JsonObject.Parse(json);
            Assert.That(obj.GetArray<string>("roles"), Is.EqualTo(new[] { "Role1", "Role 2" }));

            var map = json.FromJson<Dictionary<string, string>>();
            Assert.That(map.GetArray<string>("roles"), Is.EqualTo(new[] { "Role1", "Role 2" }));
        }

        [Test]
        public void Can_deserialize_array_numbers_in_Map()
        {
            var json = "{\"name\":\"foo\",\"roles\":[1,2]}";
            var obj = JsonObject.Parse(json);
            Assert.That(obj.GetArray<int>("roles"), Is.EqualTo(new[] { 1, 2 }));

            var map = json.FromJson<Dictionary<string, string>>();
            Assert.That(map.GetArray<int>("roles"), Is.EqualTo(new[] { 1, 2 }));
        }

        public class TestJArray
        {
            public int Id { get; set; }
            public string Name { get; set; }

            protected bool Equals(TestJArray other)
            {
                return Id == other.Id && string.Equals(Name, other.Name);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((TestJArray) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Id*397) ^ (Name != null ? Name.GetHashCode() : 0);
                }
            }
        }

        [Test]
        public void Can_deserialize_array_objects_in_Map()
        {
            var json = "{\"name\":\"foo\",\"roles\":[{\"Id\":1,\"Name\":\"Role1\"},{\"Id\":2,\"Name\":\"Role 2\"}]}";
            var obj = JsonObject.Parse(json);
            Assert.That(obj.GetArray<TestJArray>("roles"), Is.EqualTo(new[]
            {
                new TestJArray { Id = 1, Name = "Role1" },
                new TestJArray { Id = 2, Name = "Role 2" },
            }));

            var map = json.FromJson<Dictionary<string, string>>();
            Assert.That(map.GetArray<TestJArray>("roles"), Is.EqualTo(new[]
            {
                new TestJArray { Id = 1, Name = "Role1" },
                new TestJArray { Id = 2, Name = "Role 2" },
            }));
        }

        [Test]
        public void Can_deserialice_string_list()
        {
            var obj = new JsonObject {
                ["null"] = null,
                ["item"] = "foo",
                ["list"] = new List<string> { "foo", "bar", "qux" }.ToJson()
            };

            var nullList = obj["null"].FromJson<List<string>>();
            var itemList = obj["item"].FromJson<List<string>>();
            var listList = obj["list"].FromJson<List<string>>();
            
            Assert.That(nullList, Is.Null);
            Assert.That(itemList, Is.EquivalentTo(new[]{ "foo" }));
            Assert.That(listList, Is.EquivalentTo(new[]{ "foo", "bar", "qux" }));
        }

        [Test]
        public void Can_deserialize_Inherited_JSON_Object()
        {
            var jsonValue = "{\"test\":[\"Test1\",\"Test Two\"]}";

            var jsonObject = JsonSerializer.DeserializeFromString<JsonObject>(jsonValue);
            var inheritedJsonObject = JsonSerializer.DeserializeFromString<InheritedJsonObject>(jsonValue);

            string testString = jsonObject.Child("test");
            string inheritedTestString = inheritedJsonObject.Child("test");

            Assert.AreEqual(testString, inheritedTestString);

            var serializedJsonObject = JsonSerializer.SerializeToString<JsonObject>(jsonObject);
            var serializedInheritedJsonObject = JsonSerializer.SerializeToString<InheritedJsonObject>(inheritedJsonObject);

            Assert.AreEqual(serializedJsonObject, serializedInheritedJsonObject);
        }
        
        public class InheritedJsonObject : JsonObject { }

        [Test]
        public void Does_escape_string_values()
        {
            var json = JsonObject.Parse("{\"text\":\"line\nbreak\"}");
            Assert.That(json["text"], Is.EqualTo("line\nbreak"));
            
            json = JsonObject.Parse("{\"a\":{\"text\":\"line\nbreak\"}}");
            var a = json.Object("a");
            Assert.That(a["text"], Is.EqualTo("line\nbreak"));
        }
        
        public class JsonObjectWrapper
        {
            public JsonObject Prop { get; set; }
        }

        [Test]
        public void Does_escape_strings_in_JsonObject_DTO()
        {
            var dto = "{\"Prop\":{\"text\":\"line\nbreak\"}}".FromJson<JsonObjectWrapper>();
            Assert.That(dto.Prop["text"], Is.EqualTo("line\nbreak"));
            
            Assert.That(dto.ToJson(), Is.EqualTo("{\"Prop\":{\"text\":\"line\\nbreak\"}}"));

            dto = "{\"Prop\":{\"a\":{\"text\":\"line\nbreak\"}}}".FromJson<JsonObjectWrapper>();
            var a = dto.Prop.Object("a");
            Assert.That(a["text"], Is.EqualTo("line\nbreak"));
            
            //
            //Assert.That(dto.ToJson(), Is.EqualTo("{\"Prop\":{\"a\":{\"text\":\"line\\nbreak\"}}}"));
        }

        public class StringDictionaryWrapper
        {
            public Dictionary<string,string> Prop { get; set; }
        }

        public class NestedStringDictionaryWrapper
        {
            public Dictionary<string,Dictionary<string,string>> Prop { get; set; }
        }

        [Test]
        public void Does_serialize_StringDictionaryWrapper_line_breaks()
        {
            var prop = new Dictionary<string,string> {
                ["text"] = "line\nbreak"
            };
            
            Assert.That(prop.ToJson(), Is.EqualTo("{\"text\":\"line\\nbreak\"}"));
            
            var dto = new StringDictionaryWrapper { Prop = prop };
            Assert.That(dto.ToJson(), Is.EqualTo("{\"Prop\":{\"text\":\"line\\nbreak\"}}"));
            
            var nested = new NestedStringDictionaryWrapper { Prop = new Dictionary<string, Dictionary<string, string>> {
                    ["a"] = prop
                } 
            };
            Assert.That(nested.ToJson(), Is.EqualTo("{\"Prop\":{\"a\":{\"text\":\"line\\nbreak\"}}}"));
        }
        
        public class ObjectDictionaryWrapper
        {
            public object Prop { get; set; }
        }
        
        [Test]
        public void Does_serialize_ObjectDictionaryWrapper_line_breaks()
        {
            var prop = new Dictionary<string,string> {
                ["text"] = "line\nbreak"
            };

            var dto = new ObjectDictionaryWrapper { Prop = prop };
            Assert.That(dto.ToJson(), Is.EqualTo("{\"Prop\":{\"text\":\"line\\nbreak\"}}"));

            var nested = new ObjectDictionaryWrapper { Prop = new Dictionary<string, Dictionary<string, string>> {
                    ["a"] = prop
                } 
            };
            Assert.That(nested.ToJson(), Is.EqualTo("{\"Prop\":{\"a\":{\"text\":\"line\\nbreak\"}}}"));
        }

        [Test]
        public void Enumerating_JsonObject_returns_same_unescaped_value_as_indexer()
        {
            var obj = JsonObject.Parse(@"{""a"":""b\\c""}");
            Assert.That(obj["a"], Is.EqualTo("b\\c"));

            foreach (var entry in obj)
            {
                if (entry.Key == "a")
                {
                    Assert.That(entry.Value, Is.EqualTo("b\\c"));
                }
            }
            
            var asEnumerable = (IEnumerable<KeyValuePair<string, string>>)obj;
            foreach (var entry in asEnumerable)
            {
                if (entry.Key == "a")
                {
                    Assert.That(entry.Value, Is.EqualTo("b\\c"));
                }
            }
            
            var asIDict = (IDictionary<string, string>)obj;
            foreach (var entry in asIDict)
            {
                if (entry.Key == "a")
                {
                    Assert.That(entry.Value, Is.EqualTo("b\\c"));
                }
            }

            // Warning: can't override concrete Dictionary<string, string> enumerator
            // var asDict = (Dictionary<string, string>)obj;
            // foreach (var entry in asDict)
            // {
            //     if (entry.Key == "a")
            //     {
            //         Assert.That(entry.Value, Is.EqualTo("b\\c"));
            //     }
            // }
        }

    }
}