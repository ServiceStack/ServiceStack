using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class ServerHtmlUtilsTests
    {
        [Test]
        public void Can_parse_template_with_no_vars()
        {
            Assert.That(ServerHtmlUtils.ParseServerHtml("").Count, Is.EqualTo(0));
            var fragments = ServerHtmlUtils.ParseServerHtml("<h1>title</h1>");
            Assert.That(fragments.Count, Is.EqualTo(1));

            var strFragment = fragments[0] as ServerHtmlStringFragment;
            Assert.That(strFragment.Value, Is.EqualTo("<h1>title</h1>"));
        }

        [Test]
        public void Can_parse_template_with_variable()
        {
            var fragments = ServerHtmlUtils.ParseServerHtml("<h1>{{ title }}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            var strFragment1 = fragments[0] as ServerHtmlStringFragment;
            var varFragment2 = fragments[1] as ServerHtmlVariableFragment;
            var strFragment3 = fragments[2] as ServerHtmlStringFragment;

            Assert.That(strFragment1.Value, Is.EqualTo("<h1>"));
            Assert.That(varFragment2.Name, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterCommands, Is.Null);
            Assert.That(strFragment3.Value, Is.EqualTo("</h1>"));
        }

        [Test]
        public void Can_parse_template_with_filter()
        {
            var fragments = ServerHtmlUtils.ParseServerHtml("<h1>{{ title | filter }}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            var strFragment1 = fragments[0] as ServerHtmlStringFragment;
            var varFragment2 = fragments[1] as ServerHtmlVariableFragment;
            var strFragment3 = fragments[2] as ServerHtmlStringFragment;

            Assert.That(strFragment1.Value, Is.EqualTo("<h1>"));
            Assert.That(varFragment2.Name, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterCommands.Count, Is.EqualTo(1));
            Assert.That(varFragment2.FilterCommands[0].Name, Is.EqualTo("filter"));
            Assert.That(varFragment2.FilterCommands[0].Args.Count, Is.EqualTo(0));
            Assert.That(strFragment3.Value, Is.EqualTo("</h1>"));

            fragments = ServerHtmlUtils.ParseServerHtml("<h1>{{ title | filter() }}</h1>");

            varFragment2 = fragments[1] as ServerHtmlVariableFragment;
            Assert.That(varFragment2.Name, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterCommands.Count, Is.EqualTo(1));
            Assert.That(varFragment2.FilterCommands[0].Name, Is.EqualTo("filter"));
            Assert.That(varFragment2.FilterCommands[0].Args.Count, Is.EqualTo(0));
        }

        [Test]
        public void Can_parse_template_with_filter_without_whitespace()
        {
            var fragments = ServerHtmlUtils.ParseServerHtml("<h1>{{title}}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            var strFragment1 = fragments[0] as ServerHtmlStringFragment;
            var varFragment2 = fragments[1] as ServerHtmlVariableFragment;
            var strFragment3 = fragments[2] as ServerHtmlStringFragment;

            Assert.That(strFragment1.Value, Is.EqualTo("<h1>"));
            Assert.That(varFragment2.Name, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterCommands, Is.Null);

            fragments = ServerHtmlUtils.ParseServerHtml("<h1>{{title|filter}}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            strFragment1 = fragments[0] as ServerHtmlStringFragment;
            varFragment2 = fragments[1] as ServerHtmlVariableFragment;
            strFragment3 = fragments[2] as ServerHtmlStringFragment;

            Assert.That(strFragment1.Value, Is.EqualTo("<h1>"));
            Assert.That(varFragment2.Name, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterCommands.Count, Is.EqualTo(1));
            Assert.That(varFragment2.FilterCommands[0].Name, Is.EqualTo("filter"));
            Assert.That(varFragment2.FilterCommands[0].Args.Count, Is.EqualTo(0));
            Assert.That(strFragment3.Value, Is.EqualTo("</h1>"));
        }

        [Test]
        public void Can_parse_template_with_filter_with_arg()
        {
            var fragments = ServerHtmlUtils.ParseServerHtml("<h1>{{ title | filter(1) }}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            var strFragment1 = fragments[0] as ServerHtmlStringFragment;
            var varFragment2 = fragments[1] as ServerHtmlVariableFragment;
            var strFragment3 = fragments[2] as ServerHtmlStringFragment;

            Assert.That(strFragment1.Value, Is.EqualTo("<h1>"));
            Assert.That(varFragment2.Name, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterCommands.Count, Is.EqualTo(1));
            Assert.That(varFragment2.FilterCommands[0].Name, Is.EqualTo("filter"));
            Assert.That(varFragment2.FilterCommands[0].Args.Count, Is.EqualTo(1));
            Assert.That(varFragment2.FilterCommands[0].Args[0], Is.EqualTo("1"));
            Assert.That(strFragment3.Value, Is.EqualTo("</h1>"));
        }

        [Test]
        public void Can_parse_template_with_filter_with_multiple_args()
        {
            var fragments = ServerHtmlUtils.ParseServerHtml("<h1>{{ title | filter(1,2.2,'a',\"b\",true) }}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            var strFragment1 = fragments[0] as ServerHtmlStringFragment;
            var varFragment2 = fragments[1] as ServerHtmlVariableFragment;
            var strFragment3 = fragments[2] as ServerHtmlStringFragment;

            Assert.That(strFragment1.Value, Is.EqualTo("<h1>"));
            Assert.That(varFragment2.Name, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterCommands.Count, Is.EqualTo(1));
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
            var fragments = ServerHtmlUtils.ParseServerHtml("<h1>{{ title | filter1 | filter2(1) | filter3(1,2.2,'a',\"b\",true) }}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            var strFragment1 = fragments[0] as ServerHtmlStringFragment;
            var varFragment2 = fragments[1] as ServerHtmlVariableFragment;
            var strFragment3 = fragments[2] as ServerHtmlStringFragment;

            Assert.That(strFragment1.Value, Is.EqualTo("<h1>"));
            Assert.That(varFragment2.Name, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterCommands.Count, Is.EqualTo(3));

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
            var fragments = ServerHtmlUtils.ParseServerHtml("<h1>{{ title | filter1 }}</h1>\n<p>{{ content | filter2(a) }}</p>");
            Assert.That(fragments.Count, Is.EqualTo(5));

            var strFragment1 = fragments[0] as ServerHtmlStringFragment;
            var varFragment2 = fragments[1] as ServerHtmlVariableFragment;
            var strFragment3 = fragments[2] as ServerHtmlStringFragment;
            var varFragment4 = fragments[3] as ServerHtmlVariableFragment;
            var strFragment5 = fragments[4] as ServerHtmlStringFragment;

            Assert.That(strFragment1.Value, Is.EqualTo("<h1>"));

            Assert.That(varFragment2.Name, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterCommands.Count, Is.EqualTo(1));
            Assert.That(varFragment2.FilterCommands[0].Name, Is.EqualTo("filter1"));
            Assert.That(varFragment2.FilterCommands[0].Args.Count, Is.EqualTo(0));

            Assert.That(strFragment3.Value, Is.EqualTo("</h1>\n<p>"));

            Assert.That(varFragment4.Name, Is.EqualTo("content"));
            Assert.That(varFragment4.FilterCommands.Count, Is.EqualTo(1));
            Assert.That(varFragment4.FilterCommands[0].Name, Is.EqualTo("filter2"));
            Assert.That(varFragment4.FilterCommands[0].Args.Count, Is.EqualTo(1));
            Assert.That(varFragment4.FilterCommands[0].Args[0], Is.EqualTo("a"));

            Assert.That(strFragment5.Value, Is.EqualTo("</p>"));
        }

    }
}