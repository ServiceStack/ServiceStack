using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.Text;
using ServiceStack.WebHost.EndPoints.Formats;

namespace ServiceStack.ServiceHost.Tests.Formats
{
	[TestFixture]
	public class MarkdownFormatTests
	{
		public class Person
		{
			public string FirstName { get; set; }
			public string LastName { get; set; }
		}

		private MarkdownFormat markdownFormat;

		string staticTemplatePath;
		string staticTemplateContent;
		string dynamicPagePath;
		string dynamicPageContent;
		string dynamicListPagePath;
		string dynamicListPageContent;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			staticTemplatePath = "~/AppData/Template/default.htm".MapAbsolutePath();
			staticTemplateContent = File.ReadAllText(staticTemplatePath);

			dynamicPagePath = "~/AppData/Template/DynamicTpl.md".MapAbsolutePath();
			dynamicPageContent = File.ReadAllText(dynamicPagePath);

			dynamicListPagePath = "~/AppData/Template/DynamicListTpl.md".MapAbsolutePath();
			dynamicListPageContent = File.ReadAllText(dynamicListPagePath);
		}

		[SetUp]
		public void OnBeforeEachTest()
		{
			markdownFormat = new MarkdownFormat();
		}

		[Test]
		public void Can_load_all_markdown_files()
		{
			markdownFormat.RegisterMarkdownPages("~/".MapAbsolutePath());

			var expectedPageNames = new[] {
				"Dynamic", "Static", "DynamicListTpl", "DynamicNestedTpl", "DynamicTpl", "StaticTpl" 
			};

			Assert.That(markdownFormat.Pages.Count, Is.EqualTo(expectedPageNames.Length));
			Assert.That(markdownFormat.PageTemplates.Count, Is.EqualTo(2));

			var pageNames = new List<string>();
			markdownFormat.Pages.ForEach((k, v) => pageNames.Add(k));

			Console.WriteLine(pageNames.Dump());
			Assert.That(pageNames.EquivalentTo(expectedPageNames));
		}

		[Test]
		public void Can_Render_StaticPage()
		{
			markdownFormat.RegisterMarkdownPages("~/".MapAbsolutePath());
			var html = markdownFormat.RenderStaticPage("Static");

			Assert.That(html, Is.Not.Null);
			Assert.That(html, Is.StringStarting("<h1>Static Markdown template</h1>"));
		}

		[Test]
		public void Can_Render_StaticPage_WithTemplate()
		{
			markdownFormat.RegisterMarkdownPages("~/".MapAbsolutePath());
			var html = markdownFormat.RenderStaticPage("StaticTpl");

			Console.WriteLine(html);

			Assert.That(html, Is.Not.Null);
			Assert.That(html, Is.StringStarting("<!doctype html>"));
		}

		[Test]
		public void Can_Render_DynamicPage()
		{
			var person = new Person { FirstName = "Demis", LastName = "Bellot" };
			markdownFormat.RegisterMarkdownPages("~/".MapAbsolutePath());

			var html = markdownFormat.RenderDynamicPage("Dynamic", person);

			var expectedHtml = markdownFormat.Transform(dynamicPageContent)
				.Replace("@Model.FirstName", person.FirstName)
				.Replace("@Model.LastName", person.LastName);

			Console.WriteLine("Template: " + html);
			Assert.That(html, Is.EqualTo(expectedHtml));
		}
	}
}