using System.Collections.Generic;
using Funq;
using NUnit.Framework;
using ServiceStack.Formats;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    [Route("/view-pages/{Name}")]
    public class TemplateViewPage : IReturn<TemplateViewPageResponse>
    {
        public string Name { get; set; }
    }
    public class TemplateViewPageResponse
    {
        public string Name { get; set; }
    }
    
    [Route("/view-pages-request/{Name}")]
    public class TemplateViewPageRequest : IReturn<TemplateViewPageRequest>
    {
        public string Name { get; set; }
    }
    
    [Route("/view-pages-nested/{Name}")]
    public class TemplateViewPageNested : IReturn<TemplateViewPageNested>
    {
        public string Name { get; set; }
    }
    
    [Route("/view-pages-nested-sub/{Name}")]
    public class TemplateViewPageNestedSub : IReturn<TemplateViewPageNested>
    {
        public string Name { get; set; }
    }
    
    [Route("/view-pages-custom/{Name}")]
    public class TemplateViewPageCustom : IReturn<TemplateViewPageCustom>
    {
        public string Name { get; set; }
        public string View { get; set; }
        public string Layout { get; set; }
    }

    public class TemplateViewPagesServices : Service
    {
        public object Any(TemplateViewPage request) => new TemplateViewPageResponse { Name = request.Name };
        public object Any(TemplateViewPageRequest request) => request;
        public object Any(TemplateViewPageNested request) => request;
        public object Any(TemplateViewPageNestedSub request) => request;
        public object Any(TemplateViewPageCustom request) => new HttpResult(request)
        {
            View = request.View,
            Template = request.Layout,
        };
    }
    
    public class TemplateViewPagesTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(TemplateViewPagesTests), typeof(TemplateViewPagesServices).Assembly) {}

            public readonly List<IVirtualPathProvider> TemplateFiles = new List<IVirtualPathProvider>
            {
                new MemoryVirtualFiles(),
                new ResourceVirtualFiles(typeof(HtmlFormat).Assembly),
            };

            public override List<IVirtualPathProvider> GetVirtualFileSources() => TemplateFiles;

            public override void Configure(Container container)
            {
                Plugins.Add(new TemplatePagesFeature());

                var files = TemplateFiles[0];
                
                files.WriteFile("_layout.html", @"
<html>
<body id=root>
{{ page }}
{{ htmlErrorDebug }}
</body>
</html>
");
                
                files.WriteFile("alt-layout.html", @"
<html>
<body id=alt-root>
{{ page }}
{{ htmlErrorDebug }}
</body>
</html>
");
                
                files.WriteFile("Views/_layout.html", @"
<html>
<body id=views>
{{ page }}
{{ htmlErrorDebug }}
</body>
</html>
");

                files.WriteFile("Views/TemplateViewPageRequest.html", @"
<h1>TemplateViewPageRequest</h1>
<p>Name: {{ Name }}</p>
");

                files.WriteFile("Views/TemplateViewPageRequest.html", @"
<h1>TemplateViewPageRequest</h1>
<p>Name: {{ Name }}</p>
");

                files.WriteFile("Views/TemplateViewPageResponse.html", @"
<h1>TemplateViewPageResponse</h1>
<p>Name: {{ Name }}</p>
");

                files.WriteFile("Views/nested/TemplateViewPageNested.html", @"
<h1>TemplateViewPageNested</h1>
<p>Name: {{ Name }}</p>
");

                files.WriteFile("Views/nested/sub/TemplateViewPageNestedSub.html", @"
<h1>TemplateViewPageNestedSub</h1>
<p>Name: {{ Name }}</p>
");
                files.WriteFile("Views/nested/sub/_layout.html", @"
<html>
<body id=views-nested-sub>
{{ page }}
{{ htmlErrorDebug }}
</body>
</html>
");

            }
        }
        public static string BaseUrl = Config.ListeningOn;
        
        private readonly ServiceStackHost appHost;
        public TemplateViewPagesTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(BaseUrl);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Does_render_TemplateViewPageResponse_on_HTML_requests()
        {
            var html = BaseUrl.CombineWith("view-pages", "test")
                .GetStringFromUrl(accept: MimeTypes.Html);
            
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=views>

<h1>TemplateViewPageResponse</h1>
<p>Name: test</p>


</body>
</html>
".NormalizeNewLines()));
        }

        [Test]
        public void Does_render_TemplateViewPageRequest_on_HTML_requests()
        {
            var html = BaseUrl.CombineWith("view-pages-request", "test")
                .GetStringFromUrl(accept: MimeTypes.Html);
            
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=views>

<h1>TemplateViewPageRequest</h1>
<p>Name: test</p>


</body>
</html>
".NormalizeNewLines()));
        }

        [Test]
        public void Does_render_TemplateViewPageNested_on_HTML_requests()
        {
            var html = BaseUrl.CombineWith("view-pages-nested", "test")
                .GetStringFromUrl(accept: MimeTypes.Html);
            
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=views>

<h1>TemplateViewPageNested</h1>
<p>Name: test</p>


</body>
</html>
".NormalizeNewLines()));
        }

        [Test]
        public void Does_render_TemplateViewPageNestedSub_on_HTML_requests()
        {
            var html = BaseUrl.CombineWith("view-pages-nested-sub", "test")
                .GetStringFromUrl(accept: MimeTypes.Html);
            
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=views-nested-sub>

<h1>TemplateViewPageNestedSub</h1>
<p>Name: test</p>


</body>
</html>
".NormalizeNewLines()));
        }

        [Test]
        public void Does_render_TemplateViewPageCustom_with_custom_view()
        {
            var html = BaseUrl.CombineWith("view-pages-custom", "test")
                .AddQueryParam("view", "TemplateViewPageRequest")
                .GetStringFromUrl(accept: MimeTypes.Html);
            
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=views>

<h1>TemplateViewPageRequest</h1>
<p>Name: test</p>


</body>
</html>
".NormalizeNewLines()));
            
            html = BaseUrl.CombineWith("view-pages-custom", "test")
                .AddQueryParam("view", "TemplateViewPageResponse")
                .AddQueryParam("layout", "alt-layout")
                .GetStringFromUrl(accept: MimeTypes.Html);
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=alt-root>

<h1>TemplateViewPageResponse</h1>
<p>Name: test</p>


</body>
</html>
".NormalizeNewLines()));
            
        }
    }
}
