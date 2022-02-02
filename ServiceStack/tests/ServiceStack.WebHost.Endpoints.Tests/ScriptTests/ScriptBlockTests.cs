using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class ScriptBlockTests
    {
        [Test]
        public void Does_parse_template_with_Block_Statement()
        {
            var fragments = ScriptTemplateUtils.ParseTemplate("BEFORE {{#bold}} Hi, {{name}}! {{/bold}} AFTER");
            
            Assert.That(fragments.Count, Is.EqualTo(3));
            Assert.That(((PageStringFragment)fragments[0]).Value.ToString(), Is.EqualTo("BEFORE "));
            
            var statement = fragments[1] as PageBlockFragment;
            Assert.That(statement, Is.Not.Null);
            Assert.That(statement.Name, Is.EqualTo("bold"));
            Assert.That(statement.Argument.ToString(), Is.EqualTo(""));
            
            Assert.That(((PageStringFragment)statement.Body[0]).Value.ToString(), Is.EqualTo(" Hi, "));
            Assert.That(((PageVariableFragment)statement.Body[1]).Binding, Is.EqualTo("name"));
            Assert.That(((PageStringFragment)statement.Body[2]).Value.ToString(), Is.EqualTo("! "));

            Assert.That(((PageStringFragment)fragments[2]).Value.ToString(), Is.EqualTo(" AFTER"));
        }

        [Test]
        public void Does_parse_template_with_if_else_statement()
        {
            var fragments = ScriptTemplateUtils.ParseTemplate("BEFORE {{#if a < b}}YES{{else}}NO{{/if}} AFTER");
            
            Assert.That(fragments.Count, Is.EqualTo(3));
            Assert.That(((PageStringFragment)fragments[0]).Value.ToString(), Is.EqualTo("BEFORE "));
            
            var statement = fragments[1] as PageBlockFragment;
            Assert.That(statement, Is.Not.Null);
            Assert.That(statement.Name, Is.EqualTo("if"));
            Assert.That(statement.Argument.ToString(), Is.EqualTo("a < b"));            
            Assert.That(((PageStringFragment)statement.Body[0]).Value.ToString(), Is.EqualTo("YES"));

            Assert.That(statement.ElseBlocks[0].Argument.ToString(), Is.EqualTo(""));            
            Assert.That(((PageStringFragment)statement.ElseBlocks[0].Body[0]).Value.ToString(), Is.EqualTo("NO"));            
            
            Assert.That(((PageStringFragment)fragments[2]).Value.ToString(), Is.EqualTo(" AFTER"));
        }

        [Test]
        public void Does_parse_template_with_if_and_else_if_statement()
        {
            var fragments = ScriptTemplateUtils.ParseTemplate("BEFORE {{#if a < b}}YES{{else if c < d}}NO{{else}}MAYBE{{/if}} AFTER");
            
            Assert.That(fragments.Count, Is.EqualTo(3));
            Assert.That(((PageStringFragment)fragments[0]).Value.ToString(), Is.EqualTo("BEFORE "));
            
            var statement = fragments[1] as PageBlockFragment;
            Assert.That(statement, Is.Not.Null);
            Assert.That(statement.Name, Is.EqualTo("if"));
            Assert.That(statement.Argument.ToString(), Is.EqualTo("a < b"));            
            Assert.That(((PageStringFragment)statement.Body[0]).Value.ToString(), Is.EqualTo("YES"));

            Assert.That(statement.ElseBlocks[0].Argument.ToString(), Is.EqualTo("if c < d"));            
            Assert.That(((PageStringFragment)statement.ElseBlocks[0].Body[0]).Value.ToString(), Is.EqualTo("NO"));            

            Assert.That(statement.ElseBlocks[1].Argument.ToString(), Is.EqualTo(""));            
            Assert.That(((PageStringFragment)statement.ElseBlocks[1].Body[0]).Value.ToString(), Is.EqualTo("MAYBE"));            
            
            Assert.That(((PageStringFragment)fragments[2]).Value.ToString(), Is.EqualTo(" AFTER"));
        }
        
        [Test]
        public void Does_parse_template_with_nested_Block_Statement()
        {
            var fragments = ScriptTemplateUtils.ParseTemplate("BEFORE {{#bold}} Hi, {{#bold}}{{name}}{{/bold}}! {{/bold}} AFTER");
            
            Assert.That(fragments.Count, Is.EqualTo(3));
            Assert.That(((PageStringFragment)fragments[0]).Value.ToString(), Is.EqualTo("BEFORE "));
            
            var statement = fragments[1] as PageBlockFragment;
            Assert.That(statement, Is.Not.Null);
            Assert.That(statement.Name, Is.EqualTo("bold"));
            Assert.That(statement.Argument.ToString(), Is.EqualTo(""));
            
            Assert.That(((PageStringFragment)statement.Body[0]).Value.ToString(), Is.EqualTo(" Hi, "));

            var nested = (PageBlockFragment) statement.Body[1];             
            Assert.That(nested.Name, Is.EqualTo("bold"));
            Assert.That(((PageVariableFragment)nested.Body[0]).Binding, Is.EqualTo("name"));
            
            Assert.That(((PageStringFragment)statement.Body[2]).Value.ToString(), Is.EqualTo("! "));

            Assert.That(((PageStringFragment)fragments[2]).Value.ToString(), Is.EqualTo(" AFTER"));
        }

        [Test]
        public void Does_parse_Raw_block_body_as_string()
        {
            var fragments = ScriptTemplateUtils.ParseTemplate("BEFORE {{#raw}} Hi, {{ {{ name }} }} {{/raw}} AFTER");
            
            Assert.That(fragments.Count, Is.EqualTo(3));
            Assert.That(((PageStringFragment)fragments[0]).Value.ToString(), Is.EqualTo("BEFORE "));
            
            var statement = fragments[1] as PageBlockFragment;
            Assert.That(statement, Is.Not.Null);
            Assert.That(statement.Name, Is.EqualTo("raw"));
            Assert.That(statement.Argument.ToString(), Is.EqualTo(""));
            Assert.That(statement.Body.Length, Is.EqualTo(1));
            
            Assert.That(((PageStringFragment)statement.Body[0]).Value.ToString(), Is.EqualTo(" Hi, {{ {{ name }} }} "));
            
            Assert.That(((PageStringFragment)fragments[2]).Value.ToString(), Is.EqualTo(" AFTER"));
        }

        [Test]
        public void Does_evaluate_Raw_block_body_as_string()
        {
            var context = new ScriptContext {
                Plugins = { new MarkdownScriptPlugin() }
            }.Init();

            Assert.That(context.EvaluateScript("BEFORE {{#raw}} Hi, {{ {{ name }} }} {{/raw}} AFTER"),
                Is.EqualTo("BEFORE  Hi, {{ {{ name }} }}  AFTER"));

            Assert.That(context.EvaluateScript("BEFORE {{#raw md}}# Heading{{/raw}} AFTER {{ md |> markdown }}").NormalizeNewLines(),
                Is.EqualTo("BEFORE  AFTER <h1>Heading</h1>"));
        }

        [Test]
        public void Does_evaluate_Markdown_block_body_as_string()
        {
            var context = new ScriptContext {
                Plugins = { new MarkdownScriptPlugin() }
            }.Init();

            Assert.That(context.EvaluateScript("BEFORE {{#markdown}}# Heading{{/markdown}} AFTER").RemoveNewLines(),
                Is.EqualTo("BEFORE <h1>Heading</h1> AFTER"));

            Assert.That(context.EvaluateScript("BEFORE {{#markdown md}}# Heading{{/markdown}} AFTER {{ md }}").NormalizeNewLines(),
                Is.EqualTo("BEFORE  AFTER <h1>Heading</h1>"));
        }

        [Test]
        public void Does_evaluate_Raw_block_body_and_appendTo_string()
        {
            var context = new ScriptContext {
                Plugins = { new MarkdownScriptPlugin() }
            }.Init();

            Assert.That(context.EvaluateScript("BEFORE {{#raw appendTo md}}# Heading{{/raw}}{{#raw appendTo md}} Appended{{/raw}} AFTER {{ md |> markdown }}").NormalizeNewLines(),
                Is.EqualTo("BEFORE  AFTER <h1>Heading Appended</h1>"));
        }
        
        public class ScriptBoldBlock : ScriptBlock
        {
            public override string Name => "bold";
            
            public override async Task WriteAsync(ScriptScopeContext scope, PageBlockFragment block, CancellationToken token)
            {
                await scope.OutputStream.WriteAsync("<b>", token);
                await WriteBodyAsync(scope, block, token);
                await scope.OutputStream.WriteAsync("</b>", token);
            }
        }

        [Test]
        public void Does_evaluate_custom_Block_Statement()
        {
            var context = new ScriptContext {
                ScriptBlocks = { new ScriptBoldBlock() },
                Args = {
                    ["name"] = "World"
                }
            }.Init();

            var result = context.EvaluateScript("BEFORE {{#bold}} Hi, {{name}}! {{/bold}} AFTER");
            Assert.That(result, Is.EqualTo("BEFORE <b> Hi, World! </b> AFTER"));
        }
        
        [Test]
        public void Does_evaluate_template_with_nested_Block_Statement()
        {
            var context = new ScriptContext {
                ScriptBlocks = { new ScriptBoldBlock() },
                Args = {
                    ["name"] = "World"
                }
            }.Init();

            var result = context.EvaluateScript("BEFORE {{#bold}} Hi, {{#bold}}{{name}}{{/bold}}! {{/bold}} AFTER");
            Assert.That(result, Is.EqualTo("BEFORE <b> Hi, <b>World</b>! </b> AFTER"));

            var template = "BEFORE {{#bold}} Hi, {{#if a == null}}{{#bold}}{{name}}{{/bold}}{{else}}{{a}}{{/if}}! {{/bold}} AFTER";

            result = context.EvaluateScript(template);
            Assert.That(result, Is.EqualTo("BEFORE <b> Hi, <b>World</b>! </b> AFTER"));

            context.Args["a"] = "foo";
            result = context.EvaluateScript(template);
            Assert.That(result, Is.EqualTo("BEFORE <b> Hi, foo! </b> AFTER"));
        }
        
        [Test]
        public void Does_evaluate_template_with_if_else_statement()
        {
            var context = new ScriptContext {
                Args = {
                    ["a"] = 1,
                    ["b"] = 2,
                }
            }.Init();

            var template = "BEFORE {{#if a < b}}YES{{else}}NO{{/if}} AFTER";
            
            Assert.That(context.EvaluateScript(template), Is.EqualTo("BEFORE YES AFTER"));

            context.Args["a"] = 3;
            Assert.That(context.EvaluateScript(template), Is.EqualTo("BEFORE NO AFTER"));
        }
 
        [Test]
        public void Does_evaluate_template_with_if_and_else_if_statement()
        {
            var context = new ScriptContext {
                Args = {
                    ["a"] = 1,
                    ["b"] = 2,
                    ["c"] = 3,
                    ["d"] = 4
                }
            }.Init();

            var template = "BEFORE {{#if a < b}}YES{{else if c < d}}NO{{else}}MAYBE{{/if}} AFTER";

            Assert.That(context.EvaluateScript(template), Is.EqualTo("BEFORE YES AFTER"));

            context.Args["a"] = 3;
            Assert.That(context.EvaluateScript(template), Is.EqualTo("BEFORE NO AFTER"));
 
            context.Args["c"] = 5;
            Assert.That(context.EvaluateScript(template), Is.EqualTo("BEFORE MAYBE AFTER"));
        }

        [Test]
        public void Does_evaluate_template_containing_with_block()
        {
            var context = new ScriptContext {
                Args = {
                    ["person"] = new Person { Name = "poco", Age = 27 },
                    ["personMap"] = new Dictionary<string, object> {
                        ["name"] = "map",
                        ["age"] = 27,
                    } 
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("Person {{#with person}}{{Name}} is {{Age}} years old{{/with}}"), 
                Is.EqualTo("Person poco is 27 years old"));
            
            Assert.That(context.EvaluateScript("Person {{#with personMap}}{{name}} is {{age}} years old{{/with}}"), 
                Is.EqualTo("Person map is 27 years old"));
            
            Assert.That(context.EvaluateScript("Person {{#with {name:'inline',age:27} }}{{name}} is {{age}} years old{{/with}}"), 
                Is.EqualTo("Person inline is 27 years old"));
        }

        [Test]
        public void Does_evaluate_template_containing_with_and_else_block()
        {
            var context = new ScriptContext {
                Args = {
                    ["person"] = null,
                    ["personMap"] = new Dictionary<string, object> {
                        ["name"] = "map",
                        ["age"] = 27,
                    } 
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("Person {{#with person}}{{Name}} is {{Age}} years old{{else}}does not exist{{/with}}"), 
                Is.EqualTo("Person does not exist"));
            Assert.That(context.EvaluateScript("Person {{#with null}}{{Name}} is {{Age}} years old{{else}}does not exist{{/with}}"), 
                Is.EqualTo("Person does not exist"));
            Assert.That(context.EvaluateScript("Person {{#with person}}{{Name}} is {{Age}} years old{{else if personMap != null}}map does exist{{else}}does not exist{{/with}}"), 
                Is.EqualTo("Person map does exist"));
        }

        public class ScriptSimpleEachBlock : ScriptBlock
        {
            public override string Name => "each";
    
            public override async Task WriteAsync(
                ScriptScopeContext scope, PageBlockFragment block, CancellationToken token)
            {
                var collection = (IEnumerable) block.Argument.GetJsExpressionAndEvaluate(scope,
                    ifNone: () => throw new NotSupportedException("'each' block does not have a valid expression"));

                var index = 0;
                if (collection != null)
                {
                    foreach (var element in collection)
                    {
                        var scopeArgs = element.ToObjectDictionary();
                        scopeArgs["it"] = element;
                        scopeArgs[nameof(index)] = index++;
                        
                        var itemScope = scope.ScopeWithParams(scopeArgs);
                        await WriteBodyAsync(itemScope, block, token);
                    }
                }
                
                if (index == 0)
                {
                    await WriteElseAsync(scope, block.ElseBlocks, token);
                }
            }
        }
        
        [Test]
        public void Does_evaluate_template_with_simple_each_blocks()
        {
            var context = new ScriptContext {
                Args = {
                    ["numbers"] = new[]{ 1, 2, 3 },
                    ["letters"] = new[]{ "A", "B", "C" },
                    ["empty"] = new int[]{},
                },
                ScriptBlocks = {
                    new ScriptSimpleEachBlock(),
                    new IfScriptBlock(),
                }
            }
            .RemovePlugins(x => x is DefaultScriptBlocks)
            .Init();
            
            Assert.That(context.EvaluateScript("{{#each numbers}}{{it}} {{/each}}"), Is.EqualTo("1 2 3 "));
            
            Assert.That(context.EvaluateScript("{{#each letters}}{{it}} {{/each}}"), Is.EqualTo("A B C "));
            
            Assert.That(context.EvaluateScript("{{#each numbers}}{{#if isNumber(it)}}number {{it}} {{else}}letter {{it}} {{/if}}{{/each}}"), 
                Is.EqualTo("number 1 number 2 number 3 "));
            
            Assert.That(context.EvaluateScript("{{#each letters}}{{#if isNumber(it)}}number {{it}} {{else}}letter {{it}} {{/if}}{{/each}}"), 
                Is.EqualTo("letter A letter B letter C "));

            Assert.That(context.EvaluateScript("{{#each empty}}{{it}}{{else}}none{{/each}}"), Is.EqualTo("none"));
        }

        [Test]
        public void Does_evaluate_template_with_each_blocks()
        {
            var context = new ScriptContext {
                Args = {
                    ["numbers"] = new[]{ 1, 2, 3 },
                    ["letters"] = new[]{ "A", "B", "C" },
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("{{#each numbers}}{{it}} {{/each}}"), Is.EqualTo("1 2 3 "));
            
            Assert.That(context.EvaluateScript("{{#each letters}}{{it}} {{/each}}"), Is.EqualTo("A B C "));
            
            Assert.That(context.EvaluateScript("{{#each numbers}}{{#if isNumber(it)}}number {{it}} {{else}}letter {{it}} {{/if}}{{/each}}"), 
                Is.EqualTo("number 1 number 2 number 3 "));
            
            Assert.That(context.EvaluateScript("{{#each letters}}{{#if isNumber(it)}}number {{it}} {{else}}letter {{it}} {{/if}}{{/each}}"), 
                Is.EqualTo("letter A letter B letter C "));
        }

        [Test]
        public void Does_evaluate_template_with_each_else_blocks()
        {
            var context = new ScriptContext {
                Args = {
                    ["numbers"] = new int[]{},
                    ["letters"] = new[]{ "A", "B", "C" },
                    ["people"] = new[]{ new Person("name1", 1),new Person("name2", 2),new Person("name3", 3) },
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("{{#each numbers}}{{it}} {{else}}no numbers{{/each}}"), 
                Is.EqualTo("no numbers"));
            Assert.That(context.EvaluateScript("{{#each numbers}}{{it}} {{else if !isEmpty(letters)}}has letters{{else}}no numbers{{/each}}"), 
                Is.EqualTo("has letters"));
            Assert.That(context.EvaluateScript("{{#each numbers}}{{it}} {{else if !isEmpty([])}}has letters{{else}}no numbers{{/each}}"), 
                Is.EqualTo("no numbers"));
        }

        [Test]
        public void Does_evaluate_template_with_each_where_blocks()
        {
            var context = new ScriptContext {
                Args = {
                    ["numbers"] = new[]{ 1, 2, 3, 4, 5, },
                    ["letters"] = new[]{ "A", "B", "C", "D", "E" },
                    ["people"] = new[]{ new Person("name1", 1),new Person("name2", 2),new Person("name3", 3) },
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("{{#each numbers where isOdd(it)}}#{{index}} {{it}}, {{/each}}"), 
                Is.EqualTo("#0 1, #1 3, #2 5, "));
            Assert.That(context.EvaluateScript("{{#each num in numbers where isOdd(num)}}#{{index}} {{num}}, {{/each}}"), 
                Is.EqualTo("#0 1, #1 3, #2 5, "));
            Assert.That(context.EvaluateScript("{{#each people where Name == 'name2' and Age == 2 }}#{{index}} {{Name}}, {{Age}}{{/each}}"), 
                Is.EqualTo("#0 name2, 2"));
            Assert.That(context.EvaluateScript("{{#each p in people where p.Name == 'name2' }}#{{index}} {{p.Name}}, {{p.Age}}{{/each}}"), 
                Is.EqualTo("#0 name2, 2"));
        }

        [Test]
        public void Does_evaluate_template_with_each_blocks_containing_LINQ_expressions()
        {
            var context = new ScriptContext {
                Args = {
                    ["numbers"] = new[]{ 4, 5, 1, 3, 2, },
                    ["letters"] = new[]{ "C", "D", "B", "E", "A" },
                    ["people"] = new[]{ new Person("name3", 3),new Person("name2", 4),new Person("name1", 5),new Person("name5", 1),new Person("name4", 2) },
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("{{#each numbers orderby it descending}}#{{index}} {{it}}, {{/each}}"), 
                Is.EqualTo("#0 5, #1 4, #2 3, #3 2, #4 1, "));
            Assert.That(context.EvaluateScript("{{#each numbers where isOdd(it) orderby it descending}}#{{index}} {{it}}, {{/each}}"), 
                Is.EqualTo("#0 5, #1 3, #2 1, "));
            Assert.That(context.EvaluateScript("{{#each numbers orderby it}}#{{index}} {{it}}, {{/each}}"), 
                Is.EqualTo("#0 1, #1 2, #2 3, #3 4, #4 5, "));
            Assert.That(context.EvaluateScript("{{#each numbers where isOdd(it) orderby it}}#{{index}} {{it}}, {{/each}}"), 
                Is.EqualTo("#0 1, #1 3, #2 5, "));
            Assert.That(context.EvaluateScript("{{#each n in numbers orderby n}}#{{index}} {{n}}, {{/each}}"), 
                Is.EqualTo("#0 1, #1 2, #2 3, #3 4, #4 5, "));
            Assert.That(context.EvaluateScript("{{#each n in numbers where isOdd(n) orderby n}}#{{index}} {{n}}, {{/each}}"), 
                Is.EqualTo("#0 1, #1 3, #2 5, "));
            Assert.That(context.EvaluateScript("{{#each numbers where it % 2 == 1 orderby it skip 1}}#{{index}} {{it}}, {{/each}}"), 
                Is.EqualTo("#0 3, #1 5, "));
            Assert.That(context.EvaluateScript("{{#each numbers where it % 2 == 1 orderby it take 2}}#{{index}} {{it}}, {{/each}}"), 
                Is.EqualTo("#0 1, #1 3, "));
            Assert.That(context.EvaluateScript("{{#each numbers where it % 2 == 1 orderby it skip 1 take 1}}#{{index}} {{it}}, {{/each}}"), 
                Is.EqualTo("#0 3, "));

            Assert.That(context.EvaluateScript("{{#each letters orderby it}}#{{index}} {{it}}, {{/each}}"), 
                Is.EqualTo("#0 A, #1 B, #2 C, #3 D, #4 E, "));
            Assert.That(context.EvaluateScript("{{#each letters where it > 'A' orderby it skip 1 take 2}}#{{index}} {{it}}, {{/each}}"), 
                Is.EqualTo("#0 C, #1 D, "));
            Assert.That(context.EvaluateScript("{{#each letters where index > 0 orderby it skip 1 take 2}}#{{index}} {{it}}, {{/each}}"), 
                Is.EqualTo("#0 B, #1 D, "));
            
            Assert.That(context.EvaluateScript("{{#each people where Name > 'name2' orderby Age take 2}}#{{index}} {{Name}}, {{Age}} {{/each}}"), 
                Is.EqualTo("#0 name5, 1 #1 name4, 2 "));
            Assert.That(context.EvaluateScript("{{#each p in people where p.Name > 'name2' orderby p.Age take 2}}#{{index}} {{p.Name}}, {{p.Age}} {{/each}}"), 
                Is.EqualTo("#0 name5, 1 #1 name4, 2 "));
            Assert.That(context.EvaluateScript("{{#each p in people where p.Name > 'name2' orderby p.Age descending skip 1 take 2}}#{{index}} {{p.Name}}, {{p.Age}} {{/each}}"), 
                Is.EqualTo("#0 name4, 2 #1 name5, 1 "));
        }

        [Test]
        public void Template_each_blocks_without_in_explodes_ref_type_arguments_into_scope()
        {
            var context = new ScriptContext {
                Args = {
                    ["people"] = new[]{ new Person("name1", 1),new Person("name2", 2),new Person("name3", 3) },
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("{{#each people}}({{Name}},{{Age}}) {{/each}}"), 
                Is.EqualTo("(name1,1) (name2,2) (name3,3) "));
        }

        [Test]
        public void Does_evaluate_template_with_each_in_blocks()
        {
            var context = new ScriptContext {
                Args = {
                    ["numbers"] = new[]{ 1, 2, 3 },
                    ["letters"] = new[]{ "A", "B", "C" },
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("{{#each num in numbers}}{{num}} {{/each}}"), Is.EqualTo("1 2 3 "));
            Assert.That(context.EvaluateScript("{{#each num in [1,2,3] }}{{num}} {{/each}}"), Is.EqualTo("1 2 3 "));
            
            Assert.That(context.EvaluateScript("{{#each c in letters}}{{c}} {{/each}}"), Is.EqualTo("A B C "));
            Assert.That(context.EvaluateScript("{{#each c in ['A','B','C'] }}{{c}} {{/each}}"), Is.EqualTo("A B C "));
            
            Assert.That(context.EvaluateScript("{{#each num in numbers}}{{#if isNumber(num)}}number {{num}} {{else}}letter {{num}} {{/if}}{{/each}}"), 
                Is.EqualTo("number 1 number 2 number 3 "));
            
            Assert.That(context.EvaluateScript("{{#each c in letters}}{{#if isNumber(c)}}number {{c}} {{else}}letter {{c}} {{/if}}{{/each}}"), 
                Is.EqualTo("letter A letter B letter C "));
        }

        [Test]
        public void Does_export_scope_args_of_all_KeyValuePairs()
        {
            var context = new ScriptContext {
                Args = {
                    ["numbers"] = new Dictionary<string,int> { ["a"] = 1, ["b"] = 2, ["c"] = 3},
                    ["letters"] = new Dictionary<string,string> { ["a"] = "A", ["b"] = "B", ["c"] = "C"},
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("{{#each numbers}}{{Key}}={{Value}} {{/each}}"), 
                Is.EqualTo("a=1 b=2 c=3 "));
            Assert.That(context.EvaluateScript("{{#each {a:1,b:2,c:3} }}{{Key}}={{Value}} {{/each}}"), 
                Is.EqualTo("a=1 b=2 c=3 "));
            Assert.That(context.EvaluateScript("{{#each letters}}{{Key}}={{Value}} {{/each}}"), 
                Is.EqualTo("a=A b=B c=C "));            
        }

        [Test]
        public void Does_export_Key_name_of_all_KeyValuePairs()
        {
            var context = new ScriptContext {
                Args = {
                    ["posts"] = new List<Dictionary<string, object>> {
                        new Dictionary<string, object> {
                            ["title"] = "title1", 
                            ["content"] = "content1", 
                        },
                        new Dictionary<string, object> {
                            ["title"] = "title2", 
                            ["content"] = "content2", 
                        }
                    },
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("{{#each posts}}{{title}}={{content}}, {{/each}}"), 
                Is.EqualTo("title1=content1, title2=content2, "));
            
            context = new ScriptContext().Init();

            Assert.That(context.EvaluateScript(
                "{{ { title:'title1', content:'content1' } |> addTo: posts}}" +
                "{{ { title:'title2', content:'content2' } |> addTo: posts}}" +
                "{{#each posts}}{{title}}={{content}}, {{/each}}"),
                Is.EqualTo("title1=content1, title2=content2, "));
        }
        
        [Test]
        public void Does_evaluate_template_with_partial_block()
        {
            var context = new ScriptContext().Init();
            
            context.VirtualFiles.WriteFile("_layout.html", @"
{{ 'from layout' |> assignTo: layoutArg }}
I am a Layout with page
{{ page }}");
            
            context.VirtualFiles.WriteFile("page.html", @"
{{#partial my_partial}}
I am a partial called with the scoped argument <b>{{ arg }}</b>
Who can also access other arguments in scope <b>{{ layoutArg }}</b>
{{/partial}}

I am a Page with a partial
{{ 'my_partial' |> partial({ arg: 'from page' }) }}".TrimStart());

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
            var context = new ScriptContext().Init();
            
            context.VirtualFiles.WriteFile("_layout.html", @"
{{ 'from layout' |> assignTo: layoutArg }}
I am a Layout with page
{{ page }}");
            
            context.VirtualFiles.WriteFile("page.html", @"
{{#partial my_partial {partialArg: 'from partial'} }}
I am a partial called with the scoped argument <b>{{ arg }}</b> and <b>{{ partialArg }}</b>
Who can also access other arguments in scope <b>{{ layoutArg }}</b>
{{/partial}}

{{ 'from page' |> assignTo: partialArg }}
I am a Page with a partial
{{ 'my_partial' |> partial({ arg: 'from page' }) }}
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
        public void Can_use_partial_to_evaluate_Markdown()
        {
            var context = new ScriptContext {
                Plugins = { new MarkdownScriptPlugin() }
            }.Init();

            var result = context.EvaluateScript(@"
{{#partial content}}
 - List Item
{{/partial}}

<h1>Heading</h1>

{{ 'content' |> partial |> markdown }}

<footer>2000</footer>");
         
            Assert.That(result.RemoveNewLines(), Is.EqualTo(@"
<h1>Heading</h1>
<ul><li>List Item</li></ul>
<footer>2000</footer>".RemoveNewLines()));
            
        }

        [Test]
        public void Does_evaluate_template_with_noop_block()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript("Remove {{#noop}} from{{/noop}}view"), Is.EqualTo("Remove view"));
        }
 
        public class AsyncResultsFilter : ScriptMethods
        {
            public Task<object> asyncInts(ScriptScopeContext scope)
            {
                return ((object)new object[]{1, 2, 3}).InTask();
            }
            
            public Task<object> asyncDictionary(ScriptScopeContext scope)
            {
                return ((object)new Dictionary<string, object> { {"foo", 1}, {"bar", 2} }).InTask();
            }

            public Task<object> asyncResult(object result) => result.InTask();

            public Task<object> asyncTrue() => ((object) true).InTask();

            public Task<object> asyncFalse() => ((object) false).InTask();

            public Task<object> asyncPerson() => ((object) new Person("foo",1)).InTask();
        }

        [Test]
        public void Does_evaluate_async_results_in_blocks()
        {
            var context = new ScriptContext {
                ScriptMethods = { new AsyncResultsFilter() },
            }.Init();

            Assert.That(context.EvaluateScript("{{#each asyncInts}}{{it}} {{/each}}"), Is.EqualTo("1 2 3 "));
            Assert.That(context.EvaluateScript("{{#each asyncResult([1,2,3])}}{{it}} {{/each}}"), Is.EqualTo("1 2 3 "));
            Assert.That(context.EvaluateScript("{{#each asyncDictionary}}({{Key}},{{Value}}) {{/each}}"), 
                Is.EqualTo("(foo,1) (bar,2) "));
            
            Assert.That(context.EvaluateScript("{{#each []}}{{else if asyncTrue}}does async{{else}}no async{{/each}}"), 
                Is.EqualTo("does async"));
            
            Assert.That(context.EvaluateScript("{{#if asyncTrue}}does async{{else}}no async{{/if}}"), 
                Is.EqualTo("does async"));
            
            Assert.That(context.EvaluateScript("{{#if asyncFalse}}no async{{else}}does async{{/if}}"), 
                Is.EqualTo("does async"));

            Assert.That(context.EvaluateScript("{{#with asyncPerson}}({{Name}},{{Age}}) {{/with}}"), 
                Is.EqualTo("(foo,1) "));
        }

        [Test]
        public void Can_capture_output_with_capture_block()
        {
            var context = new ScriptContext {
                Args = {
                    ["nums"] = new[]{1,2,3}
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("{{#capture output}}{{#each nums}} {{it}}{{/each}}{{/capture}}BEFORE{{output}} AFTER"), 
                Is.EqualTo("BEFORE 1 2 3 AFTER"));
            
            Assert.That(context.EvaluateScript("{{#capture output {nums:[4,5,6] }}{{#each nums}} {{it}}{{/each}}{{/capture}}BEFORE{{output}} AFTER"), 
                Is.EqualTo("BEFORE 4 5 6 AFTER"));
        }

        [Test]
        public void Can_capture_and_appendTo_output_with_capture_block()
        {
            var context = new ScriptContext {
                Args = {
                    ["nums"] = new[]{1,2,3}
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("{{#capture appendTo output}} INIT{{/capture}}{{#capture appendTo output}}{{#each nums}} {{it}}{{/each}}{{/capture}}BEFORE{{output}} AFTER"), 
                Is.EqualTo("BEFORE INIT 1 2 3 AFTER"));
            
            Assert.That(context.EvaluateScript("{{#capture appendTo output}} INIT{{/capture}}{{#capture appendTo output {nums:[4,5,6] }}{{#each nums}} {{it}}{{/each}}{{/capture}}BEFORE{{output}} AFTER"), 
                Is.EqualTo("BEFORE INIT 4 5 6 AFTER"));
        }

        [Test]
        public void Does_evaluate_partial_in_existing_Context()
        {
            var context = new ScriptContext {
                Args = {
                    ["income"] = 1000
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{#partial p {expenses:100} }} {{income ?? 2000}} - {{expenses}} {{/partial}}{{ 'p' |> partial}}"), 
                Is.EqualTo(" 1000 - 100 "));
        }

        [Test]
        public void Does_evaluate_eval_block_in_new_Context_by_default()
        {
            var context = new ScriptContext {
                ScriptBlocks = { new EvalScriptBlock() },
                Args = {
                    ["income"] = 1000,
                    ["incomeExpr"] = "{{income ?? 2000}}",
                    ["expenseExpr"] = "{{expenses}}",
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{#eval {expenses:100} }} {{incomeExpr}} - {{expenses}} {{/eval}}"), 
                Is.EqualTo(" 2000 - 100 "));
            
            Assert.That(context.EvaluateScript("{{ ` ${incomeExpr} - ${expenseExpr} ` |> evalTemplate({expenses:100}) }}"), 
                Is.EqualTo(" 2000 - 100 "));
        }

        [Test]
        public void Does_evaluate_eval_block_in_existing_Context_with_use_context()
        {
            var context = new ScriptContext {
                ScriptBlocks = { new EvalScriptBlock() },
                Args = {
                    ["income"] = 1000,
                    ["incomeExpr"] = "{{income ?? 2000}}",
                    ["expenseExpr"] = "{{expenses}}",
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{#eval {use:{context:true},expenses:100} }} {{incomeExpr}} - {{expenses}} {{/eval}}"), 
                Is.EqualTo(" 1000 - 100 "));
            
            Assert.That(context.EvaluateScript("{{ ` ${incomeExpr} - ${expenseExpr} ` |> evalTemplate({use:{context:true},expenses:100}) }}"), 
                Is.EqualTo(" 1000 - 100 "));
        }

        [Test]
        public void Can_include_plugins_into_new_eval_context()
        {
            var context = new ScriptContext {
                ScriptBlocks = { new EvalScriptBlock() },
                Plugins = { new MarkdownScriptPlugin() },
                Args = {
                    ["evalContent"] = "{{#markdown}}# Heading{{/markdown}}",
                }
            }.Init();

            Assert.Throws<ScriptException>(() => 
                context.EvaluateScript("{{#eval}}{{evalContent}}{{/eval}}"));
            Assert.Throws<ScriptException>(() => 
                context.EvaluateScript("{{ evalContent |> evalTemplate}}"));

            Assert.That(context.EvaluateScript("{{#eval {use:{plugins:'MarkdownScriptPlugin'} }}{{evalContent}}{{/eval}}"), 
                Is.EqualTo("<h1>Heading</h1>\n"));

            Assert.That(context.EvaluateScript("{{ evalContent |> evalTemplate({use:{plugins:'MarkdownScriptPlugin'}}) |> raw }}"), 
                Is.EqualTo("<h1>Heading</h1>\n"));
        }
        
        [Test]
        public void Can_include_filter_into_new_eval_context()
        {
            var context = new ScriptContext {
                ScriptBlocks = { new EvalScriptBlock() },
                ScriptMethods = { new InfoScripts() },
                Args = {
                    ["evalContent"] = "{{envServerUserAgent}}",
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{#eval}}{{evalContent}}{{/eval}}"), 
                Does.Not.Contain("ServiceStack"));
            Assert.That(context.EvaluateScript("{{ evalContent |> evalTemplate}}"), 
                Does.Not.Contain("ServiceStack"));

            Assert.That(context.EvaluateScript("{{#eval {use:{filters:'InfoScripts'}}{{evalContent}}{{/eval}}"), 
                Does.Contain("ServiceStack"));
            
            Assert.That(context.EvaluateScript("{{ evalContent |> evalTemplate({use:{filters:'InfoScripts'}}) }}"), 
                Does.Contain("ServiceStack"));
        }
        
        [Test]
        public void Can_eval_dynamic_content()
        {
            var context = new ScriptContext {
                ScriptBlocks = { new EvalScriptBlock() },
                Args = {
                    ["templates"] = new List<string> {
                        "1. {{income ?? 1000}} - {{expenses}}",
                        "2. {{income ?? 2000}} - {{expenses}}",
                        "3. {{income ?? 3000}} - {{expenses}}",
                    }
                }
            }.Init();
            
            var result = context.EvaluateScript(@"{{#each templates}}{{index}} =>{{#eval {expenses: 100 * index} }} {{it}} {{/eval}}|> {{/each}}");
            Assert.That(result, Is.EqualTo("0 => 1. 1000 - 0 |> 1 => 2. 2000 - 100 |> 2 => 3. 3000 - 200 |> "));
            
            result = context.EvaluateScript(@"{{#each templates}}{{index}} =>{{ ` ${it} ` |> evalTemplate({expenses: 100 * index}) }}|> {{/each}}");
            Assert.That(result, Is.EqualTo("0 => 1. 1000 - 0 |> 1 => 2. 2000 - 100 |> 2 => 3. 3000 - 200 |> "));
        }

        [Test]
        public void Can_use_minifyjs_script_block()
        {
            var context = new ScriptContext {
                Plugins = { new ServiceStackScriptBlocks() },
                DebugMode = false,
            }.Init();

            var output = context.EvaluateScript("{{#minifyjs}}{{/minifyjs}} {{#minifyjs}} \n{{/minifyjs}}");
            Assert.That(output.Trim(), Is.EqualTo(""));
            
            output = context.EvaluateScript("{{#minifyjs}}var a = 1; var b = 2;{{/minifyjs}}");
            Assert.That(output.Trim(), Is.EqualTo("var a=1;var b=2;"));
            
            output = context.EvaluateScript("{{#minifyjs appendTo scripts}}var a = 1; var b = 2;{{/minifyjs}} |> {{#minifyjs appendTo scripts}}function fn ( a ) { }{{/minifyjs}} |> {{scripts}}");
            Assert.That(output.NormalizeNewLines(), Is.EqualTo("|>  |> \nvar a=1;var b=2;\nfunction fn(a){}"));
        }

        [Test]
        public void Can_use_minifycss_script_block()
        {
            var context = new ScriptContext {
                Plugins = { new ServiceStackScriptBlocks() },
                DebugMode = false,
            }.Init();

            var output = context.EvaluateScript("{{#minifycss}} a { width: 1px; } b { height: 1px; } {{/minifycss}}");
            Assert.That(output.Trim(), Is.EqualTo("a{width:1px}b{height:1px}"));
            
            output = context.EvaluateScript("{{#minifycss appendTo css}} a { width: 1px; } {{/minifycss}} |> {{#minifycss appendTo css}} b { height: 1px; } {{/minifycss}} |> {{css}}");
            Assert.That(output.NormalizeNewLines(), Is.EqualTo("|>  |>  a{width:1px} b{height:1px}"));
        }

        [Test]
        public void Can_use_minifyhtml_script_block()
        {
            var context = new ScriptContext {
                Plugins = { new ServiceStackScriptBlocks() },
                DebugMode = false,
            }.Init();

            var output = context.EvaluateScript("{{#minifyhtml}} <h1 >  Title  </h1>  <p >  Content  </p> {{/minifyhtml}}");
            Assert.That(output.Trim(), Is.EqualTo("<h1> Title </h1> <p> Content </p>"));
            
            output = context.EvaluateScript("{{#minifyhtml appendTo html}} <h1 >  Title  </h1> {{/minifyhtml}} |> {{#minifyhtml appendTo html}} <p >  Content  </p> {{/minifyhtml}} |> {{html |> raw}}");
            Assert.That(output.NormalizeNewLines(), Is.EqualTo("|>  |> <h1> Title </h1><p> Content </p>"));
        }

        [Test]
        public void Can_use_minifyjs_script_block_with_cache() 
        {
            // all js/css/html has same base impl/behavior
            
            var context = new ScriptContext {
                Plugins = { new ServiceStackScriptBlocks() },
                DebugMode = false,
            }.Init();

            var js = "var a = 1; var b = 2;";
            var minified = "var a=1;var b=2;";

            var output = context.EvaluateScript("{{#minifyjs}}" + js + "{{/minifyjs}}");
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(minified));

            Assert.That(context.Cache["minifyjs:" + js].ToString().NormalizeNewLines(), Is.EqualTo(minified));
            output = context.EvaluateScript("{{#minifyjs}}" + js + "{{/minifyjs}}"); 
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(minified));
            
            context.Cache.Clear();

            var js2 = "function fn ( a ) { }";
            var minified2 = "function fn(a){}";
            
            output = context.EvaluateScript("{{#minifyjs appendTo scripts}}" + js + "{{/minifyjs}} |> {{#minifyjs appendTo scripts}}" + js2 + "{{/minifyjs}} |> {{scripts}}");
            Assert.That(output.NormalizeNewLines(), Is.EqualTo($"|>  |> \n{minified}\n{minified2}"));
            
            Assert.That(context.Cache["minifyjs:" + js].ToString().NormalizeNewLines(), Is.EqualTo(minified));
            Assert.That(context.Cache["minifyjs:" + js2].ToString().NormalizeNewLines(), Is.EqualTo(minified2));
            
            output = context.EvaluateScript("{{#minifyjs appendTo scripts}}" + js + "{{/minifyjs}} |> {{#minifyjs appendTo scripts}}" + js2 + "{{/minifyjs}} |> {{scripts}}");
            Assert.That(output.NormalizeNewLines(), Is.EqualTo($"|>  |> \n{minified}\n{minified2}"));
        }

        [Test]
        public void Does_not_minify_or_cache_in_DebugMode()
        {
            // all js/css/html has same base impl/behavior
            
            var context = new ScriptContext {
                Plugins = { new ServiceStackScriptBlocks() },
                DebugMode = true,
            }.Init();

            var js = "var a = 1; var b = 2;";

            var output = context.EvaluateScript("{{#minifyjs}}" + js + "{{/minifyjs}}");
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(js));

            Assert.That(context.Cache.ContainsKey("minifyjs::" + js), Is.False);
            output = context.EvaluateScript("{{#minifyjs}}" + js + "{{/minifyjs}}"); 
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(js));
        }

        [Test]
        public void Does_parse_keyvalues()
        {
            var context = new ScriptContext().Init();

            var result = context.Evaluate(@"
                {{#keyvalues dict}}
                    Apples   2
                    Oranges  3                    
                {{/keyvalues}}{{dict|return}}");
            
            Assert.That(result, Is.EquivalentTo(new Dictionary<string, string> {
                {"Apples","2"},
                {"Oranges","3"},
            }));
        }

        [Test]
        public void Does_parse_keyvalues_with_delimiter()
        {
            var context = new ScriptContext().Init();

            var result = context.Evaluate(@"
                {{#keyvalues dict ':'}}
                    Grape Fruit:  2
                    Rock Melon:   3                    
                {{/keyvalues}}{{dict|return}}");
            
            Assert.That(result, Is.EquivalentTo(new Dictionary<string, string> {
                {"Grape Fruit","2"},
                {"Rock Melon","3"},
            }));
        }

        [Test]
        public void Does_parse_keyvalues_in_code_blocks()
        {
            var context = new ScriptContext().Init();

            var result = context.Evaluate(@"
```code
#keyvalues dict ':'
  Apples:       2
  Oranges:      3                    
  Grape Fruit:  2
  Rock Melon:   3                    
/keyvalues
dict |> return
```");
                
            Assert.That(result, Is.EquivalentTo(new Dictionary<string, string> {
                {"Apples","2"},
                {"Oranges","3"},
                {"Grape Fruit","2"},
                {"Rock Melon","3"},
            }));
        }

        [Test]
        public void Does_parse_keyvalues_in_code_blocks_with_code_preprocessor()
        {
            var context = new ScriptContext {
                Preprocessors = { ScriptPreprocessors.TransformCodeBlocks }
            }.Init();

            var result = context.Evaluate(@"
```code
#keyvalues dict ':'
  * Apples:       2
  * Oranges:      3                    
  * Grape Fruit:  2
  * Rock Melon:   3                    
/keyvalues
dict |> return
```");
                
            Assert.That(result, Is.EquivalentTo(new Dictionary<string, string> {
                {"Apples","2"},
                {"Oranges","3"},
                {"Grape Fruit","2"},
                {"Rock Melon","3"},
            }));
        }
 
        [Test]
        public void Does_parse_csv()
        {
            var context = new ScriptContext().Init();

            var result = context.Evaluate(@"
                {{#csv list}}
                    Apples,2,2
                    Oranges,3,3                   
                    Grape Fruit,2,2
                    Rock Melon,3,3                 
                {{/csv}}{{list|return}}");
            
            Assert.That(result, Is.EquivalentTo(new List<List<string>> {
                new List<string> { "Apples", "2", "2" },
                new List<string> { "Oranges", "3", "3" },
                new List<string> { "Grape Fruit", "2", "2" },
                new List<string> { "Rock Melon", "3", "3" },
            }));
        }

        [Test]
        public void Can_use_while_block()
        {
            var context = new ScriptContext().Init();

            var result = context.EvaluateScript(@"
```code
3 |> to => times
#while times > 0
    times 
    times - 1 |> to => times
/while
```");
            
            Assert.That(result.NormalizeNewLines(), Is.EqualTo("3\n2\n1"));
            
            result = context.EvaluateScript(@"
```code
0 |> to => times
#while times > 0
    times 
    times - 1 |> to => times
else
    `times == 0`
/while
```");
            
            Assert.That(result.NormalizeNewLines(), Is.EqualTo("times == 0"));
        }

        [Test]
        public void Can_use_while_block_single_new_line()
        {
            var context = new ScriptContext().Init();

            var result = context.EvaluateScript(@"
```code
3 |> to => times
#while times > 0
    times 
    times - 1 |> to => times
/while
```".Replace("\r",""));
            
            Assert.That(result.NormalizeNewLines(), Is.EqualTo("3\n2\n1"));
            
            result = context.EvaluateScript(@"
```code
0 |> to => times
#while times > 0
    times 
    times - 1 |> to => times
else
    `times == 0`
/while
```".Replace("\r",""));
            
            Assert.That(result.NormalizeNewLines(), Is.EqualTo("times == 0"));
        }

        [Test]
        public void Can_use_while_block_with_code_preprocessor()
        {
            var context = new ScriptContext {
                Preprocessors = { ScriptPreprocessors.TransformCodeBlocks }
            }.Init();

            var result = context.EvaluateScript(@"
```code
3 |> to => times
#while times > 0
    times 
    times - 1 |> to => times
/while
```");
            
            Assert.That(result.NormalizeNewLines(), Is.EqualTo("3\n2\n1"));
            
            result = context.EvaluateScript(@"
```code
0 |> to => times
#while times > 0
    times 
    times - 1 |> to => times
else
    `times == 0`
/while
```");
            
            Assert.That(result.NormalizeNewLines(), Is.EqualTo("times == 0"));
        }
    }
}