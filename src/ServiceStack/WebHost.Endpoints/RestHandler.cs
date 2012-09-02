using System;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.MiniProfiler;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
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
                if (EndpointHost.ApplyPreRequestFilters(httpReq, httpRes)) return;

				var restPath = GetRestPath(httpReq.HttpMethod, httpReq.PathInfo);
				if (restPath == null)
					throw new NotSupportedException("No RestPath found for: " + httpReq.HttpMethod + " " + httpReq.PathInfo);

				operationName = restPath.RequestType.Name;

				var callback = httpReq.GetJsonpCallback();
				var doJsonp = EndpointHost.Config.AllowJsonpRequests
							  && !string.IsNullOrEmpty(callback);

				responseContentType = httpReq.ResponseContentType;
				EndpointHost.Config.AssertContentType(responseContentType);

				var request = GetRequest(httpReq, restPath);
				if (EndpointHost.ApplyRequestFilters(httpReq, httpRes, request)) return;

				var response = GetResponse(httpReq, httpRes, request);
				if (EndpointHost.ApplyResponseFilters(httpReq, httpRes, response)) return;

				if (responseContentType.Contains("jsv") && !string.IsNullOrEmpty(httpReq.QueryString["debug"]))
				{
					JsvSyncReplyHandler.WriteDebugResponse(httpRes, response);
					return;
				}

				if (doJsonp && !(response is CompressedResult))
                    httpRes.WriteToResponse(httpReq, response, (callback + "(").ToUtf8Bytes(), ")".ToUtf8Bytes());
                else
                    httpRes.WriteToResponse(httpReq, response);
            }
            catch (Exception ex) 
			{
				if (!EndpointHost.Config.WriteErrorsToResponse) throw;
				HandleException(httpReq, httpRes, operationName, ex);
			}
		}

		public override object GetResponse(IHttpRequest httpReq, IHttpResponse httpRes, object request)
		{
			var requestContentType = ContentType.GetEndpointAttributes(httpReq.ResponseContentType);

			return ExecuteService(request,
				HandlerAttributes | requestContentType | GetEndpointAttributes(httpReq), httpReq, httpRes);
		}

		private static object GetRequest(IHttpRequest httpReq, IRestPath restPath)
		{
			var requestType = restPath.RequestType;
			using (Profiler.Current.Step("Deserialize Request"))
			{

				var requestDto = GetCustomRequestFromBinder(httpReq, requestType);
				if (requestDto != null)	return requestDto;

                var requestParams = httpReq.GetRequestParams();
                requestDto = CreateContentTypeRequest(httpReq, requestType, httpReq.ContentType);

				return restPath.CreateRequest(httpReq.PathInfo, requestParams, requestDto);
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

			return GetRequest(httpReq, this.RestPath);
		}
	}

}
