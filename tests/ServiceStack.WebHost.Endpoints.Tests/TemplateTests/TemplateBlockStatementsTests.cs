using System.Linq;
using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateBlockStatementsTests
    {
        [Test]
        public void Does_parse_template_with_Block_Statement()
        {
            var fragments = TemplatePageUtils.ParseTemplatePage("BEFORE {{#bold}} Hi, {{name}}! {{/bold}} AFTER");
            
            Assert.That(fragments.Count, Is.EqualTo(3));
            Assert.That(((PageStringFragment)fragments[0]).Value, Is.EqualTo("BEFORE ".ToStringSegment()));
            
            var statement = fragments[1] as PageStatementFragment;
            Assert.That(statement, Is.Not.Null);
            Assert.That(statement.Name, Is.EqualTo("bold".ToStringSegment()));
            Assert.That(statement.Expression, Is.EqualTo("".ToStringSegment()));
            
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
            
            var statement = fragments[1] as PageStatementFragment;
            Assert.That(statement, Is.Not.Null);
            Assert.That(statement.Name, Is.EqualTo("if".ToStringSegment()));
            Assert.That(statement.Expression, Is.EqualTo("a < b".ToStringSegment()));            
            Assert.That(((PageStringFragment)statement.Body[0]).Value, Is.EqualTo("YES".ToStringSegment()));

            Assert.That(statement.ElseStatements[0].Expression, Is.EqualTo("".ToStringSegment()));            
            Assert.That(((PageStringFragment)statement.ElseStatements[0].Body[0]).Value, Is.EqualTo("NO".ToStringSegment()));            
            
            Assert.That(((PageStringFragment)fragments[2]).Value, Is.EqualTo(" AFTER".ToStringSegment()));
        }

        [Test]
        public void Does_parse_template_with_if_and_else_if_statement()
        {
            var fragments = TemplatePageUtils.ParseTemplatePage("BEFORE {{#if a < b}}YES{{else if c < d}}NO{{else}}MAYBE{{/if}} AFTER");
            
            Assert.That(fragments.Count, Is.EqualTo(3));
            Assert.That(((PageStringFragment)fragments[0]).Value, Is.EqualTo("BEFORE ".ToStringSegment()));
            
            var statement = fragments[1] as PageStatementFragment;
            Assert.That(statement, Is.Not.Null);
            Assert.That(statement.Name, Is.EqualTo("if".ToStringSegment()));
            Assert.That(statement.Expression, Is.EqualTo("a < b".ToStringSegment()));            
            Assert.That(((PageStringFragment)statement.Body[0]).Value, Is.EqualTo("YES".ToStringSegment()));

            Assert.That(statement.ElseStatements[0].Expression, Is.EqualTo("if c < d".ToStringSegment()));            
            Assert.That(((PageStringFragment)statement.ElseStatements[0].Body[0]).Value, Is.EqualTo("NO".ToStringSegment()));            

            Assert.That(statement.ElseStatements[1].Expression, Is.EqualTo("".ToStringSegment()));            
            Assert.That(((PageStringFragment)statement.ElseStatements[1].Body[0]).Value, Is.EqualTo("MAYBE".ToStringSegment()));            
            
            Assert.That(((PageStringFragment)fragments[2]).Value, Is.EqualTo(" AFTER".ToStringSegment()));
        }
        
        [Test]
        public void Does_parse_template_with_nested_Block_Statement()
        {
            var fragments = TemplatePageUtils.ParseTemplatePage("BEFORE {{#bold}} Hi, {{#bold}}{{name}}{{/bold}}! {{/bold}} AFTER");
            
            Assert.That(fragments.Count, Is.EqualTo(3));
            Assert.That(((PageStringFragment)fragments[0]).Value, Is.EqualTo("BEFORE ".ToStringSegment()));
            
            var statement = fragments[1] as PageStatementFragment;
            Assert.That(statement, Is.Not.Null);
            Assert.That(statement.Name, Is.EqualTo("bold".ToStringSegment()));
            Assert.That(statement.Expression, Is.EqualTo("".ToStringSegment()));
            
            Assert.That(((PageStringFragment)statement.Body[0]).Value, Is.EqualTo(" Hi, ".ToStringSegment()));

            var nested = (PageStatementFragment) statement.Body[1];             
            Assert.That(nested.Name, Is.EqualTo("bold".ToStringSegment()));
            Assert.That(((PageVariableFragment)nested.Body[0]).Binding, Is.EqualTo("name".ToStringSegment()));
            
            Assert.That(((PageStringFragment)statement.Body[2]).Value, Is.EqualTo("! ".ToStringSegment()));

            Assert.That(((PageStringFragment)fragments[2]).Value, Is.EqualTo(" AFTER".ToStringSegment()));
        }

    }
}