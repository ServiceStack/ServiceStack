using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class ScriptHtmlFilterTests
    {
        [Test]
        public void Can_call_htmlList_with_empty_arg()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["arg"] = new List<Dictionary<string,object>>
                    {
                        new Dictionary<string, object>{ { "a", 1 } }
                    },
                    ["emptyArg"] = new List<Dictionary<string,object>>()
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{ arg |> htmlList }}"),
                Is.EqualTo("<table class=\"table\"><thead><tr><th>a</th></tr></thead><tbody><tr><td>1</td></tr></tbody></table>"));

            Assert.That(context.EvaluateScript("{{ arg |> htmlList() }}"),
                Is.EqualTo("<table class=\"table\"><thead><tr><th>a</th></tr></thead><tbody><tr><td>1</td></tr></tbody></table>"));

            Assert.That(context.EvaluateScript("{{ arg |> htmlList({}) }}"),
                Is.EqualTo("<table class=\"table\"><thead><tr><th>a</th></tr></thead><tbody><tr><td>1</td></tr></tbody></table>"));

            Assert.That(context.EvaluateScript("{{ arg |> htmlList({ }) }}"),
                Is.EqualTo("<table class=\"table\"><thead><tr><th>a</th></tr></thead><tbody><tr><td>1</td></tr></tbody></table>"));

            Assert.That(context.EvaluateScript("{{ emptyArg |> htmlList }}"), Is.EqualTo(""));

            Assert.That(context.EvaluateScript("{{ emptyArg |> htmlList({ captionIfEmpty: 'no rows' }) }}"),
                Is.EqualTo("<table class=\"table\"><caption>no rows</caption></table>"));
        }

        [Test]
        public void Can_render_simple_table()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["rockstars"] = new List<Dictionary<string,object>>
                    {
                        new Dictionary<string, object>{ {"FirstName", "Kurt" }, { "Age", 27 } },
                        new Dictionary<string, object>{ {"FirstName", "Jimi" }, { "Age", 27 } },
                    }
                }
            }.Init();

            Assert.That(context.EvaluateScript(@"{{ rockstars |> htmlDump({ className: ""table table-striped"", caption: ""Rockstars"" }) }}"), 
                Is.EqualTo(@"<table class=""table table-striped""><caption>Rockstars</caption><thead><tr><th>First Name</th><th>Age</th></tr></thead><tbody><tr><td>Kurt</td><td>27</td></tr><tr><td>Jimi</td><td>27</td></tr></tbody></table>"));

            Assert.That(context.EvaluateScript(@"{{ [] |> htmlDump({ captionIfEmpty: ""No Rocksars""}) }}"), 
                Is.EqualTo("<table class=\"table\"><caption>No Rocksars</caption></table>"));
        }

        [Test]
        public void Can_render_complex_object_graph_with_htmldump()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["customers"] = QueryData.Customers,
                    ["htmlOptions"] = new Dictionary<string, object>
                    {
                        { "className", "table" },
                        { "childClass", "table-striped" },
                        { "childDepth", 3 },
                    }
                }
            }.Init();

            "<div id=htmldump>".Print();

            "<h3>3x Customers:</h3>".Print();
            context.EvaluateScript("{{ customers |> take(3) |> htmlDump(htmlOptions) }}").Print();

            "<h3>Customer:</h3>".Print();
            context.EvaluateScript("{{ customers |> first |> htmlDump(htmlOptions) }}").Print();

            "<h3>Orders:</h3>".Print();
            context.EvaluateScript("{{ customers |> first |> property('Orders') |> htmlDump(htmlOptions) }}").Print();

            "<h3>Order:</h3>".Print();
            context.EvaluateScript("{{ customers |> first |> property('Orders') |> get(0) |> htmlDump(htmlOptions) }}").Print();

            "<h3>Order Date:</h3>".Print();
            context.EvaluateScript("{{ customers |> first |> property('Orders') |> get(0) |> property('OrderDate') |> htmlDump(htmlOptions) }}").Print();

            "</div>".Print();
        }

        [Test]
        public void Can_execute_custom_html_tags_with_primary_content_usage()
        {
            var context = new ScriptContext().Init();

            Assert.That(context.EvaluateScript("{{ 'http://example.org' |> htmlLink }}"), Is.EqualTo("<a href=\"http://example.org\">http://example.org</a>"));
            Assert.That(context.EvaluateScript("{{ 'http://example.org' |> htmlLink({ text:'link' }) }}"), Is.EqualTo("<a href=\"http://example.org\">link</a>"));
            Assert.That(context.EvaluateScript("{{ 'logo.png' |> htmlImage }}"), Is.EqualTo("<img src=\"logo.png\">"));
            Assert.That(context.EvaluateScript("{{ 'logo.png' |> htmlImage({ alt:'alt text' }) }}"), Is.EqualTo("<img alt=\"alt text\" src=\"logo.png\">"));
        }

        [Test]
        public void Can_execute_htmlTag_filters()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript("{{ '<h1>title</h1>' |> htmlA({ href:'#' }) }}"), Is.EqualTo("<a href=\"#\"><h1>title</h1></a>"));
            Assert.That(context.EvaluateScript("{{ { src:'logo.png', alt:'alt text' } |> htmlImg }}"), Is.EqualTo("<img alt=\"alt text\" src=\"logo.png\">"));
        }

        [Test]
        public void htmlTag_filters_does_convert_reserved_js_keywords()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript("{{ '<h1>title</h1>' |> htmlA({ href:'#', className:'cls' }) }}"), Is.EqualTo("<a class=\"cls\" href=\"#\"><h1>title</h1></a>"));
            Assert.That(context.EvaluateScript("{{ { src:'logo.png', alt:'alt text', className:'cls' } |> htmlImg }}"), Is.EqualTo("<img alt=\"alt text\" class=\"cls\" src=\"logo.png\">"));
            Assert.That(context.EvaluateScript("{{ 'text' |> htmlLabel({ htmlFor:'id' }) }}"), Is.EqualTo("<label for=\"id\">text</label>"));
        }

        [Test]
        public void Can_send_text_content_to_html_tags_primarily_used_with_text()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript("{{ 'text' |> htmlEm }}"), Is.EqualTo("<em>text</em>"));
            Assert.That(context.EvaluateScript("{{ 'text' |> htmlB }}"), Is.EqualTo("<b>text</b>"));
            Assert.That(context.EvaluateScript("{{ 'text' |> htmlB({ class:'cls' }) }}"), Is.EqualTo("<b class=\"cls\">text</b>"));
            Assert.That(context.EvaluateScript("{{ 'text' |> htmlOption }}"), Is.EqualTo("<option>text</option>"));
            Assert.That(context.EvaluateScript("{{ 'text' |> htmlOption({ value:'val' }) }}"), Is.EqualTo("<option value=\"val\">text</option>"));
            
            Assert.That(context.EvaluateScript("{{ ['A','B','C'] |> map('htmlOption(it)') |> join('') |> htmlSelect({ name:'sel' }) }}"), 
                Is.EqualTo("<select name=\"sel\"><option>A</option><option>B</option><option>C</option></select>"));
        }

        [Test]
        public void Can_generate_html_with_bindings()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript("{{ ['A','B','C'] |> map('htmlOption(it, { value: it })') |> join('') |> htmlSelect({ name:'sel' }) }}"), 
                Is.EqualTo("<select name=\"sel\"><option value=\"A\">A</option><option value=\"B\">B</option><option value=\"C\">C</option></select>"));
            
            Assert.That(context.EvaluateScript("{{ ['A','B','C'] |> map('htmlOption(it, { value: it })') |> join('') |> htmlSelect({ name:'sel' }) }}"), 
                Is.EqualTo("<select name=\"sel\"><option value=\"A\">A</option><option value=\"B\">B</option><option value=\"C\">C</option></select>"));
        }

        [Test]
        public void Does_generate_class_list_with_htmlClass()
        {
            var context = new ScriptContext {
                Args = {
                    ["index"] = 1,
                    ["name"] = "foo",
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{ {alt:isOdd(index), active:'foo'==name } |> htmlClass }}"), 
                Is.EqualTo(" class=\"alt active\""));
            Assert.That(context.EvaluateScript("{{ {alt:isEven(index), active:'bar'==name } |> htmlClass }}"), 
                Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ [isOdd(index) ? 'odd': 'even', 'foo'==name ? 'active' : ''] |> htmlClass }}"), 
                Is.EqualTo(" class=\"odd active\""));
            Assert.That(context.EvaluateScript("{{ [isOdd(index+1) ? 'odd': 'even', 'bar'==name ? 'active' : ''] |> htmlClass }}"), 
                Is.EqualTo(" class=\"even\""));

            Assert.That(context.EvaluateScript("{{ 'hide' |> if(!disclaimerAccepted) |> htmlClass }}"), 
                Is.EqualTo(" class=\"hide\""));
        }

        [Test]
        public void HtmlAttrs_with_bool_only_emits_name()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript("<option {{ {selected:true,test:'val'} |> htmlAttrs }}>"), 
                Is.EqualTo("<option  selected test=\"val\">"));

            Assert.That(context.EvaluateScript("<option {{ {selected:false,test:'val'} |> htmlAttrs }}>"), 
                Is.EqualTo("<option  test=\"val\">"));
        }

        [Test]
        public void Does_htmlDump_singleRow()
        {
            var context = new ScriptContext {
                Args = {
                    ["rows"] = new List<Dictionary<string, object>> {
                        new Dictionary<string, object> {
                            ["Id"] = 1,
                            ["Name"] = "foo",
                            ["None"] = DBNull.Value,
                        }
                    }
                }
            }.Init();

            var output = context.EvaluateScript("{{ rows |> htmlDump }}");
//            output.Print();
            Assert.That(output, Does.Contain(
                "<tr><th>Id</th><td>1</td></tr><tr><th>Name</th><td>foo</td></tr><tr><th>None</th><td></td></tr>"));
        }
    }
}