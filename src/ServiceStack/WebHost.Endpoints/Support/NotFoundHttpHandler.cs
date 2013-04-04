using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.Logging;
using HttpRequestWrapper = ServiceStack.WebHost.Endpoints.Extensions.HttpRequestWrapper;
using HttpResponseWrapper = ServiceStack.WebHost.Endpoints.Extensions.HttpResponseWrapper;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public class NotFoundHttpHandler
		: IServiceStackHttpHandler, IHttpHandler
	{
        private static readonly ILog Log = LogManager.GetLogger(typeof(NotFoundHttpHandler));

		public bool? IsIntegratedPipeline { get; set; }
		public string WebHostPhysicalPath { get; set; }
		public List<string> WebHostRootFileNames { get; set; }
		public string ApplicationBaseUrl { get; set; }
		public string DefaultRootFileName { get; set; }
		public string DefaultHandler { get; set; }

		public void ProcessRequest(IHttpRequest request, IHttpResponse response, string operationName)
		{
            Log.ErrorFormat("{0} Request not found: {1}", request.UserHostAddress, request.RawUrl);

		    var text = new StringBuilder();

            if (EndpointHost.DebugMode)
            {
                text.AppendLine("Handler for Request not found: \n\n")
                    .AppendLine("Request.HttpMethod: " + request.HttpMethod)
                    .AppendLine("Request.HttpMethod: " + request.HttpMethod)
                    .AppendLine("Request.PathInfo: " + request.PathInfo)
                    .AppendLine("Request.QueryString: " + request.QueryString)
                    .AppendLine("Request.RawUrl: " + request.RawUrl);
            }
            else
            {
                text.Append("404");
            }

		    response.ContentType = "text/plain";
			response.StatusCode = 404;
            response.EndHttpRequest(skipClose: true, afterBody: r => r.Write(text.ToString()));
		}

		public void ProcessRequest(HttpContext context)
		{
			var request = context.Request;
			var response = context.Response;

			var httpReq = new HttpRequestWrapper("NotFoundHttpHandler", request);
			if (!request.IsLocal)
			{
				ProcessRequest(httpReq, new HttpResponseWrapper(response), null);
				return;
			}

            Log.ErrorFormat("{0} Request not found: {1}", request.UserHostAddress, request.RawUrl);

			var sb = new StringBuilder();
			sb.AppendLine("Handler for Request not found: \n\n");

			sb.AppendLine("Request.ApplicationPath: " + request.ApplicationPath);
			sb.AppendLine("Request.CurrentExecutionFilePath: " + request.CurrentExecutionFilePath);
			sb.AppendLine("Request.FilePath: " + request.FilePath);
			sb.AppendLine("Request.HttpMethod: " + request.HttpMethod);
			sb.AppendLine("Request.MapPath('~'): " + request.MapPath("~"));
			sb.AppendLine("Request.Path: " + request.Path);
			sb.AppendLine("Request.PathInfo: " + request.PathInfo);
			sb.AppendLine("Request.ResolvedPathInfo: " + httpReq.PathInfo);
			sb.AppendLine("Request.PhysicalPath: " + request.PhysicalPath);
			sb.AppendLine("Request.PhysicalApplicationPath: " + request.PhysicalApplicationPath);
			sb.AppendLine("Request.QueryString: " + request.QueryString);
			sb.AppendLine("Request.RawUrl: " + request.RawUrl);
			try
			{
				sb.AppendLine("Request.Url.AbsoluteUri: " + request.Url.AbsoluteUri);
				sb.AppendLine("Request.Url.AbsolutePath: " + request.Url.AbsolutePath);
				sb.AppendLine("Request.Url.Fragment: " + request.Url.Fragment);
				sb.AppendLine("Request.Url.Host: " + request.Url.Host);
				sb.AppendLine("Request.Url.LocalPath: " + request.Url.LocalPath);
				sb.AppendLine("Request.Url.Port: " + request.Url.Port);
				sb.AppendLine("Request.Url.Query: " + request.Url.Query);
				sb.AppendLine("Request.Url.Scheme: " + request.Url.Scheme);
				sb.AppendLine("Request.Url.Segments: " + request.Url.Segments);
			}
			catch (Exception ex)
			{
				sb.AppendLine("Request.Url ERROR: " + ex.Message);
			}
			if (IsIntegratedPipeline.HasValue)
				sb.AppendLine("App.IsIntegratedPipeline: " + IsIntegratedPipeline);
			if (!WebHostPhysicalPath.IsNullOrEmpty())
				sb.AppendLine("App.WebHostPhysicalPath: " + WebHostPhysicalPath);
			if (!WebHostRootFileNames.IsEmpty())
				sb.AppendLine("App.WebHostRootFileNames: " + TypeSerializer.SerializeToString(WebHostRootFileNames));
			if (!ApplicationBaseUrl.IsNullOrEmpty())
				sb.AppendLine("App.ApplicationBaseUrl: " + ApplicationBaseUrl);
			if (!DefaultRootFileName.IsNullOrEmpty())
				sb.AppendLine("App.DefaultRootFileName: " + DefaultRootFileName);
			if (!DefaultHandler.IsNullOrEmpty())
				sb.AppendLine("App.DefaultHandler: " + DefaultHandler);
			if (!ServiceStackHttpHandlerFactory.DebugLastHandlerArgs.IsNullOrEmpty())
				sb.AppendLine("App.DebugLastHandlerArgs: " + ServiceStackHttpHandlerFactory.DebugLastHandlerArgs);

			response.ContentType = "text/plain";
			response.StatusCode = 404;
            response.EndHttpRequest(skipClose:true, afterBody: r => r.Write(sb.ToString()));
		}

		public bool IsReusable
		{
			get { return true; }
		}
	}
}