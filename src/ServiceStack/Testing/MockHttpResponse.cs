using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Testing
{
    public class MockHttpResponse : IHttpResponse
    {
        public MockHttpResponse(IRequest request = null)
        {
            this.Request = request;
            this.Headers = PclExportClient.Instance.NewNameValueCollection();
            this.OutputStream = new MemoryStream();
            this.TextWritten = new StringBuilder();
            this.Cookies = new Cookies(this);
            this.Items = new Dictionary<string, object>();
        }

        public IRequest Request { get; private set; }
        public object OriginalResponse { get; private set; }
        public int StatusCode { set; get; }
        public string StatusDescription { set; get; }
        public string ContentType { get; set; }

        public StringBuilder TextWritten { get; set; }

        public INameValueCollection Headers { get; set; }

        public ICookies Cookies { get; set; }

        public void AddHeader(string name, string value)
        {
            this.Headers.Add(name, value);
        }

        public void Redirect(string url)
        {
            this.Headers.Add(HttpHeaders.Location, url.MapServerPath());
        }

        public Stream OutputStream { get; private set; }

        public object Dto { get; set; }

        public void Write(string text)
        {
            this.TextWritten.Append(text);
        }

        public bool UseBufferedStream { get; set; }

        public void Close()
        {
            this.IsClosed = true;
        }

        public void End()
        {
            Close();
        }

        public void Flush()
        {
            OutputStream.Flush();
        }

        public string ReadAsString()
        {
            if (!IsClosed) this.OutputStream.Seek(0, SeekOrigin.Begin);
            var bytes = ((MemoryStream)OutputStream).ToArray();
            return bytes.FromUtf8Bytes();
        }

        public byte[] ReadAsBytes()
        {
            if (!IsClosed) this.OutputStream.Seek(0, SeekOrigin.Begin);
            var ms = (MemoryStream)this.OutputStream;
            return ms.ToArray();
        }

        public bool IsClosed { get; private set; }

        public void SetContentLength(long contentLength)
        {
            Headers[HttpHeaders.ContentLength] = contentLength.ToString(CultureInfo.InvariantCulture);
        }

        public bool KeepAlive { get; set; }

        public Dictionary<string, object> Items { get; private set; }

        public void SetCookie(Cookie cookie)
        {            
        }
    }
}