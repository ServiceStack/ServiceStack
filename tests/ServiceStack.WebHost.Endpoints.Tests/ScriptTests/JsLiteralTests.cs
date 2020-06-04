using NUnit.Framework;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class JsLiteralTests
    {
        [Test]
        public void Can_parse_simple_TemplateLiterals()
        {
            JsToken token;

            "``".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsTemplateLiteral(
                new[] { new JsTemplateElement("","", tail:true) })));

            "`a`".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsTemplateLiteral(
                new[] { new JsTemplateElement("a","a", tail:true) })));

            "1.0".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsLiteral(1.0)));

            var hold = ScriptConfig.ParseRealNumber;
            ScriptConfig.ParseRealNumber = numLiteral => numLiteral.ParseDecimal();

            "1.0".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsLiteral(1.0m)));

            ScriptConfig.ParseRealNumber = hold;

            "`a${b}`".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsTemplateLiteral(
                new[] {
                    new JsTemplateElement("a","a"),
                    new JsTemplateElement("","", tail:true),
                },
                new[] { new JsIdentifier("b") })));

            "`a ${b} c`".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsTemplateLiteral(
                new[] {
                    new JsTemplateElement("a ","a "),
                    new JsTemplateElement(" c"," c", tail:true),
                },
                new[] { new JsIdentifier("b") })));

            "`a ${b + 1} c ${incr(d + 1)}`".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsTemplateLiteral(
                new[] {
                    new JsTemplateElement("a ","a "),
                    new JsTemplateElement(" c "," c "),
                    new JsTemplateElement("","", tail:true)
                },
                new JsToken[] {
                    new JsBinaryExpression(
                        new JsIdentifier("b"),
                        JsAddition.Operator,
                        new JsLiteral(1)
                    ), 
                    new JsCallExpression(
                        new JsIdentifier("incr"),
                        new JsBinaryExpression(
                            new JsIdentifier("d"),
                            JsAddition.Operator,
                            new JsLiteral(1)
                        )
                    ), 
                })));

            "`\"\"`".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsTemplateLiteral(
                new[] {
                    new JsTemplateElement("\"\"", "\"\"", tail:true), 
                }
            )));

            "`''`".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsTemplateLiteral(
                new[] {
                    new JsTemplateElement("''", "''", tail:true), 
                }
            )));

            @"`""#key"".replace(/\\s+/g,'')`".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsTemplateLiteral(
                new[] {
                    new JsTemplateElement(
                        "\"#key\".replace(/\\\\s+/g,'')", 
                        "\"#key\".replace(/\\s+/g,'')", 
                        tail:true), 
                }
            )));
            
            "`${a}${b}`".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsTemplateLiteral(
                new[] {
                    new JsTemplateElement("",""),
                    new JsTemplateElement("",""),
                    new JsTemplateElement("","", tail:true),
                },
                new[] { new JsIdentifier("a"), new JsIdentifier("b") })));

        }

        [Test]
        public void Can_parse_strings_with_escape_chars()
        {
            JsToken token;
            
            @"′\\′".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsLiteral(@"\\")));
        }
         
        [Test]
        public void Can_parse_TemplateLiterals_with_escape_chars()
        {
            JsToken token;

            @"`${a}\${b}`".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsTemplateLiteral(
                new[] {
                    new JsTemplateElement("",""),
                    new JsTemplateElement(@"\${b}","${b}", tail:true),
                },
                new[] { new JsIdentifier("a") })));
            
            @"`${a}\\${b}`".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsTemplateLiteral(
                new[] {
                    new JsTemplateElement("",""),
                    new JsTemplateElement(@"\\",@"\"),
                    new JsTemplateElement("","", tail:true),
                },
                new[] { new JsIdentifier("a"), new JsIdentifier("b") })));

            @"`${a}\\\${b}`".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsTemplateLiteral(
                new[] {
                    new JsTemplateElement("",""),
                    new JsTemplateElement(@"\\\${b}",@"\${b}", tail:true),
                },
                new[] { new JsIdentifier("a") })));

            @"`\\`".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsTemplateLiteral(
                new[] { new JsTemplateElement("\\\\","\\", tail:true) })));
            
            @"`\\ \n`".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsTemplateLiteral(
                new[] { new JsTemplateElement(@"\\ \n","\\ \n", tail:true) })));
        }

        [Test]
        public void Can_evaluate_TemplateLiterals_with_escape_chars()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript(@"{{ `\\` }}"), 
                Is.EqualTo(@"\"));
            
            Assert.That(context.EvaluateScript(@"{{ `\\ \n` }}"), 
                Is.EqualTo("\\ \n"));
            
            Assert.That(context.EvaluateScript(@"{{ `""#key"".replace(/\\s+/g,'')` |> raw }}"), 
                Is.EqualTo(@"""#key"".replace(/\s+/g,'')"));
        }

        [Test]
        public void Does_evaluate_TemplateLiteral()
        {
            var context = new ScriptContext {
                Args = {
                    ["a"] = 1,
                    ["b"] = 2,
                    ["c"] = 3,
                    ["d"] = 4,
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("{{``}}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{`a`}}"), Is.EqualTo("a"));
            Assert.That(context.EvaluateScript("{{`a${b}`}}"), Is.EqualTo("a2"));
            Assert.That(context.EvaluateScript("{{`a ${b} c`}}"), Is.EqualTo("a 2 c"));
            Assert.That(context.EvaluateScript("{{`a ${b + 1} c ${incr(d + 1)}`}}"), Is.EqualTo("a 3 c 6"));
            Assert.That(context.EvaluateScript("{{`\n`}}"), Is.EqualTo("\n"));
            Assert.That(context.EvaluateScript("{{`a\n${b}`}}"), Is.EqualTo("a\n2"));
            Assert.That(context.EvaluateScript("{{`\"\"` |> raw}}"), Is.EqualTo("\"\""));
            Assert.That(context.EvaluateScript("{{`''` |> raw}}"), Is.EqualTo("''"));
            Assert.That(context.EvaluateScript("{{`a\"b\"c` |> raw}}"), Is.EqualTo("a\"b\"c"));
            Assert.That(context.EvaluateScript("{{`a'b'c` |> raw}}"), Is.EqualTo("a'b'c"));

            Assert.That(context.EvaluateScript("{{`a\"b\"c` |> appendTo: a}}{{ a |> raw }}"), Is.EqualTo("a\"b\"c"));
            Assert.That(context.EvaluateScript("{{`${a}\\\\${b}`}}"), Is.EqualTo("1\\2"));
        }
    }
}