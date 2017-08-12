using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.IO;
using ServiceStack.Templates;
using ServiceStack.IO;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    [Route("/products/{Id}")]
    public class GetProduct
    {
        public int Id { get; set; }
    }

    [Route("/existing-page")]
    public class OverrideExistingPage
    {
        public string RequestVar { get; set; }
    }
    
    public class TemplatePagesService : Service
    {
        public ITemplatePages Pages { get; set; }
        
        public object AnyHtml(GetProduct request)
        {
            return new PageResult(Pages.GetPage("product-view"))
            {
                Model = request,
                LayoutPage = Pages.GetPage("product-layout"),
            };
        }

        public object Any(OverrideExistingPage request)
        {
            return new PageResult(Pages.GetPage("override-page"))
            {
                Model = request,
                Args =
                {
                    { "title", "Service Title" }
                },
                LayoutPage = Pages.GetPage("override-layout"),
            };
        }
    }

    class Model
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TemplatePageTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(nameof(TemplatePageTests), typeof(TemplatePagesService).GetAssembly()) { }

            public override void Configure(Container container)
            {
                Plugins.Add(new TemplatePagesFeature());
            }

            static readonly Dictionary<string,string> HtmlFiles = new Dictionary<string, string>
            {
                { "_layout.html", "<html><head><title>{{ title }}</title></head><body id='layout'>{{ page }}</body></html>" },
                { "alt-layout.html", "<html><head><title>{{ title }}</title></head><body id='alt-layout'>{{ page }}</body></html>" },
                { "override-layout.html", "<html><head><title>{{ title }}</title></head><body id='override-layout'>{{ page }}</body></html>" },
                { "root-static-page.html", "<h1>/root-static page!</h1>" },
                { "full-static-page.html", "<html><head><title>Full Page</title></head><body><h1>Full Page</h1></body></html>" },
                { "existing-page.html", "<h1>Existing Page</h1>" },
                { "override-page.html", @"
<!--
layout: alt-layout
title: Override Title
-->
<h1>Override Page</h1>" },
                { "noprefix-page.html", @"
<!--
layout: alt-layout
-->
<h1>/noprefix page!</h1>" },
                { "dir/alt-layout.html", "<html><head><title>{{ title }}</title></head><body id='dir-alt-layout'>{{ page }}</body></html>" },
                { "dir/index.html", @"
<!--
layout: alt-layout
title: no prefix @ /dir
-->
<h1>/dir/noprefix page!</h1>" },
                { "variable-layout-page.html", @"
<!--
layout: alt-layout.html
title: Variable Layout
-->

<h1>Variable Page</h1>" },
                { "htmlencode-layout.html", @"
<!--
layoutvar: layoutvar(< & > "" ')
-->
<html><head><title>{{ title }}</title></head><body id='htmlencode-layout'>{{ page }}<p>{{ layoutvar }}</p></body></html>" },
                { "htmlencode-page.html", @"
<!--
layout: htmlencode-layout.html
title: We encode < & >
-->
<h1>/htmlencode-page!</h1>" },
            };

            public override List<IVirtualPathProvider> GetVirtualFileSources()
            {
                var existingProviders = base.GetVirtualFileSources();
                var memFs = new MemoryVirtualFiles();

                foreach (var entry in HtmlFiles)
                {
                    memFs.AppendFile(entry.Key, entry.Value);
                }

                existingProviders.Insert(0, memFs);
                return existingProviders;
            }
        }

        private readonly ServiceStackHost appHost;
        public TemplatePageTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown] public void OneTimeTearDown() => appHost.Dispose();

        TemplatePage CreatePage(IVirtualFile file) =>
            new TemplatePage(appHost.GetPlugin<TemplatePagesFeature>(), file);

        [Test]
        public void Request_for_partial_page_returns_complete_page_with_default_layout()
        {
            var html = Config.ListeningOn.CombineWith("root-static-page.html")
                .GetStringFromUrl(accept: MimeTypes.Html);

            Assert.That(html, Does.StartWith("<html><head><title>"));
            Assert.That(html, Does.Contain("id='layout'"));
            Assert.That(html, Does.Contain("<h1>/root-static page!</h1>"));
        }

        [Test]
        public void Request_for_noprefix_page_returns_alt_layout()
        {
            var html = Config.ListeningOn.CombineWith("noprefix-page")
                .GetStringFromUrl(accept: MimeTypes.Html);

            Assert.That(html, Does.StartWith("<html><head><title>"));
            Assert.That(html, Does.Contain("id='alt-layout'"));
            Assert.That(html, Does.Contain("<h1>/noprefix page!</h1>"));
        }

        [Test]
        public void Request_for_variable_page_returns_complete_page_with_alt_layout()
        {
            var html = Config.ListeningOn.CombineWith("variable-layout-page.html")
                .GetStringFromUrl(accept: MimeTypes.Html);

            Assert.That(html, Does.StartWith("<html><head><title>Variable Layout</title>"));
            Assert.That(html, Does.Contain("id='alt-layout'"));
            Assert.That(html, Does.Contain("<h1>Variable Page</h1>"));
        }

        [Test]
        public void Request_for_htmlencode_pages_returns_htmlencoded_variables()
        {
            var html = Config.ListeningOn.CombineWith("htmlencode-page.html")
                .GetStringFromUrl(accept: MimeTypes.Html);

            Assert.That(html, Does.StartWith("<html><head><title>We encode &lt; &amp; &gt;</title>"));
            Assert.That(html, Does.Contain("id='htmlencode-layout'"));
            Assert.That(html, Does.Contain("<h1>/htmlencode-page!</h1>"));
            Assert.That(html, Does.Contain("<p>layoutvar(&lt; &amp; &gt; &quot; &#39;)</p>"));
        }

        [Test]
        public void Request_for_dir_index_page_using_supported_conventions()
        {
            var htmlOrig = Config.ListeningOn.CombineWith("dir/index.html")
                .GetStringFromUrl(accept: MimeTypes.Html);
            
            Assert.That(htmlOrig, Does.StartWith("<html><head><title>no prefix @ /dir</title>"));
            Assert.That(htmlOrig, Does.Contain("id='dir-alt-layout'"));
            Assert.That(htmlOrig, Does.Contain("<h1>/dir/noprefix page!</h1>"));
            
            var html = Config.ListeningOn.CombineWith("dir/index")
                .GetStringFromUrl(accept: MimeTypes.Html);
            Assert.That(html, Is.EqualTo(htmlOrig));
            
            html = Config.ListeningOn.CombineWith("dir/")
                .GetStringFromUrl(accept: MimeTypes.Html);
            Assert.That(html, Is.EqualTo(htmlOrig));
            
            html = Config.ListeningOn.CombineWith("dir")
                .GetStringFromUrl(accept: MimeTypes.Html);
            Assert.That(html, Is.EqualTo(htmlOrig));
        }

#if NET45
        [Test]
        public void Request_for_dir_index_page_without_trailing_slash_auto_redirects()
        {
            Config.ListeningOn.CombineWith("dir")
                .GetStringFromUrl(accept: MimeTypes.Html, 
                    requestFilter: req => req.AllowAutoRedirect = false,
                    responseFilter: res =>
                    {
                        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.MovedPermanently));
                        Assert.That(res.Headers[HttpHeaders.Location], Is.EqualTo(Config.ListeningOn.CombineWith("dir/")));
                    });
        }
#endif

        [Test]
        public void Request_for_page_with_underscore_prefix_is_forbidden()
        {
            try
            {
                Config.ListeningOn.CombineWith("_layout.html")
                    .GetStringFromUrl(accept: MimeTypes.Html);
                
                Assert.Fail("Should throw");
            }
            catch (WebException ex)
            {
                Assert.That(((HttpWebResponse)ex.Response).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            }
            
            try
            {
                Config.ListeningOn.CombineWith("_layout")
                    .GetStringFromUrl(accept: MimeTypes.Html);
                
                Assert.Fail("Should throw");
            }
            catch (WebException ex)
            {
                Assert.That(((HttpWebResponse)ex.Response).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            }
        }

        [Test]
        public void Request_for_existing_page_can_be_overridden_by_Service()
        {
            var html = Config.ListeningOn.CombineWith("existing-page")
                .GetStringFromUrl(accept: MimeTypes.Html);

            Assert.That(html, Does.StartWith("<html><head><title>Service Title</title>"));
            Assert.That(html, Does.Contain("id='override-layout'"));
            Assert.That(html, Does.Contain("<h1>Override Page</h1>"));
        }

        [Test]
        public async Task Can_parse_page_with_page_variables()
        {
            var file = HostContext.AppHost.VirtualFileSources.GetFile("variable-layout-page.html");
            
            var page = await CreatePage(file).Init();

            Assert.That(page.Args["layout"], Is.EqualTo("alt-layout.html"));
            Assert.That(page.Args["title"], Is.EqualTo("Variable Layout"));
            Assert.That(((PageStringFragment)page.PageFragments[0]).Value, Is.EqualTo("<h1>Variable Page</h1>"));
        }

        [Test]
        public async Task Can_parse_template_with_body_variable()
        {
            var file = HostContext.AppHost.VirtualFileSources.GetFile("_layout.html");
            
            var page = await CreatePage(file).Init();

            Assert.That(page.PageFragments.Count, Is.EqualTo(5));
            var strFragment1 = (PageStringFragment)page.PageFragments[0];
            var varFragment2 = (PageVariableFragment)page.PageFragments[1];
            var strFragment3 = (PageStringFragment)page.PageFragments[2];
            var varFragment4 = (PageVariableFragment)page.PageFragments[3];
            var strFragment5 = (PageStringFragment)page.PageFragments[4];

            Assert.That(strFragment1.Value, Is.EqualTo("<html><head><title>"));
            Assert.That(varFragment2.Binding, Is.EqualTo("title"));
            Assert.That(strFragment3.Value, Is.EqualTo("</title></head><body id='layout'>"));
            Assert.That(varFragment4.Binding, Is.EqualTo("page"));
            Assert.That(strFragment5.Value, Is.EqualTo("</body></html>"));
        }

        [Test]
        public void Does_limit_file_changes_checks_to_specified_time()
        {
            var context = new TemplateContext
            {
                DebugMode = false,
                CheckForModifiedPagesAfter = TimeSpan.FromMilliseconds(100)
            }.Init();
            
            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
<body id=original>
{{ page }}
</body>
</html>
");
            context.VirtualFiles.WriteFile("page.html", "<h1>Original Contents</h1>");
            
            var output = new PageResult(context.GetPage("page")).Result;            
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=original>
<h1>Original Contents</h1>
</body>
</html>
".NormalizeNewLines()));

            context.VirtualFiles.WriteFile("_layout.html",
                context.VirtualFiles.GetFile("_layout.html").ReadAllText().Replace("original", "updated"));
            context.VirtualFiles.WriteFile("page.html",
                context.VirtualFiles.GetFile("page.html").ReadAllText().Replace("Original", "Updated"));
            
            //Should return same contents when within CheckForModifiedPagesAfter
            output = new PageResult(context.GetPage("page")).Result;            
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=original>
<h1>Original Contents</h1>
</body>
</html>
".NormalizeNewLines()));
            
            Thread.Sleep(300);
            
            //Should render updated content
            output = new PageResult(context.GetPage("page")).Result;            
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=updated>
<h1>Updated Contents</h1>
</body>
</html>
".NormalizeNewLines()));
        }

        [Test]
        public void Never_checks_for_changes_if_CheckForModifiedPagesAfter_is_null()
        {
            var context = new TemplateContext
            {
                DebugMode = false,
                CheckForModifiedPagesAfter = null
            }.Init();
            
            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
<body id=original>
{{ page }}
</body>
</html>
");
            context.VirtualFiles.WriteFile("page.html", "<h1>Original Contents</h1>");
            
            var output = new PageResult(context.GetPage("page")).Result;            
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=original>
<h1>Original Contents</h1>
</body>
</html>
".NormalizeNewLines()));

            context.VirtualFiles.WriteFile("_layout.html",
                context.VirtualFiles.GetFile("_layout.html").ReadAllText().Replace("original", "updated"));
            context.VirtualFiles.WriteFile("page.html",
                context.VirtualFiles.GetFile("page.html").ReadAllText().Replace("Original", "Updated"));
            
            Thread.Sleep(150);

            output = new PageResult(context.GetPage("page")).Result;            
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=original>
<h1>Original Contents</h1>
</body>
</html>
".NormalizeNewLines()));
        }

    }
}