using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Formats;

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

		//string staticTemplatePath;
		//string staticTemplateContent;
		string dynamicPagePath;
		string dynamicPageContent;
		//string dynamicListPagePath;
		//string dynamicListPageContent;

		readonly string[] viewPageNames = new[] {
				"Dynamic", "Customer", "CustomerDetailsResponse", "DynamicListTpl", 
				"DynamicNestedTpl", "DynamicTpl", 
			};
		readonly string[] sharedViewPageNames = new[] {
				"DynamicShared", "DynamicTplShared",
			};
		readonly string[] contentPageNames = new[] {
				"Static", "StaticTpl",
			};


		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			//staticTemplatePath = "~/AppData/Template/default.shtml".MapAbsolutePath();
			//staticTemplateContent = File.ReadAllText(staticTemplatePath);

			dynamicPagePath = "~/Views/Template/DynamicTpl.md".MapProjectPath();
			dynamicPageContent = File.ReadAllText(dynamicPagePath);

			//dynamicListPagePath = "~/Views/Template/DynamicListTpl.md".MapAbsolutePath();
			//dynamicListPageContent = File.ReadAllText(dynamicListPagePath);
		}

		[SetUp]
		public void OnBeforeEachTest()
		{
			markdownFormat = new MarkdownFormat();
		}

		[Test]
		public void Can_load_all_markdown_files()
		{
			markdownFormat.RegisterMarkdownPages("~/".MapProjectPath());

			Assert.That(markdownFormat.ViewPages.Count, Is.EqualTo(viewPageNames.Length));
			Assert.That(markdownFormat.ViewSharedPages.Count, Is.EqualTo(sharedViewPageNames.Length));
			Assert.That(markdownFormat.ContentPages.Count, Is.EqualTo(contentPageNames.Length));
			Assert.That(markdownFormat.MasterPageTemplates.Count, Is.EqualTo(2));

			var pageNames = new List<string>();
			markdownFormat.ViewPages.ForEach((k, v) => pageNames.Add(k));

			Console.WriteLine(pageNames.Dump());
			Assert.That(pageNames.EquivalentTo(viewPageNames));
		}

		[Test]
		public void Can_Render_StaticPage()
		{
			markdownFormat.RegisterMarkdownPages("~/".MapProjectPath());
			var html = markdownFormat.RenderStaticPageHtml("~/AppData/NoTemplate/Static".MapProjectPath());

			Assert.That(html, Is.Not.Null);
			Assert.That(html, Is.StringStarting("<h1>Static Markdown template</h1>"));
		}

		[Test]
		public void Can_Render_StaticPage_WithTemplate()
		{
			markdownFormat.RegisterMarkdownPages("~/".MapProjectPath());
			var html = markdownFormat.RenderStaticPageHtml("~/AppData/Template/StaticTpl".MapProjectPath());

			Console.WriteLine(html);

			Assert.That(html, Is.Not.Null);
			Assert.That(html, Is.StringStarting("<!doctype html>"));
		}

		[Test]
		public void Can_Render_DynamicPage()
		{
			var person = new Person { FirstName = "Demis", LastName = "Bellot" };
			markdownFormat.RegisterMarkdownPages("~/".MapProjectPath());

			var html = markdownFormat.RenderDynamicPageHtml("Dynamic", person);

			var expectedHtml = markdownFormat.Transform(dynamicPageContent)
				.Replace("@Model.FirstName", person.FirstName)
				.Replace("@Model.LastName", person.LastName);

			Console.WriteLine("Template: " + html);
			Assert.That(html, Is.EqualTo(expectedHtml));
		}
	}
}