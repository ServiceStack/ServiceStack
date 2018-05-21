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
            "1".ParseExpression(out var expr);
            Assert.That(expr, Is.EqualTo(new JsConstant(1)));

            "1 > 2".ParseExpression(out expr);
            Assert.That(expr,
                Is.EqualTo(new BinaryExpression(new JsConstant(1), JsGreaterThan.Operator, new JsConstant(2))));
            
            "1 > 2 && 3 > 4".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new LogicalExpression(
                    new BinaryExpression(new JsConstant(1), JsGreaterThan.Operator, new JsConstant(2)),
                    JsAnd.Operator,
                    new BinaryExpression(new JsConstant(3), JsGreaterThan.Operator, new JsConstant(4))
                )
            ));
            
            "1 > 2 and 3 > 4".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new LogicalExpression(
                    new BinaryExpression(new JsConstant(1), JsGreaterThan.Operator, new JsConstant(2)),
                    JsAnd.Operator,
                    new BinaryExpression(new JsConstant(3), JsGreaterThan.Operator, new JsConstant(4))
                )
            ));
        }

        [Test]
        public void Does_parse_linq_examples()
        {
            var it = new JsBinding("it");

            "it < 5".ParseExpression(out var expr);
            Assert.That(expr,
                Is.EqualTo(new BinaryExpression(it, JsLessThan.Operator, new JsConstant(5))));

            "it.UnitsInStock > 0 and it.UnitPrice > 3".ParseExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new LogicalExpression(
                    new BinaryExpression(new CallExpression("it.UnitsInStock"), JsGreaterThan.Operator, new JsConstant(0)),
                    JsAnd.Operator,
                    new BinaryExpression(new CallExpression("it.UnitPrice"), JsGreaterThan.Operator, new JsConstant(3))
                )
            ));
        }

        [Test]
        public void Does_parse_not_unary_expression()
        {
            var it = new JsBinding("it");

            "!it".ParseExpression(out var expr);
            Assert.That(expr,
                Is.EqualTo(new UnaryExpression(JsNot.Operator, it)));

            "!contains(items, it)".ParseExpression(out expr);
            Assert.That(expr,
                Is.EqualTo(new UnaryExpression(JsNot.Operator, 
                    new CallExpression("contains") { Args = { "items".ToStringSegment(), "it".ToStringSegment() }})));
        }

    }
}