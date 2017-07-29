using System;
using System.Collections.Generic;
using System.Net;
using Funq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.IO;
using ServiceStack.OrmLite;
using ServiceStack.Templates;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    [Route("/rockstar-pages/{Id}")]
    public class RockstarsPage
    {
        public int Id { get; set; }
        public string Layout { get; set; }
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
    }

    [Page("shadowed-page")]
    public class ShadowedPage : TemplateCodePage
    {
        string render() => @"<h1>Shadowed Template Code Page</h1>";
    }

    [Page("shadowed/index")]
    public class ShadowedIndexPage : TemplateCodePage
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
    
    public class TemplateIntegrationTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(TemplateIntegrationTests), typeof(MyTemplateServices).GetAssembly()) {}

            public override void Configure(Container container)
            {
                container.Register<IDbConnectionFactory>(new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

                using (var db = container.Resolve<IDbConnectionFactory>().Open())
                {
                    db.DropAndCreateTable<Rockstar>();
                    db.InsertAll(UnitTestExample.SeedData);
                }
                
                Plugins.Add(new TemplatePagesFeature());

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
                files.WriteFile("dir/_layout.html", @"
<html>
<body id=dir>
{{ page }}
</body>
</html>
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
                
            }

            public readonly List<IVirtualPathProvider> TemplateFiles = new List<IVirtualPathProvider> { new MemoryVirtualFiles() };
            public override List<IVirtualPathProvider> GetVirtualFileSources() => TemplateFiles;
        }

        private readonly ServiceStackHost appHost;
        public TemplateIntegrationTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Does_direct_page_with_layout()
        {
            var html = Config.ListeningOn.AppendPath("direct-page").GetStringFromUrl();
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=root>

<h1>Direct Page</h1>

</body>
</html>
".NormalizeNewLines()));
        }

        [Test]
        public void Does_return_shadowed_code_page_with_layout()
        {
            var html = Config.ListeningOn.AppendPath("shadowed-page").GetStringFromUrl();
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
            var html = Config.ListeningOn.AppendPath("shadowed").GetStringFromUrl();
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
            var html = Config.ListeningOn.AppendPath("rockstar").AddQueryParam("id", "1").GetStringFromUrl();
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
            var html = Config.ListeningOn.AppendPath("rockstar-pages", "1").GetStringFromUrl();
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
            var html = Config.ListeningOn.AppendPath("rockstar-pages", "1").AddQueryParam("layout", "custom_layout").GetStringFromUrl();
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

    }
}