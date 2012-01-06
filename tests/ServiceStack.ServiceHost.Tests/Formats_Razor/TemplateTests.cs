using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.Markdown;
using ServiceStack.RazorEngine;
using ServiceStack.ServiceHost.Tests.Formats;

namespace ServiceStack.ServiceHost.Tests.Formats_Razor
{
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

	public class CustomMarkdownViewBase<T> : RazorPageBase<T>
	{
		public CustomMarkdownHelper Ext = new CustomMarkdownHelper();
		public ExternalProductHelper Prod = new ExternalProductHelper();

		public MvcHtmlString Table(dynamic obj)
		{
			Person model = obj;
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

			return MvcHtmlString.Create(sb.ToString());
		}

		private static string[] MenuItems = new[] { "About Us", "Blog", "Links", "Contact" };

		public MvcHtmlString Menu(string selectedId)
		{
			var sb = new StringBuilder();
			sb.Append("<ul>\n");
			foreach (var menuItem in MenuItems)
			{
				var cls = menuItem == selectedId ? " class='selected'" : "";
				sb.AppendFormat("<li><a href='{0}'{1}>{0}</a></li>\n", menuItem, cls);
			}
			sb.Append("</ul>\n");
			
			return MvcHtmlString.Create(sb.ToString());
		}

		public string Lower(string name)
		{
			return name == null ? null : name.ToLower();
		}

		public string Upper(string name)
		{
			return name == null ? null : name.ToUpper();
		}

		public string Combine(string separator, params string[] parts)
		{
			return string.Join(separator, parts);
		}
	}

	public class CustomMarkdownHelper
	{
		public static CustomMarkdownHelper Instance = new CustomMarkdownHelper();

		public MvcHtmlString InlineBlock(string content, string id)
		{
			return MvcHtmlString.Create(
				"<div id=\"" + id + "\"><div class=\"inner inline-block\">" + content + "</div></div>");
		}
	}

	[TestFixture]
	public class RazorTemplateTests
	{
		string staticTemplatePath;
		string staticTemplateContent;
		string dynamicPagePath;
		string dynamicPageContent;
		string dynamicListPagePath;
		string dynamicListPageContent;

		private RazorFormat razorFormat;
		Person templateArgs;

		Person person = new Person {
			FirstName = "Demis",
			LastName = "Bellot",
			Links = new List<Link>
				{
					new Link { Name = "ServiceStack", Href = "http://www.servicestack.net", Labels = {"REST","JSON","XML"} },
					new Link { Name = "AjaxStack", Href = "http://www.ajaxstack.com", Labels = {"HTML5", "AJAX", "SPA"} },
				},
		};

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			staticTemplatePath = "~/Views/Template/_Layout.cshtml".MapProjectPath();
			staticTemplateContent = File.ReadAllText(staticTemplatePath);

			dynamicPagePath = "~/Views/Template/DynamicTpl.cshtml".MapProjectPath();
			dynamicPageContent = File.ReadAllText(dynamicPagePath);

			dynamicListPagePath = "~/Views/Template/DynamicListTpl.cshtml".MapProjectPath();
			dynamicListPageContent = File.ReadAllText(dynamicListPagePath);

			templateArgs = person;
		}

		[SetUp]
		public void OnBeforeEachTest()
		{
			razorFormat = new RazorFormat();
			razorFormat.Init();
		}

		private RazorPage AddViewPage(string pageName, string pagePath, string pageContents, string templatePath = null)
		{
			var dynamicPage = new RazorPage(razorFormat,
				pagePath, pageName, pageContents, RazorPageType.ViewPage) {
					TemplatePath = templatePath
				};

			razorFormat.AddPage(dynamicPage);
			return dynamicPage;
		}

