using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.ServiceHost.Tests.AppData;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;
using ServiceStack.VirtualPath;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Formats;
using ServiceStack.WebHost.Endpoints.Support.Markdown;

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
			var json = "~/AppData/ALFKI.json".MapProjectPath().ReadAllText();
			response = JsonSerializer.DeserializeFromString<CustomerDetailsResponse>(json);
		}

		[SetUp]
		public void OnBeforeEachTest()
		{
			appHost = new AppHost();
			markdownFormat = new MarkdownFormat {
                VirtualPathProvider = appHost.VirtualPathProvider
            };
			markdownFormat.Register(appHost);
		}

		public class AppHost : IAppHost
		{
			public AppHost()
			{
				this.Config = new EndpointHostConfig {
					HtmlReplaceTokens = new Dictionary<string, string>(),
					IgnoreFormatsInMetadata = new HashSet<string>(),
				};
				this.ContentTypeFilters = HttpResponseFilter.Instance;
				this.ResponseFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
				this.ViewEngines = new List<IViewEngine>();
				this.CatchAllHandlers = new List<HttpHandlerResolverDelegate>();
				this.VirtualPathProvider = new FileSystemVirtualPathProvider(this, "~".MapProjectPath());
			}

			public void Register<T>(T instance)
			{
				throw new NotImplementedException();
			}

			public void RegisterAs<T, TAs>() where T : TAs
			{
				throw new NotImplementedException();
			}

			public virtual void Release(object instance) { }
		    
            public void OnEndRequest() {}
		    
            public IServiceRoutes Routes { get; private set; }

		    public T TryResolve<T>()
			{
				throw new NotImplementedException();
			}

			public IContentTypeFilter ContentTypeFilters { get; set; }

            public List<Action<IHttpRequest, IHttpResponse>> PreRequestFilters { get; set; }

			public List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters { get; set; }

			public List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters { get; set; }

            public List<IViewEngine> ViewEngines { get; set; }
            
            public HandleUncaughtExceptionDelegate ExceptionHandler { get; set; }

            public HandleServiceExceptionDelegate ServiceExceptionHandler { get; set; }

		    public List<HttpHandlerResolverDelegate> CatchAllHandlers { get; set; }

			public Dictionary<Type, Func<IHttpRequest, object>> RequestBinders
			{
				get { throw new NotImplementedException(); }
			}

			public EndpointHostConfig Config { get; set; }

			public void RegisterService(Type serviceType, params string[] atRestPaths)
			{
				Config.ServiceManager.RegisterService(serviceType);
			}

		    public List<IPlugin> Plugins { get; private set; }

		    public void LoadPlugin(params IPlugin[] plugins)
			{
				plugins.ToList().ForEach(x => x.Register(this));
			}

			public IVirtualPathProvider VirtualPathProvider { get; set; }
		    
            public IServiceRunner<TRequest> CreateServiceRunner<TRequest>(ActionContext actionContext)
		    {
		        throw new NotImplementedException();
		    }
		}

		public string GetHtml(object dto, string format)
		{
			var httpReq = new MockHttpRequest {
				Headers = new NameValueCollection(),
				OperationName = "OperationName",
				QueryString = new NameValueCollection(),
			};
			httpReq.QueryString.Add("format", format);
			using (var ms = new MemoryStream())
			{
				var httpRes = new HttpResponseStreamWrapper(ms);
                appHost.ViewEngines[0].ProcessRequest(httpReq, httpRes, dto);

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
			Assert.That(html.Contains("Customer Orders Total:  $4,596.20"));
		}

		[Test]
		public void Does_serve_dynamic_view_HTML_page_without_template()
		{
			var html = GetHtml(response, "html.bare");

			Console.WriteLine(html);

			Assert.That(html.TrimStart().StartsWith("<h1>Maria Anders Customer Details (Berlin, Germany)</h1>"));
			Assert.That(html.Contains("Customer Orders Total:  $4,596.20"));
		}

		[Test]
		public void Does_serve_dynamic_view_Markdown_page_with_template()
		{
			var html = GetHtml(response, "markdown");

			Console.WriteLine(html);
			//File.WriteAllText("~/AppData/TestsResults/CustomerDetailsResponse.txt".MapAbsolutePath(), html);

			Assert.That(html.StartsWith("<!doctype html>"));
			Assert.That(html.Contains("# Maria Anders Customer Details (Berlin, Germany)"));
			Assert.That(html.Contains("Customer Orders Total:  $4,596.20"));
		}

		[Test]
		public void Does_serve_dynamic_view_Markdown_page_without_template()
		{
			var html = GetHtml(response, "markdown.bare");

			Console.WriteLine(html);

			Assert.That(html.TrimStart().StartsWith("# Maria Anders Customer Details (Berlin, Germany)"));
			Assert.That(html.Contains("Customer Orders Total:  $4,596.20"));
		}


		[Test]
		public void Does_serve_dynamic_view_HTML_page_with_ALT_template()
		{
			var html = GetHtml(response.Customer);

            html.Print();
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
				this.MemoryStream = new MemoryStream();
				this.Cookies = new Cookies(this);
			}

			public object OriginalResponse
			{
				get { throw new NotImplementedException(); }
			}

			public int StatusCode { set; get; }

            public string StatusDescription { set; get; }

			public string ContentType { get; set; }

			private Dictionary<string, string> Headers { get; set; }

			public ICookies Cookies { get; set; }

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

			public void End()
			{
				Close();
			}

			public void Flush()
			{
				MemoryStream.Flush();
			}

			public bool IsClosed { get; private set; }
		}

		[Test]
		public void Does_process_Markdown_pages()
		{
            var markdownHandler = new MarkdownHandler("/AppData/NoTemplate/Static") {
				MarkdownFormat = markdownFormat,
			};
			var httpReq = new MockHttpRequest { QueryString = new NameValueCollection() };
			var httpRes = new MockHttpResponse();
			markdownHandler.ProcessRequest(httpReq, httpRes, "Static");

			var expectedHtml = markdownFormat.Transform(
				File.ReadAllText("~/AppData/NoTemplate/Static.md".MapProjectPath()));

			httpRes.Close();
			Assert.That(httpRes.Contents, Is.EqualTo(expectedHtml));
		}

	}

}