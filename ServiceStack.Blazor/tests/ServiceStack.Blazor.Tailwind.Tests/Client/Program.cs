using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Blazor.Extensions.Logging;
using ServiceStack.Blazor;
using MyApp;
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

builder.Services.AddScoped<ServiceStackStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(s => s.GetRequiredService<ServiceStackStateProvider>());

// Use / for local or CDN resources
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddBlazorApiClient(apiBaseUrl);
builder.Services.AddLocalStorage();


var app = builder.Build();

BlazorConfig.Set(new BlazorConfig
{
    IsWasm = true,
    Services = app.Services,
    FallbackAssetsBasePath = apiBaseUrl,
    EnableLogging = true,
    EnableVerboseLogging = builder.HostEnvironment.IsDevelopment(),
});

await app.RunAsync();
