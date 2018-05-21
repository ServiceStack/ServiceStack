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

            "(1 + 2)".ParseExpression(out expr);
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
    }
}