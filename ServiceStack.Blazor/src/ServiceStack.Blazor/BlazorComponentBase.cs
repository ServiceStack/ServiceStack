using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using System.Diagnostics;
using System.Linq;

namespace ServiceStack.Blazor;

/// <summary>
/// Blazor component base
/// </summary>
public class BlazorComponentBase : ComponentBase, IHasJsonApiClient
{
    [Inject] ILogger<BlazorComponentBase> Log { get; set; }

    [Inject] public JsonApiClient? Client { get; set; }

    protected virtual async Task OnApiErrorAsync(object requestDto, IHasErrorStatus apiError)
    {
        if (BlazorConfig.Instance.OnApiErrorAsync != null)
            await BlazorConfig.Instance.OnApiErrorAsync(requestDto, apiError);
    }

    public string GetDetailedError(ResponseStatus status)
    {
        var sb = StringBuilderCache.Allocate();
        sb.AppendLine($"{status.ErrorCode} {status.Message}");
        foreach (var error in status.Errors.OrEmpty())
        {
            sb.AppendLine($" - {error.FieldName}: {error.ErrorCode} {error.Message}");
        }
        if (!string.IsNullOrEmpty(status.StackTrace))
        {
            sb.AppendLine("StackTrace:");
            sb.AppendLine(status.StackTrace);
        }

        return StringBuilderCache.ReturnAndFree(sb);
    }

    public async virtual Task<ApiResult<TResponse>> ApiAsync<TResponse>(IReturn<TResponse> request)
    {
        Stopwatch? sw = null;
        if (EnableLogging)
        {
            sw = Stopwatch.StartNew();
            log("API {0}", request.GetType().Name);
        }

        var ret = await JsonApiClientUtils.ApiAsync(this, request);
        if (ret.Error != null)
        {
            if (BlazorConfig.Instance.EnableErrorLogging)
            {
                log("ERROR {0}:\n{1}", request.GetType().Name, GetDetailedError(ret.Error));
            }
            await OnApiErrorAsync(request, ret);
        }

        if (EnableLogging)
        {
            log("END {0} took {1}ms", request.GetType().Name, sw!.ElapsedMilliseconds);
        }
        return ret;
    }

    public async virtual Task<ApiResult<EmptyResponse>> ApiAsync(IReturnVoid request)
    {
        Stopwatch? sw = null;
        if (EnableLogging)
        {
            sw = Stopwatch.StartNew();
            log("API void {0}", request.GetType().Name);
        }

        var ret = await JsonApiClientUtils.ApiAsync(this, request);
        if (ret.Error != null)
        {
            if (BlazorConfig.Instance.EnableErrorLogging)
            {
                log("ERROR {0}:\n{1}", request.GetType().Name, GetDetailedError(ret.Error));
            }
            await OnApiErrorAsync(request, ret);
        }

        if (EnableLogging)
        {
            log("END void {0} took {1}ms", request.GetType().Name, sw!.ElapsedMilliseconds);
        }
        return ret;
    }

    public async virtual Task<TResponse> SendAsync<TResponse>(IReturn<TResponse> request)
    {
        Stopwatch? sw = null;
        if (EnableLogging)
        {
            sw = Stopwatch.StartNew();
            log("API SendAsync {0}", request.GetType().Name);
        }

        try
        {
            var ret = await JsonApiClientUtils.SendAsync(this, request);
            if (EnableLogging)
            {
                log("END SendAsync {0} took {1}ms", request.GetType().Name, sw!.ElapsedMilliseconds);
            }
            return ret;
        }
        catch (Exception e)
        {
            if (BlazorConfig.Instance.EnableErrorLogging)
            {
                var status = e.GetResponseStatus();
                log("ERROR {0}:\n{1}", request.GetType().Name, status != null ? GetDetailedError(status) : e.ToString());
            }
            throw;
        }
    }

    public async virtual Task<IHasErrorStatus> ApiAsync<Model>(object request)
    {
        Stopwatch? sw = null;
        if (EnableLogging)
        {
            sw = Stopwatch.StartNew();
            log("API object {0}", request.GetType().Name);
        }

        var ret = await JsonApiClientUtils.ApiAsync<Model>(this, request);
        if (ret.Error != null)
        {
            if (BlazorConfig.Instance.EnableErrorLogging)
            {
                log("ERROR {0}:\n{1}", request.GetType().Name, GetDetailedError(ret.Error));
            }
            await OnApiErrorAsync(request, ret);
        }

        if (EnableLogging)
        {
            log("END object {0} took {1}ms", request.GetType().Name, sw!.ElapsedMilliseconds);
        }
        return ret;
    }

    public async virtual Task<IHasErrorStatus> ApiFormAsync<Model>(string method, string relativeUrl, MultipartFormDataContent request)
    {
        Stopwatch? sw = null;
        if (EnableLogging)
        {
            sw = Stopwatch.StartNew();
            log("API Form {0}", request.GetType().Name);
        }

        var ret = await JsonApiClientUtils.ApiFormAsync<Model>(this, method, relativeUrl, request);
        if (ret.Error != null)
        {
            if (BlazorConfig.Instance.EnableErrorLogging)
            {
                log("ERROR {0}:\n{1}", request.GetType().Name, GetDetailedError(ret.Error));
            }
            await OnApiErrorAsync(request, ret);
        }

        if (EnableLogging)
        {
            log("END Form {0} took {1}ms", request.GetType().Name, sw!.ElapsedMilliseconds);
        }
        return ret;
    }

    public async virtual Task<IHasErrorStatus> ApiFormAsync<Model>(string relativeUrl, MultipartFormDataContent request)
    {
        Stopwatch? sw = null;
        if (EnableLogging)
        {
            sw = Stopwatch.StartNew();
            log("API Form {0}", request.GetType().Name);
        }

        var ret = await JsonApiClientUtils.ApiFormAsync<Model>(this, relativeUrl, request);
        if (ret.Error != null)
        {
            if (BlazorConfig.Instance.EnableErrorLogging)
            {
                log("ERROR {0}:\n{1}", request.GetType().Name, GetDetailedError(ret.Error));
            }
            await OnApiErrorAsync(request, ret);
        }

        if (EnableLogging)
        {
            log("END Form {0} took {1}ms", request.GetType().Name, sw!.ElapsedMilliseconds);
        }
        return ret;
    }

    public static string ClassNames(params string?[] classes) => CssUtils.ClassNames(classes);
    public async virtual Task<ApiResult<AppMetadata>> ApiAppMetadataAsync()
    {
        Stopwatch? sw = null;
        if (EnableLogging)
        {
            sw = Stopwatch.StartNew();
            log("API ApiAppMetadataAsync");
        }

        var request = new MetadataApp();
        var ret = await JsonApiClientUtils.ApiCacheAsync(this, request);
        if (ret.Error != null)
        {
            if (BlazorConfig.Instance.EnableErrorLogging)
            {
                log("ERROR AppMetadata:\n{0}", GetDetailedError(ret.Error));
            }
            await OnApiErrorAsync(request, ret);
        }

        if (EnableLogging)
        {
            log("END ApiAppMetadataAsync took {0}ms", sw!.ElapsedMilliseconds);
        }
        return ret;
    }

    bool? enableLogging;
    protected virtual bool EnableLogging => enableLogging ??= BlazorConfig.Instance.EnableVerboseLogging;
    protected virtual void log(string? message, params object?[] args)
    {
        if (EnableLogging)
            Log.LogDebug(message, args);
    }
}

