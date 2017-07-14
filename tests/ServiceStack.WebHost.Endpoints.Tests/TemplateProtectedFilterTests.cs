using System;
using System.Reflection;
using Funq;
using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/echo-url")]
    public class IncludeUrlTest
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    
    public class TemplatePageServices : Service
    {
        public object Any(IncludeUrlTest request) => 
            $"{Request.Verb} {Request.RawUrl}";
    }
    
    public class TemplateProtectedFilterTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(TemplateProtectedFilterTests), typeof(TemplatePageServices).GetAssembly()) {}

            public override void Configure(Container container)
            {
                Plugins.Add(new TemplatePagesFeature
                {
                    Args =
                    {
                        ["baseUrl"] = Tests.Config.ListeningOn
                    }
                });
            }
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
            var context = new TemplatePagesContext().Init();
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
            var context = new TemplatePagesContext
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
            var context = new TemplatePagesContext
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
            var context = appHost.GetPlugin<TemplatePagesFeature>();

            var urlContents = new PageResult(context.OneTimePage(
                "{{ baseUrl | addPath('echo-url') | addQueryString({ id:1, name:'foo'}) | includeUrl | htmlencode }}")).Result;
            
            urlContents.Print();
            Assert.That(urlContents, Is.EqualTo("GET /echo-url?id=1&amp;name=foo"));
        }
    }
}