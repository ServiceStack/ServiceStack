using System;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
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
			var responseContentType = EndpointHost.Config.DefaultContentType;
			try
			{
				var restPath = GetRestPath(httpReq.HttpMethod, httpReq.PathInfo);
				if (restPath == null)
					throw new NotSupportedException("No RestPath found for: " + httpReq.HttpMethod + " " + httpReq.PathInfo);

				operationName = restPath.RequestType.Name;

				var callback = httpReq.QueryString["callback"];
				var doJsonp = EndpointHost.Config.AllowJsonpRequests
							  && !string.IsNullOrEmpty(callback);

				responseContentType = httpReq.ResponseContentType;

				var request = GetRequest(httpReq, restPath);
				if (EndpointHost.ApplyRequestFilters(httpReq, httpRes, request)) return;

				var response = GetResponse(httpReq, request);
				if (EndpointHost.ApplyResponseFilters(httpReq, httpRes, response)) return;

				if (responseContentType.Contains("jsv") && !string.IsNullOrEmpty(httpReq.QueryString["debug"]))
				{
					JsvSyncReplyHandler.WriteDebugResponse(httpRes, response);
					return;
				}

				var serializer = GetContentFilters().GetStreamSerializer(responseContentType);

				if (doJsonp) httpRes.Write(callback + "(");

				httpRes.WriteToResponse(response, serializer, responseContentType);

				if (doJsonp) httpRes.Write(")\n");
			}
			catch (Exception ex)
			{
				var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);
				Log.Error(errorMessage, ex);

				var attrEndpointType = ContentType.GetEndpointAttributes(responseContentType);
				httpRes.WriteErrorToResponse(attrEndpointType, operationName, errorMessage, ex);
			}
		}

		public override object GetResponse(IHttpRequest httpReq, object request)
		{
			var requestContentType = ContentType.GetEndpointAttributes(httpReq.ContentType);

			return ExecuteService(request,
				HandlerAttributes | requestContentType | GetEndpointAttributes(httpReq), httpReq);
		}

		private object GetRequest(IHttpRequest httpReq, IRestPath restPath)
		{
			var requestParams = httpReq.GetRequestParams();

			object requestDto = null;

			if (!string.IsNullOrEmpty(httpReq.ContentType) && httpReq.ContentLength > 0)
			{
				var requestDeserializer = GetContentFilters().GetStreamDeserializer(httpReq.ContentType);
				if (requestDeserializer != null)
				{
					requestDto = requestDeserializer(restPath.RequestType, httpReq.InputStream);
				}
			}

			return restPath.CreateRequest(httpReq.PathInfo, requestParams, requestDto);
		}

		/// <summary>
		/// Used in Unit tests
		/// </summary>
		/// <returns></returns>
		public override object CreateRequest(IHttpRequest httpReq, string operationName)
		{
			if (this.RestPath == null)
				throw new ArgumentNullException("No RestPath found");

			return GetRequest(httpReq, this.RestPath);
		}
	}

}