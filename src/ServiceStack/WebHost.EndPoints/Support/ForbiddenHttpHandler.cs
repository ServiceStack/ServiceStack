using System;
using System.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Support
{
    public class ForbiddenHttpHandler
        : IServiceStackHttpHandler, IHttpHandler
    {
        public void ProcessRequest(IHttpRequest request, IHttpResponse response, string operationName)
        {
            response.ContentType = "text/plain";
            response.StatusCode = 403;
            response.Write("Forbidden\n\n");

            response.Write("\nRequest.HttpMethod: " + request.HttpMethod);
            response.Write("\nRequest.PathInfo: " + request.PathInfo);
            response.Write("\nRequest.QueryString: " + request.QueryString);
            response.Write("\nRequest.RawUrl: " + request.RawUrl);
        }

        public void ProcessRequest(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;

            response.ContentType = "text/plain";
            response.StatusCode = 403;
            response.Write("Forbidden\n\n");

            response.Write("\nRequest.HttpMethod: " + request.HttpMethod);
            response.Write("\nRequest.PathInfo: " + request.PathInfo);
            response.Write("\nRequest.QueryString: " + request.QueryString);
            response.Write("\nRequest.RawUrl: " + request.RawUrl);
        }

        public bool IsReusable
        {
            get { return true; }
        }
    }
}