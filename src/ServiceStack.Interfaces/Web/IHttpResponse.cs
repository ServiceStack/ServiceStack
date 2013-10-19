//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System.Net;

namespace ServiceStack.Web
{
    /// <summary>
    /// A thin wrapper around ASP.NET or HttpListener's HttpResponse
    /// </summary>
    public interface IHttpResponse : IResponse
    {
        ICookies Cookies { get; }

        void SetCookie(Cookie cookie);
    }
}