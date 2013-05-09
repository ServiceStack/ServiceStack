using System;
using NUnit.Framework;
using ServiceStack.Html;
using ServiceStack.Razor;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.ServiceHost.Tests.Formats_Razor
{
    [TestFixture]
    public class RazorEngineTests
    {
        const string LayoutHtml = "<html><body><div>@RenderSection(\"Title\")</div>@RenderBody()</body></html>";

        RazorFormat razorFormat;

        [SetUp]
        public void OnBeforeEachTest()
        {
            RazorFormat.Instance = null;
            razorFormat = new RazorFormat
            {
                VirtualPathProvider = new InMemoryVirtualPathProvider(new BasicAppHost()),
                PageBaseType = typeof(CustomRazorBasePage<>),
                EnableLiveReload = false,
            }.Init();

            razorFormat.AddFileAndPage("/views/TheLayout.cshtml", LayoutHtml);
        }

        [Test]
        public void Can_compile_simple_template()
        {
            const string template = "This is my sample template, Hello @Model.Name!";
            var result = razorFormat.CreateAndRenderToHtml(template, model: new { Name = "World" });

            Assert.That(result, Is.EqualTo("This is my sample template, Hello World!"));
        }

        [Test]
        public void Can_compile_simple_template_by_name()
        {
            const string template = "This is my sample template, Hello @Model.Name!";
            razorFormat.AddFileAndPage("/simple.cshtml", template);
            var result = razorFormat.RenderToHtml("/simple.cshtml", new { Name = "World" });

            Assert.That(result, Is.EqualTo("This is my sample template, Hello World!"));
        }

        [Test]
        public void Can_compile_simple_template_by_name_with_layout()
        {
            const string template = "@{ Layout = \"TheLayout.cshtml\"; }This is my sample template, Hello @Model.Name!";
            razorFormat.AddFileAndPage("/simple.cshtml", template);

            var result = razorFormat.RenderToHtml("/simple.cshtml", model: new { Name = "World" });
            Assert.That(result, Is.EqualTo("<html><body><div></div>This is my sample template, Hello World!</body></html>"));

            var result2 = razorFormat.RenderToHtml("/simple.cshtml", model: new { Name = "World2" }, layout:"bare");
            Assert.That(result2, Is.EqualTo("This is my sample template, Hello World2!"));
        }

        [Test]
        public void Can_get_executed_template_by_name_with_layout()
        {
            const string html = "@{ Layout = \"TheLayout.cshtml\"; }This is my sample template, Hello @Model.Name!";
            razorFormat.AddFileAndPage("/simple2.cshtml", html);

            var result = razorFormat.RenderToHtml("/simple2.cshtml", new { Name = "World" });

            Assert.That(result, Is.EqualTo("<html><body><div></div>This is my sample template, Hello World!</body></html>"));
        }

        [Test]
        public void Can_get_executed_template_by_name_with_section()
        {
            const string html = "@{ Layout = \"TheLayout.cshtml\"; }This is my sample template, @section Title {<h1>Hello @Model.Name!</h1>}";
            var page = razorFormat.AddFileAndPage("/views/simple3.cshtml", html);

            IRazorView view;
            var result = razorFormat.RenderToHtml(page, out view, model: new { Name = "World" });

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
            var page = razorFormat.AddFileAndPage("/views/simple4.cshtml", html);

            var result = razorFormat.RenderToHtml(page, model: new { Name = "World" });

            result.Print();
            Assert.That(result, Is.EqualTo("<html><body><div><h1>Hello World!</h1></div>This is my sample template, </body></html>"));
        }

    }
}
