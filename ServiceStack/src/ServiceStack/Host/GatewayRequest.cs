#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using ServiceStack.Web;

namespace ServiceStack.Host;

public class GatewayRequest : BasicRequest, IHttpRequest, IConvertRequest, ICloneable
{
    public GatewayRequest() : this(null) {}

    public GatewayRequest(object? requestDto,
        RequestAttributes requestAttributes =
            RequestAttributes.None | RequestAttributes.LocalSubnet | RequestAttributes.Http)
        : base(requestDto, requestAttributes)
    {
        Response = HttpResponse = new BasicHttpResponse(this);
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

    private object? dto;
    public override object? Dto
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

    public void Clear()
    {
        FormData.Clear();
        QueryString.Clear();
        Items.Clear();
    }
    
    public static GatewayRequest Create(IRequest req)
    {
        if (req is GatewayRequest gatewayRequest)
            return gatewayRequest;
        
        var ret = FromRequest(req);
        ret.SetInProcessRequest();
        return ret;
    }

    static GatewayRequest FromRequest(IRequest req)
    {
        var httpReq = (IHttpRequest)req;
        var ret = new GatewayRequest
        {
            PathInfo = httpReq.PathInfo,
            OriginalRequest = httpReq.OriginalRequest,
            QueryString = new(),
            FormData = new(),
            Headers = httpReq.Headers.Clone(),
            Cookies = new Dictionary<string, Cookie>(httpReq.Cookies),
            Items = new(httpReq.Items),
            RawUrl = httpReq.RawUrl,
            Verb = httpReq.Verb,
            UserAgent = httpReq.UserAgent,
            RemoteIp = httpReq.RemoteIp,
            UserHostAddress = httpReq.UserHostAddress,
            AcceptTypes = httpReq.AcceptTypes,
            IsSecureConnection = true,
            HttpMethod = httpReq?.HttpMethod ?? httpReq.Verb,
            XForwardedFor = httpReq?.XForwardedFor,
            XForwardedPort = httpReq?.XForwardedPort,
            XForwardedProtocol = httpReq?.XForwardedProtocol,
            XRealIp = httpReq?.XRealIp,
            Accept = httpReq?.Accept,
        };
        return ret;
    }

    // Need to create copy of Request DTO in InProc gateway otherwise client mutations can impact service impls
    public T Convert<T>(T value) => value.CreateCopy();

    public object Clone()
    {
        return FromRequest(this.OriginalRequest as IHttpRequest ?? this);
    }
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