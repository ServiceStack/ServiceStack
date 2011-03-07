using System;
using System.Collections.Generic;
using System.Web;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public class NotFoundHttpHandler
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
			response.StatusCode = 404;
			response.Write("Handler for Request not found: \n\n");

			response.Write("\nRequest.HttpMethod: " + request.HttpMethod);
			response.Write("\nRequest.PathInfo: " + request.PathInfo);
			response.Write("\nRequest.QueryString: " + request.QueryString);
			response.Write("\nRequest.RawUrl: " + request.RawUrl);
		}

		public void ProcessRequest(HttpContext context)
		{
			var request = context.Request;
			var response = context.Response;

			var httpReq = new HttpRequestWrapper("NotFoundHttpHandler", request);
			
			response.ContentType = "text/plain";
			response.StatusCode = 404;
			response.Write("Handler for Request not found: \n\n");

			response.Write("\nRequest.ApplicationPath: " + request.ApplicationPath);
			response.Write("\nRequest.CurrentExecutionFilePath: " + request.CurrentExecutionFilePath);
			response.Write("\nRequest.FilePath: " + request.FilePath);
			response.Write("\nRequest.HttpMethod: " + request.HttpMethod);
			response.Write("\nRequest.MapPath('~'): " + request.MapPath("~"));
			response.Write("\nRequest.Path: " + request.Path);
			response.Write("\nRequest.PathInfo: " + request.PathInfo);
			response.Write("\nRequest.ResolvedPathInfo: " + httpReq.PathInfo);
			response.Write("\nRequest.PhysicalPath: " + request.PhysicalPath);
			response.Write("\nRequest.PhysicalApplicationPath: " + request.PhysicalApplicationPath);
			response.Write("\nRequest.QueryString: " + request.QueryString);
			response.Write("\nRequest.RawUrl: " + request.RawUrl);
			try
			{
				response.Write("\nRequest.Url.AbsoluteUri: " + request.Url.AbsoluteUri);
				response.Write("\nRequest.Url.AbsolutePath: " + request.Url.AbsolutePath);
				response.Write("\nRequest.Url.Fragment: " + request.Url.Fragment);
				response.Write("\nRequest.Url.Host: " + request.Url.Host);
				response.Write("\nRequest.Url.LocalPath: " + request.Url.LocalPath);
				response.Write("\nRequest.Url.Port: " + request.Url.Port);
				response.Write("\nRequest.Url.Query: " + request.Url.Query);
				response.Write("\nRequest.Url.Scheme: " + request.Url.Scheme);
				response.Write("\nRequest.Url.Segments: " + request.Url.Segments);
			}
			catch (Exception ex)
			{
				response.Write("\nRequest.Url ERROR: " + ex.Message);
			}
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