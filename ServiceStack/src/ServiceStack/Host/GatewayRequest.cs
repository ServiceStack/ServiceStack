#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using ServiceStack.Web;

namespace ServiceStack.Host;

public class GatewayRequest : BasicRequest, IHttpRequest, IConvertRequest
{
    public GatewayRequest() : this(null) {}

    public GatewayRequest(object? requestDto,
        RequestAttributes requestAttributes =
            RequestAttributes.None | RequestAttributes.LocalSubnet | RequestAttributes.Http)
        : base(requestDto, requestAttributes)
    {
        Response = new BasicHttpResponse(this);
    }
    
    public IHttpResponse HttpResponse { get; set; }

    public string HttpMethod
    {
        get => Verb;
        set => Verb = value;
    }
    
    public string? XForwardedFor { get; set; }
    public int? XForwardedPort { get; set; }
    public string? XForwardedProtocol { get; set; }
    public string? XRealIp { get; set; }
    public string? Accept { get; set; }

    private object dto;
    public override object Dto
    {
        get => dto;
        set
        {
            dto = value;
            if (dto == null) return;
            OperationName = dto.GetType().Name;
            PathInfo = "/api/" + OperationName;
            RawUrl = AbsoluteUri = "gateway:/" + PathInfo;
            HttpMethod = ServiceClientUtils.GetHttpMethod(dto.GetType()) ?? Verb;
        }
    }

    public static GatewayRequest Create(IRequest hostReq)
    {
        if (hostReq is GatewayRequest gatewayRequest)
            return gatewayRequest;
        var httpReq = hostReq as IHttpRequest;
        var ret = new GatewayRequest
        {
            PathInfo = hostReq.PathInfo,
            OriginalRequest = hostReq.OriginalRequest,
            QueryString = new(),
            FormData = new(),
            Headers = hostReq.Headers.Clone(),
            Cookies = new Dictionary<string, Cookie>(hostReq.Cookies),
            Items = new(hostReq.Items),
            RawUrl = hostReq.RawUrl,
            Verb = hostReq.Verb,
            UserAgent = hostReq.UserAgent,
            RemoteIp = hostReq.RemoteIp,
            UserHostAddress = hostReq.UserHostAddress,
            AcceptTypes = hostReq.AcceptTypes,
            IsSecureConnection = true,
            HttpMethod = httpReq?.HttpMethod ?? hostReq.Verb,
            XForwardedFor = httpReq?.XForwardedFor,
            XForwardedPort = httpReq?.XForwardedPort,
            XForwardedProtocol = httpReq?.XForwardedProtocol,
            XRealIp = httpReq?.XRealIp,
            Accept = httpReq?.Accept,
        };
        ret.SetInProcessRequest();
        return ret;
    }

    // Need to create copy of Request DTO in InProc gateway otherwise client mutations can impact service impls
    public T Convert<T>(T value) => value.CreateCopy();
}

public class GatewayResponse : BasicResponse, IHttpResponse
{
    public GatewayResponse(BasicRequest requestContext) : base(requestContext)
    {
        Cookies = new Cookies(this);
    }

    public List<Cookie> CookieCollection { get; } = new();


    public ICookies Cookies { get; }
    public void SetCookie(Cookie cookie) 
    {
        if (!HostContext.AppHost.SetCookieFilter(Request, cookie))
            return;

        CookieCollection.RemoveAll(x => x.Name == cookie.Name);
        CookieCollection.Add(cookie);
    }

    public void ClearCookies() {}
}