using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using ServiceStack.Html;
using ServiceStack.Razor;
using ServiceStack.ServiceHost.Tests.Formats;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.ServiceHost.Tests.Formats_Razor
{
    public class ExternalProductHelper
    {
        //Any helpers returning MvcHtmlString won't be escaped
        public MvcHtmlString ProductTable(List<Product> products)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("<table><thead><tr><th>Id</th><th>Name</th><th>Price</th></tr></thead><tbody>\n");
            products.ForEach(x =>
                sb.AppendFormat("<tr><th>{0}</th><th>{1}</th><th>{2}</th></tr>\n",
                x.ProductID, x.Name, x.Price)
            );
            sb.AppendFormat("</tbody></table>\n");

            return MvcHtmlString.Create(sb.ToString());
        }
    }

    public class CustomBaseClass<T> : ViewPage<T>
    {
        public MvcHtmlString Field(string fieldName, string fieldValue)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("<label for='{0}'>{0}</label>\n", fieldName);
            sb.AppendFormat("<input name='{0}' value='{1}'/>\n", fieldName, fieldValue);

            return MvcHtmlString.Create(sb.ToString());
        }

        public override void Execute()
        {
        }
    }

    [TestFixture]
    public class IntroductionLayoutRazorTests : RazorTestBase
    {
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

        [SetUp]
        public void OnBeforeEachTest()
        {
            RazorFormat.Instance = null;
            base.RazorFormat = new RazorFormat
            {
                VirtualPathProvider = new InMemoryVirtualPathProvider(new BasicAppHost()),
            }.Init();
        }

        [Test]
        public void Simple_Layout_Example()
        {
            var websiteTemplate =
@"<!DOCTYPE html>
<html>
    <head>
        <title>Simple Site</title>
    </head>
    <body>
    
        <div id=""header"">
            <a href=""/"">Home</a>
            <a href=""/About"">About</a>
        </div>
        
        <div id=""body"">
            @RenderBody()
        </div>
    
    </body>
</html>".NormalizeNewLines();

            var pageTemplate =
@"@layout websiteTemplate

<h1>About this Site</h1>

<p>This is some content that will make up the ""about"" 
page of our web-site. We'll use this in conjunction
with a layout template. The content you are seeing here
comes from ^^^websiteTemplate.</p>

<p>And obviously I can have code in here too. Here is the
current date/year: @DateTime.Now.Year</p>
".NormalizeNewLines();

            var expectedHtml = @"<!DOCTYPE html>
<html>
    <head>
        <title>Simple Site</title>
    </head>
    <body>
    
        <div id=""header"">
            <a href=""/"">Home</a>
            <a href=""/About"">About</a>
        </div>
        
        <div id=""body"">
            <h1>About this Site</h1>

<p>This is some content that will make up the ""about"" 
page of our web-site. We'll use this in conjunction
with a layout template. The content you are seeing here
comes from ^^^websiteTemplate.</p>

<p>And obviously I can have code in here too. Here is the
current date/year: 2015</p>

        </div>
    
    </body>
</html>".NormalizeNewLines();


            RazorFormat.AddFileAndPage("/views/websiteTemplate.cshtml", websiteTemplate);
            var dynamicPage = RazorFormat.AddFileAndPage(@"/page.cshtml", pageTemplate);

            var template = RazorFormat.RenderToHtml(dynamicPage);

            template.Print();
            Assert.That(template, Is.EqualTo(expectedHtml));
        }

        [Test]
        public void Nested_Layout_Example_With_Sections()
        {
            var websiteTemplate2 =
                @"<div id=""body2"">@RenderBody()</div>
                @RenderSection(""Section1"")"
                .StripLinesAndWhitespace();

            var websiteTemplate1 =
                @"@{Layout=""websiteTemplate2"";}
                <div id=""body1"">@RenderBody()</div>
                @RenderSection(""Section1"")
                @section Section1 {<div>Menu2</div>}"
                .StripLinesAndWhitespace();

            var pageTemplate =
                @"@{Layout=""websiteTemplate1"";}
                <h1>@DateTime.Now.Year</h1>
                @section Section1 {<div>Menu1</div>}"
                .StripLinesAndWhitespace();

            var expectedHtml = (@"<div id=""body2""><div id=""body1"">
                <h1>" + DateTime.Now.Year + @"</h1></div>
                <div>Menu1</div></div><div>Menu2</div>")
                .StripLinesAndWhitespace();

            RazorFormat.AddFileAndPage("/views/websiteTemplate2.cshtml", websiteTemplate2);
            RazorFormat.AddFileAndPage("/views/websiteTemplate1.cshtml", websiteTemplate1);
            var dynamicPage = RazorFormat.AddFileAndPage(@"/page.cshtml", pageTemplate);
            var template = RazorFormat.RenderToHtml(dynamicPage);
            template.Print();

            Assert.That(template, Is.EqualTo(expectedHtml));
        }

        [Test]
        public void Layout_MasterPage_Scenarios_Adding_Sections()
        {
            var websiteTemplate =
@"<!DOCTYPE html>
<html>
    <head>
        <title>Simple Site</title>
    </head>
    <body>
    
        <div id=""header"">
            <a href=""/"">Home</a>
            <a href=""/About"">About</a>
        </div>
        
        <div id=""left-menu"">
            @RenderSection(""Menu"")
        </div>
        
        <div id=""body"">
            @RenderBody()
        </div>
        
        <div id=""footer"">
            @RenderSection(""Footer"")
        </div>
    
    </body>
</html>".NormalizeNewLines();

            var pageTemplate = @"<h1>About this Site</h1>

<p>This is some content that will make up the ""about"" 
page of our web-site. We'll use this in conjunction
with a layout template. The content you are seeing here
comes from ^^^websiteTemplate.</p>

<p>And obviously I can have code in here too. Here is the
current date/year: @DateTime.Now.Year</p>

@section Menu {
<ul>
  <li>About Item 1</li>
  <li>About Item 2</li>
</ul>
}

@section Footer {
<p>This is my custom footer for Home</p>
}
".NormalizeNewLines();

            var expectedHtml = @"<!DOCTYPE html>
<html>
    <head>
        <title>Simple Site</title>
    </head>
    <body>
    
        <div id=""header"">
            <a href=""/"">Home</a>
            <a href=""/About"">About</a>
        </div>
        
        <div id=""left-menu"">
            
<ul>
  <li>About Item 1</li>
  <li>About Item 2</li>
</ul>

        </div>
        
        <div id=""body"">
            <h1>About this Site</h1>

<p>This is some content that will make up the ""about"" 
page of our web-site. We'll use this in conjunction
with a layout template. The content you are seeing here
comes from ^^^websiteTemplate.</p>

<p>And obviously I can have code in here too. Here is the
current date/year: 2015</p>



        </div>
        
        <div id=""footer"">
            
<p>This is my custom footer for Home</p>

        </div>
    
    </body>
</html>".NormalizeNewLines();


            RazorFormat.AddFileAndPage("/views/websiteTemplate.cshtml", websiteTemplate);

            var dynamicPage = RazorFormat.AddFileAndPage("/page.cshtml", pageTemplate);

            var html = RazorFormat.RenderToHtml(dynamicPage, layout: "websiteTemplate");

            html.Print();
            Assert.That(html, Is.EqualTo(expectedHtml));
        }

        [Test]
        public void Encapsulation_and_reuse_with_HTML_helpers()
        {
            var pageTemplate =
@"@model ServiceStack.ServiceHost.Tests.Formats_Razor.Product
<fieldset>
    <legend>Edit Product</legend>
    
    <div>
        @Html.LabelFor(m => m.ProductID)
    </div>
    <div>
        @Html.TextBoxFor(m => m.ProductID)
    </div>
</fieldset>
".NormalizeNewLines();

            var expectedHtml =
@"<fieldset>
    <legend>Edit Product</legend>
    
    <div>
        <label for=""ProductID"">ProductID</label>
    </div>
    <div>
        <input id=""ProductID"" name=""ProductID"" type=""text"" value=""10"" />
    </div>
</fieldset>
".NormalizeNewLines();

            var product = new Product { ProductID = 10 };
            var html = RazorFormat.CreateAndRenderToHtml(pageTemplate, product);

            html.Print();
            Assert.That(html, Is.EqualTo(expectedHtml));
        }

        [Test]
        public void Using_External_HTML_Helpers()
        {
            var pageTemplate =
@"@model System.Collections.Generic.List<ServiceStack.ServiceHost.Tests.Formats_Razor.Product>

<fieldset>
    <legend>All Products</legend>
    @Prod.ProductTable(Model)
</fieldset>".NormalizeNewLines();

            var expectedHtml = @"<fieldset>
    <legend>All Products</legend>
    <table><thead><tr><th>Id</th><th>Name</th><th>Price</th></tr></thead><tbody>
<tr><th>0</th><th>Pen</th><th>1.99</th></tr>
<tr><th>0</th><th>Glass</th><th>9.99</th></tr>
<tr><th>0</th><th>Book</th><th>14.99</th></tr>
<tr><th>0</th><th>DVD</th><th>11.99</th></tr>
</tbody></table>

</fieldset>".NormalizeNewLines();

            var products = new List<Product> {
                new Product("Pen", 1.99m),
                new Product("Glass", 9.99m),
                new Product("Book", 14.99m),
                new Product("DVD", 11.99m),
            };

            RazorFormat.PageBaseType = typeof(CustomViewBase<>);

            var html = RazorFormat.CreateAndRenderToHtml(pageTemplate, model: products);

            html.Print();
            Assert.That(html, Is.EqualTo(expectedHtml));
        }

        [Test]
        public void Using_Custom_base_class()
        {
            var pageTemplate =
@"@inherits ServiceStack.ServiceHost.Tests.Formats_Razor.CustomBaseClass<ServiceStack.ServiceHost.Tests.Formats_Razor.Product>

<fieldset>
    <legend>All Products</legend>
    @Field(""Name"", Model.Name)
</fieldset>".NormalizeNewLines();

            var expectedHtml = @"<fieldset>
    <legend>All Products</legend>
    <label for='Name'>Name</label>
<input name='Name' value='Pen'/>

</fieldset>".NormalizeNewLines();

            RazorFormat.PageBaseType = typeof(CustomBaseClass<>);

            var html = RazorFormat.CreateAndRenderToHtml(pageTemplate, new Product("Pen", 1.99m));

            html.Print();
            Assert.That(html, Is.EqualTo(expectedHtml));
        }

    }
}
