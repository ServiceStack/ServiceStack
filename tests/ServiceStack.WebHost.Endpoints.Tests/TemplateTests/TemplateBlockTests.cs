using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.IO;
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
            
            public override async Task WriteAsync(TemplateScopeContext scope, PageBlockFragment fragment, CancellationToken cancel)
            {
                await scope.OutputStream.WriteAsync("<b>", cancel);
                await WriteBodyAsync(scope, fragment, cancel);
                await scope.OutputStream.WriteAsync("</b>", cancel);
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

        class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }

            public Person() { }
            public Person(string name, int age)
            {
                Name = name;
                Age = age;
            }
        }

        [Test]
        public void Does_evaluate_template_containing_with_block()
        {
            var context = new TemplateContext {
                Args = {
                    ["person"] = new Person { Name = "poco", Age = 27 },
                    ["personMap"] = new Dictionary<string, object> {
                        ["name"] = "map",
                        ["age"] = 27,
                    } 
                }
            }.Init();
            
            Assert.That(context.EvaluateTemplate("Person {{#with person}}{{Name}} is {{Age}} years old{{/with}}"), 
                Is.EqualTo("Person poco is 27 years old"));
            
            Assert.That(context.EvaluateTemplate("Person {{#with personMap}}{{name}} is {{age}} years old{{/with}}"), 
                Is.EqualTo("Person map is 27 years old"));
            
            Assert.That(context.EvaluateTemplate("Person {{#with {name:'inline',age:27} }}{{name}} is {{age}} years old{{/with}}"), 
                Is.EqualTo("Person inline is 27 years old"));
        }

        [Test]
        public void Does_evaluate_template_containing_with_and_else_block()
        {
            var context = new TemplateContext {
                Args = {
                    ["person"] = null,
                    ["personMap"] = new Dictionary<string, object> {
                        ["name"] = "map",
                        ["age"] = 27,
                    } 
                }
            }.Init();
            
            Assert.That(context.EvaluateTemplate("Person {{#with person}}{{Name}} is {{Age}} years old{{else}}does not exist{{/with}}"), 
                Is.EqualTo("Person does not exist"));
            Assert.That(context.EvaluateTemplate("Person {{#with null}}{{Name}} is {{Age}} years old{{else}}does not exist{{/with}}"), 
                Is.EqualTo("Person does not exist"));
            Assert.That(context.EvaluateTemplate("Person {{#with person}}{{Name}} is {{Age}} years old{{else if personMap != null}}map does exist{{else}}does not exist{{/with}}"), 
                Is.EqualTo("Person map does exist"));
        }

        [Test]
        public void Does_evaluate_template_with_each_blocks()
        {
            var context = new TemplateContext {
                Args = {
                    ["numbers"] = new[]{ 1, 2, 3 },
                    ["letters"] = new[]{ "A", "B", "C" },
                }
            }.Init();
            
            Assert.That(context.EvaluateTemplate("{{#each numbers}}{{it}} {{/each}}"), Is.EqualTo("1 2 3 "));
            
            Assert.That(context.EvaluateTemplate("{{#each letters}}{{it}} {{/each}}"), Is.EqualTo("A B C "));
            
            Assert.That(context.EvaluateTemplate("{{#each numbers}}{{#if isNumber(it)}}number {{it}} {{else}}letter {{it}} {{/if}}{{/each}}"), 
                Is.EqualTo("number 1 number 2 number 3 "));
            
            Assert.That(context.EvaluateTemplate("{{#each letters}}{{#if isNumber(it)}}number {{it}} {{else}}letter {{it}} {{/if}}{{/each}}"), 
                Is.EqualTo("letter A letter B letter C "));
        }

        [Test]
        public void Does_evaluate_template_with_each_else_blocks()
        {
            var context = new TemplateContext {
                Args = {
                    ["numbers"] = new int[]{},
                    ["letters"] = new[]{ "A", "B", "C" },
                    ["people"] = new[]{ new Person("name1", 1),new Person("name2", 2),new Person("name3", 3) },
                }
            }.Init();
            
            Assert.That(context.EvaluateTemplate("{{#each numbers}}{{it}} {{else}}no numbers{{/each}}"), 
                Is.EqualTo("no numbers"));
            Assert.That(context.EvaluateTemplate("{{#each numbers}}{{it}} {{else if !isEmpty(letters)}}has letters{{else}}no numbers{{/each}}"), 
                Is.EqualTo("has letters"));
            Assert.That(context.EvaluateTemplate("{{#each numbers}}{{it}} {{else if !isEmpty([])}}has letters{{else}}no numbers{{/each}}"), 
                Is.EqualTo("no numbers"));
        }

        [Test]
        public void Template_each_blocks_without_in_explodes_ref_type_arguments_into_scope()
        {
            var context = new TemplateContext {
                Args = {
                    ["people"] = new[]{ new Person("name1", 1),new Person("name2", 2),new Person("name3", 3) },
                }
            }.Init();
            
            Assert.That(context.EvaluateTemplate("{{#each people}}({{Name}},{{Age}}) {{/each}}"), 
                Is.EqualTo("(name1,1) (name2,2) (name3,3) "));
        }

        [Test]
        public void Does_evaluate_template_with_each_in_blocks()
        {
            var context = new TemplateContext {
                Args = {
                    ["numbers"] = new[]{ 1, 2, 3 },
                    ["letters"] = new[]{ "A", "B", "C" },
                }
            }.Init();
            
            Assert.That(context.EvaluateTemplate("{{#each num in numbers}}{{num}} {{/each}}"), Is.EqualTo("1 2 3 "));
            Assert.That(context.EvaluateTemplate("{{#each num in [1,2,3] }}{{num}} {{/each}}"), Is.EqualTo("1 2 3 "));
            
            Assert.That(context.EvaluateTemplate("{{#each c in letters}}{{c}} {{/each}}"), Is.EqualTo("A B C "));
            Assert.That(context.EvaluateTemplate("{{#each c in ['A','B','C'] }}{{c}} {{/each}}"), Is.EqualTo("A B C "));
            
            Assert.That(context.EvaluateTemplate("{{#each num in numbers}}{{#if isNumber(num)}}number {{num}} {{else}}letter {{num}} {{/if}}{{/each}}"), 
                Is.EqualTo("number 1 number 2 number 3 "));
            
            Assert.That(context.EvaluateTemplate("{{#each c in letters}}{{#if isNumber(c)}}number {{c}} {{else}}letter {{c}} {{/if}}{{/each}}"), 
                Is.EqualTo("letter A letter B letter C "));
        }

        [Test]
        public void Does_evaluate_template_with_partial_block()
        {
            var context = new TemplateContext().Init();
            
            context.VirtualFiles.WriteFile("_layout.html", @"
{{ 'from layout' | assignTo: layoutArg }}
I am a Layout with page
{{ page }}");
            
            context.VirtualFiles.WriteFile("page.html", @"
{{#partial my_partial}}
I am a partial called with the scoped argument <b>{{ arg }}</b>
Who can also access other arguments in scope <b>{{ layoutArg }}</b>
{{/partial}}

I am a Page with a partial
{{ 'my_partial' | partial({ arg: 'from page' }) }}".TrimStart());

            var pageResult = new PageResult(context.GetPage("page"));

            var result = pageResult.Result;
            
            result.Print();
            
            Assert.That(result.Trim(), Is.EqualTo(@"I am a Layout with page

I am a Page with a partial
I am a partial called with the scoped argument <b>from page</b>
Who can also access other arguments in scope <b>from layout</b>"));
        }

        [Test]
        public void Does_evaluate_template_with_partial_block_and_args()
        {
            var context = new TemplateContext().Init();
            
            context.VirtualFiles.WriteFile("_layout.html", @"
{{ 'from layout' | assignTo: layoutArg }}
I am a Layout with page
{{ page }}");
            
            context.VirtualFiles.WriteFile("page.html", @"
{{#partial my_partial {partialArg: 'from partial'} }}
I am a partial called with the scoped argument <b>{{ arg }}</b> and <b>{{ partialArg }}</b>
Who can also access other arguments in scope <b>{{ layoutArg }}</b>
{{/partial}}

{{ 'from page' | assignTo: partialArg }}
I am a Page with a partial
{{ 'my_partial' | partial({ arg: 'from page' }) }}
partialArg in page scope is <b>{{ partialArg }}</b>".TrimStart());

            var pageResult = new PageResult(context.GetPage("page"));

            var result = pageResult.Result;
            
            result.Print();
            
            Assert.That(result.Trim(), Is.EqualTo(@"I am a Layout with page

I am a Page with a partial
I am a partial called with the scoped argument <b>from page</b> and <b>from partial</b>
Who can also access other arguments in scope <b>from layout</b>

partialArg in page scope is <b>from page</b>"));
        }

        [Test]
        public void Does_evaluate_template_with_noop_block()
        {
            var context = new TemplateContext().Init();
            
            Assert.That(context.EvaluateTemplate("Remove {{#noop}} from{{/noop}}view"), Is.EqualTo("Remove view"));
        }
    }
}