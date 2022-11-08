using System;
using System.Collections.Generic;
using System.Net;
using ServiceStack.Web;

namespace ServiceStack.Host;

public class BasicHttpRequest : BasicRequest, IHttpRequest
{
    public BasicHttpRequest() : this(null) {}

    public BasicHttpRequest(object requestDto,
        RequestAttributes requestAttributes =
            RequestAttributes.None | RequestAttributes.LocalSubnet | RequestAttributes.Http)
        : base(requestDto, requestAttributes)
    {
        Response = new BasicHttpResponse(this);
    }
    
    public IHttpResponse HttpResponse { get; set; }
    public string HttpMethod { get; set; }
    public string XForwardedFor { get; set; }
    public int? XForwardedPort { get; set; }
    public string XForwardedProtocol { get; set; }
    public string XRealIp { get; set; }
    public string Accept { get; set; }
}

public class BasicHttpResponse : BasicResponse, IHttpResponse
{
    public BasicHttpResponse(BasicRequest requestContext) : base(requestContext)
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