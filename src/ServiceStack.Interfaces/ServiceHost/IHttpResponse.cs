using System.IO;

namespace ServiceStack.ServiceHost
{
    /// <summary>
    /// A thin wrapper around ASP.NET or HttpListener's HttpResponse
    /// </summary>
    public interface IHttpResponse
    {
        /// <summary>
        /// The underlying ASP.NET or HttpListener HttpResponse
        /// </summary>
        object OriginalResponse { get; }

        int StatusCode { get; set; }

        string StatusDescription { get; set; }

        string ContentType { get; set; }

        ICookies Cookies { get; }

        void AddHeader(string name, string value);

        void Redirect(string url);

        Stream OutputStream { get; }

        void Write(string text);

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
    }
}