		[Test]
		public void Can_Render_RazorTemplate()
		{
			const string mockContents = "[Replaced with Template]";

			razorFormat.AddTemplate(staticTemplatePath, staticTemplateContent);
			var page = AddViewPage("MockPage", "/path/to/page", mockContents, staticTemplatePath);

			var expectedHtml = staticTemplateContent.ReplaceFirst(RazorFormat.TemplatePlaceHolder, mockContents);

			var templateOutput = page.RenderToString(templateArgs);

			Console.WriteLine(templateOutput);

			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}

		[Test]
		public void Can_Render_RazorPage()
		{
			var dynamicPage = AddViewPage("DynamicTpl", dynamicPagePath, dynamicPageContent, staticTemplatePath);

			var expectedHtml = dynamicPageContent
				.Replace("@Model.FirstName", person.FirstName)
				.Replace("@Model.LastName", person.LastName);

			expectedHtml = staticTemplateContent.Replace(RazorFormat.TemplatePlaceHolder, expectedHtml);

			var templateOutput = dynamicPage.RenderToHtml(templateArgs);

			Console.WriteLine(templateOutput);

			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}

		[Test]
		public void Can_Render_RazorPage_with_foreach()
		{
			var dynamicPage = AddViewPage("DynamicListTpl", dynamicListPagePath, dynamicListPageContent, staticTemplatePath);

			var expectedHtml = dynamicListPageContent
				.Replace("@Model.FirstName", person.FirstName)
				.Replace("@Model.LastName", person.LastName);

			var foreachLinks = "  <li>ServiceStack - http://www.servicestack.net</li>\r\n"
							 + "  <li>AjaxStack - http://www.ajaxstack.com</li>";

			expectedHtml = expectedHtml.ReplaceForeach(foreachLinks);

			expectedHtml = staticTemplateContent.Replace(RazorFormat.TemplatePlaceHolder, expectedHtml);

			var templateOutput = dynamicPage.RenderToHtml(templateArgs);

			Console.WriteLine(templateOutput);

			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}

		[Test]
		public void Can_Render_RazorPage_with_IF_statement()
		{
			var template = @"<h1>Dynamic If Markdown Template</h1>

<p>Hello @Model.FirstName,</p>

<ul>
@if (Model.FirstName == ""Bellot"") {
	<li>@Model.FirstName</li>
}
@if (Model.LastName == ""Bellot"") {
	<li>@Model.LastName</li>
}
</ul>

<h3>heading 3</h3>";

			var expectedHtml = @"<h1>Dynamic If Markdown Template</h1>

<p>Hello Demis,</p>

<ul>
	<li>Bellot</li>
</ul>

<h3>heading 3</h3>";

			var dynamicPage = AddViewPage("DynamicIfTpl", "/path/to/tpl", template);

			var templateOutput = dynamicPage.RenderToHtml(templateArgs);

			Console.WriteLine(templateOutput);
			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}

		[Test]
		public void Can_Render_RazorPage_with_Nested_Statements()
		{
			var template = @"<h1>@Model.FirstName Dynamic Nested Markdown Template</h1>

<h1>heading 1</h1>

<ul>
@foreach (var link in Model.Links) {
	@if (link.Name == ""AjaxStack"") {
	<li>@link.Name - @link.Href</li>
	}
}
</ul>

@if (Model.Links.Count == 2) {
<h2>Haz 2 links</h2>
<ul>
	@foreach (var link in Model.Links) {
		<li>@link.Name - @link.Href</li>
		@foreach (var label in link.Labels) { 
			<li>@label</li>
		}
	}
</ul>
}

<h3>heading 3</h3>";

			var expectedHtml = @"<h1>Demis Dynamic Nested Markdown Template</h1>

<h1>heading 1</h1>

<ul>
	<li>AjaxStack - http://www.ajaxstack.com</li>
</ul>

<h2>Haz 2 links</h2>
<ul>
		<li>ServiceStack - http://www.servicestack.net</li>
			<li>REST</li>
			<li>JSON</li>
			<li>XML</li>
		<li>AjaxStack - http://www.ajaxstack.com</li>
			<li>HTML5</li>
			<li>AJAX</li>
			<li>SPA</li>
</ul>

<h3>heading 3</h3>";


			var dynamicPage = AddViewPage("DynamicNestedTpl", "/path/to/tpl", template);

			var templateOutput = dynamicPage.RenderToHtml(templateArgs);

			Console.WriteLine(templateOutput);
			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}

