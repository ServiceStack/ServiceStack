//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Web
{
    /// <summary>
    /// A thin wrapper around each host's Response e.g: ASP.NET, HttpListener, MQ, etc
    /// </summary>
    public interface IResponse
    {
        /// <summary>
        /// The underlying ASP.NET, .NET Core or HttpListener HttpResponse
        /// </summary>
        object OriginalResponse { get; }

        /// <summary>
        /// The corresponding IRequest API for this Response
        /// </summary>
        IRequest Request { get; }

        /// <summary>
        /// The Response Status Code
        /// </summary>
        int StatusCode { get; set; }

        /// <summary>
        /// The Response Status Description
        /// </summary>
        string StatusDescription { get; set; }

        /// <summary>
        /// The Content-Type for this Response
        /// </summary>
        string ContentType { get; set; }

        /// <summary>
        /// Add a Header to this Response
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        void AddHeader(string name, string value);

        /// <summary>
        /// Remove an existing Header added on this Response
        /// </summary>
        /// <param name="name"></param>
        void RemoveHeader(string name);

        /// <summary>
        /// Get an existing Header added to this Response
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        string GetHeader(string name);

        /// <summary>
        /// Return a Redirect Response to the URL specified
        /// </summary>
        /// <param name="url"></param>
        void Redirect(string url);

        /// <summary>
        /// The Response Body Output Stream
        /// </summary>
        Stream OutputStream { get; }

        /// <summary>
        /// The Response DTO
        /// </summary>
        object Dto { get; set; }

        /// <summary>
        /// Buffer the Response OutputStream so it can be written in 1 batch
        /// </summary>
        bool UseBufferedStream { get; set; }

        /// <summary>
        /// Signal that this response has been handled and no more processing should be done.
        /// When used in a request or response filter, no more filters or processing is done on this request.
        /// </summary>
        void Close();

        /// <summary>
        /// Calls Response.End() on ASP.NET HttpResponse otherwise is an alias for Close().
        /// Useful when you want to prevent ASP.NET to provide it's own custom error page.
        /// </summary>
        void End();

        /// <summary>
        /// Response.Flush() and OutputStream.Flush() seem to have different behaviour in ASP.NET
        /// </summary>
        void Flush();

        /// <summary>
        /// Flush this Response Output Stream Async
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task FlushAsync(CancellationToken token = default(CancellationToken));

        /// <summary>
        /// Gets a value indicating whether this instance is closed.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Set the Content Length in Bytes for this Response
        /// </summary>
        /// <param name="contentLength"></param>
        void SetContentLength(long contentLength);

        /// <summary>
        /// Whether the underlying TCP Connection for this Response should remain open
        /// </summary>
        bool KeepAlive { get; set; }

        /// <summary>
        /// Whether the HTTP Response Headers have already been written.
        /// </summary>
        bool HasStarted { get; }

        //Add Metadata to Response
        Dictionary<string, object> Items { get; }
    }
}