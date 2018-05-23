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
            JsToken expr;
            
            "1".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(new JsLiteral(1)));

            "1 > 2".ParseJsExpression(out expr);
            Assert.That(expr,
                Is.EqualTo(new JsBinaryExpression(new JsLiteral(1), JsGreaterThan.Operator, new JsLiteral(2))));
            
            "1 > 2 && 3 > 4".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new JsLogicalExpression(
                    new JsBinaryExpression(new JsLiteral(1), JsGreaterThan.Operator, new JsLiteral(2)),
                    JsAnd.Operator,
                    new JsBinaryExpression(new JsLiteral(3), JsGreaterThan.Operator, new JsLiteral(4))
                )
            ));
            
            "1 > 2 and 3 > 4".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new JsLogicalExpression(
                    new JsBinaryExpression(new JsLiteral(1), JsGreaterThan.Operator, new JsLiteral(2)),
                    JsAnd.Operator,
                    new JsBinaryExpression(new JsLiteral(3), JsGreaterThan.Operator, new JsLiteral(4))
                )
            ));
        }

        [Test]
        public void Does_parse_linq_examples()
        {
            var it = new JsIdentifier("it");

            "it < 5".ParseJsExpression(out var expr);
            Assert.That(expr,
                Is.EqualTo(new JsBinaryExpression(it, JsLessThan.Operator, new JsLiteral(5))));

            "it.UnitsInStock > 0 and it.UnitPrice > 3".ParseJsExpression(out expr);
            Assert.That(expr, Is.EqualTo(
                new JsLogicalExpression(
                    new JsBinaryExpression(
                        new JsMemberExpression(new JsIdentifier("it"), new JsIdentifier("UnitsInStock")), 
                        JsGreaterThan.Operator, 
                        new JsLiteral(0)
                    ),
                    JsAnd.Operator,
                    new JsBinaryExpression(
                        new JsMemberExpression(new JsIdentifier("it"), new JsIdentifier("UnitPrice")), 
                        JsGreaterThan.Operator, 
                        new JsLiteral(3)
                    )
                )
            ));
        }

        [Test]
        public void Does_parse_not_unary_expression()
        {
            var it = new JsIdentifier("it");

            "!it".ParseJsExpression(out var expr);
            Assert.That(expr,
                Is.EqualTo(new JsUnaryExpression(JsNot.Operator, it)));

            "!contains(items, it)".ParseJsExpression(out expr);
            Assert.That(expr,
                Is.EqualTo(new JsUnaryExpression(JsNot.Operator, 
                    new JsCallExpression("contains") { Args = { "items".ToStringSegment(), "it".ToStringSegment() }})));
        }

    }
}