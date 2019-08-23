using System;
using NUnit.Framework;
using ServiceStack.Script;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class ScriptDelegateTests
    {
        static string Hi() => "static fn";

        [Test]
        public void Can_call_delegates_as_function()
        {
            var context = new ScriptContext().Init();

            var result = context.EvaluateScript("{{ fn() }}", new ObjectDictionary {
                ["fn"] = (Func<string>) Hi
            });
            
            Assert.That(result, Is.EqualTo("static fn"));

            Func<string> hi = () => "instance fn";
            
            result = context.EvaluateScript("{{ fn() }}", new ObjectDictionary {
                ["fn"] = hi
            });

            Assert.That(result, Is.EqualTo("instance fn"));
        }

        [Test]
        public void Can_call_delegates_with_args_as_function()
        {
            var context = new ScriptContext().Init();

            Func<int, int, int> add = (a, b) => a + b; 

            var result = context.EvaluateScript("{{ fn(1,2) }}", new ObjectDictionary {
                ["fn"] = add
            });
            
            Assert.That(result, Is.EqualTo("3"));
        }

        [Test]
        public void Can_call_delegates_with_args_as_ext_methods()
        {
            var context = new ScriptContext().Init();

            Func<int, int, int> add = (a, b) => a + b; 

            var result = context.EvaluateScript("{{ 1.fn(2) }}", new ObjectDictionary {
                ["fn"] = add
            });
            
            Assert.That(result, Is.EqualTo("3"));
        }
    }
}