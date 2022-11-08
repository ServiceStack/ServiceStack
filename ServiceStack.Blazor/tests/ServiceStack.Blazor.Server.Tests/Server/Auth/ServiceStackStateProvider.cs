namespace MyApp;

/// <summary>
/// Manages App Authentication State
/// </summary>
public class ServiceStackStateProvider : BlazorServerAuthenticationStateProvider
{
    public ServiceStackStateProvider(BlazorServerAuthContext context, ILogger<BlazorServerAuthenticationStateProvider> log) : base(context, log) {}
}
