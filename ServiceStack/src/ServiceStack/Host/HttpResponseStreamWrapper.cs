using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack.Host;

public class HttpResponseStreamWrapper(Stream stream, IRequest request) : IHttpResponse, IHasHeaders
{
    public Dictionary<string, string> Headers { get; set; } = new();

    public object OriginalResponse => null;

    public IRequest Request { get; } = request;

    public int StatusCode { set; get; }
    public string StatusDescription { set; get; }
    public string ContentType { get; set; }
    public bool KeepOpen { get; set; }
    public ICookies Cookies { get; set; }

    public void AddHeader(string name, string value)
    {
        this.Headers[name] = value;
    }

    public void RemoveHeader(string name)
    {
        this.Headers.Remove(name);
    }

    public string GetHeader(string name)
    {
        this.Headers.TryGetValue(name, out var value);
        return value;
    }

    public void Redirect(string url)
    {
        this.Headers[HttpHeaders.Location] = url;
    }

    public Stream OutputStream { get; private set; } = stream;

    public object Dto { get; set; }

    public bool UseBufferedStream { get; set; }

    public void Close()
    {
        if (KeepOpen) return;
        ForceClose();
    }

    public Task CloseAsync(CancellationToken token = default(CancellationToken))
    {
        Close();
        return TypeConstants.EmptyTask;
    }

    public void ForceClose()
    {
        if (IsClosed) return;

        OutputStream.Close();
        IsClosed = true;
    }

    public void End()
    {
        Close();
    }

    public void Flush()
    {
        OutputStream.Flush();
    }

    public Task FlushAsync(CancellationToken token = new CancellationToken()) => OutputStream.FlushAsync(token);

    public bool IsClosed { get; private set; }

    public void SetContentLength(long contentLength) {}

    public bool KeepAlive { get; set; }

    public bool HasStarted { get; set; }

    public Dictionary<string, object> Items { get; private set; } = new();

    public void SetCookie(Cookie cookie)
    {
    }

    public void ClearCookies()
    {
    }
}
