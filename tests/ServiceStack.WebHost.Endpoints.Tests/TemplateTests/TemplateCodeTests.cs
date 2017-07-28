using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Templates;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    [Page("code-layout")]
    [PageArg("title", "Code Layout Title")]
    public class CodeLayout : TemplateCodePage
    {
        public string render(string title, string content) => $@"
<h1>{title}</h1>
<p>
    {content}
</p>
";
    }

    [Page("foreach-code")]
    public class ForEachCodeExample : TemplateCodePage
    {
        public IAppSettings AppSettings { get; set; }

        public string render(string title, string[] items) => $@"
<h1>{title}</h1>
<ul>
    {items.Map(x => $"<li>{x}</li>").Join("")}        
</ul>
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
                    ["items"] = new[] { "A", "B", "C" }
                },
                ScanTypes = { typeof(ForEachCodeExample) }
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
        
        [Test]
        public void Can_execute_CodePage_with_Layout()
        {
            var context = new TemplateContext
            {
                Args =
                {
                    ["items"] = new[] { "A", "B", "C" }
                },
                ScanTypes = { typeof(ForEachCodeExample) }
            }.Init();
            
            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
<body>
{{ page }}
</body>
</html>
");

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
<body>

<h1>For each code</h1>
<ul>
    <li>A</li><li>B</li><li>C</li>        
</ul>

</body>
</html>
".NormalizeNewLines()));
        }
    }
}