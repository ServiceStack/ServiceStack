using OpenApi3Swashbuckle.ServiceInterface;
using ServiceStack;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddServiceStack(typeof(MyServices).Assembly);

if (builder.Environment.IsDevelopment())
{
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();
    services.AddServiceStackSwagger();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();


app.UseServiceStack(new AppHost());

app.Run();

