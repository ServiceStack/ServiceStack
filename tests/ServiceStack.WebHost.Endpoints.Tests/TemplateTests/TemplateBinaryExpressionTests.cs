using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateBinaryExpressionTests
    {
        [Test]
        public void Does_parse_basic_binary_expressions()
        {
            JsToken expr;

            "1 + 2".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(new BinaryExpression(new JsConstant(1), JsAddition.Operator, new JsConstant(2))));

            "1 - 2".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(new BinaryExpression(new JsConstant(1), JsSubtraction.Operator, new JsConstant(2))));
            
            "1 * 2".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(new BinaryExpression(new JsConstant(1), JsMultiplication.Operator, new JsConstant(2))));
            
            "1 / 2".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(new BinaryExpression(new JsConstant(1), JsDivision.Operator, new JsConstant(2))));
            
            "1 & 2".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(new BinaryExpression(new JsConstant(1), JsBitwiseAnd.Operator, new JsConstant(2))));
            
            "1 | 2".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(new BinaryExpression(new JsConstant(1), JsBitwiseOr.Operator, new JsConstant(2))));
            
            "1 ^ 2".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(new BinaryExpression(new JsConstant(1), JsBitwiseXOr.Operator, new JsConstant(2))));
            
            "1 << 2".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(new BinaryExpression(new JsConstant(1), JsBitwiseLeftShift.Operator, new JsConstant(2))));
            
            "1 >> 2".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(new BinaryExpression(new JsConstant(1), JsBitwiseRightShift.Operator, new JsConstant(2))));
        }
        
        [Test]
        public void Does_parse_composite_binary_expressions()
        {
            JsToken expr;

            "1 + 2 + 3".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new BinaryExpression(
                    new BinaryExpression(new JsConstant(1), JsAddition.Operator, new JsConstant(2)), 
                    JsAddition.Operator, 
                    new JsConstant(3)
                )
            ));

            "1 + 2 + 3 + 4".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new BinaryExpression(
                    new BinaryExpression(
                        new BinaryExpression(new JsConstant(1), JsAddition.Operator, new JsConstant(2)), 
                        JsAddition.Operator, 
                        new JsConstant(3)), 
                    JsAddition.Operator, 
                    new JsConstant(4)
                )
            ));
        }
        
        [Test]
        public void Does_parse_binary_expressions_with_precedence()
        {
            JsToken expr;

            "1 + 2 * 3".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new BinaryExpression(
                    new JsConstant(1), 
                    JsAddition.Operator, 
                    new BinaryExpression(new JsConstant(2), JsMultiplication.Operator, new JsConstant(3))
                )
            ));

            "1 + 2 * 3 - 4".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new BinaryExpression(
                    new BinaryExpression(
                        new JsConstant(1), 
                        JsAddition.Operator, 
                        new BinaryExpression(new JsConstant(2), JsMultiplication.Operator, new JsConstant(3))), 
                    JsSubtraction.Operator, 
                    new JsConstant(4)
                )
            ));

            "1 + 2 * 3 - 4 / 5".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new BinaryExpression(
                    new BinaryExpression(
                        new JsConstant(1), 
                        JsAddition.Operator, 
                        new BinaryExpression(new JsConstant(2), JsMultiplication.Operator, new JsConstant(3))), 
                    JsSubtraction.Operator, 
                    new BinaryExpression(new JsConstant(4), JsDivision.Operator, new JsConstant(5)))
                )
            );
        }

        [Test]
        public void Does_parse_binary_expression_with_brackets()
        {
            JsToken expr;

            "(1 + 2)".ToStringSegment().ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new BinaryExpression(new JsConstant(1), JsAddition.Operator, new JsConstant(2))
            ));

            "(1 + 2) * 3".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new BinaryExpression(
                    new BinaryExpression(new JsConstant(1), JsAddition.Operator, new JsConstant(2)), 
                    JsMultiplication.Operator, 
                    new JsConstant(3)
                )
            ));
            
            "(1 + 2) * (3 - 4)".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new BinaryExpression(
                    new BinaryExpression(new JsConstant(1), JsAddition.Operator, new JsConstant(2)), 
                    JsMultiplication.Operator, 
                    new BinaryExpression(new JsConstant(3), JsSubtraction.Operator, new JsConstant(4))
                )
            ));
            
            "(1 + 2) * ((3 - 4) / 5)".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new BinaryExpression(
                    new BinaryExpression(new JsConstant(1), JsAddition.Operator, new JsConstant(2)), 
                    JsMultiplication.Operator, 
                    new BinaryExpression(
                        new BinaryExpression(new JsConstant(3), JsSubtraction.Operator, new JsConstant(4)),
                        JsDivision.Operator,
                        new JsConstant(5)
                    )
                )
            ));
        }

        [Test]
        public void Does_parse_unary_expression()
        {
            JsToken expr;

            "-1".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(new UnaryExpression(JsMinus.Operator, new JsConstant(1))));
            "+1".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(new UnaryExpression(JsPlus.Operator, new JsConstant(1))));
            "!true".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(new UnaryExpression(JsNot.Operator, new JsConstant(true))));
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

    }
}