using ServiceStack.Service;

namespace ServiceStack.ServiceHost
{
    public interface IPartialContentResult : IHttpResult, IStreamWriter
    {
        /* Start and End will be set in IHttpResponseExtensions.WriteToResponse() and used within Write() method if IHttpResult */
        long Start { get; set; }
        long End { get; set; }
        long GetContentLength();
    }
}