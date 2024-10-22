using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using NUnit.Framework;
#if NETFRAMEWORK
using ServiceStack.Host;
using ServiceStack.Testing;
#endif

namespace ServiceStack.Text.Tests
{
    public class C
    {
        public int? A { get; set; }
        public int? B { get; set; }
    }

    [TestFixture]
    public class QueryStringSerializerTests : TestBase
    {
        class D
        {
            public string A { get; set; }
            public string B { get; set; }
        }

        [Test]
        public void Can_serialize_query_string()
        {
            Assert.That(QueryStringSerializer.SerializeToString(new C { A = 1, B = 2 }),
                Is.EqualTo("A=1&B=2"));

            Assert.That(QueryStringSerializer.SerializeToString(new C { A = null, B = 2 }),
                Is.EqualTo("B=2"));
        }

        [Test]
        public void Can_Serialize_Unicode_Query_String()
        {
            Assert.That(QueryStringSerializer.SerializeToString(new D { A = "믬㼼摄䰸蠧蛛㙷뇰믓堐锗멮ᙒ덃", B = "八敁喖䉬ڵẀ똦⌀羭䥀主䧒蚭㾐타" }),
                Is.EqualTo("A=%eb%af%ac%e3%bc%bc%e6%91%84%e4%b0%b8%e8%a0%a7%e8%9b%9b%e3%99%b7%eb%87%b0%eb%af%93%e5%a0" +
                "%90%e9%94%97%eb%a9%ae%e1%99%92%eb%8d%83&B=%e5%85%ab%e6%95%81%e5%96%96%e4%89%ac%da%b5%e1%ba%80%eb%98%a6%e2%8c%80%e7%be%ad%e4" +
                "%a5%80%e4%b8%bb%e4%a7%92%e8%9a%ad%e3%be%90%ed%83%80"));

            Assert.That(QueryStringSerializer.SerializeToString(new D { A = "崑⨹堡ꁀᢖ㤹ì㭡줪銬", B = null }),
                Is.EqualTo("A=%e5%b4%91%e2%a8%b9%e5%a0%a1%ea%81%80%e1%a2%96%e3%a4%b9%c3%ac%e3%ad%a1%ec%a4%aa%e9%8a%ac"));
        }

        [Test]
        public void Does_serialize_Poco_and_string_dictionary_with_encoded_data()
        {
            var msg = "Field with comma, to demo. ";
            Assert.That(QueryStringSerializer.SerializeToString(new D { A = msg }),
                Is.EqualTo("A=Field+with+comma,+to+demo.+"));

            Assert.That(QueryStringSerializer.SerializeToString(new D { A = msg }.ToStringDictionary()),
                Is.EqualTo("A=Field+with+comma,+to+demo.+"));
        }

        class Empty { }

        [Test]
        public void Can_serialize_empty_object()
        {
            Assert.That(QueryStringSerializer.SerializeToString(new Empty()), Is.Empty);
        }

        [Test]
        public void Can_serialize_newline()
        {
            Assert.That(QueryStringSerializer.SerializeToString(new { newline = "\r\n" }), Is.EqualTo("newline=%0d%0a"));
        }

        [Test]
        public void Can_serialize_array_of_strings_with_colon()
        {
            var t = new List<string>();
            t.Add("Foo:Bar");
            t.Add("Get:Out");
            Assert.That(QueryStringSerializer.SerializeToString(new { list = t }), Is.EqualTo("list=Foo%3aBar,Get%3aOut"));
        }

        [Test]
        public void Can_serialize_tab()
        {
            Assert.That(QueryStringSerializer.SerializeToString(new { tab = "\t" }), Is.EqualTo("tab=%09"));
        }

        // NOTE: QueryStringSerializer doesn't have Deserialize, but this is how QS is parsed in ServiceStack
        [Test]
        public void Can_deserialize_query_string_nullableInt_null_yields_null()
        {
            Assert.That(ServiceStack.Text.Common.DeserializeBuiltin<int?>.Parse(null), Is.EqualTo(null));
        }

        [Test]
        public void Can_deserialize_query_string_nullableInt_empty_yields_null()
        {
            Assert.That(ServiceStack.Text.Common.DeserializeBuiltin<int?>.Parse(string.Empty), Is.EqualTo(null));
        }

        [Test]
        public void Can_deserialize_query_string_nullableInt_intValues_yields_null()
        {
            Assert.That(ServiceStack.Text.Common.DeserializeBuiltin<int?>.Parse(int.MaxValue.ToString()), Is.EqualTo(int.MaxValue));
            Assert.That(ServiceStack.Text.Common.DeserializeBuiltin<int?>.Parse(int.MinValue.ToString()), Is.EqualTo(int.MinValue));
            Assert.That(ServiceStack.Text.Common.DeserializeBuiltin<int?>.Parse(0.ToString()), Is.EqualTo(0));
            Assert.That(ServiceStack.Text.Common.DeserializeBuiltin<int?>.Parse((-1).ToString()), Is.EqualTo(-1));
            Assert.That(ServiceStack.Text.Common.DeserializeBuiltin<int?>.Parse(1.ToString()), Is.EqualTo(1));
        }

        [Test]
        public void Can_deserialize_query_string_nullableInt_NaN_throws()
        {
            Assert.Throws(typeof(FormatException), () => Common.DeserializeBuiltin<int?>.Parse("NaN"));
        }

