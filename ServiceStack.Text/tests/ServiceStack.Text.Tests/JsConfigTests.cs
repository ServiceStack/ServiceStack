using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    public class Foo
    {
        public string FooBar { get; set; }
    }

    public class Bar
    {
        public string FooBar { get; set; }
    }

    [TestFixture]
    public class JsConfigAdhocTests
    {
        [Test]
        public void Can_escape_Html_Chars()
        {
            var dto = new Foo { FooBar = "<script>danger();</script>" };

            Assert.That(dto.ToJson(), Is.EqualTo("{\"FooBar\":\"<script>danger();</script>\"}"));

            JsConfig.EscapeHtmlChars = true;

            Assert.That(dto.ToJson(), Is.EqualTo("{\"FooBar\":\"\\u003cscript\\u003edanger();\\u003c/script\\u003e\"}"));

            JsConfig.Reset();
        }
    }

    [TestFixture]
    public class JsConfigTests
    {
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            JsConfig.TextCase = TextCase.SnakeCase;
            JsConfig<Bar>.TextCase = TextCase.PascalCase;
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            JsConfig.Reset();
        }

        [Test]
        public void Does_use_specific_configuration()
        {
            Assert.That(new Foo { FooBar = "value" }.ToJson(), Is.EqualTo("{\"foo_bar\":\"value\"}"));
            Assert.That(new Bar { FooBar = "value" }.ToJson(), Is.EqualTo("{\"FooBar\":\"value\"}"));
        }

        [Test]
        public void Can_override_default_configuration()
        {
            using (JsConfig.With(new Config { TextCase = TextCase.PascalCase }))
            {
                Assert.That(new Foo { FooBar = "value" }.ToJson(), Is.EqualTo("{\"FooBar\":\"value\"}"));
            }
        }
    }

    [TestFixture]
    public class SerializeEmitLowerCaseUnderscoreNamesTests
    {
        [Test]
        public void TestJsonDataWithJsConfigScope()
        {
            using (JsConfig.With(new Config { TextCase = TextCase.SnakeCase, PropertyConvention = PropertyConvention.Lenient}))
                AssertObjectJson();
        }

        [Test]
        public void TestJsonDataWithJsConfigScope_ext()
        {
            Assert.That(CreateObject().ToJson(config => config.TextCase = TextCase.SnakeCase),
                Is.EqualTo("{\"id\":1,\"root_id\":100,\"display_name\":\"Test object\"}"));
            Assert.That(CreateObject().ToJson(config => config.TextCase = TextCase.CamelCase),
                Is.EqualTo("{\"id\":1,\"rootId\":100,\"displayName\":\"Test object\"}"));
            Assert.That(CreateObject().ToJson(config => config.TextCase = TextCase.PascalCase),
                Is.EqualTo("{\"Id\":1,\"RootId\":100,\"DisplayName\":\"Test object\"}"));
        }

        [Test]
        public void TestCloneObjectWithJsConfigScope()
        {
            using (JsConfig.With(new Config { TextCase = TextCase.SnakeCase, PropertyConvention = PropertyConvention.Lenient}))
                AssertObject();
        }

        [Test]
        public void TestJsonDataWithJsConfigGlobal()
        {
            JsConfig.TextCase = TextCase.SnakeCase;
            JsConfig.PropertyConvention = PropertyConvention.Lenient;

            AssertObjectJson();

            JsConfig.Reset();
        }

        [Test]
        public void TestCloneObjectWithJsConfigGlobal()
        {
            JsConfig.TextCase = TextCase.SnakeCase;
            JsConfig.PropertyConvention = PropertyConvention.Lenient;

            AssertObject();

            JsConfig.Reset();
        }

        [Test]
        public void TestJsonDataWithJsConfigLocal()
        {
            JsConfig.TextCase = TextCase.SnakeCase;
            JsConfig.PropertyConvention = PropertyConvention.Lenient;

            AssertObjectJson();

            JsConfig.Reset();
        }

        [Test]
        public void TestCloneObjectWithJsConfigLocal()
        {
            JsConfig.TextCase = TextCase.Default;
            JsConfig<TestObject>.TextCase = TextCase.SnakeCase;

            AssertObject();

            JsConfig.Reset();
        }

        [Test]
        public void TestCloneObjectWithoutLowercaseThroughJsConfigLocal()
        {
            JsConfig.TextCase = TextCase.SnakeCase;
            JsConfig<TestObject>.TextCase = TextCase.Default;
            JsConfig.PropertyConvention = PropertyConvention.Lenient;

            AssertObject();

            JsConfig.Reset();
        }

        private void AssertObject()
        {
            var obj = CreateObject();
            var clonedObj = Deserialize(Serialize(obj));

            Assert.AreEqual(obj.Id, clonedObj.Id, AssertMessageFormat.Fmt("Id"));
            Assert.AreEqual(obj.RootId, clonedObj.RootId, AssertMessageFormat.Fmt("RootId"));
            Assert.AreEqual(obj.DisplayName, clonedObj.DisplayName, AssertMessageFormat.Fmt("DisplayName"));
        }

        private void AssertObjectJson()
        {
            var obj = CreateObject();
            var json = Serialize(obj);
            AssertObjectJson("Object Json: {0}", json);

            var cloned = CloneObject(obj);
            var clonedJson = Serialize(cloned);
            AssertObjectJson("Clone Object Json: {0}", clonedJson);
        }

        private void AssertObjectJson(string traceFormat, string json)
        {
            Trace.WriteLine(string.Format(traceFormat, json));

            Assert.True(json.Contains("\"root_id\":100,"), AssertMessageFormat.Fmt("root_id"));
            Assert.True(json.Contains("\"display_name\":\"Test object\""), AssertMessageFormat.Fmt("display_name"));
        }

        private string Serialize(TestObject obj)
        {
            return obj.ToJson();
        }

        private TestObject Deserialize(string str)
        {
            return str.FromJson<TestObject>();
        }

        private TestObject CreateObject()
        {
            return new TestObject
            {
                Id = 1,
                RootId = 100,
                DisplayName = "Test object"
            };
        }

        private TestObject CloneObject(TestObject src)
        {
            return Deserialize(Serialize(src));
        }

        class TestObject
        {
            public int Id { get; set; }
            public int RootId { get; set; }
            public string DisplayName { get; set; }
        }

        private const string AssertMessageFormat = "Cannot find correct property value ({0})";

        [Test]
        public void Can_indent_in_scoped_json()
        {
            var obj = new TestObject { Id = 1 };
            using (JsConfig.With(new Config { Indent = true }))
            {
                var scopedJson = obj.ToJson();
                Assert.That(scopedJson.NormalizeNewLines(), Is.EqualTo("{\n    \"Id\": 1,\n    \"RootId\": 0\n}"));
            }
            var json = obj.ToJson();
            Assert.That(json, Is.EqualTo("{\"Id\":1,\"RootId\":0}"));
        }
    }

    [TestFixture]
    public class JsConfigCreateTests
    {
        [Test]
        public void Does_create_scope_from_string()
        {
            var scope = JsConfig.CreateScope("emitlowercaseunderscorenames,IncludeNullValues:false,ExcludeDefaultValues:0,IncludeDefaultEnums:1,indent");
            Assert.That(scope.TextCase, Is.EqualTo(TextCase.SnakeCase));
            Assert.That(!scope.IncludeNullValues);
            Assert.That(!scope.ExcludeDefaultValues);
            Assert.That(scope.IncludeDefaultEnums);
            Assert.That(scope.Indent);
            scope.Dispose();

            scope = JsConfig.CreateScope("DateHandler:ISO8601,timespanhandler:durationformat,PropertyConvention:strict,TextCase:CamelCase");
            Assert.That(scope.DateHandler, Is.EqualTo(DateHandler.ISO8601));
            Assert.That(scope.TimeSpanHandler, Is.EqualTo(TimeSpanHandler.DurationFormat));
            Assert.That(scope.PropertyConvention, Is.EqualTo(PropertyConvention.Strict));
            Assert.That(scope.TextCase, Is.EqualTo(TextCase.CamelCase));
            scope.Dispose();
        }

        [Test]
        public void Does_create_scope_from_string_using_CamelCaseHumps()
        {
            var scope = JsConfig.CreateScope("eccn,inv:false,edv:0,ide:1,pp");
            Assert.That(scope.TextCase, Is.EqualTo(TextCase.CamelCase));
            Assert.That(!scope.IncludeNullValues);
            Assert.That(!scope.ExcludeDefaultValues);
            Assert.That(scope.IncludeDefaultEnums);
            Assert.That(scope.Indent);
            scope.Dispose();

            scope = JsConfig.CreateScope("dh:ISO8601,tsh:df,pc:strict,tc:cc");
            Assert.That(scope.DateHandler, Is.EqualTo(DateHandler.ISO8601));
            Assert.That(scope.TimeSpanHandler, Is.EqualTo(TimeSpanHandler.DurationFormat));
            Assert.That(scope.PropertyConvention, Is.EqualTo(PropertyConvention.Strict));
            Assert.That(scope.TextCase, Is.EqualTo(TextCase.CamelCase));
            scope.Dispose();
        }
    }

    public class JsConfigInitTests
    {
        [TearDown] public void TearDown() => JsConfig.Reset();
        
        [Test]
        public void Allows_setting_config_before_Init()
        {
            JsConfig.MaxDepth = 1;
            JsConfig.Init(new Config {
                DateHandler = DateHandler.UnixTime
            });
        }
        
        [Test]
        public void Does_not_allow_setting_JsConfig_after_Init()
        {
            JsConfig.Init(new Config {
                DateHandler = DateHandler.UnixTime
            });

            Assert.Throws<NotSupportedException>(() => JsConfig.MaxDepth = 1000);
        }

        [Test]
        public void Does_not_allow_setting_multiple_inits_in_StrictMode()
        {
            JsConfig.Init();
            JsConfig.Init(new Config { MaxDepth = 1 });

            Env.StrictMode = true;
            
            Assert.Throws<NotSupportedException>(JsConfig.Init);
        }

        [Test]
        public void Does_combine_global_configs_in_multiple_inits()
        {
            JsConfig.Init(new Config { MaxDepth = 1 });
            JsConfig.Init(new Config { DateHandler = DateHandler.UnixTime });
            
            Assert.That(JsConfig.MaxDepth, Is.EqualTo(1));
            Assert.That(JsConfig.DateHandler, Is.EqualTo(DateHandler.UnixTime));

            var newConfig = new Config();
            Assert.That(newConfig.MaxDepth, Is.EqualTo(1));
            Assert.That(newConfig.DateHandler, Is.EqualTo(DateHandler.UnixTime));
        }
    }

    public class JsConfigScopeTests
    {
        [Test]
        public void Does_ExcludeTypes_within_scope()
        {
            var o = new SomeObj
            {
                Name = "Freddie",
                Content = new byte[] { 42 }
            };

            var normalSerialization = NormalSerialization(o);
            var serializedWithoutByteArray = SerializeWithoutBytes(o);

            Console.WriteLine("Normal Serialization: " + normalSerialization);
            Console.WriteLine("Serialization without byte[]: " + serializedWithoutByteArray);

            Assert.That(normalSerialization.IndexOf("content", StringComparison.OrdinalIgnoreCase) >= 0,
                "normalSerialization: the Content property is missing");
            Assert.That(serializedWithoutByteArray.IndexOf("content", StringComparison.OrdinalIgnoreCase) == -1,
                "serializedWithoutByteArray: the Content property should not be present");
            Assert.That(serializedWithoutByteArray, Is.Not.EqualTo(normalSerialization),
                "Serializing the object with a different JsConfig settings should generate a different value.");

            string NormalSerialization<T>(T objToSerialize)
            {
                return objToSerialize.ToJson();
            }

            string SerializeWithoutBytes<T>(T objToSerialize)
            {
                using var scope = JsConfig.With(new Config {
                    ExcludeTypes = new HashSet<Type>(new[] { typeof(byte[]) })
                });
                return objToSerialize.ToJson();
            }
        }
    }

    public class SomeObj
    {
        public string Name { get; set; }

        public byte[] Content { get; set; }
    }    
}