using System;
using System.Web;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public class CustomResponseHandler : IServiceStackHttpHandler, IHttpHandler
    {
        public string OperationName { get; set; }

        public Func<IHttpRequest, IHttpResponse, object> Action { get; set; }

        public CustomResponseHandler(Func<IHttpRequest, IHttpResponse, object> action, string operationName = null)
        {
            Action = action;
            OperationName = operationName ?? "CustomResponse";
        }

        public void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
        {
            if (Action == null)
                throw new Exception("Action was not supplied to ActionHandler");

            if (httpReq.OperationName == null)
                httpReq.SetOperationName(OperationName);

            var response = Action(httpReq, httpRes);
            httpRes.WriteToResponse(httpReq, response);
        }

        public void ProcessRequest(HttpContext context)
        {
            ProcessRequest(context.Request.ToRequest(OperationName),
                context.Response.ToResponse(),
                OperationName);
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}