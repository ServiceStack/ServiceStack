using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Funq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.IO;
using ServiceStack.OrmLite;
using ServiceStack.Script;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    [Route("/rockstar-pages/{Id}")]
    public class RockstarsPage
    {
        public int Id { get; set; }
        public string Layout { get; set; }
    }

    public class GetRockstarTemplate : IReturn<Rockstar>
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
    }

    public class AddRockstarTemplate : IReturnVoid
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
    }
    
    public class MyTemplateServices : Service
    {
        public object Any(RockstarsPage request) =>
            new PageResult(Request.GetCodePage("rockstar-view")) 
            {
                Layout = request.Layout,
                Args =
                {
                    ["rockstar"] = Db.SingleById<Rockstar>(request.Id)
                }
            };

        public object Any(GetRockstarTemplate request) => !string.IsNullOrEmpty(request.FirstName) 
            ? Db.Single<Rockstar>(x => x.FirstName == request.FirstName)
            : Db.SingleById<Rockstar>(request.Id);

        public void Any(AddRockstarTemplate request) =>
            Db.Save(request.ConvertTo<Rockstar>());
    }

    [Page("shadowed-page")]
    public class ShadowedPage : SharpCodePage
    {
        string render() => @"<h1>Shadowed Template Code Page</h1>";
    }

    [Page("shadowed/index")]
    public class ShadowedIndexPage : SharpCodePage
    {
        string render() => @"<h1>Shadowed Index Code Page</h1>";
    }

    [Page("rockstar")]
    public class RockstarPage : ServiceStackCodePage
    {
        string render(int id) => renderRockstar(Db.SingleById<Rockstar>(id));

        string renderRockstar(Rockstar rockstar) => $@"
<h1>{Request.RawUrl}</h1>
<h2>{rockstar.FirstName} {rockstar.LastName}</h2>
<b>{rockstar.Age}</b>
";
    }

    [Page("rockstar-view")]
    public class RockstarPageView : ServiceStackCodePage
    {
        string render(Rockstar rockstar) => $@"
<h1>{Request.RawUrl}</h1>
<h2>{rockstar.FirstName} {rockstar.LastName}</h2>
<b>{rockstar.Age}</b>
";
    }
    
    [Page("products")]
    [PageArg("title", "Products")]
    public class ProductsPage : SharpCodePage
    {
        string render(Product[] products) => $@"
        <table class='table'>
            <thead>
                <tr>
                    <th>Category</th>
                    <td>Name</td>
                    <td>Price</td>
                </tr>
            </thead>
            {products.OrderBy(x => x.Category).Map(x => 
            $"<tr><th>{x.Category}</th><td>{x.ProductName}</td><td>{x.UnitPrice:C}</td></tr>\n").Join("")}
        </table>";
    }
    
    [Page("products-sidebar", "layout-with-sidebar")]
    [PageArg("title", "Products with Sidebar")]
    public class ProductsSidebarPage : SharpCodePage
    {
        string render(Product[] products) => $@"
        <table class='table'>
            <thead>
                <tr>
                    <th>Category</th>
                    <td>Name</td>
                    <td>Price</td>
                </tr>
            </thead>
            {products.OrderBy(x => x.Category).Map(x => 
            $"<tr><th>{x.Category}</th><td>{x.ProductName}</td><td>{x.UnitPrice:C}</td></tr>\n").Join("")}
        </table>";
    }

    [Page("sidebar")]
    public class SidebarPage : SharpCodePage
    {
        string render(Dictionary<string, object> links) => $@"<ul>
    {links.Map(entry => $"<li><a href='{entry.Key}'>{entry.Value}</a></li>\n").Join("")}
</ul>";
    }

    [Page("requestInfo")]
    public class RequestInfoPartial : ServiceStackCodePage
    {
        string render() => $@"PathInfo: {Request.PathInfo}";
    }
    
    public class SharpPagesIntegrationTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(SharpPagesIntegrationTests), typeof(MyTemplateServices).Assembly) {}

            public override void Configure(Container container)
            {
                SetConfig(new HostConfig
                {
                    DebugMode = true,
                    ForbiddenPaths = { "/plugins" }
                });
                
                container.Register<IDbConnectionFactory>(new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

                using (var db = container.Resolve<IDbConnectionFactory>().Open())
                {
                    db.DropAndCreateTable<Rockstar>();
                    db.InsertAll(UnitTestExample.SeedData);
                }
                
                Plugins.Add(new SharpPagesFeature
                {
                    Args =
                    {
                        ["products"] = QueryData.Products,
                    }
                });

                var files = TemplateFiles[0];
                
                files.WriteFile("_layout.html", @"
<html>
<body id=root>
{{ page }}
</body>
</html>
");
                files.WriteFile("custom_layout.html", @"
<html>
<body id=custom>
{{ page }}
</body>
</html>
");
                files.WriteFile("layout-with-sidebar.html", @"
<html>
<body id=sidebar>
{{ 'sidebar' |> partial({ links: { 'a.html': 'A Page', 'b.html': 'B Page' } }) }}
{{ page }}
</body>
</html>
");
                files.WriteFile("dir/_layout.html", @"
<html>
<body id=dir>
{{ page }}
</body>
</html>
");
                files.WriteFile("alt-layout.html", @"
<html>
<body id=alt-layout>
{{ page }}
</body>
</html>
");
                files.WriteFile("dir/alt-layout.html", @"
<html>
<body id=dir-alt-layout>
{{ page }}
</body>
</html>
");
                files.WriteFile("alt/alt-layout.html", @"
<html>
<body id=alt-alt-layout>
{{ page }}
</body>
</html>
");
                files.WriteFile("dir/dir-page.html", @"
<h1>Dir Page</h1>
");

                files.WriteFile("dir/alt-dir-page.html", @"<!--
layout: alt-layout
-->

<h1>Alt Dir Page</h1>
");
                
                files.WriteFile("dir/alt-layout-alt-dir-page.html", @"<!--
layout: alt/alt-layout
-->

<h1>Alt Layout Alt Dir Page</h1>
");

                files.WriteFile("index.html", @"
<h1>The Home Page</h1>
");

                files.WriteFile("direct-page.html", @"
<h1>Direct Page</h1>
");
                files.WriteFile("shadowed-page.html", @"
<h1>Shadowed Template Page</h1>
");

                files.WriteFile("shadowed/index.html", @"
<h1>Shadowed Index Page</h1>
");
                files.WriteFile("requestinfo-page.html", @"
<h1>The Request Info Page</h1>
<p>{{ 'requestInfo' |> partial }}</p>
");
                
                files.WriteFile("dir/dir-partial.html", @"
<h2>Dir Partial</h2>
");
                files.WriteFile("dir/dir-page-partial.html", @"
<h1>Dir Page Partial</h1>
{{ 'dir-partial' |> partial }}
");
                
                files.WriteFile("dir/dir-file.txt", @"
<h2>Dir File</h2>
");
                files.WriteFile("dir/dir-page-file.html", @"
<h1>Dir Page File</h1>
{{ 'dir-file.txt' |> includeFile }}
");
                files.WriteFile("dir/dir-page-file-cache.html", @"
<h1>Dir Page File Cache</h1>
{{ 'dir-file.txt' |> includeFileWithCache }}
");
                
                files.WriteFile("rockstar-details.html", @"{{ it.FirstName }} {{ it.LastName }} ({{ it.Age }})");

                files.WriteFile("rockstar-gateway.html", @"
{{ { qs.id, qs.firstName } |> ensureAnyArgsNotNull |> sendToGateway('GetRockstarTemplate') |> assignTo: rockstar }}
{{ rockstar |> ifExists     |> selectPartial: rockstar-details }}
{{ rockstar |> endIfExists  |> select: No rockstar with id: { qs.id } }}
{{ htmlError }}
");

                files.WriteFile("rockstar-gateway-publish.html", @"
{{ 'id,firstName,lastName,age' |> importRequestParams }}{{ { id, firstName, lastName, age } |> ensureAllArgsNotNull |> publishToGateway('AddRockstarTemplate') }}
{{ 'rockstar-gateway' |> partial({ firstName }) }}
{{ htmlError }}");

                files.WriteFile("plugins/dll.txt", "Forbidden File");
            }

            public readonly List<IVirtualPathProvider> TemplateFiles = new List<IVirtualPathProvider> { new MemoryVirtualFiles() };
            public override List<IVirtualPathProvider> GetVirtualFileSources() => TemplateFiles;
        }

        public static string BaseUrl = Config.ListeningOn;
        
        private readonly ServiceStackHost appHost;
        public SharpPagesIntegrationTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(BaseUrl);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Can_process_home_page()
        {
            var html = BaseUrl.GetStringFromUrl();
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=root>

<h1>The Home Page</h1>

</body>
</html>
".NormalizeNewLines()));
        }

        void Assert404(string url)
        {
            try
            {
                var response = url.GetStreamFromUrl();
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.That(e.GetStatus(), Is.EqualTo(HttpStatusCode.NotFound));
            }
        }

        [Test]
        public void Unknown_paths_throw_404()
        {
            Assert404(BaseUrl.CombineWith(".unknown"));
            Assert404(BaseUrl.CombineWith(".unknown/path"));
        }

        [Test]
        public void Does_direct_page_with_layout()
        {
            var html = BaseUrl.AppendPath("direct-page").GetStringFromUrl();
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=root>

<h1>Direct Page</h1>

</body>
</html>
".NormalizeNewLines()));
        }

        [Test]
        public void Does_return_dir_page_with_dir_layout_by_default()
        {
            var html = BaseUrl.AppendPath("dir", "dir-page").GetStringFromUrl();
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=dir>

<h1>Dir Page</h1>

</body>
</html>
".NormalizeNewLines()));
        }

        [Test]
        public void Does_return_alt_dir_page_with_closest_alt_layout()
        {
            var html = BaseUrl.AppendPath("dir", "alt-dir-page").GetStringFromUrl();
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=dir-alt-layout>
<h1>Alt Dir Page</h1>

</body>
</html>
".NormalizeNewLines()));
        }

        [Test]
        public void Can_request_alt_layout_within_alt_subdir()
        {
            var html = BaseUrl.AppendPath("dir", "alt-layout-alt-dir-page").GetStringFromUrl();
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=alt-alt-layout>
<h1>Alt Layout Alt Dir Page</h1>

</body>
</html>
".NormalizeNewLines()));
        }

        [Test]
        public void Does_return_shadowed_code_page_with_layout()
        {
            var html = BaseUrl.AppendPath("shadowed-page").GetStringFromUrl();
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=root>
<h1>Shadowed Template Code Page</h1>
</body>
</html>
".NormalizeNewLines()));
        }

        [Test]
        public void Does_return_shadowed_index_code_page_with_layout()
        {
            var html = BaseUrl.AppendPath("shadowed").GetStringFromUrl();
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=root>
<h1>Shadowed Index Code Page</h1>
</body>
</html>
".NormalizeNewLines()));
        }

        [Test]
        public void Does_execute_ServiceStackCodePage_with_Db_and_Request()
        {
            var html = BaseUrl.AppendPath("rockstar").AddQueryParam("id", "1").GetStringFromUrl();
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=root>

<h1>/rockstar?id=1</h1>
<h2>Jimi Hendrix</h2>
<b>27</b>

</body>
</html>
".NormalizeNewLines()));
        }

        [Test]
        public void Does_execute_RockstarPageView()
        {
            var html = BaseUrl.AppendPath("rockstar-pages", "1").GetStringFromUrl();
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=root>

<h1>/rockstar-pages/1</h1>
<h2>Jimi Hendrix</h2>
<b>27</b>

</body>
</html>
".NormalizeNewLines()));
        }

        [Test]
        public void Does_execute_RockstarPageView_with_custom_layout()
        {
            var html = BaseUrl.AppendPath("rockstar-pages", "1").AddQueryParam("layout", "custom_layout").GetStringFromUrl();
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=custom>

<h1>/rockstar-pages/1?layout=custom_layout</h1>
<h2>Jimi Hendrix</h2>
<b>27</b>

</body>
</html>
".NormalizeNewLines()));
        }

        [Test]
        public void Does_execute_ProductsPage_with_default_layout()
        {
            var html = BaseUrl.AppendPath("products").GetStringFromUrl();
            
            Assert.That(html.NormalizeNewLines(), Does.StartWith(@"<html>
<body id=root>

        <table class='table'>
            <thead>
                <tr>
                    <th>Category</th>
                    <td>Name</td>
                    <td>Price</td>
                </tr>
            </thead>
            <tr><th>Beverages</th><td>Chai</td><td>$18.00</td></tr>
<tr><th>Beverages</th><td>Chang</td><td>$19.00</td></tr>".NormalizeNewLines()));
        }

        [Test]
        public void Does_execute_ProductsPage_with_Sidebar_CodePage_layout()
        {
            var html = BaseUrl.AppendPath("products-sidebar").GetStringFromUrl();

            Assert.That(html.NormalizeNewLines(), Does.StartWith(@"<html>
<body id=sidebar>
<ul>
    <li><a href='a.html'>A Page</a></li>
<li><a href='b.html'>B Page</a></li>

</ul>

        <table class='table'>
            <thead>
                <tr>
                    <th>Category</th>
                    <td>Name</td>
                    <td>Price</td>
                </tr>
            </thead>
            <tr><th>Beverages</th><td>Chai</td><td>$18.00</td></tr>
<tr><th>Beverages</th><td>Chang</td><td>$19.00</td></tr>".NormalizeNewLines()));
        }

        [Test]
        public void CodePage_partials_are_injected_with_current_Request()
        {
            var html = BaseUrl.AppendPath("requestinfo-page").GetStringFromUrl();

            Assert.That(html.NormalizeNewLines(), Does.StartWith(@"
<html>
<body id=root>

<h1>The Request Info Page</h1>
<p>PathInfo: /requestinfo-page</p>

</body>
</html>".NormalizeNewLines()));
        }

        [Test]
        public void Can_resolve_closest_partial_starting_from_page_directory()
        {
            var html = BaseUrl.AppendPath("dir","dir-page-partial").GetStringFromUrl();
            
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=dir>

<h1>Dir Page Partial</h1>

<h2>Dir Partial</h2>


</body>
</html>".NormalizeNewLines()));
        }

        [Test]
        public void Can_resolve_closest_file_starting_from_page_directory()
        {
            var html = BaseUrl.AppendPath("dir", "dir-page-file").GetStringFromUrl();

            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=dir>

<h1>Dir Page File</h1>

<h2>Dir File</h2>


</body>
</html>".NormalizeNewLines()));
        }

        [Test]
        public void Can_resolve_closest_file_with_cache_starting_from_page_directory()
        {
            var html = BaseUrl.AppendPath("dir", "dir-page-file-cache").GetStringFromUrl();

            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=dir>

<h1>Dir Page File Cache</h1>

<h2>Dir File</h2>


</body>
</html>".NormalizeNewLines()));
        }

        [Test]
        public void Can_call_sendToGateway()
        {
            var html = BaseUrl.AppendPath("rockstar-gateway").AddQueryParam("id","1").GetStringFromUrl();
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"<html>
<body id=root>

Jimi Hendrix (27)



</body>
</html>".NormalizeNewLines()));
            
            html = BaseUrl.AppendPath("rockstar-gateway").AddQueryParam("firstName","Kurt").GetStringFromUrl();
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"<html>
<body id=root>

Kurt Cobain (27)



</body>
</html>".NormalizeNewLines()));
        }

        [Test]
        public void Does_handle_error_calling_sendToGateway()
        {
            var html = BaseUrl.AppendPath("rockstar-gateway").GetStringFromUrl();
            Assert.That(html.NormalizeNewLines()
              .Replace("\nParameter name: firstName","")
              .Replace(" (Parameter 'firstName')",""), 
                Does.StartWith(@"<html>
<body id=root>



<pre class=""alert alert-danger"">ArgumentNullException: Value cannot be null.

StackTrace:
   at JsObjectExpression: {:qs.:id,:qs.:firstName}".NormalizeNewLines()));
            
            html = BaseUrl.AppendPath("rockstar-gateway").AddQueryParam("id","Kurt").GetStringFromUrl();
            Assert.That(html.NormalizeNewLines(), Does.StartWith(@"<html>
<body id=root>



<pre class=""alert alert-danger"">FormatException: Input string was not in a correct format.

StackTrace:
   at JsObjectExpression: {:qs.:id,:qs.:firstName}".NormalizeNewLines()));
        }

        [Test]
        public void Can_call_publishToGateway()
        {
            var html = BaseUrl.AppendPath("rockstar-gateway-publish")
                .AddQueryParam("id","8")
                .AddQueryParam("firstName","Amy")
                .AddQueryParam("lastName","Winehouse")
                .AddQueryParam("age","27")
                .GetStringFromUrl();
            
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"<html>
<body id=root>


Amy Winehouse (27)




</body>
</html>".NormalizeNewLines()));
        }

        [Test]
        public void Does_handle_error_calling_publishToGateway()
        {
            var html = BaseUrl.AppendPath("rockstar-gateway-publish")
                .AddQueryParam("id","8")
                .AddQueryParam("firstName","Amy")
                .AddQueryParam("age","27")
                .GetStringFromUrl();

            Assert.That(html.NormalizeNewLines()
                    .Replace("\nParameter name: lastName","")
                    .Replace(" (Parameter 'lastName')",""), 
                Does.StartWith(@"<html>
<body id=root>


<pre class=""alert alert-danger"">ArgumentNullException: Value cannot be null.

StackTrace:
   at JsObjectExpression: {:id,".NormalizeNewLines()));
        }

        [Test]
        public void Should_not_be_allowed_to_access_plugins_folder()
        {

            try
            {
                var contents = BaseUrl.AppendPath("plugins", "dll.txt").GetStringFromUrl();
                Assert.Fail("Should throw");
            }
            catch (Exception ex)
            {
                Assert.That(ex.GetStatus(), Is.EqualTo(HttpStatusCode.Forbidden));
            }
        }

    }
}