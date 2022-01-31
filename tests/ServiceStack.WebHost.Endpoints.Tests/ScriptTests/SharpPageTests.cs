using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
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
    
    public class SharpPagesService : Service
    {
        public ISharpPages Pages { get; set; }
        
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

    public class SharpPageTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(nameof(SharpPageTests), typeof(SharpPagesService).Assembly) { }

            public override void Configure(Container container)
            {
                Plugins.Add(new SharpPagesFeature());
                
                AfterInitCallbacks.Add(host => {
                    var memFs = VirtualFileSources.GetMemoryVirtualFiles();
                    foreach (var entry in HtmlFiles)
                    {
                        memFs.AppendFile(entry.Key, entry.Value);
                    }
                });
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
                { "variable-layout-page-space-delim.html", @"
<!--
layout alt-layout.html
title Variable Layout
files.config {AccessKey:$AWS_S3_ACCESS_KEY}
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
        }

        private readonly ServiceStackHost appHost;
        public SharpPageTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown] public void OneTimeTearDown() => appHost.Dispose();

        SharpPage CreatePage(IVirtualFile file) =>
            new SharpPage(appHost.GetPlugin<SharpPagesFeature>(), file);

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

#if NETFX
        [Test]
        public void Request_for_dir_index_page_without_trailing_slash_auto_redirects()
        {
            Config.ListeningOn.CombineWith("dir")
                .GetStringFromUrl(accept: MimeTypes.Html, 
                    requestFilter: req => req.AllowAutoRedirect = false,
                    responseFilter: res =>
                    {
                        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
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
            catch (Exception ex)
            {
                Assert.That(ex.GetStatus(), Is.EqualTo(HttpStatusCode.Forbidden));
            }
            
            try
            {
                Config.ListeningOn.CombineWith("_layout")
                    .GetStringFromUrl(accept: MimeTypes.Html);
                
                Assert.Fail("Should throw");
            }
            catch (Exception ex)
            {
                Assert.That(ex.GetStatus(), Is.EqualTo(HttpStatusCode.Forbidden));
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
            Assert.That(((PageStringFragment)page.PageFragments[0]).Value.ToString(), Is.EqualTo("<h1>Variable Page</h1>"));
        }

        [Test]
        public async Task Can_parse_page_with_page_variables_without_colon_separators()
        {
            var file = HostContext.AppHost.VirtualFileSources.GetFile("variable-layout-page-space-delim.html");
            
            var page = await CreatePage(file).Init();

            Assert.That(page.Args["layout"], Is.EqualTo("alt-layout.html"));
            Assert.That(page.Args["title"], Is.EqualTo("Variable Layout"));
            Assert.That(page.Args["files.config"], Is.EqualTo("{AccessKey:$AWS_S3_ACCESS_KEY}"));
            Assert.That(((PageStringFragment)page.PageFragments[0]).Value.ToString(), Is.EqualTo("<h1>Variable Page</h1>"));
        }

        [Test]
        public async Task Can_parse_template_with_body_variable()
        {
            var file = HostContext.AppHost.VirtualFileSources.GetFile("_layout.html");
            
            var page = await CreatePage(file).Init();

            Assert.That(page.PageFragments.Length, Is.EqualTo(5));
            var strFragment1 = (PageStringFragment)page.PageFragments[0];
            var varFragment2 = (PageVariableFragment)page.PageFragments[1];
            var strFragment3 = (PageStringFragment)page.PageFragments[2];
            var varFragment4 = (PageVariableFragment)page.PageFragments[3];
            var strFragment5 = (PageStringFragment)page.PageFragments[4];

            Assert.That(strFragment1.Value.ToString(), Is.EqualTo("<html><head><title>"));
            Assert.That(varFragment2.Binding, Is.EqualTo("title"));
            Assert.That(strFragment3.Value.ToString(), Is.EqualTo("</title></head><body id='layout'>"));
            Assert.That(varFragment4.Binding, Is.EqualTo("page"));
            Assert.That(strFragment5.Value.ToString(), Is.EqualTo("</body></html>"));
        }

        [NUnit.Framework.Ignore("Flaky when run in suite on .NET Framework only, passes when run on its own or on .NET Core")]
        [Test]
        public void Does_limit_file_changes_checks_to_specified_time()
        {
            var context = new ScriptContext
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

            var pageResult = new PageResult(context.GetPage("page"));
            Assert.That(pageResult.ResultOutput, Is.Null);

            var output = pageResult.Result;            
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
            var context = new ScriptContext
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

        [Test]
        public void Does_find_last_modified_file_in_page()
        {
            var context = new ScriptContext
            {
                ScriptMethods = { new ProtectedScripts() }
            }.Init();

            context.VirtualFiles.WriteFile("_layout.html", "layout {{ page }} {{ 'layout-partial' |> partial }}  {{ 'layout-file.txt' |> includeFile }} ");
            context.VirtualFiles.WriteFile("page.html", "page: partial {{ 'root-partial' |> partial }}, file {{ 'file.txt' |> includeFile }}, selectParital: {{ true |> selectPartial: select-partial }}");
            context.VirtualFiles.WriteFile("root-partial.html", "root-partial: partial {{ 'inner-partial' |> partial }}, partial-file {{ 'partial-file.txt' |> includeFile }}");
            context.VirtualFiles.WriteFile("file.txt", "file.txt");
            context.VirtualFiles.WriteFile("inner-partial.html", "inner-partial.html");
            context.VirtualFiles.WriteFile("partial-file.txt", "partial-file.txt");
            context.VirtualFiles.WriteFile("select-partial.html", "select-partial.html");
            context.VirtualFiles.WriteFile("layout-partial.html", "layout-partial.html");
            context.VirtualFiles.WriteFile("layout-file.txt", "layout-file.txt");

            ((InMemoryVirtualFile)context.VirtualFiles.GetFile("page.html")).FileLastModified = new DateTime(2001, 01, 01);
            ((InMemoryVirtualFile)context.VirtualFiles.GetFile("_layout.html")).FileLastModified = new DateTime(2001, 01, 02);
            ((InMemoryVirtualFile)context.VirtualFiles.GetFile("root-partial.html")).FileLastModified = new DateTime(2001, 01, 03);
            ((InMemoryVirtualFile)context.VirtualFiles.GetFile("file.txt")).FileLastModified = new DateTime(2001, 01, 04);
            ((InMemoryVirtualFile)context.VirtualFiles.GetFile("inner-partial.html")).FileLastModified = new DateTime(2001, 01, 05);
            ((InMemoryVirtualFile)context.VirtualFiles.GetFile("partial-file.txt")).FileLastModified = new DateTime(2001, 01, 06);
            ((InMemoryVirtualFile)context.VirtualFiles.GetFile("select-partial.html")).FileLastModified = new DateTime(2001, 01, 07);
            ((InMemoryVirtualFile)context.VirtualFiles.GetFile("layout-partial.html")).FileLastModified = new DateTime(2001, 01, 08);
            ((InMemoryVirtualFile)context.VirtualFiles.GetFile("layout-file.txt")).FileLastModified = new DateTime(2001, 01, 09);

            var page = context.Pages.GetPage("page").Init().Result;
            context.Pages.GetPage("root-partial").Init().Wait();

            var lastModified = context.Pages.GetLastModified(page);
            Assert.That(lastModified, Is.EqualTo(new DateTime(2001, 01, 09)));
        }

        public class AsyncFilters : ScriptMethods
        {
            public async Task<object> reverseString(string text)
            {
                await Task.Yield();
                var chars = text.ToCharArray();
                Array.Reverse(chars);
                return new string(chars);            
            }
        }

        [Test]
        public void Can_call_async_filters()
        {
            var context = new ScriptContext
            {
                ScriptMethods = { new AsyncFilters() }
            }.Init();

            var output = context.EvaluateScript("{{ 'foo' |> reverseString }}");
            Assert.That(output, Is.EqualTo("oof"));
        }

        [Test]
        public void Can_ignore_page_template_and_layout_with_Page_args()
        {
            var context = new ScriptContext().Init();
            
            context.VirtualFiles.WriteFile("_layout.html", "<html><body>{{ page }}</body></html>");
            context.VirtualFiles.WriteFile("page.html", "<pre>{{ 12.34 |> currency }}</pre>");
            context.VirtualFiles.WriteFile("page-nolayout.html", "<!--\nlayout: none\n-->\n<pre>{{ 12.34 |> currency }}</pre>");
            context.VirtualFiles.WriteFile("ignore-page.html", "<!--\nignore: page\n-->\n<pre>{{ 12.34 |> currency }}</pre>");
            context.VirtualFiles.WriteFile("ignore-template.html", "<!--\nignore: template\n-->\n<pre>{{ 12.34 |> currency }}</pre>");
            
            Assert.That(new PageResult(context.GetPage("page")).Result, Is.EqualTo("<html><body><pre>$12.34</pre></body></html>"));
            Assert.That(new PageResult(context.GetPage("page-nolayout")).Result, Is.EqualTo("<pre>$12.34</pre>"));
            Assert.That(new PageResult(context.GetPage("ignore-page")).Result, Is.EqualTo("<html><body><pre>{{ 12.34 |> currency }}</pre></body></html>"));
            Assert.That(new PageResult(context.GetPage("ignore-template")).Result, Is.EqualTo("<pre>{{ 12.34 |> currency }}</pre>"));
        }

        [Test]
        public void Can_comment_out_filters()
        {
            var context = new ScriptContext().Init();
            context.VirtualFiles.WriteFile("page.html", "<pre>currency: {{* 12.34 |> currency *}}, date: {{* now *}}</pre>");
            
            Assert.That(new PageResult(context.GetPage("page")).Result, Is.EqualTo("<pre>currency: , date: </pre>"));
        }

        [Test]
        public void Does_preverve_content_after_html_comments()
        {
            var context = new ScriptContext().Init();
            context.VirtualFiles.WriteFile("_layout.html", "<html><body><h1>{{title}}</h1>{{ page }}</body></html>");
            context.VirtualFiles.WriteFile("page.html", "<!--\ntitle:The Title\n--><p>para</p>");

            var html = new PageResult(context.GetPage("page")).Result;
            Assert.That(html, Is.EqualTo("<html><body><h1>The Title</h1><p>para</p></body></html>"));
        }

        [Test]
        public void Can_resolve_hidden_partials_without_prefix()
        {
            var context = new ScriptContext().Init();
            context.VirtualFiles.WriteFile("page.html", "Page {{ 'menu' |> partial }} {{ '_test-partial' |> partial }}");
            context.VirtualFiles.WriteFile("_menu-partial.html", "MENU");
            context.VirtualFiles.WriteFile("_test-partial.html", "TEST");
            
            var result = new PageResult(context.GetPage("page")).Result;
            result.Print();
            
            Assert.That(result, Is.EqualTo("Page MENU TEST"));
        }
    }
}