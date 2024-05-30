using System;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.MiniProfiler;
using ServiceStack.Serialization;
using ServiceStack.Web;
using ServiceStack.Text;

namespace ServiceStack.Host.Handlers;

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

    public async Task<object> CreateRequestAsync(IRequest req, string operationName)
    {
        var requestType = GetOperationType(operationName);

        AssertOperationExists(operationName, requestType);

        using var step = Profiler.Current.Step("Deserialize Request");
        var requestDto = GetCustomRequestFromBinder(req, requestType)
                         ?? (await DeserializeHttpRequestAsync(requestType, req, HandlerContentType).ConfigAwaitNetCore()
                             ?? requestType.CreateInstance());

        // Override Default Request DTO Properties with any QueryString Params
        if (req.QueryString.Count > 0 && HttpUtils.HasRequestBody(req.Verb))
        {
            var typeSerializer = KeyValueDataContractDeserializer.Instance.GetOrAddStringMapTypeDeserializer(requestType);
            foreach (var key in req.QueryString.AllKeys)
            {
                if (key == null) continue; //.NET Framework NameValueCollection can contain null keys
                var value = req.QueryString[key];
                if (string.IsNullOrEmpty(value)) continue;
                
                var propSerializer = typeSerializer.GetPropertySerializer(key);
                if (propSerializer is { PropertyGetFn: not null, PropertySetFn: not null, PropertyParseStringFn: not null })
                {
                    var dtoValue = propSerializer.PropertyGetFn(requestDto);
                    if (dtoValue == null || dtoValue.Equals(propSerializer.PropertyType.GetDefaultValue()))
                    {
                        var qsValue = propSerializer.PropertyParseStringFn(value);
                        propSerializer.PropertySetFn(requestDto, qsValue);
                    }
                }
            }
        }
        
        HostContext.AppHost.OnAfterAwait(req);
        var ret = await appHost.ApplyRequestConvertersAsync(req, requestDto).ConfigAwaitNetCore();
        HostContext.AppHost.OnAfterAwait(req);
        return ret;
    }

    public override bool RunAsAsync() => true;

    public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
    {
        try
        {
            appHost.AssertFeatures(format);

            if (appHost.ApplyPreRequestFilters(httpReq, httpRes))
                return;

            httpReq.ResponseContentType = httpReq.GetQueryStringContentType() ?? this.HandlerContentType;

            var request = httpReq.Dto = await CreateRequestAsync(httpReq, operationName).ConfigAwaitNetCore();
            HostContext.AppHost.OnAfterAwait(httpReq);

            await appHost.ApplyRequestFiltersAsync(httpReq, httpRes, request).ConfigAwaitNetCore();
            HostContext.AppHost.OnAfterAwait(httpReq);
            if (httpRes.IsClosed)
                return;

            httpReq.RequestAttributes |= HandlerAttributes;

            var rawResponse = await GetResponseAsync(httpReq, request).ConfigAwaitNetCore();
            HostContext.AppHost.OnAfterAwait(httpReq);
            if (httpRes.IsClosed)
                return;

            await HandleResponse(httpReq, httpRes, rawResponse).ConfigAwaitNetCore();
            HostContext.AppHost.OnAfterAwait(httpReq);
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
                await HostContext.AppHost.ApplyResponseConvertersAsync(httpReq, ex).ConfigAwait();
            }
            else
            {
                var useEx = await HostContext.AppHost.ApplyResponseConvertersAsync(httpReq, ex).ConfigAwait() as Exception ?? ex;
                await HandleException(httpReq, httpRes, operationName, useEx).ConfigAwait();
            }
        }
    }

}