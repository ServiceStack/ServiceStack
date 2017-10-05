using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServiceStack.Host.Handlers;
using ServiceStack.MiniProfiler;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public class RestHandler
        : ServiceStackHandlerBase
    {
        public RestHandler()
        {
            this.HandlerAttributes = RequestAttributes.Reply;
        }

        public static IRestPath FindMatchingRestPath(string httpMethod, string pathInfo, out string contentType)
        {
            pathInfo = GetSanitizedPathInfo(pathInfo, out contentType);

            return HostContext.ServiceController.GetRestPathForRequest(httpMethod, pathInfo);
        }

        public static string GetSanitizedPathInfo(string pathInfo, out string contentType)
        {
            contentType = null;
            if (HostContext.Config.AllowRouteContentTypeExtensions)
            {
                var pos = pathInfo.LastIndexOf('.');
                if (pos >= 0)
                {
                    var format = pathInfo.Substring(pos + 1);
                    contentType = HostContext.ContentTypes.GetFormatContentType(format);
                    if (contentType != null)
                    {
                        pathInfo = pathInfo.Substring(0, pos);
                    }
                }
            }
            return pathInfo;
        }

        public IRestPath GetRestPath(string httpMethod, string pathInfo)
        {
            if (this.RestPath == null)
            {
                string contentType;
                this.RestPath = FindMatchingRestPath(httpMethod, pathInfo, out contentType);

                if (contentType != null)
                    ResponseContentType = contentType;
            }
            return this.RestPath;
        }

        public IRestPath RestPath { get; set; }

        // Set from SSHHF.GetHandlerForPathInfo()
        public string ResponseContentType { get; set; }

        public override bool RunAsAsync() => true;

        public override Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
        {
            try
            {
                var restPath = GetRestPath(httpReq.Verb, httpReq.PathInfo);
                if (restPath == null)
                {
                    return new NotSupportedException("No RestPath found for: " + httpReq.Verb + " " + httpReq.PathInfo)
                        .AsTaskException();
                }
                httpReq.SetRoute(restPath as RestPath);
                httpReq.OperationName = operationName = restPath.RequestType.GetOperationName();

                var appHost = HostContext.AppHost;
                if (appHost.ApplyPreRequestFilters(httpReq, httpRes))
                    return TypeConstants.EmptyTask;

                var callback = httpReq.GetJsonpCallback();
                var doJsonp = HostContext.Config.AllowJsonpRequests
                              && !string.IsNullOrEmpty(callback);

                if (ResponseContentType != null)
                    httpReq.ResponseContentType = ResponseContentType;

                appHost.AssertContentType(httpReq.ResponseContentType);

                var request = httpReq.Dto = CreateRequest(httpReq, restPath);

                return appHost.ApplyRequestFiltersAsync(httpReq, httpRes, request)
                    .Continue(t =>
                    {
                        if (t.IsFaulted)
                            return t;

                        if (t.IsCanceled)
                            httpRes.EndRequest();

                        if (httpRes.IsClosed)
                            return TypeConstants.EmptyTask;

                        var rawResponse = GetResponse(httpReq, request);

                        if (httpRes.IsClosed)
                            return TypeConstants.EmptyTask;

                        return HandleResponse(rawResponse, async response =>
                        {
                            UpdateResponseContentType(httpReq, response);
                            response = appHost.ApplyResponseConverters(httpReq, response);

                            await appHost.ApplyResponseFiltersAsync(httpReq, httpRes, response);
                            if (httpRes.IsClosed)
                                return;

                            if (httpReq.ResponseContentType.Contains("jsv") && !string.IsNullOrEmpty(httpReq.QueryString[Keywords.Debug]))
                            {
                                await WriteDebugResponse(httpRes, response);
                                return;
                            }

                            if (doJsonp && !(response is CompressedResult))
                            {
                                await httpRes.WriteToResponse(httpReq, response, (callback + "(").ToUtf8Bytes(), ")".ToUtf8Bytes());
                                return;
                            }

                            await httpRes.WriteToResponse(httpReq, response);
                        });
                    })
                    .Unwrap()
                    .Continue(t =>
                    {
                        if (t.IsFaulted)
                        {
                            var taskEx = t.Exception.UnwrapIfSingleException();
                            return !HostContext.Config.WriteErrorsToResponse
                                ? taskEx.ApplyResponseConverters(httpReq).AsTaskException()
                                : HandleException(httpReq, httpRes, operationName, taskEx.ApplyResponseConverters(httpReq));
                        }
                        return t;
                    })
                    .Unwrap();
            }
            catch (Exception ex)
            {
                return !HostContext.Config.WriteErrorsToResponse
                    ? ex.ApplyResponseConverters(httpReq).AsTaskException()
                    : HandleException(httpReq, httpRes, operationName, ex.ApplyResponseConverters(httpReq));
            }
        }

        public override object GetResponse(IRequest request, object requestDto)
        {
            var requestContentType = ContentFormat.GetEndpointAttributes(request.ResponseContentType);

            request.RequestAttributes |= HandlerAttributes | requestContentType;

            return ExecuteService(requestDto, request);
        }

        public static object CreateRequest(IRequest httpReq, IRestPath restPath)
        {
            using (Profiler.Current.Step("Deserialize Request"))
            {
                var dtoFromBinder = GetCustomRequestFromBinder(httpReq, restPath.RequestType);
                if (dtoFromBinder != null)
                    return HostContext.AppHost.ApplyRequestConverters(httpReq, dtoFromBinder);

                var requestParams = httpReq.GetFlattenedRequestParams();
                return HostContext.AppHost.ApplyRequestConverters(httpReq,
                    CreateRequest(httpReq, restPath, requestParams));
            }
        }

        public static object CreateRequest(IRequest httpReq, IRestPath restPath, Dictionary<string, string> requestParams)
        {
            var requestDto = CreateContentTypeRequest(httpReq, restPath.RequestType, httpReq.ContentType);

            return CreateRequest(httpReq, restPath, requestParams, requestDto);
        }

        public static object CreateRequest(IRequest httpReq, IRestPath restPath, Dictionary<string, string> requestParams, object requestDto)
        {
            string contentType;
            var pathInfo = !restPath.IsWildCardPath
                ? GetSanitizedPathInfo(httpReq.PathInfo, out contentType)
                : httpReq.PathInfo;

            return restPath.CreateRequest(pathInfo, requestParams, requestDto);
        }

        /// <summary>
        /// Used in Unit tests
        /// </summary>
        /// <returns></returns>
        public override object CreateRequest(IRequest httpReq, string operationName)
        {
            if (this.RestPath == null)
                throw new ArgumentNullException(nameof(RestPath), "No RestPath found");

            return CreateRequest(httpReq, this.RestPath);
        }
    }

}
