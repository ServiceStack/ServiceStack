using OpenApiScalar;
using OpenApiScalar.ServiceInterface;
using Scalar.AspNetCore;
using ServiceStack;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddOpenApi();
services.AddServiceStackOpenApi();
services.AddServiceStack(typeof(MyServices).Assembly);

var app = builder.Build();

app.UseHttpsRedirection();

app.UseServiceStack(new AppHost());

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();
