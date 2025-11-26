using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using MyApp.Data;
using MyApp.ServiceInterface;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddAuthorization();
services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();
services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("App_Data"));

services.AddDatabaseDeveloperPageExceptionFilter();

services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

services.AddServiceStack(typeof(MyServices).Assembly);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();

app.UseServiceStack(new AppHost());

app.Run();
