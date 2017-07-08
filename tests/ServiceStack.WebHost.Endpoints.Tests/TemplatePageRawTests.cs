using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class TemplatePageRawTests
    {
        [Test]
        public async Task Can_generate_template_with_layout_in_memory()
        {
            var context = new TemplatePagesContext
            {
                VirtualFileSources = new MemoryVirtualFiles(),
            };

            context.VirtualFileSources.AppendFile("_layout.html", @"
<html>
  <title>{{ title }}</title>
</head>
<body>
  {{ body }}
</body>");

            context.VirtualFileSources.AppendFile("page.html", @"<h1>{{ title }}</h1>");

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

    }
}