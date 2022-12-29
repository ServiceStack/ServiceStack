using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Formats;
using ServiceStack.Host;
using ServiceStack.ServiceHost.Tests.AppData;
using ServiceStack.Support.Markdown;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.ServiceHost.Tests.Formats
{
    [TestFixture]
    public class ViewTests
    {
        private CustomerDetailsResponse response;
        private MarkdownFormat markdownFormat;
        private ServiceStackHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            var json = "~/AppData/ALFKI.json".MapProjectPath().ReadAllText();
            response = JsonSerializer.DeserializeFromString<CustomerDetailsResponse>(json);

            appHost = new BasicAppHost
            {
                Plugins = { new MarkdownFormat() },
                ConfigFilter = config =>
                {
                    //Files aren't copied, set RootDirectory to ProjectPath instead.
                    config.WebHostPhysicalPath = "~".MapProjectPath();
                }
            }.Init();
            markdownFormat = appHost.GetPlugin<MarkdownFormat>();
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        public string GetHtml(object dto, string format)
        {
            var httpReq = new MockHttpRequest
            {
                Headers = new NameValueCollection(),
                OperationName = "OperationName",
                QueryString = new NameValueCollection(),
            };
            httpReq.QueryString.Add("format", format);
            using (var ms = new MemoryStream())
            {
                var httpRes = new HttpResponseStreamWrapper(ms, httpReq);
                appHost.ViewEngines[0].ProcessRequestAsync(httpReq, dto, httpRes.OutputStream);

                var html = ms.ReadToEnd();
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

            html.Print();
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

            public MockHttpResponse(IRequest httpReq)
            {
                this.Request = httpReq;
                this.Headers = new Dictionary<string, string>();
                this.MemoryStream = new MemoryStream();
                this.Cookies = new Host.Cookies(this);
                this.Items = new Dictionary<string, object>();
            }

            public object OriginalResponse
            {
                get { throw new NotImplementedException(); }
            }

            public IRequest Request { get; private set; }

            public int StatusCode { set; get; }

            public string StatusDescription { set; get; }

            public string ContentType { get; set; }

            private Dictionary<string, string> Headers { get; set; }

            public ICookies Cookies { get; set; }

            public void AddHeader(string name, string value)
            {
                this.Headers.Add(name, value);
            }

            public void RemoveHeader(string name)
            {
                Headers.Remove(name);
            }

            public string GetHeader(string name)
            {
                this.Headers.TryGetValue(name, out var value);
                return value;
            }

            public void Redirect(string url)
            {
                this.Headers[HttpHeaders.Location] = url;
            }

            public Stream OutputStream => MemoryStream;

            public object Dto { get; set; }

            public bool UseBufferedStream { get; set; }

            public string Contents { get; set; }

            public void Close()
            {
                this.Contents = MemoryStream.ReadToEnd();
                MemoryStream.Close();
                this.IsClosed = true;
            }

            public Task CloseAsync(CancellationToken token = default(CancellationToken))
            {
                Close();
                return TypeConstants.EmptyTask;
            }

            public void End()
            {
                Close();
            }

            public void Flush()
            {
                MemoryStream.Flush();
            }

            public Task FlushAsync(CancellationToken token = new CancellationToken()) => MemoryStream.FlushAsync(token);

            public bool IsClosed { get; private set; }

            public void SetContentLength(long contentLength)
            {
                Headers[HttpHeaders.ContentLength] = contentLength.ToString();
            }

            public bool KeepAlive { get; set; }

            public bool HasStarted { get; set; }

            public Dictionary<string, object> Items { get; private set; }

            public void SetCookie(Cookie cookie)
            {
            }

            public void ClearCookies()
            {
            }
        }

        [Test]
        public void Does_process_Markdown_pages()
        {
            var markdownHandler = new MarkdownHandler("/AppData/NoTemplate/Static")
            {
                MarkdownFormat = markdownFormat,
            };
            var httpReq = new MockHttpRequest { QueryString = new NameValueCollection() };
            var httpRes = new MockHttpResponse(httpReq);
            markdownHandler.ProcessRequestAsync(httpReq, httpRes, "Static").Wait();

            var expectedHtml = markdownFormat.Transform(
                File.ReadAllText("~/AppData/NoTemplate/Static.md".MapProjectPath()));

            httpRes.Close();
            Assert.That(httpRes.Contents, Is.EqualTo(expectedHtml));
        }

    }

}