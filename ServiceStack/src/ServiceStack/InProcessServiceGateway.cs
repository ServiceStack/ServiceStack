using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using ServiceStack.Web;
using ServiceStack.Validation;
using ServiceStack.Logging;
using ServiceStack.Host;
using ServiceStack.Text;

namespace ServiceStack;

public partial class InProcessServiceGateway : IServiceGateway, IServiceGatewayAsync, IRequiresRequest
{
    protected static ILog Log = LogManager.GetLogger(typeof(InProcessServiceGateway));

    private IRequest req;
    public IRequest Request
    {
        get => req;
        set => req = value;
    }

    public InProcessServiceGateway(IRequest req)
    {
        this.req = req;
    }

    protected string SetVerb(object requestDto)
    {
        var hold = req.GetItem(Keywords.InvokeVerb) as string;
        if (requestDto is IVerb)
        {
            if (requestDto is IGet)
                req.SetItem(Keywords.InvokeVerb, HttpMethods.Get);
            if (requestDto is IPost)
                req.SetItem(Keywords.InvokeVerb, HttpMethods.Post);
            if (requestDto is IPut)
                req.SetItem(Keywords.InvokeVerb, HttpMethods.Put);
            if (requestDto is IDelete)
                req.SetItem(Keywords.InvokeVerb, HttpMethods.Delete);
            if (requestDto is IPatch)
                req.SetItem(Keywords.InvokeVerb, HttpMethods.Patch);
            if (requestDto is IOptions)
                req.SetItem(Keywords.InvokeVerb, HttpMethods.Options);
        }
        return hold;
    }

    protected void ResetVerb(string verb)
    {
        if (verb == null)
            req.Items.Remove(Keywords.InvokeVerb);
        else
            req.SetItem(Keywords.InvokeVerb, verb);
    }

    protected virtual void InitRequest(object request)
    {
        req.Dto = request;
        // Simulate an IHasQueryParams DTO which populates ?queryString in C# .NET Clients, by populating DTO here
        if (request is IHasQueryParams hasQueryParams && hasQueryParams.QueryParams?.Count > 0)
        {
            hasQueryParams.QueryParams.PopulateInstance(request);
        }
    }

    protected virtual TResponse ExecSync<TResponse>(object request)
    {
        if (Request is IConvertRequest convertRequest)
            request = convertRequest.Convert(request);

        foreach (var filter in HostContext.AppHost.GatewayRequestFiltersArray)
        {
            filter(req, request);
            if (req.Response.IsClosed)
                return default;
        }
        foreach (var filter in HostContext.AppHost.GatewayRequestFiltersAsyncArray)
        {
            filter(req, request).Wait();
            if (req.Response.IsClosed)
                return default;
        }

        ExecValidatorsAsync(request).Wait();

        var response = HostContext.ServiceController.Execute(request, req);
        response = UnwrapResponse(response);

        if (response is Task[] batchResponseTasks)
        {
            Task.WaitAll(batchResponseTasks);
            var to = new object[batchResponseTasks.Length];
            for (int i = 0; i < batchResponseTasks.Length; i++)
            {
                to[i] = batchResponseTasks[i].GetResult();
            }
            response = to.ConvertTo<TResponse>();
        }

        var responseDto = ConvertToResponse<TResponse>(response);

        foreach (var filter in HostContext.AppHost.GatewayResponseFiltersArray)
        {
            filter(req, responseDto);
            if (req.Response.IsClosed)
                return default;
        }
        foreach (var filter in HostContext.AppHost.GatewayResponseFiltersAsyncArray)
        {
            filter(req, responseDto).Wait();
            if (req.Response.IsClosed)
                return default;
        }

        return responseDto;
    }

    protected virtual async Task<TResponse> ExecAsync<TResponse>(object request)
    {
        if (Request is IConvertRequest convertRequest)
            request = convertRequest.Convert(request);

        var appHost = HostContext.AppHost;
        if (!await appHost.ApplyGatewayRequestFiltersAsync(req, request)) 
            return default;

        await ExecValidatorsAsync(request);

        var response = await HostContext.ServiceController.GatewayExecuteAsync(request, req, applyFilters: false);

        var responseDto = await ConvertToResponseAsync<TResponse>(response).ConfigAwait();

        if (!await appHost.ApplyGatewayRespoonseFiltersAsync(req, responseDto))
            return default;

        return responseDto;
    }

