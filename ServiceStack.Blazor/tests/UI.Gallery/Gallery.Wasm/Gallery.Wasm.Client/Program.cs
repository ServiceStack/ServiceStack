using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Blazor.Extensions.Logging;
using ServiceStack.Blazor;
using MyApp.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddLogging(c => c
    .AddBrowserConsole()
    .SetMinimumLevel(LogLevel.Trace)
);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();

// Use / for local or CDN resources
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<AuthenticationStateProvider>(s => s.GetRequiredService<ServiceStackStateProvider>());

builder.Services.AddBlazorApiClient(builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress);

builder.Services.AddScoped<ServiceStackStateProvider>();

BlazorConfig.Set(new BlazorConfig {
    IsWasm = true,
    EnableLogging = true,
});

await builder.Build().RunAsync();