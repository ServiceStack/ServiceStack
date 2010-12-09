using System;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class RestHandler : EndpointHandlerBase
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(RestHandler));

		public static string[] PreferredContentTypes = new[] {
			ContentType.Json, ContentType.Xml, ContentType.Jsv
		};

		public static IRestPath FindMatchingRestPath(string httpMethod, string pathInfo)
		{
			var controller = ServiceManager != null
				? ServiceManager.ServiceController
				: EndpointHost.Config.ServiceController;

			return controller.GetRestPathForRequest(httpMethod, pathInfo);
		}

		public static string GetDefaultContentType(string[] acceptContentType, string defaultContentType)
		{
			var acceptsAnything = false;
			var hasDefaultContentType = !string.IsNullOrEmpty(defaultContentType);
			foreach (var contentType in acceptContentType)
			{
				acceptsAnything = acceptsAnything || contentType == "*/*";
				if (acceptsAnything && hasDefaultContentType) return defaultContentType;

				foreach (var preferredContentType in PreferredContentTypes)
				{
					if (contentType.StartsWith(preferredContentType)) return preferredContentType;
				}
			}

			//We could also send a '406 Not Acceptable', but this is allowed also
			return PreferredContentTypes[0]; 
		}

		public void ProcessRequest(HttpContext context)
		{
			var response = new HttpResponseWrapper(context.Response);
			var operationName = this.RequestName ?? context.Request.GetOperationName();

			if (string.IsNullOrEmpty(operationName)) return;

			if (DefaultHandledRequest(context)) return;

			try
			{
				var req = context.Request;
				var httpMethod = req.HttpMethod;
				var pathInfo = req.PathInfo;

				var restPath = FindMatchingRestPath(httpMethod, pathInfo);

				if (restPath == null)
				{
					response.StatusCode = 404;
					response.ContentType = "text/plain";
					response.Write("No resource found at: " + pathInfo);
					return;
				}

				var defaultContentType = GetDefaultContentType(req.AcceptTypes, restPath.DefaultContentType);

				var requestParams = req.GetRequestParams();
				var request = restPath.CreateRequest(pathInfo, requestParams);

				var attrEndpointType = ContentType.GetEndpointAttributes(defaultContentType);

				var endpointAttributes = EndpointAttributes.SyncReply | attrEndpointType
					| GetEndpointAttributes(context.Request);

				var result = ExecuteService(request, endpointAttributes);

				response.WriteToResponse(result, 
					(dto) => HttpResponseFilter.Instance.Serialize(defaultContentType, dto), 
					defaultContentType);
			}
			catch (Exception ex)
			{
				var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);
				Log.Error(errorMessage, ex);

				response.WriteJsonErrorToResponse(operationName, errorMessage, ex);
			}
		}

		public override object CreateRequest(string operationName, string httpMethod, NameValueCollection queryString, NameValueCollection formData, Stream inputStream)
		{
			throw new NotImplementedException();
		}
	}

}