    protected virtual Task ExecValidatorsAsync(object request) => HostContext.ServiceController.ExecValidatorsAsync(request, req);

    public virtual object UnwrapResponse(object response)
    {
        if (response is Task responseTask)
            return responseTask.GetResult();
        if (response is ValueTask<object> valueTaskResponse)
            return valueTaskResponse.GetAwaiter().GetResult();
        if (response is ValueTask valueTaskVoid)
        {
            valueTaskVoid.GetAwaiter().GetResult();
            return null;
        }
        return response;
    }

    public virtual async Task<object> UnwrapResponseAsync(object response)
    {
        if (response is Task<object> responseTaskObject)
            return await responseTaskObject;

        if (response is Task responseTask)
        {
            await responseTask;
            return responseTask.GetResult();
        }
        if (response is ValueTask<object> valueTaskResponse)
            return await valueTaskResponse;
        if (response is ValueTask valueTaskVoid)
        {
            await valueTaskVoid;
            return null;
        }

        return response;
    }

    public virtual async Task<TResponse> ConvertToResponseAsync<TResponse>(object response)
    {
        if (response is Task task)
        {
            await task;
            response = task.GetResult();
        }
        return ConvertToResponse<TResponse>(response);
    }

    public static Type[] ConvertibleTypes { get; set; } = new[]
    {
        typeof(EmptyResponse),
        typeof(IdResponse),
        typeof(ErrorResponse),
    };

    public virtual TResponse ConvertToResponse<TResponse>(object response)
    {
        if (response is HttpError error)
            throw error.ToWebServiceException();

        // Ensure Response Cookies are added before eliding the HttpResult
        if (response is IHttpResult httpResult && req.Response is IHttpResponse httpRes)
        {
            foreach (var cookie in httpResult.Cookies)
            {
                httpRes.Cookies.Collection.Add(cookie);
            }
        }

        var responseDto = response.GetResponseDto();
        if (responseDto == null)
            return default;
        if (responseDto is TResponse typedResponse)
            return typedResponse;
        
        if (typeof(TResponse) == typeof(byte[]))
        {
            if (responseDto is System.IO.Stream stream)
                return (TResponse)(object)stream.ReadFully();
            if (responseDto is string str)
                return (TResponse)(object)str.ToUtf8Bytes();

            var json = responseDto.ToJson(); // IReturnVoid
            return (TResponse)(object)json.ToUtf8Bytes();
        }
        if (typeof(TResponse) == typeof(string))
        {
            var json = responseDto.ToJson();
            return (TResponse)(object)json;
        }
        
        if (!ConvertibleTypes.Contains(responseDto.GetType()) && !ConvertibleTypes.Contains(typeof(TResponse)))
            Log.WarnFormat($"Gateway ConvertToResponse: {0} is not of type {1}", responseDto.GetType().Name, typeof(TResponse).Name);
        
        return responseDto.ConvertTo<TResponse>();
    }

    public virtual TResponse Send<TResponse>(object requestDto)
    {
        var holdDto = req.Dto;
        var holdOp = req.OperationName;
        var holdAttrs = req.RequestAttributes;
        var holdVerb = SetVerb(requestDto);
        InitRequest(requestDto);

        req.RequestAttributes |= RequestAttributes.InProcess;

        var id = Diagnostics.ServiceStack.WriteGatewayBefore(req);
        Exception e = null;
        try
        {
            return ExecSync<TResponse>(requestDto);
        }
        catch (AggregateException ae)
        {
            e = ae.UnwrapIfSingleException();
            HostContext.RaiseGatewayException(req, requestDto, e).Wait();
            throw e;
        }
        catch (Exception ex)
        {
            e = ex;
            HostContext.RaiseGatewayException(req, requestDto, ex).Wait();
            throw;
        }
        finally
        {
            if (e != null)
                Diagnostics.ServiceStack.WriteGatewayError(id, req, e);
            else
                Diagnostics.ServiceStack.WriteGatewayAfter(id, req);
            
            req.Dto = holdDto;
            req.OperationName = holdOp;
            req.RequestAttributes = holdAttrs;
            ResetVerb(holdVerb);
        }
    }

