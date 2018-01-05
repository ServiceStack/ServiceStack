using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Templates;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    [Route("/includeUrl-echo")]
    public class IncludeUrlEcho : IReturn<string> {}

    [Route("/includeUrl-model")]
    public class IncludeUrlModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    
    [Route("/includeUrl-models")]
    public class IncludeUrlModels : List<IncludeUrlModel> {}
    
    [Route("/includeUrl-time")]
    public class GetCurrentTime : IReturn<string> {}

    public class TemplatePageServices : Service
    {
        public object Any(IncludeUrlEcho request) => 
            $"{Request.Verb} {Request.RawUrl}";

        public object Any(IncludeUrlModel request) => 
            request;

        public object Any(IncludeUrlModels request) => 
            request;

        public object Any(GetCurrentTime request) =>
            DateTime.Now.ToString("o");
    }
    
    public class TemplateProtectedFilterTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(TemplateProtectedFilterTests), typeof(TemplatePageServices).Assembly) {}

            public override void Configure(Container container)
            {
                SetConfig(new HostConfig
                {
                    UseCamelCase = false, //normalize with .NET Core
                });

                Plugins.Add(new TemplatePagesFeature
                {
                    Args =
                    {
                        ["baseUrl"] = Tests.Config.ListeningOn
                    }
                });
            }

            private readonly List<IVirtualPathProvider> virtualFiles = new List<IVirtualPathProvider> { new MemoryVirtualFiles() };
            public override List<IVirtualPathProvider> GetVirtualFileSources() => virtualFiles;
        }

        private readonly ServiceStackHost appHost;
        public TemplateProtectedFilterTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Does_not_include_protected_filters_by_default()
        {
            var context = new TemplateContext().Init();
            context.VirtualFiles.WriteFile("index.txt", "file contents");

            Assert.That(new PageResult(context.OneTimePage("{{ 'index.txt' | includeFile }}")).Result, 
                Is.EqualTo("{{ 'index.txt' | includeFile }}"));

            var feature = new TemplatePagesFeature().Init();
            feature.VirtualFiles.WriteFile("index.txt", "file contents");

            Assert.That(new PageResult(context.OneTimePage("{{ 'index.txt' | includeFile }}")).Result, 
                Is.EqualTo("{{ 'index.txt' | includeFile }}"));
        }

        [Test]
        public void Can_use_protected_includeFiles_in_context_or_PageResult()
        {
            var context = new TemplateContext
            {
                TemplateFilters = { new TemplateProtectedFilters() }
            }.Init();
            context.VirtualFiles.WriteFile("index.txt", "file contents");
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'index.txt' | includeFile }}")).Result, 
                Is.EqualTo("file contents"));
        }

        [Test]
        public void Can_use_transformers_on_block_filter_outputs()
        {
            var context = new TemplateContext
            {
                TemplateFilters = { new TemplateProtectedFilters() },
                FilterTransformers =
                {
                    ["markdown"] = MarkdownPageFormat.TransformToHtml
                }
            }.Init();
            context.VirtualFiles.WriteFile("index.md", "## Markdown Heading");
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'index.md' | includeFile | markdown }}")).Result.Trim(), 
                Is.EqualTo("<h2>Markdown Heading</h2>"));
        }

        [Test]
        public void Can_use_includeUrl()
        {
            string urlContents;
            var context = appHost.GetPlugin<TemplatePagesFeature>();

            urlContents = new PageResult(context.OneTimePage(
                "{{ baseUrl | addPath('includeUrl-echo') | addQueryString({ id:1, name:'foo'}) | includeUrl | htmlencode }}")).Result;
            Assert.That(urlContents, Is.EqualTo("GET /includeUrl-echo?id=1&amp;name=foo"));
            
            urlContents = new PageResult(context.OneTimePage(
                "{{ baseUrl | addPath('includeUrl-model') | addQueryString({ id:1, name:'foo'}) | includeUrl({ accept: 'application/json' }) }}")).Result;
            Assert.That(urlContents, Is.EqualTo("{\"Id\":1,\"Name\":\"foo\"}"));
            
            urlContents = new PageResult(context.OneTimePage(
                "{{ baseUrl | addPath('includeUrl-model') | addQueryString({ id:1, name:'foo'}) | includeUrl({ dataType: 'json' }) }}")).Result;
            Assert.That(urlContents, Is.EqualTo("{\"Id\":1,\"Name\":\"foo\"}"));
            
            urlContents = new PageResult(context.OneTimePage(
                "{{ baseUrl | addPath('includeUrl-model') | includeUrl({ method:'POST', data: { id: 1, name: 'foo' }, accept: 'application/jsv' }) }}")).Result;
            Assert.That(urlContents, Is.EqualTo("{Id:1,Name:foo}"));
            
            urlContents = new PageResult(context.OneTimePage(
                "{{ baseUrl | addPath('includeUrl-model') | includeUrl({ method:'POST', data: { id: 1, name: 'foo' }, accept: 'application/json', contentType: 'application/json' }) }}")).Result;
            Assert.That(urlContents, Is.EqualTo("{\"Id\":1,\"Name\":\"foo\"}"));
            
            urlContents = new PageResult(context.OneTimePage(
                "{{ baseUrl | addPath('includeUrl-model') | includeUrl({ method:'POST', data: { id: 1, name: 'foo' }, dataType: 'json' }) }}")).Result;
            Assert.That(urlContents, Is.EqualTo("{\"Id\":1,\"Name\":\"foo\"}"));
            
            urlContents = new PageResult(context.OneTimePage(
                "{{ baseUrl | addPath('includeUrl-model') | includeUrl({ method:'POST', data: { id: 1, name: 'foo' }, dataType: 'jsv' }) }}")).Result;
            Assert.That(urlContents, Is.EqualTo("{Id:1,Name:foo}"));
            
            urlContents = new PageResult(context.OneTimePage(
                "{{ baseUrl | addPath('includeUrl-models') | includeUrl({ method:'POST', data: [{ id: 1, name: 'foo' }, { id: 2, name: 'bar' }], contentType:'application/json', accept: 'application/jsv' }) }}")).Result;
            Assert.That(urlContents, Is.EqualTo("[{Id:1,Name:foo},{Id:2,Name:bar}]"));
            
            urlContents = new PageResult(context.OneTimePage(
                "{{ baseUrl | addPath('includeUrl-models') | includeUrl({ method:'POST', data: [{ id: 1, name: 'foo' }, { id: 2, name: 'bar' }], contentType:'application/jsv', accept: 'text/csv' }) }}")).Result.NormalizeNewLines();
            Assert.That(urlContents, Is.EqualTo("Id,Name\n1,foo\n2,bar"));
            
            urlContents = new PageResult(context.OneTimePage(
                "{{ baseUrl | addPath('includeUrl-models') | includeUrl({ method:'POST', data: [{ id: 1, name: 'foo' }, { id: 2, name: 'bar' }], dataType:'csv' }) }}")).Result.NormalizeNewLines();
            Assert.That(urlContents, Is.EqualTo("Id,Name\n1,foo\n2,bar"));
        }

        [Test]
        public void Filter_includeFile_does_load_modiifed_contents()
        {
            var context = appHost.GetPlugin<TemplatePagesFeature>();
            context.VirtualFiles.WriteFile("page.txt", "Original Content");
            
            var includeFilePage = context.OneTimePage("{{ 'page.txt' | includeFile }}");
            var fileContents = new PageResult(includeFilePage).Result;
            Assert.That(fileContents, Is.EqualTo("Original Content"));

            context.VirtualFiles.WriteFile("page.txt", "Modified Content");
            fileContents = new PageResult(includeFilePage).Result;
            Assert.That(fileContents, Is.EqualTo("Modified Content"));
        }

        [Test]
        public void Can_cache_contents_with_includeUrlWithCache_and_includeFileWithCache()
        {
            var context = appHost.GetPlugin<TemplatePagesFeature>();
            context.VirtualFiles.WriteFile("page.txt", "Original Content");

            var urlWithDefaultCache = context.OneTimePage("{{ baseUrl | addPath('includeUrl-time') | includeUrlWithCache }}");

            var urlContents1 = new PageResult(urlWithDefaultCache).Result;
            var urlContents2 = new PageResult(urlWithDefaultCache).Result;
            Assert.That(urlContents1, Is.EqualTo(urlContents2));

            Assert.That(new PageResult(context.OneTimePage("{{ 'page.txt' | includeFileWithCache }}")).Result, Is.EqualTo("Original Content"));
            context.VirtualFiles.WriteFile("page.txt", "Modified Content");

            var fileWithCachePage = context.OneTimePage("{{ 'page.txt' | includeFileWithCache({ expiresInSecs: 1 }) }}");
            var urlWithCache1Sec = context.OneTimePage("{{ baseUrl | addPath('includeUrl-time') | includeUrlWithCache({ expireInSecs: 1 }) }}");
            
            urlContents1 = new PageResult(urlWithCache1Sec).Result;

            Thread.Sleep(TimeSpan.FromMilliseconds(1001));
            
            urlContents2 = new PageResult(urlWithCache1Sec).Result;
            Assert.That(urlContents1, Is.Not.EqualTo(urlContents2));
            
            Assert.That(new PageResult(fileWithCachePage).Result, Is.EqualTo("Modified Content"));
        }

        [Test]
        public void Can_exclude_individual_filters()
        {
            var context = new TemplateContext
            {
                ExcludeFiltersNamed = { "includeUrl" },
                TemplateFilters = { new TemplateProtectedFilters() },
            }.Init();
            
            context.VirtualFiles.WriteFile("file.txt", "File Contents");

            context.VirtualFiles.WriteFile("page.html", @"
includeUrl = {{ baseUrl | addPath('includeUrl-time') | includeUrl }}
includFile = {{ 'file.txt' | includeFile }}
");
            
            Assert.That(new PageResult(context.GetPage("page")).Result.NormalizeNewLines(), Is.EqualTo(@"
includeUrl = {{ baseUrl | addPath('includeUrl-time') | includeUrl }}
includFile = File Contents
".NormalizeNewLines()));
        }

        #if NET45
        [Test]
        public void Does_use_dollar_as_currency_symbol_when_InvariantCulture()
        {
            var hold = Thread.CurrentThread.CurrentCulture; 
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            
            var context = new TemplateContext().Init();

            var output = context.EvaluateTemplate("{{ 12.345 | currency }}");
            Assert.That(output, Is.EqualTo("$12.35"));

            Thread.CurrentThread.CurrentCulture = hold;
        }
        #endif

    }
}