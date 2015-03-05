//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System.Collections.Generic;
using System.IO;

namespace ServiceStack.Web
{
    /// <summary>
    /// A thin wrapper around each host's Response e.g: ASP.NET, HttpListener, MQ, etc
    /// </summary>
    public interface IResponse
    {
        /// <summary>
        /// The underlying ASP.NET or HttpListener HttpResponse
        /// </summary>
        object OriginalResponse { get; }

        IRequest Request { get; }

        int StatusCode { get; set; }

        string StatusDescription { get; set; }

        string ContentType { get; set; }

        void AddHeader(string name, string value);

        void Redirect(string url);

        Stream OutputStream { get; }

        /// <summary>
        /// The Response DTO
        /// </summary>
        object Dto { get; set; }

        /// <summary>
        /// Write once to the Response Stream then close it. 
        /// </summary>
        /// <param name="text"></param>
        void Write(string text);

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
        /// Gets a value indicating whether this instance is closed.
        /// </summary>
        bool IsClosed { get; }

        void SetContentLength(long contentLength);

        bool KeepAlive { get; set; }

        //Add Metadata to Response
        Dictionary<string, object> Items { get; }
    }
}