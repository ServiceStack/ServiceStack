using System.IO;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.ServiceInterface.Testing
{
    public class MockHttpResponse : IHttpResponse
    {
        public MockHttpResponse()
        {
            this.Cookies = new Cookies(this);
            this.OutputStream = new MemoryStream();
        }

        public object OriginalResponse { get; private set; }
        public int StatusCode { set; get; }
        public string StatusDescription { set; get; }
        public string ContentType { get; set; }

        public ICookies Cookies { get; set; }

        public void AddHeader(string name, string value)
        {
        }

        public void Redirect(string url)
        {
        }

        public Stream OutputStream { get; private set; }

        public void Write(string text)
        {
        }

        public void Close()
        {
            IsClosed = true;
        }

        public void End()
        {
            Close();
        }

        public void Flush()
        {
            this.OutputStream.Position = 0;
        }

        public string ReadAsString()
        {
            this.OutputStream.Position = 0;
            return this.OutputStream.ReadFully().FromUtf8Bytes();
        }

        public bool IsClosed { get; private set; }

        public void SetContentLength(long contentLength) {}
    }
}