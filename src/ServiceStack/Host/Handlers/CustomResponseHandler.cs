using System;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public class CustomResponseHandler : HttpAsyncTaskHandler
    {
        public Func<IHttpRequest, IHttpResponse, object> Action { get; set; }

        public CustomResponseHandler(Func<IHttpRequest, IHttpResponse, object> action, string operationName = null)
        {
            Action = action;
            RequestName = operationName ?? "CustomResponse";
        }

        public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
        {
            if (Action == null)
                throw new Exception("Action was not supplied to ActionHandler");

            if (httpReq.OperationName == null)
                httpReq.SetOperationName(RequestName);

            var response = Action(httpReq, httpRes);
            httpRes.WriteToResponse(httpReq, response);
        }
    }
}