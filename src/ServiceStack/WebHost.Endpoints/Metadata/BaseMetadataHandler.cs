using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
		const string ResponseSuffix = "Response";

		public abstract EndpointType EndpointType { get; }

		public string ContentType { get; set; }
		public string ContentFormat { get; set; }

		public override void Execute(HttpContext context)
		{
			var writer = new HtmlTextWriter(context.Response.Output);
			context.Response.ContentType = "text/html";

			ProcessOperations(writer, new HttpRequestWrapper(GetType().Name, context.Request));
		}

		public virtual void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
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

			var operations = EndpointHost.ServiceOperations;
			var operationName = httpReq.QueryString["op"];
			if (operationName != null)
			{
				var allTypes = operations.AllOperations.Types;
				var operationType = allTypes.Single(x => x.Name == operationName);
				var requestMessage = CreateMessage(operationType);
				var restPaths = CreateRestPaths(operationType);
				string responseMessage = null;
				if (allTypes.Any(x => x.Name == operationName + ResponseSuffix))
				{
					var operationResponseType = allTypes.Single(x => x.Name == operationName + ResponseSuffix);
					responseMessage = CreateMessage(operationResponseType);
				}
				var description = "";
				var descAttrs = operationType.GetCustomAttributes(typeof(DescriptionAttribute), true);
				if (descAttrs.Length > 0)
				{
					var descAttr = (DescriptionAttribute) descAttrs[0]; 
					if (!descAttr.Description.IsNullOrEmpty())
					{
						description = "<div id='desc'>" 
							+ "<p>" + descAttr.Description
								.Replace("<", "&lt;")
								.Replace(">", "&gt;")
								.Replace("\n", "<br />\n")
							+ "</p>"
							+ "</div>";
					}
				}

				RenderOperation(writer, httpReq, operationName, requestMessage, responseMessage, restPaths, description);
				return;
			}

			RenderOperations(writer, httpReq, operations.AllOperations);
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
				EndpointType = this.EndpointType,
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

		protected abstract void RenderOperations(HtmlTextWriter writer, IHttpRequest httpReq, Operations allOperations);

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