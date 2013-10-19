using System.Collections.Generic;
using System.IO;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public class BasicResponse : IResponse
    {
        private readonly BasicRequest requestContext;
        private Dictionary<string, string> Headers { get; set; }

        public BasicResponse(BasicRequest requestContext)
        {
            this.requestContext = requestContext;
            this.Headers = new Dictionary<string, string>();
            this.Cookies = new BasicCookies(this);
        }

        public object OriginalResponse { get; set; }

        public int StatusCode { get; set; }

        public string StatusDescription { get; set; }

        public string ContentType
        {
            get { return requestContext.ResponseContentType; }
            set { requestContext.ResponseContentType = value; }
        }

        public ICookies Cookies { get; set; }

        public void AddHeader(string name, string value)
        {
            Headers[name] = value;
        }

        public void Redirect(string url) {}

        private MemoryStream ms;
        public Stream OutputStream
        {
            get { return ms ?? (ms = new MemoryStream()); }
        }

        public void Write(string text)
        {
            var bytes = text.ToUtf8Bytes();
            ms.Write(bytes, 0, bytes.Length);
        }

        public void Close()
        {
            IsClosed = true;
        }

        public void End()
        {
            Close();
        }

        public void Flush() {}

        public bool IsClosed { get; set; }

        public void SetContentLength(long contentLength) {}
    }

    public class BasicCookies : ICookies
    {
        private IResponse response;

        public BasicCookies(IResponse response)
        {
            this.response = response;
        }

        public void DeleteCookie(string cookieName)
        {            
        }

        public void AddPermanentCookie(string cookieName, string cookieValue, bool? secureOnly = null)
        {
        }

        public void AddSessionCookie(string cookieName, string cookieValue, bool? secureOnly = null)
        {
        }
    }
}