using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack.Host;

public class BasicResponse(BasicRequest requestContext) : IResponse, IHasHeaders
{
    public Dictionary<string, string> Headers { get; } = new();

    public object OriginalResponse { get; set; }

    public IRequest Request => requestContext;

    public int StatusCode { get; set; }

    public string StatusDescription { get; set; }

    public string ContentType
    {
        get => requestContext.ResponseContentType;
        set => requestContext.ResponseContentType = value;
    }

    public void AddHeader(string name, string value)
    {
        Headers[name] = value;
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
    }

    private MemoryStream ms;

    public Stream OutputStream => ms ?? (ms = new MemoryStream());

    public object Dto { get; set; }

    public void Write(string text)
    {
        var bytes = text.ToUtf8Bytes();
        ms.Write(bytes, 0, bytes.Length);
    }

    public bool UseBufferedStream { get; set; }

    public void Close()
    {
        if (IsClosed) return;
        IsClosed = true;
        if (ms != null && ms.CanWrite)
            ms.Dispose();
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
    }

    public Task FlushAsync(CancellationToken token = default(CancellationToken)) => TypeConstants.EmptyTask;

    public bool IsClosed { get; set; }

    public void SetContentLength(long contentLength)
    {
    }

    public bool KeepAlive { get; set; }

    public bool HasStarted { get; set; }

    public Dictionary<string, object> Items { get; } = new();
}
