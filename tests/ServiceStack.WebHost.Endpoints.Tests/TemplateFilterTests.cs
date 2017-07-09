using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Templates;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class FilterExamples : TemplateFilter
    {
        public IAppSettings AppSettings { get; set; }

        public string appsetting(string name) => 
            AppSettings.GetString(name);

        public string capitalise(string text) => 
            text.ToPascalCase();

        public int add(int target, int value) => 
            target + value;
    }

    public class TemplateFilterTests
    {
        [Test]
        public void Can_Scan_FilterExamples_TemplateFilter()
        {
            var contexts = new[]
            {
                new TemplatePagesContext
                {
                    ScanTypes = {typeof(FilterExamples)}
                },
                new TemplatePagesContext
                {
                    ScanAssemblies = {typeof(FilterExamples).GetAssembly()}
                },
                new TemplatePagesContext
                {
                    TemplateFilters = {new FilterExamples {AppSettings = new DictionarySettings()}}
                },
            };

            foreach (var context in contexts)
            {
                context.Container.AddSingleton<IAppSettings>(() => new DictionarySettings());

                context.Init();
                Assert.That(context.TemplateFilters.Count, Is.EqualTo(1));
                Assert.That(context.TemplateFilters[0].Pages, Is.EqualTo(context.Pages));
                Assert.That(((FilterExamples) context.TemplateFilters[0]).AppSettings, Is.Not.Null);
            }
        }

        class AppHost : BasicAppHost
        {
            public AppHost() : base(typeof(AppHost).GetAssembly()) {}
        }

        [Test]
        public void Does_scan_AppHost_Service_Assemblies_in_TemplatePagesFeature()
        {
            using (new AppHost().Init())
            {
                var context = new TemplatePagesFeature().Init();

                Assert.That(context.TemplateFilters.Count, Is.EqualTo(1));
                Assert.That(context.TemplateFilters[0].Pages, Is.EqualTo(context.Pages));
                Assert.That(((FilterExamples) context.TemplateFilters[0]).AppSettings, Is.Not.Null);
            }
        }

        public TemplatePagesContext CreateContext()
        {
            var context = new TemplatePagesContext
            {
                ScanAssemblies = {typeof(FilterExamples).GetAssembly()}
            };

            context.Container.AddSingleton<IAppSettings>(() => new DictionarySettings(new Dictionary<string, string> {
                { "foo", "bar" },
            }));

            return context;
        }
            
        [Test]
        public async Task Does_call_simple_filter()
        {
            var context = CreateContext().Init();
            
            context.VirtualFiles.AppendFile("page.html", "<h1>{{ 'foo' | appsetting }}</h1>");
            
            var result = new PageResult(context.Pages.GetOrCreatePage("page"));

            var html = await result.RenderToStringAsync();
            
            Assert.That(html, Is.EqualTo("<h1>bar</h1>"));
        }
            
        [Test]
        public async Task Does_call_add_filter_with_args()
        {
            var context = CreateContext().Init();
            
            context.VirtualFiles.AppendFile("page.html", "<h1>{{ 1 | add(2) }}</h1>");
            
            var result = new PageResult(context.Pages.GetOrCreatePage("page"));

            var html = await result.RenderToStringAsync();
            
            Assert.That(html, Is.EqualTo("<h1>3</h1>"));
        }
            
        [Test]
        public async Task Does_call_multiple_add_filters_with_args()
        {
            var context = CreateContext().Init();
            
            context.VirtualFiles.AppendFile("page.html", "<h1>{{ 1 | add(2) | add(3) }}</h1>");
            
            var result = new PageResult(context.Pages.GetOrCreatePage("page"));

            var html = await result.RenderToStringAsync();
            
            Assert.That(html, Is.EqualTo("<h1>6</h1>"));
        }

        [Test]
        public async Task Can_use_add_filter_with_page_and_result_args()
        {
            var context = CreateContext().Init();
            
            context.VirtualFiles.AppendFile("page.html", @"
<!--
pageArg: 2
-->

<h1>{{ 1 | add(pageArg) | add(resultArg) }}</h1>");
            
            var result = new PageResult(context.Pages.GetOrCreatePage("page"))
            {
                Args =
                {
                    { "resultArg", "3" },
                }
            };

            var html = await result.RenderToStringAsync();
            
            Assert.That(html, Is.EqualTo("<h1>6</h1>"));
        }

        [Test]
        public async Task Does_call_recursive_add_filter_with_args()
        {
            var context = CreateContext().Init();
            
            context.VirtualFiles.AppendFile("page.html", "<h1>{{ 1 | add(add(2,3)) }}</h1>");
            
            var result = new PageResult(context.Pages.GetOrCreatePage("page"));

            var html = await result.RenderToStringAsync();
            
            Assert.That(html, Is.EqualTo("<h1>6</h1>"));
        }

        [Test]
        public async Task Can_use_nested_add_filter_with_page_and_result_args()
        {
            var context = CreateContext().Init();

            context.Args["contextArg"] = 10;
            
            context.VirtualFiles.AppendFile("page.html", @"
<!--
pageArg: 2
-->

<h1>{{ 1 | add(pageArg) | add( add(add(2,resultArg),contextArg) ) }}</h1>");
            
            var result = new PageResult(context.Pages.GetOrCreatePage("page"))
            {
                Args =
                {
                    { "resultArg", "3" },
                }
            };

            var html = await result.RenderToStringAsync();
            
            Assert.That(html, Is.EqualTo("<h1>18</h1>"));
        }
    }
}