        [Test]
        public void Deos_serialize_QueryStrings()
        {
            var testPocos = new TestPocos { ListOfA = new List<A> { new A { ListOfB = new List<B> { new B { Property = "prop1" }, new B { Property = "prop2" } } } } };

            Assert.That(QueryStringSerializer.SerializeToString(testPocos), Is.EqualTo(
                "ListOfA={ListOfB:[{Property:prop1},{Property:prop2}]}"));

            Assert.That(QueryStringSerializer.SerializeToString(new[] { 1, 2, 3 }), Is.EqualTo(
                "[1,2,3]"));

            Assert.That(QueryStringSerializer.SerializeToString(new[] { "AA", "BB", "CC" }), Is.EqualTo(
                "[AA,BB,CC]"));
        }

        [Test]
        public void Can_serialize_quoted_strings()
        {
            Assert.That(QueryStringSerializer.SerializeToString(new B { Property = "\"quoted content\"" }), Is.EqualTo("Property=%22quoted+content%22"));
            Assert.That(QueryStringSerializer.SerializeToString(new B { Property = "\"quoted content, and with a comma\"" }), Is.EqualTo("Property=%22quoted+content,+and+with+a+comma%22"));
        }

#if NETFRAMEWORK
        private T StringToPoco<T>(string str)
        {
            var envKey = Environment.GetEnvironmentVariable("SERVICESTACK_LICENSE");
            if (!string.IsNullOrEmpty(envKey)) Licensing.RegisterLicense(envKey);
            using (new BasicAppHost().Init())
            {
                NameValueCollection queryString = HttpUtility.ParseQueryString(str);
                var restPath = new RestPath(typeof(T), "/query", "GET, POST");
                var restHandler = new RestHandler
                {
                    RestPath = restPath
                };
                var httpReq = new MockHttpRequest("query", "GET", "application/json", "query", queryString,
                                                  new MemoryStream(), new NameValueCollection());
                var request = (T)restHandler.CreateRequestAsync(httpReq, "query").Result;
                return request;
            }
        }

        [Test]
        public void Can_deserialize_quoted_strings()
        {
            Assert.That(StringToPoco<B>("Property=%22%22quoted%20content%22%22").Property, Is.EqualTo("\"\"quoted content\"\""));
            Assert.That(StringToPoco<B>("Property=%22%22quoted%20content,%20and%20with%20a%20comma%22%22").Property, Is.EqualTo("\"\"quoted content, and with a comma\"\""));
        }
#endif

        [Test]
        public void Can_serialize_with_comma_in_property_in_list()
        {
            var testPocos = new TestPocos
                {
                    ListOfA = new List<A> { new A { ListOfB = new List<B> { new B { Property = "Doe, John", Property2 = "Doe", Property3 = "John" } } } }
                };
            Assert.That(QueryStringSerializer.SerializeToString(testPocos), Is.EqualTo("ListOfA={ListOfB:[{Property:%22Doe,+John%22,Property2:Doe,Property3:John}]}"));
        }

#if NETFRAMEWORK
        [Test]
        public void Can_deserialize_with_comma_in_property_in_list_from_QueryStringSerializer()
        {
            var testPocos = new TestPocos
            {
                ListOfA = new List<A> { new A { ListOfB = new List<B> { new B { Property = "Doe, John", Property2 = "Doe", Property3 = "John" } } } }
            };
            var str = QueryStringSerializer.SerializeToString(testPocos);
            var poco = StringToPoco<TestPocos>(str);
            Assert.That(poco.ListOfA[0].ListOfB[0].Property, Is.EqualTo("Doe, John"));
            Assert.That(poco.ListOfA[0].ListOfB[0].Property2, Is.EqualTo("Doe"));
            Assert.That(poco.ListOfA[0].ListOfB[0].Property3, Is.EqualTo("John"));
        }

        [Test]
        public void Can_deserialize_with_comma_in_property_in_list_from_static()
        {
            var str = "ListOfA={ListOfB:[{Property:\"Doe,%20John\",Property2:Doe,Property3:John}]}";
            var poco = StringToPoco<TestPocos>(str);
            Assert.That(poco.ListOfA[0].ListOfB[0].Property, Is.EqualTo("Doe, John"));
            Assert.That(poco.ListOfA[0].ListOfB[0].Property2, Is.EqualTo("Doe"));
            Assert.That(poco.ListOfA[0].ListOfB[0].Property3, Is.EqualTo("John"));
        }
#endif

        [Test]
        public void Can_serialize_Poco_with_comma_in_string()
        {
            var dto = new B { Property = "Foo,Bar" };
            var qs = QueryStringSerializer.SerializeToString(dto);

            Assert.That(qs, Is.EqualTo("Property=Foo,Bar"));
        }

        [Test]
        public void Does_urlencode_Poco_with_escape_char()
        {
            var dto = new B { Property = "Foo&Bar" };
            var qs = QueryStringSerializer.SerializeToString(dto);

            Assert.That(qs, Is.EqualTo("Property=Foo%26Bar"));
        }

        public class TestPocos
        {
            public List<A> ListOfA { get; set; }
        }

        public class A
        {
            public List<B> ListOfB { get; set; }
        }

        public class B
        {
            public string Property { get; set; }
            public string Property2 { get; set; }
            public string Property3 { get; set; }
        }
    }
}