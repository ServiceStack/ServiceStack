#if !SILVERLIGHT && !XBOX
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;

namespace ServiceStack.ServiceHost
{
    /// <summary>
    /// A thin wrapper around ASP.NET or HttpListener's HttpRequest
    /// </summary>
    public interface IHttpRequest : IResolver
    {
        /// <summary>
        /// The underlying ASP.NET or HttpListener HttpRequest
        /// </summary>
        object OriginalRequest { get; }

        /// <summary>
        /// The name of the service being called (e.g. Request DTO Name)
        /// </summary>
        string OperationName { get; }

        /// <summary>
        /// The request ContentType
        /// </summary>
        string ContentType { get; }

        string HttpMethod { get; }
        
        bool IsLocal { get; }

        string UserAgent { get; }

        IDictionary<string, System.Net.Cookie> Cookies { get; }

        /// <summary>
        /// The expected Response ContentType for this request
        /// </summary>
        string ResponseContentType { get; set; }

        /// <summary>
        /// Attach any data to this request that all filters and services can access.
        /// </summary>
        Dictionary<string, object> Items { get; }

        NameValueCollection Headers { get; }

        NameValueCollection QueryString { get; }

        NameValueCollection FormData { get; }

        /// <summary>
        /// Buffer the Request InputStream so it can be re-read
        /// </summary>
        bool UseBufferedStream { get; set; }

        /// <summary>
        /// The entire string contents of Request.InputStream
        /// </summary>
        /// <returns></returns>
        string GetRawBody();

        string RawUrl { get; }

        string AbsoluteUri { get; }

        /// <summary>
        /// The Remote Ip as reported by Request.UserHostAddress
        /// </summary>
        string UserHostAddress { get; }

        /// <summary>
        /// The Remote Ip as reported by X-Forwarded-For, X-Real-IP or Request.UserHostAddress
        /// </summary>
        string RemoteIp { get; }

        /// <summary>
        /// The value of the X-Forwarded-For header, null if null or empty
        /// </summary>
        string XForwardedFor { get; }

        /// <summary>
        /// The value of the X-Real-IP header, null if null or empty
        /// </summary>
        string XRealIp { get; }

        /// <summary>
        /// e.g. is https or not
        /// </summary>
        bool IsSecureConnection { get; }

        string[] AcceptTypes { get; }

        string PathInfo { get; }

        Stream InputStream { get; }

        long ContentLength { get; }

        /// <summary>
        /// Access to the multi-part/formdata files posted on this request
        /// </summary>
        IFile[] Files { get; }

        string ApplicationFilePath { get; }
    }
}
#endif
