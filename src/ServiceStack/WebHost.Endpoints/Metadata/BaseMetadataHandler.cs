using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.UI;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Web;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.WebHost.Endpoints.Support.Metadata.Controls;

namespace ServiceStack.WebHost.Endpoints.Metadata
{
	using System.Collections.Generic;
	using System.Text;
	using ServiceHost;

	public abstract class BaseMetadataHandler : HttpHandlerBase, IServiceStackHttpHandler
	{
		public abstract Format Format { get; }

		public string ContentType { get; set; }
		public string ContentFormat { get; set; }

		public override void Execute(HttpContext context)
		{
            var httpReq = new HttpRequestWrapper(context.Request);
            var httpRes = new HttpResponseWrapper(context.Response);
            var operationName = httpReq.QueryString["op"];
            if (!EndpointHost.Metadata.IsVisible(httpReq, Format, operationName))
            {
                EndpointHost.Config.HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Service Not Available");
                return;
            }

			var writer = new HtmlTextWriter(context.Response.Output);
			context.Response.ContentType = "text/html";

			ProcessOperations(writer, new HttpRequestWrapper(GetType().Name, context.Request));
		}

		public virtual void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
            operationName = httpReq.QueryString["op"];
            if (!EndpointHost.Metadata.IsVisible(httpReq, Format, operationName))
            {
                EndpointHost.Config.HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Service Not Available");
                return;
            }

            using (var sw = new StreamWriter(httpRes.OutputStream))
			{
				var writer = new HtmlTextWriter(sw);
				httpRes.ContentType = "text/html";
				ProcessOperations(writer, httpReq);
			}
		}

		protected virtual void ProcessOperations(HtmlTextWriter writer, IHttpRequest httpReq)
		{
			EndpointHost.Config.AssertFeatures(Feature.Metadata);

            if (EndpointHost.Config.MetadataVisibility != EndpointAttributes.Any)
            {
                var actualAttributes = Extensions.HttpRequestExtensions.GetAttributes(httpReq);
                if ((actualAttributes & EndpointHost.Config.MetadataVisibility) != EndpointHost.Config.MetadataVisibility)
                    throw new UnauthorizedAccessException("Access to metadata is unauthorized.");

            }

			var metadata = EndpointHost.Metadata;
			var operationName = httpReq.QueryString["op"];
			if (operationName != null)
			{
				var allTypes = metadata.GetAllTypes();
				var operationType = allTypes.Single(x => x.Name == operationName);
				var requestMessage = CreateMessage(operationType);
				var restPaths = CreateRestPaths(operationType);
				string responseMessage = null;

			    var responseType = metadata.GetResponseTypeByRequest(operationType);
                if (responseType != null)
                {
                    responseMessage = CreateMessage(responseType);                    
                }
				
                var description = GetDescriptionFromOperationType(operationType);
                if (!description.IsNullOrEmpty())
                {
                    description = "<div id='desc'>"
                        + "<p>" + description
                            .Replace("<", "&lt;")
                            .Replace(">", "&gt;")
                            .Replace("\n", "<br />\n")
                        + "</p>"
                        + "</div>";
                }


			    RenderOperation(writer, httpReq, operationName, requestMessage, responseMessage, restPaths, description);
				return;
			}

			RenderOperations(writer, httpReq, metadata);
		}

	    public static string GetDescriptionFromOperationType(Type operationType)
	    {
	        var description = "";
	        var descAttrs = operationType.GetCustomAttributes(typeof(DescriptionAttribute), true);
	        if (descAttrs.Length > 0)
	        {
	            var descAttr = (DescriptionAttribute) descAttrs[0];
	            return descAttr.Description;
	        }
	        return description;
	    }

	    protected abstract string CreateMessage(Type dtoType);

		protected virtual void RenderOperation(HtmlTextWriter writer, IHttpRequest httpReq, string operationName, 
			string requestMessage, string responseMessage, string restPaths, string descriptionHtml)
		{
			var operationControl = new OperationControl
			{
				HttpRequest = httpReq,
				MetadataConfig = EndpointHost.Config.ServiceEndpointsMetadataConfig,
				Title = EndpointHost.Config.ServiceName,
				Format = this.Format,
				OperationName = operationName,
				HostName = httpReq.GetUrlHostName(),
				RequestMessage = requestMessage,
				ResponseMessage = responseMessage,
				RestPaths = restPaths,
				DescriptionHtml = descriptionHtml,
			};
			if (!this.ContentType.IsNullOrEmpty())
			{
				operationControl.ContentType = this.ContentType;
			}
			if (!this.ContentFormat.IsNullOrEmpty())
			{
				operationControl.ContentFormat = this.ContentFormat;
			}

			operationControl.Render(writer);
		}

		protected abstract void RenderOperations(HtmlTextWriter writer, IHttpRequest httpReq, ServiceMetadata metadata);

		protected virtual string CreateRestPaths(Type operationType)
		{
			var map = EndpointHost.ServiceManager.ServiceController.RestPathMap;
			var paths = new List<RestPath>();
			foreach (var key in map.Keys)
			{
				paths.AddRange(map[key].Where(x => x.RequestType == operationType));
			}
			var restPaths = new StringBuilder();
			foreach (var restPath in paths)
			{
				var verbs = restPath.AllowsAllVerbs ? "All Verbs" : restPath.AllowedVerbs;
				restPaths.AppendLine(verbs + " " + restPath.Path);
			}
			return restPaths.ToString();
		}

	}
}