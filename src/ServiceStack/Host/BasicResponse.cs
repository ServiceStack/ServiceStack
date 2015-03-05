using System.Collections.Generic;
using System.IO;
using ServiceStack.Text;
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
            this.Items = new Dictionary<string, object>();
        }

        public object OriginalResponse { get; set; }

        public IRequest Request
        {
            get { return requestContext; }
        }

        public int StatusCode { get; set; }

        public string StatusDescription { get; set; }

        public string ContentType
        {
            get { return requestContext.ResponseContentType; }
            set { requestContext.ResponseContentType = value; }
        }

        public void AddHeader(string name, string value)
        {
            Headers[name] = value;
        }

        public void Redirect(string url)
        {
        }

        private MemoryStream ms;

        public Stream OutputStream
        {
            get { return ms ?? (ms = new MemoryStream()); }
        }

        public object Dto { get; set; }

        public void Write(string text)
        {
            var bytes = text.ToUtf8Bytes();
            ms.Write(bytes, 0, bytes.Length);
        }

        public bool UseBufferedStream { get; set; }

        public void Close()
        {
            IsClosed = true;
            if (ms != null && ms.CanWrite)
                ms.Dispose();
        }

        public void End()
        {
            Close();
        }

        public void Flush()
        {
        }

        public bool IsClosed { get; set; }

        public void SetContentLength(long contentLength)
        {
        }

        public bool KeepAlive { get; set; }

        public Dictionary<string, object> Items { get; private set; }
    }
}