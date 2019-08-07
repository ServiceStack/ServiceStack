using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.IO;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.Redis;
using ServiceStack.Script;
using ServiceStack.Templates;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class TemplateDbFiltersAsync : ScriptMethods
    {
        private IDbConnectionFactory dbFactory;
        public IDbConnectionFactory DbFactory
        {
            get => dbFactory ?? (dbFactory = Context.Container.Resolve<IDbConnectionFactory>());
            set => dbFactory = value;
        }

        public async Task<IDbConnection> OpenDbConnectionAsync(ScriptScopeContext scope, Dictionary<string, object> options)
        {
            if (options != null)
            {
                if (options.TryGetValue("connectionString", out var connectionString))
                    return options.TryGetValue("providerName", out var providerName)
                        ? await DbFactory.OpenDbConnectionStringAsync((string)connectionString, (string)providerName) 
                        : await DbFactory.OpenDbConnectionStringAsync((string)connectionString);
                
                if (options.TryGetValue("namedConnection", out var namedConnection))
                    return await DbFactory.OpenDbConnectionAsync((string)namedConnection);
            }
            
            if (scope.PageResult != null && scope.PageResult.Args.TryGetValue("__dbinfo", out var oDbInfo) && oDbInfo is ConnectionInfo dbInfo) // Keywords.DbInfo
                return await DbFactory.OpenDbConnectionAsync(dbInfo);

            return await DbFactory.OpenAsync();
        }

        async Task<object> exec<T>(Func<IDbConnection, Task<T>> fn, ScriptScopeContext scope, object options)
        {
            try
            {
                using (var db = await OpenDbConnectionAsync(scope, options as Dictionary<string, object>))
                {
                    var result = await fn(db);
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new StopFilterExecutionException(scope, options, ex);
            }
        }

        public Task<object> dbSelect(TemplateScopeContext scope, string sql) => 
            exec(db => db.SqlListAsync<Dictionary<string, object>>(sql), scope, null);

        public Task<object> dbSelect(TemplateScopeContext scope, string sql, Dictionary<string, object> args) => 
            exec(db => db.SqlListAsync<Dictionary<string, object>>(sql, args), scope, null);

        public Task<object> dbSingle(TemplateScopeContext scope, string sql) => 
            exec(db => db.SingleAsync<Dictionary<string, object>>(sql), scope, null);

        public Task<object> dbSingle(TemplateScopeContext scope, string sql, Dictionary<string, object> args) =>
            exec(db => db.SingleAsync<Dictionary<string, object>>(sql, args), scope, null);

        public Task<object> dbScalar(ScriptScopeContext scope, string sql) => 
            exec(db => db.ScalarAsync<object>(sql), scope, null);

        public string sqlLimit(int? offset, int? limit) => padCondition(OrmLiteConfig.DialectProvider.SqlLimit(offset, limit));
        public string sqlLimit(int? limit) => padCondition(OrmLiteConfig.DialectProvider.SqlLimit(null, limit));
        private string padCondition(string text) => string.IsNullOrEmpty(text) ? "" : " " + text;
    }

    public class TemplateRedisFilters : TemplateFilter
    {
        private IRedisClientsManager redisManager;
        public IRedisClientsManager RedisManager
        {
            get => redisManager ?? (redisManager = Context.Container.Resolve<IRedisClientsManager>());
            set => redisManager = value;
        }

        T exec<T>(Func<IRedisClient, T> fn, TemplateScopeContext scope, object options)
        {
            try
            {
                using (var redis = RedisManager.GetClient())
                {
                    return fn(redis);
                }
            }
            catch (Exception ex)
            {
                throw new StopFilterExecutionException(scope, options, ex);
            }
        }
        
        public Dictionary<string, object> redisConnection(TemplateScopeContext scope) => exec(r => new Dictionary<string, object>
        {
            { "host", r.Host },
            { "port", r.Port },
            { "db", r.Db },
        }, scope, null);    
    }

    class Post
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime Created { get; set; }
        public string CreatedBy { get; set; }
        public DateTime Modified { get; set; }
        public string ModifiedBy { get; set; }
    }

    [TestFixture]
    public class BAK_CompatTemplateTests
    {
        [Test]
        public void BAK_Filters_evaluates_async_results()
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
        
        [Test]
        public void BAK_Can_pass_filter_by_argument_to_partial() 
        {   
            var context = new TemplateContext
            {
                TemplateFilters =
                {
                    new TemplateRedisFilters { RedisManager = new RedisManagerPool() },
                }
            }.Init();
            
            context.VirtualFiles.WriteFile("page-argument.html", "{{ 'partial-argument' | partial({ redis: redisConnection }) }}");
            context.VirtualFiles.WriteFile("partial-argument.html", "{{ redis.host }}, {{ redis.port }}");
            
            var output = new PageResult(context.GetPage("page-argument")).Result;
            
            Assert.That(output, Is.EqualTo("localhost, 6379"));
        }
        
        [Test]
        public void BAK_Can_include_plugins_into_new_eval_context()
        {
            var context = new TemplateContext {
                TemplateBlocks = { new EvalScriptBlock() },
                Plugins = { new MarkdownTemplatePlugin() },
                Args = {
                    ["evalContent"] = "{{#markdown}}# Heading{{/markdown}}",
                }
            }.Init();

            Assert.Throws<NotSupportedException>(() => 
                context.EvaluateTemplate("{{#eval}}{{evalContent}}{{/eval}}"));
            Assert.Throws<NotSupportedException>(() => 
                context.EvaluateTemplate("{{ evalContent | evalTemplate}}"));

            Assert.That(context.EvaluateTemplate("{{#eval {use:{plugins:'MarkdownTemplatePlugin'} }}{{evalContent}}{{/eval}}"), 
                Is.EqualTo("<h1>Heading</h1>\n"));

            Assert.That(context.EvaluateTemplate("{{ evalContent | evalTemplate({use:{plugins:'MarkdownTemplatePlugin'}}) | raw }}"), 
                Is.EqualTo("<h1>Heading</h1>\n"));
        }
        
        [Test]
        public void BAK_Can_include_filter_into_new_eval_context()
        {
            var context = new TemplateContext {
                TemplateBlocks = { new EvalScriptBlock() },
                TemplateFilters = { new TemplateInfoFilters() },
                Args = {
                    ["evalContent"] = "{{envServerUserAgent}}",
                }
            }.Init();

            Assert.That(context.EvaluateTemplate("{{#eval}}{{evalContent}}{{/eval}}"), 
                Does.Not.Contain("ServiceStack"));
            Assert.That(context.EvaluateTemplate("{{ evalContent | evalTemplate}}"), 
                Does.Not.Contain("ServiceStack"));

            Assert.That(context.EvaluateTemplate("{{#eval {use:{filters:'TemplateInfoFilters'}}{{evalContent}}{{/eval}}"), 
                Does.Contain("ServiceStack"));
            
            Assert.That(context.EvaluateTemplate("{{ evalContent | evalTemplate({use:{filters:'TemplateInfoFilters'}}) }}"), 
                Does.Contain("ServiceStack"));
        }

        [Test]
        public void BAK_Installation()
        {
            var context = new TemplateContext().Init();
            var dynamicPage = context.OneTimePage("The time is now: {{ now | dateFormat('HH:mm:ss') }}"); 
            var output = new PageResult(dynamicPage).Result;
            Assert.That(output, Does.StartWith("The time is now: "));
            
            output = context.EvaluateTemplate("The time is now: {{ now | dateFormat('HH:mm:ss') }}");
            Assert.That(output, Does.StartWith("The time is now: "));
        }

        [Test]
        public async Task BAK_Introduction()
        {
            var context = new TemplateContext().Init();
            var output = context.EvaluateTemplate("{{ 12.34 | currency }}");
            Assert.That(output, Is.EqualTo("$12.34"));
            
            context.VirtualFiles.WriteFile("_layout.html", "I am the Layout: <b>{{ page }}</b>");
            context.VirtualFiles.WriteFile("page.html", "I am the Page");
            var page = context.GetPage("page");
            output = await new PageResult(page).RenderToStringAsync();
            output = new PageResult(page).Result;
            Assert.That(output, Is.EqualTo("I am the Layout: <b>I am the Page</b>"));
        }

        [Test]
        public void BAK_Arguments()
        {
            var context = new TemplateContext { 
                Args = {
                    ["arg"] = 1
                }
            }.Init();
            
            context.VirtualFiles.WriteFile("_layout.html", @"<!--
arg: 2
arg2: 2
-->

layout args: <b>{{ arg }}, {{ arg2 }}</b> 
<p>{{ page }}</p>");
            
            context.VirtualFiles.WriteFile("page.html", @"<!--
arg: 3
-->

page arg: <b>{{ arg }}</b>");
            
            var output = new PageResult(context.GetPage("page")).Result;
            output.Print();
            
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"layout args: <b>3, 2</b> 
<p>page arg: <b>3</b></p>".NormalizeNewLines()));
            
            var result = new PageResult(context.GetPage("page")) { 
                Args = {
                    ["arg"] = 4
                }
            };
            output = result.Result;
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"layout args: <b>4, 2</b> 
<p>page arg: <b>4</b></p>".NormalizeNewLines()));
        }

        public class TemplateNoopBlock : TemplateBlock
        {
            public override string Name => "noop";

            public override Task WriteAsync(TemplateScopeContext scope, PageBlockFragment block, CancellationToken ct)
                => Task.CompletedTask;
        }
        
        public class TemplateBoldBlock : TemplateBlock
        {
            public override string Name => "bold";
    
            public override async Task WriteAsync(
                TemplateScopeContext scope, PageBlockFragment block, CancellationToken token)
            {
                await scope.OutputStream.WriteAsync("<b>", token);
                await WriteBodyAsync(scope, block, token);
                await scope.OutputStream.WriteAsync("</b>", token);
            }
        }

        [Test]
        public void BAK_Blocks()
        {
            var context = new TemplateContext {
                TemplateBlocks = { new TemplateNoopBlock() },
            }.Init();
            
            context = new TemplateContext
            {
                ScanTypes = { typeof(TemplateNoopBlock) }
            };
            context.Container.AddSingleton<ICacheClient>(() => new MemoryCacheClient());
            context.Init();
        
            context = new TemplateContext
            {
                ScanAssemblies = { typeof(TemplateNoopBlock).Assembly }
            }.Init();

            var output = context.EvaluateTemplate("BEFORE {{#noop}}BETWEEN{{/noop}} AFTER");
            Assert.That(output, Is.EqualTo("BEFORE  AFTER"));
            
            context = new TemplateContext {
                TemplateBlocks = { new TemplateBoldBlock() },
            }.Init();

            output = context.EvaluateTemplate("{{#bold}}This text will be bold{{/bold}}");
            Assert.That(output, Is.EqualTo("<b>This text will be bold</b>"));
        }
        
        class MyFilter : TemplateFilter
        {
            public string echo(string text) => $"{text} {text}";
            public double squared(double value) => value * value;
            public string greetArg(string key) => $"Hello {Context.Args[key]}";
            
            public ICacheClient Cache { get; set; } //injected dependency
            public string fromCache(string key) => Cache.Get<string>(key);
            
            public async Task includeFile(TemplateScopeContext scope, string virtualPath)
            {
                var file = scope.Context.VirtualFiles.GetFile(virtualPath);
                if (file == null)
                    throw new FileNotFoundException($"includeFile '{virtualPath}' was not found");

                using (var reader = file.OpenRead())
                {
                    await reader.CopyToAsync(scope.OutputStream);
                }
            }
        }

        [Test]
        public void BAK_Filters()
        {
            var context = new TemplateContext
            {
                Args =
                {
                    ["contextArg"] = "foo"
                },
                TemplateFilters = { new MyFilter() }
            }.Init();
            
            var output = context.EvaluateTemplate("<p>{{ 'contextArg' | greetArg }}</p>");
            Assert.That(output, Is.EqualTo("<p>Hello foo</p>"));
            
            output = new PageResult(context.OneTimePage("<p>{{ 'hello' | echo }}</p>"))
            {
                TemplateFilters = { new MyFilter() }
            }.Result;
            Assert.That(output, Is.EqualTo("<p>hello hello</p>"));
            
            context = new TemplateContext
            {
                ScanTypes = { typeof(MyFilter) }
            };
            context.Container.AddSingleton<ICacheClient>(() => new MemoryCacheClient());
            context.Container.Resolve<ICacheClient>().Set("key", "foo");
            context.Init();

            output = context.EvaluateTemplate("<p>{{ 'key' | fromCache }}</p>");
            Assert.That(output, Is.EqualTo("<p>foo</p>"));
            
            context = new TemplateContext
            {
                Plugins = { new MarkdownTemplatePlugin() },
                ScanAssemblies = { typeof(MyFilter).Assembly }
            }.Init();
            context.VirtualFiles.WriteFile("doc.md", "# Hello");

            output = context.EvaluateTemplate("{{ 'doc.md' | includeFile | markdown }}");
            Assert.That(output.Trim(), Is.EqualTo("<h1>Hello</h1>"));
        }

        [Test]
        public void BAK_DefaultFilters()
        {
            var context = new TemplateContext {
                Args = {
                    [TemplateConstants.DefaultDateFormat] = "yyyy-MM-dd HH:mm:ss"
                }
            }.Init();
        }

        [Test]
        public void BAK_ServiceStackFilters()
        {
            using (new BasicAppHost().Init())
            {
                var context = new TemplatePagesFeature 
                {
                    TemplateFilters = { new TemplateInfoFilters() }
                }.Init();

                var plugin = new TemplatePagesFeature {
                    MetadataDebugAdminRole = RoleNames.AllowAnon
                };

                plugin = new TemplatePagesFeature {
                    MetadataDebugAdminRole = RoleNames.AllowAnyUser, // Allow Authenticated Users
                };
            }
        }

        [Test]
        public void BAK_DatabaseFilters()
        {
            var context = new TemplateContext { 
                TemplateFilters = {
                    new TemplateDbFiltersAsync()
                }
            }.Init();
        }

        [Test]
        public void BAK_RedisFilters()
        {
            var context = new TemplateContext { 
                TemplateFilters = {
                    new TemplateRedisFilters()
                }
            }.Init();
        }

        [Test]
        public void BAK_ErrorHandling()
        {
            var context = new TemplateContext {
                SkipExecutingFiltersIfError = true
            };
            
            context = new TemplateContext {
                RenderExpressionExceptions = true
            }.Init();
        }
        
        public class MarkdownPageFormat : PageFormat
        {
            private static readonly MarkdownDeep.Markdown markdown = new MarkdownDeep.Markdown();

            public MarkdownPageFormat()
            {
                Extension = "md";
                ContentType = MimeTypes.MarkdownText;
            }

            public static async Task<Stream> TransformToHtml(Stream markdownStream)
            {
                using (var reader = new StreamReader(markdownStream))
                {
                    var md = await reader.ReadToEndAsync();
                    var html = markdown.Transform(md);
                    return MemoryStreamFactory.GetStream(html.ToUtf8Bytes());
                }
            }
        }

        [Test]
        public async Task BAK_Transformers()
        {
            var context = new TemplateContext {
                PageFormats = { new MarkdownPageFormat() }
            }.Init();

            context.VirtualFiles.WriteFile("_layout.md", @"
The Header

{{ page }}");

            context.VirtualFiles.WriteFile("page.md",  @"
## {{ title }}

The Content");
            
            var result = new PageResult(context.GetPage("page")) 
            {
                Args = { {"title", "The Title"} },
                ContentType = MimeTypes.Html,
                OutputTransformers = { MarkdownPageFormat.TransformToHtml },
            };

            var html = await result.RenderToStringAsync();
            Assert.That(html.NormalizeNewLines(), Is.EqualTo("<p>The Header</p>\n<h2>The Title</h2>\n<p>The Content</p>\n".NormalizeNewLines()));
            
            
            context = new TemplateContext {
                PageFormats = { new MarkdownPageFormat() }
            }.Init();

            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
  <title>{{ title }}</title>
</head>
<body>
  {{ page }}
</body>");

            context.VirtualFiles.WriteFile("page.md",  @"
## Transformers

The Content");
            
            result = new PageResult(context.GetPage("page")) 
            {
                Args = { {"title", "The Title"} },
                ContentType = MimeTypes.Html,
                PageTransformers = { MarkdownPageFormat.TransformToHtml },
            };

            html = await result.RenderToStringAsync();
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"<html>
  <title>The Title</title>
</head>
<body>
  <h2>Transformers</h2>
<p>The Content</p>

</body>".NormalizeNewLines()));
            
            
            context = new TemplateContext
            {
                TemplateFilters = { new TemplateProtectedFilters() },
                FilterTransformers =
                {
                    ["markdown"] = MarkdownPageFormat.TransformToHtml
                }
            }.Init();

            context.VirtualFiles.WriteFile("doc.md", @"## The Heading

The Content");

            context.VirtualFiles.WriteFile("page.html", @"
<div id=content>
    {{ 'doc.md' | includeFile | markdown }}
</div>");
            
            html = new PageResult(context.GetPage("page")).Result;
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"<div id=content>
    <h2>The Heading</h2>
<p>The Content</p>

</div>".NormalizeNewLines()));
        }
        
        [Route("/BAK_customers/{Id}")]
        public class ViewCustomer
        {
            public string Id { get; set; }
        }

        public class CustomerServices : Service
        {
            public ITemplatePages Pages { get; set; }

            public object Any(ViewCustomer request) =>
                new PageResult(Pages.GetPage("examples/customer")) {
                    Model = QueryData.GetCustomer(request.Id)
                };

            public object Get(ViewCustomer request) =>
                new PageResult(Request.GetPage("examples/customer")) {
                    Model = QueryData.GetCustomer(request.Id)
                };
        }

        [Test]
        public void BAK_ModelViewController()
        {
            using (var appHost = new BasicAppHost {
                ConfigureAppHost = host => {
                    host.Plugins.Add(new TemplatePagesFeature());
                    host.Container.AddTransient<CustomerServices>();
                } 
            }.Init())
            {
                var service = appHost.Container.Resolve<CustomerServices>();
                Assert.That(service.Pages, Is.Not.Null);
            }
        }
        
        [Page("BAK_products")]
        [PageArg("title", "Products")]
        public class ProductsPage : TemplateCodePage
        {
            string render(Product[] products) => $@"
        <table class='table table-striped'>
            <thead>
                <tr>
                    <th>Category</th>
                    <th>Name</th>
                    <th>Price</th>
                </tr>
            </thead>
            {products.OrderBy(x => x.Category).ThenBy(x => x.ProductName).Map(x => $@"
                <tr>
                    <th>{x.Category}</th>
                    <td>{x.ProductName.HtmlEncode()}</td>
                    <td>{x.UnitPrice:C}</td>
                </tr>").Join("")}
        </table>";
        }
        
        [Route("/BAK_products/view")]
        public class ViewProducts
        {
            public string Id { get; set; }
        }

        public class ProductsServices : Service
        {
            public object Any(ViewProducts request) =>
                new PageResult(Request.GetCodePage("products")) {
                    Args = {
                        ["products"] = QueryData.Products
                    }
                };
        }
        
        [Page("BAK_navLinks")]
        public class NavLinksPartial : TemplateCodePage
        {
            string render(string PathInfo, Dictionary<string, object> links) => $@"
        <ul>
            {links.Map(entry => $@"<li class='{GetClass(PathInfo, entry.Key)}'>
                <a href='{entry.Key}'>{entry.Value}</a>
            </li>").Join("")}
        </ul>";

            string GetClass(string pathInfo, string url) => url == pathInfo ? "active" : ""; 
        }
        
        [Test]
        public void BAK_CodePages()
        {
        }

        [Test]
        public void BAK_Sandbox()
        {
            var context = new TemplateContext {
                ExcludeFiltersNamed = { "partial", "selectPartial" }
            }.Init();
            
            context = new TemplateContext {
                Args = {
                    [TemplateConstants.MaxQuota] = 1000
                }
            }.Init();
        }
        
        public class MarkdownTemplatePlugin : ITemplatePlugin
        {
            public bool RegisterPageFormat { get; set; } = true;

            public void Register(ScriptContext context)
            {
                if (RegisterPageFormat)
                    context.PageFormats.Add(new MarkdownPageFormat());
        
                context.FilterTransformers["markdown"] = MarkdownPageFormat.TransformToHtml;
        
                context.ScriptMethods.Add(new MarkdownTemplateFilter());

                TemplateConfig.DontEvaluateBlocksNamed.Add("markdown");
        
                context.ScriptBlocks.Add(new TemplateMarkdownBlock());
            }
        }

        [Test]
        public void BAK_APIReference()
        {
            ScriptContext context = new TemplateContext {
                Plugins = { new MarkdownTemplatePlugin { RegisterPageFormat = false } }
            }.Init();
            
            context = new TemplateContext()
                .RemovePlugins(x => x is DefaultScriptBlocks) // Remove default blocks
                .RemovePlugins(x => x is HtmlScriptBlocks)    // Remove all html blocks
                .Init();
            
            context = new TemplateContext {
                    OnAfterPlugins = ctx => ctx.RemoveBlocks(x => x.Name == "capture")
                }
                .Init();
            
            context = new TemplateContext { 
                Args = {
                    [TemplateConstants.MaxQuota] = 10000,
                    [TemplateConstants.DefaultCulture] = CultureInfo.CurrentCulture,
                    [TemplateConstants.DefaultDateFormat] = "yyyy-MM-dd",
                    [TemplateConstants.DefaultDateTimeFormat] = "u",
                    [TemplateConstants.DefaultTimeFormat] = "h\\:mm\\:ss",
                    [TemplateConstants.DefaultFileCacheExpiry] = TimeSpan.FromMinutes(1),
                    [TemplateConstants.DefaultUrlCacheExpiry] = TimeSpan.FromMinutes(1),
                    [TemplateConstants.DefaultIndent] = "\t",
                    [TemplateConstants.DefaultNewLine] = Environment.NewLine,
                    [TemplateConstants.DefaultJsConfig] = "excludetypeinfo",
                    [TemplateConstants.DefaultStringComparison] = StringComparison.Ordinal,
                    [TemplateConstants.DefaultTableClassName] = "table",
                    [TemplateConstants.DefaultErrorClassName] = "alert alert-danger",
                }
            }.Init();

            var page = context.EmptyPage;
            new PageResult(page) { Layout = "custom-layout" };
            new PageResult(page) {
                TemplateFilters = {new MyFilter()}
            };
            new PageResult(page) {
                ContentType = MimeTypes.Html,
                OutputTransformers = {MarkdownPageFormat.TransformToHtml},
            };
            new PageResult(page) {
                ContentType = MimeTypes.Html,
                PageTransformers = {MarkdownPageFormat.TransformToHtml},
            };
            new PageResult(page) {
                FilterTransformers = {
                    ["markdown"] = MarkdownPageFormat.TransformToHtml
                }
            };
            new PageResult(page) {
                ExcludeFiltersNamed = {"partial", "selectPartial"}
            };
            new PageResult(page) {
                Options = {
                    ["X-Powered-By"] = "ServiceStack Templates"
                }
            };
            new PageResult(page) {
                ContentType = "text/plain"
            };
        }
   
    }
}