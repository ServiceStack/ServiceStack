using System;
using NUnit.Framework;
using ServiceStack.Razor;

namespace ServiceStack.ServiceHost.Tests.Formats_Razor
{
	[TestFixture]
	public class RazorEngineTests
	{
		const string LayoutHtml = "<html><body><div>@RenderSection(\"Title\")</div>@RenderBody()</body></html>";

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			var mvcRazorFormat = new RazorFormat();
			mvcRazorFormat.Init(typeof(CustomRazorBasePage<>));
		}

		[SetUp]
		public void OnBeforeEachTest()
		{
            RazorHost.Compile(LayoutHtml, "TheLayout.cshtml");
		}

		[Test]
		public void Can_compile_simple_template()
		{
			const string template = "This is my sample template, Hello @Model.Name!";
            var result = RazorHost.Parse(template, new { Name = "World" });

			Assert.That(result, Is.EqualTo("This is my sample template, Hello World!"));
		}

		[Test]
		public void Can_compile_simple_template_by_name()
		{
			const string template = "This is my sample template, Hello @Model.Name!";
            RazorHost.Compile(template, "simple");
            var result = RazorHost.Run(new { Name = "World" }, "simple");

			Assert.That(result, Is.EqualTo("This is my sample template, Hello World!"));
		}

		[Test]
		public void Can_compile_simple_template_by_name_with_layout()
		{
			const string template = "@{ Layout = \"TheLayout.cshtml\"; }This is my sample template, Hello @Model.Name!";
            RazorHost.Compile(template, "simple");

            var result = RazorHost.Run(new { Name = "World" }, "simple");
			Assert.That(result, Is.EqualTo("This is my sample template, Hello World!"));

            var result2 = RazorHost.Run(new { Name = "World2" }, "simple");
			Assert.That(result2, Is.EqualTo("This is my sample template, Hello World2!"));
		}

		[Test]
		public void Can_get_executed_template_by_name_with_layout()
		{
			const string html = "@{ Layout = \"TheLayout.cshtml\"; }This is my sample template, Hello @Model.Name!";
            RazorHost.Compile(html, "simple2");

            var template = RazorHost.DefaultTemplateService.ExecuteTemplate(new { Name = "World" }, "simple2");

			Assert.That(template.ChildTemplate.Layout, Is.EqualTo("TheLayout.cshtml"));
			Assert.That(template.ChildTemplate.Sections.Count, Is.EqualTo(0));
			Assert.That(template.Result, Is.EqualTo("<html><body><div></div>This is my sample template, Hello World!</body></html>"));

            template = RazorHost.DefaultTemplateService.GetTemplate("simple2");
			Assert.That(template.Layout, Is.EqualTo("TheLayout.cshtml"));
		}

		[Test]
		public void Can_get_executed_template_by_name_with_section()
		{
			const string html = "@{ Layout = \"TheLayout.cshtml\"; }This is my sample template, @section Title {<h1>Hello @Model.Name!</h1>}";
            RazorHost.Compile(html, "simple3");

            var template = RazorHost.DefaultTemplateService.ExecuteTemplate(new { Name = "World" }, "simple3");

			Assert.That(template.ChildTemplate.Layout, Is.EqualTo("TheLayout.cshtml"));
			Assert.That(template.ChildTemplate.Sections.Count, Is.EqualTo(1));
			Assert.That(template.Result, Is.EqualTo("<html><body><div><h1>Hello World!</h1></div>This is my sample template, </body></html>"));

            template = RazorHost.DefaultTemplateService.GetTemplate("simple3");
			Assert.That(template.Layout, Is.EqualTo("TheLayout.cshtml"));

			var titleAction = template.Sections["Title"];
			template.Clear();
			titleAction();
			Assert.That(template.Result, Is.EqualTo("<h1>Hello World!</h1>"));
		}

		[Test]
		public void Can_compile_template_with_RenderBody()
		{
			const string html = "@{ Layout = \"TheLayout.cshtml\"; }This is my sample template, @section Title {<h1>Hello @Model.Name!</h1>}";
            RazorHost.Compile(html, "simple4");

            var template = RazorHost.DefaultTemplateService.ExecuteTemplate(new { Name = "World" }, "simple4");

			Console.WriteLine(template.Result);

			Assert.That(template.Result, Is.EqualTo("<html><body><div><h1>Hello World!</h1></div>This is my sample template, </body></html>"));
		}

	}
}