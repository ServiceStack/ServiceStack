using System;
using System.Threading.Tasks;
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
            this.contentType = contentType ?? throw new ArgumentNullException(nameof(contentType)); 
            this.RequestName = nameof(StaticContentHandler);
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

        public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
        {
            if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
                return;
            if (textContents == null && bytes == null)
                return;

            httpRes.ContentType = contentType;

            if (textContents != null)
                await httpRes.WriteAsync(textContents);
            else if (bytes != null)
                await httpRes.OutputStream.WriteAsync(bytes, 0, bytes.Length);

            await httpRes.FlushAsync();
            httpRes.EndHttpHandlerRequest(skipHeaders: true);
        }
    }
}