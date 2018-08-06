using System.Collections.Generic;
using Funq;
using NUnit.Framework;
using ServiceStack.IO;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplatePageBasedRoutingTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(nameof(TemplatePageTests), typeof(TemplatePagesService).Assembly) { }

            public override void Configure(Container container)
            {
                Plugins.Add(new TemplatePagesFeature());
            }

            static readonly Dictionary<string,string> HtmlFiles = new Dictionary<string, string>
            {
                { "_layout.html", "<html><body>{{ page }}</body></html>" },
                { "_slug/index.html", "_slug/index.html slug: {{slug}}" },
                { "_slug/_category.html", "_slug/_category.html slug: {{slug}}, category: {{category}}" },
                { "comments/_postid/_id.html", "comments/_postid/_id.html postid: {{postid}}, id: {{id}}" },
                { "favorites/index.html", "favorites/index.html" },
                { "login/_provider.html", "login/_provider.html provider: {{provider}}" },
                { "organizations/_slug/index.html", "organizations/_slug/index.html slug: {{slug}}" },
                { "organizations/index.html", "organizations/index.html" },
                { "posts/_id/_postslug.html", "posts/_id/_postslug.html id: {{id}}, postslug: {{postslug}}" },
                { "stacks/index.html", "stacks/index.html" },
                { "stacks/new.html", "stacks/new.html" },
                { "stacks/_slug/index.html", "stacks/_slug/index.html slug: {{slug}}" },
                { "stacks/_slug/edit.html", "stacks/_slug/edit.html slug: {{slug}}" },
                { "tech/index.html", "tech/index.html" },
                { "tech/new.html", "tech/new.html" },
                { "tech/_slug/index.html", "tech/_slug/index.html slug: {{slug}}" },
                { "tech/_slug/edit.html", "tech/_slug/edit.html slug: {{slug}}" },
                { "top/index.html", "top/index.html" },
                { "users/_username.html", "users/_username.html username: {{username}}" },
            };

            public override List<IVirtualPathProvider> GetVirtualFileSources()
            {
                var existingProviders = base.GetVirtualFileSources();
                var memFs = new MemoryVirtualFiles();

                foreach (var entry in HtmlFiles)
                {
                    memFs.AppendFile(entry.Key, entry.Value);
                }

                existingProviders.Insert(0, memFs);
                return existingProviders;
            }
        }

        private readonly ServiceStackHost appHost;
        public TemplatePageBasedRoutingTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown] public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        [TestCase("/redis", "<html><body>_slug/index.html slug: redis</body></html>")]
        [TestCase("/redis/", "<html><body>_slug/index.html slug: redis</body></html>")]
        [TestCase("/redis/clients", "<html><body>_slug/_category.html slug: redis, category: clients</body></html>")]
        [TestCase("/comments/1/2", "<html><body>comments/_postid/_id.html postid: 1, id: 2</body></html>")]
        [TestCase("/favorites", "<html><body>favorites/index.html</body></html>")]
        [TestCase("/login/github", "<html><body>login/_provider.html provider: github</body></html>")]
        [TestCase("/organizations/redis", "<html><body>organizations/_slug/index.html slug: redis</body></html>")]
        [TestCase("/organizations", "<html><body>organizations/index.html</body></html>")]
        [TestCase("/posts/1/the-slug", "<html><body>posts/_id/_postslug.html id: 1, postslug: the-slug</body></html>")]
        [TestCase("/stacks", "<html><body>stacks/index.html</body></html>")]
        [TestCase("/stacks/new", "<html><body>stacks/new.html</body></html>")]
        [TestCase("/stacks/redis", "<html><body>stacks/_slug/index.html slug: redis</body></html>")]
        [TestCase("/stacks/redis/edit", "<html><body>stacks/_slug/edit.html slug: redis</body></html>")]
        [TestCase("/tech", "<html><body>tech/index.html</body></html>")]
        [TestCase("/tech/new", "<html><body>tech/new.html</body></html>")]
        [TestCase("/tech/redis", "<html><body>tech/_slug/index.html slug: redis</body></html>")]
        [TestCase("/tech/redis/edit", "<html><body>tech/_slug/edit.html slug: redis</body></html>")]
        [TestCase("/top", "<html><body>top/index.html</body></html>")]
        [TestCase("/users/mythz", "<html><body>users/_username.html username: mythz</body></html>")]
        public void Can_use_page_based_routing(string path, string expectedHtml)
        {
            var html = Config.ListeningOn.CombineWith(path)
                .GetStringFromUrl();
            
            Assert.That(html, Is.EqualTo(expectedHtml));
        }
    }
}