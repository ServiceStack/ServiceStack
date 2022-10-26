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

    public virtual Task<ApiResult<TResponse>> ApiAsync<TResponse>(IReturn<TResponse> request) => Client!.ManagedApiAsync(request);

    public virtual Task<ApiResult<EmptyResponse>> ApiAsync(IReturnVoid request) => Client!.ManagedApiAsync(request);

    public virtual Task<TResponse> SendAsync<TResponse>(IReturn<TResponse> request) => Client!.ManagedSendAsync(request);

    public virtual Task<IHasErrorStatus> ApiAsync<Model>(object request) => Client!.ManagedApiAsync<Model>(request);

    public virtual Task<IHasErrorStatus> ApiFormAsync<Model>(string method, string relativeUrl, MultipartFormDataContent request) =>
        Client!.ManagedApiFormAsync<Model>(method, relativeUrl, request);

    public virtual Task<IHasErrorStatus> ApiFormAsync<Model>(string relativeUrl, MultipartFormDataContent request) =>
        Client!.ManagedApiFormAsync<Model>(relativeUrl, request);

    public static string ClassNames(params string?[] classes) => CssUtils.ClassNames(classes);
    public virtual Task<ApiResult<AppMetadata>> ApiAppMetadataAsync() => Client!.ApiAppMetadataAsync();

    bool? enableLogging;
    protected virtual bool EnableLogging => enableLogging ??= BlazorConfig.Instance.EnableVerboseLogging;
    protected virtual void log(string? message, params object?[] args)
    {
        if (EnableLogging)
            Log.LogDebug(message, args);
    }
}

