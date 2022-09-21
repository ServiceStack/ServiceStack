using System.Globalization;
using System.Net;
using MyApp.Data;
using MyApp.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using ServiceStack.Blazor;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor().AddCircuitOptions(o =>
{
    o.DetailedErrors = true;
});
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddScoped<ProtectedLocalStorage>();
var baseUrl = builder.Environment.IsDevelopment() ? 
    "https://localhost:5001" : "http://" + IPAddress.Loopback;

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseUrl) });
builder.Services.AddBlazorApiClient(baseUrl);

builder.Services.AddScoped<AuthenticationStateProvider>(s => s.GetRequiredService<ServiceStackStateProvider>());
builder.Services.AddScoped<ServiceStackStateProvider>();
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo("/app/App_Data/"));

var app = builder.Build();
app.UseWebSockets();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.UseServiceStack(new AppHost());

app.Run();
