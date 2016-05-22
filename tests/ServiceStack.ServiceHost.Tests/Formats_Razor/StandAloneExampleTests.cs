using NUnit.Framework;
using ServiceStack.Razor;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.ServiceHost.Tests.Formats_Razor
{
    [TestFixture]
    public class StandAloneExampleTests
    {
        private ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new BasicAppHost().Init();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Simple_static_example()
        {
            RazorFormat.Instance = null;
            var razor = new RazorFormat
            {
                VirtualFileSources = new InMemoryVirtualPathProvider(new BasicAppHost()),
                EnableLiveReload = false,
            }.Init();

            var page = razor.CreatePage("Hello @Model.Name! Welcome to Razor!");
            var html = razor.RenderToHtml(page, new { Name = "World" });
            html.Print();

            Assert.That(html, Is.EqualTo("Hello World! Welcome to Razor!"));
        }
    }
}