		[Test]
		public void Can_Render_Razor_with_StaticMethods()
		{
			var headerTemplate = @"<h2>Header Links!</h2>
<ul>
	<li><a href=""http://google.com"">Google</a></li>
	<li><a href=""http://bing.com"">Bing</a></li>
</ul>".NormalizeNewLines();

			var template = @"<h2>Welcome to Razor!</h2>

@Html.Partial(""HeaderLinks"", Model)

<p>Hello @Upper(Model.LastName), @Model.FirstName</p>

<h3>Breadcrumbs</h3>

@Combine("" / "", Model.FirstName, Model.LastName)

<h3>Menus</h3>
<ul>
@foreach (var link in Model.Links) {
	<li>@link.Name - @link.Href
		<ul>
		@foreach (var label in link.Labels) { 
			<li>@label</li>
		}
		</ul>
	</li>
}
</ul>

<h3>HTML Table</h3>
@Table(Model)".NormalizeNewLines();

			var expectedHtml = @"<h2>Welcome to Razor!</h2>

<h2>Header Links!</h2>
<ul>
	<li><a href=""http://google.com"">Google</a></li>
	<li><a href=""http://bing.com"">Bing</a></li>
</ul>

<p>Hello BELLOT, Demis</p>

<h3>Breadcrumbs</h3>

Demis / Bellot

<h3>Menus</h3>
<ul>
	<li>ServiceStack - http://www.servicestack.net
		<ul>
			<li>REST</li>
			<li>JSON</li>
			<li>XML</li>
		</ul>
	</li>
	<li>AjaxStack - http://www.ajaxstack.com
		<ul>
			<li>HTML5</li>
			<li>AJAX</li>
			<li>SPA</li>
		</ul>
	</li>
</ul>

<h3>HTML Table</h3>
<table><caption>Demis's Links</caption><thead><tr><th>Name</th><th>Link</th></tr></thead>
<tbody>
<tr><td>ServiceStack</td><td>http://www.servicestack.net</td></tr><tr><td>AjaxStack</td><td>http://www.ajaxstack.com</td></tr></tbody>
</table>
".NormalizeNewLines();

			Razor.SetTemplateBase(typeof(CustomMarkdownViewBase<>));

			AddViewPage("HeaderLinks", "/path/to/page", headerTemplate);

			var dynamicPage = AddViewPage("DynamicIfTpl", "/path/to/tpl", template);

			var templateOutput = dynamicPage.RenderToHtml(templateArgs).NormalizeNewLines();

			Console.WriteLine(templateOutput);
			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}

		[Test]
		public void Can_inherit_from_Generic_RazorViewPage_from_model_directive()
		{
			var template = @"@model ServiceStack.ServiceHost.Tests.Formats_Razor.Person
<h1>Generic View Page</h1>

<h2>Form fields</h2>
@Html.LabelFor(m => m.FirstName) @Html.TextBoxFor(m => m.FirstName)
";

			var expectedHtml = @"<h1>Generic View Page</h1>

<h2>Form fields</h2>
<label for=""FirstName"">FirstName</label> <input name=""FirstName"" type=""text"" value=""Demis"" />
".NormalizeNewLines();


			var dynamicPage = AddViewPage("DynamicModelTpl", "/path/to/tpl", template);

			var templateOutput = dynamicPage.RenderToHtml(templateArgs).NormalizeNewLines();

			Console.WriteLine(templateOutput);
			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}

