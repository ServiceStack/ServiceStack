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
    }
}