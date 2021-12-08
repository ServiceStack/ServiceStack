using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor;

public class BlazorComponentBase : ComponentBase
{
    [Inject]
    protected JsonApiClient? Client { get; set; }

    protected virtual async Task<ApiResult<TResponse>> ApiAsync<TResponse>(IReturn<TResponse> request) =>
        await Client!.ApiAsync(request);

    protected virtual async Task<ApiResult<EmptyResponse>> ApiAsync(IReturnVoid request) =>
        await Client!.ApiAsync(request);

    protected virtual async Task<TResponse> SendAsync<TResponse>(IReturn<TResponse> request) =>
        await Client!.SendAsync(request);

    public static string ClassNames(params string?[] classes) => CssUtils.ClassNames(classes);

    private AppMetadata? appMetadata;
    public virtual async Task<AppMetadata> GetAppMetadata()
    {
        if (appMetadata != null)
            return appMetadata!;
        appMetadata = await Client!.GetAsync(new MetadataApp());
        return appMetadata;
    }
}
