using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.Blazor;

namespace MyApp.Client;

/// <summary>
/// Manages App Authentication State
/// </summary>
public class ServiceStackStateProvider : ServiceStackAuthenticationStateProvider
{
    public ServiceStackStateProvider(JsonApiClient client, ILogger<ServiceStackAuthenticationStateProvider> log, NavigationManager navigationManager)
        : base(client, log, navigationManager) { }
}
