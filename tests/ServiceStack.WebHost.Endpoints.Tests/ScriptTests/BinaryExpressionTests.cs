using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Script;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class BinaryExpressionTests
    {
        [Test]
        public void Does_parse_basic_binary_expressions()
        {
            JsToken expr;

            "1 + 2".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(new JsBinaryExpression(new JsLiteral(1), JsAddition.Operator, new JsLiteral(2))));

            "1 - 2".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(new JsBinaryExpression(new JsLiteral(1), JsSubtraction.Operator, new JsLiteral(2))));
            
            "1 * 2".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(new JsBinaryExpression(new JsLiteral(1), JsMultiplication.Operator, new JsLiteral(2))));
            
            "1 / 2".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(new JsBinaryExpression(new JsLiteral(1), JsDivision.Operator, new JsLiteral(2))));
            
            "1 & 2".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(new JsBinaryExpression(new JsLiteral(1), JsBitwiseAnd.Operator, new JsLiteral(2))));
            
            "1 | 2".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(new JsBinaryExpression(new JsLiteral(1), JsBitwiseOr.Operator, new JsLiteral(2))));
            
            "1 ^ 2".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(new JsBinaryExpression(new JsLiteral(1), JsBitwiseXOr.Operator, new JsLiteral(2))));
            
            "1 << 2".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(new JsBinaryExpression(new JsLiteral(1), JsBitwiseLeftShift.Operator, new JsLiteral(2))));
            
            "1 >> 2".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(new JsBinaryExpression(new JsLiteral(1), JsBitwiseRightShift.Operator, new JsLiteral(2))));
        }
        
        [Test]
        public void Does_parse_composite_binary_expressions()
        {
            JsToken expr;

            "1 + 2 + 3".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new JsBinaryExpression(
                    new JsBinaryExpression(new JsLiteral(1), JsAddition.Operator, new JsLiteral(2)), 
                    JsAddition.Operator, 
                    new JsLiteral(3)
                )
            ));

            "1 + 2 + 3 + 4".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new JsBinaryExpression(
                    new JsBinaryExpression(
                        new JsBinaryExpression(new JsLiteral(1), JsAddition.Operator, new JsLiteral(2)), 
                        JsAddition.Operator, 
                        new JsLiteral(3)), 
                    JsAddition.Operator, 
                    new JsLiteral(4)
                )
            ));
        }
        
        [Test]
        public void Does_parse_binary_expressions_with_precedence()
        {
            JsToken expr;

            "1 + 2 * 3".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new JsBinaryExpression(
                    new JsLiteral(1), 
                    JsAddition.Operator, 
                    new JsBinaryExpression(new JsLiteral(2), JsMultiplication.Operator, new JsLiteral(3))
                )
            ));

            "1 + 2 * 3 - 4".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new JsBinaryExpression(
                    new JsBinaryExpression(
                        new JsLiteral(1), 
                        JsAddition.Operator, 
                        new JsBinaryExpression(new JsLiteral(2), JsMultiplication.Operator, new JsLiteral(3))), 
                    JsSubtraction.Operator, 
                    new JsLiteral(4)
                )
            ));

            "1 + 2 * 3 - 4 / 5".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new JsBinaryExpression(
                    new JsBinaryExpression(
                        new JsLiteral(1), 
                        JsAddition.Operator, 
                        new JsBinaryExpression(new JsLiteral(2), JsMultiplication.Operator, new JsLiteral(3))), 
                    JsSubtraction.Operator, 
                    new JsBinaryExpression(new JsLiteral(4), JsDivision.Operator, new JsLiteral(5)))
                )
            );
        }

        [Test]
        public void Does_parse_binary_expression_with_brackets()
        {
            JsToken expr;

            "(1 + 2)".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new JsBinaryExpression(new JsLiteral(1), JsAddition.Operator, new JsLiteral(2))
            ));

            "(1 + 2) * 3".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new JsBinaryExpression(
                    new JsBinaryExpression(new JsLiteral(1), JsAddition.Operator, new JsLiteral(2)), 
                    JsMultiplication.Operator, 
                    new JsLiteral(3)
                )
            ));
            
            "(1 + 2) * (3 - 4)".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new JsBinaryExpression(
                    new JsBinaryExpression(new JsLiteral(1), JsAddition.Operator, new JsLiteral(2)), 
                    JsMultiplication.Operator, 
                    new JsBinaryExpression(new JsLiteral(3), JsSubtraction.Operator, new JsLiteral(4))
                )
            ));
            
            "(1 + 2) * ((3 - 4) / 5)".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new JsBinaryExpression(
                    new JsBinaryExpression(new JsLiteral(1), JsAddition.Operator, new JsLiteral(2)), 
                    JsMultiplication.Operator, 
                    new JsBinaryExpression(
                        new JsBinaryExpression(new JsLiteral(3), JsSubtraction.Operator, new JsLiteral(4)),
                        JsDivision.Operator,
                        new JsLiteral(5)
                    )
                )
            ));
        }

        [Test]
        public void Does_parse_logical_expressions()
        {
            JsToken expr;

            "!(true || false)".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new JsUnaryExpression(
                    JsNot.Operator,
                    new JsLogicalExpression(new JsLiteral(true), JsOr.Operator, new JsLiteral(false))
                )
            ));
        }

        [Test]
        public void Does_parse_binary_and_logical_expressions()
        {
            JsToken expr;

            "[1 + 2 * 3 > one && 1 * 2 < ten]".ParseJsExpression(out expr);
            
            Assert.That(expr, Is.EqualTo(
                new JsArrayExpression(
                    new JsLogicalExpression(
                        new JsBinaryExpression(
                            new JsBinaryExpression(
                                new JsLiteral(1),
                                JsAddition.Operator,
                                new JsBinaryExpression(
                                    new JsLiteral(2),
                                    JsMultiplication.Operator, 
                                    new JsLiteral(3)
                                )
                            ),
                            JsGreaterThan.Operator,
                            new JsIdentifier("one")                       
                        ),
                        JsAnd.Operator, 
                        new JsBinaryExpression(
                            new JsBinaryExpression(
                                new JsLiteral(1),
                                JsMultiplication.Operator, 
                                new JsLiteral(2)
                            ),
                            JsLessThan.Operator,
                            new JsIdentifier("ten")                       
                        )
                    )
                ))
            );
        }

        [Test]
        public void Does_parse_unary_expression()
        {
            JsToken expr;

            "-1".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(new JsUnaryExpression(JsMinus.Operator, new JsLiteral(1))));
            "+1".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(new JsUnaryExpression(JsPlus.Operator, new JsLiteral(1))));
            "!true".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(new JsUnaryExpression(JsNot.Operator, new JsLiteral(true))));
        }

        [Test]
        public void Does_evaluate_templates_with_expressions()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript("{{ 1 + 2 }}"), Is.EqualTo("3"));
            Assert.That(context.EvaluateScript("{{ 1 - 2 }}"), Is.EqualTo("-1"));
            Assert.That(context.EvaluateScript("{{ 1 * 2 }}"), Is.EqualTo("2"));
            Assert.That(context.EvaluateScript("{{ 1 / 2 }}"), Is.EqualTo("0.5"));
            Assert.That(context.EvaluateScript("{{ 1 / 2.0 }}"), Is.EqualTo("0.5"));
            Assert.That(context.EvaluateScript("{{ 1 & 2 }}"), Is.EqualTo("0"));
            //Needs to be in brackets so it's not considered as different filter expressions
            Assert.That(context.EvaluateScript("{{ (1 | 2) }}"), Is.EqualTo("3")); 
            Assert.That(context.EvaluateScript("{{ 1 ^ 2 }}"), Is.EqualTo("3"));
            Assert.That(context.EvaluateScript("{{ 1 << 2 }}"), Is.EqualTo("4"));
            Assert.That(context.EvaluateScript("{{ 1 >> 2 }}"), Is.EqualTo("0"));
            
            Assert.That(context.EvaluateScript("{{ 1 + 2 + 3 }}"), Is.EqualTo("6"));
            Assert.That(context.EvaluateScript("{{ 1 + 2 + 3 + 4 }}"), Is.EqualTo("10"));
            
            Assert.That(context.EvaluateScript("{{ 1 + 2 * 3 }}"), Is.EqualTo("7"));
            Assert.That(context.EvaluateScript("{{ 1 + 2 * 3 - 4 }}"), Is.EqualTo("3"));
            Assert.That(context.EvaluateScript("{{ 1 + 2 * 3 - 4 / 5 }}"), Is.EqualTo("6.2"));
            Assert.That(context.EvaluateScript("{{ 1 + 2 * 3 - 4 / 5.0 }}"), Is.EqualTo("6.2"));
            
            Assert.That(context.EvaluateScript("{{ (1 + 2) }}"), Is.EqualTo("3"));
            Assert.That(context.EvaluateScript("{{ (1 + 2) * 3 }}"), Is.EqualTo("9"));
            Assert.That(context.EvaluateScript("{{ (1 + 2) * (3 - 4) }}"), Is.EqualTo("-3"));
            Assert.That(context.EvaluateScript("{{ (1 + 2) * ((3 - 4) / 5.0) }}"), 
                Is.EqualTo("-0.6").Or.EqualTo("-0.6000000000000001")); // .NET Core 3.1+
        }

        [Test]
        public void Does_evaluate_binary_expressions_with_filters()
        {
            var context = new ScriptContext().Init();

            Assert.That(context.EvaluateScript("{{ 1 + 2 * 3 | add(3) }}"), Is.EqualTo("10"));
            Assert.That(context.EvaluateScript("{{ (1 | 2) | add(3) }}"), Is.EqualTo("6"));

            Assert.That(context.EvaluateScript("{{ add(1 + 2 * 3, 4) | add(-5) }}"), Is.EqualTo("6"));

            Assert.That(context.EvaluateScript("{{ [1+2,1+2*3] | sum }}"), Is.EqualTo("10"));
            Assert.That(context.EvaluateScript("{{ {a:1+2*3} | get('a') }}"), Is.EqualTo("7"));
        }

        [Test]
        public void Does_evaluate_binary_and_logical_expressions()
        {
            var context = new ScriptContext {
                Args = {
                    ["one"] = 1,
                    ["ten"] = 10,                    
                },
            }.Init();
            
            Assert.That(context.EvaluateScript("{{ [1 + 2 * 3 > one && 1 * 2 < ten] | get(0) }}"), Is.EqualTo("True"));
        }

        private static ScriptContext CreateContext()
        {
            var context = new ScriptContext {
                Args = {
                    ["a"] = null,
                    ["b"] = 2,
                    ["empty"] = "",
                    ["f"] = false,
                    ["zero"] = 0,
                    ["t"] = true,
                    ["one"] = 1,
                    ["obj"] = new Dictionary<string, object>(),
                    ["array"] = new List<object>(),
                }
            }.Init();
            return context;
        }

        [Test]
        public void Does_evaluate_Coalescing_expressions()
        {
            var context = CreateContext();
            
            Assert.That(context.EvaluateScript("{{ null ?? 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ a ?? 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ '' ?? 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ empty ?? 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ false ?? 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ f ?? 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ 0 ?? 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ zero ?? 1 }}"), Is.EqualTo("1"));

            Assert.That(context.EvaluateScript("{{ true ?? 1 }}"), Is.EqualTo("True"));
            Assert.That(context.EvaluateScript("{{ t ?? 1 }}"), Is.EqualTo("True"));
            Assert.That(context.EvaluateScript("{{ b ?? 1 }}"), Is.EqualTo("2"));
            Assert.That(context.EvaluateScript("{{ 2 ?? 1 }}"), Is.EqualTo("2"));
            Assert.That(context.EvaluateScript("{{ 1 ?? 2 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ one ?? 2 }}"), Is.EqualTo("1"));

            Assert.That(context.EvaluateScript("{{ 0 ?? 2 > 1 ? 'Y' : 'N' }}"), Is.EqualTo("Y"));
            Assert.That(context.EvaluateScript("{{ 2 ?? 0 > 1 ? 'Y' : 'N' }}"), Is.EqualTo("Y"));
        }

        [Test]
        public void Does_use_truthy_for_logical_expression()
        {
            var context = CreateContext();
            
            Assert.That(context.EvaluateScript("{{#if null}}f{{else}}t{{/if}}"), Is.EqualTo("t"));
            Assert.That(context.EvaluateScript("{{#if a}}f{{else}}t{{/if}}"), Is.EqualTo("t"));
            Assert.That(context.EvaluateScript("{{#if unknown}}f{{else}}t{{/if}}"), Is.EqualTo("t"));
            Assert.That(context.EvaluateScript("{{#if 0}}f{{else}}t{{/if}}"), Is.EqualTo("t"));
            Assert.That(context.EvaluateScript("{{#if ''}}f{{else}}t{{/if}}"), Is.EqualTo("t"));
            Assert.That(context.EvaluateScript("{{#if empty}}f{{else}}t{{/if}}"), Is.EqualTo("t"));
            Assert.That(context.EvaluateScript("{{#if false}}f{{else}}t{{/if}}"), Is.EqualTo("t"));

            Assert.That(context.EvaluateScript("{{#if a && true}}f{{else}}t{{/if}}"), Is.EqualTo("t"));
            Assert.That(context.EvaluateScript("{{#if a && t}}f{{else}}t{{/if}}"), Is.EqualTo("t"));
            Assert.That(context.EvaluateScript("{{#if true && a}}f{{else}}t{{/if}}"), Is.EqualTo("t"));
            Assert.That(context.EvaluateScript("{{#if unknown && true}}f{{else}}t{{/if}}"), Is.EqualTo("t"));
            Assert.That(context.EvaluateScript("{{#if true && unknown}}f{{else}}t{{/if}}"), Is.EqualTo("t"));
            Assert.That(context.EvaluateScript("{{#if empty && true}}f{{else}}t{{/if}}"), Is.EqualTo("t"));
            Assert.That(context.EvaluateScript("{{#if true && empty}}f{{else}}t{{/if}}"), Is.EqualTo("t"));

            Assert.That(context.EvaluateScript("{{#if a || true}}t{{else}}f{{/if}}"), Is.EqualTo("t"));
            Assert.That(context.EvaluateScript("{{#if true || a}}t{{else}}f{{/if}}"), Is.EqualTo("t"));
            Assert.That(context.EvaluateScript("{{#if unknown || true}}t{{else}}f{{/if}}"), Is.EqualTo("t"));
            Assert.That(context.EvaluateScript("{{#if true || unknown}}t{{else}}f{{/if}}"), Is.EqualTo("t"));
            Assert.That(context.EvaluateScript("{{#if empty || true}}t{{else}}f{{/if}}"), Is.EqualTo("t"));
            Assert.That(context.EvaluateScript("{{#if true || empty}}t{{else}}f{{/if}}"), Is.EqualTo("t"));
            
            Assert.That(context.EvaluateScript("{{#if {} && true}}t{{else}}f{{/if}}"), Is.EqualTo("t"));
            Assert.That(context.EvaluateScript("{{#if obj && true}}t{{else}}f{{/if}}"), Is.EqualTo("t"));
            Assert.That(context.EvaluateScript("{{#if [] && true}}t{{else}}f{{/if}}"), Is.EqualTo("t"));
            Assert.That(context.EvaluateScript("{{#if array && true}}t{{else}}f{{/if}}"), Is.EqualTo("t"));
        }

    }
}