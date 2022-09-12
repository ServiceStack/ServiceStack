using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor;

/// <summary>
/// Blazor com
/// </summary>
public class BlazorComponentBase : ComponentBase, IHasJsonApiClient
{
    [Inject]
    public JsonApiClient? Client { get; set; }

    public virtual Task<ApiResult<TResponse>> ApiAsync<TResponse>(IReturn<TResponse> request) => JsonApiClientUtils.ApiAsync(this, request);
    public virtual Task<ApiResult<EmptyResponse>> ApiAsync(IReturnVoid request) => JsonApiClientUtils.ApiAsync(this, request);
    public virtual Task<TResponse> SendAsync<TResponse>(IReturn<TResponse> request) => JsonApiClientUtils.SendAsync(this, request);

    public static string ClassNames(params string?[] classes) => CssUtils.ClassNames(classes);
    public virtual Task<ApiResult<AppMetadata>> ApiAppMetadataAsync() => JsonApiClientUtils.ApiAppMetadataAsync(this);

    protected bool EnableLogging { get; set; } = BlazorConfig.EnableVerboseLogging;
    protected void log(string? message = null)
    {
        if (EnableLogging)
            BlazorUtils.Log(message);
    }
}

/// <summary>
/// Also extend functionality to any class implementing IHasJsonApiClient
/// </summary>
public static class BlazorUtils
{
    public static void Log(string? message = null)
    {
        if (BlazorConfig.EnableVerboseLogging)
            Console.WriteLine(message ?? "");
    }
}

