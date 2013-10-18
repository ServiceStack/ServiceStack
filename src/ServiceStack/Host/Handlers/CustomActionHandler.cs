using System;
using System.Web;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public class CustomActionHandler : HttpAsyncTaskHandler
    {
        public Action<IHttpRequest, IHttpResponse> Action { get; set; }

        public CustomActionHandler(Action<IHttpRequest, IHttpResponse> action)
        {
            if (action == null)
                throw new NullReferenceException("action");

            Action = action;
        }

        public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
        {
            Action(httpReq, httpRes);
        }

        public override void ProcessRequest(HttpContext context)
        {
            ProcessRequest(context.Request.ToRequest("CustomAction"), context.Response.ToResponse(), "CustomAction");
        }
    }
}