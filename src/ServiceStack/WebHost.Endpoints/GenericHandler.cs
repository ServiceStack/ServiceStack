using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.Remoting.Messaging;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.MiniProfiler;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;
using HttpRequestWrapper = ServiceStack.WebHost.Endpoints.Extensions.HttpRequestWrapper;
using HttpResponseWrapper = ServiceStack.WebHost.Endpoints.Extensions.HttpResponseWrapper;

namespace ServiceStack.WebHost.Endpoints
{
	public class GenericHandler : EndpointHandlerBase
	{
		public GenericHandler(string contentType, EndpointAttributes handlerAttributes, Feature format)
		{
			this.HandlerContentType = contentType;
			this.ContentTypeAttribute = ContentType.GetEndpointAttributes(contentType);
			this.HandlerAttributes = handlerAttributes;
			this.format = format;
		}

        private Feature format;
		public string HandlerContentType { get; set; }

		public EndpointAttributes ContentTypeAttribute { get; set; }

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

		public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName, Action closeAction = null)
		{
			try
			{
				EndpointHost.Config.AssertFeatures(format);

				if (EndpointHost.ApplyPreRequestFilters(httpReq, httpRes)) return;

				httpReq.ResponseContentType = httpReq.GetQueryStringContentType() ?? this.HandlerContentType;

				var request = CreateRequest(httpReq, operationName);
				if (EndpointHost.ApplyRequestFilters(httpReq, httpRes, request)) return;

				var response = GetResponse(httpReq, httpRes, request);

				if (response is IAsyncResult)
				{
					AsyncResultFactory.ProcessAsyncResponse(response as IAsyncResult, result => ProcessResponse(httpReq, httpRes, result, closeAction));
					return;
				}

				ProcessResponse(httpReq, httpRes, response, closeAction);
			}
			catch (Exception ex)
			{
				if (!EndpointHost.Config.WriteErrorsToResponse) throw;
				HandleException(httpReq, httpRes, operationName, ex);
			}
		}

		private static void ProcessResponse(IHttpRequest httpReq, IHttpResponse httpRes, object response, Action closeAction)
		{
			if (EndpointHost.ApplyResponseFilters(httpReq, httpRes, response))
			{
				if (closeAction != null)
					closeAction();
				return;
			}

			var callback = httpReq.QueryString["callback"];
			var doJsonp = EndpointHost.Config.AllowJsonpRequests && !string.IsNullOrEmpty(callback);
			if (doJsonp && !(response is CompressedResult))
				httpRes.WriteToResponse(httpReq, response, (callback + "(").ToUtf8Bytes(), ")".ToUtf8Bytes());
			else
				httpRes.WriteToResponse(httpReq, response);
			if (closeAction != null)
				closeAction();
		}
	}
}
