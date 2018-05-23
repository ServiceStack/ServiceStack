using NUnit.Framework;
using ServiceStack.Templates;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateCallExpressionTests
    {
        [Test]
        public void Does_parse_call_expression()
        {
            JsToken token;

            "a()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression("a")));
        }
        
    }
}