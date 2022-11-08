using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;

namespace ServiceStack.Blazor;

/// <summary>
/// Blazor component base
/// </summary>
public class BlazorComponentBase : ComponentBase, IHasJsonApiClient
{
    [Inject] ILogger<BlazorComponentBase> Log { get; set; }
    [Inject] public JsonApiClient? Client { get; set; }
    [Inject] public IServiceGateway? Gateway { get; set; }

    [Parameter] public bool UseGateway { get; set; } = BlazorConfig.Instance.UseInProcessClient;
    
    public virtual Task<ApiResult<TResponse>> ApiAsync<TResponse>(IReturn<TResponse> request) => UseGateway 
        ? Gateway!.ManagedApiAsync(request)
        : Client!.ManagedApiAsync(request);

    public virtual Task<ApiResult<EmptyResponse>> ApiAsync(IReturnVoid request) => UseGateway 
        ? Gateway!.ManagedApiAsync(request)
        : Client!.ManagedApiAsync(request);

    public virtual Task<TResponse> SendAsync<TResponse>(IReturn<TResponse> request) => UseGateway
        ? Gateway!.ManagedSendAsync(request)
        : Client!.ManagedSendAsync(request);

    public virtual Task<IHasErrorStatus> ApiAsync<Model>(object request) => UseGateway 
        ? Gateway!.ManagedApiAsync<Model>(request)
        : Client!.ManagedApiAsync<Model>(request);

    public virtual Task<ApiResult<Model>> ApiFormAsync<Model>(object requestDto, MultipartFormDataContent request) => 
        UseGateway && Gateway is IServiceGatewayFormAsync gatewayForm
        ? gatewayForm.ManagedApiFormAsync<Model>(requestDto, request)
        : Client!.ManagedApiFormAsync<Model>(requestDto, request);

    public static string ClassNames(params string?[] classes) => CssUtils.ClassNames(classes);
    public virtual Task<ApiResult<AppMetadata>> ApiAppMetadataAsync() => UseGateway 
        ? Gateway!.ApiAppMetadataAsync()
        : Client!.ApiAppMetadataAsync();

    bool? enableLogging;
    protected virtual bool EnableLogging => enableLogging ??= BlazorConfig.Instance.EnableVerboseLogging;
    protected virtual void log(string? message, params object?[] args)
    {
        if (EnableLogging)
            Log.LogDebug(message, args);
    }
}

