using System.Reflection;
using Funq;
using NUnit.Framework;
using ServiceStack.Templates;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateLiteralTests
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
            var context = new TemplateContext().Init();
            
            Assert.That(context.EvaluateTemplate(@"{{ `\\` }}"), 
                Is.EqualTo(@"\"));
            
            Assert.That(context.EvaluateTemplate(@"{{ `\\ \n` }}"), 
                Is.EqualTo("\\ \n"));
            
            Assert.That(context.EvaluateTemplate(@"{{ `""#key"".replace(/\\s+/g,'')` | raw }}"), 
                Is.EqualTo(@"""#key"".replace(/\s+/g,'')"));
        }

        [Test]
        public void Does_evaluate_TemplateLiteral()
        {
            var context = new TemplateContext {
                Args = {
                    ["a"] = 1,
                    ["b"] = 2,
                    ["c"] = 3,
                    ["d"] = 4,
                }
            }.Init();
            
            Assert.That(context.EvaluateTemplate("{{``}}"), Is.EqualTo(""));
            Assert.That(context.EvaluateTemplate("{{`a`}}"), Is.EqualTo("a"));
            Assert.That(context.EvaluateTemplate("{{`a${b}`}}"), Is.EqualTo("a2"));
            Assert.That(context.EvaluateTemplate("{{`a ${b} c`}}"), Is.EqualTo("a 2 c"));
            Assert.That(context.EvaluateTemplate("{{`a ${b + 1} c ${incr(d + 1)}`}}"), Is.EqualTo("a 3 c 6"));
            Assert.That(context.EvaluateTemplate("{{`\n`}}"), Is.EqualTo("\n"));
            Assert.That(context.EvaluateTemplate("{{`a\n${b}`}}"), Is.EqualTo("a\n2"));
            Assert.That(context.EvaluateTemplate("{{`\"\"` | raw}}"), Is.EqualTo("\"\""));
            Assert.That(context.EvaluateTemplate("{{`''` | raw}}"), Is.EqualTo("''"));
            Assert.That(context.EvaluateTemplate("{{`a\"b\"c` | raw}}"), Is.EqualTo("a\"b\"c"));
            Assert.That(context.EvaluateTemplate("{{`a'b'c` | raw}}"), Is.EqualTo("a'b'c"));

            Assert.That(context.EvaluateTemplate("{{`a\"b\"c` | appendTo: a}}{{ a | raw }}"), Is.EqualTo("a\"b\"c"));
        }
    }
}