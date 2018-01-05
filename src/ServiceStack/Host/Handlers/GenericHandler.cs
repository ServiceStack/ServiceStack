using System;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.MiniProfiler;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public class GenericHandler : ServiceStackHandlerBase, IRequestHttpHandler
    {
        public GenericHandler(string contentType, RequestAttributes handlerAttributes, Feature format)
        {
            this.HandlerContentType = contentType;
            this.ContentTypeAttribute = ContentFormat.GetEndpointAttributes(contentType);
            this.HandlerAttributes = handlerAttributes;
            this.format = format;
        }

        private readonly Feature format;
        public string HandlerContentType { get; set; }

        public RequestAttributes ContentTypeAttribute { get; set; }

        public Task<object> CreateRequestAsync(IRequest req, string operationName)
        {
            var requestType = GetOperationType(operationName);

            AssertOperationExists(operationName, requestType);

            using (Profiler.Current.Step("Deserialize Request"))
            {
                var requestDto = GetCustomRequestFromBinder(req, requestType)
                    ?? (DeserializeHttpRequest(requestType, req, HandlerContentType)
                    ?? requestType.CreateInstance());

                return appHost.ApplyRequestConvertersAsync(req, requestDto);
            }
        }

        public override bool RunAsAsync()
        {
            return true;
        }

        public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
        {
            try
            {
                appHost.AssertFeatures(format);

                if (appHost.ApplyPreRequestFilters(httpReq, httpRes))
                    return;

                httpReq.ResponseContentType = httpReq.GetQueryStringContentType() ?? this.HandlerContentType;

                var request = httpReq.Dto = await CreateRequestAsync(httpReq, operationName);

                await appHost.ApplyRequestFiltersAsync(httpReq, httpRes, request);
                if (httpRes.IsClosed)
                    return;

                httpReq.RequestAttributes |= HandlerAttributes;

                var rawResponse = await GetResponseAsync(httpReq, request);
                if (httpRes.IsClosed)
                    return;

                await HandleResponse(httpReq, httpRes, rawResponse);
            }
            //sync with RestHandler
            catch (TaskCanceledException)
            {
                httpRes.StatusCode = (int)HttpStatusCode.PartialContent;
                httpRes.EndRequest();
            }
            catch (Exception ex)
            {
                if (!HostContext.Config.WriteErrorsToResponse)
                {
                    await HostContext.AppHost.ApplyResponseConvertersAsync(httpReq, ex);
                }
                else
                {
                    await HandleException(httpReq, httpRes, operationName, 
                        await HostContext.AppHost.ApplyResponseConvertersAsync(httpReq, ex) as Exception ?? ex);
                }
            }
        }

    }
}
