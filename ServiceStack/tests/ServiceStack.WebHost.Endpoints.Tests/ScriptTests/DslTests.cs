using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.Script;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class ProtoBufMethods : ScriptMethods
    {
        public ReadOnlyMemory<byte> serialize(object target)
        {
            var ms = new MemoryStream();
            global::ProtoBuf.Serializer.NonGeneric.Serialize(ms, target);
            return ms.GetBufferAsMemory();
        }
    }

    [DataContract]
    public class PersonContract
    {
        [DataMember(Order = 1)]
        public string Name { get; set; }
            
        [DataMember(Order = 2)]
        public int Age { get; set; }
    }

    public class DslTests
    {
        private static ScriptContext CreateContext() => new ScriptContext {
            InsertScriptMethods = {
                new ProtoBufMethods()
            }
        }.Init();

        [Test]
        public void Can_use_injected_binary_serializer_with_EvaluateScript()
        {
            var context = CreateContext();

            var person = new PersonContract { Name = "Kurt", Age = 27 };
            var result = (ReadOnlyMemory<byte>)context.Evaluate("{{ serialize(target) |> return }}", new Dictionary<string, object> {
                ["target"] = person
            });
            
            var ms = new MemoryStream(result.ToArray());
            var fromScript = (PersonContract)global::ProtoBuf.Serializer.NonGeneric.Deserialize(typeof(PersonContract), ms);
            
            Assert.That(fromScript.Name, Is.EqualTo(person.Name));
            Assert.That(fromScript.Age, Is.EqualTo(person.Age));
        }

        [Test]
        public void Can_use_injected_binary_serializer_via_eval_and_custom_Scope()
        {
            var context = CreateContext();

            var person = new PersonContract { Name = "Kurt", Age = 27 };
            var scope = context.CreateScope(new Dictionary<string, object> {
                ["target"] = person
            });
            var result = (ReadOnlyMemory<byte>)JS.eval("serialize(target)", scope);
            
            var ms = new MemoryStream(result.ToArray());
            var fromScript = (PersonContract)global::ProtoBuf.Serializer.NonGeneric.Deserialize(typeof(PersonContract), ms);
            
            Assert.That(fromScript.Name, Is.EqualTo(person.Name));
            Assert.That(fromScript.Age, Is.EqualTo(person.Age));
        }

        [Test]
        public void Can_use_injected_binary_serializer_via_eval_and_custom_ScriptScopeContext()
        {
            var context = CreateContext();

            var person = new PersonContract { Name = "Kurt", Age = 27 };
            var scope = new ScriptScopeContext(context, new Dictionary<string, object> {
                ["target"] = person
            });

            var result = (ReadOnlyMemory<byte>)JS.eval("serialize(target)", scope);
            
            var ms = new MemoryStream(result.ToArray());
            var fromScript = (PersonContract)global::ProtoBuf.Serializer.NonGeneric.Deserialize(typeof(PersonContract), ms);
            
            Assert.That(fromScript.Name, Is.EqualTo(person.Name));
            Assert.That(fromScript.Age, Is.EqualTo(person.Age));
        }

        [Test]
        public void Can_use_injected_binary_serializer_via_page_result()
        {
            var context = CreateContext();

            var person = new PersonContract { Name = "Kurt", Age = 27 };
            
            var evalContext = new PageResult(context.OneTimePage("{{ serialize(target) |> return }}")) {
                Args = {
                    ["target"] = person
                }
            };
            evalContext.RenderToStringAsync().Wait();

            var result = (ReadOnlyMemory<byte>) evalContext.ReturnValue.Result;
            
            var ms = new MemoryStream(result.ToArray());
            var fromScript = (PersonContract)global::ProtoBuf.Serializer.NonGeneric.Deserialize(typeof(PersonContract), ms);
            
            Assert.That(fromScript.Name, Is.EqualTo(person.Name));
            Assert.That(fromScript.Age, Is.EqualTo(person.Age));
        }
    }
}