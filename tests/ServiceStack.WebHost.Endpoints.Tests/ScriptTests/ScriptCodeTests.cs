using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class ScriptCodeTests
    {
        static ScriptCodeTests() => LogManager.LogFactory = new ConsoleLogFactory();

        [OneTimeTearDown]
        public void OneTimeTearDown() => LogManager.LogFactory = null;
        
        [Test]
        public void Can_parse_single_and_multi_line_code_statements()
        {
            var context = new ScriptContext().Init();

            JsStatement[] ParseCode(string str)
            {
                var statements = context.ParseCode(str).Statements;
                return statements;
            }

            JsStatement[] expr;
            expr = new[] {
                new JsExpressionStatement(new JsBinaryExpression(new JsLiteral(1), JsAddition.Operator,
                    new JsLiteral(2))),
                new JsExpressionStatement(new JsBinaryExpression(new JsLiteral(3), JsMultiplication.Operator,
                    new JsLiteral(4))),
            };

            Assert.That(ParseCode("1 + 2\n3 * 4"), Is.EqualTo(expr));
            Assert.That(ParseCode("1 + 2\n{{ 3 * 4 }}"), Is.EqualTo(expr));
            Assert.That(ParseCode("1 + 2\n {{ 3 * 4 }} "), Is.EqualTo(expr));
            Assert.That(ParseCode("1 + 2\n \n {{ \n  3 * 4 \n }} \n "), Is.EqualTo(expr));

            expr = new List<JsStatement>(expr) {
                new JsExpressionStatement(new JsBinaryExpression(new JsLiteral(5), JsDivision.Operator,
                    new JsLiteral(6))),
            }.ToArray();

            Assert.That(ParseCode("1 + 2\n \n {{ \n  3 * 4 \n }} \n 5 / 6"), Is.EqualTo(expr));
            Assert.That(ParseCode("1 + 2\n \n {{ \n  3 * 4 \n }} \n {{ 5 / 6 }} "), Is.EqualTo(expr));
            Assert.That(ParseCode("1 + 2\n \n {{ \n  3 * 4 \n }} \n {{ \n  5 / 6 \n  }} \n  "), Is.EqualTo(expr));
            Assert.That(ParseCode("1 + 2\n \n {{ \n  3\n*\n4 \n }} \n {{ \n  5 \n / \n 6 \n  }} \n  "),
                Is.EqualTo(expr));

            expr = new[] {
                new JsExpressionStatement(new JsBinaryExpression(new JsLiteral(1), JsAddition.Operator,
                    new JsLiteral(2))),
                new JsExpressionStatement(new JsLiteral("\n3\n*\n4\n")),
                new JsExpressionStatement(new JsLiteral(" 5 \n / \n 6 ")),
            };
            Assert.That(ParseCode("1 + 2\n \n {{ \n  '\n3\n*\n4\n' \n }} \n {{ \n ' 5 \n / \n 6 ' \n  }} \n  "),
                Is.EqualTo(expr));

            expr = new[] {
                new JsExpressionStatement(new JsBinaryExpression(new JsLiteral(1), JsAddition.Operator,
                    new JsLiteral(2))),
                new JsExpressionStatement(new JsTemplateLiteral("\n3\n*\n4\n")),
                new JsExpressionStatement(new JsTemplateLiteral(" 5 \n / \n 6 ")),
            };
            Assert.That(ParseCode("1 + 2\n \n {{ \n  `\n3\n*\n4\n` \n }} \n {{ \n ` 5 \n / \n 6 ` \n  }} \n  "),
                Is.EqualTo(expr));
            
            expr = new[] {
                new JsExpressionStatement(
                    new JsConditionalExpression(
                        new JsBinaryExpression(new JsIdentifier("a"), JsGreaterThan.Operator, new JsLiteral(2)),
                        new JsConditionalExpression(
                            new JsCallExpression(new JsMemberExpression(new JsIdentifier("a"), new JsIdentifier("isOdd"))),
                            new JsLiteral("a > 2 and odd"), 
                            new JsLiteral("a > 2 and even") 
                        ),
                        new JsConditionalExpression(
                            new JsCallExpression(new JsMemberExpression(new JsIdentifier("a"), new JsIdentifier("isOdd"))),
                            new JsLiteral("a <= 2 and odd"), 
                            new JsLiteral("a <= 2 and even") 
                        )
                    )
                ),
            };
            
            Assert.That(ParseCode(@"{{ (a > 2 
                ? (a.isOdd() ? 'a > 2 and odd'  : 'a > 2 and even') 
                : (a.isOdd() ? 'a <= 2 and odd' : 'a <= 2 and even')) }}"), Is.EqualTo(expr));

            Assert.That(ParseCode(@"  {{ 
(a > 2 
 ? 
(a.isOdd() ? 'a > 2 and odd'  : 'a > 2 and even') 
 : 
(a.isOdd() ? 'a <= 2 and odd' : 'a <= 2 and even')) 
}}  
"), Is.EqualTo(expr));
        }

        [Test]
        public void Can_parse_filter_expressions()
        {
            var context = new ScriptContext().Init();

            JsStatement[] ParseCode(string str)
            {
                var statements = context.ParseCode(str).Statements;
                return statements;
            }


            JsStatement[] expr;

            expr = new[] {
                new JsFilterExpressionStatement("1 | add(2)", new JsLiteral(1),
                    new JsCallExpression(new JsIdentifier("add"), new JsLiteral(2))),
            };

            Assert.That(ParseCode("1 | add(2)"), Is.EqualTo(expr));
            Assert.That(ParseCode("{{ 1 | add(2) }}"), Is.EqualTo(expr));
            Assert.That(ParseCode(" \n {{ \n 1 | add(2) \n }} \n "), Is.EqualTo(expr));

            expr = new[] {
                new JsFilterExpressionStatement("1 \n | \n add(2)", new JsLiteral(1),
                    new JsCallExpression(new JsIdentifier("add"), new JsLiteral(2))),
            };

            Assert.That(ParseCode("{{ \n 1 \n | \n add(2) \n }}"), Is.EqualTo(expr));
        }

        [Test]
        public void Can_parse_code_statements_with_blocks()
        {
            var context = new ScriptContext().Init();

            JsStatement[] ParseCode(string str)
            {
                var statements = context.ParseCode(str).Statements;
                return statements;
            }

            JsStatement[] expr;
            expr = new[] {
                new JsPageBlockFragmentStatement(
                    new PageBlockFragment("1", "if", "true",
                        new JsBlockStatement(new JsExpressionStatement(new JsLiteral(1)))
                    )
                )
            };

            Assert.That(ParseCode("#if true\n1\n/if"), Is.EqualTo(expr));
            Assert.That(ParseCode(" #if true \n 1 \n /if "), Is.EqualTo(expr));
            Assert.That(ParseCode(" \n #if true \n \n 1 \n \n /if \n "), Is.EqualTo(expr));

            expr = new[] {
                new JsPageBlockFragmentStatement(new PageBlockFragment("1", "if", "true",
                    new JsBlockStatement(
                        new JsPageBlockFragmentStatement(
                            new PageBlockFragment("1", "if", "a > b",
                                new JsBlockStatement(new JsExpressionStatement(new JsLiteral(1)))
                            )
                        )
                    )
                ))
            };
            Assert.That(ParseCode("#if true\n#if a > b\n1\n/if\n/if"), Is.EqualTo(expr));
            Assert.That(ParseCode("#if true \n \n #if a > b \n \n 1 \n \n /if \n \n /if \n \n "), Is.EqualTo(expr));

            expr = new[] {
                new JsPageBlockFragmentStatement(new PageBlockFragment("1", "if", "true",
                    new JsBlockStatement(
                        new JsPageBlockFragmentStatement(
                            new PageBlockFragment("1", "if", "a > b",
                                new JsBlockStatement(new JsExpressionStatement(new JsLiteral(1))),
                                new List<PageElseBlock> {
                                    new PageElseBlock("", new JsBlockStatement(new JsExpressionStatement(new JsLiteral(2))))                                    
                                }
                            )
                        )
                    ),
                    new List<PageElseBlock> {
                        new PageElseBlock("", new JsBlockStatement(new JsExpressionStatement(new JsLiteral("3"))))                                    
                    }
                ))
            };

            Assert.That(ParseCode("#if true\n#if a > b\n1\nelse\n2\n/if\nelse\n'3'\n/if"), Is.EqualTo(expr));
            Assert.That(ParseCode("#if true \n \n #if a > b \n \n 1 \n \n else \n \n 2 \n \n /if \n \n else \n \n '3' \n \n /if \n \n "), Is.EqualTo(expr));
            
            expr = new[] {
                new JsPageBlockFragmentStatement(new PageBlockFragment("", "if", "a > 1",
                    new JsBlockStatement(
                        new JsExpressionStatement(new JsLiteral("a > 1"))
                    ),
                    new List<PageElseBlock> {
                        new PageElseBlock("", 
                            new JsBlockStatement(
                                new JsExpressionStatement(new JsLiteral("a <= 1"))
                            )
                        )
                    }
                ))
            };
            
            // Tests \r\n on Windows
            Assert.That(ParseCode(@"
#if a > 1
'a > 1'
else
'a <= 1'
/if
"), Is.EqualTo(expr));
        }

        [Test]
        public void Can_parse_verbatim_blocks()
        {
            var context = new ScriptContext().Init();

            JsStatement[] ParseCode(string str)
            {
                var statements = context.ParseCode(str).Statements;
                return statements;
            }
 
            JsStatement[] expr;
            expr = new[] {
                new JsPageBlockFragmentStatement(
                    new PageBlockFragment("", "raw", "",
                        new List<PageFragment> { new PageStringFragment("1\n2") }
                    )
                )
            };
            Assert.That(ParseCode("#raw\n1\n2\n/raw"), Is.EqualTo(expr));
            Assert.That(ParseCode(" \n #raw\n1\n2\n/raw \n "), Is.EqualTo(expr));
            
            expr = new[] {
                new JsPageBlockFragmentStatement(
                    new PageBlockFragment("", "raw", "",
                        new List<PageFragment> { new PageStringFragment(" \n 1\n2 \n ") }
                    )
                )
            };
            Assert.That(ParseCode("#raw \n \n 1\n2 \n \n /raw"), Is.EqualTo(expr));
            Assert.That(ParseCode(" \n #raw \n \n 1\n2 \n \n /raw \n "), Is.EqualTo(expr));
            
            expr = new[] {
                new JsPageBlockFragmentStatement(
                    new PageBlockFragment("", "raw", "a > b",
                        new List<PageFragment> { new PageStringFragment(" \n 1\n2 \n ") }
                    )
                )
            };
            Assert.That(ParseCode("#raw a > b\n \n 1\n2 \n \n /raw"), Is.EqualTo(expr));
            Assert.That(ParseCode(" \n #raw a > b  \n \n 1\n2 \n \n /raw \n "), Is.EqualTo(expr));
        }

        [Test]
        public void Can_Evaluate_code()
        {
            var context = new ScriptContext().Init();

            string result = null;
            string code = null;
            var expected = @"1 <= 2 and odd
2 <= 2 and even
3 > 2 and odd
4 > 2 and even
5 > 2 and odd
".Replace("\r\n","\n");
            
            code = @"
#if a > 1
    `${a} > 1`
else
    `${a} <= 1`
/if
";
            result = context.RenderCode(code, new Dictionary<string, object> { ["a"] = 2 });
            Assert.That(result.Trim(), Is.EqualTo("2 &gt; 1"));

            code = @"
#if a > 1
    `${a} > 1` | raw
else
    `${a} <= 1` | raw
/if
";

            result = context.RenderCode(code, new Dictionary<string, object> { ["a"] = 1 });
            Assert.That(result.Trim(), Is.EqualTo("1 <= 1"));

            code = @"
range(5) | map => it + 1 | to => nums
#each a in nums
    #if a > 2
        #if a.isOdd() 
            `${a} > 2 and odd` | raw
        else
            `${a} > 2 and even` | raw
        /if
    else
        #if a.isOdd() 
            `${a} <= 2 and odd` | raw
        else
            `${a} <= 2 and even` | raw
        /if
    /if
/each
";

            result = context.RenderCode(code); 
            Assert.That(result, Is.EqualTo(expected));
            
            code = @"
#function testValue(a) 
    #if a > 2
        #if a.isOdd() 
            `${a} > 2 and odd` | return
        else
            `${a} > 2 and even` | return
        /if
    else
        #if a.isOdd() 
            `${a} <= 2 and odd` | return
        else
            `${a} <= 2 and even` | return
        /if
    /if
/function

range(5) | map => it + 1 | to => nums
#each nums
    it.testValue() | raw
/each
";
            
            result = context.RenderCode(code);
            Assert.That(result, Is.EqualTo(expected));
            
            code = @"
#function testValue(a) 
    return (a > 2 ? (a.isOdd() ? `${a} > 2 and odd`  : `${a} > 2 and even`) : (a.isOdd() ? `${a} <= 2 and odd` : `${a} <= 2 and even`)) 
/function

range(5) | map => it + 1 | to => nums
#each nums
    it.testValue() | raw
/each
";
            
            result = context.RenderCode(code);
            Assert.That(result, Is.EqualTo(expected));
            
            code = @"
#function testValue(a) 
    {{ return (a > 2 
        ? (a.isOdd() ? `${a} > 2 and odd`  : `${a} > 2 and even`) 
        : (a.isOdd() ? `${a} <= 2 and odd` : `${a} <= 2 and even`)) }} 
/function

range(5) | map => it + 1 | to => nums
#each nums
    it.testValue() | raw
/each
";
            
            result = context.RenderCode(code);
            Assert.That(result, Is.EqualTo(expected));
       }

        [Test]
        public void Can_evaluate_template_code_in_code_blocks()
        {
            var context = new ScriptContext().Init();

            string result = null;
            string code = null;

            result = context.RenderCode(@"
{{#if a > 1}}
    {{a}} > 1
{{else}}
    {{a}} <= 1
{{/if}}
", new Dictionary<string, object> { ["a"] = 1 });
            Assert.That(result.Trim(), Is.EqualTo("1 <= 1"));

            result = context.RenderCode(@"
                {{#if a > 1}}
                    {{a}} > 1
                {{else}}
                    {{a}} <= 1
                {{/if}}", new Dictionary<string, object> { ["a"] = 1 });
            Assert.That(result.Trim(), Is.EqualTo("1 <= 1"));

            result = context.RenderCode(@"
{{#if a > 1}}
    {{a}} > 1
{{else}}
    {{a}} <= 1
{{/if}}

#if a.isOdd()
    ` and is odd`
else
    ` and is even`
/if
", new Dictionary<string, object> { ["a"] = 1 });
            Assert.That(result.Trim(), Is.EqualTo("1 <= 1\n and is odd"));
        }

        [Test]
        public void Cannot_evaluate_Template_only_blocks_in_code_blocks()
        {
            var context = new ScriptContext().Init();
            try 
            { 
                context.RenderCode(@"
#capture out
    {{#each range(3)}}
        - {{it}}
    {{/each}}
/capture
");
                
                Assert.Fail("Should throw");
            }
            catch (ScriptException e)
            {
                e.Message.Print();
                if (e.InnerException.GetType() != typeof(NotSupportedException))
                    throw;
            }
        }

    }
}