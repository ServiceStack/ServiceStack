using Microsoft.AspNetCore.Components;
using ServiceStack;
using ServiceStack.Blazor;

namespace MyApp.Client;

/// <summary>
/// Manages App Authentication State
/// </summary>
public class ServiceStackStateProvider : BlazorWasmAuthenticationStateProvider
{
    public ServiceStackStateProvider(BlazorWasmAuthContext context, ILogger<BlazorWasmAuthenticationStateProvider> log)
        : base(context, log) { }
}
