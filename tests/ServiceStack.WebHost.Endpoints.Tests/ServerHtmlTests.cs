using System.Collections.Generic;
using Funq;
using NUnit.Framework;
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
                { "_layout.htm", "<html><head><title>/ Layout</title><body>{{ body }}</body></html>" },
                { "alt-layout.htm", "<html><head><title>{{ title }}</title><body>{{ body }}</body></html>" },
                { "root-static-page.htm", "<h1>/root-static Page!</h1>" },
                { "full-static-page.htm", "<html><head><title>Full Page</title><body><h1>Full Page</h1></body></html>" },
                { "variable-layout-page.htm", @"
<!--
layout: /alt-layout.html
title: Variable Layout
-->

<h1>Variable Page</h1>
" },
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

        [Test]
        public void Calling_partial_page_returns_complete_page()
        {
            var html = Config.ListeningOn.CombineWith("root-static-page.html")
                .GetStringFromUrl(accept: MimeTypes.Html);

            Assert.That(html, Does.Contain("<title>/ Layout</title>"));
            Assert.That(html, Does.Contain("<body><h1>/root-static Page!</h1></body>"));
        }

    }
}