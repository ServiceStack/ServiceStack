#if !NETCORE

//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host.AspNet;

public class AspNetResponse : IHttpResponse, IHasHeaders
{
    //private static readonly ILog Log = LogManager.GetLogger(typeof(HttpResponseWrapper));

    private readonly HttpResponseBase response;

    public AspNetResponse(HttpResponseBase response, IRequest request = null)
    {
        this.response = response;
        this.Request = request;
        this.response.TrySkipIisCustomErrors = true;
        this.Cookies = HostContext.AppHost.GetCookies(this);
        this.Items = new Dictionary<string, object>();
    }

    public HttpResponseBase Response => response;

    public object OriginalResponse => response;

    public IRequest Request { get; private set; }

    public int StatusCode
    {
        get => this.response.StatusCode;
        set => this.response.StatusCode = value;
    }

    public string StatusDescription
    {
        get => this.response.StatusDescription;
        set => this.response.StatusDescription = value;
    }

    public string ContentType
    {
        get => response.ContentType;
        set => response.ContentType = value;
    }

    public void AddHeader(string name, string value)
    {
        response.AddHeader(name, value);
    }

    public void RemoveHeader(string name)
    {
        response.Headers.Remove(name);
    }

    public string GetHeader(string name)
    {
        return response.Headers[name];
    }

    public void Redirect(string url)
    {
        response.Redirect(url);
    }

    public MemoryStream BufferedStream { get; set; }
    public Stream OutputStream => BufferedStream ?? response.OutputStream;

    public bool UseBufferedStream
    {
        get => BufferedStream != null;
        set
        {
            if (true)
                this.response.BufferOutput = false;

            BufferedStream = value
                ? BufferedStream ?? this.CreateBufferedStream()
                : null;
        }
    }

    public object Dto { get; set; }

    public void Close()
    {
        if (IsClosed) return;
        this.IsClosed = true;

        this.FlushBufferIfAny(BufferedStream, response.OutputStream);
        BufferedStream?.Dispose();
        BufferedStream = null;

        response.CloseOutputStream();
    }

    public Task CloseAsync(CancellationToken token = default(CancellationToken))
    {
        Close();
        return TypeConstants.EmptyTask;
    }

    public void End()
    {
        if (IsClosed) return;
        this.IsClosed = true;
        try
        {
            this.FlushBufferIfAny(BufferedStream, response.OutputStream);
            BufferedStream?.Dispose();
            BufferedStream = null;

            response.ClearContent();
            response.End();
        }
        catch { }
    }

    public void Flush()
    {
        this.FlushBufferIfAny(BufferedStream, response.OutputStream);

        response.Flush();
    }

    public async Task FlushAsync(CancellationToken token = default(CancellationToken))
    {
        await this.FlushBufferIfAnyAsync(BufferedStream, response.OutputStream, token).ConfigAwait();
        await response.OutputStream.FlushAsync(token).ConfigAwait();
    }

    public bool IsClosed
    {
        get;
        private set;
    }

    public void SetContentLength(long contentLength)
    {
        try
        {
            if (contentLength >= 0)
                response.Headers["Content-Length"] = contentLength.ToString(CultureInfo.InvariantCulture);
        }
        catch (PlatformNotSupportedException /*ignore*/) { } //This operation requires IIS integrated pipeline mode.
    }

    //Benign, see how to enable in ASP.NET: http://technet.microsoft.com/en-us/library/cc772183(v=ws.10).aspx
    public bool KeepAlive { get; set; }

    /// <summary>
    /// Can ignore as doesn't throw if HTTP Headers already written
    /// </summary>
    public bool HasStarted => false;

    public Dictionary<string, object> Items { get; }

    public ICookies Cookies { get; set; }

    public void SetCookie(Cookie cookie)
    {
        if (!HostContext.AppHost.SetCookieFilter(Request, cookie))
            return;

        var httpCookie = cookie.ToHttpCookie();
        response.SetCookie(httpCookie);            
    }

    public void ClearCookies()
    {
        response.Cookies.Clear();
    }

    public Dictionary<string, string> Headers => response.Headers.ToDictionary();
}

#endif
