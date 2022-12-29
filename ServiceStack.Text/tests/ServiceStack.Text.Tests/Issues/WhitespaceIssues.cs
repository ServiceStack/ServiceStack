using System.Collections.Generic;
using System.IO;
using Northwind.Common.DataModel;
using NUnit.Framework;
using ServiceStack.Text.Json;

namespace ServiceStack.Text.Tests.Issues
{
    public class WhitespaceIssues
    {
        [Test]
        public void Does_deserialize_JsonObject_empty_string()
        {
            var json = "{\"Name\":\"\"}"; 
            var obj = json.FromJson<JsonObject>(); 
            var name = obj.Get("Name");
            Assert.That(name, Is.EqualTo(""));
        }

        [Test]
        public void Does_deserialize_empty_string_to_object()
        {
            var json = "\"\"";
            var obj = json.FromJson<object>();
            Assert.That(obj, Is.EqualTo(""));
        }

        public class ObjectEmptyStringTest
        {
            public object Name { get; set; }
        }

        [Test]
        public void Does_serialize_property_Empty_String()
        {
            JS.Configure();
            var dto = new ObjectEmptyStringTest { Name = "" };
            var json = dto.ToJson();

            var fromJson = json.FromJson<ObjectEmptyStringTest>();
            Assert.That(fromJson.Name, Is.EqualTo(dto.Name));
            
            var utf8 = json.ToUtf8Bytes();
            var ms = new MemoryStream(utf8);
            var fromMs = JsonSerializer.DeserializeFromStream<ObjectEmptyStringTest>(ms);
            Assert.That(fromMs.Name, Is.EqualTo(dto.Name));
            JS.UnConfigure();
        }

        [Test]
        public void Does_serialize_object_dictionary()
        {
            JsConfig.ConvertObjectTypesIntoStringDictionary = true;
            
            var value = "{Number:10330,CountryKey:DE,FederalStateKey:\"\",Salutation:,City:''}";
            var map = (Dictionary<string,object>) value.FromJsv<object>();
            
            Assert.That(map["Number"], Is.EqualTo("10330"));
            Assert.That(map["CountryKey"], Is.EqualTo("DE"));
            Assert.That(map["FederalStateKey"], Is.EqualTo(""));
            Assert.That(map["Salutation"], Is.EqualTo(""));
            Assert.That(map["City"], Is.EqualTo("''"));

            JsConfig.ConvertObjectTypesIntoStringDictionary = false;
        }
    }
}