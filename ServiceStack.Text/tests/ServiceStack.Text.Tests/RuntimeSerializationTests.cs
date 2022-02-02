using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Messaging;
using ServiceStack.Templates;
using ServiceStack.Text.Json;

namespace ServiceStack.Text.Tests
{
    public class RuntimeObject
    {
        public object Object { get; set; }
    }

    public class AType {}

    public class JsonType
    {
        public int Int { get; set; }
        public string String { get; set; }
        public bool Bool { get; set; }
        public string Null { get; set; }
        public List<object> List { get; set; }
        public Dictionary<string, object> Dictionary { get; set; }
    }

    [DataContract]
    public class DtoType { }

    [RuntimeSerializable]
    public class RuntimeSerializableType { }

    public class MetaType : IMeta
    {
        public Dictionary<string, string> Meta { get; set; }
    }

    public class RequestDto : IReturn<RequestDto> {}

#if NETFX
    [Serializable]
    public class SerialiazableType { }
#endif

    public class RuntimeInterface
    {
        public IObject Object { get; set; }
    }
    public interface IObject { }
    public class AInterface : IObject { }

    public class RuntimeObjects
    {
        public object[] Objects { get; set; }
    }

    public class RuntimeSerializationTests
    {
        string CreateJson(Type type) => CreateJson(type.AssemblyQualifiedName);
        string CreateJson(string typeInfo) => "{\"Object\":{\"__type\":\"" + typeInfo + "\"}}";

        [Test]
        public void Does_allow_builtin_DataTypes_in_Object()
        {
            var dto = "{\"Object\":1}".FromJson<RuntimeObject>();
            Assert.That(dto.Object, Is.EqualTo("1"));

            dto = "{\"Object\":\"foo\"}".FromJson<RuntimeObject>();
            Assert.That(dto.Object, Is.EqualTo("foo"));
        }

        [Test]
        public void Does_not_allow_UnknownTypes_in_Object()
        {
            var types = new[]
            {
                typeof(AType),
                typeof(XmlReaderSettings)
            };

            foreach (var type in types)
            {
                var json = CreateJson(type);
                try
                {
                    var instance = json.FromJson<RuntimeObject>();
                    Assert.Fail("Should throw " + type.Name);
                }
                catch (NotSupportedException ex)
                {
                    ex.Message.Print();
                }
            }
        }

        [Test]
        public void Can_bypass_RuntimeType_validation()
        {
            JsConfig.AllowRuntimeType = type => true;

            var json = CreateJson(typeof(AType));
            var instance = json.FromJson<RuntimeObject>();
            Assert.That(instance.Object, Is.TypeOf<AType>());

            JsConfig.AllowRuntimeType = null;
        }

        [Test]
        public void Does_Serialize_Allowed_Types()
        {
            var allowTypes = new[]
            {
                typeof(DtoType),
                typeof(RuntimeSerializableType),
                typeof(MetaType),
                typeof(RequestDto),
                typeof(UserAuth),
                typeof(UserAuthDetails),
                typeof(Message),
            };

            foreach (var allowType in allowTypes)
            {
                var json = CreateJson(allowType);
                var instance = json.FromJson<RuntimeObject>();
                Assert.That(instance.Object.GetType(), Is.EqualTo(allowType));
            }
        }

        [Test]
        public void Does_allow_Unknown_Types_in_Interface()
        {
            var allowTypes = new[]
            {
                typeof(AInterface),
            };

            foreach (var allowType in allowTypes)
            {
                var json = CreateJson(allowType);
                var instance = json.FromJson<RuntimeInterface>();
                Assert.That(instance.Object.GetType(), Is.EqualTo(allowType));
            }
        }

        string CreateJsonArray(Type type) => CreateJsonArray(type.AssemblyQualifiedName);
        string CreateJsonArray(string typeInfo) => "{\"Objects\":[{\"__type\":\"" + typeInfo + "\"}]}";

        [Test]
        public void Does_not_allow_UnknownTypes_in_Objects_Array()
        {
            var types = new[]
            {
                typeof(AType),
                typeof(XmlReaderSettings)
            };

            foreach (var type in types)
            {
                var json = CreateJsonArray(type);
                try
                {
                    var instance = json.FromJson<RuntimeObjects>();
                    Assert.Fail("Should throw " + type.Name);
                }
                catch (NotSupportedException ex)
                {
                    ex.Message.Print();
                }
            }
        }

        [Test]
        public void Does_Serialize_Allowed_Types_in_Objects_Array()
        {
            var allowTypes = new[]
            {
                typeof(DtoType),
                typeof(RuntimeSerializableType),
                typeof(MetaType),
                typeof(RequestDto),
                typeof(UserAuth),
                typeof(UserAuthDetails),
                typeof(Message),
            };

            foreach (var allowType in allowTypes)
            {
                var json = CreateJsonArray(allowType);
                var instance = json.FromJson<RuntimeObjects>();
                Assert.That(instance.Objects.Length, Is.EqualTo(1));
                Assert.That(instance.Objects[0].GetType(), Is.EqualTo(allowType));
            }
        }

        [Test]
        public void Does_allow_Unknown_Type_in_MQ_Messages()
        {
            //Uses JsConfig.AllowRuntimeTypeInTypesWithNamespaces

            var mqMessage = new Message<AType> //Internal Type used for ServiceStack MQ
            {
                Body = new AType()
            };

            var json = mqMessage.ToJson();

            var fromJson = json.FromJson<Message>();
            Assert.That(fromJson.Body.GetType(), Is.EqualTo(typeof(AType)));
        }