		[Test]
		public void Can_inherit_from_CustomViewPage_using_inherits_directive()
		{
			var template = @"@inherits ServiceStack.ServiceHost.Tests.Formats_Razor.CustomMarkdownViewBase<ServiceStack.ServiceHost.Tests.Formats_Razor.Person>
<h1>Generic View Page</h1>

<h2>Form fields</h2>
@Html.LabelFor(m => m.FirstName) @Html.TextBoxFor(m => m.FirstName)

<h2>Person Table</h2>
@Table(Model)";

			var expectedHtml = @"<h1>Generic View Page</h1>

<h2>Form fields</h2>
<label for=""FirstName"">FirstName</label> <input name=""FirstName"" type=""text"" value=""Demis"" />

<h2>Person Table</h2>
<table><caption>Demis's Links</caption><thead><tr><th>Name</th><th>Link</th></tr></thead>
<tbody>
<tr><td>ServiceStack</td><td>http://www.servicestack.net</td></tr><tr><td>AjaxStack</td><td>http://www.ajaxstack.com</td></tr></tbody>
</table>
".NormalizeNewLines();

			Razor.SetTemplateBase(typeof(CustomMarkdownViewBase<>));

			var dynamicPage = AddViewPage("DynamicModelTpl", "/path/to/tpl", template);

			var templateOutput = dynamicPage.RenderToHtml(templateArgs).NormalizeNewLines();

			Console.WriteLine(templateOutput);
			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}


		[Test]
		public void Can_Render_RazorPage_with_external_helper()
		{
			var template = @"<h1>View Page with Custom Helper</h1>

<h2>External Helper</h2>
<img src='path/to/img' class='inline-block' />
@Ext.InlineBlock(Model.FirstName, ""first-name"")
";

			var expectedHtml =
			@"<h1>View Page with Custom Helper</h1>

<h2>External Helper</h2>
<img src='path/to/img' class='inline-block' />
<div id=""first-name""><div class=""inner inline-block"">Demis</div></div>
".NormalizeNewLines();


			Razor.SetTemplateBase(typeof(CustomMarkdownViewBase<>));

			var dynamicPage = AddViewPage("DynamicModelTpl", "/path/to/tpl", template);

			var templateOutput = dynamicPage.RenderToHtml(templateArgs).NormalizeNewLines();

			Console.WriteLine(templateOutput);
			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}

		[Test]
		public void Can_Render_RazorPage_with_variable_statements()
		{
			var template = @"<h2>Welcome to Razor!</h2>

@{ var lastName = Model.LastName; }
Hello @Upper(lastName), @Model.FirstName

<h3>Breadcrumbs</h3>
@Combine("" / "", Model.FirstName, lastName)

@{ var links = Model.Links; }
<h3>Menus</h3>
<ul>
@foreach (var link in links) {
	<li>@link.Name - @link.Href
		<ul>
		@{ var labels = link.Labels; }
		@foreach (var label in labels) { 
			<li>@label</li>
		}
		</ul>
	</li>
}
</ul>";

			var expectedHtml = @"<h2>Welcome to Razor!</h2>

Hello BELLOT, Demis

<h3>Breadcrumbs</h3>
Demis / Bellot

<h3>Menus</h3>
<ul>
	<li>ServiceStack - http://www.servicestack.net
		<ul>
			<li>REST</li>
			<li>JSON</li>
			<li>XML</li>
		</ul>
	</li>
	<li>AjaxStack - http://www.ajaxstack.com
		<ul>
			<li>HTML5</li>
			<li>AJAX</li>
			<li>SPA</li>
		</ul>
	</li>
</ul>".NormalizeNewLines();

			Razor.SetTemplateBase(typeof(CustomMarkdownViewBase<>));

			var dynamicPage = AddViewPage("DynamicModelTpl", "/path/to/tpl", template);

			var templateOutput = dynamicPage.RenderToHtml(templateArgs).NormalizeNewLines();

			Console.WriteLine(templateOutput);
			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}


