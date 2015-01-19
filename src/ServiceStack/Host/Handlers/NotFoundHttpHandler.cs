using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using ServiceStack.Text;
using ServiceStack.Logging;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public class NotFoundHttpHandler : HttpAsyncTaskHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(NotFoundHttpHandler));

        public bool? IsIntegratedPipeline { get; set; }
        public string WebHostPhysicalPath { get; set; }
        public List<string> WebHostRootFileNames { get; set; }
        public string WebHostUrl { get; set; }
        public string DefaultRootFileName { get; set; }
        public string DefaultHandler { get; set; }

        public override void ProcessRequest(IRequest request, IResponse response, string operationName)
        {
            Log.ErrorFormat("{0} Request not found: {1}", request.UserHostAddress, request.RawUrl);

            var text = new StringBuilder();

            var responseStatus = response.Dto.GetResponseStatus();
            if (responseStatus != null)
            {
                text.AppendLine(
                    responseStatus.ErrorCode != responseStatus.Message
                    ? "Error ({0}): {1}\n".Fmt(responseStatus.ErrorCode, responseStatus.Message)
                    : "Error: {0}\n".Fmt(responseStatus.Message ?? responseStatus.ErrorCode));
            }

            if (HostContext.DebugMode)
            {
                text.AppendLine("Handler for Request not found (404):\n")
                    .AppendLine("  Request.HttpMethod: " + request.Verb)
                    .AppendLine("  Request.PathInfo: " + request.PathInfo)
                    .AppendLine("  Request.QueryString: " + request.QueryString)
                    .AppendLine("  Request.RawUrl: " + request.RawUrl);
            }
            else
            {
                text.Append("404");
            }

            response.ContentType = "text/plain";
            response.StatusCode = 404;

            if (responseStatus != null)
                response.StatusDescription = responseStatus.ErrorCode;

            response.EndHttpHandlerRequest(skipClose: true, afterHeaders: r => r.Write(text.ToString()));
        }

        public override void ProcessRequest(HttpContextBase context)
        {
            var request = context.Request;
            var response = context.Response;

            var httpReq = context.ToRequest(GetType().GetOperationName());
            if (!request.IsLocal)
            {
                ProcessRequestAsync(httpReq, httpReq.Response, null);
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
            if (!WebHostUrl.IsNullOrEmpty())
                sb.AppendLine("App.ApplicationBaseUrl: " + WebHostUrl);
            if (!DefaultRootFileName.IsNullOrEmpty())
                sb.AppendLine("App.DefaultRootFileName: " + DefaultRootFileName);
            if (!DefaultHandler.IsNullOrEmpty())
                sb.AppendLine("App.DefaultHandler: " + DefaultHandler);
            if (!HttpHandlerFactory.DebugLastHandlerArgs.IsNullOrEmpty())
                sb.AppendLine("App.DebugLastHandlerArgs: " + HttpHandlerFactory.DebugLastHandlerArgs);

            response.ContentType = "text/plain";
            response.StatusCode = 404;
            response.EndHttpHandlerRequest(skipClose: true, afterHeaders: r => r.Write(sb.ToString()));
        }

        public override bool IsReusable
        {
            get { return true; }
        }
    }
}