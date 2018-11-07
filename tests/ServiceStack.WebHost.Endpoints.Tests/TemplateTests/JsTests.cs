using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
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
}