		[Test]
		public void Can_Render_RazorPage_with_comments()
		{
			var template = @"<h1>Dynamic If Markdown Template</h1>

<p>Hello @Model.FirstName,</p>

@if (Model.FirstName == ""Bellot"") {
<ul>
	<li>@Model.FirstName</li>
</ul>
}
@*
@if (Model.LastName == ""Bellot"") {
	* @Model.LastName
}
*@

@*
Plain text in a comment
*@
<h3>heading 3</h3>";

			var expectedHtml = @"<h1>Dynamic If Markdown Template</h1>

<p>Hello Demis,</p>

<h3>heading 3</h3>".NormalizeNewLines();

			var dynamicPage = AddViewPage("DynamicIfTpl", "/path/to/tpl", template);

			var templateOutput = dynamicPage.RenderToHtml(templateArgs).NormalizeNewLines();

			Console.WriteLine(templateOutput);
			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}


		[Test]
		public void Can_capture_Section_statements_and_store_them_in_Sections()
		{
			var template = @"<h2>Welcome to Razor!</h2>

@{ var lastName = Model.LastName; }
@section Salutations {
<p>Hello @Upper(lastName), @Model.FirstName</p>
}

@section Breadcrumbs {
<h3>Breadcrumbs</h3>
<p>@Combine("" / "", Model.FirstName, lastName)</p>
}

@{ var links = Model.Links; }
@section Menus {
<h3>Menus</h3>
<ul>
@foreach (var link in links) {
	<li>@link.Name - @link.Href
		<ul>
		@{ var labels = link.Labels; }
		@foreach (var label in labels) { 
			<li>@label</li>
		}
		</ul>
	</li>
}
</ul>
}

<h2>Captured Sections</h2>

<div id='breadcrumbs'>
@RenderSection(""Breadcrumbs"")
</div>

@RenderSection(""Menus"")

<h2>Salutations</h2>
@RenderSection(""Salutations"")";

			var expectedHtml =
			@"<h2>Welcome to Razor!</h2>







<h2>Captured Sections</h2>

<div id='breadcrumbs'>

<h3>Breadcrumbs</h3>
<p>Demis / Bellot</p>

</div>


<h3>Menus</h3>
<ul>
	<li>ServiceStack - http://www.servicestack.net
		<ul>
			<li>REST</li>
			<li>JSON</li>
			<li>XML</li>
		</ul>
	</li>
	<li>AjaxStack - http://www.ajaxstack.com
		<ul>
			<li>HTML5</li>
			<li>AJAX</li>
			<li>SPA</li>
		</ul>
	</li>
</ul>


<h2>Salutations</h2>

<p>Hello BELLOT, Demis</p>
".NormalizeNewLines();

			Razor.SetTemplateBase(typeof(CustomMarkdownViewBase<>));
			
			var dynamicPage = AddViewPage("DynamicModelTpl", "/path/to/tpl", template);

			var templateOutput = dynamicPage.RenderToHtml(templateArgs).NormalizeNewLines();

			Console.WriteLine(templateOutput);

			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
			
			var razorTemplate = dynamicPage.GetRazorTemplate();

			razorTemplate.Clear();
			Action section;
			razorTemplate.Sections.TryGetValue("Salutations", out section);
			section();

			Assert.That(razorTemplate.Result, Is.EqualTo("\r\n<p>Hello BELLOT, Demis</p>\r\n"));
		}


