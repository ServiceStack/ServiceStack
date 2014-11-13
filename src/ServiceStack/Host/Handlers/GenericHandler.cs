using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServiceStack.MiniProfiler;
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
		    this.appHost = HostContext.AppHost;
		}

	    private ServiceStackHost appHost;
        private readonly Feature format;
		public string HandlerContentType { get; set; }
        public bool IsMutltiRequest { get; set; }

		public RequestAttributes ContentTypeAttribute { get; set; }

		public override object CreateRequest(IRequest request, string operationName)
		{
			return GetRequest(request, operationName);
		}

		public override object GetResponse(IRequest httpReq, object request)
		{
		    httpReq.RequestAttributes |= HandlerAttributes;
			var response = ExecuteService(request, httpReq);
			
			return response;
		}

		public object GetRequest(IRequest httpReq, string operationName)
		{
            httpReq.OperationType = GetOperationType(operationName);
            AssertOperationExists(operationName, httpReq.OperationType);

		    var requestType = IsMutltiRequest 
                ? httpReq.OperationType.MakeArrayType() 
                : httpReq.OperationType;

            using (Profiler.Current.Step("Deserialize Request"))
			{
				var requestDto = GetCustomRequestFromBinder(httpReq, requestType);
				return requestDto ?? DeserializeHttpRequest(requestType, httpReq, HandlerContentType)
                    ?? requestType.CreateInstance();
			}
		}

        public override bool RunAsAsync()
        {
            return true;
        }

        public virtual bool ApplyRequestFilters(IRequest req, IResponse res, object requestDto)
        {
            if (!IsMutltiRequest)
                return appHost.ApplyRequestFilters(req, res, requestDto);

            var dtos = (IEnumerable)requestDto;
            foreach (var dto in dtos)
            {
                if (appHost.ApplyRequestFilters(req, res, dto))
                    return true;
            }
            return false;
        }

        public virtual bool ApplyResponseFilters(IRequest req, IResponse res, object responseDto)
        {
            if (!IsMutltiRequest)
                return appHost.ApplyResponseFilters(req, res, responseDto);

            var dtos = (IEnumerable)responseDto;
            foreach (var dto in dtos)
            {
                if (appHost.ApplyResponseFilters(req, res, dto))
                    return true;
            }
            return false;
        }

	    public override Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
        {
			try
            {
                var appHost = HostContext.AppHost;
                appHost.AssertFeatures(format);

                if (appHost.ApplyPreRequestFilters(httpReq, httpRes))
                    return EmptyTask;

                httpReq.ResponseContentType = httpReq.GetQueryStringContentType() ?? this.HandlerContentType;
                var callback = httpReq.QueryString["callback"];
                var doJsonp = HostContext.Config.AllowJsonpRequests
                              && !string.IsNullOrEmpty(callback);

                var request = httpReq.Dto = CreateRequest(httpReq, operationName);

                if (ApplyRequestFilters(httpReq, httpRes, request))
                    return EmptyTask;

                var rawResponse = GetResponse(httpReq, request);
                return HandleResponse(rawResponse, response => 
                {
                    if (ApplyResponseFilters(httpReq, httpRes, response))
                        return EmptyTask;

                    if (doJsonp && !(response is CompressedResult))
                        return httpRes.WriteToResponse(httpReq, response, (callback + "(").ToUtf8Bytes(),")".ToUtf8Bytes());

                    return httpRes.WriteToResponse(httpReq, response);
                },
                ex => !HostContext.Config.WriteErrorsToResponse
                    ? ex.AsTaskException()
                    : HandleException(httpReq, httpRes, operationName, ex));
            }
            catch (Exception ex)
            {
                return !HostContext.Config.WriteErrorsToResponse
                    ? ex.AsTaskException()
                    : HandleException(httpReq, httpRes, operationName, ex);
            }
        }

	}
}
