using System;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Script;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class ClassTypeName
    {
        private readonly string fullTypeName;
        public ClassTypeName(object instance)
        {
            fullTypeName = ProtectedScripts.Instance.typeQualifiedName(instance.GetType());
        }

        public override string ToString() => fullTypeName;
    }


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

            var result = context.EvaluateScript("{{#function hi}}'hello' |> return{{/function}}{{ hi() }}");

            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public void Can_use_function_block_to_create_delegate_with_multiple_args_and_invoke_it()
        {
            var context = new ScriptContext().Init();

            var result = context.Evaluate(@"
                {{#function calc(a,b) }}
                    a * b |> to => c
                    a + b + c |> return
                {{/function}}
                {{ calc(1,2) |> return }}");

            Assert.That(result, Is.EqualTo(5));

            result = context.Evaluate(@"
                {{#function calc(a,b) }}
                    a * b |> to => c
                    a + b + c |> return
                {{/function}}
                {{ 1.calc(2) |> return }}");

            Assert.That(result, Is.EqualTo(5));
        }

        [Test]
        public void Can_use_function_block_to_create_delegate_with_multiple_args_and_invoke_it_LISP()
        {
            var context = new ScriptContext {
                ScriptLanguages = { ScriptLisp.Language },
            }.Init();

            var result = context.Evaluate(@"
                {{#defn calc [a, b] }}
                    (let ( (c (* a b)) )
                        (+ a b c)
                    )
                {{/defn}}
                {{ calc(3,4) }}
                {{ calc(1,2) |> return }}");

            Assert.That(result, Is.EqualTo(5));

            result = context.Evaluate(@"
                {{#defn calc [a, b] }}
                    (let ( (c (* a b)) )
                        (+ a b c)
                    )
                {{/defn}}
                {{ calc(3,4) }}
                {{ calc(1,2) |> return }}");

            Assert.That(result, Is.EqualTo(5));

            result = context.Evaluate(@"
                {{#defn fib [n] }}
                    (if (<= n 1)
                        n
                        (+ (fib (- n 1))
                           (fib (- n 2)) ))
                {{/defn}}
                {{ 10.fib() |> return }}");

            Assert.That(result, Is.EqualTo(55));
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
                {{ fib(" + depth + ") |> return }}";

            var result = context.Evaluate<int>(template(10));

            Assert.That(result, Is.EqualTo(55));

            Assert.That(context.MaxStackDepth, Is.EqualTo(25));

            result = context.Evaluate<int>(template(context.MaxStackDepth - 1));

            Assert.That(result, Is.EqualTo(46368));

            try
            {
                result = context.Evaluate<int>(template(context.MaxStackDepth + 1));
                Assert.Fail("Should throw");
            }
            catch (ScriptException e)
            {
                e.GetType().Name.Print();
                e.Message.Print();
                if (!(e.InnerException is StackOverflowException))
                    throw;
            }
        }
        
        static string staticTypeName(object o) => ProtectedScripts.Instance.typeQualifiedName(o.GetType());

        [Test]
        public void Can_call_Delegates_in_filter_expression()
        {
            Func<object, string> typeName = o => 
                ProtectedScripts.Instance.typeQualifiedName(o.GetType());
            
            var context = new ScriptContext {
                Args = {
                    ["fn"] = typeName,
                    ["staticfn"] = (Func<object, string>) staticTypeName,
                },
                ScriptMethods = { new ProtectedScripts() },
                AllowScriptingOfAllTypes = true
            }.Init();
            

            string result = null;
            result = context.Evaluate<string>(@"
                {{#function info(o) }}
                    o |> getType |> typeQualifiedName |> return
                {{/function}}
                {{ 'System.Text.StringBuilder'.new() |> info |> return }}");
            Assert.That(result, Is.EqualTo("System.Text.StringBuilder"));

            result = context.Evaluate<string>( 
                "{{ 'System.Text.StringBuilder'.new() |> fn |> return }}");
            Assert.That(result, Is.EqualTo("System.Text.StringBuilder"));

            result = context.Evaluate<string>( 
                "{{ 'System.Text.StringBuilder'.new() |> staticfn |> return }}");
            Assert.That(result, Is.EqualTo("System.Text.StringBuilder"));

            result = context.Evaluate<string>( 
                @"
                {{ Constructor('ServiceStack.WebHost.Endpoints.Tests.ScriptTests.ClassTypeName(string)') |> to => ctorfn }}
                {{ 'System.Text.StringBuilder'.new() |> ctorfn |> toString |> return }}");
            Assert.That(result, Is.EqualTo("System.Text.StringBuilder"));
        }

        public class CustomMethods : ScriptMethods
        {
            public static int Counter = 0;
            public string chooseColor(ScriptScopeContext scope) => chooseColor(scope, "#ffffff");
            public string chooseColor(ScriptScopeContext scope, string defaultColor)
            {
                Counter++;
                return defaultColor;
            }
        }

        [Test]
        public async Task Only_call_EvaluateCode_method_once()
        {
            CustomMethods.Counter = 0;
            var context = new ScriptContext {
                ScriptMethods = { new CustomMethods() },
            }.Init();

            var fn = ScriptCodeUtils.EnsureReturn("chooseColor(`#336699`)");
            var ret = await context.EvaluateCodeAsync(fn);
            Assert.That(ret, Is.EqualTo("#336699"));
            Assert.That(CustomMethods.Counter, Is.EqualTo(1));

            CustomMethods.Counter = 0;
            fn = ScriptCodeUtils.EnsureReturn("chooseColor");
            ret = await context.EvaluateCodeAsync(fn);
            Assert.That(ret, Is.EqualTo("#ffffff"));
            Assert.That(CustomMethods.Counter, Is.EqualTo(1));

            CustomMethods.Counter = 0;
            fn = ScriptCodeUtils.EnsureReturn("chooseColor()");
            ret = await context.EvaluateCodeAsync(fn);
            Assert.That(ret, Is.EqualTo("#ffffff"));
            Assert.That(CustomMethods.Counter, Is.EqualTo(1));
        }
    }
}