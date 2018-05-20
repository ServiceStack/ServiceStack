using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.Text;

#if NETCORE
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class QueryExpressionTests
    {
        [Test]
        public void Does_parse_basic_QueryExpressions()
        {
            "1".ParseConditionExpression(out var expr);
            Assert.That(expr, Is.EqualTo(new BinaryConditionExpression(new JsConstant(1), JsEquals.Operand, JsConstant.True)));

            "1 > 2".ParseConditionExpression(out expr);
            Assert.That(expr,
                Is.EqualTo(new BinaryConditionExpression(new JsConstant(1), JsGreaterThan.Operand, new JsConstant(2))));
            
            "1 > 2 and 3 > 4".ParseConditionExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new AndExpression(
                    new BinaryConditionExpression(new JsConstant(1), JsGreaterThan.Operand, new JsConstant(2)),
                    new BinaryConditionExpression(new JsConstant(3), JsGreaterThan.Operand, new JsConstant(4))
                )
            ));
        }

        [Test]
        public void Does_parse_linq_examples()
        {
            var it = new JsBinding("it");

            "it < 5".ParseConditionExpression(out var expr);
            Assert.That(expr,
                Is.EqualTo(new BinaryConditionExpression(it, JsLessThan.Operand, new JsConstant(5))));

            "it.UnitsInStock > 0 and it.UnitPrice > 3".ParseConditionExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new AndExpression(
                    new BinaryConditionExpression(new JsExpression("it.UnitsInStock"), JsGreaterThan.Operand, new JsConstant(0)),
                    new BinaryConditionExpression(new JsExpression("it.UnitPrice"), JsGreaterThan.Operand, new JsConstant(3))
                )
            ));
        }

        [Test]
        public void Does_parse_not_unary_expression()
        {
            var it = new JsBinding("it");

            "!it".ParseConditionExpression(out var expr);
            Assert.That(expr,
                Is.EqualTo(new BinaryConditionExpression(
                    new UnaryExpression(JsNot.Operator, it),
                    JsEquals.Operand,
                    JsConstant.True)));

            "!contains(items, it)".ParseConditionExpression(out expr);
            Assert.That(expr,
                Is.EqualTo(new BinaryConditionExpression(
                    new UnaryExpression(JsNot.Operator, 
                        new JsExpression("contains") { Args = { "items".ToStringSegment(), "it".ToStringSegment() }}),
                    JsEquals.Operand,
                    JsConstant.True)));
        }

    }
}