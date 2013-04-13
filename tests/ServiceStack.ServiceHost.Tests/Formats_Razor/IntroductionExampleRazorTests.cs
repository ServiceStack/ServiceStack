using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common.Utils;
using ServiceStack.Razor2;
using ServiceStack.ServiceHost.Tests.Formats;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;
using ServiceStack.VirtualPath;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceHost.Tests.Formats_Razor
{
	public class Product
	{
		public Product() { }
		public Product(string name, decimal price)
		{
			Name = name;
			Price = price;
		}

		public int ProductID { get; set; }
		public string Name { get; set; }
		public decimal Price { get; set; }
	}

	[TestFixture]
	public class IntroductionExampleRazorTests : RazorTestBase
	{
		private List<Product> products;
		object productArgs;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			this.products = new List<Product> {
				new Product("Pen", 1.99m),
				new Product("Glass", 9.99m),
				new Product("Book", 14.99m),
				new Product("DVD", 11.99m),
			};
			productArgs = new { products = products };
		}
        
        [SetUp]
        public void SetUp()
        {
            RazorFormat = new RazorFormat {
                DefaultBaseType = typeof(CustomRazorBasePage<>),
                VirtualPathProvider = new InMemoryVirtualPathProvider(new BasicAppHost()),
                TemplateProvider = { CompileInParallelWithNoOfThreads = 0 },
            };
            RazorFormat.Init();            
        }

		[Test]
		public void Basic_Razor_Example()
		{
			var template = 
@"<h1>Razor Example</h1>

<h3>Hello @Model.name, the year is @DateTime.Now.Year</h3>

<p>Checkout <a href=""/Product/Details/@Model.productId"">this product</a></p>
".NormalizeNewLines();

			var expectedHtml = 
@"<h1>Razor Example</h1>

<h3>Hello Demis, the year is 2013</h3>

<p>Checkout <a href=""/Product/Details/10"">this product</a></p>
".NormalizeNewLines();

			var html = RenderToHtml(template, new { name = "Demis", productId = 10 });

			Console.WriteLine(html);
			Assert.That(html, Is.EqualTo(expectedHtml));
		}


		[Test]
		public void Simple_loop()
		{
			var template = @"<ul>
@foreach (var p in Model.products) {
	<li>@p.Name: (@p.Price)</li>
}
</ul>
".NormalizeNewLines();

			var expectedHtml = 
@"<ul>
	<li>Pen: (1.99)</li>
	<li>Glass: (9.99)</li>
	<li>Book: (14.99)</li>
	<li>DVD: (11.99)</li>
</ul>
".NormalizeNewLines();

			var html = RenderToHtml(template, productArgs);

			Console.WriteLine(html);
			Assert.That(html, Is.EqualTo(expectedHtml));
		}

		[Test]
		public void If_Statment()
		{
			var template = @"
@if (Model.products.Count == 0) {
<p>Sorry - no products in this category</p>
} else {
<p>We have products for you!</p>
}
".NormalizeNewLines();

			var expectedHtml = @"
<p>We have products for you!</p>
".NormalizeNewLines();

			var html = RenderToHtml(template, productArgs);

			Console.WriteLine(html);
			Assert.That(html, Is.EqualTo(expectedHtml));
		}

		[Test]
		public void Multi_variable_declarations()
		{
			var template = @"
@{ 
var number = 1; 
var message = ""Number is "" + number; 
}
<p>Your Message: @message</p>
".NormalizeNewLines();

			var expectedHtml = @"
<p>Your Message: Number is 1</p>
".NormalizeNewLines();

			var html = RenderToHtml(template, productArgs);

			Console.WriteLine(html);
			Assert.That(html, Is.EqualTo(expectedHtml));
		}


		[Test]
		public void Integrating_content_and_code()
		{
			var template = 
@"<p>Send mail to demis.bellot@gmail.com telling him the time: @DateTime.Now.</p>
".NormalizeNewLines();

			var expectedHtml = 
@"<p>Send mail to demis.bellot@gmail.com telling him the time: 02/06/2011 06:38:34.</p>
".NormalizeNewLines();

			var html = RenderToHtml(template, productArgs);

			Console.WriteLine(html);
			Assert.That(html, Is.StringMatching(expectedHtml.Substring(0, expectedHtml.Length - 25)));
		}


		[Test]
		public void Identifying_nested_content()
		{
			var template = 
@"
@if (DateTime.Now.Year == 2013) {
<p>If the year is 2013 then print this 
multi-line text block and 
the date: @DateTime.Now</p>
}
".NormalizeNewLines();

			var expectedHtml = 
@"<p>If the year is 2013 then print this 
multi-line text block and 
the date: 02/06/2013 06:42:45</p>
".NormalizeNewLines();

			var html = RenderToHtml(template, productArgs);

			Console.WriteLine(html);
			Assert.That(html, Is.StringMatching(expectedHtml.Substring(0, expectedHtml.Length - 25)));
		}

		[Test]
		public void HTML_encoding()
		{
			var template = 
@"<p>Some Content @Model.stringContainingHtml</p>
".NormalizeNewLines();

			var expectedHtml = 
@"<p>Some Content &lt;span&gt;html&lt;/span&gt;</p>
".NormalizeNewLines();

			var html = RenderToHtml(template, new { stringContainingHtml = "<span>html</span>"});

            html.Print();
			Assert.That(html, Is.EqualTo(expectedHtml));
		}

	}
}
