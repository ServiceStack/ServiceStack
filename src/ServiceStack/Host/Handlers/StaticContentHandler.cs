using System;
using System.Web;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public class StaticContentHandler : HttpAsyncTaskHandler
    {
        readonly string textContents;
        private readonly byte[] bytes;
        readonly string contentType;

        private StaticContentHandler(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                throw new ArgumentNullException(nameof(contentType));

            this.contentType = contentType;
            this.RequestName = GetType().Name;
        }

        public StaticContentHandler(string textContents, string contentType)
            : this(contentType)
        {
            this.textContents = textContents;
        }

        public StaticContentHandler(byte[] bytes, string contentType)
            : this(contentType)
        {
            this.bytes = bytes;
        }

        public override void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName)
        {
            if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
                return;
            if (textContents == null && bytes == null)
                return;

            httpRes.ContentType = contentType;

            if (textContents != null)
                httpRes.Write(textContents);
            else if (bytes != null)
                httpRes.OutputStream.Write(bytes, 0, bytes.Length);

            httpRes.Flush();
            httpRes.EndHttpHandlerRequest(skipHeaders: true);
        }

#if !NETSTANDARD1_6
        public override void ProcessRequest(HttpContextBase context)
        {
            var httpReq = context.ToRequest("StaticContent");
            ProcessRequest(httpReq, httpReq.Response, "StaticContent");
        }
#endif
    }
}