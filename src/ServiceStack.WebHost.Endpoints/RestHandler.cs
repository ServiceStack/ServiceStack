using System;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Extensions;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class RestHandler 
		: EndpointHandlerBase
	{
		public RestHandler()
		{
			this.HandlerAttributes = EndpointAttributes.SyncReply;
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(RestHandler));

		public static IRestPath FindMatchingRestPath(string httpMethod, string pathInfo)
		{
			var controller = ServiceManager != null
				? ServiceManager.ServiceController
				: EndpointHost.Config.ServiceController;

			return controller.GetRestPathForRequest(httpMethod, pathInfo);
		}

		public IRestPath GetRestPath(string httpMethod, string pathInfo)
		{
			if (this.RestPath == null)
			{
				this.RestPath = FindMatchingRestPath(httpMethod, pathInfo);
			}
			return this.RestPath;
		}

		internal IRestPath RestPath { get; set; }

		public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
			var attrEndpointType = EndpointAttributes.Json;
			try
			{
				var restPath = GetRestPath(httpReq.HttpMethod, httpReq.PathInfo);
				if (restPath == null)
					throw new NotSupportedException("No RestPath found for: " + httpReq.HttpMethod + " " + httpReq.PathInfo);

				var contentType = httpReq.GetContentType();

				attrEndpointType = ContentType.GetEndpointAttributes(contentType);

				var callback = httpReq.QueryString["callback"];
				var doJsonp = EndpointHost.Config.AllowJsonpRequests
							  && !string.IsNullOrEmpty(callback);

				var requestParams = httpReq.GetRequestParams();
				var request = restPath.CreateRequest(httpReq.PathInfo, requestParams);

				var response = ExecuteService(request,
					HandlerAttributes | attrEndpointType | GetEndpointAttributes(httpReq), httpReq);

				var serializer = EndpointHost.Config.ContentTypeFilter.GetStreamSerializer(contentType);

				if (doJsonp) httpRes.Write(callback + "(");

				httpRes.WriteToResponse(response, serializer, contentType);

				if (doJsonp) httpRes.Write(")");
			}
			catch (Exception ex)
			{
				var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);
				Log.Error(errorMessage, ex);

				httpRes.WriteErrorToResponse(attrEndpointType, operationName, errorMessage, ex);
			}
		}

		/// <summary>
		/// Used in Unit tests
		/// </summary>
		/// <returns></returns>
		public override object CreateRequest(IHttpRequest httpReq, string operationName)
		{
			if (this.RestPath == null)
				throw new ArgumentNullException("No RestPath found");

			var requestParams = httpReq.QueryString.ToDictionary();
			httpReq.FormData.ToDictionary().ForEach(x => requestParams.Add(x.Key, x.Value));

			var request = this.RestPath.CreateRequest(httpReq.PathInfo, requestParams);
			return request;
		}
	}

}