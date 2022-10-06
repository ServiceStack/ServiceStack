using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;

namespace ServiceStack.Blazor;

/// <summary>
/// Blazor com
/// </summary>
public class BlazorComponentBase : ComponentBase, IHasJsonApiClient
{
    [Inject] ILogger<BlazorComponentBase> Log { get; set; }

    [Inject] public JsonApiClient? Client { get; set; }

    public async virtual Task<ApiResult<TResponse>> ApiAsync<TResponse>(IReturn<TResponse> request)
    {
        Stopwatch? sw = null;
        if (EnableLogging)
        {
            sw = Stopwatch.StartNew();
            log("API {0}", request.GetType().Name);
        }

        var ret = await JsonApiClientUtils.ApiAsync(this, request);

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

        var ret = await JsonApiClientUtils.SendAsync(this, request);

        if (EnableLogging)
        {
            log("END SendAsync {0} took {1}ms", request.GetType().Name, sw!.ElapsedMilliseconds);
        }
        return ret;
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

        var ret = await JsonApiClientUtils.ApiAppMetadataAsync(this);

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

