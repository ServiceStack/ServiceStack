using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.Html;
using ServiceStack.Markdown;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;
using ServiceStack.VirtualPath;
using ServiceStack.WebHost.Endpoints.Formats;
using ServiceStack.WebHost.Endpoints.Support.Markdown;

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

        private MarkdownFormat markdownFormat;
        Dictionary<string, object> templateArgs;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            staticTemplatePath = "~/Views/Shared/_Layout.shtml".MapProjectPath();
            staticTemplateContent = File.ReadAllText(staticTemplatePath);

            dynamicPagePath = "~/Views/Template/DynamicTpl.md".MapProjectPath();
            dynamicPageContent = File.ReadAllText(dynamicPagePath);

            dynamicListPagePath = "~/Views/Template/DynamicListTpl.md".MapProjectPath();
            dynamicListPageContent = File.ReadAllText(dynamicListPagePath);
        }

        [SetUp]
        public void OnBeforeEachTest()
        {
            markdownFormat = new MarkdownFormat {
                VirtualPathProvider = new InMemoryVirtualPathProvider(new BasicAppHost())
            };
            templateArgs = new Dictionary<string, object> { { MarkdownPage.ModelName, person } };
        }

        [Test]
        public void Can_Render_MarkdownTemplate()
        {
            var template = new MarkdownTemplate(staticTemplatePath, "default", staticTemplateContent);
            template.Prepare();

            Assert.That(template.TextBlocks.Length, Is.EqualTo(2));

            const string mockResponse = "[Replaced with Template]";
            var expectedHtml = staticTemplateContent.ReplaceFirst(MarkdownFormat.TemplatePlaceHolder, mockResponse);

            var mockArgs = new Dictionary<string, object> { { MarkdownTemplate.BodyPlaceHolder, mockResponse } };
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
            var dynamicPage = new MarkdownPage(markdownFormat, dynamicPageContent, "DynamicTpl", dynamicPageContent);
            dynamicPage.Compile();

            Assert.That(dynamicPage.HtmlBlocks.Length, Is.EqualTo(9));

            var expectedHtml = markdownFormat.Transform(dynamicPageContent)
                .Replace("@Model.FirstName", person.FirstName)
                .Replace("@Model.LastName", person.LastName);

            var templateOutput = dynamicPage.RenderToHtml(templateArgs);

            Console.WriteLine("Template Output: " + templateOutput);

            Assert.That(templateOutput, Is.EqualTo(expectedHtml));
        }

        [Test]
        public void Can_Render_MarkdownPage_with_foreach()
        {
            var dynamicPage = new MarkdownPage(markdownFormat,
                dynamicListPagePath, "DynamicListTpl", dynamicListPageContent);
            dynamicPage.Compile();

            Assert.That(dynamicPage.HtmlBlocks.Length, Is.EqualTo(11));

            var expectedMarkdown = dynamicListPageContent
                .Replace("@Model.FirstName", person.FirstName)
                .Replace("@Model.LastName", person.LastName);

            var foreachLinks = "  - ServiceStack - http://www.servicestack.net\r\n"
                             + "  - AjaxStack - http://www.ajaxstack.com\r\n";

            expectedMarkdown = expectedMarkdown.ReplaceForeach(foreachLinks);

            var expectedHtml = markdownFormat.Transform(expectedMarkdown);

            Console.WriteLine("ExpectedHtml: " + expectedHtml);

            var templateOutput = dynamicPage.RenderToHtml(templateArgs);

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

            var expectedHtml = markdownFormat.Transform(expected);

            var dynamicPage = new MarkdownPage(markdownFormat, "/path/to/tpl", "DynamicIfTpl", template);
            dynamicPage.Compile();

            var templateOutput = dynamicPage.RenderToHtml(templateArgs);

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

### heading 3".NormalizeNewLines();

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

### heading 3".NormalizeNewLines();

            var expectedHtml = markdownFormat.Transform(expected);

            var dynamicPage = new MarkdownPage(markdownFormat, "/path/to/tpl", "DynamicNestedTpl", template);
            dynamicPage.Compile();

            var templateOutput = dynamicPage.RenderToHtml(templateArgs);

            Console.WriteLine(templateOutput);
            Assert.That(templateOutput, Is.EqualTo(expectedHtml));
        }

        public class CustomMarkdownViewBase<T> : MarkdownViewBase<T>
        {
            public MvcHtmlString Table(Person model)
            {
                return new CustomMarkdownViewBase().Table(model);
            }
        }

        public class CustomMarkdownViewBase : MarkdownViewBase
        {
            public MvcHtmlString Table(Person model)
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

                return MvcHtmlString.Create(sb.ToString());
            }

            private static string[] MenuItems = new[] { "About Us", "Blog", "Links", "Contact" };

            public void Menu(string selectedId)
            {
                var sb = new StringBuilder();
                sb.Append("<ul>\n");
                foreach (var menuItem in MenuItems)
                {
                    var cls = menuItem == selectedId ? " class='selected'" : "";
                    sb.AppendFormat("<li><a href='{0}'{1}>{0}</a></li>\n", menuItem, cls);
                }
                sb.Append("</ul>\n");
                ScopeArgs.Add("Menu", MvcHtmlString.Create(sb.ToString()));
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
@Table(Model)
".NormalizeNewLines();

            var expectedHtml = @"<h2>Welcome to Razor!</h2>
<h2>Header Links!</h2>
<ul>
<li><a href=""http://google.com"">Google</a></li>
<li><a href=""http://bing.com"">Bing</a></li>
</ul>
<p>Hello  BELLOT, Demis</p>
<h3>Breadcrumbs</h3>
Demis / Bellot<h3>Menus</h3>
<ul>
<li>
ServiceStack - http://www.servicestack.net
<ul>
<li>REST</li>
<li>JSON</li>
<li>XML</li>
</ul>
</li>
<li>
AjaxStack - http://www.ajaxstack.com
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


            markdownFormat.MarkdownBaseType = typeof(CustomMarkdownViewBase);
            markdownFormat.MarkdownGlobalHelpers = new Dictionary<string, Type> 
				{
					{"Ext", typeof(CustomMarkdownHelper)}
				};

            markdownFormat.RegisterMarkdownPage(new MarkdownPage(markdownFormat,
                "/path/to/page", "HeaderLinks", headerTemplate));

            var dynamicPage = new MarkdownPage(markdownFormat, "/path/to/tpl", "DynamicIfTpl", template);
            dynamicPage.Compile();

            var templateOutput = dynamicPage.RenderToHtml(templateArgs);
            templateOutput = templateOutput.Replace("\r\n", "\n");

            templateOutput.Print();
            Assert.That(templateOutput, Is.EqualTo(expectedHtml));
        }

        [Test]
        public void Can_inherit_from_Generic_view_page_from_model_directive()
        {
            var template = @"@model ServiceStack.ServiceHost.Tests.Formats.TemplateTests+Person
# Generic View Page

## Form fields
@Html.LabelFor(m => m.FirstName) @Html.TextBoxFor(m => m.FirstName)
";

            var expectedHtml = @"<h1>Generic View Page</h1>
<h2>Form fields</h2>
<label for=""FirstName"">FirstName</label> <input id=""FirstName"" name=""FirstName"" type=""text"" value=""Demis"" />".NormalizeNewLines();


            var dynamicPage = new MarkdownPage(markdownFormat, "/path/to/tpl", "DynamicModelTpl", template);
            dynamicPage.Compile();

            var templateOutput = dynamicPage.RenderToHtml(templateArgs);
            templateOutput = templateOutput.Replace("\r\n", "\n");

            Assert.That(dynamicPage.ExecutionContext.BaseType, Is.EqualTo(typeof(MarkdownViewBase<>)));

            Console.WriteLine(templateOutput);
            Assert.That(templateOutput, Is.EqualTo(expectedHtml));
        }

        [Test]
        public void Can_inherit_from_CustomViewPage_using_inherits_directive()
        {
            var template = @"@inherits ServiceStack.ServiceHost.Tests.Formats.TemplateTests+CustomMarkdownViewBase<ServiceStack.ServiceHost.Tests.Formats.TemplateTests+Person>
# Generic View Page

## Form fields
@Html.LabelFor(m => m.FirstName) @Html.TextBoxFor(m => m.FirstName)

## Person Table
@Table(Model)
";

            var expectedHtml = @"<h1>Generic View Page</h1>
<h2>Form fields</h2>
<label for=""FirstName"">FirstName</label> <input id=""FirstName"" name=""FirstName"" type=""text"" value=""Demis"" /><h2>Person Table</h2>
<table><caption>Demis's Links</caption><thead><tr><th>Name</th><th>Link</th></tr></thead>
<tbody>
<tr><td>ServiceStack</td><td>http://www.servicestack.net</td></tr><tr><td>AjaxStack</td><td>http://www.ajaxstack.com</td></tr></tbody>
</table>
".NormalizeNewLines();


            var dynamicPage = new MarkdownPage(markdownFormat, "/path/to/tpl", "DynamicModelTpl", template);
            dynamicPage.Compile();

            var templateOutput = dynamicPage.RenderToHtml(templateArgs);
            templateOutput = templateOutput.Replace("\r\n", "\n");

            Assert.That(dynamicPage.ExecutionContext.BaseType, Is.EqualTo(typeof(CustomMarkdownViewBase<>)));

            Console.WriteLine(templateOutput);
            Assert.That(templateOutput, Is.EqualTo(expectedHtml));
        }

        [Test]
        public void Can_Render_MarkdownPage_with_external_helper()
        {
            var template = @"# View Page with Custom Helper

## External Helper 
![inline-img](path/to/img)
@Ext.InlineBlock(Model.FirstName, ""first-name"")
";

            var expectedHtml = @"<h1>View Page with Custom Helper</h1>
<h2>External Helper</h2>
<p><img src=""path/to/img"" alt=""inline-img"" />
 <div id=""first-name""><div class=""inner inline-block"">Demis</div></div>".NormalizeNewLines();


            markdownFormat.MarkdownGlobalHelpers.Add("Ext", typeof(CustomMarkdownHelper));
            var dynamicPage = new MarkdownPage(markdownFormat, "/path/to/tpl", "DynamicModelTpl", template);
            dynamicPage.Compile();

            var templateOutput = dynamicPage.RenderToHtml(templateArgs);
            templateOutput = templateOutput.Replace("\r\n", "\n");

            Assert.That(dynamicPage.ExecutionContext.BaseType, Is.EqualTo(typeof(MarkdownViewBase)));

            Console.WriteLine(templateOutput);
            Assert.That(templateOutput, Is.EqualTo(expectedHtml));
        }

        [Test]
        public void Can_Render_MarkdownPage_with_external_helper_using_helper_directive()
        {
            var template = @"@helper Ext: ServiceStack.ServiceHost.Tests.Formats.TemplateTests+CustomMarkdownHelper
# View Page with Custom Helper

## External Helper 
![inline-img](path/to/img)
@Ext.InlineBlock(Model.FirstName, ""first-name"")
";

            var expectedHtml = @"<h1>View Page with Custom Helper</h1>
<h2>External Helper</h2>
<p><img src=""path/to/img"" alt=""inline-img"" />
 <div id=""first-name""><div class=""inner inline-block"">Demis</div></div>".NormalizeNewLines();


            var dynamicPage = new MarkdownPage(markdownFormat, "/path/to/tpl", "DynamicModelTpl", template);
            dynamicPage.Compile();

            var templateOutput = dynamicPage.RenderToHtml(templateArgs);
            templateOutput = templateOutput.Replace("\r\n", "\n");

            Assert.That(dynamicPage.ExecutionContext.BaseType, Is.EqualTo(typeof(MarkdownViewBase)));

            Console.WriteLine(templateOutput);
            Assert.That(templateOutput, Is.EqualTo(expectedHtml));
        }

        [Test]
        public void Can_Render_page_to_Markdown_only()
        {
            var headerTemplate = @"## Header Links!
  - [Google](http://google.com)
  - [Bing](http://bing.com)
";

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
}".NormalizeNewLines();
            var expectedHtml = @"## Welcome to Razor!

 ## Header Links!
  - [Google](http://google.com)
  - [Bing](http://bing.com)

Hello  BELLOT, Demis

### Breadcrumbs
 Demis / Bellot

### Menus
  - ServiceStack - http://www.servicestack.net
    - REST
    - JSON
    - XML
  - AjaxStack - http://www.ajaxstack.com
    - HTML5
    - AJAX
    - SPA
".NormalizeNewLines();

            markdownFormat.RegisterMarkdownPage(new MarkdownPage(markdownFormat,
                "/path/to/page", "HeaderLinks", headerTemplate));

            var dynamicPage = new MarkdownPage(markdownFormat, "/path/to/tpl", "DynamicModelTpl", template);
            dynamicPage.Compile();

            var templateOutput = dynamicPage.RenderToMarkdown(templateArgs);
            templateOutput = templateOutput.Replace("\r\n", "\n");

            Assert.That(dynamicPage.ExecutionContext.BaseType, Is.EqualTo(typeof(MarkdownViewBase)));

            Console.WriteLine(templateOutput);
            Assert.That(templateOutput, Is.EqualTo(expectedHtml));
        }


        [Test]
        public void Can_Render_Markdown_with_variable_statements()
        {
            var template = @"## Welcome to Razor!

@var lastName = Model.LastName;

Hello @Upper(lastName), @Model.FirstName


### Breadcrumbs
@Combine("" / "", Model.FirstName, lastName)

@var links = Model.Links

### Menus
@foreach (var link in links) {
  - @link.Name - @link.Href
  @var labels = link.Labels
  @foreach (var label in labels) { 
    - @label
  }
}".NormalizeNewLines();
            var expectedHtml = @"## Welcome to Razor!


Hello  BELLOT, Demis


### Breadcrumbs
 Demis / Bellot

### Menus
  - ServiceStack - http://www.servicestack.net
    - REST
    - JSON
    - XML
  - AjaxStack - http://www.ajaxstack.com
    - HTML5
    - AJAX
    - SPA
".NormalizeNewLines();

            var dynamicPage = new MarkdownPage(markdownFormat, "/path/to/tpl", "DynamicModelTpl", template);
            dynamicPage.Compile();

            var templateOutput = dynamicPage.RenderToMarkdown(templateArgs);
            templateOutput = templateOutput.Replace("\r\n", "\n");

            Assert.That(dynamicPage.ExecutionContext.BaseType, Is.EqualTo(typeof(MarkdownViewBase)));

            Console.WriteLine(templateOutput);
            Assert.That(templateOutput, Is.EqualTo(expectedHtml));
        }

        [Test]
        public void Can_Render_MarkdownPage_with_comments()
        {
            var template = @"# Dynamic If Markdown Template
Hello @Model.FirstName,

@if (Model.FirstName == ""Bellot"") {
  * @Model.FirstName
}
@*
@if (Model.LastName == ""Bellot"") {
  * @Model.LastName
}
*@

@*
Plain text in a comment
*@

### heading 3";

            var expectedHtml = @"<h1>Dynamic If Markdown Template</h1>
<p>Hello Demis,</p>
<h3>heading 3</h3>
".NormalizeNewLines();

            var dynamicPage = new MarkdownPage(markdownFormat, "/path/to/tpl", "DynamicIfTpl", template);
            dynamicPage.Compile();

            var templateOutput = dynamicPage.RenderToHtml(templateArgs);

            Console.WriteLine(templateOutput);
            Assert.That(templateOutput, Is.EqualTo(expectedHtml));
        }

        [Test]
        public void Can_Render_MarkdownPage_with_unmatching_escaped_braces()
        {
            var template = @"# Dynamic If Markdown Template 
Hello @Model.FirstName, { -- unmatched, leave unescaped outside statement

{ -- inside matching braces, outside statement -- }

@if (Model.LastName == ""Bellot"") {
  * @Model.LastName

{{ -- inside matching braces, escape inside statement -- }}

{{ -- unmatched

}

### heading 3";

            var expectedHtml = @"<h1>Dynamic If Markdown Template</h1>
<p>Hello Demis, { -- unmatched, leave unescaped outside statement</p>
<p>{ -- inside matching braces, outside statement -- }</p>
<ul>
<li>Bellot</li>
</ul>
<p>{ -- inside matching braces, escape inside statement -- }</p>
<p>{ -- unmatched</p>
<h3>heading 3</h3>
".NormalizeNewLines();

            var dynamicPage = new MarkdownPage(markdownFormat, "/path/to/tpl", "DynamicIfTpl", template);
            dynamicPage.Compile();

            var templateOutput = dynamicPage.RenderToHtml(templateArgs);

            Console.WriteLine(templateOutput);
            Assert.That(templateOutput, Is.EqualTo(expectedHtml));
        }

        [Test]
        public void Can_capture_Section_statements_and_store_them_in_scopeargs()
        {
            var template = @"## Welcome to Razor!

@var lastName = Model.LastName;
@section Salutations {
Hello @Upper(lastName), @Model.FirstName
}

@section Breadcrumbs {
### Breadcrumbs
@Combine("" / "", Model.FirstName, lastName)
}

@var links = Model.Links
@section Menus {
### Menus
@foreach (var link in links) {
  - @link.Name - @link.Href
  @var labels = link.Labels
  @foreach (var label in labels) { 
    - @label
  }
}
}

## Captured Sections

<div id='breadcrumbs'>@Breadcrumbs</div>

@Menus

## Salutations
@Salutations".NormalizeNewLines();

            var expectedHtml = @"<h2>Welcome to Razor!</h2>
<h2>Captured Sections</h2>
<div id='breadcrumbs'><h3>Breadcrumbs</h3>
<p>Demis / Bellot</p>
</div>
<p><h3>Menus</h3>
<ul>
<li>
ServiceStack - http://www.servicestack.net
<ul>
<li>REST</li>
<li>JSON</li>
<li>XML</li>
</ul>
</li>
<li>
AjaxStack - http://www.ajaxstack.com
<ul>
<li>HTML5</li>
<li>AJAX</li>
<li>SPA</li>
</ul>
</li>
</ul>
</p>
<h2>Salutations</h2>
<p><p>Hello  BELLOT, Demis</p>
</p>
".NormalizeNewLines();

            var dynamicPage = new MarkdownPage(markdownFormat, "/path/to/tpl", "DynamicModelTpl", template);
            dynamicPage.Compile();

            var templateOutput = dynamicPage.RenderToHtml(templateArgs);
            templateOutput = templateOutput.Replace("\r\n", "\n");

            Assert.That(dynamicPage.ExecutionContext.BaseType, Is.EqualTo(typeof(MarkdownViewBase)));

            Console.WriteLine(templateOutput);

            Assert.That(templateArgs["Salutations"].ToString(), Is.EqualTo("<p>Hello  BELLOT, Demis</p>\n"));
            Assert.That(templateOutput, Is.EqualTo(expectedHtml));
        }

        [Test]
        public void Can_Render_Template_with_section_and_variable_placeholders()
        {
            var template = @"## Welcome to Razor!

@var lastName = Model.LastName;

Hello @Upper(lastName), @Model.FirstName,

@section Breadcrumbs {
### Breadcrumbs
@Combine("" / "", Model.FirstName, lastName)
}

@section Menus {
### Menus
@foreach (var link in Model.Links) {
  - @link.Name - @link.Href
  @var labels = link.Labels
  @foreach (var label in labels) { 
    - @label
  }
}
}".NormalizeNewLines();

            var websiteTemplate = @"<!doctype html>
<html lang=""en-us"">
<head>
    <title><!--@lastName--> page</title>
</head>
<body>

    <header>
        <!--@Menus-->
    </header>

    <h1>Website Template</h1>

    <div id=""content""><!--@Body--></div>

    <footer>
        <!--@Breadcrumbs-->
    </footer>

</body>
</html>".NormalizeNewLines();

            var expectedHtml = @"<!doctype html>
<html lang=""en-us"">
<head>
    <title>Bellot page</title>
</head>
<body>

    <header>
        <h3>Menus</h3>
<ul>
<li>
ServiceStack - http://www.servicestack.net
<ul>
<li>REST</li>
<li>JSON</li>
<li>XML</li>
</ul>
</li>
<li>
AjaxStack - http://www.ajaxstack.com
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
<p>Hello  BELLOT, Demis,</p>
</div>

    <footer>
        <h3>Breadcrumbs</h3>
<p>Demis / Bellot</p>

    </footer>

</body>
</html>".NormalizeNewLines();

            markdownFormat.AddFileAndTemplate("websiteTemplate", websiteTemplate);

            markdownFormat.AddPage(
                new MarkdownPage(markdownFormat, "/path/to/page-tpl", "DynamicModelTpl", template) {
                    Template = "websiteTemplate"
                });

            var templateOutput = markdownFormat.RenderDynamicPageHtml("DynamicModelTpl", person);
            templateOutput = templateOutput.Replace("\r\n", "\n");

            Console.WriteLine(templateOutput);

            Assert.That(templateOutput, Is.EqualTo(expectedHtml));
        }

        [Test]
        public void Can_Render_Static_ContentPage_that_populates_variable_and_displayed_on_website_template()
        {

            var websiteTemplate = @"<!doctype html>
<html lang=""en-us"">
<head>
    <title>Static page</title>
</head>
<body>
    <header>
      <!--@Header-->
    </header>

    <div id='menus'>
        <!--@Menu-->
    </div>

    <h1>Website Template</h1>

    <div id=""content""><!--@Body--></div>

</body>
</html>".NormalizeNewLines();

            var template = @"# Static Markdown Template
@Menu(""Links"")

@section Header {
### Static Page Title  
}

### heading 3
paragraph";

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
<p>paragraph</p>
</div>

</body>
</html>".NormalizeNewLines();

            markdownFormat.MarkdownBaseType = typeof(CustomMarkdownViewBase);

            markdownFormat.AddFileAndTemplate("websiteTemplate", websiteTemplate);

            markdownFormat.RegisterMarkdownPage(
                new MarkdownPage(markdownFormat, "pagetpl", "StaticTpl", template, MarkdownPageType.ContentPage) {
                    Template = "websiteTemplate"
                });

            var templateOutput = markdownFormat.RenderStaticPage("pagetpl", true);

            Console.WriteLine(templateOutput);
            Assert.That(templateOutput, Is.EqualTo(expectedHtml));
        }

    }
}