    public virtual async Task<TResponse> SendAsync<TResponse>(object requestDto, CancellationToken token = new CancellationToken())
    {
        var holdDto = req.Dto;
        var holdOp = req.OperationName;
        var holdVerb = SetVerb(requestDto);
        var holdAttrs = req.RequestAttributes;
        InitRequest(requestDto);

        req.SetInProcessRequest();

        var id = Diagnostics.ServiceStack.WriteGatewayBefore(req);
        Exception e = null;
        try
        {
            var response = await ExecAsync<TResponse>(requestDto);
            return response;
        }
        catch (Exception ex)
        {
            e = ex;
            await HostContext.RaiseGatewayException(req, requestDto, ex);
            throw;
        }
        finally
        {
            if (e != null)
                Diagnostics.ServiceStack.WriteGatewayError(id, req, e);
            else
                Diagnostics.ServiceStack.WriteGatewayAfter(id, req);
            
            req.Dto = holdDto;
            req.OperationName = holdOp;
            req.RequestAttributes = holdAttrs;
            ResetVerb(holdVerb);
        }
    }

    protected static object[] CreateTypedArray(IEnumerable<object> requestDtos)
    {
        var requestsArray = requestDtos.ToArray();
        var elType = requestDtos.GetType().GetCollectionType();
        var toArray = (object[])Array.CreateInstance(elType, requestsArray.Length);
        for (int i = 0; i < requestsArray.Length; i++)
        {
            toArray[i] = requestsArray[i];
        }
        return toArray;
    }

    public virtual List<TResponse> SendAll<TResponse>(IEnumerable<object> requestDtos)
    {
        var holdDto = req.Dto;
        string holdVerb = req.GetItem(Keywords.InvokeVerb) as string;
        var holdAttrs = req.RequestAttributes;

        var typedArray = CreateTypedArray(requestDtos);
        req.SetItem(Keywords.InvokeVerb, HttpMethods.Post);
        req.SetInProcessRequest();

        var id = Diagnostics.ServiceStack.WriteGatewayBefore(req);
        Exception e = null;
        try
        {
            return ExecSync<TResponse[]>(typedArray).ToList();
        }
        catch (AggregateException ae)
        {
            e = ae.UnwrapIfSingleException();
            HostContext.RaiseGatewayException(req, requestDtos, e).Wait();
            throw e;
        }
        catch (Exception ex)
        {
            e = ex;
            HostContext.RaiseGatewayException(req, requestDtos, ex).Wait();
            throw;
        }
        finally
        {
            if (e != null)
                Diagnostics.ServiceStack.WriteGatewayError(id, req, e);
            else
                Diagnostics.ServiceStack.WriteGatewayAfter(id, req);
            
            req.Dto = holdDto;
            req.RequestAttributes = holdAttrs;
            ResetVerb(holdVerb);
        }
    }

    public virtual async Task<List<TResponse>> SendAllAsync<TResponse>(IEnumerable<object> requestDtos, CancellationToken token = new CancellationToken())
    {
        var holdDto = req.Dto;
        var holdAttrs = req.RequestAttributes;
        string holdVerb = req.GetItem(Keywords.InvokeVerb) as string;

        var typedArray = CreateTypedArray(requestDtos);
        req.SetItem(Keywords.InvokeVerb, HttpMethods.Post);
        req.SetInProcessRequest();

        var id = Diagnostics.ServiceStack.WriteGatewayBefore(req);
        Exception e = null;
        try
        {
            var response = await ExecAsync<TResponse[]>(typedArray);
            return response.ToList();
        }
        catch (Exception ex)
        {
            e = ex;
            await HostContext.RaiseGatewayException(req, requestDtos, ex);
            throw;
        }
        finally
        {
            if (e != null)
                Diagnostics.ServiceStack.WriteGatewayError(id, req, e);
            else
                Diagnostics.ServiceStack.WriteGatewayAfter(id, req);
            
            req.Dto = holdDto;
            req.RequestAttributes = holdAttrs;
            ResetVerb(holdVerb);
        }
    }

