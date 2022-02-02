using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.JsonTests
{
    [TestFixture]
    public class JsonArrayObjectTests
    {

        [Test]
        public void Can_serialize_int_array()
        {
            var array = new[] { 1, 2 };
            Assert.That(JsonSerializer.SerializeToString(array), Is.EqualTo("[1,2]"));
        }

        [Test]
        public void Can_parse_empty_array()
        {
            Assert.That(JsonArrayObjects.Parse("[]"), Is.Empty);
        }

        [Test]
        public void Can_parse_empty_array_with_tab()
        {
            Assert.That(JsonArrayObjects.Parse("[\t]"), Is.Empty);
        }

        [Test]
        public void Can_parse_array_with_null()
        {
            Assert.That(JsonArrayObjects.Parse("[null]"), Is.EqualTo(new string[] { null }));
        }

        [Test]
        public void Can_parse_array_with_nulls()
        {
            Assert.That(JsonArrayObjects.Parse("[null,null]"), Is.EqualTo(new string[] { null, null }));
        }

        [Test]
        public void Can_parse_empty_array_with_whitespaces()
        {
            Assert.That(JsonArrayObjects.Parse("[    ]"), Is.Empty);
            Assert.That(JsonArrayObjects.Parse("[\n\n]"), Is.Empty);
            Assert.That(JsonArrayObjects.Parse("[\t\t]"), Is.Empty);
        }

        [Test]
        public void Can_parse_empty_array_with_mixed_whitespaces()
        {
            Assert.That(JsonArrayObjects.Parse("[ \n\t  \n\r]"), Is.Empty);
        }

        public class NamesTest
        {
            public NamesTest(List<string> names)
            {
                Names = names;
            }

            public List<string> Names { get; set; }
        }

        [Test]
        public void Can_parse_empty_array_in_dto_with_tab()
        {
            var prettyJson = "{\"Names\":[\t]}";
            var oPretty = prettyJson.FromJson<NamesTest>();
            Assert.That(oPretty.Names.Count, Is.EqualTo(0));
        }

        public class CustomAuth
        {
            public string City { get; set; }
            public int Country { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string ScreenName { get; set; }
            public long Uid { get; set; }
        }

        public class CustomAuthResponse
        {
            public List<CustomAuth> Response { get; set; }
        }

        [Test]
        public void Can_parse_custom_AuthResponse()
        {
            var json = @"{ 
  ""response"" : [ 
    { ""city"" : 56,
    ""country"" : 1,
    ""first_name"" : ""Rouslan"",
    ""last_name"" : ""Grabar"",
    ""screen_name"" : ""iamruss2"",
    ""uid"" : 180423804
    }
] 
}";

            var dto = JsonObject.Parse(json)
                .ArrayObjects("response")[0].ConvertTo(x =>
                    new CustomAuth
                    {
                        City = x["city"],
                        Country = x.Get<int>("country"),
                        FirstName = x["first_name"],
                        LastName = x["last_name"],
                        ScreenName = x["screen_name"],
                        Uid = x.Get<long>("uid"),
                    });

            dto.PrintDump();

            using (JsConfig.With(new Config {
                TextCase = TextCase.SnakeCase,
                PropertyConvention = PropertyConvention.Lenient
            }))
            {
                var response = json.FromJson<CustomAuthResponse>();
                response.PrintDump();
            }
        }

        class Test
        {
            public string Value { get; set; }
        }

        [Test]
        public void Can_deserialize_empty_array_with_whitespace()
        {
            const string data = "[]\n";
            var result = data.FromJson<Test[]>();
            Assert.That(result.Length, Is.EqualTo(0));
        }

        public class MyClass
        {
            public string Item { get; set; }
        }

        [Test]
        public void Can_parse_array_with_null_objects_starting_with_not_null_item()
        {
            var compactJson = @"{""items"":[{""Item"":""myitem""},null]}";
            var json = JsonObject.Parse(compactJson);
            var items = json.ArrayObjects("items");
            Assert.NotNull(items[0]);
            Assert.Null(items[1]);
        }

        [Test]
        public void Can_parse_array_with_null_objects_starting_with_null_item()
        {
            var compactJson = @"{""items"":[null,{""Item"":""myitem""}]}";
            var json = JsonObject.Parse(compactJson);
            var items = json.ArrayObjects("items");
            Assert.Null(items[0]);
            Assert.NotNull(items[1]);
        }

        [Test]
        public void Can_parse_array_with_whitespaces()
        {
            var json = "[{}, {}]";
            var arrayObjs = JsonArrayObjects.Parse(json);
            Assert.That(arrayObjs.Count, Is.EqualTo(2));

            json = "  [{}, {}]";
            arrayObjs = JsonArrayObjects.Parse(json);
            Assert.That(arrayObjs.Count, Is.EqualTo(2));

            json = "  [  {  }  ,  {  }  ]  ";
            arrayObjs = JsonArrayObjects.Parse(json);
            Assert.That(arrayObjs.Count, Is.EqualTo(2));
        }
    }
}
