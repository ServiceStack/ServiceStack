using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.Text;

#if NETCORE
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
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
        public void Can_parse_template_with_only_variable()
        {
            var fragments = TemplatePageUtils.ParseTemplatePage("{{ filter }}");
            Assert.That(fragments.Count, Is.EqualTo(1));
            Assert.That(((PageVariableFragment)fragments[0]).Binding, Is.EqualTo("filter"));
        }

        [Test]
        public void Can_parse_template_with_arg_and_multiple_filters()
        {
            var fragments = TemplatePageUtils.ParseTemplatePage("{{ ' - {{it}}' | forEach(items) | markdown }}");
            var varFragment = fragments[0] as PageVariableFragment;
            
            Assert.That(varFragment.OriginalText, Is.EqualTo("{{ ' - {{it}}' | forEach(items) | markdown }}"));
            Assert.That(varFragment.FilterExpressions.Length, Is.EqualTo(2));
            Assert.That(varFragment.FilterExpressions[0].Name, Is.EqualTo("forEach"));
            Assert.That(varFragment.FilterExpressions[0].Args.Count, Is.EqualTo(1));
            Assert.That(varFragment.FilterExpressions[0].Args[0], Is.EqualTo("items"));
            Assert.That(varFragment.FilterExpressions[1].Name, Is.EqualTo("markdown"));
        }

        [Test]
        public void Can_parse_filter_with_different_arg_types()
        {
            var fragments = TemplatePageUtils.ParseTemplatePage("{{ array(['a',1,'c']) }}");
            var varFragment = (PageVariableFragment)fragments[0];
            
            Assert.That(varFragment.OriginalText, Is.EqualTo("{{ array(['a',1,'c']) }}"));
            Assert.That(varFragment.InitialExpression.Name, Is.EqualTo("array"));
            Assert.That(varFragment.InitialExpression.Args.Count, Is.EqualTo(1));
        }

        [Test]
        public void Can_parse_next_token()
        {
            object value;
            JsBinding binding;

            "a".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(binding.Binding, Is.EqualTo("a"));
            "a2".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(binding.Binding, Is.EqualTo("a2"));
            " a2 ".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(binding.Binding, Is.EqualTo("a2"));
            "'a'".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EqualTo("a"));
            "\"a\"".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EqualTo("a"));
            "`a`".ToStringSegment().ParseNextToken(out value, out binding);
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
            Assert.That(value, Is.EqualTo(JsNull.Value));
            "{foo:1}".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new Dictionary<string,object>{ {"foo", 1 }}));
            "{ foo : 1 , bar: 'qux', d: 1.1, b:false, n:null }".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new Dictionary<string,object>{ { "foo", 1 }, {"bar", "qux"}, {"d", 1.1d}, {"b", false}, {"n", JsNull.Value} }));
            "{ map : { bar: 'qux', b: true } }".ToStringSegment().ParseNextToken(out value, out binding);
            var map = (Dictionary<string, object>) value;
            Assert.That(map["map"], Is.EquivalentTo(new Dictionary<string,object>{{"bar", "qux"}, {"b", true}}));
            "{varRef:foo}".ToStringSegment().ParseNextToken(out value, out binding);
            map = (Dictionary<string, object>) value;
            Assert.That(map["varRef"], Is.EqualTo(new JsBinding("foo")));
            "{ \"foo\" : 1 , \"bar\": 'qux', \"d\": 1.1, \"b\":false, \"n\":null }".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new Dictionary<string,object>{ { "foo", 1 }, {"bar", "qux"}, {"d", 1.1d}, {"b", false}, {"n", JsNull.Value} }));
            "{ `foo` : 1 , `bar`: 'qux', `d`: 1.1, `b`:false, `n`:null }".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new Dictionary<string,object>{ { "foo", 1 }, {"bar", "qux"}, {"d", 1.1d}, {"b", false}, {"n", JsNull.Value} }));

            "[1,2,3]".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new[]{ 1, 2, 3 }));
            "[a,b,c]".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new[]{ new JsBinding("a"), new JsBinding("b"), new JsBinding("c") }));
            "[a.Id,b.Name]".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new[]{ new JsExpression("a.Id"), new JsExpression("b.Name") }));
            "{ x: a.Id, y: b.Name }".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new Dictionary<string, object>
            {
                { "x", new JsExpression("a.Id") },
                { "y", new JsExpression("b.Name") },
            }));
            
            "['a',\"b\",`c`]".ToStringSegment().ParseNextToken(out value, out binding);
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
                JsNull.Value
            }));
            "{ k:'v', data: { id: 1, name: 'foo' }, k2: 'v2', k3: 'v3' }".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new Dictionary<string, object>
            {
                { "k", "v" },
                { "data", new Dictionary<string,object> { { "id", 1 }, {"name", "foo"} } },
                { "k2", "v2" },
                { "k3", "v3" },                
            }));
            "[{name:'Alice', score:50}, {name: 'Bob', score:40}, {name:'Cathy', score:45}]".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new[]
            {
                new Dictionary<string, object> { { "name", "Alice" }, { "score", 50 } },
                new Dictionary<string, object> { { "name", "Bob" }, { "score", 40 } },
                new Dictionary<string, object> { { "name", "Cathy" }, { "score", 45 } },
            }));
            
            //{{  | assignTo: words }}
        }

        [Test]
        public void Can_parse_templates_within_literals()
        {
            object value;
            JsBinding binding;

            "'<li>{{it}}</li>''".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EqualTo("<li>{{it}}</li>"));

            var fragments = TemplatePageUtils.ParseTemplatePage("<ul>{{ '<li>{{it}}</li>' }}</ul>");
            Assert.That(fragments.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_parse_method_binding_expressions()
        {
            object value;
            JsBinding binding;
            JsExpression expr;

            "if(or(gt(1,2),lt(3,4)))".ToStringSegment().ParseNextToken(out value, out binding);
            expr = (JsExpression) binding;

            Assert.That(expr.Args[0], Is.EqualTo("or(gt(1,2),lt(3,4))"));

            expr.Args[0].ParseNextToken(out value, out binding);
            expr = (JsExpression) binding;
            
            Assert.That(expr.Args[0], Is.EqualTo("gt(1,2)"));
            Assert.That(expr.Args[1], Is.EqualTo("lt(3,4)"));

            expr.Args[0].ParseNextToken(out value, out binding);
            expr = (JsExpression) binding;
            Assert.That(expr.Args[0], Is.EqualTo("1"));
            Assert.That(expr.Args[1], Is.EqualTo("2"));
            

            @"
            if (
                or (
                    gt ( 1 , 2 ) ,
                    lt ( 3 , 4 )
                )
            )".ToStringSegment().ParseNextToken(out value, out binding);
            expr = (JsExpression) binding;

            Assert.That(expr.Args[0].RemoveAllWhitespace(), Is.EqualTo("or(gt(1,2),lt(3,4))"));

            expr.Args[0].ParseNextToken(out value, out binding);
            expr = (JsExpression) binding;
            
            Assert.That(expr.Args[0].RemoveAllWhitespace(), Is.EqualTo("gt(1,2)"));
            Assert.That(expr.Args[1].RemoveAllWhitespace(), Is.EqualTo("lt(3,4)"));

            expr.Args[0].ParseNextToken(out value, out binding);
            expr = (JsExpression) binding;
            Assert.That(expr.Args[0], Is.EqualTo("1"));
            Assert.That(expr.Args[1], Is.EqualTo("2"));
        }

        [Test]
        public void Does_support_shorthand_object_initializers()
        {
            object value;
            JsBinding binding;

            "{key}".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new Dictionary<string,object>{ { "key", new JsBinding("key") }}));
            "{ key }".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new Dictionary<string,object>{ { "key", new JsBinding("key") }}));
            "{ map : { key , foo: 'bar' , qux } }".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EquivalentTo(new Dictionary<string,object>{ { "map", 
                new Dictionary<string, object>
                {
                    {"key", new JsBinding("key")},
                    {"foo", "bar"},
                    {"qux", new JsBinding("qux")},
                } 
            }}));
        }

        [Test]
        public void Does_preserve_new_lines()
        {
            object value;
            JsBinding binding;

            "'a\n'".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(value, Is.EqualTo("a\n"));
        }

        [Test]
        public void Can_parse_boolean_logic_expressions()
        {
            object value;
            JsBinding binding;

            var literal = "it.Id = 0".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(((JsExpression)binding).Name, Is.EqualTo("it.Id"));
            literal = literal.ParseNextToken(out value, out binding);
            Assert.That(binding, Is.EqualTo(JsAssignment.Operator));
            literal = literal.ParseNextToken(out value, out binding);
            Assert.That(value, Is.EqualTo(0));
        }

        [Test]
        public void Can_use_cleaner_whitespace_sensitive_syntax_for_string_arguments()
        {
            var fragments1 = TemplatePageUtils.ParseTemplatePage(
                @"{{ 
products 
  | where: it.UnitsInStock = 0 
  | select: { it.productName | raw } is sold out!\n 
}}");
            
            var fragments2 = TemplatePageUtils.ParseTemplatePage(
            @"{{ products 
                 | where: it.UnitsInStock = 0 
                 | select: { it.productName | raw } is sold out!\n }}");
            
            // i.e. is rewritten and is equivalent to:
            var fragments3 = TemplatePageUtils.ParseTemplatePage(
                @"{{ products | where(′it.UnitsInStock = 0′) | select(′{{ it.productName | raw }} is sold out!\n′)}}");
            Assert.That(fragments3.Count, Is.EqualTo(1));
            
            Assert.That(fragments1.Count, Is.EqualTo(1));
            var varFragment1 = fragments1[0] as PageVariableFragment;
            Assert.That(varFragment1.FilterExpressions[0].Name, Is.EqualTo("where"));
            Assert.That(varFragment1.FilterExpressions[0].Args.Count, Is.EqualTo(1));
            Assert.That(varFragment1.FilterExpressions[0].Args[0], Is.EqualTo("′it.UnitsInStock = 0′"));
            Assert.That(varFragment1.FilterExpressions[1].Name, Is.EqualTo("select"));
            Assert.That(varFragment1.FilterExpressions[1].Args.Count, Is.EqualTo(1));
            Assert.That(varFragment1.FilterExpressions[1].Args[0], Is.EqualTo("′{{ it.productName | raw }} is sold out!\\n′"));

            foreach (var fragments in new[]{ fragments2, fragments3 })
            {
                var varFragment = fragments[0] as PageVariableFragment;
                Assert.That(varFragment.FilterExpressions[0].Name, Is.EqualTo(varFragment1.FilterExpressions[0].Name));
                Assert.That(varFragment.FilterExpressions[0].Args.Count, Is.EqualTo(varFragment1.FilterExpressions[0].Args.Count));
                Assert.That(varFragment.FilterExpressions[0].Args[0], Is.EqualTo(varFragment1.FilterExpressions[0].Args[0]));
                Assert.That(varFragment.FilterExpressions[1].Name, Is.EqualTo(varFragment1.FilterExpressions[1].Name));
                Assert.That(varFragment.FilterExpressions[1].Args.Count, Is.EqualTo(varFragment1.FilterExpressions[1].Args.Count));
                Assert.That(varFragment.FilterExpressions[1].Args[0], Is.EqualTo(varFragment1.FilterExpressions[1].Args[0]));
            }
        }

        [Test]
        public void Can_parse_pages_starting_with_values()
        {
            var fragments = TemplatePageUtils.ParseTemplatePage(
                @"{{ [c.CustomerId, o.OrderId, o.OrderDate] | jsv }}\n");

            var varFragment = (PageVariableFragment) fragments[0];
            Assert.That(varFragment.InitialValue, Is.EqualTo(new[]
            {
                new JsExpression("c.CustomerId"),
                new JsExpression("o.OrderId"),
                new JsExpression("o.OrderDate"),
            }));
            
            Assert.That(varFragment.OriginalText, Is.EqualTo("{{ [c.CustomerId, o.OrderId, o.OrderDate] | jsv }}"));
            
            var newLine = (PageStringFragment) fragments[1];
            Assert.That(newLine.Value, Is.EqualTo("\\n"));
        }

        [Test]
        public void Can_parse_pages_starting_with_values_newLine()
        {
            var context = new TemplateContext().Init();
            var page = context.OneTimePage("{{ [c.CustomerId, o.OrderId, o.OrderDate] | jsv }}\n");
            var fragments = page.PageFragments;
            
//            var fragments = TemplatePageUtils.ParseTemplatePage(
//                "{{ [c.CustomerId, o.OrderId, o.OrderDate] | jsv }}\n");

            var varFragment = (PageVariableFragment) fragments[0];
            Assert.That(varFragment.InitialValue, Is.EqualTo(new[]
            {
                new JsExpression("c.CustomerId"),
                new JsExpression("o.OrderId"),
                new JsExpression("o.OrderDate"),
            }));
            
            var newLine = (PageStringFragment) fragments[1];
            Assert.That(newLine.Value, Is.EqualTo("\n"));
        }

        [Test]
        public void Can_parse_expressions_with_methods()
        {
            object value;
            JsBinding binding;

            "mod(it,3) != 0".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(binding, Is.EqualTo(new JsExpression("mod") { 
                Args = {
                    "it".ToStringSegment(),
                    "3".ToStringSegment(),
                }
            }));
        }

        [Test]
        public void Can_detect_invalid_syntax()
        {
            try
            {
                var fragments = TemplatePageUtils.ParseTemplatePage("{{ arg | filter(' 1) }}");
                Assert.Fail("should throw");
            }
            catch (ArgumentException e)
            {
                e.Message.Print();
            }

            try
            {
                var fragments = TemplatePageUtils.ParseTemplatePage("square = {{ 'square-partial | partial({ ten }) }}");
                Assert.Fail("should throw");
            }
            catch (ArgumentException e)
            {
                e.Message.Print();
            }

            try
            {
                var fragments = TemplatePageUtils.ParseTemplatePage("{{ arg | filter({ unterminated:1) }}");
                Assert.Fail("should throw");
            }
            catch (ArgumentException e)
            {
                e.Message.Print();
            }

            try
            {
                var fragments = TemplatePageUtils.ParseTemplatePage("{{ arg | filter([ 1) }}");
                Assert.Fail("should throw");
            }
            catch (ArgumentException e)
            {
                e.Message.Print();
            }
            
        }

        [Test]
        public void Does_remove_new_line_between_var_literals()
        {
            var fragments = TemplatePageUtils.ParseTemplatePage("{{ 'foo' | assignTo: bar }}\n{{ bar }}");
            Assert.That(fragments.Count, Is.EqualTo(2));
            fragments = TemplatePageUtils.ParseTemplatePage("{{ 'foo' | assignTo: bar }}\r\n{{ bar }}");
            Assert.That(fragments.Count, Is.EqualTo(2));

            fragments = TemplatePageUtils.ParseTemplatePage("{{ ['foo'] | do: assign('bar', it) }}\n{{ bar }}");
            Assert.That(fragments.Count, Is.EqualTo(2));
            fragments = TemplatePageUtils.ParseTemplatePage("{{ do: assign('bar', 'foo') }}\n{{ bar }}");
            Assert.That(fragments.Count, Is.EqualTo(2));
            fragments = TemplatePageUtils.ParseTemplatePage("{{ 10 | times | do: assign('bar', 'foo') }}\n{{ bar }}");
            Assert.That(fragments.Count, Is.EqualTo(2));
            fragments = TemplatePageUtils.ParseTemplatePage("{{ 10 | times | do: assign('bar', 'foo') }}\nbar");
            Assert.That(fragments.Count, Is.EqualTo(2));
            var stringFragment = (PageStringFragment) fragments[1];
            Assert.That(stringFragment.Value, Is.EqualTo("bar"));
        }

        [Test]
        public void Can_parse_empty_arguments()
        {
            object value;
            JsBinding binding;
            JsExpression fn;
            
            "fn()".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(((JsExpression)binding).Name, Is.EqualTo("fn"));
            "fn({})".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(((JsExpression)binding).Args.Count, Is.EqualTo(1));
            "fn({ })".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(((JsExpression)binding).Args.Count, Is.EqualTo(1));
            "fn({  })".ToStringSegment().ParseNextToken(out value, out binding);
            Assert.That(((JsExpression)binding).Args.Count, Is.EqualTo(1));
        }


    }
}