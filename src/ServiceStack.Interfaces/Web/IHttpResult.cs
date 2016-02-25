//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Net;

namespace ServiceStack.Web
{
    public interface IHttpResult : IHasOptions
    {
        /// <summary>
        /// The HTTP Response Status
        /// </summary>
        int Status { get; set; }

        /// <summary>
        /// The HTTP Response Status Code
        /// </summary>
        HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// The HTTP Status Description
        /// </summary>
        string StatusDescription { get; set; }

        /// <summary>
        /// The HTTP Response ContentType
        /// </summary>
        string ContentType { get; set; }

        /// <summary>
        /// Additional HTTP Headers
        /// </summary>
        Dictionary<string, string> Headers { get; }

        /// <summary>
        /// Additional HTTP Cookies
        /// </summary>
        List<System.Net.Cookie> Cookies { get; }

        /// <summary>
        /// Response DTO
        /// </summary>
        object Response { get; set; }

        /// <summary>
        /// if not provided, get's injected by ServiceStack
        /// </summary>
        IContentTypeWriter ResponseFilter { get; set; }

        /// <summary>
        /// Holds the request call context
        /// </summary>
        IRequest RequestContext { get; set; }

        /// <summary>
        /// The padding length written with the body, to be added to ContentLength of body
        /// </summary>
        int PaddingLength { get; set; }

        /// <summary>
        /// Serialize the Response within the specified scope
        /// </summary>
        Func<IDisposable> ResultScope { get; set; }
    }
}