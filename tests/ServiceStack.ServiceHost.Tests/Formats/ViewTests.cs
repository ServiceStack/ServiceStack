using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
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

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var json = "~/AppData/ALFKI.json".MapProjectPath().ReadAllText();
            response = JsonSerializer.DeserializeFromString<CustomerDetailsResponse>(json);

            appHost = new BasicAppHost
            {
                ConfigFilter = config => {
                    //Files aren't copied, set RootDirectory to ProjectPath instead.
                    config.WebHostPhysicalPath = "~".MapProjectPath(); 
                }
            }.Init();
            markdownFormat = appHost.GetPlugin<MarkdownFormat>();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        public string GetHtml(object dto, string format)
        {
            var httpReq = new MockHttpRequest
            {
                Headers = PclExportClient.Instance.NewNameValueCollection(),
                OperationName = "OperationName",
                QueryString = PclExportClient.Instance.NewNameValueCollection(),
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

            public MockHttpResponse()
            {
                this.Headers = new Dictionary<string, string>();
                this.MemoryStream = new MemoryStream();
                this.Cookies = new Cookies(this);
                this.Items = new Dictionary<string, object>();
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

            public object Dto { get; set; }

            public void Write(string text)
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                MemoryStream.Write(bytes, 0, bytes.Length);
            }

            public bool UseBufferedStream { get; set; }

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

            public void SetContentLength(long contentLength)
            {
                Headers[HttpHeaders.ContentLength] = contentLength.ToString();
            }

            public bool KeepAlive { get; set; }

            public Dictionary<string, object> Items { get; private set; }

            public void SetCookie(Cookie cookie)
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
            var httpReq = new MockHttpRequest { QueryString = PclExportClient.Instance.NewNameValueCollection() };
            var httpRes = new MockHttpResponse();
            markdownHandler.ProcessRequestAsync(httpReq, httpRes, "Static").Wait();

            var expectedHtml = markdownFormat.Transform(
                File.ReadAllText("~/AppData/NoTemplate/Static.md".MapProjectPath()));

            httpRes.Close();
            Assert.That(httpRes.Contents, Is.EqualTo(expectedHtml));
        }

    }

}