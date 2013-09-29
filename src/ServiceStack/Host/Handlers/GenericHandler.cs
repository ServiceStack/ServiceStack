using System;
using ServiceStack.MiniProfiler;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
	public class GenericHandler : ServiceStackHandlerBase
	{
		public GenericHandler(string contentType, RequestAttributes handlerAttributes, Feature format)
		{
			this.HandlerContentType = contentType;
			this.ContentTypeAttribute = ContentFormat.GetEndpointAttributes(contentType);
			this.HandlerAttributes = handlerAttributes;
			this.format = format;
		}

        private Feature format;
		public string HandlerContentType { get; set; }

		public RequestAttributes ContentTypeAttribute { get; set; }

		public override object CreateRequest(IHttpRequest request, string operationName)
		{
			return GetRequest(request, operationName);
		}

		public override object GetResponse(IHttpRequest httpReq, IHttpResponse httpRes, object request)
		{
			var response = ExecuteService(request,
                HandlerAttributes | httpReq.GetAttributes(), httpReq, httpRes);
			
			return response;
		}

		public object GetRequest(IHttpRequest httpReq, string operationName)
		{
			var requestType = GetOperationType(operationName);
			AssertOperationExists(operationName, requestType);

			using (Profiler.Current.Step("Deserialize Request"))
			{
				var requestDto = GetCustomRequestFromBinder(httpReq, requestType);
				return requestDto ?? DeserializeHttpRequest(requestType, httpReq, HandlerContentType)
                    ?? requestType.CreateInstance();
			}
		}

		public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
			try
			{
                HostContext.AssertFeatures(format);

                if (HostContext.ApplyPreRequestFilters(httpReq, httpRes)) return;

				httpReq.ResponseContentType = httpReq.GetQueryStringContentType() ?? this.HandlerContentType;
				var callback = httpReq.QueryString["callback"];
				var doJsonp = HostContext.Config.AllowJsonpRequests
							  && !string.IsNullOrEmpty(callback);

				var request = CreateRequest(httpReq, operationName);
				if (HostContext.ApplyRequestFilters(httpReq, httpRes, request)) return;

				var response = GetResponse(httpReq, httpRes, request);
				if (HostContext.ApplyResponseFilters(httpReq, httpRes, response)) return;

				if (doJsonp && !(response is CompressedResult))
					httpRes.WriteToResponse(httpReq, response, (callback + "(").ToUtf8Bytes(), ")".ToUtf8Bytes());
				else
					httpRes.WriteToResponse(httpReq, response);
			}
			catch (Exception ex)
			{
				if (!HostContext.Config.WriteErrorsToResponse) throw;
				HandleException(httpReq, httpRes, operationName, ex);
			}
		}

	}
}
