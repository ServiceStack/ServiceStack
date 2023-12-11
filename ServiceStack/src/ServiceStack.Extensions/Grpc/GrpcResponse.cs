using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.Grpc;

public class GrpcResponse : IHttpResponse, IHasHeaders, IWriteEvent, IWriteEventAsync
{
    private readonly GrpcRequest request;
    public Dictionary<string, string> Headers { get; }

    public GrpcResponse(GrpcRequest request)
    {
        this.request = request;
        this.OriginalResponse = request.Context;
        this.Headers = new Dictionary<string, string>();
        this.Items = new Dictionary<string, object>();
        Cookies = new Cookies(this);
        this.OutputStream = Stream.Null;
    }

    public object OriginalResponse { get; set; }

    public IRequest Request => request;

    public int StatusCode { get; set; } = 200;

    public string StatusDescription { get; set; }

    public string ContentType
    {
        get => request.ResponseContentType;
        set => request.ResponseContentType = value;
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

    public void Redirect(string url) { }

    public Stream OutputStream { get; }

    public object Dto { get; set; }

    public void Write(string text) { }

    public bool UseBufferedStream { get; set; }

    public void Close()
    {
        IsClosed = true;
    }

    public Task CloseAsync(CancellationToken token = default)
    {
        Close();
        return TypeConstants.EmptyTask;
    }

    public void End()
    {
        Close();
    }

    public void Flush() { }

    public Task FlushAsync(CancellationToken token = default) => TypeConstants.EmptyTask;

    public bool IsClosed { get; set; }

    public void SetContentLength(long contentLength) { }

    public bool KeepAlive { get; set; }

    public bool HasStarted { get; set; }

    public Dictionary<string, object> Items { get; }

    public ICookies Cookies { get; }
    public void SetCookie(Cookie cookie) { }

    public void ClearCookies() { }

    private Channel<string> eventsChannel;

    public Channel<string> EventsChannel =>
        eventsChannel ??= Channel.CreateUnbounded<string>(new UnboundedChannelOptions {SingleReader = true});

    public void WriteEvent(string msg)
    {
        while (!EventsChannel.Writer.TryWrite(msg)) { }
    }

    public async Task WriteEventAsync(string msg, CancellationToken token = default)
    {
        await EventsChannel.Writer.WriteAsync(msg, token);
    }
}