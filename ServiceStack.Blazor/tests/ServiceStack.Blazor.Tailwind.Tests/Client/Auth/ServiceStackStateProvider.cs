using Microsoft.AspNetCore.Components;
using ServiceStack;
using ServiceStack.Blazor;

namespace MyApp;

/// <summary>
/// Manages App Authentication State
/// </summary>
public class ServiceStackStateProvider : ServiceStackAuthenticationStateProvider
{
    public ServiceStackStateProvider(JsonApiClient client, ILogger<ServiceStackAuthenticationStateProvider> log, NavigationManager navigationManager)
        : base(client, log, navigationManager) { }
}
