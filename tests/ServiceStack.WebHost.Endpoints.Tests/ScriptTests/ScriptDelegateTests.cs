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
            Func<int, int, int> add = (a, b) => a + b;

            var context = new ScriptContext {
                Args = {
                    ["fn"] = add
                }
            }.Init();

            var result = context.EvaluateScript("{{ fn(1,2) }}");

            Assert.That(result, Is.EqualTo("3"));

            result = context.EvaluateScript("{{ 1.fn(2) }}");

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

        [Test]
        public void Can_use_function_block_to_create_delegate_and_invoke_it()
        {
            var context = new ScriptContext().Init();

            var result = context.EvaluateScript("{{#function hi}}'hello' | return{{/function}}{{ hi() }}");

            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public void Can_use_function_block_to_create_delegate_with_multiple_args_and_invoke_it()
        {
            var context = new ScriptContext().Init();

            var result = context.Evaluate(@"
                {{#function calc(a,b) }}
                    a * b | to => c
                    a + b + c | return
                {{/function}}
                {{ calc(1,2) | return }}");

            Assert.That(result, Is.EqualTo(5));

            result = context.Evaluate(@"
                {{#function calc(a,b) }}
                    a * b | to => c
                    a + b + c | return
                {{/function}}
                {{ 1.calc(2) | return }}");

            Assert.That(result, Is.EqualTo(5));
        }

        [Test]
        public void Does_not_Exceed_MaxStackDepth()
        {
            var context = new ScriptContext().Init();

            string template(int depth) => @"
                {{#function fib(num) }}
                    #if num <= 1
                        return(num)
                    /if
                    return (fib(num-1) + fib(num-2))
                {{/function}}
                {{ fib(" + depth + ") | return }}";

            var result = context.Evaluate<int>(template(10));

            Assert.That(result, Is.EqualTo(55));

            Assert.That(ScriptConfig.MaxStackDepth, Is.EqualTo(25));

            result = context.Evaluate<int>(template(ScriptConfig.MaxStackDepth - 1));

            Assert.That(result, Is.EqualTo(46368));

            try
            {
                result = context.Evaluate<int>(template(ScriptConfig.MaxStackDepth + 1));
                Assert.Fail("Should throw");
            }
            catch (ScriptException e)
            {
                if (!(e.InnerException is NotSupportedException))
                    throw;
            }
        }
    }
}