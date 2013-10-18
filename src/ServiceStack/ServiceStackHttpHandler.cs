using System.Web;
using ServiceStack.Host.Handlers;
using ServiceStack.Web;

namespace ServiceStack
{
    public class ServiceStackHttpHandler : HttpAsyncTaskHandler
    {
        readonly IServiceStackHttpHandler servicestackHandler;

        public ServiceStackHttpHandler(IServiceStackHttpHandler servicestackHandler)
        {
            this.servicestackHandler = servicestackHandler;
        }

        public override void ProcessRequest(HttpContext context)
        {
            ProcessRequest(
                context.Request.ToRequest(),
                context.Response.ToResponse(),
                null);
        }

        public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
        {
            servicestackHandler.ProcessRequest(httpReq, httpRes, operationName ?? httpReq.OperationName);
        }
    }
}