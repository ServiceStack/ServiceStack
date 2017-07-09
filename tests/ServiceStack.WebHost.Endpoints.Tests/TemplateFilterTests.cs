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
    }
}