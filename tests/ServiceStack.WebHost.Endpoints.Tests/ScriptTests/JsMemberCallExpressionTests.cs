using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Script;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class JsMemberCallExpressionTests
    {
        [Test]
        public void Does_parse_member_call_expressions()
        {
            JsToken token;

            "a.b()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsMemberExpression(
                    new JsIdentifier("a"),
                    new JsIdentifier("b") 
                )
            )));
            
            "a[key]()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsMemberExpression(
                    new JsIdentifier("a"),
                    new JsIdentifier("key"),
                    computed:true
                )
            )));
            
            "a['key']()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsMemberExpression(
                    new JsIdentifier("a"),
                    new JsLiteral("key"),
                    computed:true
                )
            )));

            "a.b.c[key]()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsMemberExpression(
                new JsMemberExpression(
                    new JsMemberExpression(
                        new JsIdentifier("a"), 
                        new JsIdentifier("b") 
                    ),
                    new JsIdentifier("c")
                ),
                new JsIdentifier("key"),
                computed:true
            ))));
            
            "toDateTime('2001-01-01').Day()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsMemberExpression(
                    new JsCallExpression(
                        new JsIdentifier("toDateTime"),
                        new JsLiteral("2001-01-01")
                    ),
                    new JsIdentifier("Day")
                )
            )));

            "[toDateTime('2001-01-01')][0].Day()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                    new JsMemberExpression(
                    new JsMemberExpression(
                        new JsArrayExpression(
                            new JsCallExpression(
                                new JsIdentifier("toDateTime"),
                                new JsLiteral("2001-01-01")
                            )
                        ), 
                        new JsLiteral(0),
                        computed:true
                    ), 
                    new JsIdentifier("Day")
                )
            )));
        }

        [Test]
        public void Does_parse_member_call_expressions_on_literals()
        {
            JsToken token;

            "1.a()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsMemberExpression(
                    new JsLiteral(1), 
                    new JsIdentifier("a") 
                )
            )));            

            "1.2.a()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsMemberExpression(
                    new JsLiteral(1.2), 
                    new JsIdentifier("a") 
                )
            )));            

            "'a'.a()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsMemberExpression(
                    new JsLiteral("a"), 
                    new JsIdentifier("a") 
                )
            )));            

            "[1].a()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsMemberExpression(
                    new JsArrayExpression(new JsLiteral(1)), 
                    new JsIdentifier("a") 
                )
            )));            

            "{k:1}.a()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsMemberExpression(
                    new JsObjectExpression(
                        new JsProperty(new JsIdentifier("k"), new JsLiteral(1))
                    ), 
                    new JsIdentifier("a") 
                )
            )));
            
            "''.a()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsMemberExpression(
                    new JsLiteral(""), 
                    new JsIdentifier("a") 
                )
            )));            

            "[].a()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsMemberExpression(
                    new JsArrayExpression(), 
                    new JsIdentifier("a") 
                )
            )));            
        }

        [Test]
        public void Does_parse_member_call_expressions_on_literals_chained()
        {
            JsToken token;

            "1.a().b()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsMemberExpression(
                    new JsCallExpression(
                        new JsMemberExpression(
                            new JsLiteral(1), 
                            new JsIdentifier("a") 
                        )
                    ),
                    new JsIdentifier("b") 
                )
            )));

            "1.2.a().b()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsMemberExpression(
                    new JsCallExpression(
                        new JsMemberExpression(
                            new JsLiteral(1.2), 
                            new JsIdentifier("a") 
                        )
                    ),
                    new JsIdentifier("b") 
                )
            )));

            "'a'.a().b()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsMemberExpression(
                    new JsCallExpression(
                        new JsMemberExpression(
                            new JsLiteral("a"), 
                            new JsIdentifier("a") 
                        )
                    ),
                    new JsIdentifier("b") 
                )
            )));

            "[1].a().b()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsMemberExpression(
                    new JsCallExpression(
                        new JsMemberExpression(
                            new JsArrayExpression(new JsLiteral(1)), 
                            new JsIdentifier("a") 
                        )
                    ),
                    new JsIdentifier("b") 
                )
            )));

            "{k:1}.a().b()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsMemberExpression(
                    new JsCallExpression(
                        new JsMemberExpression(
                            new JsObjectExpression(
                                new JsProperty(new JsIdentifier("k"), new JsLiteral(1))
                            ), 
                            new JsIdentifier("a") 
                        )
                    ),
                    new JsIdentifier("b") 
                )
            )));
        }

        [Test]
        public void Does_parse_member_call_expressions_with_arrow_expression_args()
        {
            JsToken token;
            
            "a.b(x => x * 2)".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsMemberExpression(
                    new JsIdentifier("a"), 
                    new JsIdentifier("b") 
                ),
                new JsArrowFunctionExpression(
                    new JsIdentifier("x"),
                    new JsBinaryExpression(
                        new JsIdentifier("x"),
                        JsMultiplication.Operator, 
                        new JsLiteral(2)
                    )
                )
            )));
        }

        public class MyMethods : ScriptMethods
        {
            public object count(object target) => target == null
                ? 0
                : target is string s
                    ? s.Length
                    : target is ICollection c
                        ? c.Count
                            : target is IEnumerable e
                                ? e.Cast<object>().Count()
                                : throw new NotSupportedException($"Cannot count '{target.GetType().Name}'");

            public List<object> reverse(object target) => target == null
                ? new List<object>()
                : target is string s
                    ? s.Reverse().Cast<object>().ToList()
                        : target is IEnumerable e
                            ? e.Cast<object>().Reverse().ToList()
                            : throw new NotSupportedException($"Cannot count '{target.GetType().Name}'");

            public double square(double target) => target * target;
        }

        class TestTarget
        {
            public string String { get; set; }
            
            public int Int { get; set; }
            public double Double { get; set; }
            
            public int[] Nums { get; set; }
        }

        private static ScriptContext CreateScriptContext()
        {
            return new ScriptContext {
                ScriptMethods = { new MyMethods() },
                Args = {
                    ["a"] = new TestTarget {
                        Int = 2,
                        String = "test",
                        Nums = new[] { 1,2,3 },
                    },
                    ["c"] = "count",
                    ["two"] = 2,
                }
            };
        }
        
        [Test]
        public void Calling_method_with_no_args_on_members_calls_script_methods()
        {                     
            var context = CreateScriptContext().Init();
            
            Assert.That(context.EvaluateScript("{{ a.String.count() }}"), Is.EqualTo("4"));
            Assert.That(context.EvaluateScript("{{ a.String['count']() }}"), Is.EqualTo("4"));
            Assert.That(context.EvaluateScript("{{ a.String[c]() }}"), Is.EqualTo("4"));
            
            Assert.That(context.EvaluateScript("{{ [1,2,3].count() }}"), Is.EqualTo("3"));
            Assert.That(context.EvaluateScript("{{ {a:1,b:2}.count() }}"), Is.EqualTo("2"));

            Assert.That(context.EvaluateScript("{{ [1,2,3].reverse().reverse().count() }}"), Is.EqualTo("3"));
            Assert.That(context.EvaluateScript("{{ a.Nums.reverse().reverse().count() }}"), Is.EqualTo("3"));
            
            Assert.That(context.EvaluateScript("{{ 2.square() }}"), Is.EqualTo("4"));
            Assert.That(context.EvaluateScript("{{ 2.square().square() }}"), Is.EqualTo("16"));
            Assert.That(context.EvaluateScript("{{ two.square() }}"), Is.EqualTo("4"));
            Assert.That(context.EvaluateScript("{{ two.square().square() }}"), Is.EqualTo("16"));
            
            Assert.That(context.EvaluateScript("{{ a.String.count().square() }}"), Is.EqualTo("16"));
        }

        [Test]
        public void Can_call_methods_with_multiple_args()
        {
            var context = CreateScriptContext().Init();
            
            Assert.That(context.EvaluateScript("{{ 2.add(2) }}"), Is.EqualTo("4"));
            Assert.That(context.EvaluateScript("{{ two.add(two) }}"), Is.EqualTo("4"));
            Assert.That(context.EvaluateScript("{{ a.Int.add(two) }}"), Is.EqualTo("4"));

            Assert.That(context.EvaluateScript("{{ 2.add(2).add(2) }}"), Is.EqualTo("6"));
            Assert.That(context.EvaluateScript("{{ two.add(two).add(two) }}"), Is.EqualTo("6"));
            Assert.That(context.EvaluateScript("{{ a.Int.add(two).add(two) }}"), Is.EqualTo("6"));
            Assert.That(context.EvaluateScript("{{ a.Int.add(a.Int.add(two)) }}"), Is.EqualTo("6"));
            
            Assert.That(context.EvaluateScript("{{ 'fmt {0} {1}'.fmt('a',2.add(2)) }}"), Is.EqualTo("fmt a 4"));
            Assert.That(context.EvaluateScript("{{ 'fmt {0} {1} {2}'.fmt('a',2.add(2),a.Int.add(two).add(two)) }}"), Is.EqualTo("fmt a 4 6"));
        }
        
        [Test]
        public void Can_call_methods_with_multiple_args_and_scope()
        {
            var context = CreateScriptContext().Init();
            
            Assert.That(context.EvaluateScript("{{ 'foo'.assign(2).add(foo) }}"), Is.EqualTo("4"));
        }
        
        [Test]
        public void Can_call_methods_with_spread_args()
        {
            var context = CreateScriptContext().Init();
            
            Assert.That(context.EvaluateScript("{{ 'foo'.assign(...[2]).add(foo) }}"), Is.EqualTo("4"));
            Assert.That(context.EvaluateScript("{{ 'fmt {0} {1}'.fmt(...['a',2.add(2)]) }}"), Is.EqualTo("fmt a 4"));
            Assert.That(context.EvaluateScript("{{ 'fmt {0} {1} {2}'.fmt(...['a',2.add(2),a.Int.add(two).add(two)]) }}"), Is.EqualTo("fmt a 4 6"));
            Assert.That(context.EvaluateScript("{{ 'fmt {0} {1} {2}'.fmt(...range(3)) }}"), Is.EqualTo("fmt 0 1 2"));
            Assert.That(context.EvaluateScript("{{ 'fmt {0}'.fmt([...range(3)].count()) }}"), Is.EqualTo("fmt 3"));
        }
        
        [Test]
        public void Can_call_methods_with_arrow_expression_args()
        {
            var context = CreateScriptContext().Init();
            
            Assert.That(context.EvaluateScript("{{ a.Nums.map(x => x * 2) |> join }}"), Is.EqualTo("2,4,6"));
            Assert.That(context.EvaluateScript("{{ a.Nums.map(x => x * 2).count() }}"), Is.EqualTo("3"));
            Assert.That(context.EvaluateScript("{{ a.Nums.map(x => x * 2).map(x => x.square()) |> join }}"), Is.EqualTo("4,16,36"));
            
            Assert.That(context.EvaluateScript("{{ a.Int.times().map(x => x + 2) |> join }}"), Is.EqualTo("2,3"));
            Assert.That(context.EvaluateScript("{{ 'ABC'.repeat(3).count().divide(3).times().map(x => x + 2) |> join }}"), Is.EqualTo("2,3,4"));
            
            Assert.That(context.EvaluateScript("{{ 3.times().map(x => x[x.isEven() ? 'decr' : 'incr']()) |> join }}"), Is.EqualTo("-1,2,1"));
        }

        [Test]
        public void Does_stop_execution()
        {
            var context = CreateScriptContext().Init();

            Assert.That(context.EvaluateScript("{{ a.Nums.map(x => x * 2).use('A') }}"), Is.EqualTo("A"));
            Assert.That(context.EvaluateScript("{{ a.Nums.map(x => x * 2).end().use('A') }}"), Is.EqualTo(""));

            Assert.That(context.EvaluateScript("{{ a.Nums.map(x => x * 2).end().use('A') }}{{ 1 + 1 }}"), Is.EqualTo("2"));
            Assert.That(context.EvaluateScript("{{ a.Nums.map(x => x * 2).return().use('A') }}{{ 1 + 1 }}"), Is.EqualTo(""));
        }

    }
}