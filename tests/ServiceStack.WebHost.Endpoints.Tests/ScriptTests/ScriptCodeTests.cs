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
            var context = new ScriptContext {
                Plugins = { new MarkdownScriptPlugin() }
            }.Init();
            
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

        [Test]
        public void Can_execute_existing_Script_Blocks_in_Code_Statements()
        {
            var context = new ScriptContext {
                DebugMode = true,
                Plugins = {
                    new MarkdownScriptPlugin(),
                }
            }.Init();

            string output = null;
            object result = null;
            
            output = context.RenderCode(@"
                #noop
                    #each range(3)
                        ` - ${it + 1}`
                    /each
                /noop
            ");
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@""));
            
            output = context.RenderCode(@"
                #each range(3)
                    ` - ${it + 1}`
                /each
            ");
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"
 - 1
 - 2
 - 3".NormalizeNewLines()));
            

            output = context.RenderCode(@"
* comment *

{{#capture text}}
## Title
{{/capture}}

{{#capture appendTo text}}
{{#each range(3)}}
  - {{it + 1}}
{{/each}}
{{/capture}}

text | markdown
");
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"
<h2>Title</h2>
<ul>
<li>1</li>
<li>2</li>
<li>3</li>
</ul>".NormalizeNewLines()));
            
            result = context.EvaluateCode(@"
                #keyvalues dict ':'
                    Apples:       2
                    Oranges:      3                    
                    Grape Fruit:  2
                    Rock Melon:   3                    
                /keyvalues
                dict | return
            ");
                
            Assert.That(result, Is.EquivalentTo(new Dictionary<string, string> {
                {"Apples","2"},
                {"Oranges","3"},
                {"Grape Fruit","2"},
                {"Rock Melon","3"},
            }));
            
            result = context.EvaluateCode(@"
                #csv list
                    Apples,2,2
                    Oranges,3,3                   
                    Grape Fruit,2,2
                    Rock Melon,3,3                 
                /csv
                list | return");

            Assert.That(result, Is.EquivalentTo(new List<List<string>> {
                new List<string> { "Apples", "2", "2" },
                new List<string> { "Oranges", "3", "3" },
                new List<string> { "Grape Fruit", "2", "2" },
                new List<string> { "Rock Melon", "3", "3" },
            }));

            output = context.RenderCode(@"
{{#ul {if:hasAccess, each:items, where:'Age >= 2', class:['nav', !disclaimerAccepted?'blur':''], id:`ul-${id}`} }}
    {{#li {class: {alt:isOdd(index), active:Name==highlight} }}
        {{Name}}
    {{/li}}
{{else}}
    <div>no items</div>
{{/ul}}", new Dictionary<string, object> {
                ["items"] = new[] {new Person("foo", 1), new Person("bar", 2), new Person("baz", 3)},
                ["id"] = "menu",
                ["disclaimerAccepted"] = false,
                ["hasAccess"] = true,
                ["highlight"] = "baz",
                ["digits"] = new[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" },
            });
            
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"
<ul class=""nav blur"" id=""ul-menu"">
    <li>
        bar
    </li>
    <li class=""alt active"">
        baz
    </li>
</ul>".NormalizeNewLines()));
            

            output = context.RenderCode(@"
{{#partial content}}
 - List Item
{{/partial}}

'<h1>Title</h1>' | raw
'content' | partial | markdown");
         
            Assert.That(output.RemoveNewLines(), Is.EqualTo(@"<h1>Title</h1><ul><li>List Item</li></ul>".RemoveNewLines()));

            output = context.RenderCode(@"
{{#raw content}}
{{ - List Item }}
{{/raw}}

'# Title'
'{{ - List Item }}'");
         
            Assert.That(output.RemoveNewLines(), Is.EqualTo(@"# Title{{ - List Item }}".RemoveNewLines()));
            
            
            output = context.RenderCode(@"
3 | to => times
{{#while times > 0}}
{{times}} time{{times == 1 ? '' : 's'}}
{{times - 1 | to => times}}
{{/while}}
");
            
            Assert.That(output.NormalizeNewLines(), Is.EqualTo("3 times\n2 times\n1 time"));
            
            Assert.That(context.RenderCode(@"'Person '
{{#with person}}
{{Name}} is {{Age}} years old
{{/with}}".NormalizeNewLines(), 
                    new Dictionary<string, object> {
                        ["person"] = new Person { Name = "poco", Age = 27 },
                    }), 
                Is.EqualTo("Person \npoco is 27 years old\n"));
            
        }

    }
}