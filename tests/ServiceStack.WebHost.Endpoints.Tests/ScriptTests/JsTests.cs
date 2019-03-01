using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class JsTests
    {
        public class HasObject 
        {
            public object Value { get; set; }
        }

        [Test]
        public void Does_deserialize_late_bound_object_with_quotes()
        {
            var dto = new HasObject {
                Value = "<bla fasel=\"hurz\" />"
            };

            var json = JSON.stringify(dto);
            json.Print();
            
            Assert.That(json, Is.EqualTo("{\"Value\":\"<bla fasel=\\\"hurz\\\" />\"}"));

            var obj = (Dictionary<string,object>)JSON.parse(json);
            
            Assert.That(obj["Value"], Is.EqualTo(dto.Value));
            
            JS.Configure();

            var fromJson = json.FromJson<HasObject>();
            
            JS.UnConfigure();

            Assert.That(fromJson.Value, Is.EqualTo(dto.Value));
        }

        [Test]
        public void Can_parse_json_values()
        {
            Assert.That(JSON.parseSpan("true".AsSpan()), Is.EqualTo(true));
            Assert.That(JSON.parseSpan("false".AsSpan()), Is.EqualTo(false));
            Assert.That(JSON.parseSpan("1".AsSpan()), Is.EqualTo(1));
            Assert.That(JSON.parseSpan("1.1".AsSpan()), Is.EqualTo(1.1));
            Assert.That(JSON.parseSpan("foo".AsSpan()), Is.EqualTo("foo"));
            Assert.That(JSON.parseSpan("null".AsSpan()), Is.EqualTo(null));
            Assert.That(JSON.parseSpan("[1]".AsSpan()), Is.EqualTo(new object[]{ 1 }));
            Assert.That(JSON.parseSpan("{\"foo\":1}".AsSpan()), Is.EqualTo(new Dictionary<string,object> { ["foo"] = 1 }));
        }
    }
}