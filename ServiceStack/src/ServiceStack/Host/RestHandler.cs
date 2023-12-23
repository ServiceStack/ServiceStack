using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.Host.Handlers;
using ServiceStack.MiniProfiler;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host;

public class RestHandler
    : ServiceStackHandlerBase, IRequestHttpHandler
{
    public RestHandler()
    {
        this.HandlerAttributes = RequestAttributes.Reply;
    }

    public static IRestPath FindMatchingRestPath(IHttpRequest httpReq, out string contentType)
    {
        var pathInfo = GetSanitizedPathInfo(httpReq.PathInfo, out contentType);
        return HostContext.ServiceController.GetRestPathForRequest(httpReq.HttpMethod, pathInfo, httpReq);
    }

    public static IRestPath FindMatchingRestPath(string httpMethod, string pathInfo, out string contentType)
    {
        pathInfo = GetSanitizedPathInfo(pathInfo, out contentType);
        return HostContext.ServiceController.GetRestPathForRequest(httpMethod, pathInfo, null);
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

    public IRestPath GetRestPath(IHttpRequest httpReq)
    {
        if (this.RestPath == null)
        {
            this.RestPath = FindMatchingRestPath(httpReq, out var contentType);

            if (contentType != null)
                ResponseContentType = contentType;
        }
        return this.RestPath;
    }

    public IRestPath RestPath { get; set; }

    // Set from SSHHF.GetHandlerForPathInfo()
    public string ResponseContentType { get; set; }

    public override bool RunAsAsync() => true;

    public override async Task ProcessRequestAsync(IRequest req, IResponse httpRes, string operationName)
    {
        var httpReq = (IHttpRequest) req;
        try
        {
            var restPath = GetRestPath(httpReq);
            if (restPath == null)
                throw new NotSupportedException("No RestPath found for: " + httpReq.Verb + " " + httpReq.PathInfo);

            httpReq.SetRoute(restPath as RestPath);
            httpReq.OperationName = operationName = restPath.RequestType.GetOperationName();

            if (appHost.ApplyPreRequestFilters(httpReq, httpRes))
                return;

            if (ResponseContentType != null)
                httpReq.ResponseContentType = ResponseContentType;

            appHost.AssertContentType(httpReq.ResponseContentType);

            var request = httpReq.Dto = await CreateRequestAsync(httpReq, restPath).ConfigAwaitNetCore();
            HostContext.AppHost.OnAfterAwait(httpReq);

            await appHost.ApplyRequestFiltersAsync(httpReq, httpRes, request).ConfigAwaitNetCore();
            HostContext.AppHost.OnAfterAwait(httpReq);
            if (httpRes.IsClosed)
                return;

            var requestContentType = ContentFormat.GetEndpointAttributes(httpReq.ResponseContentType);
            httpReq.RequestAttributes |= HandlerAttributes | requestContentType;

            var rawResponse = await GetResponseAsync(httpReq, request).ConfigAwaitNetCore();
            HostContext.AppHost.OnAfterAwait(httpReq);
            if (httpRes.IsClosed)
                return;

            await HandleResponse(httpReq, httpRes, rawResponse).ConfigAwaitNetCore();
            HostContext.AppHost.OnAfterAwait(httpReq);
        }
        //sync with GenericHandler
        catch (TaskCanceledException)
        {
            httpRes.StatusCode = (int)HttpStatusCode.PartialContent;
            httpRes.EndRequest();
        }
        catch (Exception ex)
        {
            if (!appHost.Config.WriteErrorsToResponse)
            {
                await appHost.ApplyResponseConvertersAsync(httpReq, ex).ConfigAwait();
            }
            else
            {
                var useEx = await appHost.ApplyResponseConvertersAsync(httpReq, ex).ConfigAwait() as Exception ?? ex;
                await HandleException(httpReq, httpRes, operationName, useEx).ConfigAwait();
            }
        }
    }

    public static async Task<object> CreateRequestAsync(IRequest httpReq, IRestPath restPath)
    {
        using var step = Profiler.Current.Step("Deserialize Request");
        var dtoFromBinder = GetCustomRequestFromBinder(httpReq, restPath.RequestType);
        if (dtoFromBinder != null)
        {
            var ret = await HostContext.AppHost.ApplyRequestConvertersAsync(httpReq, dtoFromBinder).ConfigAwaitNetCore();
            HostContext.AppHost.OnAfterAwait(httpReq);
            return ret;
        }
        else
        {
            var requestParams = httpReq.GetFlattenedRequestParams();
            if (Log.IsDebugEnabled)
                Log.DebugFormat("CreateRequestAsync/requestParams:" + string.Join(",", requestParams.Keys));

            var requestDto = await CreateRequestAsync(httpReq, restPath, requestParams).ConfigAwaitNetCore();
            HostContext.AppHost.OnAfterAwait(httpReq);

            var ret = await HostContext.AppHost.ApplyRequestConvertersAsync(httpReq, requestDto).ConfigAwaitNetCore();
            HostContext.AppHost.OnAfterAwait(httpReq);
            return ret;
        }
    }

    public static async Task<object> CreateRequestAsync(IRequest httpReq, IRestPath restPath, Dictionary<string, string> requestParams)
    {
        var requestDto = await CreateContentTypeRequestAsync(httpReq, restPath.RequestType, httpReq.ContentType).ConfigAwaitNetCore();
        HostContext.AppHost.OnAfterAwait(httpReq);

        return CreateRequest(httpReq, restPath, requestParams, requestDto);
    }

    public static object CreateRequest(IRequest httpReq, IRestPath restPath, Dictionary<string, string> requestParams, object requestDto)
    {
        var pathInfo = !restPath.IsWildCardPath
            ? GetSanitizedPathInfo(httpReq.PathInfo, out _)
            : httpReq.PathInfo;

        return restPath.CreateRequest(pathInfo, requestParams, requestDto);
    }

    /// <summary>
    /// Used in Unit tests
    /// </summary>
    /// <returns></returns>
    public Task<object> CreateRequestAsync(IRequest httpReq, string operationName)
    {
        if (this.RestPath == null)
            throw new ArgumentNullException(nameof(RestPath), "No RestPath found");

        return CreateRequestAsync(httpReq, this.RestPath);
    }
}
