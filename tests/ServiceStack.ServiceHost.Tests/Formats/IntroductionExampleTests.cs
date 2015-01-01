using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.ServiceHost.Tests.Formats
{
	public class Product
	{
		public Product(){}
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
	public class IntroductionExampleTests : MarkdownTestBase
	{
		private List<Product> products;
		Dictionary<string, object> productArgs;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			this.products = new List<Product> {
				new Product("Pen", 1.99m),
				new Product("Glass", 9.99m),
				new Product("Book", 14.99m),
				new Product("DVD", 11.99m),
			};
			productArgs = new Dictionary<string, object> { { "products", products } };
		}

		[Test]
		public void Basic_Razor_Example()
		{
			var template = 
@"# Razor Example

###  Hello @name, the year is @DateTime.Now.Year

Checkout [this product](/Product/Details/@productId)";

			var expectedHtml = 
@"<h1>Razor Example</h1>
<h3>Hello Demis, the year is 2015</h3>
<p>Checkout <a href=""/Product/Details/10"">this product</a></p>
".NormalizeNewLines();

			var html = RenderToHtml(template, new Dictionary<string, object> { 
				{"name", "Demis"}, {"productId", 10} });

			Console.WriteLine(html);
			Assert.That(html, Is.EqualTo(expectedHtml));
		}


		[Test]
		public void Simple_loop()
		{
			var template = @"
@foreach (var p in products) {
  - @p.Name: (@p.Price)
}
";

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
		public void Simple_loop_with_parens_free_syntax()
		{
			var template = @"
@foreach p in products {
  - @p.Name: (@p.Price)
}
";

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
@if (products.Count == 0) {
Sorry - no products in this category
} else {
We have products for you!
}
";

			var expectedHtml = 
@"<p>We have products for you!</p>
".NormalizeNewLines();

			var html = RenderToHtml(template, productArgs);

			Console.WriteLine(html);
			Assert.That(html, Is.EqualTo(expectedHtml));
		}

		[Test]
		public void If_Statment_with_parens_free_syntax()
		{
			var template = @"
@if products.Count == 0 {
Sorry - no products in this category
} else {
We have products for you!
}
";

			var expectedHtml = 
@"<p>We have products for you!</p>
".NormalizeNewLines();

			var html = RenderToHtml(template, productArgs);

			Console.WriteLine(html);
			Assert.That(html, Is.EqualTo(expectedHtml));
		}


		[Test]
		public void Multi_variable_declarations()
		{
			var template = @"
@var number = 1
@var message = ""Number is "" + number

Your Message: @message
";

			var expectedHtml = @"<p>Your Message: Number is 1</p>
".NormalizeNewLines();

			var html = RenderToHtml(template, productArgs);

			Console.WriteLine(html);
			Assert.That(html, Is.EqualTo(expectedHtml));
		}


		[Test]
		public void Integrating_content_and_code()
		{
			var template = 
@"Send mail to demis.bellot@gmail.com telling him the time: @DateTime.Now.
";

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
@if (DateTime.Now.Year == 2015) {

If the year is 2015 then print this 
multi-line text block and 
the date: @DateTime.Now
}
".NormalizeNewLines();

			var expectedHtml = 
@"<p>If the year is 2015 then print this 
multi-line text block and 
the date: 02/06/2014 06:42:45</p>
".NormalizeNewLines();

			var html = RenderToHtml(template, productArgs);

            html.Print();
			Assert.That(html.Substring(0,html.Length-27),
                Is.EqualTo(expectedHtml.Substring(0, html.Length - 27)));
		}

		[Test]
		public void HTML_encoding()
		{
			var template = 
@"
Some Content @stringContainingHtml
";

			var expectedHtml = 
@"<p>Some Content &lt;span&gt;html&lt;/span&gt;</p>
".NormalizeNewLines();

			var html = RenderToHtml(template, new Dictionary<string, object> {
				{"stringContainingHtml", "<span>html</span>"}
			});

			Console.WriteLine(html);
			Assert.That(html, Is.EqualTo(expectedHtml));
		}

	}
}