using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Script;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class EvaluateScriptTests
    {
        [Test]
        public void Evaluate_does_return_ReturnValue()
        {
            var identity = new object();
            var context = new ScriptContext {
                Args = {
                    ["identity"] = identity
                }
            }.Init();

            Assert.That(context.Evaluate("{{ identity |> return }}"), Is.EqualTo(identity));
            Assert.That(context.Evaluate("{{ id |> return }}", new ObjectDictionary {
                ["id"] = identity,
            }), Is.EqualTo(identity));

            Assert.That(context.Evaluate("{{ 1 + 1 |> return }}"), Is.EqualTo(2));
            Assert.That(context.Evaluate("{{ return(1 + 1) }}"), Is.EqualTo(2));
        }

        [Test]
        public async Task Evaluate_does_return_ReturnValue_Async()
        {
            var identity = new object();
            var context = new ScriptContext {
                Args = {
                    ["identity"] = identity
                }
            }.Init();

            Assert.That(await context.EvaluateAsync("{{ identity |> return }}"), Is.EqualTo(identity));
            Assert.That(await context.EvaluateAsync("{{ id |> return }}", new ObjectDictionary {
                ["id"] = identity,
            }), Is.EqualTo(identity));

            Assert.That(await context.EvaluateAsync("{{ 1 + 1 |> return }}"), Is.EqualTo(2));
            Assert.That(await context.EvaluateAsync("{{ return(1 + 1) }}"), Is.EqualTo(2));
        }

        [Test]
        public void Evaluate_does_throw_ScriptException()
        {
            var context = new ScriptContext().Init();

            try
            {
                context.Evaluate("{{ 'fail' |> throw }} {{ 1 |> return }}");
                Assert.Fail("Should throw");
            }
            catch (ScriptException e)
            {
                Assert.That(e.Message, Is.EqualTo("fail"));
                Assert.That(e.InnerException.Message, Is.EqualTo("fail"));
                Assert.That(e.PageStackTrace, Is.Not.Null);
            }

            try
            {
                context.EvaluateScript("{{ 'fail' |> throw }} {{ 1 |> return }}");
                Assert.Fail("Should throw");
            }
            catch (ScriptException e)
            {
                Assert.That(e.Message, Is.EqualTo("fail"));
                Assert.That(e.InnerException.Message, Is.EqualTo("fail"));
                Assert.That(e.PageStackTrace, Is.Not.Null);
            }
        }

        [Test]
        public async Task Evaluate_does_throw_ScriptException_Async()
        {
            var context = new ScriptContext().Init();

            try
            {
                await context.EvaluateAsync("{{ 'fail' |> throw }} {{ 1 |> return }}");
                Assert.Fail("Should throw");
            }
            catch (ScriptException e)
            {
                Assert.That(e.Message, Is.EqualTo("fail"));
                Assert.That(e.InnerException.Message, Is.EqualTo("fail"));
                Assert.That(e.PageStackTrace, Is.Not.Null);
            }

            try
            {
                await context.EvaluateScriptAsync("{{ 'fail' |> throw }} {{ 1 |> return }}");
                Assert.Fail("Should throw");
            }
            catch (ScriptException e)
            {
                Assert.That(e.Message, Is.EqualTo("fail"));
                Assert.That(e.InnerException.Message, Is.EqualTo("fail"));
                Assert.That(e.PageStackTrace, Is.Not.Null);
            }
        }

        [Test]
        public void Evaluate_script_without_return_throws_NotSupportedException()
        {
            var context = new ScriptContext().Init();
            try
            {
                Assert.That(context.EvaluateScript("{{1 + 1}}"), Is.EqualTo("2"));
                
                context.Evaluate("{{ 1 + 1 }}");
                Assert.Fail("Should throw");
            }
            catch (NotSupportedException) {}
        }

        [Test]
        public void Evaluate_does_convert_return_Value()
        {
            var context = new ScriptContext().Init();

            Assert.That(context.Evaluate<string>("{{ return(1 + 1) }}"), Is.EqualTo("2"));
            Assert.That(context.Evaluate<int>("{{ return(1 + 1) }}"), Is.EqualTo(2));
            Assert.That(context.Evaluate<long>("{{ return(1 + 1) }}"), Is.EqualTo(2));
            Assert.That(context.Evaluate<double>("{{ return(1 + 1) }}"), Is.EqualTo(2));
            Assert.That(context.Evaluate<double>("{{ return('2') }}"), Is.EqualTo(2));

            Assert.That(context.Evaluate("{{ return({Age:1,Name:'foo'}) }}"), Is.EqualTo(new Dictionary<string, object> {
                {"Age", 1},
                {"Name", "foo"},
            }));

            var person = context.Evaluate<Person>("{{ return({Age:1,Name:'foo'}) }}");
            Assert.That(person.Age, Is.EqualTo(1));
            Assert.That(person.Name, Is.EqualTo("foo"));
            
            Assert.That(context.Evaluate<Dictionary<string, object>>("{{ return(person) }}", new ObjectDictionary {
                ["person"] = new Person { Age = 1, Name = "foo" }
            }), Is.EqualTo(new Dictionary<string, object> {
                {"Age", 1},
                {"Name", "foo"},
            }));
        }

    }
}