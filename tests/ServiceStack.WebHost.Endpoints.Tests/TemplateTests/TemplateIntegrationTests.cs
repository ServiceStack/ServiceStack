using System;
using System.Collections.Generic;
using System.Net;
using Funq;
using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Templates;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class MyTemplateServices : Service
    {
        
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
    
    public class TemplateIntegrationTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(TemplateIntegrationTests), typeof(MyTemplateServices).GetAssembly()) {}

            public override void Configure(Container container)
            {
                Plugins.Add(new TemplatePagesFeature());

                var files = TemplateFiles[0];
                
                files.WriteFile("_layout.html", @"
<html>
<body id=root>
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
    }
}