    public virtual void Publish(object request)
    {
        if (Request is IConvertRequest convertRequest)
            request = convertRequest.Convert(request);

        var holdDto = req.Dto;
        var holdOp = req.OperationName;
        var holdAttrs = req.RequestAttributes;
        var holdVerb = SetVerb(request);
        InitRequest(request);

        req.RequestAttributes &= ~RequestAttributes.Reply;
        req.RequestAttributes |= RequestAttributes.OneWay;
        req.RequestAttributes |= RequestAttributes.InProcess;

        var id = Diagnostics.ServiceStack.WriteGatewayBefore(req);
        Exception e = null;
        try
        {
            var response = HostContext.ServiceController.Execute(request, req);
        }
        catch (Exception ex)
        {
            e = ex;
            HostContext.RaiseGatewayException(req, request, ex).Wait();
            throw;
        }
        finally
        {
            if (e != null)
                Diagnostics.ServiceStack.WriteGatewayError(id, req, e);
            else
                Diagnostics.ServiceStack.WriteGatewayAfter(id, req);
            
            req.Dto = holdDto;
            req.OperationName = holdOp;
            req.RequestAttributes = holdAttrs;
            ResetVerb(holdVerb);
        }
    }

    public virtual async Task PublishAsync(object request, CancellationToken token = new CancellationToken())
    {
        if (Request is IConvertRequest convertRequest)
            request = convertRequest.Convert(request);

        var holdDto = req.Dto;
        var holdOp = req.OperationName;
        var holdAttrs = req.RequestAttributes;
        var holdVerb = SetVerb(request);
        InitRequest(request);

        req.RequestAttributes &= ~RequestAttributes.Reply;
        req.RequestAttributes |= RequestAttributes.OneWay;
        req.RequestAttributes |= RequestAttributes.InProcess;

        var id = Diagnostics.ServiceStack.WriteGatewayBefore(req);
        Exception e = null;
        try
        {
            await HostContext.ServiceController.GatewayExecuteAsync(request, req, applyFilters: false);
        }
        catch (Exception ex)
        {
            e = ex;
            await HostContext.RaiseGatewayException(req, request, ex);
            throw;
        }
        finally
        {
            if (e != null)
                Diagnostics.ServiceStack.WriteGatewayError(id, req, e);
            else
                Diagnostics.ServiceStack.WriteGatewayAfter(id, req);
            
            req.Dto = holdDto;
            req.OperationName = holdOp;
            req.RequestAttributes = holdAttrs;
            ResetVerb(holdVerb);
        }
    }

    public virtual void PublishAll(IEnumerable<object> requestDtos)
    {
        if (Request is IConvertRequest convertRequest)
            requestDtos = convertRequest.Convert(requestDtos);

        var holdDto = req.Dto;
        var holdAttrs = req.RequestAttributes;
        string holdVerb = req.GetItem(Keywords.InvokeVerb) as string;

        var typedArray = CreateTypedArray(requestDtos);
        req.SetItem(Keywords.InvokeVerb, HttpMethods.Post);
        req.RequestAttributes &= ~RequestAttributes.Reply;
        req.RequestAttributes |= RequestAttributes.OneWay;
        req.RequestAttributes |= RequestAttributes.InProcess;

        var id = Diagnostics.ServiceStack.WriteGatewayBefore(req);
        Exception e = null;
        try
        {
            var response = HostContext.ServiceController.Execute(typedArray, req);
        }
        catch (Exception ex)
        {
            e = ex;
            HostContext.RaiseGatewayException(req, requestDtos, ex).Wait();
            throw;
        }
        finally
        {
            if (e != null)
                Diagnostics.ServiceStack.WriteGatewayError(id, req, e);
            else
                Diagnostics.ServiceStack.WriteGatewayAfter(id, req);
            
            req.Dto = holdDto;
            req.RequestAttributes = holdAttrs;
            ResetVerb(holdVerb);
        }
    }

