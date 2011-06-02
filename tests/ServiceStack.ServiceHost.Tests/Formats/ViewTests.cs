using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using NUnit.Framework;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost.Tests.AppData;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;
using ServiceStack.WebHost.EndPoints.Formats;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceHost.Tests.Formats
{
	[TestFixture]
	public class ViewTests
	{
		private CustomerDetailsResponse response;
		private MarkdownFormat markdownFormat;
		private AppHost appHost;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			var json = "~/AppData/ALFKI.json".MapAbsolutePath().ReadAllText();
			response = JsonSerializer.DeserializeFromString<CustomerDetailsResponse>(json);
		}

		[SetUp]
		public void OnBeforeEachTest()
		{
			appHost = new AppHost();
			markdownFormat = new MarkdownFormat();
			markdownFormat.Register(appHost);
		}

		public class AppHost : IAppHost
		{
			public AppHost()
			{
				this.Config = new EndpointHostConfig {
					WebHostPhysicalPath = "~".MapAbsolutePath(), //Registers all .md;.markdown files from here
				};
				this.ContentTypeFilters = HttpResponseFilter.Instance;
				this.ResponseFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
				this.HtmlProviders = new List<StreamSerializerResolverDelegate>();
			}

			public T TryResolve<T>()
			{
				throw new NotImplementedException();
			}

			public IContentTypeFilter ContentTypeFilters { get; set; }

			public List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters { get; set; }

			public List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters { get; set; }

			public List<StreamSerializerResolverDelegate> HtmlProviders { get; set; }

			public EndpointHostConfig Config { get; set; }
		}

		public string GetHtml(object dto, string format)
		{
			var httpReq = new MockHttpRequest {
				Headers = new NameValueCollection(),
				OperationName = "OperationName",
				QueryString = new NameValueCollection(),
			};
			httpReq.QueryString.Add("format", format);
			var requestContext = new HttpRequestContext(httpReq, dto);
			using (var ms = new MemoryStream())
			{
				appHost.HtmlProviders[0](requestContext, dto, ms);

				var utf8Bytes = ms.ToArray();
				var html = utf8Bytes.FromUtf8Bytes();
				return html;
			}
		}

		public string GetHtml(object dto)
		{
			return GetHtml(dto, "html");
		}

		[Test]
		public void Does_server_dynamic_view_HTML_page_with_template()
		{
			var html = GetHtml(response);

			Console.WriteLine(html);
			File.WriteAllText("~/AppData/TestsResults/CustomerDetailsResponse.htm".MapAbsolutePath(), html);

			Assert.That(html.StartsWith("<!doctype html>"));
			Assert.That(html.Contains("Customer Orders Total:  &#163;4,596.20"));
		}

		[Test]
		public void Does_server_dynamic_view_HTML_page_without_template()
		{
			var html = GetHtml(response, "html.bare");

			Console.WriteLine(html);

			Assert.That(html.TrimStart().StartsWith("<h1>Maria Anders Customer Details (Berlin, Germany)</h1>"));
			Assert.That(html.Contains("Customer Orders Total:  &#163;4,596.20"));
		}

		[Test]
		public void Does_server_dynamic_view_Markdown_page_with_template()
		{
			var html = GetHtml(response, "markdown");

			Console.WriteLine(html);
			File.WriteAllText("~/AppData/TestsResults/CustomerDetailsResponse.txt".MapAbsolutePath(), html);

			Assert.That(html.StartsWith("<!doctype html>"));
			Assert.That(html.Contains("# Maria Anders Customer Details (Berlin, Germany)"));
			Assert.That(html.Contains("Customer Orders Total:  &#163;4,596.20"));
		}

		[Test]
		public void Does_server_dynamic_view_Markdown_page_without_template()
		{
			var html = GetHtml(response, "markdown.bare");

			Console.WriteLine(html);

			Assert.That(html.TrimStart().StartsWith("# Maria Anders Customer Details (Berlin, Germany)"));
			Assert.That(html.Contains("Customer Orders Total:  &#163;4,596.20"));
		}


		[Test]
		public void Does_server_dynamic_view_HTML_page_with_ALT_template()
		{
			var html = GetHtml(response.Customer);

			Console.WriteLine(html);
			File.WriteAllText("~/AppData/TestsResults/Customer.htm".MapAbsolutePath(), html);

			Assert.That(html.StartsWith("<!doctype html>"));
			Assert.That(html.Contains("ALT Template"));
			Assert.That(html.Contains("<li><strong>Address:</strong> Obere Str. 57</li>"));
		}

	}

}