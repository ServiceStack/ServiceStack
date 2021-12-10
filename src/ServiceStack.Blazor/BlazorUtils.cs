namespace ServiceStack.Blazor;

class ApiResultsCache<T>
{
    internal static ApiResult<T>? Instance { get; set; }
}

public static class BlazorUtils
{
    public static async Task<ApiResult<T>> ApiCacheAsync<T>(this BlazorComponentBase component, IReturn<T> requestDto)
    {
        if (ApiResultsCache<T>.Instance != null)
            return ApiResultsCache<T>.Instance;

        var apiResult = await component.Client!.ApiAsync(requestDto);
        if (apiResult.IsSuccess)
            ApiResultsCache<T>.Instance = apiResult;
        return apiResult;
    }

    public static Task<ApiResult<AppMetadata>> ApiAppMetadataAsync(this BlazorComponentBase component) =>
        component.ApiCacheAsync(new MetadataApp());
}
