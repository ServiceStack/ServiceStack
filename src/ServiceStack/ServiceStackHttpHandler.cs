using System.Web;
using ServiceStack.Server;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack
{
    public class ServiceStackHttpHandler : IHttpHandler, IServiceStackHttpHandler
    {
        IServiceStackHttpHandler servicestackHandler;

        public ServiceStackHttpHandler(IServiceStackHttpHandler servicestackHandler)
        {
            this.servicestackHandler = servicestackHandler;
        }

        public virtual void ProcessRequest(HttpContext context)
        {
            ProcessRequest(
                context.Request.ToRequest(), 
                context.Response.ToResponse(),
                null);
        }

        public bool IsReusable
        {
            get { return false; }
        }

        public virtual void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
        {
            servicestackHandler.ProcessRequest(httpReq, httpRes, operationName ?? httpReq.OperationName);
        }
    }
}