using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Templates;
using ServiceStack.IO;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    [Page("foreach-code")]
    public class ForEachCodeExample : TemplateCodePage
    {
        public string render(string title, string[] items) => $@"
<h1>{title}</h1>
<ul>
    {items.Map(x => $"<li>{x}</li>").Join("")}        
</ul>
";
    }

    [Page("dir/foreach-code")]
    [PageArg("title", "Dir foreach code")]
    public class DirForEachCodeExample : TemplateCodePage
    {
        public string render(string title, string[] items) => $@"
<h1>{title}</h1>
<ul>
    {items.Map(x => $"<li>{x}</li>").Join("")}        
</ul>
";
    }

    [Page("codepage-dep")]
    public class CodePageWithDep : TemplateCodePage
    {
        public IAppSettings AppSettings { get; set; }

        public string render(string key, string content) => $@"
<h2>{AppSettings.GetString(key)}</h2>
<p>
    {content}
</p>
";
    }

    public class TemplateCodeTests
    {
        [Test]
        public void Can_execute_CodePage()
        {
            var context = new TemplateContext
            {
                Args =
                {
                    ["items"] = new[] {"A", "B", "C"}
                },
                ScanTypes = {typeof(ForEachCodeExample)}
            }.Init();

            var page = context.GetCodePage("foreach-code");
            var html = new PageResult(page)
            {
                Args =
                {
                    ["title"] = "For each code"
                }
            }.Result;

            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<h1>For each code</h1>
<ul>
    <li>A</li><li>B</li><li>C</li>        
</ul>
".NormalizeNewLines()));
        }

        private static void WriteLayouts(TemplateContext context)
        {
            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
<body id=root>
{{ page }}
</body>
</html>
");

            context.VirtualFiles.WriteFile("dir/_layout.html", @"
<html>
<body id=dir>
{{ page }}
</body>
</html>
");
        }

        [Test]
        public void Can_execute_CodePage_with_Layout()
        {
            var context = new TemplateContext
            {
                Args =
                {
                    ["items"] = new[] {"A", "B", "C"}
                },
                ScanTypes = {typeof(ForEachCodeExample)}
            }.Init();

            WriteLayouts(context);

            var page = context.GetCodePage("foreach-code");
            var html = new PageResult(page)
            {
                Args =
                {
                    ["title"] = "For each code"
                }
            }.Result;

            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=root>

<h1>For each code</h1>
<ul>
    <li>A</li><li>B</li><li>C</li>        
</ul>

</body>
</html>
".NormalizeNewLines()));
        }

        [Test]
        public void Can_execute_nested_CodePage_with_Layout()
        {
            var context = new TemplateContext
            {
                Args =
                {
                    ["items"] = new[] {"A", "B", "C"}
                },
                ScanTypes = {typeof(DirForEachCodeExample)}
            }.Init();

            WriteLayouts(context);

            var page = context.GetCodePage("dir/foreach-code");
            var html = new PageResult(page).Result;

            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=dir>

<h1>Dir foreach code</h1>
<ul>
    <li>A</li><li>B</li><li>C</li>        
</ul>

</body>
</html>
".NormalizeNewLines()));
        }

        [Test]
        public void Can_execute_CodePage_with_Dep()
        {
            var context = new TemplateContext
            {
                ScanTypes = {typeof(CodePageWithDep)}
            }.Init();

            WriteLayouts(context);

            context.Container.AddSingleton<IAppSettings>(() => new SimpleAppSettings(new Dictionary<string, string> {
                ["foo"] = "bar"
            }));

            var page = context.GetCodePage("codepage-dep");
            page.Args["content"] = "The Content";
            
            var html = new PageResult(page)
            {
                Args =
                {
                    ["key"] = "foo"
                }
            }.Result;

            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=root>

<h2>bar</h2>
<p>
    The Content
</p>

</body>
</html>
".NormalizeNewLines()));
        }
    }
}