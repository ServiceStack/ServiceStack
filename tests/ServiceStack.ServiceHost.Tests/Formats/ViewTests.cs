﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using NUnit.Framework;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost.Tests.AppData;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;
using ServiceStack.WebHost.EndPoints.Formats;
using ServiceStack.WebHost.EndPoints.Support.Markdown;
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
					MarkdownSearchPath = "~".MapAbsolutePath(),
					MarkdownReplaceTokens = new Dictionary<string, string>(),
					IgnoreFormatsInMetadata = new HashSet<string>(),
				};
				this.ContentTypeFilters = HttpResponseFilter.Instance;
				this.ResponseFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
				this.HtmlProviders = new List<StreamSerializerResolverDelegate>();
				this.CatchAllHandlers = new List<HttpHandlerResolverDelegate>();
			}

			public T TryResolve<T>()
			{
				throw new NotImplementedException();
			}

			public IContentTypeFilter ContentTypeFilters { get; set; }

			public List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters { get; set; }

			public List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters { get; set; }

			public List<StreamSerializerResolverDelegate> HtmlProviders { get; set; }

			public List<HttpHandlerResolverDelegate> CatchAllHandlers { get; set; }

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
			var requestContext = new HttpRequestContext((IHttpRequest)httpReq, dto);
			using (var ms = new MemoryStream())
			{
				var httpRes = new HttpResponseStreamWrapper(ms);
				appHost.HtmlProviders[0](requestContext, dto, httpRes);

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
		public void Does_serve_dynamic_view_HTML_page_with_template()
		{
			var html = GetHtml(response);

			Console.WriteLine(html);
			//File.WriteAllText("~/AppData/TestsResults/CustomerDetailsResponse.htm".MapAbsolutePath(), html);

			Assert.That(html.StartsWith("<!doctype html>"));
			Assert.That(html.Contains("Customer Orders Total:  &#163;4,596.20"));
		}

		[Test]
		public void Does_serve_dynamic_view_HTML_page_without_template()
		{
			var html = GetHtml(response, "html.bare");

			Console.WriteLine(html);

			Assert.That(html.TrimStart().StartsWith("<h1>Maria Anders Customer Details (Berlin, Germany)</h1>"));
			Assert.That(html.Contains("Customer Orders Total:  &#163;4,596.20"));
		}

		[Test]
		public void Does_serve_dynamic_view_Markdown_page_with_template()
		{
			var html = GetHtml(response, "markdown");

			Console.WriteLine(html);
			//File.WriteAllText("~/AppData/TestsResults/CustomerDetailsResponse.txt".MapAbsolutePath(), html);

			Assert.That(html.StartsWith("<!doctype html>"));
			Assert.That(html.Contains("# Maria Anders Customer Details (Berlin, Germany)"));
			Assert.That(html.Contains("Customer Orders Total:  &#163;4,596.20"));
		}

		[Test]
		public void Does_serve_dynamic_view_Markdown_page_without_template()
		{
			var html = GetHtml(response, "markdown.bare");

			Console.WriteLine(html);

			Assert.That(html.TrimStart().StartsWith("# Maria Anders Customer Details (Berlin, Germany)"));
			Assert.That(html.Contains("Customer Orders Total:  &#163;4,596.20"));
		}


		[Test]
		public void Does_serve_dynamic_view_HTML_page_with_ALT_template()
		{
			var html = GetHtml(response.Customer);

			Console.WriteLine(html);
			//File.WriteAllText("~/AppData/TestsResults/Customer.htm".MapAbsolutePath(), html);

			Assert.That(html.StartsWith("<!doctype html>"));
			Assert.That(html.Contains("ALT Template"));
			Assert.That(html.Contains("<li><strong>Address:</strong> Obere Str. 57</li>"));
		}

		public class MockHttpResponse : IHttpResponse
		{
			public MemoryStream MemoryStream { get; set; }

			public MockHttpResponse()
			{
				this.Headers = new Dictionary<string, string>();
				MemoryStream = new MemoryStream();
			}

			public int StatusCode { set; private get; }

            public string StatusDescription { set; private get; }

			public string ContentType { get; set; }

			private Dictionary<string, string> Headers { get; set; }

			public void AddHeader(string name, string value)
			{
				this.Headers.Add(name, value);
			}

			public void Redirect(string url)
			{
				this.Headers[HttpHeaders.Location] = url;
			}

			public Stream OutputStream { get { return MemoryStream; } }

			public void Write(string text)
			{
				var bytes = Encoding.UTF8.GetBytes(text);
				MemoryStream.Write(bytes, 0, bytes.Length);
			}

			public string Contents { get; set; }

			public void Close()
			{
				this.Contents = Encoding.UTF8.GetString(MemoryStream.ToArray());
				MemoryStream.Close();
				this.IsClosed = true;
			}

			public bool IsClosed { get; private set; }
		}

		[Test]
		public void Does_process_Markdown_pages()
		{
			var markdownHandler = new MarkdownHandler {
				MarkdownFormat = markdownFormat,
				PathInfo = "/AppData/NoTemplate/Static.md",
				FilePath = "~/AppData/NoTemplate/Static.md".MapAbsolutePath(),
			};
			var httpReq = new MockHttpRequest { QueryString = new NameValueCollection() };
			var httpRes = new MockHttpResponse();
			markdownHandler.ProcessRequest(httpReq, httpRes, "Static");

			var expectedHtml = markdownFormat.Transform(
				File.ReadAllText("~/AppData/NoTemplate/Static.md".MapAbsolutePath()));

			httpRes.Close();
			Assert.That(httpRes.Contents, Is.EqualTo(expectedHtml));
		}

	}

}