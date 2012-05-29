using System.IO;
using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface.Testing
{
    public class MockHttpResponse : IHttpResponse
    {
        public MockHttpResponse()
        {
            this.Cookies = new Cookies(this);
        }

        public object OriginalResponse { get; private set; }
        public int StatusCode { set; private get; }
        public string StatusDescription { set; private get; }
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
        }

        public bool IsClosed { get; private set; }
    }
}