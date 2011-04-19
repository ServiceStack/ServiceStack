using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
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

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			staticTemplatePath = "~/AppData/Template/default.htm".MapAbsolutePath();
			staticTemplateContent = File.ReadAllText(staticTemplatePath);

			dynamicPagePath = "~/AppData/Template/DynamicTpl.md".MapAbsolutePath();
			dynamicPageContent = File.ReadAllText(dynamicPagePath);
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

		[Test]
		public void Can_Render_MarkdownPage()
		{
			var dynamicPage = new MarkdownPage(dynamicPagePath, "DynamicTpl", dynamicPageContent);
			dynamicPage.Prepare();


		}
	}
}