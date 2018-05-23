using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.Text;

#if NETCORE
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateCallExpressionTests
    {
        [Test]
        public void Does_parse_call_expression()
        {
            JsCallExpression expr;

            "a".ToStringSegment().ParseJsCallExpression(out expr);
            Assert.That(expr, Is.EqualTo(new JsCallExpression(new JsIdentifier("a"))));
            "a()".ToStringSegment().ParseJsCallExpression(out expr);
            Assert.That(expr, Is.EqualTo(new JsCallExpression(new JsIdentifier("a"))));
        }

        [Test]
        public void Can_parse_expressions_with_methods()
        {
            "mod(it,3) != 0".ParseJsExpression(out var token);
            Assert.That(token, Is.EqualTo(
                new JsBinaryExpression(
                    new JsCallExpression(
                        new JsIdentifier("mod"),
                        new JsIdentifier("it"),
                        new JsLiteral(3)
                    ),
                    JsNotEquals.Operator, 
                    new JsLiteral(0)
                )
            ));
        }
        
    }
}