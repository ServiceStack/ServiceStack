using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateBlockTests
    {
        [Test]
        public void Does_parse_template_with_Block_Statement()
        {
            var fragments = TemplatePageUtils.ParseTemplatePage("BEFORE {{#bold}} Hi, {{name}}! {{/bold}} AFTER");
            
            Assert.That(fragments.Count, Is.EqualTo(3));
            Assert.That(((PageStringFragment)fragments[0]).Value, Is.EqualTo("BEFORE ".ToStringSegment()));
            
            var statement = fragments[1] as PageBlockFragment;
            Assert.That(statement, Is.Not.Null);
            Assert.That(statement.Name, Is.EqualTo("bold".ToStringSegment()));
            Assert.That(statement.Argument, Is.EqualTo("".ToStringSegment()));
            
            Assert.That(((PageStringFragment)statement.Body[0]).Value, Is.EqualTo(" Hi, ".ToStringSegment()));
            Assert.That(((PageVariableFragment)statement.Body[1]).Binding, Is.EqualTo("name".ToStringSegment()));
            Assert.That(((PageStringFragment)statement.Body[2]).Value, Is.EqualTo("! ".ToStringSegment()));

            Assert.That(((PageStringFragment)fragments[2]).Value, Is.EqualTo(" AFTER".ToStringSegment()));
        }

        [Test]
        public void Does_parse_template_with_if_else_statement()
        {
            var fragments = TemplatePageUtils.ParseTemplatePage("BEFORE {{#if a < b}}YES{{else}}NO{{/if}} AFTER");
            
            Assert.That(fragments.Count, Is.EqualTo(3));
            Assert.That(((PageStringFragment)fragments[0]).Value, Is.EqualTo("BEFORE ".ToStringSegment()));
            
            var statement = fragments[1] as PageBlockFragment;
            Assert.That(statement, Is.Not.Null);
            Assert.That(statement.Name, Is.EqualTo("if".ToStringSegment()));
            Assert.That(statement.Argument, Is.EqualTo("a < b".ToStringSegment()));            
            Assert.That(((PageStringFragment)statement.Body[0]).Value, Is.EqualTo("YES".ToStringSegment()));

            Assert.That(statement.ElseBlocks[0].Argument, Is.EqualTo("".ToStringSegment()));            
            Assert.That(((PageStringFragment)statement.ElseBlocks[0].Body[0]).Value, Is.EqualTo("NO".ToStringSegment()));            
            
            Assert.That(((PageStringFragment)fragments[2]).Value, Is.EqualTo(" AFTER".ToStringSegment()));
        }

        [Test]
        public void Does_parse_template_with_if_and_else_if_statement()
        {
            var fragments = TemplatePageUtils.ParseTemplatePage("BEFORE {{#if a < b}}YES{{else if c < d}}NO{{else}}MAYBE{{/if}} AFTER");
            
            Assert.That(fragments.Count, Is.EqualTo(3));
            Assert.That(((PageStringFragment)fragments[0]).Value, Is.EqualTo("BEFORE ".ToStringSegment()));
            
            var statement = fragments[1] as PageBlockFragment;
            Assert.That(statement, Is.Not.Null);
            Assert.That(statement.Name, Is.EqualTo("if".ToStringSegment()));
            Assert.That(statement.Argument, Is.EqualTo("a < b".ToStringSegment()));            
            Assert.That(((PageStringFragment)statement.Body[0]).Value, Is.EqualTo("YES".ToStringSegment()));

            Assert.That(statement.ElseBlocks[0].Argument, Is.EqualTo("if c < d".ToStringSegment()));            
            Assert.That(((PageStringFragment)statement.ElseBlocks[0].Body[0]).Value, Is.EqualTo("NO".ToStringSegment()));            

            Assert.That(statement.ElseBlocks[1].Argument, Is.EqualTo("".ToStringSegment()));            
            Assert.That(((PageStringFragment)statement.ElseBlocks[1].Body[0]).Value, Is.EqualTo("MAYBE".ToStringSegment()));            
            
            Assert.That(((PageStringFragment)fragments[2]).Value, Is.EqualTo(" AFTER".ToStringSegment()));
        }
        
        [Test]
        public void Does_parse_template_with_nested_Block_Statement()
        {
            var fragments = TemplatePageUtils.ParseTemplatePage("BEFORE {{#bold}} Hi, {{#bold}}{{name}}{{/bold}}! {{/bold}} AFTER");
            
            Assert.That(fragments.Count, Is.EqualTo(3));
            Assert.That(((PageStringFragment)fragments[0]).Value, Is.EqualTo("BEFORE ".ToStringSegment()));
            
            var statement = fragments[1] as PageBlockFragment;
            Assert.That(statement, Is.Not.Null);
            Assert.That(statement.Name, Is.EqualTo("bold".ToStringSegment()));
            Assert.That(statement.Argument, Is.EqualTo("".ToStringSegment()));
            
            Assert.That(((PageStringFragment)statement.Body[0]).Value, Is.EqualTo(" Hi, ".ToStringSegment()));

            var nested = (PageBlockFragment) statement.Body[1];             
            Assert.That(nested.Name, Is.EqualTo("bold".ToStringSegment()));
            Assert.That(((PageVariableFragment)nested.Body[0]).Binding, Is.EqualTo("name".ToStringSegment()));
            
            Assert.That(((PageStringFragment)statement.Body[2]).Value, Is.EqualTo("! ".ToStringSegment()));

            Assert.That(((PageStringFragment)fragments[2]).Value, Is.EqualTo(" AFTER".ToStringSegment()));
        }
        
        public class TemplateBoldBlock : TemplateBlock
        {
            public override string Name => "bold";
            
            public override async Task WriteAsync(TemplateScopeContext scope, PageBlockFragment fragment, CancellationToken token)
            {
                await scope.OutputStream.WriteAsync("<b>", token);
                await WriteBodyAsync(scope, fragment, token);
                await scope.OutputStream.WriteAsync("</b>", token);
            }
        }

        [Test]
        public void Does_evaluate_custom_Block_Statement()
        {
            var context = new TemplateContext {
                TemplateBlocks = { new TemplateBoldBlock() },
                Args = {
                    ["name"] = "World"
                }
            }.Init();

            var result = context.EvaluateTemplate("BEFORE {{#bold}} Hi, {{name}}! {{/bold}} AFTER");
            Assert.That(result, Is.EqualTo("BEFORE <b> Hi, World! </b> AFTER"));
        }
        
        [Test]
        public void Does_evaluate_template_with_nested_Block_Statement()
        {
            var context = new TemplateContext {
                TemplateBlocks = { new TemplateBoldBlock() },
                Args = {
                    ["name"] = "World"
                }
            }.Init();

            var result = context.EvaluateTemplate("BEFORE {{#bold}} Hi, {{#bold}}{{name}}{{/bold}}! {{/bold}} AFTER");
            Assert.That(result, Is.EqualTo("BEFORE <b> Hi, <b>World</b>! </b> AFTER"));

            var template = "BEFORE {{#bold}} Hi, {{#if a == null}}{{#bold}}{{name}}{{/bold}}{{else}}{{a}}{{/if}}! {{/bold}} AFTER";

            result = context.EvaluateTemplate(template);
            Assert.That(result, Is.EqualTo("BEFORE <b> Hi, <b>World</b>! </b> AFTER"));

            context.Args["a"] = "foo";
            result = context.EvaluateTemplate(template);
            Assert.That(result, Is.EqualTo("BEFORE <b> Hi, foo! </b> AFTER"));
        }
        
        [Test]
        public void Does_evaluate_template_with_if_else_statement()
        {
            var context = new TemplateContext {
                Args = {
                    ["a"] = 1,
                    ["b"] = 2,
                }
            }.Init();

            var template = "BEFORE {{#if a < b}}YES{{else}}NO{{/if}} AFTER";
            
            Assert.That(context.EvaluateTemplate(template), Is.EqualTo("BEFORE YES AFTER"));

            context.Args["a"] = 3;
            Assert.That(context.EvaluateTemplate(template), Is.EqualTo("BEFORE NO AFTER"));
        }
 
        [Test]
        public void Does_evaluate_template_with_if_and_else_if_statement()
        {
            var context = new TemplateContext {
                Args = {
                    ["a"] = 1,
                    ["b"] = 2,
                    ["c"] = 3,
                    ["d"] = 4
                }
            }.Init();

            var template = "BEFORE {{#if a < b}}YES{{else if c < d}}NO{{else}}MAYBE{{/if}} AFTER";

            Assert.That(context.EvaluateTemplate(template), Is.EqualTo("BEFORE YES AFTER"));

            context.Args["a"] = 3;
            Assert.That(context.EvaluateTemplate(template), Is.EqualTo("BEFORE NO AFTER"));
 
            context.Args["c"] = 5;
            Assert.That(context.EvaluateTemplate(template), Is.EqualTo("BEFORE MAYBE AFTER"));
        }

    }
}