using MyApp;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseServiceStack(new AppHost());

/* Run migrations in ASP.NET Core Apps
var migrator = new Migrator(app.Services.Resolve<IDbConnectionFactory>(), typeof(MyApp.Migrations.Migration1000).Assembly);
AppTasks.Register("migrate", _ => migrator.Run());
AppTasks.Register("migrate.revert", args => migrator.Revert(args[0]));
AppTasks.Run();
*/

app.Run();
