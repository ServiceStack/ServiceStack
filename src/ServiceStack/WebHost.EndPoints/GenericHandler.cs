using System;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
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

		public override object GetResponse(IHttpRequest httpReq, object request)
		{
			var response = ExecuteService(request,
				HandlerAttributes | GetEndpointAttributes(httpReq), httpReq);
			
			return response;
		}

		public object GetRequest(IHttpRequest httpReq, string operationName)
		{
			var operationType = GetOperationType(operationName);
			AssertOperationExists(operationName, operationType);

			return DeserializeContentType(operationType, httpReq, HandlerContentType);
		}
        
		//public StreamSerializerDelegate GetStreamSerializer(string contentType)
		//{
		//    return GetContentFilters().GetStreamSerializer(contentType);
		//}

		public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
			try
			{
				EndpointHost.Config.AssertFeatures(usesFeature);

				httpReq.ResponseContentType = httpReq.GetQueryStringContentType() ?? this.HandlerContentType;
				var callback = httpReq.QueryString["callback"];
				var doJsonp = EndpointHost.Config.AllowJsonpRequests
							  && !string.IsNullOrEmpty(callback);

				var request = CreateRequest(httpReq, operationName);
				if (EndpointHost.ApplyRequestFilters(httpReq, httpRes, request)) return;

				var response = GetResponse(httpReq, request);
				if (EndpointHost.ApplyResponseFilters(httpReq, httpRes, response)) return;

				if (doJsonp)
					httpRes.WriteToResponse(httpReq, response, (callback + "(").ToUtf8Bytes(), ")".ToUtf8Bytes());
				else
					httpRes.WriteToResponse(httpReq, response);
			}
			catch (Exception ex)
			{
                bool writeErrorToResponse = ServiceStack.Configuration.ConfigUtils.GetAppSetting<bool>(ServiceStack.Configuration.Keys.WriteErrorsToResponse, true);
                if(!writeErrorToResponse) {
                    throw;
                }
                var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);
                Log.Error(errorMessage, ex);

                try {
                    //httpRes.WriteToResponse always calls .Close in it's finally statement so if there is a problem writing to response, by now it will be closed
                    if(!httpRes.IsClosed) {
                        httpRes.WriteErrorToResponse(HandlerContentType, operationName, errorMessage, ex);
                    }
                }
                catch(Exception WriteErrorEx) {
                    //Exception in writing to response should not hide the original exception
                    Log.Info("Failed to write error to response: {0}", WriteErrorEx);
                    //rethrow the original exception
                    throw ex;
                }
            }
		}
	}
}
