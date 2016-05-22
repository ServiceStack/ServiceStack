using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Html;
using ServiceStack.Razor;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.ServiceHost.Tests.Formats_Razor
{
    [TestFixture]
    public class RazorEngineTests
    {
        const string LayoutHtml = "<html><body><div>@RenderSection(\"Title\")</div>@RenderBody()</body></html>";

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

        protected RazorFormat RazorFormat;

        public virtual bool PrecompileEnabled { get { return false; } }
        public virtual bool WaitForPrecompileEnabled { get { return false; } }

        [SetUp]
        public void OnBeforeEachTest()
        {
            RazorFormat.Instance = null;

            var fileSystem = new InMemoryVirtualPathProvider(new BasicAppHost());
            fileSystem.WriteFile("/views/TheLayout.cshtml", LayoutHtml);
            InitializeFileSystem(fileSystem);

            RazorFormat = new RazorFormat
            {
                VirtualFileSources = fileSystem,
                PageBaseType = typeof(CustomRazorBasePage<>),
                EnableLiveReload = false,
                PrecompilePages = PrecompileEnabled,
                WaitForPrecompilationOnStartup = WaitForPrecompileEnabled,
            }.Init();
        }

        protected virtual void InitializeFileSystem(InMemoryVirtualPathProvider fileSystem)
        {
        }

        [Test]
        public void Can_compile_simple_template()
        {
            const string template = "This is my sample template, Hello @Model.Name!";
            var result = RazorFormat.CreateAndRenderToHtml(template, model: new { Name = "World" });

            Assert.That(result, Is.EqualTo("This is my sample template, Hello World!"));
        }

        [Test]
        public void Can_compile_simple_template_by_name()
        {
            const string template = "This is my sample template, Hello @Model.Name!";
            RazorFormat.AddFileAndPage("/simple.cshtml", template);
            var result = RazorFormat.RenderToHtml("/simple.cshtml", new { Name = "World" });

            Assert.That(result, Is.EqualTo("This is my sample template, Hello World!"));
        }

        [Test]
        public void Can_compile_simple_template_by_name_with_layout()
        {
            const string template = "@{ Layout = \"TheLayout.cshtml\"; }This is my sample template, Hello @Model.Name!";
            RazorFormat.AddFileAndPage("/simple.cshtml", template);

            var result = RazorFormat.RenderToHtml("/simple.cshtml", model: new { Name = "World" });
            Assert.That(result, Is.EqualTo("<html><body><div></div>This is my sample template, Hello World!</body></html>"));

            var result2 = RazorFormat.RenderToHtml("/simple.cshtml", model: new { Name = "World2" }, layout: "bare");
            Assert.That(result2, Is.EqualTo("This is my sample template, Hello World2!"));
        }

        [Test]
        public void Can_get_executed_template_by_name_with_layout()
        {
            const string html = "@{ Layout = \"TheLayout.cshtml\"; }This is my sample template, Hello @Model.Name!";
            RazorFormat.AddFileAndPage("/simple2.cshtml", html);

            var result = RazorFormat.RenderToHtml("/simple2.cshtml", new { Name = "World" });

            Assert.That(result, Is.EqualTo("<html><body><div></div>This is my sample template, Hello World!</body></html>"));
        }

        [Test]
        public void Can_get_executed_template_by_name_with_section()
        {
            const string html = "@{ Layout = \"TheLayout.cshtml\"; }This is my sample template, @section Title {<h1>Hello @Model.Name!</h1>}";
            var page = RazorFormat.AddFileAndPage("/views/simple3.cshtml", html);

            IRazorView view;
            var result = RazorFormat.RenderToHtml(page, out view, model: new { Name = "World" });

            Assert.That(result, Is.EqualTo("<html><body><div><h1>Hello World!</h1></div>This is my sample template, </body></html>"));

            Assert.That(view.ChildPage.Layout, Is.EqualTo("TheLayout.cshtml"));

            Assert.That(view.ChildPage.IsSectionDefined("Title"), Is.True);

            var titleResult = view.ChildPage.RenderSectionToHtml("Title");
            Assert.That(titleResult, Is.EqualTo("<h1>Hello World!</h1>"));
        }

        [Test]
        public void Can_compile_template_with_RenderBody()
        {
            const string html = "@{ Layout = \"TheLayout.cshtml\"; }This is my sample template, @section Title {<h1>Hello @Model.Name!</h1>}";
            var page = RazorFormat.AddFileAndPage("/views/simple4.cshtml", html);

            var result = RazorFormat.RenderToHtml(page, model: new { Name = "World" });

            result.Print();
            Assert.That(result, Is.EqualTo("<html><body><div><h1>Hello World!</h1></div>This is my sample template, </body></html>"));
        }

        [Test]
        public void Rendering_is_thread_safe()
        {
            const string template = "This is my sample template, Hello @Model.Name!";
            RazorFormat.AddFileAndPage("/simple.cshtml", template);

            Parallel.For(0, 10, i =>
            {
                var result = RazorFormat.RenderToHtml("/simple.cshtml", new { Name = "World" });
                Assert.That(result, Is.EqualTo("This is my sample template, Hello World!"));
            });
        }

    }
}
