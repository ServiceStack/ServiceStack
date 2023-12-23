using System;
using System.IO;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers;

public class StaticContentHandler : HttpAsyncTaskHandler
{
    readonly string textContents;
    private readonly byte[] bytes;
    readonly Stream stream;
    readonly string contentType;

    private StaticContentHandler(string contentType)
    {
        this.contentType = contentType ?? throw new ArgumentNullException(nameof(contentType)); 
        this.RequestName = nameof(StaticContentHandler);
    }

    public StaticContentHandler(string textContents, string contentType) : this(contentType) => this.textContents = textContents;
    public StaticContentHandler(byte[] bytes, string contentType) : this(contentType) => this.bytes = bytes;
    public StaticContentHandler(Stream stream, string contentType) : this(contentType)
    {
        stream.Position = 0;
        this.stream = stream;
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
        else if (stream != null)
            await stream.CopyToAsync(httpRes.OutputStream).ConfigAwait();

        await httpRes.FlushAsync();
        httpRes.EndHttpHandlerRequest(skipHeaders: true);
    }
}
