using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.Host.Handlers;

namespace ServiceStack;

/// <summary>
/// Transparently Proxy requests through to downstream HTTP Servers
/// </summary>
public class ProxyFeature : IPlugin, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.Proxy;
    private readonly Func<IHttpRequest, bool> matchingRequests;
    public readonly Func<IHttpRequest, string> ResolveUrl;

    /// <summary>
    /// Customize the HTTP Request Headers that are sent to downstream server
    /// </summary>
    public Action<IHttpRequest, HttpWebRequest> ProxyRequestFilter { get; set; }

    /// <summary>
    /// Customize the downstream HTTP Response Headers that are returned to client
    /// </summary>
    public Action<IHttpResponse, HttpWebResponse> ProxyResponseFilter { get; set; }

    /// <summary>
    /// Inspect or Transform the HTTP Request Body that's sent downstream
    /// </summary>
    public Func<IHttpRequest, Stream, Task<Stream>> TransformRequest { get; set; }

    /// <summary>
    /// Inspect or Transform the downstream HTTP Response Body that's returned
    /// </summary>
    public Func<IHttpResponse, Stream, Task<Stream>> TransformResponse { get; set; }

    public HashSet<string> IgnoreResponseHeaders = new(StringComparer.OrdinalIgnoreCase) {
        HttpHeaders.TransferEncoding
    };

    /// <summary>
    /// Required filters to specify which requests to proxy and which url to use.
    /// </summary>
    /// <param name="matchingRequests">Specify which requests should be proxied</param>
    /// <param name="resolveUrl">Specify which downstream url to use</param>
    public ProxyFeature(
        Func<IHttpRequest, bool> matchingRequests,
        Func<IHttpRequest, string> resolveUrl)
    {
        this.matchingRequests = matchingRequests ?? throw new ArgumentNullException(nameof(matchingRequests));
        this.ResolveUrl = resolveUrl ?? throw new ArgumentNullException(nameof(resolveUrl));
    }

    public void Register(IAppHost appHost)
    {
        appHost.Config.SkipFormDataInCreatingRequest = true;

        appHost.RawHttpHandlers.Add(req => matchingRequests(req)
            ? new ProxyFeatureHandler
            {
                ResolveUrl = ResolveUrl,
                ProxyRequestFilter = ProxyRequestFilter,
                ProxyResponseFilter = ProxyResponseFilter,
                TransformRequest = TransformRequest,
                TransformResponse = TransformResponse,
                IgnoreResponseHeaders = IgnoreResponseHeaders,
            }
            : null);
    }
}

public class ProxyFeatureHandler : HttpAsyncTaskHandler
{
    public override bool RunAsAsync() => true;

    public Func<IHttpRequest, string> ResolveUrl { get; set; }
    public Action<IHttpRequest, HttpWebRequest> ProxyRequestFilter { get; set; }
    public Action<IHttpResponse, HttpWebResponse> ProxyResponseFilter { get; set; }
    public Func<IHttpRequest, Stream, Task<Stream>> TransformRequest { get; set; }
    public Func<IHttpResponse, Stream, Task<Stream>> TransformResponse { get; set; }
    public HashSet<string> IgnoreResponseHeaders { get; set; } = new(StringComparer.OrdinalIgnoreCase) {
        HttpHeaders.TransferEncoding
    };

    public override Task ProcessRequestAsync(IRequest req, IResponse response, string operationName)
    {
        if (HostContext.ApplyCustomHandlerRequestFilters(req, response))
            return TypeConstants.EmptyTask;

        var httpReq = (IHttpRequest)req;
        var proxyUrl = ResolveUrl(httpReq);
        try
        {
            return ProxyRequestAsync(httpReq, proxyUrl);
        }
        catch (Exception ex)
        {
            return req.Response.WriteErrorBody(ex);
        }
    }

