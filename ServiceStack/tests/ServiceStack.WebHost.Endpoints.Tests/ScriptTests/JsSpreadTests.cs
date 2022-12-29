using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Script;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class JsSpreadTests
    {
        [Test]
        public void Does_parse_ArrayExpression_with_spread_operator()
        {
            JsToken token;
            
            "[...a]".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsArrayExpression(
                new JsSpreadElement(new JsIdentifier("a"))
            )));
            
            "[...[1]]".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsArrayExpression(
                new JsSpreadElement(new JsArrayExpression(
                    new JsLiteral(1)
                ))
            )));
            
            "[1, ...[2], 3]".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsArrayExpression(
                new JsLiteral(1),
                new JsSpreadElement(new JsArrayExpression(
                    new JsLiteral(2)
                )),
                new JsLiteral(3)
            )));
        }
        
        [Test]
        public void Does_parse_ObjectExpression_with_spread_operator()
        {
            JsToken token;
            
            "{...a}".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsObjectExpression(
                new JsProperty(null, new JsSpreadElement(new JsIdentifier("a")))
            )));
            
            "{...{b:2}}".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsObjectExpression(
                new JsProperty(
                    null, 
                    new JsSpreadElement(
                        new JsObjectExpression(
                            new JsProperty(new JsIdentifier("b"), new JsLiteral(2))
                        )
                    )
                )
            )));
            
            "{a:1, ...{b:2}, c:3}".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsObjectExpression(
                new JsProperty(new JsIdentifier("a"), new JsLiteral(1)),
                new JsProperty(
                    null, 
                    new JsSpreadElement(
                        new JsObjectExpression(
                            new JsProperty(new JsIdentifier("b"), new JsLiteral(2))
                        )
                    )
                ),
                new JsProperty(new JsIdentifier("c"), new JsLiteral(3))
            )));
        }
        
        [Test]
        public void Does_parse_CallExpression_with_spread_operator()
        {
            JsToken token;
            
            "fn(...a)".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsIdentifier("fn"),
                new JsSpreadElement(new JsIdentifier("a"))
            )));
            
            "fn(...[1])".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsIdentifier("fn"),
                new JsSpreadElement(new JsArrayExpression(
                    new JsLiteral(1)
                ))
            )));
            
            "fn(1, ...[2], 3)".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsIdentifier("fn"),
                new JsLiteral(1),
                new JsSpreadElement(new JsArrayExpression(
                    new JsLiteral(2)
                )),
                new JsLiteral(3)
            )));
            
            "fn(...range(3))".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsIdentifier("fn"),
                new JsSpreadElement(
                    new JsCallExpression(
                        new JsIdentifier("range"),
                        new JsLiteral(3)
                    )
                )
            )));
        }
        
        [Test]
        public void Does_evaluate_ArrayExpression_with_spread_operator()
        {
            var context = new ScriptContext {
                Args = {
                    ["a"] = new[]{ 2, 1 },
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("{{ [...a] |> sum }}"), Is.EqualTo("3"));
            
            Assert.That(context.EvaluateScript("{{ [...[2,1]] |> sum }}"), Is.EqualTo("3"));
            
            Assert.That(context.EvaluateScript("{{ [1, ...a, 4] |> sum }}"), Is.EqualTo("8"));
        }
        
        [Test]
        public void Does_evaluate_ObjectExpression_with_spread_operator()
        {
            var context = new ScriptContext {
                Args = {
                    ["a"] = new{ b = 2, c = 3 },
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("{{ {...a}.b }}"), Is.EqualTo("2"));
            Assert.That(context.EvaluateScript("{{ {...{b:2,c:3}}.b }}"), Is.EqualTo("2"));

            Assert.That(context.EvaluateScript("{{ {...a} |> values |> sum }}"), Is.EqualTo("5"));
            Assert.That(context.EvaluateScript("{{ {...{b:2,c:3}} |> values |> sum }}"), Is.EqualTo("5"));
            
            Assert.That(context.EvaluateScript("{{ { a:1, ...a, d:4} |> values |> sum }}"), Is.EqualTo("10"));
            Assert.That(context.EvaluateScript("{{ { a:1, ...{b:2,c:3}, d:4} |> values |> sum }}"), Is.EqualTo("10"));

            Assert.That(context.EvaluateScript("{{ { b:4, ...a, c:6} |> values |> sum }}"), Is.EqualTo("8"));
            Assert.That(context.EvaluateScript("{{ { b:4, ...{b:2,c:3}, c:6} |> values |> sum }}"), Is.EqualTo("8"));
        }

        [Test]
        public void Spread_operator_does_cascade_object_properties()
        {
            var context = new ScriptContext {
                Args = {
                    ["poco"] = new Person("foo", 1),
                    ["anon"] = new { Name = "bar", Age = 2 },
                    ["foo"] = new Person("foo", 3),
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("{{ {...poco}.Age }}"), Is.EqualTo("1"));

            Assert.That(context.EvaluateScript("{{ {...poco, ...anon}.Name }}"), Is.EqualTo("bar"));
            Assert.That(context.EvaluateScript("{{ {...poco, ...anon}.Age }}"), Is.EqualTo("2"));
            Assert.That(context.EvaluateScript("{{ {...poco, ...foo}.Age }}"), Is.EqualTo("3"));
        }

        class MyFilters : ScriptMethods
        {
            public double Min2(double a, double b) => Math.Min(a, b);
            public double Min3(double a, double b, double c) => new[]{ a,b,c }.Min();
        }

        [Test]
        public void Does_evaluate_CallExpression_with_spread_operator()
        {
            var context = new ScriptContext {
                Args = {
                    ["nums2"] = new[]{ 20,10 },
                    ["nums3"] = new[]{ 20,10,1 },
                },
                ScriptMethods = {
                    new MyFilters()
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("{{ Min2(...[20,10]) }}"), Is.EqualTo("10"));
            Assert.That(context.EvaluateScript("{{ Min2(...nums2) }}"), Is.EqualTo("10"));
            Assert.That(context.EvaluateScript("{{ Min3(...nums3) }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ 1 |> Min3(...[20,10]) }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ 30 |> Min3(...[20,10]) }}"), Is.EqualTo("10"));

            Assert.That(context.EvaluateScript("{{ Min3(30,...[20,10]) }}"), Is.EqualTo("10"));
            Assert.That(context.EvaluateScript("{{ Min3(30,...nums2) }}"), Is.EqualTo("10"));
            Assert.That(context.EvaluateScript("{{ Min3(...[20,10],1) }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ Min3(...nums2,1) }}"), Is.EqualTo("1"));
        }
        
    }
}