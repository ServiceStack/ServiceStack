using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.Text;
using ServiceStack.WebHost.EndPoints.Formats;
using ServiceStack.WebHost.EndPoints.Support.Markdown;

namespace ServiceStack.ServiceHost.Tests.Formats
{
	[TestFixture]
	public class TemplateTests
	{
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

		[Test]
		public void Can_Render_MarkdownTemplate()
		{
			var template = new MarkdownTemplate(staticTemplatePath, "default", staticTemplateContent);
			template.Prepare();

			Assert.That(template.Blocks.Count, Is.EqualTo(3));

			const string mockResponse = "[Replaced with Template]";
			var expectedHtml = staticTemplateContent.ReplaceFirst(MarkdownFormat.TemplatePlaceHolder, mockResponse);

			var templateArgs = new Dictionary<string, object> { { "model", mockResponse } };
			var templateOutput = template.RenderToString(templateArgs);

			Console.WriteLine("Template Output: " + templateOutput);

			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}

		public class Person
		{
			public string FirstName { get; set; }
			public string LastName { get; set; }
			public List<Link> Links { get; set; }
		}

		public class Link
		{
			public Link()
			{
				this.Labels = new List<string>();
			}
			public string Name { get; set; }
			public string Href { get; set; }
			public List<string> Labels { get; set; }
		}

		Person person = new Person
		{
			FirstName = "Demis",
			LastName = "Bellot",
			Links = new List<Link>
				{
					new Link { Name = "ServiceStack", Href = "http://www.servicestack.net", Labels = {"REST","JSON","XML"} },
					new Link { Name = "AjaxStack", Href = "http://www.ajaxstack.net", Labels = {"HTML5", "AJAX", "SPA"} },
				},
		};


		[Test]
		public void Can_Render_MarkdownPage()
		{
			var person = new Person { FirstName = "Demis", LastName = "Bellot" };

			var dynamicPage = new MarkdownPage(dynamicPageContent, "DynamicTpl", dynamicPageContent);
			dynamicPage.Prepare();

			Assert.That(dynamicPage.Blocks.Count, Is.EqualTo(9));

			var expectedHtml = MarkdownFormat.Instance.Transform(dynamicPageContent)
				.Replace("@model.FirstName", person.FirstName)
				.Replace("@model.LastName", person.LastName);

			var templateArgs = new Dictionary<string, object> { { "model", person } };
			var templateOutput = dynamicPage.RenderToString(templateArgs);

			Console.WriteLine("Template Output: " + templateOutput);

			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}

		[Test]
		public void Can_Render_MarkdownPage_with_foreach()
		{
			var dynamicPage = new MarkdownPage(
				dynamicListPagePath, "DynamicListTpl", dynamicListPageContent);
			dynamicPage.Prepare();

			Assert.That(dynamicPage.Blocks.Count, Is.EqualTo(11));

			var expectedMarkdown = dynamicListPageContent
				.Replace("@model.FirstName", person.FirstName)
				.Replace("@model.LastName", person.LastName);

			var foreachLinks = "  - ServiceStack - http://www.servicestack.net\r\n"
							 + "  - AjaxStack - http://www.ajaxstack.net\r\n";

			expectedMarkdown = expectedMarkdown.ReplaceForeach(foreachLinks);

			var expectedHtml = MarkdownFormat.Instance.Transform(expectedMarkdown);

			Console.WriteLine("ExpectedHtml: " + expectedHtml);

			var templateArgs = new Dictionary<string, object> { { "model", person } };
			var templateOutput = dynamicPage.RenderToString(templateArgs);

			Console.WriteLine("Template Output: " + templateOutput);

			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}

		[Test]
		public void Can_Render_MarkdownPage_with_IF_statement()
		{
			var template = @"# Dynamic If Markdown Template

Hello @model.FirstName,

@if (model.FirstName == ""Bellot"") {
  * @model.FirstName
}
@if (model.LastName == ""Bellot"") {
  * @model.LastName
}

### heading 3";

			var expected = @"# Dynamic If Markdown Template

Hello Demis,

  * Bellot

### heading 3";

			var expectedHtml = MarkdownFormat.Instance.Transform(expected);

			var dynamicPage = new MarkdownPage("/path/to/tpl", "DynamicIfTpl", template);
			dynamicPage.Prepare();

			var templateArgs = new Dictionary<string, object> { { "model", person } };
			var templateOutput = dynamicPage.RenderToString(templateArgs);

			Console.WriteLine(templateOutput);
			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}

	}

}