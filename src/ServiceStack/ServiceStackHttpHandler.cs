using ServiceStack.Host.Handlers;
using ServiceStack.Web;

namespace ServiceStack
{
    public class ServiceStackHttpHandler : HttpAsyncTaskHandler
    {
        readonly IServiceStackHandler servicestackHandler;

        public ServiceStackHttpHandler(IServiceStackHandler servicestackHandler)
        {
            this.servicestackHandler = servicestackHandler;
        }

        public override void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName)
        {
            servicestackHandler.ProcessRequest(httpReq, httpRes, operationName ?? httpReq.OperationName);
        }
    }
}