using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Templates;
using ServiceStack.Testing;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public interface IDep
    {
        string Greeting { get; set; }

        string SayHi(string name);
    }

    public class Dep : IDep
    {
        public string Greeting { get; set; } = "Hello ";

        public string SayHi(string name) => Greeting + name;
    }
    
    public class FilterExamples : TemplateFilter
    {
        public IDep Dep { get; set; }
        
        public IAppSettings AppSettings { get; set; } 

        public string greet(string name) => Dep.SayHi(name);

        public int addInt(int target, int value) => 
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
                    TemplateFilters = {new FilterExamples { Dep = new Dep()} }
                },
            };

            foreach (var context in contexts)
            {
                context.Container.AddSingleton<IDep>(() => new Dep());

                context.Init();
                Assert.That(context.TemplateFilters.Count, Is.GreaterThanOrEqualTo(2));
                var filter = (FilterExamples)context.TemplateFilters.First(x => x is FilterExamples);
                Assert.That(filter.Pages, Is.EqualTo(context.Pages));
                Assert.That(filter.Dep, Is.Not.Null);
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

                Assert.That(context.TemplateFilters.Count, Is.GreaterThanOrEqualTo(2));
                var filter = (FilterExamples)context.TemplateFilters.First(x => x is FilterExamples);
                Assert.That(filter.Pages, Is.EqualTo(context.Pages));
                Assert.That(filter.AppSettings, Is.Not.Null);
            }
        }

        public TemplatePagesContext CreateContext()
        {
            var context = new TemplatePagesContext
            {
                ScanAssemblies = {typeof(FilterExamples).GetAssembly()}
            };

            context.Container.AddSingleton<IDep>(() => new Dep { Greeting = "hi " });

            return context;
        }
            
        [Test]
        public async Task Does_call_simple_filter()
        {
            var context = CreateContext().Init();
            
            context.VirtualFiles.WriteFile("page.html", "<h1>{{ 'foo' | greet }}, {{ \"bar\" | greet }}</h1>");
            
            var result = new PageResult(context.GetPage("page"));

            var html = await result.RenderToStringAsync();
            
            Assert.That(html, Is.EqualTo("<h1>hi foo, hi bar</h1>"));
        }
            
        [Test]
        public async Task Does_call_addInt_filter_with_args()
        {
            var context = CreateContext().Init();
            
            context.VirtualFiles.WriteFile("page.html", "<h1>{{ 1 | addInt(2) }}</h1>");
            
            var result = new PageResult(context.GetPage("page"));

            var html = await result.RenderToStringAsync();
            
            Assert.That(html, Is.EqualTo("<h1>3</h1>"));
        }
            
        [Test]
        public async Task Does_call_multiple_addInt_filters_with_args()
        {
            var context = CreateContext().Init();
            
            context.VirtualFiles.WriteFile("page.html", "<h1>{{ 1 | addInt(2) | addInt(3) }}</h1>");
            
            var result = new PageResult(context.GetPage("page"));

            var html = await result.RenderToStringAsync();
            
            Assert.That(html, Is.EqualTo("<h1>6</h1>"));
        }

        [Test]
        public async Task Can_use_addInt_filter_with_page_and_result_args()
        {
            var context = CreateContext().Init();
            
            context.VirtualFiles.WriteFile("page.html", @"
<!--
pageArg: 2
-->

<h1>{{ 1 | addInt(pageArg) | addInt(resultArg) }}</h1>");
            
            var result = new PageResult(context.GetPage("page"))
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
        public async Task Does_call_recursive_addInt_filter_with_args()
        {
            var context = CreateContext().Init();
            
            context.VirtualFiles.WriteFile("page.html", "<h1>{{ 1 | addInt(addInt(2,3)) }}</h1>");
            
            var result = new PageResult(context.GetPage("page"));

            var html = await result.RenderToStringAsync();
            
            Assert.That(html, Is.EqualTo("<h1>6</h1>"));
        }

        [Test]
        public async Task Can_use_nested_addInt_filter_with_page_and_result_args()
        {
            var context = CreateContext().Init();

            context.Args["contextArg"] = 10;
            
            context.VirtualFiles.WriteFile("page.html", @"
<!--
pageArg: 2
-->

<h1>{{ 1 | addInt(pageArg) | addInt( addInt(addInt(2,resultArg),contextArg) ) }}</h1>");
            
            var result = new PageResult(context.GetPage("page"))
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