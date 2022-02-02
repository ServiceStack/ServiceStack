using System;
using NUnit.Framework;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class ScriptSyntaxErrorTests
    {
        [Test]
        public void Does_handle_unterminated_expression()
        {
            var context = new ScriptContext().Init();

            try
            {
                context.Evaluate("{{");
                Assert.Fail("Should throw");
            }
            catch (SyntaxErrorException e)
            {
                e.Message.Print();
            }
        }
    }
}