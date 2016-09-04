using System;
using System.Threading.Tasks;
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
            this.appHost = HostContext.AppHost;
        }

        private readonly ServiceStackHost appHost;
        private readonly Feature format;
        public string HandlerContentType { get; set; }

        public RequestAttributes ContentTypeAttribute { get; set; }

        public override object CreateRequest(IRequest req, string operationName)
        {
            var requestType = GetOperationType(operationName);

            AssertOperationExists(operationName, requestType);

            using (Profiler.Current.Step("Deserialize Request"))
            {
                var requestDto = GetCustomRequestFromBinder(req, requestType) 
                    ?? (DeserializeHttpRequest(requestType, req, HandlerContentType)
                        ?? requestType.CreateInstance());

                return appHost.ApplyRequestConverters(req, requestDto);
            }
        }

        public override object GetResponse(IRequest httpReq, object request)
        {
            httpReq.RequestAttributes |= HandlerAttributes;
            var response = ExecuteService(request, httpReq);

            return response;
        }

        public override bool RunAsAsync()
        {
            return true;
        }

        public override Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
        {
            try
            {
                appHost.AssertFeatures(format);

                if (appHost.ApplyPreRequestFilters(httpReq, httpRes))
                    return TypeConstants.EmptyTask;

                httpReq.ResponseContentType = httpReq.GetQueryStringContentType() ?? this.HandlerContentType;
                var callback = httpReq.QueryString[Keywords.Callback];
                var doJsonp = HostContext.Config.AllowJsonpRequests
                              && !string.IsNullOrEmpty(callback);

                var request = httpReq.Dto = CreateRequest(httpReq, operationName);

                if (appHost.ApplyRequestFilters(httpReq, httpRes, request))
                    return TypeConstants.EmptyTask;

                var rawResponse = GetResponse(httpReq, request);

                if (httpRes.IsClosed)
                    return TypeConstants.EmptyTask;

                return HandleResponse(rawResponse, response =>
                {
                    response = appHost.ApplyResponseConverters(httpReq, response);

                    if (appHost.ApplyResponseFilters(httpReq, httpRes, response))
                        return TypeConstants.EmptyTask;

                    if (doJsonp && !(response is CompressedResult))
                        return httpRes.WriteToResponse(httpReq, response, (callback + "(").ToUtf8Bytes(), ")".ToUtf8Bytes());

                    return httpRes.WriteToResponse(httpReq, response);
                },
                ex => !HostContext.Config.WriteErrorsToResponse
                    ? ex.ApplyResponseConverters(httpReq).AsTaskException()
                    : HandleException(httpReq, httpRes, operationName, ex.ApplyResponseConverters(httpReq)));
            }
            catch (Exception ex)
            {
                return !HostContext.Config.WriteErrorsToResponse
                    ? ex.ApplyResponseConverters(httpReq).AsTaskException()
                    : HandleException(httpReq, httpRes, operationName, ex.ApplyResponseConverters(httpReq));
            }
        }

    }
}
