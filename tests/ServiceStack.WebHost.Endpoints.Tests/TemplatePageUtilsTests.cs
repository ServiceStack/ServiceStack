using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.Text;

#if NETCORE
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class TemplatePageUtilsTests
    {
        [Test]
        public void Can_parse_template_with_no_vars()
        {
            Assert.That(TemplatePageUtils.ParseTemplatePage("").Count, Is.EqualTo(0));
            var fragments = TemplatePageUtils.ParseTemplatePage("<h1>title</h1>");
            Assert.That(fragments.Count, Is.EqualTo(1));

            var strFragment = fragments[0] as PageStringFragment;
            Assert.That(strFragment.Value, Is.EqualTo("<h1>title</h1>"));
        }

        [Test]
        public void Can_parse_template_with_variable()
        {
            var fragments = TemplatePageUtils.ParseTemplatePage("<h1>{{ title }}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            var strFragment1 = fragments[0] as PageStringFragment;
            var varFragment2 = fragments[1] as PageVariableFragment;
            var strFragment3 = fragments[2] as PageStringFragment;

            Assert.That(strFragment1.Value, Is.EqualTo("<h1>"));
            Assert.That(varFragment2.OriginalText, Is.EqualTo("{{ title }}"));
            Assert.That(varFragment2.Binding, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterExpressions.Length, Is.EqualTo(0));
            Assert.That(strFragment3.Value, Is.EqualTo("</h1>"));
        }

        [Test]
        public void Can_parse_template_with_filter()
        {
            var fragments = TemplatePageUtils.ParseTemplatePage("<h1>{{ title | filter }}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            var strFragment1 = fragments[0] as PageStringFragment;
            var varFragment2 = fragments[1] as PageVariableFragment;
            var strFragment3 = fragments[2] as PageStringFragment;

            Assert.That(strFragment1.Value, Is.EqualTo("<h1>"));
            Assert.That(varFragment2.OriginalText, Is.EqualTo("{{ title | filter }}"));
            Assert.That(varFragment2.Binding, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterExpressions.Length, Is.EqualTo(1));
            Assert.That(varFragment2.FilterExpressions[0].Name, Is.EqualTo("filter"));
            Assert.That(varFragment2.FilterExpressions[0].Args.Count, Is.EqualTo(0));
            Assert.That(strFragment3.Value, Is.EqualTo("</h1>"));

            fragments = TemplatePageUtils.ParseTemplatePage("<h1>{{ title | filter() }}</h1>");

            varFragment2 = fragments[1] as PageVariableFragment;
            Assert.That(varFragment2.OriginalText, Is.EqualTo("{{ title | filter() }}"));
            Assert.That(varFragment2.Binding, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterExpressions.Length, Is.EqualTo(1));
            Assert.That(varFragment2.FilterExpressions[0].Name, Is.EqualTo("filter"));
            Assert.That(varFragment2.FilterExpressions[0].Args.Count, Is.EqualTo(0));
        }

        [Test]
        public void Can_parse_template_with_filter_without_whitespace()
        {
            var fragments = TemplatePageUtils.ParseTemplatePage("<h1>{{title}}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            var strFragment1 = fragments[0] as PageStringFragment;
            var varFragment2 = fragments[1] as PageVariableFragment;
            var strFragment3 = fragments[2] as PageStringFragment;

            Assert.That(strFragment1.Value, Is.EqualTo("<h1>"));
            Assert.That(varFragment2.Binding, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterExpressions.Length, Is.EqualTo(0));

            fragments = TemplatePageUtils.ParseTemplatePage("<h1>{{title|filter}}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            strFragment1 = fragments[0] as PageStringFragment;
            varFragment2 = fragments[1] as PageVariableFragment;
            strFragment3 = fragments[2] as PageStringFragment;

            Assert.That(strFragment1.Value, Is.EqualTo("<h1>"));
            Assert.That(varFragment2.OriginalText, Is.EqualTo("{{title|filter}}"));
            Assert.That(varFragment2.Binding, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterExpressions.Length, Is.EqualTo(1));
            Assert.That(varFragment2.FilterExpressions[0].Name, Is.EqualTo("filter"));
            Assert.That(varFragment2.FilterExpressions[0].Args.Count, Is.EqualTo(0));
            Assert.That(strFragment3.Value, Is.EqualTo("</h1>"));
        }

        [Test]
        public void Can_parse_template_with_filter_with_arg()
        {
            var fragments = TemplatePageUtils.ParseTemplatePage("<h1>{{ title | filter(1) }}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            var strFragment1 = fragments[0] as PageStringFragment;
            var varFragment2 = fragments[1] as PageVariableFragment;
            var strFragment3 = fragments[2] as PageStringFragment;

            Assert.That(strFragment1.Value, Is.EqualTo("<h1>"));
            Assert.That(varFragment2.OriginalText, Is.EqualTo("{{ title | filter(1) }}"));
            Assert.That(varFragment2.Binding, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterExpressions.Length, Is.EqualTo(1));
            Assert.That(varFragment2.FilterExpressions[0].Name, Is.EqualTo("filter"));
            Assert.That(varFragment2.FilterExpressions[0].Args.Count, Is.EqualTo(1));
            Assert.That(varFragment2.FilterExpressions[0].Args[0], Is.EqualTo("1"));
            Assert.That(strFragment3.Value, Is.EqualTo("</h1>"));
        }

        [Test]
        public void Can_parse_template_with_filter_with_multiple_args()
        {
            var fragments = TemplatePageUtils.ParseTemplatePage("<h1>{{ title | filter(1,2.2,'a',\"b\",true) }}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            var strFragment1 = fragments[0] as PageStringFragment;
            var varFragment2 = fragments[1] as PageVariableFragment;
            var strFragment3 = fragments[2] as PageStringFragment;

            Assert.That(strFragment1.Value, Is.EqualTo("<h1>"));
            Assert.That(varFragment2.OriginalText, Is.EqualTo("{{ title | filter(1,2.2,'a',\"b\",true) }}"));
            Assert.That(varFragment2.Binding, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterExpressions.Length, Is.EqualTo(1));
            Assert.That(varFragment2.FilterExpressions[0].Name, Is.EqualTo("filter"));
            Assert.That(varFragment2.FilterExpressions[0].Args.Count, Is.EqualTo(5));
            Assert.That(varFragment2.FilterExpressions[0].Args[0], Is.EqualTo("1"));
            Assert.That(varFragment2.FilterExpressions[0].Args[1], Is.EqualTo("2.2"));
            Assert.That(varFragment2.FilterExpressions[0].Args[2], Is.EqualTo("'a'"));
            Assert.That(varFragment2.FilterExpressions[0].Args[3], Is.EqualTo("\"b\""));
            Assert.That(varFragment2.FilterExpressions[0].Args[4], Is.EqualTo("true"));
            Assert.That(strFragment3.Value, Is.EqualTo("</h1>"));
        }

        [Test]
        public void Can_parse_template_with_multiple_filters_and_multiple_args()
        {
            var fragments = TemplatePageUtils.ParseTemplatePage("<h1>{{ title | filter1 | filter2(1) | filter3(1,2.2,'a',\"b\",true) }}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            var strFragment1 = fragments[0] as PageStringFragment;
            var varFragment2 = fragments[1] as PageVariableFragment;
            var strFragment3 = fragments[2] as PageStringFragment;

            Assert.That(strFragment1.Value, Is.EqualTo("<h1>"));
            Assert.That(varFragment2.OriginalText, Is.EqualTo("{{ title | filter1 | filter2(1) | filter3(1,2.2,'a',\"b\",true) }}"));
            Assert.That(varFragment2.Binding, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterExpressions.Length, Is.EqualTo(3));

            Assert.That(varFragment2.FilterExpressions[0].Name, Is.EqualTo("filter1"));
            Assert.That(varFragment2.FilterExpressions[0].Args.Count, Is.EqualTo(0));

            Assert.That(varFragment2.FilterExpressions[1].Name, Is.EqualTo("filter2"));
            Assert.That(varFragment2.FilterExpressions[1].Args.Count, Is.EqualTo(1));
            Assert.That(varFragment2.FilterExpressions[2].Args[0], Is.EqualTo("1"));

            Assert.That(varFragment2.FilterExpressions[2].Name, Is.EqualTo("filter3"));
            Assert.That(varFragment2.FilterExpressions[2].Args.Count, Is.EqualTo(5));
            Assert.That(varFragment2.FilterExpressions[2].Args[0], Is.EqualTo("1"));
            Assert.That(varFragment2.FilterExpressions[2].Args[1], Is.EqualTo("2.2"));
            Assert.That(varFragment2.FilterExpressions[2].Args[2], Is.EqualTo("'a'"));
            Assert.That(varFragment2.FilterExpressions[2].Args[3], Is.EqualTo("\"b\""));
            Assert.That(varFragment2.FilterExpressions[2].Args[4], Is.EqualTo("true"));

            Assert.That(strFragment3.Value, Is.EqualTo("</h1>"));
        }

        [Test]
        public void Can_parse_template_with_multiple_variables_and_filters()
        {
            var fragments = TemplatePageUtils.ParseTemplatePage("<h1>{{ title | filter1 }}</h1>\n<p>{{ content | filter2(a) }}</p>");
            Assert.That(fragments.Count, Is.EqualTo(5));

            var strFragment1 = fragments[0] as PageStringFragment;
            var varFragment2 = fragments[1] as PageVariableFragment;
            var strFragment3 = fragments[2] as PageStringFragment;
            var varFragment4 = fragments[3] as PageVariableFragment;
            var strFragment5 = fragments[4] as PageStringFragment;

            Assert.That(strFragment1.Value, Is.EqualTo("<h1>"));

            Assert.That(varFragment2.OriginalText, Is.EqualTo("{{ title | filter1 }}"));
            Assert.That(varFragment2.Binding, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterExpressions.Length, Is.EqualTo(1));
            Assert.That(varFragment2.FilterExpressions[0].Name, Is.EqualTo("filter1"));
            Assert.That(varFragment2.FilterExpressions[0].Args.Count, Is.EqualTo(0));

            Assert.That(strFragment3.Value, Is.EqualTo("</h1>\n<p>"));

            Assert.That(varFragment4.OriginalText, Is.EqualTo("{{ content | filter2(a) }}"));
            Assert.That(varFragment4.Binding, Is.EqualTo("content"));
            Assert.That(varFragment4.FilterExpressions.Length, Is.EqualTo(1));
            Assert.That(varFragment4.FilterExpressions[0].Name, Is.EqualTo("filter2"));
            Assert.That(varFragment4.FilterExpressions[0].Args.Count, Is.EqualTo(1));
            Assert.That(varFragment4.FilterExpressions[0].Args[0], Is.EqualTo("a"));

            Assert.That(strFragment5.Value, Is.EqualTo("</p>"));
        }

        [Test]
        public void Can_parse_next_token()
        {
            object value;
            JsBinding binding;

            "a".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(binding.Binding, Is.EqualTo("a"));
            "'a'".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EqualTo("a"));
            "\"a\"".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EqualTo("a"));
            "1".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EqualTo(1));
            "100".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EqualTo(100));
            "100.0".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EqualTo(100d));
            "1.0E+2".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EqualTo(100d));
            "1e+2".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EqualTo(100d));
            "true".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.True);
            "false".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.False);
            "null".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EqualTo(JsNull.Instance));
            "{foo:1}".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new Dictionary<string,object>{ {"foo", 1 }}));
            "{ foo : 1 , bar: 'qux', d: 1.1, b:false, n:null }".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new Dictionary<string,object>{ { "foo", 1 }, {"bar", "qux"}, {"d", 1.1d}, {"b", false}, {"n", JsNull.Instance} }));
            "{ map : { bar: 'qux', b: true } }".ToStringSegment().ParseNextToken(out value, out binding);
            var map = (Dictionary<string, object>) value;
            Assert.That(map["map"], Is.EquivalentTo(new Dictionary<string,object>{{"bar", "qux"}, {"b", true}}));
            "{varRef:foo}".ToStringSegment().ParseNextToken(out value, out binding);
            map = (Dictionary<string, object>) value;
            Assert.That(map["varRef"], Is.EqualTo(new JsBinding("foo")));
            "{ \"foo\" : 1 , \"bar\": 'qux', \"d\": 1.1, \"b\":false, \"n\":null }".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new Dictionary<string,object>{ { "foo", 1 }, {"bar", "qux"}, {"d", 1.1d}, {"b", false}, {"n", JsNull.Instance} }));

            "[1,2,3]".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new[]{ 1, 2, 3 }));
            "['a',\"b\",'c']".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new []{ "a", "b", "c" }));
            " [ 'a' , \"b\"  , 'c' ] ".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new []{ "a", "b", "c" }));
            "[ {a: 1}, {b: 2} ]".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new []{ new Dictionary<string,object>{ {"a", 1} }, new Dictionary<string,object>{ {"b", 2} } }));
            "[ {a: { 'aa': [1,2,3] } }, { b: [a,b,c] }, 3, true, null ]".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new object[]
            {
                new Dictionary<string,object>{ {"a", new Dictionary<string,object>{ {"aa", new[]{ 1, 2, 3} } } } }, 
                new Dictionary<string,object>{ {"b", new[]{ new JsBinding("a"), new JsBinding("b"), new JsBinding("c") }} },
                3,
                true,
                JsNull.Instance
            }));
        }

    }
}