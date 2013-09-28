using System.Web;
using ServiceStack.Host.Handlers;
using ServiceStack.Support.WebHost;
using ServiceStack.Web;

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