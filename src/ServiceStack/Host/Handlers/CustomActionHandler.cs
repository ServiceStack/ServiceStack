﻿using System;
using System.Web;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public class CustomActionHandler : HttpAsyncTaskHandler
    {
        public Action<IRequest, IResponse> Action { get; set; }

        public CustomActionHandler(Action<IRequest, IResponse> action)
        {
            if (action == null)
                throw new NullReferenceException("action");

            Action = action;
            this.RequestName = GetType().Name;
        }

        public override void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName)
        {
            if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
                return;

            Action(httpReq, httpRes);
            httpRes.EndHttpHandlerRequest(skipHeaders:true);
        }

#if !NETSTANDARD1_6
        public override void ProcessRequest(HttpContextBase context)
        {
            var httpReq = context.ToRequest("CustomAction");
            ProcessRequest(httpReq, httpReq.Response, "CustomAction");
        }
#endif

    }
}