        [Test]
        public void Does_allow_Unknown_Type_in_RequestLogEntry()
        {
            //Uses JsConfig.AllowRuntimeTypeInTypes

            var logEntry = new RequestLogEntry //Internal Type used for ServiceStack LogEntry
            {
                RequestDto = new AType()
            };

            var json = logEntry.ToJson();

            var fromJson = json.FromJson<RequestLogEntry>();
            Assert.That(fromJson.RequestDto.GetType(), Is.EqualTo(typeof(AType)));
        }

        [Test]
        public void Can_deserialize_object_with_unknown_JSON_into_object_type()
        {
            using (JsConfig.With(new Config { ExcludeTypeInfo = true }))
            {
                JS.Configure();

                var dto = new RuntimeObject
                {
                    Object = new JsonType
                    {
                        Int = 1,
                        String = "foo",
                        Bool = true,
                        List = new List<object> { new JsonType{Int = 1, String = "foo", Bool = true} },
                        Dictionary = new Dictionary<string, object> {{ "key", new JsonType{Int = 1, String = "foo", Bool = true} }},
                    }
                };

                var json = dto.ToJson();
                Assert.That(json, Is.EqualTo(@"{""Object"":{""Int"":1,""String"":""foo"",""Bool"":true," +
                    @"""List"":[{""Int"":1,""String"":""foo"",""Bool"":true}],""Dictionary"":{""key"":{""Int"":1,""String"":""foo"",""Bool"":true}}}}"));

                // into object
                var fromJson = json.FromJson<object>();
                var jsonObj = (Dictionary<string, object>)fromJson;
                var jsonType = (Dictionary<string, object>)jsonObj["Object"];
                Assert.That(jsonType["Int"], Is.EqualTo(1));
                Assert.That(jsonType["String"], Is.EqualTo("foo"));
                Assert.That(jsonType["Bool"], Is.EqualTo(true));
                var jsonList = (List<object>)jsonType["List"];
                Assert.That(((Dictionary<string, object>)jsonList[0])["Int"], Is.EqualTo(1));
                var jsonDict = (Dictionary<string, object>)jsonType["Dictionary"];
                Assert.That(((Dictionary<string, object>)jsonDict["key"])["Int"], Is.EqualTo(1));

                // into DTO with Object property
                var dtoFromJson = json.FromJson<RuntimeObject>();
                jsonType = (Dictionary<string, object>) dtoFromJson.Object;
                Assert.That(jsonType["Int"], Is.EqualTo(1));
                Assert.That(jsonType["String"], Is.EqualTo("foo"));
                Assert.That(jsonType["Bool"], Is.EqualTo(true));
                jsonList = (List<object>)jsonType["List"];
                Assert.That(((Dictionary<string, object>)jsonList[0])["Int"], Is.EqualTo(1));
                jsonDict = (Dictionary<string, object>)jsonType["Dictionary"];
                Assert.That(((Dictionary<string, object>)jsonDict["key"])["Int"], Is.EqualTo(1));

                JS.UnConfigure();
            }
        }

        [Test]
        public void Can_serialize_JS_literal_into_DTO()
        {
            JS.Configure();

            var js = @"{""Object"":{ Int:1,String:'foo',Bool:true,List:[{Int:1,String:`foo`,Bool:true}],Dictionary:{key:{Int:1,String:""foo"",Bool:true}}}}";

            // into DTO with Object property
            var dtoFromJson = js.FromJson<RuntimeObject>();
            var jsonType = (Dictionary<string, object>)dtoFromJson.Object;
            Assert.That(jsonType["Int"], Is.EqualTo(1));
            Assert.That(jsonType["Int"], Is.EqualTo(1));
            Assert.That(jsonType["String"], Is.EqualTo("foo"));
            Assert.That(jsonType["Bool"], Is.EqualTo(true));
            var jsonList = (List<object>)jsonType["List"];
            Assert.That(((Dictionary<string, object>)jsonList[0])["Int"], Is.EqualTo(1));
            var jsonDict = (Dictionary<string, object>)jsonType["Dictionary"];
            Assert.That(((Dictionary<string, object>)jsonDict["key"])["Int"], Is.EqualTo(1));

            JS.UnConfigure();
        }

        [Test]
        public void ServiceStack_AllowRuntimeType()
        {
            // Initialize static delegate to allow all types to be deserialized with the type attribute
            JsConfig.AllowRuntimeType = _ => true;
            JsConfig.TypeAttr = "$type";
            var example = new Example { Property = new MyProperty { Value = "Hello serializer" } };

            var serialized = JsonSerializer.SerializeToString(example);
            var deserialized = JsonSerializer.DeserializeFromString<Example>(serialized);
            Assert.IsNotNull(deserialized?.Property);

            // Now the same process with a config scope that has a TypeAttr that differs from the global TypeAttr value
            using var scope = JsConfig.With(new Config { TypeAttr = "_type" });
            serialized = JsonSerializer.SerializeToString(example);
            deserialized = JsonSerializer.DeserializeFromString<Example>(serialized);
            Assert.IsNotNull(deserialized?.Property);
            
            JsConfig.Reset();
        }

        private class Example
        {
            public IProperty Property { get; set; }
        }
        private interface IProperty
        {
        }
        private class MyProperty : IProperty
        {
            public string Value { get; set; }
        }

        [Test]
        public void Can_deserialize_object_into_string_dictionary()
        {
            var json = "{\"__type\":\"System.Collections.Generic.KeyValuePair`2[[System.String, System.Private.CoreLib],[System.String, System.Private.CoreLib]], System.Private.CoreLib\",\"Key\":\"A\",\"Value\":\"B\"}";
            var dto = json.FromJson<KeyValuePair<string, string>>();
            Assert.That(dto.Key, Is.EqualTo("A"));
            Assert.That(dto.Value, Is.EqualTo("B"));
        }
    }
}