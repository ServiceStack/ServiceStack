using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateBinaryExpressionTests
    {
        [Test]
        public void Does_parse_basic_binary_arithmetic_expressions()
        {
            BinaryExpression expr;

            "1 + 2".ToStringSegment().ParseBinaryExpression(out expr);
            Assert.That(expr, Is.EqualTo(new BinaryExpression(new JsConstant(1), JsAddition.Operator, new JsConstant(2))));

            "1 - 2".ToStringSegment().ParseBinaryExpression(out expr);
            Assert.That(expr, Is.EqualTo(new BinaryExpression(new JsConstant(1), JsSubtraction.Operator, new JsConstant(2))));
            
            "1 * 2".ToStringSegment().ParseBinaryExpression(out expr);
            Assert.That(expr, Is.EqualTo(new BinaryExpression(new JsConstant(1), JsMultiplication.Operator, new JsConstant(2))));
            
            "1 / 2".ToStringSegment().ParseBinaryExpression(out expr);
            Assert.That(expr, Is.EqualTo(new BinaryExpression(new JsConstant(1), JsDivision.Operator, new JsConstant(2))));
            
            "1 & 2".ToStringSegment().ParseBinaryExpression(out expr);
            Assert.That(expr, Is.EqualTo(new BinaryExpression(new JsConstant(1), JsBitwiseAnd.Operator, new JsConstant(2))));
            
            "1 | 2".ToStringSegment().ParseBinaryExpression(out expr);
            Assert.That(expr, Is.EqualTo(new BinaryExpression(new JsConstant(1), JsBitwiseOr.Operator, new JsConstant(2))));
            
            "1 ^ 2".ToStringSegment().ParseBinaryExpression(out expr);
            Assert.That(expr, Is.EqualTo(new BinaryExpression(new JsConstant(1), JsBitwiseXOr.Operator, new JsConstant(2))));
            
            "1 << 2".ToStringSegment().ParseBinaryExpression(out expr);
            Assert.That(expr, Is.EqualTo(new BinaryExpression(new JsConstant(1), JsBitwiseLeftShift.Operator, new JsConstant(2))));
            
            "1 >> 2".ToStringSegment().ParseBinaryExpression(out expr);
            Assert.That(expr, Is.EqualTo(new BinaryExpression(new JsConstant(1), JsBitwiseRightShift.Operator, new JsConstant(2))));
        }

    }
}