    public virtual async Task ProxyRequestAsync(IHttpRequest httpReq, string url) => 
#pragma warning disable CS0618, SYSLIB0014
        await ProxyRequestAsync(httpReq, WebRequest.CreateHttp(url));
#pragma warning restore CS0618, SYSLIB0014

    public async Task ProxyRequestAsync(IHttpRequest httpReq, HttpWebRequest webReq)
    {
        InitWebRequest(httpReq, webReq);

        ProxyRequestFilter?.Invoke(httpReq, webReq);

        if (httpReq.Response.IsClosed)
            return;

        if (httpReq.ContentLength > 0)
        {
            var inputStream = httpReq.InputStream;
            if (TransformRequest != null)
                inputStream = await TransformRequest(httpReq, inputStream) ?? inputStream;

            using (inputStream)
            using (var requestStream = await webReq.GetRequestStreamAsync())
            {
                await inputStream.WriteToAsync(requestStream);
            }
        }

        var res = (IHttpResponse) httpReq.Response;
        await ProxyToResponse(res, webReq);
    }

    public static void InitWebRequest(IHttpRequest httpReq, HttpWebRequest webReq)
    {
        webReq.Method = httpReq.Verb;
        webReq.ContentType = httpReq.ContentType;
        webReq.Accept = httpReq.Accept;

        PclExport.Instance.SetUserAgent(webReq, httpReq.UserAgent);

#if NETFRAMEWORK
            webReq.Referer = httpReq.UrlReferrer?.ToString();
            webReq.ServicePoint.Expect100Continue = false;

            var date = httpReq.GetDate();
            if (date != null)
                webReq.Date = date.Value;

            var ifModifiedSince = httpReq.GetIfModifiedSince();
            if (ifModifiedSince != null)
                webReq.IfModifiedSince = ifModifiedSince.Value;
#endif

        foreach (var header in httpReq.Headers.AllKeys)
        {
            if (header == null || HttpHeaders.RestrictedHeaders.Contains(header))
                continue;

            try
            {
                if (header.StartsWith(":"))
                {
                    if (Log.IsDebugEnabled)
                        Log.Debug($"Ignoring Invalid Proxy Request Header '{header}'");
                    continue;
                }
                    
                webReq.Headers[header] = httpReq.Headers[header];
            }
            catch (Exception e)
            {
                Log.Error($"Could not set Proxy Header: {header} = {httpReq.Headers[header]}\n{e.Message}", e);
            }
        }
    }

    public async Task ProxyToResponse(IHttpResponse res, HttpWebRequest webReq)
    {
        try
        {
            using var webRes = (HttpWebResponse) await webReq.GetResponseAsync();
            await CopyToResponse(res, webRes);
        }
        catch (WebException webEx)
        {
            using var errorResponse = (HttpWebResponse) webEx.Response;
            await CopyToResponse(res, errorResponse);
        }
    }

    public virtual async Task CopyToResponse(IHttpResponse res, HttpWebResponse webRes)
    {
        if (webRes == null)
            return;
            
        res.StatusCode = (int) webRes.StatusCode;
        res.StatusDescription = webRes.StatusDescription;
        res.ContentType = webRes.ContentType;
        if (webRes.ContentLength >= 0)
        {
            res.SetContentLength(webRes.ContentLength);
        }

        foreach (var header in webRes.Headers.AllKeys)
        {
            if (IgnoreResponseHeaders.Contains(header))
                continue;

            var value = webRes.Headers[header];
            res.AddHeader(header, value);
        }

        ProxyResponseFilter?.Invoke(res, webRes);

        var responseStream = webRes.GetResponseStream();
        if (responseStream != null)
        {
            if (TransformResponse != null)
                responseStream = await TransformResponse(res, responseStream) ?? responseStream;
    
            using (responseStream)
            {
                await responseStream.WriteToAsync(res.OutputStream);
            }
        }

        res.EndHttpHandlerRequest(skipHeaders: true);
    }
}