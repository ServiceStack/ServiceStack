//Copyright (c) ServiceStack, Inc. All Rights Reserved.
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

        /// <summary>
        /// Adds a new Set-Cookie instruction to Response
        /// </summary>
        /// <param name="cookie"></param>
        void SetCookie(Cookie cookie);

        /// <summary>
        /// Removes all pending Set-Cookie instructions 
        /// </summary>
        void ClearCookies();
    }
}