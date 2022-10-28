using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ServiceStack;
using ServiceStack.Blazor;

namespace MyApp.Client;

/// <summary>
/// Manages App Authentication State
/// </summary>
public class ServiceStackStateProvider : BlazorServerAuthenticationStateProvider
{
    public ServiceStackStateProvider(
        JsonApiClient client, ILogger<BlazorServerAuthenticationStateProvider> log, IHttpContextAccessor accessor, NavigationManager navigationManager, IJSRuntime js)
        : base(client, log, accessor, navigationManager, js) { }
}