		[Test]
		public void Can_Render_RazorTemplate_with_section_and_variable_placeholders()
		{
			var template = @"<h2>Welcome to Razor!</h2>

@{ var lastName = Model.LastName; }

<p>Hello @Upper(lastName), @Model.FirstName,</p>

@section Breadcrumbs {
<h3>Breadcrumbs</h3>
@Combine("" / "", Model.FirstName, lastName)
}

@section Menus {
<h3>Menus</h3>
<ul>
@foreach (var link in Model.Links) {
	<li>@link.Name - @link.Href
		<ul>
		@{ var labels = link.Labels; }
		@foreach (var label in labels) { 
			<li>@label</li>
		}
		</ul>
	</li>
}
</ul>
}";
			var websiteTemplatePath = "/path/to/tpl";
			
			var websiteTemplate = @"<!doctype html>
<html lang=""en-us"">
<head>
	<title>Bellot page</title>
</head>
<body>

	<header>
		@RenderSection(""Menus"")
	</header>

	<h1>Website Template</h1>

	<div id=""content"">@RenderBody()</div>

	<footer>
		@RenderSection(""Breadcrumbs"")
	</footer>

</body>
</html>";

			var expectedHtml =
			@"<!doctype html>
<html lang=""en-us"">
<head>
	<title>Bellot page</title>
</head>
<body>

	<header>
		
<h3>Menus</h3>
<ul>
	<li>ServiceStack - http://www.servicestack.net
		<ul>
			<li>REST</li>
			<li>JSON</li>
			<li>XML</li>
		</ul>
	</li>
	<li>AjaxStack - http://www.ajaxstack.com
		<ul>
			<li>HTML5</li>
			<li>AJAX</li>
			<li>SPA</li>
		</ul>
	</li>
</ul>

	</header>

	<h1>Website Template</h1>

	<div id=""content""><h2>Welcome to Razor!</h2>


<p>Hello BELLOT, Demis,</p>



</div>

	<footer>
		
<h3>Breadcrumbs</h3>
Demis / Bellot

	</footer>

</body>
</html>".NormalizeNewLines();

			Razor.SetTemplateBase(typeof(CustomMarkdownViewBase<>));

			razorFormat.AddTemplate(websiteTemplatePath, websiteTemplate);
			AddViewPage("DynamicModelTpl", "/path/to/page-tpl", template, websiteTemplatePath);

			var razorTemplate = razorFormat.ExecuteTemplate(
				person, "DynamicModelTpl", websiteTemplatePath);

			var templateOutput = razorTemplate.Result.NormalizeNewLines();

			Console.WriteLine(templateOutput);

			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}

		[Test]
		public void Can_Render_Static_RazorContentPage_that_populates_variable_and_displayed_on_website_template()
		{

			var websiteTemplate = @"<!doctype html>
<html lang=""en-us"">
<head>
	<title>Static page</title>
</head>
<body>
	<header>
		@RenderSection(""Header"")
	</header>

	<div id='menus'>
		@RenderSection(""Menu"")
	</div>

	<h1>Website Template</h1>

	<div id=""content"">@RenderBody()</div>

</body>
</html>".NormalizeNewLines();

	var template = @"<h1>Static Markdown Template</h1>
@section Menu {
  @Menu(""Links"")
}

@section Header {
<h3>Static Page Title</h3>
}

<h3>heading 3</h3>
<p>paragraph</p>";

	var expectedHtml = @"<!doctype html>
<html lang=""en-us"">
<head>
	<title>Static page</title>
</head>
<body>
	<header>
		
<h3>Static Page Title</h3>

	</header>

	<div id='menus'>
		
  <ul>
<li><a href='About Us'>About Us</a></li>
<li><a href='Blog'>Blog</a></li>
<li><a href='Links' class='selected'>Links</a></li>
<li><a href='Contact'>Contact</a></li>
</ul>


	</div>

	<h1>Website Template</h1>

	<div id=""content""><h1>Static Markdown Template</h1>




<h3>heading 3</h3>
<p>paragraph</p></div>

</body>
</html>".NormalizeNewLines();

			Razor.SetTemplateBase(typeof(CustomMarkdownViewBase<>));

			var websiteTemplatePath = "/path/to/tpl";
			razorFormat.AddTemplate(websiteTemplatePath, websiteTemplate);

			var staticPage = new RazorPage(razorFormat,
				"/path/to/pagetpl", "StaticTpl", template, RazorPageType.ContentPage) {
					TemplatePath = websiteTemplatePath
				};

			razorFormat.AddPage(staticPage);

			var templateOutput = razorFormat.RenderStaticPage("/path/to/pagetpl").NormalizeNewLines();

			Console.WriteLine(templateOutput);
			Assert.That(templateOutput, Is.EqualTo(expectedHtml));
		}

	}
}