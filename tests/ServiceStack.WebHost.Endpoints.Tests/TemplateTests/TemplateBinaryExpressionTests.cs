using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.Text;
using System.Collections.Generic;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateBinaryExpressionTests
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

//        [Test]
//        public void Does_parse_binary_and_logical_expressions()
//        {
//            JsToken expr;
//
//            "[1 + 2 * 3 > one && 1 * 2 < ten]".ParseExpression(out expr);
//            
//            Assert.That(expr, Is.EqualTo(
//                new JsConstant(new List<object> {
//                    new JsLogicalExpression(
//                        new JsBinaryExpression(
//                            new JsBinaryExpression(
//                                new JsConstant(1),
//                                JsAddition.Operator,
//                                new JsBinaryExpression(
//                                    new JsConstant(2),
//                                    JsMultiplication.Operator, 
//                                    new JsConstant(3)
//                                )
//                            ),
//                            JsGreaterThan.Operator,
//                            new JsIdentifier("one")                       
//                        ),
//                        JsAnd.Operator, 
//                        new JsBinaryExpression(
//                            new JsBinaryExpression(
//                                new JsConstant(1),
//                                JsMultiplication.Operator, 
//                                new JsConstant(2)
//                            ),
//                            JsLessThan.Operator,
//                            new JsIdentifier("ten")                       
//                        )
//                    )
//                })
//            ));
//        }

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
            var context = new TemplateContext().Init();
            
            Assert.That(context.EvaluateTemplate("{{ 1 + 2 }}"), Is.EqualTo("3"));
            Assert.That(context.EvaluateTemplate("{{ 1 - 2 }}"), Is.EqualTo("-1"));
            Assert.That(context.EvaluateTemplate("{{ 1 * 2 }}"), Is.EqualTo("2"));
            Assert.That(context.EvaluateTemplate("{{ 1 / 2.0 }}"), Is.EqualTo("0.5"));
            Assert.That(context.EvaluateTemplate("{{ 1 & 2 }}"), Is.EqualTo("0"));
            //Needs to be in brackets so it's not considered as different filter expressions
            Assert.That(context.EvaluateTemplate("{{ (1 | 2) }}"), Is.EqualTo("3")); 
            Assert.That(context.EvaluateTemplate("{{ 1 ^ 2 }}"), Is.EqualTo("3"));
            Assert.That(context.EvaluateTemplate("{{ 1 << 2 }}"), Is.EqualTo("4"));
            Assert.That(context.EvaluateTemplate("{{ 1 >> 2 }}"), Is.EqualTo("0"));
            
            Assert.That(context.EvaluateTemplate("{{ 1 + 2 + 3 }}"), Is.EqualTo("6"));
            Assert.That(context.EvaluateTemplate("{{ 1 + 2 + 3 + 4 }}"), Is.EqualTo("10"));
            
            Assert.That(context.EvaluateTemplate("{{ 1 + 2 * 3 }}"), Is.EqualTo("7"));
            Assert.That(context.EvaluateTemplate("{{ 1 + 2 * 3 - 4 }}"), Is.EqualTo("3"));
            Assert.That(context.EvaluateTemplate("{{ 1 + 2 * 3 - 4 / 5.0 }}"), Is.EqualTo("6.2"));
            
            Assert.That(context.EvaluateTemplate("{{ (1 + 2) }}"), Is.EqualTo("3"));
            Assert.That(context.EvaluateTemplate("{{ (1 + 2) * 3 }}"), Is.EqualTo("9"));
            Assert.That(context.EvaluateTemplate("{{ (1 + 2) * (3 - 4) }}"), Is.EqualTo("-3"));
            Assert.That(context.EvaluateTemplate("{{ (1 + 2) * ((3 - 4) / 5.0) }}"), Is.EqualTo("-0.6"));
        }

        [Test]
        public void Does_evaluate_binary_expressions_with_filters()
        {
            var context = new TemplateContext().Init();

            Assert.That(context.EvaluateTemplate("{{ 1 + 2 * 3 | add(3) }}"), Is.EqualTo("10"));
            Assert.That(context.EvaluateTemplate("{{ (1 | 2) | add(3) }}"), Is.EqualTo("6"));

            Assert.That(context.EvaluateTemplate("{{ add(1 + 2 * 3, 4) | add(-5) }}"), Is.EqualTo("6"));

            Assert.That(context.EvaluateTemplate("{{ [1+2,1+2*3] | sum }}"), Is.EqualTo("10"));
            Assert.That(context.EvaluateTemplate("{{ {a:1+2*3} | get('a') }}"), Is.EqualTo("7"));
        }

        [Test]
        public void Does_evaluate_binary_and_logical_expressions()
        {
            var context = new TemplateContext {
                Args = {
                    ["one"] = 1,
                    ["ten"] = 10,                    
                },
            }.Init();
            
            Assert.That(context.EvaluateTemplate("{{ [1 + 2 * 3 > one && 1 * 2 < ten] | get(0) }}"), Is.EqualTo("True"));
        }

    }
}