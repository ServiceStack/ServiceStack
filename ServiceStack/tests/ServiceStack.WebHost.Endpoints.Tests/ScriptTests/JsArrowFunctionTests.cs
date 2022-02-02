using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Script;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    [TestFixture]
    public class JsArrowFunctionTests
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

            "{ k: a => 1 }".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsObjectExpression(
                new JsProperty(
                    new JsIdentifier("k"),
                    new JsArrowFunctionExpression(
                        new JsIdentifier("a"),
                        new JsLiteral(1)
                    )
                ))));
        }

        [Test]
        public void Does_evaluate_shorthand_Arrow_Expressions()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript("{{ [1,2,3] |> map(it => it * it) |> sum }}"),  Is.EqualTo("14"));
            
            Assert.That(context.EvaluateScript("{{ [1,2,3] |> map(n => n * n) |> sum }}"),  Is.EqualTo("14"));
            
            Assert.That(context.EvaluateScript("{{ [1,2,3] |> map => it * it |> sum }}"),  Is.EqualTo("14"));
            
            Assert.That(context.EvaluateScript("{{ [1,2,3] |> where => it % 2 == 1 |> map => it * it |> sum }}"),  Is.EqualTo("10"));
            
            Assert.That(context.EvaluateScript("{{ [1,2,3] |> all => it > 2 |> lower }}"),  Is.EqualTo("false"));
            
            Assert.That(context.EvaluateScript("{{ [1,2,3] |> any => it > 2 |> show: Y }}"),  Is.EqualTo("Y"));
            
            Assert.That(context.EvaluateScript("{{ [1,2,3] |> orderByDesc => it |> join }}"),  Is.EqualTo("3,2,1"));
            
            Assert.That(context.EvaluateScript("{{ [3,2,1] |> orderBy => it |> join }}"),  Is.EqualTo("1,2,3"));

            Assert.That(context.EvaluateScript("{{ [1,2,3] |> map => it * it |> assignTo => values }}{{ values |> sum }}"),  Is.EqualTo("14"));

            Assert.That(context.EvaluateScript("{{ ['A','B','C'] |> map => lower(it) |> map => `${it}` |> join('') }}"),  Is.EqualTo("abc"));

            Assert.That(context.EvaluateScript("{{ ['A','B','C'] |> map => lower(it) |> map => `${it}` |> concat }}"),  Is.EqualTo("abc"));
        }

        [Test]
        public void Does_evaluate_let_bindings_Arrow_Expressions()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["people"] = new[] { new Person("name1", 1), new Person("name2", 2), new Person("name3", 3), }
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{ [1,2,3] |> let => { a: it * it, b: isOdd(it) } |> select: ({a},{b}), }}"),  
                Is.EqualTo("(1,True),(4,False),(9,True),"));

            Assert.That(context.EvaluateScript("{{ people |> let => { a: it.Name, b: it.Age * 2 } |> select: ({a},{b}), }}"),  
                Is.EqualTo("(name1,2),(name2,4),(name3,6),"));
            
            Assert.That(context.EvaluateScript("{{ people |> let => { it.Name, it.Age } |> select: ({Name},{Age}), }}"),  
                Is.EqualTo("(name1,1),(name2,2),(name3,3),"));
            
            Assert.That(context.EvaluateScript("{{ people |> map => { it.Name, it.Age } |> select: ({it.Name},{it.Age}), }}"),  
                Is.EqualTo("(name1,1),(name2,2),(name3,3),"));
        }

        [Test]
        public void Does_evaluate_toDictionary_Arrow_Expressions()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript(@"{{ [{name:'Alice',score:50},{name:'Bob',score:40},{name:'Cathy',score:45}] |> assignTo=>scoreRecords }}
Bob's score: {{ scoreRecords 
   |> toDictionary => it.name
   |> map => it.Bob
   |> select: { it.name } = { it.score }
}}"), Is.EqualTo("Bob's score: Bob = 40"));
        }

        [Test]
        public void Does_evaluate_reduce_Arrow_Expressions()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript(@"{{ [20, 10, 40, 50, 10, 70, 30] |> assignTo: attemptedWithdrawals }}
{{ attemptedWithdrawals 
   |> reduce((balance, nextWithdrawal) => ((nextWithdrawal <= balance) ? (balance - nextWithdrawal) : balance), 
            { initialValue: 100.0, })
   |> select: Ending balance: { it }. }}"), 
                Is.EqualTo("Ending balance: 20."));
        }
        
        public class AnagramEqualityComparer : IEqualityComparer<string> 
        {
            public bool Equals(string x, string y) => GetCanonicalString(x) == GetCanonicalString(y);
            public int GetHashCode(string obj) => GetCanonicalString(obj).GetHashCode();
            private string GetCanonicalString(string word) 
            {
                var wordChars = word.ToCharArray();
                Array.Sort(wordChars);
                return new string(wordChars);
            }
        }

        [Test]
        public void Does_evaluate_groupBy_Arrow_Expressions()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["anagramComparer"] = new AnagramEqualityComparer()
                }
            }.Init();
            
            Assert.That(context.EvaluateScript(@"{{ ['from   ', ' salt', ' earn ', '  last   ', ' near ', ' form  '] |> assignTo: anagrams }}
{{#each groupBy(anagrams, w => trim(w), { map: x => upper(x), comparer: anagramComparer }) }}{{it |> json}}{{/each}}"),
                Is.EqualTo(@"[""FROM   "","" FORM  ""]["" SALT"",""  LAST   ""]["" EARN "","" NEAR ""]"));
        }

        class MyFilters : ScriptMethods
        {
            public double pow(double arg1, double arg2) => arg1 / arg2;
        }

        [Test]
        public void Can_Invoke_Arrow_Expressions()
        {
            var context = new ScriptContext().Init();

            var expr = JS.expression("pow(2,2) + pow(4,2)");
            Assert.That(expr.Evaluate(), Is.EqualTo(20));
            
            Assert.That(JS.eval("pow(2,2) + pow(4,2)"), Is.EqualTo(20));

            var scope = JS.CreateScope(args: new Dictionary<string, object> {
                ["a"] = 2,
                ["b"] = 4,
            }); 
            Assert.That(JS.eval("pow(a,2) + pow(b,2)", scope), Is.EqualTo(20));

            var customPow = JS.CreateScope(functions: new MyFilters());
            Assert.That(JS.eval("pow(2,2) + pow(4,2)", customPow), Is.EqualTo(3));

            var arrowExpr = (JsArrowFunctionExpression)JS.expression("(a,b) => pow(a,2) + pow(b,2)");
            
            Assert.That(arrowExpr.Invoke(2,4), Is.EqualTo(20));

            Assert.That(arrowExpr.Invoke(customPow, 2,4), Is.EqualTo(3));
        }
    }
}