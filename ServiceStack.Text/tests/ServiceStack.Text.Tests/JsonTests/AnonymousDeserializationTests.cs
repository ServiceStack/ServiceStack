using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.JsonTests
{
    [TestFixture]
    public class AnonymousDeserializationTests
        : TestBase
    {
        private class Item
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }

            public static Item Create()
            {
                return new Item { IntValue = 42, StringValue = "Foo" };
            }
        }

        [Test]
        public void Can_deserialize_to_anonymous_type()
        {
            var original = Item.Create();
            var json = JsonSerializer.SerializeToString(original);

            var item = DeserializeAnonymousType(new { IntValue = default(int), StringValue = default(string) }, json);

            Assert.That(item.IntValue, Is.EqualTo(42));
            Assert.That(item.StringValue, Is.EqualTo("Foo"));
        }

        private static T DeserializeAnonymousType<T>(T template, string json)
        {
            TypeConfig<T>.EnableAnonymousFieldSetters = true;
            return (T)JsonSerializer.DeserializeFromString(json, template.GetType());
        }

        [Test]
        public void Deserialize_dynamic_json()
        {
            var json = "{\"Id\":\"fb1d17c7298c448cb7b91ab7041e9ff6\",\"Name\":\"John\",\"DateOfBirth\":\"\\/Date(317433600000-0000)\\/\"}";

            var obj = JsonObject.Parse(json);
            obj.Get<Guid>("Id").ToString().Print();
            obj.Get<string>("Name").Print();
            obj.Get<DateTime>("DateOfBirth").ToString("D").Print();

            dynamic dyn = DynamicJson.Deserialize(json);
            string id = dyn.Id;
            string name = dyn.Name;
            string dob = dyn.DateOfBirth;
            "DynamicJson: {0}, {1}, {2}".Print(id, name, dob);

            using (JsConfig.With(new Config { ConvertObjectTypesIntoStringDictionary = true }))
            {
                "Object Dictionary".Print();
                var map = (Dictionary<string, object>)json.FromJson<object>();
                map.PrintDump();
            }
        }

        [Test]
        public void Deserialize_dynamic_json_with_inner_obj_and_array()
        {
            var json = @"{""obj"":{""name"":""Alex"",""address"":{""street"":""zbra st.""},""phones"":[{""area"":""101"",""number"":""867-5309""},{""area"":""11"",""number"":""39967""}]}}";
            var dyn = DynamicJson.Deserialize(json);
            var name = dyn.obj.name;
            Assert.AreEqual(name, "Alex");
            var address = dyn.obj.address.street;
            Assert.AreEqual(address, "zbra st.");
            var phone1 = dyn.obj.phones[0].number;
            Assert.AreEqual(phone1, "867-5309");
            var area2 = dyn.obj.phones[1].area;
            Assert.AreEqual(area2, "11");
            var phone2 = dyn.obj.phones[1].number;
            Assert.AreEqual(phone2, "39967");
        }

        [Test]
        public void Deserialize_dynamic_json_with_keys_starting_with_object_literal()
        {
            using (JsConfig.With(new Config {ConvertObjectTypesIntoStringDictionary = true}))
            {
                var json = @"{""prop1"": ""value1"", ""prop2"": ""{tag} value2"", ""prop3"": { ""A"" : 1 } }";
                var obj = json.FromJson<Dictionary<string, object>>();
                Assert.That(obj["prop1"], Is.EqualTo("value1"));
                Assert.That(obj["prop2"], Is.EqualTo("{tag} value2"));
                Assert.That(obj["prop3"], Is.EqualTo(new Dictionary<string,object> { ["A"] = "1" }));
            }
        }
    }
}