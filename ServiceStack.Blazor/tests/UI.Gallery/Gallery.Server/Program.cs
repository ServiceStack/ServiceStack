using Microsoft.AspNetCore.Components.Authorization;
using MyApp;
using MyApp.Client;
using MyApp.Client.Data;
using ServiceStack;
using ServiceStack.Blazor;
using System.Net;

Licensing.RegisterLicense("OSS BSD-3-Clause 2023 https://github.com/NetCoreApps/BlazorGallery WZ4hSuvzmoeRsZSygYGa3b7y1F+ohVZhYvHsfbRBw3hr0Jhyk/xSrPOl86g8St+H9zll7ehDjG5D5176JvP9baU3zIoZKek3+RvDbr+Th/COMaXrnByIA/pBjIR7aApF6l8tLXA4qV05rEE7wNxla74QGCupdH5NU2r2algvLTU=");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddLogging();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddScoped<ServiceStackStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(s => s.GetRequiredService<ServiceStackStateProvider>());

var baseUrl = builder.Configuration["ApiBaseUrl"] ?? 
    (builder.Environment.IsDevelopment() ? "https://localhost:5001" : "http://" + IPAddress.Loopback);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseUrl) });
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddBlazorServerApiClient(baseUrl);
builder.Services.AddLocalStorage();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.UseServiceStack(new AppHost());

BlazorConfig.Set(new() {
    UseInProcessClient = true,
    Services = app.Services,
    JSParseObject = JS.ParseObject,
    EnableLogging = app.Environment.IsDevelopment(),
    EnableVerboseLogging = app.Environment.IsDevelopment(),
});

app.Run();