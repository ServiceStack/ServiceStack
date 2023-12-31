using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using ServiceStack;
using CheckGrpc;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
}
app.UseRouting();
app.UseServiceStack(new AppHost());

app.Run();