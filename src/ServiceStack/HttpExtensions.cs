using System.Net;
using System.Web;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack
{
    public static class HttpExtensions
    {
        public static HttpRequestContext ToRequestContext(this HttpContext httpContext, object requestDto = null)
        {
            return new HttpRequestContext(
                httpContext.Request.ToRequest(),
                httpContext.Response.ToResponse(),
                requestDto);
        }

        public static HttpRequestContext ToRequestContext(this HttpListenerContext httpContext, object requestDto = null)
        {
            return new HttpRequestContext(
                httpContext.Request.ToRequest(),
                httpContext.Response.ToResponse(),
                requestDto);
        }
    }
}