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
	public class GenericHandler : EndpointHandlerBase
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof (GenericHandler));

		public GenericHandler(string contentType, EndpointAttributes handlerAttributes, Feature usesFeature)
		{
			this.HandlerContentType = contentType;
			this.ContentTypeAttribute = ContentType.GetEndpointAttributes(contentType);
			this.HandlerAttributes = handlerAttributes;
			this.usesFeature = usesFeature;
		}

		private Feature usesFeature;
		public string HandlerContentType { get; set; }

		public EndpointAttributes ContentTypeAttribute { get; set; }

		public override object CreateRequest(IHttpRequest request, string operationName)
		{
			return GetRequest(request, operationName);
		}

		public override object GetResponse(IHttpRequest httpReq, IHttpResponse httpRes, object request)
		{
			var response = ExecuteService(request,
				HandlerAttributes | GetEndpointAttributes(httpReq), httpReq, httpRes);
			
			return response;
		}

		public object GetRequest(IHttpRequest httpReq, string operationName)
		{
			var requestType = GetOperationType(operationName);
			AssertOperationExists(operationName, requestType);

			using (Profiler.Current.Step("Deserialize Request"))
			{
				var requestDto = GetCustomRequestFromBinder(httpReq, requestType);
				return requestDto ?? DeserializeHttpRequest(requestType, httpReq, HandlerContentType);
			}
		}

		public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
			try
			{
                if (EndpointHost.ApplyPreRequestFilters(httpReq, httpRes)) return;

                EndpointHost.Config.AssertFeatures(usesFeature);

				httpReq.ResponseContentType = httpReq.GetQueryStringContentType() ?? this.HandlerContentType;
				var callback = httpReq.QueryString["callback"];
				var doJsonp = EndpointHost.Config.AllowJsonpRequests
							  && !string.IsNullOrEmpty(callback);

				var request = CreateRequest(httpReq, operationName);
				if (EndpointHost.ApplyRequestFilters(httpReq, httpRes, request)) return;

				var response = GetResponse(httpReq, httpRes, request);
				if (EndpointHost.ApplyResponseFilters(httpReq, httpRes, response)) return;

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

	}
}
