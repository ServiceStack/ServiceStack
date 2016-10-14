﻿using System.Collections.Generic;
using System.Web;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public class ForbiddenHttpHandler : HttpAsyncTaskHandler
    {
        public ForbiddenHttpHandler()
        {
            this.RequestName = GetType().Name;
        }

        public bool? IsIntegratedPipeline { get; set; }
        public string WebHostPhysicalPath { get; set; }
        public List<string> WebHostRootFileNames { get; set; }
        public string WebHostUrl { get; set; }
        public string DefaultRootFileName { get; set; }
        public string DefaultHandler { get; set; }

        public override void ProcessRequest(IRequest request, IResponse response, string operationName)
        {
            response.ContentType = "text/plain";
            response.StatusCode = 403;

            response.EndHttpHandlerRequest(skipClose: true, afterHeaders: r =>
            {
                r.Write("Forbidden\n\n");

                r.Write("\nRequest.HttpMethod: " + request.Verb);
                r.Write("\nRequest.PathInfo: " + request.PathInfo);
                r.Write("\nRequest.QueryString: " + request.QueryString);

                if (HostContext.Config.DebugMode)
                {
                    r.Write("\nRequest.RawUrl: " + request.RawUrl);

                    if (IsIntegratedPipeline.HasValue)
                        r.Write("\nApp.IsIntegratedPipeline: " + IsIntegratedPipeline);
                    if (!WebHostPhysicalPath.IsNullOrEmpty())
                        r.Write("\nApp.WebHostPhysicalPath: " + WebHostPhysicalPath);
                    if (!WebHostRootFileNames.IsEmpty())
                        r.Write("\nApp.WebHostRootFileNames: " + TypeSerializer.SerializeToString(WebHostRootFileNames));
                    if (!WebHostUrl.IsNullOrEmpty())
                        r.Write("\nApp.WebHostUrl: " + WebHostUrl);
                    if (!DefaultRootFileName.IsNullOrEmpty())
                        r.Write("\nApp.DefaultRootFileName: " + DefaultRootFileName);
                    if (!DefaultHandler.IsNullOrEmpty())
                        r.Write("\nApp.DefaultHandler: " + DefaultHandler);
                    if (!HttpHandlerFactory.DebugLastHandlerArgs.IsNullOrEmpty())
                        r.Write("\nApp.DebugLastHandlerArgs: " + HttpHandlerFactory.DebugLastHandlerArgs);
                }
            });
        }

#if !NETSTANDARD1_6
        public override void ProcessRequest(HttpContextBase context)
        {
            var request = context.Request;
            var response = context.Response;

            response.ContentType = "text/plain";
            response.StatusCode = 403;

            context.EndHttpHandlerRequest(skipClose: true, afterHeaders: r =>
            {
                r.Write("Forbidden\n\n");

                r.Write("\nRequest.HttpMethod: " + request.HttpMethod);
                r.Write("\nRequest.PathInfo: " + request.PathInfo);
                r.Write("\nRequest.QueryString: " + request.QueryString);

                if (HostContext.Config.DebugMode)
                {
                    r.Write("\nRequest.RawUrl: " + request.RawUrl);

                    if (IsIntegratedPipeline.HasValue)
                        r.Write("\nApp.IsIntegratedPipeline: " + IsIntegratedPipeline);
                    if (!WebHostPhysicalPath.IsNullOrEmpty())
                        r.Write("\nApp.WebHostPhysicalPath: " + WebHostPhysicalPath);
                    if (!WebHostRootFileNames.IsEmpty())
                        r.Write("\nApp.WebHostRootFileNames: " + TypeSerializer.SerializeToString(WebHostRootFileNames));
                    if (!WebHostUrl.IsNullOrEmpty())
                        r.Write("\nApp.ApplicationBaseUrl: " + WebHostUrl);
                    if (!DefaultRootFileName.IsNullOrEmpty())
                        r.Write("\nApp.DefaultRootFileName: " + DefaultRootFileName);
                }
            });
        }
#endif

        public override bool IsReusable => true;
    }
}