    public virtual async Task PublishAllAsync(IEnumerable<object> requestDtos, CancellationToken token = new CancellationToken())
    {
        if (Request is IConvertRequest convertRequest)
            requestDtos = convertRequest.Convert(requestDtos);

        var holdDto = req.Dto;
        var holdAttrs = req.RequestAttributes;
        string holdVerb = req.GetItem(Keywords.InvokeVerb) as string;

        var typedArray = CreateTypedArray(requestDtos);
        req.SetItem(Keywords.InvokeVerb, HttpMethods.Post);
        req.RequestAttributes &= ~RequestAttributes.Reply;
        req.RequestAttributes |= RequestAttributes.OneWay;

        var id = Diagnostics.ServiceStack.WriteGatewayBefore(req);
        Exception e = null;
        try
        {
            await HostContext.ServiceController.GatewayExecuteAsync(typedArray, req, applyFilters: false);
        }
        catch (Exception ex)
        {
            e = ex;
            await HostContext.RaiseGatewayException(req, requestDtos, ex);
            throw;
        }
        finally
        {
            if (e != null)
                Diagnostics.ServiceStack.WriteGatewayError(id, req, e);
            else
                Diagnostics.ServiceStack.WriteGatewayAfter(id, req);
            
            req.Dto = holdDto;
            req.RequestAttributes = holdAttrs;
            ResetVerb(holdVerb);
        }
    }
}

#if NET6_0_OR_GREATER
public partial class InProcessServiceGateway : IServiceGatewayFormAsync, ICloneServiceGateway
{
    public IServiceGateway Clone()
    {
        if (Request is ICloneable cloneable)
        {
            var ret = new InProcessServiceGateway((IRequest)cloneable.Clone());
            // Need to retain cookies reference to preserve auth in Blazor Server
            if (ret.Request is GatewayRequest httpReq)
            {
                httpReq.Cookies = Request.Cookies;
            }
            return ret;
        }
        return this;
    }

    public async Task<TResponse> SendFormAsync<TResponse>(object requestDto, System.Net.Http.MultipartFormDataContent formData, CancellationToken token = default)
    {
        var holdDto = req.Dto;
        var holdOp = req.OperationName;
        var holdVerb = SetVerb(requestDto);
        var holdAttrs = req.RequestAttributes;
        InitRequest(requestDto);

        var requestType = requestDto.GetType();
        var typeProps = TypeProperties.Get(requestType);
        var httpFiles = new List<HttpFileContent>();
        foreach (System.Net.Http.HttpContent entry in formData)
        {
            string propName = null;
            string strValue = null;
            try
            {                
                propName = entry.Headers.ContentDisposition?.Name?.StripQuotes();
                if (propName == null)
                    continue;

                if (entry.Headers.ContentDisposition?.FileName != null)
                {
                    httpFiles.Add(new HttpFileContent(entry));
                }
                else
                {
                    var contentType = entry.Headers.ContentType?.MediaType ?? MimeTypes.Jsv;
                    strValue = await entry.ReadAsStringAsync();
                    if (strValue == null)
                        continue;
                    var accessor = typeProps.GetAccessor(propName);
                    if (accessor != null)
                    {
                        var value = contentType.MatchesContentType(MimeTypes.PlainText) 
                            ? strValue.ConvertTo(accessor.PropertyInfo.PropertyType)
                            : HostContext.ContentTypes.DeserializeFromString(contentType, accessor.PropertyInfo.PropertyType, strValue);
                        accessor.PublicSetter(requestDto, value);
                    }
                    req.FormData[propName] = strValue;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not populate {0}.{1} with: {2}", requestType.Name, propName, strValue.SafeSubstring(0, 100));
            }
        }
        if (httpFiles.Count > 0 && req is BasicRequest basicRequest)
        {
            basicRequest.Files = httpFiles.ToArray();
        }

        req.SetInProcessRequest();

        var id = Diagnostics.ServiceStack.WriteGatewayBefore(req);
        Exception e = null;
        try
        {
            var response = await ExecAsync<TResponse>(requestDto);
            return response;
        }
        catch (Exception ex)
        {
            e = ex;
            await HostContext.RaiseGatewayException(req, requestDto, ex);
            throw;
        }
        finally
        {
            if (e != null)
                Diagnostics.ServiceStack.WriteGatewayError(id, req, e);
            else
                Diagnostics.ServiceStack.WriteGatewayAfter(id, req);

            req.Dto = holdDto;
            req.OperationName = holdOp;
            req.RequestAttributes = holdAttrs;
            ResetVerb(holdVerb);
        }
    }
}

#endif