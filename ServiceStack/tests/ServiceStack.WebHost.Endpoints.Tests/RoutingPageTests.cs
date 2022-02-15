using System;
using System.IO;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class RoutingPageTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(nameof(RoutingPageTests), typeof(RoutingPageTests).Assembly) { }
        
            public override void Configure(Container container)
            {
                SetConfig(new HostConfig {
                    DebugMode = true, // need this to disable caching
                });
                Plugins.Add(new SharpPagesFeature());
            }
        }

        protected ServiceStackHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            try
            {
                appHost.VirtualFileSources.GetMemoryVirtualFiles().Clear();
                appHost.VirtualFileSources.GetFileSystemVirtualFiles().DeleteFolder("dir");
            }
            catch (IOException)
            {
                //sometimes throws The process cannot access the file 'bundle.js' because it is being used by another process.
            }
        }
        
        public static StringDictionary Files = new StringDictionary {
            { "dir/contacts/_layout.html", PageHtml("{{ page }}") },
            { "dir/contacts/index.html", "<h2>/contacts/index.html</h2>" },
            { "dir/contacts/_id/edit.html", "<h2>/_id/edit.html {{id}}</h2>" },
            { "dir/contacts/edit/_id.html", "<h2>/edit/_id.html {{id}}</h2>" },
        };

        static string PageHtml(string pageHtml) => $"<html><body><h1>/contacts/_layout.html</h1>{pageHtml}</body></html>";

        public static StringDictionary Expected = new StringDictionary {
            { "dir/contacts/index.html", PageHtml("<h2>/contacts/index.html</h2>") },
            { "dir/contacts/index", PageHtml("<h2>/contacts/index.html</h2>") },
            { "dir/contacts/", PageHtml("<h2>/contacts/index.html</h2>") },
            { "dir/contacts/1/edit", PageHtml("<h2>/_id/edit.html 1</h2>") },
            { "dir/contacts/edit/1", PageHtml("<h2>/edit/_id.html 1</h2>") },
        };

        private static void AssertCanGetRoutingPages(IVirtualFiles vfs, Action fn=null)
        {
            Files.ForEach(vfs.WriteFile);

            foreach (var entry in Expected)
            {
                entry.Key.Print();
                var html = Config.ListeningOn.CombineWith(entry.Key).GetStringFromUrl(accept: MimeTypes.Html);
                Assert.That(html.NormalizeNewLines(), Is.EqualTo(entry.Value));
            }
            
            fn?.Invoke();
        }

        [Test]
        public void Can_get_routing_pages_from_FileSystemVirtualFiles()
        {
            AssertCanGetRoutingPages(appHost.VirtualFileSources.GetFileSystemVirtualFiles());
        }

        [Test]
        public void Can_get_routing_pages_from_MemoryVirtualFiles()
        {
            AssertCanGetRoutingPages(appHost.VirtualFileSources.GetMemoryVirtualFiles());
        }
 
        [Test]
        public void Can_get_routing_pages_from_FileSystemVirtualFiles_when_file_in_MemoryVfs()
        {
            var memFs = appHost.VirtualFileSources.GetMemoryVirtualFiles();
            memFs.WriteFile("alt-dir/page.html", "<h2>alt-dir/page.html</h2>");
            
            AssertCanGetRoutingPages(appHost.VirtualFileSources.GetFileSystemVirtualFiles(), () => {
                var html = Config.ListeningOn.CombineWith("alt-dir/page").GetStringFromUrl(accept:MimeTypes.Html);
                Assert.That(html, Is.EqualTo("<h2>alt-dir/page.html</h2>"));
            });
        }
 
        [Test]
        public void Can_get_routing_pages_from_FileSystemVirtualFiles_when_file_in_dir_in_MemoryVfs()
        {
            var memFs = appHost.VirtualFileSources.GetMemoryVirtualFiles();
            memFs.WriteFile("dir/page.html", "<h2>dir/page.html</h2>");
            memFs.WriteFile("dir/contacts/page.html", "<h2>dir/contacts/page.html</h2>");
            
            AssertCanGetRoutingPages(appHost.VirtualFileSources.GetFileSystemVirtualFiles(), () => {
                var html = Config.ListeningOn.CombineWith("dir/page").GetStringFromUrl(accept:MimeTypes.Html);
                Assert.That(html.NormalizeNewLines(), Is.EqualTo("<h2>dir/page.html</h2>"));

                html = Config.ListeningOn.CombineWith("dir/contacts/page").GetStringFromUrl(accept:MimeTypes.Html);
                Assert.That(html.NormalizeNewLines(), Is.EqualTo(PageHtml("<h2>dir/contacts/page.html</h2>")));
            });
        }
        
        static string BundledJsPageHtml(string pageHtml) => $"<html><body><script src=\"/dir/js/bundle.js\"></script><h1>/contacts/_layout.html</h1>{pageHtml}</body></html>";

        public static StringDictionary BundledJsFiles => new StringDictionary {
            { "dir/js/default.js", "function fn(){  }" },
            { "dir/lib/js/a.js", "function a(){  }" },
            { "dir/lib/js/b.js", "function b(){  }" },
            { "dir/lib/js/c.js", "function c(){  }" },
            { "dir/contacts/_layout.html", "<html><body>{{ ['/dir/lib/js','/dir/js/default.js'] | bundleJs({disk:false,out:'/dir/js/bundle.js'}) }}<h1>/contacts/_layout.html</h1>{{page}}</body></html>" },
            { "dir/contacts/index.html", "<h2>/contacts/index.html</h2>" },
            { "dir/contacts/_id/edit.html", "<h2>/_id/edit.html {{id}}</h2>" },
            { "dir/contacts/edit/_id.html", "<h2>/edit/_id.html {{id}}</h2>" },
        };

        public static StringDictionary BundledJsExpected => new StringDictionary {
            { "dir/contacts/", BundledJsPageHtml("<h2>/contacts/index.html</h2>") },
            { "dir/contacts/1/edit", BundledJsPageHtml("<h2>/_id/edit.html 1</h2>") },
            { "dir/contacts/edit/1", BundledJsPageHtml("<h2>/edit/_id.html 1</h2>") },
        };

        public static string ExpectedBundleJs = "function a(){};\n\nfunction b(){};\n\nfunction c(){};\n\nfunction fn(){};";

        [Test]
        [Ignore("Needs review - MONOREPO")]
        public void Can_get_Routing_Page_after_in_Memory_js_Bundle()
        {
            var fs = appHost.VirtualFileSources.GetFileSystemVirtualFiles();
            BundledJsFiles.ForEach(fs.WriteFile);
            
            foreach (var entry in BundledJsExpected)
            {
                entry.Key.Print();
                var html = Config.ListeningOn.CombineWith(entry.Key).GetStringFromUrl(accept: MimeTypes.Html);
                Assert.That(html.NormalizeNewLines(), Is.EqualTo(entry.Value));
            }
            
            var memFs = appHost.VirtualFileSources.GetMemoryVirtualFiles();
            var bundleJs = memFs.GetFile("dir/js/bundle.js")?.ReadAllText();
            Assert.That(bundleJs, Is.Not.Null);
            $"\nbundle.js:\n{bundleJs}".Print();
            Assert.That(bundleJs.NormalizeNewLines(), Is.EqualTo(ExpectedBundleJs));

            bundleJs = Config.ListeningOn.CombineWith("/dir/js/bundle.js").GetStringFromUrl();
            Assert.That(bundleJs.NormalizeNewLines(), Is.EqualTo(ExpectedBundleJs));
        }

        public static string ExpectedOnDiskBundleJs = "function a(){  }\nfunction b(){  }\nfunction c(){  }\nfunction fn(){  }";

        [Test]
        [Ignore("Needs review - MONOREPO")]
        public void Can_get_Routing_Page_after_on_disk_js_Bundle()
        {
            var fs = appHost.VirtualFileSources.GetFileSystemVirtualFiles();
            var files = BundledJsFiles;
            files["dir/contacts/_layout.html"] = files["dir/contacts/_layout.html"]
                .Replace("disk:false", "disk:true,minify:false"); 
            files.ForEach(fs.WriteFile);
            
            foreach (var entry in BundledJsExpected)
            {
                entry.Key.Print();
                var html = Config.ListeningOn.CombineWith(entry.Key).GetStringFromUrl(accept: MimeTypes.Html);
                Assert.That(html.NormalizeNewLines(), Is.EqualTo(entry.Value));
            }
            
            var bundleJs = fs.GetFile("dir/js/bundle.js")?.ReadAllText();
            Assert.That(bundleJs, Is.Not.Null);
            $"\nbundle.js:\n{bundleJs}".Print();
            Assert.That(bundleJs.NormalizeNewLines(), Is.EqualTo(ExpectedOnDiskBundleJs));

            bundleJs = Config.ListeningOn.CombineWith("/dir/js/bundle.js").GetStringFromUrl();
            Assert.That(bundleJs.NormalizeNewLines(), Is.EqualTo(ExpectedOnDiskBundleJs));
        }
        
        static string BundledCssPageHtml(string pageHtml) => $"<html><body><link rel=\"stylesheet\" href=\"/dir/css/bundle.css\"><h1>/contacts/_layout.html</h1>{pageHtml}</body></html>";

        public static StringDictionary BundledCssFiles => new StringDictionary {
            { "dir/css/default.css", "body {  }" },
            { "dir/lib/css/a.css", ".a {  }" },
            { "dir/lib/css/b.css", ".b {  }" },
            { "dir/lib/css/c.css", ".c {  }" },
            { "dir/contacts/_layout.html", "<html><body>{{ ['/dir/lib/css','/dir/css/default.css'] | bundleCss({disk:false,out:'/dir/css/bundle.css'}) }}<h1>/contacts/_layout.html</h1>{{page}}</body></html>" },
            { "dir/contacts/index.html", "<h2>/contacts/index.html</h2>" },
            { "dir/contacts/_id/edit.html", "<h2>/_id/edit.html {{id}}</h2>" },
            { "dir/contacts/edit/_id.html", "<h2>/edit/_id.html {{id}}</h2>" },
        };

        public static StringDictionary BundledCssExpected => new StringDictionary {
            { "dir/contacts/", BundledCssPageHtml("<h2>/contacts/index.html</h2>") },
            { "dir/contacts/1/edit", BundledCssPageHtml("<h2>/_id/edit.html 1</h2>") },
            { "dir/contacts/edit/1", BundledCssPageHtml("<h2>/edit/_id.html 1</h2>") },
        };

        public static string ExpectedBundleCss = ".a{}\n.b{}\n.c{}\nbody{}";

        [Test]
        public void Can_get_Routing_Page_after_in_Memory_css_Bundle()
        {
            var fs = appHost.VirtualFileSources.GetFileSystemVirtualFiles();
            BundledCssFiles.ForEach(fs.WriteFile);
            
            foreach (var entry in BundledCssExpected)
            {
                entry.Key.Print();
                var html = Config.ListeningOn.CombineWith(entry.Key).GetStringFromUrl(accept: MimeTypes.Html);
                Assert.That(html.NormalizeNewLines(), Is.EqualTo(entry.Value));
            }
            
            var memFs = appHost.VirtualFileSources.GetMemoryVirtualFiles();
            var bundleCss = memFs.GetFile("dir/css/bundle.css")?.ReadAllText();
            Assert.That(bundleCss, Is.Not.Null);
            $"\nbundle.css:\n{bundleCss}".Print();
            Assert.That(bundleCss.NormalizeNewLines(), Is.EqualTo(ExpectedBundleCss));

            bundleCss = Config.ListeningOn.CombineWith("/dir/css/bundle.css").GetStringFromUrl();
            Assert.That(bundleCss.NormalizeNewLines(), Is.EqualTo(ExpectedBundleCss));
        }

        public static string ExpectedOnDiskBundleCss = ".a {  }\n.b {  }\n.c {  }\nbody {  }";

        [Test]
        public void Can_get_Routing_Page_after_on_disk_css_Bundle()
        {
            var fs = appHost.VirtualFileSources.GetFileSystemVirtualFiles();
            var files = BundledCssFiles;
            files["dir/contacts/_layout.html"] = files["dir/contacts/_layout.html"]
                .Replace("disk:false", "disk:true,minify:false"); 
            files.ForEach(fs.WriteFile);
            
            foreach (var entry in BundledCssExpected)
            {
                entry.Key.Print();
                var html = Config.ListeningOn.CombineWith(entry.Key).GetStringFromUrl(accept: MimeTypes.Html);
                Assert.That(html.NormalizeNewLines(), Is.EqualTo(entry.Value));
            }
            
            var bundleCss = fs.GetFile("dir/css/bundle.css")?.ReadAllText();
            Assert.That(bundleCss, Is.Not.Null);
            $"\nbundle.js:\n{bundleCss}".Print();
            Assert.That(bundleCss.NormalizeNewLines(), Is.EqualTo(ExpectedOnDiskBundleCss));

            bundleCss = Config.ListeningOn.CombineWith("/dir/css/bundle.css").GetStringFromUrl();
            Assert.That(bundleCss.NormalizeNewLines(), Is.EqualTo(ExpectedOnDiskBundleCss));
        }
        
    }
}