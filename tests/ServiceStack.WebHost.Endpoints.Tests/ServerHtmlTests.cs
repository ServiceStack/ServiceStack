using System.Collections.Generic;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using ServiceStack.IO;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class ServerHtmlService : Service
    {
        
    }

    public class ServerHtmlTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(nameof(ServerHtmlTests), typeof(ServerHtmlService).GetAssembly()) { }

            public override void Configure(Container container)
            {                
            }

            static readonly Dictionary<string,string> HtmlFiles = new Dictionary<string, string>
            {
                { "_layout.html", "<html><head><title>{{ title }}</title></head><body>{{ body }}</body></html>" },
                { "alt-layout.html", "<html><head><title>{{ title }}</title></head><body>{{ body }}</body></html>" },
                { "root-static-page.html", "<h1>/root-static Page!</h1>" },
                { "full-static-page.html", "<html><head><title>Full Page</title></head><body><h1>Full Page</h1></body></html>" },
                { "variable-layout-page.html", @"
<!--
template: alt-layout.html
title: Variable Layout
-->

<h1>Variable Page</h1>" },
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

        [Explicit("developing...")]
        [Test]
        public void Calling_partial_page_returns_complete_page()
        {
            var html = Config.ListeningOn.CombineWith("root-static-page.html")
                .GetStringFromUrl(accept: MimeTypes.Html);

            Assert.That(html, Does.Contain("<title>/ Layout</title>"));
            Assert.That(html, Does.Contain("<body><h1>/root-static Page!</h1></body>"));
        }

        [Test]
        public async Task Can_parse_page_with_page_variables()
        {
            var file = HostContext.AppHost.VirtualFileSources.GetFile("variable-layout-page.html");
            
            var page = await new ServerHtmlPage(file).Load();

            Assert.That(page.PageVars["template"], Is.EqualTo("alt-layout.html"));
            Assert.That(page.PageVars["title"], Is.EqualTo("Variable Layout"));
            Assert.That(((ServerHtmlStringFragment)page.PageFragments[0]).Value, Is.EqualTo("\r\n<h1>Variable Page</h1>"));
        }

        [Test]
        public async Task Can_parse_template_with_body_variable()
        {
            var file = HostContext.AppHost.VirtualFileSources.GetFile("_layout.html");
            
            var page = await new ServerHtmlPage(file).Load();

            Assert.That(page.PageFragments.Count, Is.EqualTo(5));
            var strFragment1 = (ServerHtmlStringFragment)page.PageFragments[0];
            var varFragment2 = (ServerHtmlVariableFragment)page.PageFragments[1];
            var strFragment3 = (ServerHtmlStringFragment)page.PageFragments[2];
            var varFragment4 = (ServerHtmlVariableFragment)page.PageFragments[3];
            var strFragment5 = (ServerHtmlStringFragment)page.PageFragments[4];

            Assert.That(strFragment1.Value, Is.EqualTo("<html><head><title>"));
            Assert.That(varFragment2.Name, Is.EqualTo("title"));
            Assert.That(strFragment3.Value, Is.EqualTo("</title></head><body>"));
            Assert.That(varFragment4.Name, Is.EqualTo("body"));
            Assert.That(strFragment5.Value, Is.EqualTo("</body></html>"));
        }

    
    }
}