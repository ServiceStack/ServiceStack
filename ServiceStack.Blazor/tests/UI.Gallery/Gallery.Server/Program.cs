using Microsoft.AspNetCore.Components.Authorization;
using MyApp;
using MyApp.Client;
using MyApp.Client.Data;
using ServiceStack;
using ServiceStack.Blazor;
using System.Net;

Licensing.RegisterLicense("OSS BSD-3-Clause 2022 https://github.com/NetCoreApps/BlazorGallery aG/bfnbSOwyw1RnIF/FDKGNNOGGxQIU6EUpTRRi+T+5xwitylq/eECYb1auMpMYavN5HsY6zgphgNy9xq94a9GP5/OJzhnNS9WJPf0sXKt/iBk6Fdd4TzaZxyD57fPEKzTYtYof/Z6xtJmP8avbAvivfr19HaGkNcyD02KlTs4s=");

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
    Services = app.Services,
    JSParseObject = JS.ParseObject,
    EnableLogging = app.Environment.IsDevelopment(),
    EnableVerboseLogging = app.Environment.IsDevelopment(),
});

app.Run();