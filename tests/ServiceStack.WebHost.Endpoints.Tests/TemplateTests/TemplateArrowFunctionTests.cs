using NUnit.Framework;
using ServiceStack.Templates;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    [TestFixture]
    public class TemplateArrowFunctionTests
    {
        [Test]
        public void Does_parse_Arrow_Expressions()
        {
            JsToken token;

            "a => 1".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsArrowFunctionExpression(
                new JsIdentifier("a"),
                new JsLiteral(1)
            )));

            "a => a + 1".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsArrowFunctionExpression(
                new JsIdentifier("a"),
                new JsBinaryExpression(
                    new JsIdentifier("a"), 
                    JsAddition.Operator,
                    new JsLiteral(1)
                )
            )));

            "a=>a+1".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsArrowFunctionExpression(
                new JsIdentifier("a"),
                new JsBinaryExpression(
                    new JsIdentifier("a"), 
                    JsAddition.Operator,
                    new JsLiteral(1)
                )
            )));

            "(a,b) => a + b".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsArrowFunctionExpression(
                new[]
                {
                    new JsIdentifier("a"),
                    new JsIdentifier("b"),
                },
                new JsBinaryExpression(
                    new JsIdentifier("a"), 
                    JsAddition.Operator,
                    new JsIdentifier("b")
                )
            )));

            "fn(a => a + b)".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(new JsIdentifier("fn"), 
                new JsArrowFunctionExpression(
                    new[]
                    {
                        new JsIdentifier("a"),
                    },
                    new JsBinaryExpression(
                        new JsIdentifier("a"), 
                        JsAddition.Operator,
                        new JsIdentifier("b")
                    )
                ))));

            "fn((a,b) => a + b)".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(new JsIdentifier("fn"), 
                new JsArrowFunctionExpression(
                    new[]
                    {
                        new JsIdentifier("a"),
                        new JsIdentifier("b"),
                    },
                    new JsBinaryExpression(
                        new JsIdentifier("a"), 
                        JsAddition.Operator,
                        new JsIdentifier("b")
                    )
                ))));
        }

        [Test]
        public void Does_evaluate_shorthand_Arrow_Expressions()
        {
            var context = new TemplateContext().Init();
            
            Assert.That(context.EvaluateTemplate("{{ [1,2,3] | map(it => it * it) | sum }}"),  Is.EqualTo("14"));
            
            Assert.That(context.EvaluateTemplate("{{ [1,2,3] | map(n => n * n) | sum }}"),  Is.EqualTo("14"));
            
            Assert.That(context.EvaluateTemplate("{{ [1,2,3] | map => it * it | sum }}"),  Is.EqualTo("14"));
            
            Assert.That(context.EvaluateTemplate("{{ [1,2,3] | where => it % 2 == 1 | map => it * it | sum }}"),  Is.EqualTo("10"));
            
            Assert.That(context.EvaluateTemplate("{{ [1,2,3] | all => it > 2 | lower }}"),  Is.EqualTo("false"));
            
            Assert.That(context.EvaluateTemplate("{{ [1,2,3] | any => it > 2 | show: Y }}"),  Is.EqualTo("Y"));
            
            Assert.That(context.EvaluateTemplate("{{ [1,2,3] | orderByDesc => it | join }}"),  Is.EqualTo("3,2,1"));
            
            Assert.That(context.EvaluateTemplate("{{ [3,2,1] | orderBy => it | join }}"),  Is.EqualTo("1,2,3"));

            Assert.That(context.EvaluateTemplate("{{ [1,2,3] | map => it * it | assignTo => values }}{{ values | sum }}"),  Is.EqualTo("14"));
        }

        [Test]
        public void Does_evaluate_let_bindings_Arrow_Expressions()
        {
            var context = new TemplateContext
            {
                Args =
                {
                    ["people"] = new[] { new Person("name1", 1), new Person("name2", 2), new Person("name3", 3), }
                }
            }.Init();

            Assert.That(context.EvaluateTemplate("{{ [1,2,3] | let => { a: it * it, b: isOdd(it) } | select: ({a},{b}), }}"),  
                Is.EqualTo("(1,True),(4,False),(9,True),"));

            Assert.That(context.EvaluateTemplate("{{ people | let => { a: it.Name, b: it.Age * 2 } | select: ({a},{b}), }}"),  
                Is.EqualTo("(name1,2),(name2,4),(name3,6),"));
            
            Assert.That(context.EvaluateTemplate("{{ people | let => { it.Name, it.Age } | select: ({Name},{Age}), }}"),  
                Is.EqualTo("(name1,1),(name2,2),(name3,3),"));
            
            Assert.That(context.EvaluateTemplate("{{ people | map => { it.Name, it.Age } | select: ({it.Name},{it.Age}), }}"),  
                Is.EqualTo("(name1,1),(name2,2),(name3,3),"));
        }

        [Test]
        public void Does_evaluate_toDictionary_Arrow_Expressions()
        {
            var context = new TemplateContext().Init();
            
            Assert.That(context.EvaluateTemplate(@"{{ [{name:'Alice',score:50},{name:'Bob',score:40},{name:'Cathy',score:45}] | assignTo=>scoreRecords }}
Bob's score: {{ scoreRecords 
   | toDictionary => it.name
   | map => it.Bob
   | select: { it.name } = { it.score }
}}"), Is.EqualTo("Bob's score: Bob = 40"));
        }

        [Test]
        public void Does_evaluate_reduce_ArrowExpression()
        {
            var context = new TemplateContext().Init();
            
            Assert.That(context.EvaluateTemplate(@"{{ [20, 10, 40, 50, 10, 70, 30] | assignTo: attemptedWithdrawals }}
{{ attemptedWithdrawals 
   | reduce((balance, nextWithdrawal) => ((nextWithdrawal <= balance) ? (balance - nextWithdrawal) : balance), 
            { initialValue: 100.0, })
   | select: Ending balance: { it }. }}"), 
                Is.EqualTo("Ending balance: 20."));
        }
    }
}