using System;
using System.Collections.Generic;
using System.Web;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using HttpResponseExtensions = ServiceStack.WebHost.Endpoints.Extensions.HttpResponseExtensions;

namespace ServiceStack.WebHost.Endpoints.Support
{
    public class ForbiddenHttpHandler
        : IServiceStackHttpHandler, IHttpHandler
    {
		public bool? IsIntegratedPipeline { get; set; }
		public string WebHostPhysicalPath { get; set; }
		public List<string> WebHostRootFileNames { get; set; }
		public string ApplicationBaseUrl { get; set; }
		public string DefaultRootFileName { get; set; }
		public string DefaultHandler { get; set; }

		public void ProcessRequest(IHttpRequest request, IHttpResponse response, string operationName)
        {
            response.ContentType = "text/plain";
            response.StatusCode = 403;
            response.Write("Forbidden\n\n");

            response.Write("\nRequest.HttpMethod: " + request.HttpMethod);
            response.Write("\nRequest.PathInfo: " + request.PathInfo);
            response.Write("\nRequest.QueryString: " + request.QueryString);
            response.Write("\nRequest.RawUrl: " + request.RawUrl);

			if (IsIntegratedPipeline.HasValue)
				response.Write("\nApp.IsIntegratedPipeline: " + IsIntegratedPipeline);
			if (!WebHostPhysicalPath.IsNullOrEmpty())
				response.Write("\nApp.WebHostPhysicalPath: " + WebHostPhysicalPath);
			if (!WebHostRootFileNames.IsEmpty())
				response.Write("\nApp.WebHostRootFileNames: " + TypeSerializer.SerializeToString(WebHostRootFileNames));
			if (!ApplicationBaseUrl.IsNullOrEmpty())
				response.Write("\nApp.ApplicationBaseUrl: " + ApplicationBaseUrl);
			if (!DefaultRootFileName.IsNullOrEmpty())
				response.Write("\nApp.DefaultRootFileName: " + DefaultRootFileName);
			if (!DefaultHandler.IsNullOrEmpty())
				response.Write("\nApp.DefaultHandler: " + DefaultHandler);
			if (!ServiceStackHttpHandlerFactory.DebugLastHandlerArgs.IsNullOrEmpty())
				response.Write("\nApp.DebugLastHandlerArgs: " + ServiceStackHttpHandlerFactory.DebugLastHandlerArgs);

			response.ApplyGlobalResponseHeaders();
			//Apache+mod_mono doesn't like this
			//response.OutputStream.Flush();
			//response.Close();
		}

        public void ProcessRequest(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;

            response.ContentType = "text/plain";
            response.StatusCode = 403;
            response.Write("Forbidden\n\n");

            response.Write("\nRequest.HttpMethod: " + request.HttpMethod);
            response.Write("\nRequest.PathInfo: " + request.PathInfo);
            response.Write("\nRequest.QueryString: " + request.QueryString);
            response.Write("\nRequest.RawUrl: " + request.RawUrl);

			if (IsIntegratedPipeline.HasValue)
				response.Write("\nApp.IsIntegratedPipeline: " + IsIntegratedPipeline);
			if (!WebHostPhysicalPath.IsNullOrEmpty())
				response.Write("\nApp.WebHostPhysicalPath: " + WebHostPhysicalPath);
			if (!WebHostRootFileNames.IsEmpty())
				response.Write("\nApp.WebHostRootFileNames: " + TypeSerializer.SerializeToString(WebHostRootFileNames));
			if (!ApplicationBaseUrl.IsNullOrEmpty())
				response.Write("\nApp.ApplicationBaseUrl: " + ApplicationBaseUrl);
			if (!DefaultRootFileName.IsNullOrEmpty())
				response.Write("\nApp.DefaultRootFileName: " + DefaultRootFileName);

            response.ApplyGlobalResponseHeaders();
			//Apache+mod_mono doesn't like this
			//response.OutputStream.Flush();
			//response.Close();
		}

        public bool IsReusable
        {
            get { return true; }
        }
    }
}