using ServiceStack;
using ServiceStack.Blazor;

namespace MyApp;

/// <summary>
/// Manages App Authentication State
/// </summary>
public class ServiceStackStateProvider : ServiceStackAuthenticationStateProvider
{
    public ServiceStackStateProvider(JsonApiClient client, ILogger<ServiceStackAuthenticationStateProvider> log)
        : base(client, log) { }
}
