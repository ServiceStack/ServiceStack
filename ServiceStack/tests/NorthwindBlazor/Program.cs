using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ServiceStack.Blazor;
using MyApp.Data;
using MyApp.Components;
using MyApp.Components.Account;
using MyApp.ServiceInterface;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var config = builder.Configuration;

// Add services to the container.
services.AddRazorComponents()
    .AddInteractiveServerComponents();

services.AddCascadingAuthenticationState();
services.AddScoped<IdentityUserAccessor>();
services.AddScoped<IdentityRedirectManager>();
services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidIssuer = config["JwtBearer:ValidIssuer"],
            ValidAudience = config["JwtBearer:ValidAudience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JwtBearer:IssuerSigningKey"]!)),
            ValidateIssuerSigningKey = true,
        };
    })
    .AddScheme<AuthenticationSchemeOptions,BasicAuthenticationHandler<ApplicationUser>>(BasicAuthenticationHandler.Scheme, null)
    .AddIdentityCookies();
services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("App_Data"));

// $ dotnet ef migrations add CreateIdentitySchema
// $ dotnet ef database update
var connectionString = config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString/*, b => b.MigrationsAssembly(nameof(MyApp))*/ ));
services.AddDatabaseDeveloperPageExceptionFilter();

services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
// Uncomment to send emails with SMTP, configure SMTP with "SmtpConfig" in appsettings.json
//services.AddSingleton<IEmailSender<ApplicationUser>, EmailSender>();
services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, AdditionalUserClaimsPrincipalFactory>();

var baseUrl = builder.Configuration["ApiBaseUrl"] ??
    (builder.Environment.IsDevelopment() ? "https://localhost:5001" : "http://" + IPAddress.Loopback);
services.AddScoped(c => new HttpClient { BaseAddress = new Uri(baseUrl) });
services.AddBlazorServerIdentityApiClient(baseUrl);
services.AddLocalStorage();

services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

// Register all services
services.AddServiceStack(typeof(MyServices).Assembly, c => {
    c.AddSwagger(o => {
        o.AddJwtBearer();
        //o.AddBasicAuth();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.UseServiceStack(new AppHost(), options =>
{
    options.MapEndpoints(force:true);
});

BlazorConfig.Set(new()
{
    Services = app.Services,
    JSParseObject = JS.ParseObject,
    IsDevelopment = app.Environment.IsDevelopment(),
    EnableLogging = app.Environment.IsDevelopment(),
    EnableVerboseLogging = app.Environment.IsDevelopment(),
});

app.Run();
