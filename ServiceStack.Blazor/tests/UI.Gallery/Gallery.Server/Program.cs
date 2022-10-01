using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using MyApp;
using MyApp.Client;
using MyApp.Client.Data;
using ServiceStack;
using ServiceStack.Blazor;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var baseUrl = builder.Configuration["ApiBaseUrl"] ?? 
    (builder.Environment.IsDevelopment() ? "https://localhost:7142" : "http://" + IPAddress.Loopback);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseUrl) });
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddBlazorApiClient(baseUrl);

builder.Services.AddScoped<AuthenticationStateProvider>(s => s.GetRequiredService<ServiceStackStateProvider>());
builder.Services.AddScoped<ServiceStackStateProvider>();


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
    EnableLogging = true,
    EnableVerboseLogging = true,
});

app.Run();