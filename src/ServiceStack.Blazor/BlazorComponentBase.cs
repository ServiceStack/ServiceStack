using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor;

public class BlazorComponentBase : ComponentBase
{
    [Inject]
    public JsonApiClient? Client { get; set; }

    public virtual async Task<ApiResult<TResponse>> ApiAsync<TResponse>(IReturn<TResponse> request) =>
        await Client!.ApiAsync(request);

    public virtual async Task<ApiResult<EmptyResponse>> ApiAsync(IReturnVoid request) =>
        await Client!.ApiAsync(request);

    public virtual async Task<TResponse> SendAsync<TResponse>(IReturn<TResponse> request) =>
        await Client!.SendAsync(request);

    public static string ClassNames(params string?[] classes) => CssUtils.ClassNames(classes);

    private static ApiResult<AppMetadata> appMetadataResult = new();
    public virtual async Task<ApiResult<AppMetadata>> ApiAppMetadataAsync()
    {
        if (appMetadataResult.IsSuccess)
            return appMetadataResult;
        return appMetadataResult = await Client!.ApiAsync(new MetadataApp());
    }
}
