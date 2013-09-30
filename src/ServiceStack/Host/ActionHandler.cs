using System;
using System.Web;
using ServiceStack.Host.Handlers;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public class ActionHandler : IServiceStackHttpHandler, IHttpHandler 
    {
        public string OperationName { get; set; }

        public Func<IHttpRequest, IHttpResponse, object> Action { get; set; }

        public ActionHandler(Func<IHttpRequest, IHttpResponse, object> action, string operationName=null)
        {
            Action = action;
            OperationName = operationName;
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