using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.Templates;
using ServiceStack.Testing;
using ServiceStack.IO;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
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
                new TemplateContext
                {
                    ScanTypes = {typeof(FilterExamples)}
                },
                new TemplateContext
                {
                    ScanAssemblies = {typeof(FilterExamples).Assembly}
                },
                new TemplateContext
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
            public AppHost() : base(typeof(AppHost).Assembly) {}
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

        public TemplateContext CreateContext()
        {
            var context = new TemplateContext
            {
                ScanAssemblies = {typeof(FilterExamples).Assembly}
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

        [Test]
        public void Can_disable_disable_filters()
        {
            var context = new TemplateContext
            {
                ExcludeFiltersNamed = { "repeat" }
            }.Init();

            var page = context.OneTimePage("{{ '.' | repeat(3) }}{{ 3 | repeating('-') }}");
            
            Assert.That(new PageResult(page).Result, Is.EqualTo("---"));
            
            Assert.That(new PageResult(page){ ExcludeFiltersNamed = {"repeating"} }.Result, 
                Is.EqualTo(""));
        }

        [Test]
        public void Caches_are_kept_isolated_in_each_Context_Filter_instance()
        {
            var context = new TemplateContext
            {
                TemplateFilters = { new TemplateProtectedFilters() }
            }.Init();
            context.VirtualFiles.WriteFile("file.txt", "foo");
            context.VirtualFiles.WriteFile("page.html", "{{ 'file.txt' | includeFileWithCache | assignTo: contents }}" +
                                                        "{{ contents | append('bar') | upper | repeat(2) }}");
            
            Assert.That(new PageResult(context.GetPage("page")).Result, Is.EqualTo("FOOBARFOOBAR"));
            Assert.That(context.ExpiringCache.Count, Is.EqualTo(1));
            Assert.That(context.TemplateFilters.First(x => x is TemplateDefaultFilters).InvokerCache.Count, Is.EqualTo(4));
            
            /* TEMP START */
            var tempContext = new TemplateContext
            {
                TemplateFilters = { new TemplateProtectedFilters() }
            }.Init();
            tempContext.VirtualFiles.WriteFile("file.txt", "...");
            
            var tmpPage = tempContext.OneTimePage("{{ 'file.txt' | includeFileWithCache | assignTo: contents }}" +
                                                  "{{ contents | append('bar') | repeat(3) }}");
            Assert.That(new PageResult(tmpPage).Result, Is.EqualTo("...bar...bar...bar"));
            Assert.That(new PageResult(tmpPage).Result, Is.EqualTo("...bar...bar...bar"));
            
            Assert.That(tempContext.ExpiringCache.Count, Is.EqualTo(1));
            Assert.That(tempContext.TemplateFilters.First(x => x is TemplateDefaultFilters).InvokerCache.Count, Is.EqualTo(3));
            /* TEMP END */
            
            Assert.That(new PageResult(context.GetPage("page")).Result, Is.EqualTo("FOOBARFOOBAR"));
            Assert.That(context.ExpiringCache.Count, Is.EqualTo(1));
            Assert.That(context.TemplateFilters.First(x => x is TemplateDefaultFilters).InvokerCache.Count, Is.EqualTo(4));
        }

        class Post
        {
            [AutoIncrement]
            public int Id { get; set; }
            
            public string Title { get; set; }
            
            public string Content { get; set; }
            
            public DateTime Created { get; set; }

            public DateTime Modified { get; set; }
        }

        [Test]
        public void Filters_evaluates_async_results()
        {
            OrmLiteConfig.BeforeExecFilter = cmd => cmd.GetDebugString().Print();

            var context = new TemplateContext
            {
                TemplateFilters = { new TemplateDbFiltersAsync() },
                Args = {
                    ["objectCount"] = Task.FromResult((object)1)
                }
            };
            context.Container.AddSingleton<IDbConnectionFactory>(() => new OrmLiteConnectionFactory(":memory:", SqliteOrmLiteDialectProvider.Instance));
            context.Init();
            
            using (var db = context.Container.Resolve<IDbConnectionFactory>().Open())
            {
                db.DropAndCreateTable<Post>();
                db.Insert(new Post { Title = "The Title", Content = "The Content", Created = DateTime.Now, Modified = DateTime.Now });
            }
                
            context.VirtualFiles.WriteFile("objectCount.html", "{{ objectCount | assignTo: count }}{{ count }}");
            context.VirtualFiles.WriteFile("dbCount.html", "{{ dbScalar(`SELECT COUNT(*) FROM Post`) | assignTo: count }}{{ count }}");
            
            Assert.That(new PageResult(context.GetPage("objectCount")).Result, Is.EqualTo("1"));
            Assert.That(new PageResult(context.GetPage("dbCount")).Result, Is.EqualTo("1"));

            OrmLiteConfig.BeforeExecFilter = null;
        }
    }
}