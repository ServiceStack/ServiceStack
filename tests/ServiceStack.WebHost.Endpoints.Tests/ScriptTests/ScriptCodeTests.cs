using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Script;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class ScriptCodeTests
    {
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
                new PageBlockFragmentStatement(
                    new PageBlockFragment("1", "if", "true",
                        new JsBlockStatement(new JsExpressionStatement(new JsLiteral(1)))
                    )
                )
            };

            Assert.That(ParseCode("#if true\n1\n/if"), Is.EqualTo(expr));
            Assert.That(ParseCode(" #if true \n 1 \n /if "), Is.EqualTo(expr));
            Assert.That(ParseCode(" \n #if true \n \n 1 \n \n /if \n "), Is.EqualTo(expr));

            expr = new[] {
                new PageBlockFragmentStatement(new PageBlockFragment("1", "if", "true",
                    new JsBlockStatement(
                        new PageBlockFragmentStatement(
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
                new PageBlockFragmentStatement(new PageBlockFragment("1", "if", "true",
                    new JsBlockStatement(
                        new PageBlockFragmentStatement(
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
                new PageBlockFragmentStatement(
                    new PageBlockFragment("", "raw", "",
                        new List<PageFragment> { new PageStringFragment("1\n2") }
                    )
                )
            };
            Assert.That(ParseCode("#raw\n1\n2\n/raw"), Is.EqualTo(expr));
            Assert.That(ParseCode(" \n #raw\n1\n2\n/raw \n "), Is.EqualTo(expr));
            
            expr = new[] {
                new PageBlockFragmentStatement(
                    new PageBlockFragment("", "raw", "",
                        new List<PageFragment> { new PageStringFragment(" \n 1\n2 \n ") }
                    )
                )
            };
            Assert.That(ParseCode("#raw \n \n 1\n2 \n \n /raw"), Is.EqualTo(expr));
            Assert.That(ParseCode(" \n #raw \n \n 1\n2 \n \n /raw \n "), Is.EqualTo(expr));
        }
    }
}