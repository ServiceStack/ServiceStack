using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazor.Extensions.Logging;
using MyApp.Data;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddLogging(c => c
    .AddBrowserConsole()
    .SetMinimumLevel(LogLevel.Trace)
);

builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton<WeatherForecastService>();

// Use / for local or CDN resources
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
// builder.Services.AddScoped<AuthenticationStateProvider>(s => s.GetRequiredService<ServiceStackStateProvider>());

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddBlazorApiClient(apiBaseUrl);

// builder.Services.AddScoped<ServiceStackStateProvider>();

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
