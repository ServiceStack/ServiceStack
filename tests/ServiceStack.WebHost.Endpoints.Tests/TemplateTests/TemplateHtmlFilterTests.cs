using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateHtmlFilterTests
    {
        [Test]
        public void Can_call_htmlList_with_empty_arg()
        {
            var context = new TemplateContext
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

            Assert.That(context.EvaluateTemplate("{{ arg | htmlList }}"),
                Is.EqualTo("<table class=\"table\"><thead><tr><th>a</th></tr></thead><tbody><tr><td>1</td></tr></tbody></table>"));

            Assert.That(context.EvaluateTemplate("{{ arg | htmlList() }}"),
                Is.EqualTo("<table class=\"table\"><thead><tr><th>a</th></tr></thead><tbody><tr><td>1</td></tr></tbody></table>"));

            Assert.That(context.EvaluateTemplate("{{ arg | htmlList({}) }}"),
                Is.EqualTo("<table class=\"table\"><thead><tr><th>a</th></tr></thead><tbody><tr><td>1</td></tr></tbody></table>"));

            Assert.That(context.EvaluateTemplate("{{ arg | htmlList({ }) }}"),
                Is.EqualTo("<table class=\"table\"><thead><tr><th>a</th></tr></thead><tbody><tr><td>1</td></tr></tbody></table>"));

            Assert.That(context.EvaluateTemplate("{{ emptyArg | htmlList }}"), Is.EqualTo(""));

            Assert.That(context.EvaluateTemplate("{{ emptyArg | htmlList({ captionIfEmpty: 'no rows' }) }}"),
                Is.EqualTo("<table class=\"table\"><caption>no rows</caption></table>"));
        }

        [Test]
        public void Can_render_simple_table()
        {
            var context = new TemplateContext
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

            Assert.That(context.EvaluateTemplate(@"{{ rockstars | htmlDump({ className: ""table table-striped"", caption: ""Rockstars"" }) }}"), 
                Is.EqualTo(@"<table class=""table table-striped""><caption>Rockstars</caption><thead><tr><th>First Name</th><th>Age</th></tr></thead><tbody><tr><td>Kurt</td><td>27</td></tr><tr><td>Jimi</td><td>27</td></tr></tbody></table>"));

            Assert.That(context.EvaluateTemplate(@"{{ [] | htmlDump({ captionIfEmpty: ""No Rocksars""}) }}"), 
                Is.EqualTo("<table class=\"table\"><caption>No Rocksars</caption></table>"));
        }

        [Test]
        public void Can_render_complex_object_graph_with_htmldump()
        {
            var context = new TemplateContext
            {
                Args =
                {
                    ["customers"] = TemplateQueryData.Customers,
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
            context.EvaluateTemplate("{{ customers | take(3) | htmlDump(htmlOptions) }}").Print();

            "<h3>Customer:</h3>".Print();
            context.EvaluateTemplate("{{ customers | first | htmlDump(htmlOptions) }}").Print();

            "<h3>Orders:</h3>".Print();
            context.EvaluateTemplate("{{ customers | first | property('Orders') | htmlDump(htmlOptions) }}").Print();

            "<h3>Order:</h3>".Print();
            context.EvaluateTemplate("{{ customers | first | property('Orders') | get(0) | htmlDump(htmlOptions) }}").Print();

            "<h3>Order Date:</h3>".Print();
            context.EvaluateTemplate("{{ customers | first | property('Orders') | get(0) | property('OrderDate') | htmlDump(htmlOptions) }}").Print();

            "</div>".Print();
        }

        [Test]
        public void Can_execute_custom_html_tags_with_primary_content_usage()
        {
            var context = new TemplateContext().Init();

            Assert.That(context.EvaluateTemplate("{{ 'http://example.org' | htmlLink }}"), Is.EqualTo("<a href=\"http://example.org\">http://example.org</a>"));
            Assert.That(context.EvaluateTemplate("{{ 'http://example.org' | htmlLink({ text:'link' }) }}"), Is.EqualTo("<a href=\"http://example.org\">link</a>"));
            Assert.That(context.EvaluateTemplate("{{ 'logo.png' | htmlImage }}"), Is.EqualTo("<img src=\"logo.png\">"));
            Assert.That(context.EvaluateTemplate("{{ 'logo.png' | htmlImage({ alt:'alt text' }) }}"), Is.EqualTo("<img alt=\"alt text\" src=\"logo.png\">"));
        }

        [Test]
        public void Can_execute_htmlTag_filters()
        {
            var context = new TemplateContext().Init();
            
            Assert.That(context.EvaluateTemplate("{{ '<h1>title</h1>' | htmlA({ href:'#' }) }}"), Is.EqualTo("<a href=\"#\"><h1>title</h1></a>"));
            Assert.That(context.EvaluateTemplate("{{ { src:'logo.png', alt:'alt text' } | htmlImg }}"), Is.EqualTo("<img alt=\"alt text\" src=\"logo.png\">"));
        }

        [Test]
        public void htmlTag_filters_does_convert_reserved_js_keywords()
        {
            var context = new TemplateContext().Init();
            
            Assert.That(context.EvaluateTemplate("{{ '<h1>title</h1>' | htmlA({ href:'#', className:'cls' }) }}"), Is.EqualTo("<a class=\"cls\" href=\"#\"><h1>title</h1></a>"));
            Assert.That(context.EvaluateTemplate("{{ { src:'logo.png', alt:'alt text', className:'cls' } | htmlImg }}"), Is.EqualTo("<img alt=\"alt text\" class=\"cls\" src=\"logo.png\">"));
            Assert.That(context.EvaluateTemplate("{{ 'text' | htmlLabel({ htmlFor:'id' }) }}"), Is.EqualTo("<label for=\"id\">text</label>"));
        }

        [Test]
        public void Can_send_text_content_to_html_tags_primarily_used_with_text()
        {
            var context = new TemplateContext().Init();
            
            Assert.That(context.EvaluateTemplate("{{ 'text' | htmlEm }}"), Is.EqualTo("<em>text</em>"));
            Assert.That(context.EvaluateTemplate("{{ 'text' | htmlB }}"), Is.EqualTo("<b>text</b>"));
            Assert.That(context.EvaluateTemplate("{{ 'text' | htmlB({ class:'cls' }) }}"), Is.EqualTo("<b class=\"cls\">text</b>"));
            Assert.That(context.EvaluateTemplate("{{ 'text' | htmlOption }}"), Is.EqualTo("<option>text</option>"));
            Assert.That(context.EvaluateTemplate("{{ 'text' | htmlOption({ value:'val' }) }}"), Is.EqualTo("<option value=\"val\">text</option>"));
            
            Assert.That(context.EvaluateTemplate("{{ ['A','B','C'] | map('htmlOption(it)') | join('') | htmlSelect({ name:'sel' }) }}"), 
                Is.EqualTo("<select name=\"sel\"><option>A</option><option>B</option><option>C</option></select>"));
        }

        [Test]
        public void Can_generate_html_with_bindings()
        {
            var context = new TemplateContext().Init();
            
            Assert.That(context.EvaluateTemplate("{{ ['A','B','C'] | map('htmlOption(it, { value: it })') | join('') | htmlSelect({ name:'sel' }) }}"), 
                Is.EqualTo("<select name=\"sel\"><option value=\"A\">A</option><option value=\"B\">B</option><option value=\"C\">C</option></select>"));
        }

    }
}