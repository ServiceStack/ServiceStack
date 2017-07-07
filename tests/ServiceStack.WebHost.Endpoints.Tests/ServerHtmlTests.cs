using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests
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
    
    public class ServerHtmlService : Service
    {
        public IHtmlPages HtmlPages { get; set; }
        
        public object AnyHtml(GetProduct request)
        {
            return new HtmlResult(HtmlPages.GetOrCreatePage("product-view"))
            {
                Model = request,
                LayoutPage = HtmlPages.GetOrCreatePage("product-layout"),
            };
        }

        public object Any(OverrideExistingPage request)
        {
            return new HtmlResult(HtmlPages.GetOrCreatePage("override-page"))
            {
                Model = request,
                Args =
                {
                    { "title", "Service Title" }
                },
                LayoutPage = HtmlPages.GetOrCreatePage("override-layout"),
            };
        }
    }

    public class ServerHtmlTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(nameof(ServerHtmlTests), typeof(ServerHtmlService).GetAssembly()) { }

            public override void Configure(Container container)
            {
                Plugins.Add(new ServerHtmlFeature());
            }

            static readonly Dictionary<string,string> HtmlFiles = new Dictionary<string, string>
            {
                { "_layout.html", "<html><head><title>{{ title }}</title></head><body id='layout'>{{ body }}</body></html>" },
                { "alt-layout.html", "<html><head><title>{{ title }}</title></head><body id='alt-layout'>{{ body }}</body></html>" },
                { "override-layout.html", "<html><head><title>{{ title }}</title></head><body id='override-layout'>{{ body }}</body></html>" },
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
                { "dir/alt-layout.html", "<html><head><title>{{ title }}</title></head><body id='dir-alt-layout'>{{ body }}</body></html>" },
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
<html><head><title>{{ title }}</title></head><body id='htmlencode-layout'>{{ body }}<p>{{ layoutvar }}</p></body></html>" },
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
                var memFs = new InMemoryVirtualPathProvider(this);

                foreach (var entry in HtmlFiles)
                {
                    memFs.AppendFile(entry.Key, entry.Value);
                }

                existingProviders.Insert(0, memFs);
                return existingProviders;
            }
        }

        private readonly ServiceStackHost appHost;
        public ServerHtmlTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown] public void OneTimeTearDown() => appHost.Dispose();

        ServerHtmlPage CreatePage(IVirtualFile file) =>
            new ServerHtmlPage(appHost.GetPlugin<ServerHtmlFeature>(), file);

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

            Assert.That(page.PageVars["layout"], Is.EqualTo("alt-layout.html"));
            Assert.That(page.PageVars["title"], Is.EqualTo("Variable Layout"));
            Assert.That(((ServerHtmlStringFragment)page.PageFragments[0]).Value, Is.EqualTo("<h1>Variable Page</h1>"));
        }

        [Test]
        public async Task Can_parse_template_with_body_variable()
        {
            var file = HostContext.AppHost.VirtualFileSources.GetFile("_layout.html");
            
            var page = await CreatePage(file).Init();

            Assert.That(page.PageFragments.Count, Is.EqualTo(5));
            var strFragment1 = (ServerHtmlStringFragment)page.PageFragments[0];
            var varFragment2 = (ServerHtmlVariableFragment)page.PageFragments[1];
            var strFragment3 = (ServerHtmlStringFragment)page.PageFragments[2];
            var varFragment4 = (ServerHtmlVariableFragment)page.PageFragments[3];
            var strFragment5 = (ServerHtmlStringFragment)page.PageFragments[4];

            Assert.That(strFragment1.Value, Is.EqualTo("<html><head><title>"));
            Assert.That(varFragment2.Name, Is.EqualTo("title"));
            Assert.That(strFragment3.Value, Is.EqualTo("</title></head><body id='layout'>"));
            Assert.That(varFragment4.Name, Is.EqualTo("body"));
            Assert.That(strFragment5.Value, Is.EqualTo("</body></html>"));
        }
        
    }
}