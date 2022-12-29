using NUnit.Framework;
using ServiceStack.Script;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class JsLogicalExpressionTests
    {
        [Test]
        public void Does_parse_logical_expressions()
        {
            JsToken token;

            "a && b".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsLogicalExpression(
                new JsIdentifier("a"),
                JsAnd.Operator, 
                new JsIdentifier("b")
            )));

            "a || b".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsLogicalExpression(
                new JsIdentifier("a"),
                JsOr.Operator, 
                new JsIdentifier("b")
            )));

            "a || b && c || d".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsLogicalExpression(
                new JsLogicalExpression(
                    new JsIdentifier("a"),
                    JsOr.Operator, 
                    new JsLogicalExpression(
                        new JsIdentifier("b"),
                        JsAnd.Operator, 
                        new JsIdentifier("c")
                    )
                ), 
                JsOr.Operator, 
                new JsIdentifier("d")
            )));

            "(a || b) && (c || d)".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsLogicalExpression(
                new JsLogicalExpression(
                    new JsIdentifier("a"),
                    JsOr.Operator, 
                    new JsIdentifier("b")
                ),
                JsAnd.Operator, 
                new JsLogicalExpression(
                        new JsIdentifier("c"),
                        JsOr.Operator, 
                        new JsIdentifier("d")
                    )
                )
            ));
        }
        
        [Test]
        public void Does_parse_logical_expressions_using_keyword_operators()
        {
            JsToken token;

            "a and b".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsLogicalExpression(
                new JsIdentifier("a"),
                JsAnd.Operator, 
                new JsIdentifier("b")
            )));

            "a or b".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsLogicalExpression(
                new JsIdentifier("a"),
                JsOr.Operator, 
                new JsIdentifier("b")
            )));

            "a or b and c or d".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsLogicalExpression(
                new JsLogicalExpression(
                    new JsIdentifier("a"),
                    JsOr.Operator, 
                    new JsLogicalExpression(
                        new JsIdentifier("b"),
                        JsAnd.Operator, 
                        new JsIdentifier("c")
                    )
                ), 
                JsOr.Operator, 
                new JsIdentifier("d")
            )));

            "(a or b) and (c or d)".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsLogicalExpression(
                    new JsLogicalExpression(
                        new JsIdentifier("a"),
                        JsOr.Operator, 
                        new JsIdentifier("b")
                    ),
                    JsAnd.Operator, 
                    new JsLogicalExpression(
                        new JsIdentifier("c"),
                        JsOr.Operator, 
                        new JsIdentifier("d")
                    )
                )
            ));
        }

        [Test]
        public void Does_parse_ConditionalExpression()
        {
            JsToken token;

            "a ? b : c".ParseJsExpression(out token);  
            Assert.That(token, Is.EqualTo(new JsConditionalExpression(
                new JsIdentifier("a"),
                new JsIdentifier("b"),
                new JsIdentifier("c")
            )));

            "(1 < 2) ? 3 + 4 : -5 + (add(6,a) + 7)".ParseJsExpression(out token);  
            Assert.That(token, Is.EqualTo(new JsConditionalExpression(
                new JsBinaryExpression(
                    new JsLiteral(1),
                    JsLessThan.Operator,
                    new JsLiteral(2)
                ),
                new JsBinaryExpression(
                    new JsLiteral(3),
                    JsAddition.Operator,
                    new JsLiteral(4)
                ), 
                new JsBinaryExpression(
                    new JsUnaryExpression(JsMinus.Operator, new JsLiteral(5)),
                    JsAddition.Operator,
                    new JsBinaryExpression(
                        new JsCallExpression(
                            new JsIdentifier("add"),
                            new JsLiteral(6),
                            new JsIdentifier("a")
                        ), 
                        JsAddition.Operator,
                        new JsLiteral(7)
                    )
                )
            )));

            "1 + 2 > subtract(3, 4) ? 'YES' : 'NO'".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsConditionalExpression(
                new JsBinaryExpression(
                    new JsBinaryExpression(
                        new JsLiteral(1),
                        JsAddition.Operator,
                        new JsLiteral(2)
                    ), 
                    JsGreaterThan.Operator, 
                    new JsCallExpression(
                        new JsIdentifier("subtract"),
                        new JsLiteral(3),
                        new JsLiteral(4)
                    )
                ), 
                new JsLiteral("YES"),
                new JsLiteral("NO")
            )));
        }

        [Test]
        public void Does_evaluate_ConditionalExpression()
        {
            var context = new ScriptContext {
                Args = {
                    ["varTrue"] = true,
                    ["varFalse"] = false,
                    ["a"] = 1,
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("{{ true ? 1 : 0 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ false ? 1 : 0 }}"), Is.EqualTo("0"));
            Assert.That(context.EvaluateScript("{{ (1 < 2) ? 3 + 4 : -5 + (add(6,a) + 7) }}"), Is.EqualTo("7"));
            Assert.That(context.EvaluateScript("{{ 1 + 2 > subtract(3, 4) ? 'YES' : 'NO' }}"), Is.EqualTo("YES"));
        }
    }
}