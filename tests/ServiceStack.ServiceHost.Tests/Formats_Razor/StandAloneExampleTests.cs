using NUnit.Framework;
using ServiceStack.Razor;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.ServiceHost.Tests.Formats_Razor
{
    [TestFixture]
    public class StandAloneExampleTests
    {
        [Test]
        public void Simple_static_example()
        {
            RazorFormat.Instance = null;
            var razor = new RazorFormat {
                VirtualPathProvider = new InMemoryVirtualPathProvider(new BasicAppHost()),
                EnableLiveReload = false,
            }.Init();

            var page = razor.CreatePage("Hello @Model.Name! Welcome to Razor!");
            var html = razor.RenderToHtml(page, new { Name = "World" });
            html.Print();

            Assert.That(html, Is.EqualTo("Hello World! Welcome to Razor!"));
        }
    }
}
