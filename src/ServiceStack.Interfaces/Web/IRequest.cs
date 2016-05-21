//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Configuration;

namespace ServiceStack.Web
{
    /// <summary>
    /// A thin wrapper around each host's Request e.g: ASP.NET, HttpListener, MQ, etc
    /// </summary>
    public interface IRequest : IResolver
    {
        /// <summary>
        /// The underlying ASP.NET or HttpListener HttpRequest
        /// </summary>
        object OriginalRequest { get; }

        IResponse Response { get; }

        /// <summary>
        /// The name of the service being called (e.g. Request DTO Name)
        /// </summary>
        string OperationName { get; set; }

        /// <summary>
        /// The Verb / HttpMethod or Action for this request
        /// </summary>
        string Verb { get; }

        RequestAttributes RequestAttributes { get; set; }

        /// <summary>
        /// Optional preferences for the processing of this Request
        /// </summary>
        IRequestPreferences RequestPreferences { get; }

        /// <summary>
        /// The Request DTO, after it has been deserialized.
        /// </summary>
        object Dto { get; set; }

        /// <summary>
        /// The request ContentType
        /// </summary>
        string ContentType { get; }

        bool IsLocal { get; }

        string UserAgent { get; }

        IDictionary<string, System.Net.Cookie> Cookies { get; }

        /// <summary>
        /// The expected Response ContentType for this request
        /// </summary>
        string ResponseContentType { get; set; }

        /// <summary>
        /// Whether the ResponseContentType has been explicitly overrided or whether it was just the default
        /// </summary>
        bool HasExplicitResponseContentType { get; }

        /// <summary>
        /// Attach any data to this request that all filters and services can access.
        /// </summary>
        Dictionary<string, object> Items { get; }

        INameValueCollection Headers { get; }

        INameValueCollection QueryString { get; }

        INameValueCollection FormData { get; }
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
        /// The value of the Authorization Header used to send the Api Key, null if not available
        /// </summary>
        string Authorization { get; }

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
        IHttpFile[] Files { get; }

        /// <summary>
        /// The value of the Referrer, null if not available
        /// </summary>
        Uri UrlReferrer { get; }
    }
}
