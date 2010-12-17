using System;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class GenericHandler : EndpointHandlerBase
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof (GenericHandler));

		protected GenericHandler(string contentType, EndpointAttributes handlerAttributes)
		{
			this.HandlerContentType = contentType;
			this.ContentTypeAttribute = ContentType.GetEndpointAttributes(contentType);
			this.HandlerAttributes = handlerAttributes;
		}

		public string HandlerContentType { get; set; }

		public EndpointAttributes ContentTypeAttribute { get; set; }

		public override object CreateRequest(IHttpRequest request, string operationName)
		{
			return GetRequest(request, operationName);
		}

		public object GetRequest(IHttpRequest httpReq, string operationName)
		{
			var operationType = GetOperationType(operationName);
			AssertOperationExists(operationName, operationType);

			return DeserializeContentType(operationType, httpReq, ContentTypeAttribute);
		}
        
		public StreamSerializerDelegate GetStreamSerializer(string contentType)
		{
			return EndpointHost.Config.ContentTypeFilter.GetStreamSerializer(contentType);
		}

		public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
			try
			{
				var contentType = httpReq.GetQueryStringContentType() ?? this.HandlerContentType;
				var callback = httpReq.QueryString["callback"];
				var doJsonp = EndpointHost.Config.AllowJsonpRequests
							  && !string.IsNullOrEmpty(callback);

				var request = CreateRequest(httpReq, operationName);

				var response = ExecuteService(request,
					HandlerAttributes | GetEndpointAttributes(httpReq), httpReq);

				var serializer = GetStreamSerializer(contentType);

				
				if (doJsonp) httpRes.Write(callback + "(");
				
				httpRes.WriteToResponse(response, serializer, contentType);
	
				if (doJsonp) httpRes.Write(")");

			}
			catch (Exception ex)
			{
				var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);
				Log.Error(errorMessage, ex);

				httpRes.WriteErrorToResponse(HandlerContentType, operationName, errorMessage, ex);
			}
		}
	}
}