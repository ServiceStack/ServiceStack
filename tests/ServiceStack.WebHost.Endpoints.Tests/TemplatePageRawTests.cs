using System;
using System.IO;
using System.Threading;
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

            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
  <title>{{ title }}</title>
</head>
<body>
  {{ page }}
</body>");

            context.VirtualFiles.WriteFile("page.html", @"<h1>{{ title }}</h1>");

            var page = context.GetPage("page");
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
            
            context.VirtualFiles.WriteFile("_layout.md", @"
# {{ title }}

Brackets in Layout < & > 

{{ page }}");

            context.VirtualFiles.WriteFile("page.md",  @"## {{ title }}");

            var page = context.GetPage("page");
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
            
            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
  <title>{{ title }}</title>
</head>
<body>
  {{ page }}
</body>");

            context.VirtualFiles.WriteFile("page.md",  @"### {{ title }}");

            var page = context.GetPage("page");
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

        [Test]
        public async Task Does_explode_Model_properties_into_scope()
        {
            var context = new TemplatePagesContext();
            
            context.VirtualFiles.WriteFile("page.html", @"Id: {{ Id }}, Name: {{ Name }}");
            
            var result = await new PageResult(context.GetPage("page"))
            {
                Model = new Model { Id = 1, Name = "<foo>" }
            }.RenderToStringAsync();
            
            Assert.That(result, Is.EqualTo("Id: 1, Name: &lt;foo&gt;"));
        }

        [Test]
        public async Task Does_explode_Model_properties_of_anon_object_into_scope()
        {
            var context = new TemplatePagesContext();
            
            context.VirtualFiles.WriteFile("page.html", @"Id: {{ Id }}, Name: {{ Name }}");
            
            var result = await new PageResult(context.GetPage("page"))
            {
                Model = new { Id = 1, Name = "<foo>" }
            }.RenderToStringAsync();
            
            Assert.That(result, Is.EqualTo("Id: 1, Name: &lt;foo&gt;"));
        }

        [Test]
        public async Task Does_reload_modified_page_contents_in_DebugMode()
        {
            var context = new TemplatePagesContext
            {
                DebugMode = true, //default
            };
            
            context.VirtualFiles.WriteFile("page.html", "<h1>Original</h1>");
            Assert.That(await new PageResult(context.GetPage("page")).RenderToStringAsync(), Is.EqualTo("<h1>Original</h1>"));

            await Task.Delay(1); //Memory VFS is too fast!
            
            context.VirtualFiles.WriteFile("page.html", "<h1>Updated</h1>");
            Assert.That(await new PageResult(context.GetPage("page")).RenderToStringAsync(), Is.EqualTo("<h1>Updated</h1>"));
        }

        [Test]
        public void Context_Throws_FileNotFoundException_when_page_does_not_exist()
        {
            var context = new TemplatePagesContext();

            Assert.That(context.Pages.GetPage("not-exists.html"), Is.Null);

            try
            {
                var page = context.GetPage("not-exists.html");
                Assert.Fail("Should throw");
            }
            catch (FileNotFoundException e)
            {
                e.ToString().Print();
            }
        }

        class MyFilter : TemplateFilter
        {
            public string echo(string text) => $"{text} {text}";
            public string greetArg(string key) => $"Hello {Context.Args[key]}";
        }

        [Test]
        public async Task Does_use_custom_filter()
        {
            var context = new TemplatePagesContext
            {
                Args =
                {
                    ["contextArg"] = "foo"
                },                
            }.Init();
            
            context.VirtualFiles.WriteFile("page.html", "<h1>{{ 'hello' | echo }}</h1>");
            var result = await new PageResult(context.GetPage("page"))
            {
                TemplateFilters = { new MyFilter() }
            }.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("<h1>hello hello</h1>"));

            context.VirtualFiles.WriteFile("page-greet.html", "<h1>{{ 'contextArg' | greetArg }}</h1>");
            result = await new PageResult(context.GetPage("page-greet"))
            {
                TemplateFilters = { new MyFilter() }
            }.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("<h1>Hello foo</h1>"));
        }

    }
    
    public static class TestUtils
    {
        public static string SanitizeNewLines(this string text) => text.Trim().Replace("\r", "");
    }
}