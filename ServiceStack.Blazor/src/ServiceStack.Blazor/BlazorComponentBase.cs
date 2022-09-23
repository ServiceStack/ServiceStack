using Microsoft.AspNetCore.Components;
using System.Linq;

namespace ServiceStack.Blazor;

/// <summary>
/// Blazor com
/// </summary>
public class BlazorComponentBase : ComponentBase, IHasJsonApiClient
{
    [Inject] public JsonApiClient? Client { get; set; }

    public virtual Task<ApiResult<TResponse>> ApiAsync<TResponse>(IReturn<TResponse> request) => JsonApiClientUtils.ApiAsync(this, request);
    public virtual Task<ApiResult<EmptyResponse>> ApiAsync(IReturnVoid request) => JsonApiClientUtils.ApiAsync(this, request);
    public virtual Task<TResponse> SendAsync<TResponse>(IReturn<TResponse> request) => JsonApiClientUtils.SendAsync(this, request);

    public virtual Task<IHasErrorStatus> ApiAsync<Model>(object request) => JsonApiClientUtils.ApiAsync<Model>(this, request);

    public static string ClassNames(params string?[] classes) => CssUtils.ClassNames(classes);
    public virtual Task<ApiResult<AppMetadata>> ApiAppMetadataAsync() => JsonApiClientUtils.ApiAppMetadataAsync(this);

    protected bool EnableLogging { get; set; } = BlazorConfig.Instance.EnableVerboseLogging;
    protected void log(string? message = null)
    {
        if (EnableLogging)
            BlazorUtils.Log(message);
    }
}

