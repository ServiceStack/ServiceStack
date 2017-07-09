using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class TemplatePageRawTests
    {
        [Test]
        public async Task Can_generate_html_template_with_layout_in_memory()
        {
            var context = new TemplatePagesContext();

            context.VirtualFiles.AppendFile("_layout.html", @"
<html>
  <title>{{ title }}</title>
</head>
<body>
  {{ page }}
</body>");

            context.VirtualFiles.AppendFile("page.html", @"<h1>{{ title }}</h1>");

            var page = context.Pages.GetOrCreatePage("page");
            var result = new PageResult(page)
            {
                Args =
                {
                    {"title", "The Title"},
                }
            };

            var html = await result.RenderToStringAsync();
            
            Assert.That(html, Is.EqualTo(@"<html>
  <title>The Title</title>
</head>
<body>
  <h1>The Title</h1>
</body>"));
        }

        [Test]
        public async Task Can_generate_markdown_template_with_layout_in_memory()
        {
            var context = new TemplatePagesContext
            {
                PageFormats =
                {
                    new MarkdownPageFormat()
                }
            };
            
            context.VirtualFiles.AppendFile("_layout.md", @"
# {{ title }}

Brackets in Layout < & > 

{{ page }}");

            context.VirtualFiles.AppendFile("page.md",  @"## {{ title }}");

            var page = context.Pages.GetOrCreatePage("page");
            var result = new PageResult(page)
            {
                Args =
                {
                    {"title", "The Title"},
                },
                ContentType = MimeTypes.Html,
                OutputFilters = { MarkdownPageFormat.TransformToHtml },
            };

            var html = await result.RenderToStringAsync();
            
            Assert.That(html.SanitizeNewLines(), Is.EqualTo(@"<h1>The Title</h1>
<p>Brackets in Layout &lt; &amp; &gt; </p>
<h2>The Title</h2>".SanitizeNewLines()));
            
        }

        [Test]
        public async Task Can_generate_markdown_template_with_html_layout_in_memory()
        {
            var context = new TemplatePagesContext
            {
                PageFormats =
                {
                    new MarkdownPageFormat()
                }
            };
            
            context.VirtualFiles.AppendFile("_layout.html", @"
<html>
  <title>{{ title }}</title>
</head>
<body>
  {{ page }}
</body>");

            context.VirtualFiles.AppendFile("page.md",  @"### {{ title }}");

            var page = context.Pages.GetOrCreatePage("page");
            var result = new PageResult(page)
            {
                Args =
                {
                    {"title", "The Title"},
                },
                ContentType = MimeTypes.Html,
                PageFilters = { MarkdownPageFormat.TransformToHtml },
            };

            var html = await result.RenderToStringAsync();
            
            Assert.That(html.SanitizeNewLines(), Is.EqualTo(@"<html>
  <title>The Title</title>
</head>
<body>
  <h3>The Title</h3>

</body>".SanitizeNewLines()));
            
        }

    }
    
    public static class TestUtils
    {
        public static string SanitizeNewLines(this string text) => text.Trim().Replace("\r", "");
    }
}