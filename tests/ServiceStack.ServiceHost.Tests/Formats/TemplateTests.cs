using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.Markdown;
using ServiceStack.ServiceModel.Serialization;
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
		
		Dictionary<string, object> templateArgs;
		
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			staticTemplatePath = "~/AppData/Template/default.htm".MapAbsolutePath();
			staticTemplateContent = File.ReadAllText(staticTemplatePath);

			dynamicPagePath = "~/AppData/Template/DynamicTpl.md".MapAbsolutePath();
			dynamicPageContent = File.ReadAllText(dynamicPagePath);

			dynamicListPagePath = "~/AppData/Template/DynamicListTpl.md".MapAbsolutePath();
			dynamicListPageContent = File.ReadAllText(dynamicListPagePath);

			templateArgs = new Dictionary<string, object> { { MarkdownPage.ModelName, person } };
		}

		[Test]
		public void Can_Render_MarkdownTemplate()
		{
			var template = new MarkdownTemplate(staticTemplatePath, "default", staticTemplateContent);
			template.Prepare();

			Assert.That(template.Blocks.Count, Is.EqualTo(3));

			const string mockResponse = "[Replaced with Template]";
			var expectedHtml = staticTemplateContent.ReplaceFirst(MarkdownFormat.TemplatePlaceHolder, mockResponse);

			var mockArgs = new Dictionary<string, object> { { MarkdownPage.ModelName, mockResponse } };
			var templateOutput = template.RenderToString(mockArgs);

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

		Person person = new Person {
			FirstName = "Demis",
			LastName = "Bellot",
			Links = new List<Link>
				{
					new Link { Name = "ServiceStack", Href = "http://www.servicestack.net", Labels = {"REST","JSON","XML"} },
					new Link { Name = "AjaxStack", Href = "http://www.ajaxstack.com", Labels = {"HTML5", "AJAX", "SPA"} },
				},
		};


		[Test]
		public void Can_Render_MarkdownPage()
		{
			var dynamicPage = new MarkdownPage(dynamicPageContent, "DynamicTpl", dynamicPageContent);
			dynamicPage.Prepare();

			Assert.That(dynamicPage.Blocks.Count, Is.EqualTo(9));

			var expectedHtml = MarkdownFormat.Instance.Transform(dynamicPageContent)
				.Replace("@Model.FirstName", person.FirstName)
				.Replace("@Model.LastName", person.LastName);

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
				.Replace("@Model.FirstName", person.FirstName)
				.Replace("@Model.LastName", person.LastName);

			var foreachLinks = "  - ServiceStack - http://www.servicestack.net\r\n"
							 + "  - AjaxStack - http://www.ajaxstack.com\r\n";

			expectedMarkdown = expectedMarkdown.ReplaceForeach(foreachLinks);

			var expectedHtml = MarkdownFormat.Instance.Transform(expectedMarkdown);

			Console.WriteLine("ExpectedHtml: " + expectedHtml);

			var templateOutput = dynamicPage.RenderToString(templateArgs);

			Console.WriteLine("Template Output: " + templateOutput);

			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}

		[Test]
		public void Can_Render_MarkdownPage_with_IF_statement()
		{
			var template = @"# Dynamic If Markdown Template

Hello @Model.FirstName,

@if (Model.FirstName == ""Bellot"") {
  * @Model.FirstName
}
@if (Model.LastName == ""Bellot"") {
  * @Model.LastName
}

### heading 3";

			var expected = @"# Dynamic If Markdown Template

Hello Demis,

  * Bellot

### heading 3";

			var expectedHtml = MarkdownFormat.Instance.Transform(expected);

			var dynamicPage = new MarkdownPage("/path/to/tpl", "DynamicIfTpl", template);
			dynamicPage.Prepare();

			var templateOutput = dynamicPage.RenderToString(templateArgs);

			Console.WriteLine(templateOutput);
			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}

		[Test]
		public void Can_Render_Markdown_with_Nested_Statements()
		{
			var template = @"# @Model.FirstName Dynamic Nested Markdown Template

# heading 1

@foreach (var link in Model.Links) {
  @if (link.Name == ""AjaxStack"") {
  - @link.Name - @link.Href
  }
}

@if (Model.Links.Count == 2) {
## Haz 2 links

  @foreach (var link in Model.Links) {
  - @link.Name - @link.Href
    @foreach (var label in link.Labels) { 
    - @label
	}
  }
}

### heading 3";

			var expected = @"# Demis Dynamic Nested Markdown Template

# heading 1

  - AjaxStack - http://www.ajaxstack.com

## Haz 2 links

  - ServiceStack - http://www.servicestack.net
    - REST
    - JSON
    - XML
  - AjaxStack - http://www.ajaxstack.com
    - HTML5
    - AJAX
    - SPA

### heading 3";

			var expectedHtml = MarkdownFormat.Instance.Transform(expected);

			var dynamicPage = new MarkdownPage("/path/to/tpl", "DynamicNestedTpl", template);
			dynamicPage.Prepare();

			var templateOutput = dynamicPage.RenderToString(templateArgs);

			Console.WriteLine(templateOutput);
			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}

		public class CustomMarkdownViewBase : MarkdownViewBase
		{
			public string Table(Person model)
			{
				var sb = new StringBuilder();

				sb.AppendFormat("<table><caption>{0}'s Links</caption>", model.FirstName);
				sb.AppendLine("<thead><tr><th>Name</th><th>Link</th></tr></thead>");
				sb.AppendLine("<tbody>");
				foreach (var link in model.Links)
				{
					sb.AppendFormat("<tr><td>{0}</td><td>{1}</td></tr>", link.Name, link.Href);
				}
				sb.AppendLine("</tbody>");
				sb.AppendLine("</table>");

				return sb.ToString();
			}
		}

		public class CustomMarkdownHelper
		{
			public CustomMarkdownHelper Instance = new CustomMarkdownHelper();

			public string Wrap(string content, string id)
			{
				return "<div id=\"" + id + "\">" + content + "</div>";
			}
		}

		[Test]
		public void Can_Render_Markdown_with_StaticMethods()
		{
			var headerTemplate = @"## Header Links!
  - [Google](http://google.com)
  - [Bing](http://bing.com)";

			var template = @"## Welcome to Razor!

@Html.Partial(""HeaderLinks"", Model)

Hello @Upper(Model.LastName), @Model.FirstName

### Breadcrumbs
@Combine("" / "", Model.FirstName, Model.LastName)

### Menus
@foreach (var link in Model.Links) {
  - @link.Name - @link.Href
  @foreach (var label in link.Labels) { 
    - @label
  }
}

### HTML Table 
#### Encoded	
@Table(Model)

#### Raw
@Html.Raw(Table(Model))
";

			var expectedHtml = @"<h2>Welcome to Razor!</h2>

<h2>Header Links!</h2>

<ul>
<li><a href=""http://google.com"">Google</a></li>
<li><a href=""http://bing.com"">Bing</a></li>
</ul>

<p>Hello  BELLOT, Demis</p>

<h3>Breadcrumbs</h3>

Demis / Bellot
<h3>Menus</h3>

<ul>
<li>ServiceStack - http://www.servicestack.net
<ul>
<li>REST</li>
<li>JSON</li>
<li>XML</li>
</ul></li>
<li>AjaxStack - http://www.ajaxstack.com
<ul>
<li>HTML5</li>
<li>AJAX</li>
<li>SPA</li>
</ul></li>
</ul>

<h3>HTML Table</h3>

<h4>Encoded</h4>

&lt;table&gt;&lt;caption&gt;Demis's Links&lt;/caption&gt;&lt;thead&gt;&lt;tr&gt;&lt;th&gt;Name&lt;/th&gt;&lt;th&gt;Link&lt;/th&gt;&lt;/tr&gt;&lt;/thead&gt;
&lt;tbody&gt;
&lt;tr&gt;&lt;td&gt;ServiceStack&lt;/td&gt;&lt;td&gt;http://www.servicestack.net&lt;/td&gt;&lt;/tr&gt;&lt;tr&gt;&lt;td&gt;AjaxStack&lt;/td&gt;&lt;td&gt;http://www.ajaxstack.com&lt;/td&gt;&lt;/tr&gt;&lt;/tbody&gt;
&lt;/table&gt;

<h4>Raw</h4>

<table><caption>Demis's Links</caption><thead><tr><th>Name</th><th>Link</th></tr></thead>
<tbody>
<tr><td>ServiceStack</td><td>http://www.servicestack.net</td></tr><tr><td>AjaxStack</td><td>http://www.ajaxstack.com</td></tr></tbody>
</table>
".Replace("\r\n", "\n");


			MarkdownFormat.Instance.MarkdownBaseType = typeof(CustomMarkdownViewBase);
			MarkdownFormat.Instance.MarkdownGlobalHelpers = new Dictionary<string, Type> 
				{
					{"Ext", typeof(CustomMarkdownHelper)}
				};

			MarkdownFormat.Instance.RegisterMarkdownPage(new MarkdownPage(
				"/path/to/page", "HeaderLinks", headerTemplate));

			var dynamicPage = new MarkdownPage("/path/to/tpl", "DynamicIfTpl", template);
			dynamicPage.Prepare();

			var templateOutput = dynamicPage.RenderToString(templateArgs);
			templateOutput = templateOutput.Replace("\r\n", "\n");

			Console.WriteLine(templateOutput);
			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}

		[Test]
		public void Can_inherit_from_Generic_view_page_from_model_directive()
		{
			var template = @"@model ServiceStack.ServiceHost.Tests.Formats.TemplateTests+Person
# Generic View Page

## TextBox
@Html.TextBoxFor(m => m.FirstName)
"; 

			var expectedHtml = @"
<h1>Generic View Page</h1>

<h2>TextBox</h2>

<input name=""FirstName"" type=""text"" value="""" />".Replace("\r\n", "\n");


			var dynamicPage = new MarkdownPage("/path/to/tpl", "DynamicModelTpl", template);
			dynamicPage.Prepare();

			var templateOutput = dynamicPage.RenderToString(templateArgs);
			templateOutput = templateOutput.Replace("\r\n", "\n");

			Assert.That(dynamicPage.ExecutionContext.BaseType, Is.EqualTo(typeof(MarkdownViewBase<>)));

			Console.WriteLine(templateOutput);
			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}

	}

}