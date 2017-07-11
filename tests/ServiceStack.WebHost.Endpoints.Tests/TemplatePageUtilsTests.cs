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
            Assert.That(varFragment2.Name, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterCommands.Length, Is.EqualTo(0));
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
            Assert.That(varFragment2.Name, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterCommands.Length, Is.EqualTo(1));
            Assert.That(varFragment2.FilterCommands[0].Name, Is.EqualTo("filter"));
            Assert.That(varFragment2.FilterCommands[0].Args.Count, Is.EqualTo(0));
            Assert.That(strFragment3.Value, Is.EqualTo("</h1>"));

            fragments = TemplatePageUtils.ParseTemplatePage("<h1>{{ title | filter() }}</h1>");

            varFragment2 = fragments[1] as PageVariableFragment;
            Assert.That(varFragment2.OriginalText, Is.EqualTo("{{ title | filter() }}"));
            Assert.That(varFragment2.Name, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterCommands.Length, Is.EqualTo(1));
            Assert.That(varFragment2.FilterCommands[0].Name, Is.EqualTo("filter"));
            Assert.That(varFragment2.FilterCommands[0].Args.Count, Is.EqualTo(0));
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
            Assert.That(varFragment2.Name, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterCommands.Length, Is.EqualTo(0));

            fragments = TemplatePageUtils.ParseTemplatePage("<h1>{{title|filter}}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            strFragment1 = fragments[0] as PageStringFragment;
            varFragment2 = fragments[1] as PageVariableFragment;
            strFragment3 = fragments[2] as PageStringFragment;

            Assert.That(strFragment1.Value, Is.EqualTo("<h1>"));
            Assert.That(varFragment2.OriginalText, Is.EqualTo("{{title|filter}}"));
            Assert.That(varFragment2.Name, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterCommands.Length, Is.EqualTo(1));
            Assert.That(varFragment2.FilterCommands[0].Name, Is.EqualTo("filter"));
            Assert.That(varFragment2.FilterCommands[0].Args.Count, Is.EqualTo(0));
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
            Assert.That(varFragment2.Name, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterCommands.Length, Is.EqualTo(1));
            Assert.That(varFragment2.FilterCommands[0].Name, Is.EqualTo("filter"));
            Assert.That(varFragment2.FilterCommands[0].Args.Count, Is.EqualTo(1));
            Assert.That(varFragment2.FilterCommands[0].Args[0], Is.EqualTo("1"));
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
            Assert.That(varFragment2.Name, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterCommands.Length, Is.EqualTo(1));
            Assert.That(varFragment2.FilterCommands[0].Name, Is.EqualTo("filter"));
            Assert.That(varFragment2.FilterCommands[0].Args.Count, Is.EqualTo(5));
            Assert.That(varFragment2.FilterCommands[0].Args[0], Is.EqualTo("1"));
            Assert.That(varFragment2.FilterCommands[0].Args[1], Is.EqualTo("2.2"));
            Assert.That(varFragment2.FilterCommands[0].Args[2], Is.EqualTo("'a'"));
            Assert.That(varFragment2.FilterCommands[0].Args[3], Is.EqualTo("\"b\""));
            Assert.That(varFragment2.FilterCommands[0].Args[4], Is.EqualTo("true"));
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
            Assert.That(varFragment2.Name, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterCommands.Length, Is.EqualTo(3));

            Assert.That(varFragment2.FilterCommands[0].Name, Is.EqualTo("filter1"));
            Assert.That(varFragment2.FilterCommands[0].Args.Count, Is.EqualTo(0));

            Assert.That(varFragment2.FilterCommands[1].Name, Is.EqualTo("filter2"));
            Assert.That(varFragment2.FilterCommands[1].Args.Count, Is.EqualTo(1));
            Assert.That(varFragment2.FilterCommands[2].Args[0], Is.EqualTo("1"));

            Assert.That(varFragment2.FilterCommands[2].Name, Is.EqualTo("filter3"));
            Assert.That(varFragment2.FilterCommands[2].Args.Count, Is.EqualTo(5));
            Assert.That(varFragment2.FilterCommands[2].Args[0], Is.EqualTo("1"));
            Assert.That(varFragment2.FilterCommands[2].Args[1], Is.EqualTo("2.2"));
            Assert.That(varFragment2.FilterCommands[2].Args[2], Is.EqualTo("'a'"));
            Assert.That(varFragment2.FilterCommands[2].Args[3], Is.EqualTo("\"b\""));
            Assert.That(varFragment2.FilterCommands[2].Args[4], Is.EqualTo("true"));

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
            Assert.That(varFragment2.Name, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterCommands.Length, Is.EqualTo(1));
            Assert.That(varFragment2.FilterCommands[0].Name, Is.EqualTo("filter1"));
            Assert.That(varFragment2.FilterCommands[0].Args.Count, Is.EqualTo(0));

            Assert.That(strFragment3.Value, Is.EqualTo("</h1>\n<p>"));

            Assert.That(varFragment4.OriginalText, Is.EqualTo("{{ content | filter2(a) }}"));
            Assert.That(varFragment4.Name, Is.EqualTo("content"));
            Assert.That(varFragment4.FilterCommands.Length, Is.EqualTo(1));
            Assert.That(varFragment4.FilterCommands[0].Name, Is.EqualTo("filter2"));
            Assert.That(varFragment4.FilterCommands[0].Args.Count, Is.EqualTo(1));
            Assert.That(varFragment4.FilterCommands[0].Args[0], Is.EqualTo("a"));

            Assert.That(strFragment5.Value, Is.EqualTo("</p>"));
        }

        [Test]
        public void Can_parse_next_token()
        {
            StringSegment name;
            object value;
            Command cmd;

            "a".ToStringSegment().ParseNextToken(out name, out value, out cmd);
            Assert.That(name, Is.EqualTo("a"));
            "'a'".ToStringSegment().ParseNextToken(out name, out value, out cmd);
            Assert.That(value, Is.EqualTo("a"));
            "\"a\"".ToStringSegment().ParseNextToken(out name, out value, out cmd);
            Assert.That(value, Is.EqualTo("a"));
            "1".ToStringSegment().ParseNextToken(out name, out value, out cmd);
            Assert.That(value, Is.EqualTo(1));
            "100".ToStringSegment().ParseNextToken(out name, out value, out cmd);
            Assert.That(value, Is.EqualTo(100));
            "100.0".ToStringSegment().ParseNextToken(out name, out value, out cmd);
            Assert.That(value, Is.EqualTo(100d));
            "1.0E+2".ToStringSegment().ParseNextToken(out name, out value, out cmd);
            Assert.That(value, Is.EqualTo(100d));
            "1e+2".ToStringSegment().ParseNextToken(out name, out value, out cmd);
            Assert.That(value, Is.EqualTo(100d));
            "true".ToStringSegment().ParseNextToken(out name, out value, out cmd);
            Assert.That(value, Is.True);
            "false".ToStringSegment().ParseNextToken(out name, out value, out cmd);
            Assert.That(value, Is.False);
            "null".ToStringSegment().ParseNextToken(out name, out value, out cmd);
            Assert.That(value, Is.EqualTo(NullValue.Instance));
        }

    }
}