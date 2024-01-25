using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyApp;
using MyApp.Data;
using MyApp.ServiceInterface;
using ServiceStack;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var config = builder.Configuration;

services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidIssuer = "https://localhost:5001",
            ValidAudience = "https://localhost:5001",
            IssuerSigningKey = new SymmetricSecurityKey("a47e02ff-a88b-4480-b791-67aae6b1076a"u8.ToArray()),
            ValidateIssuerSigningKey = true,
        };
    })
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler<ApplicationUser>>(
        BasicAuthenticationHandler.Scheme, null)
    .AddIdentityCookies(options => options.DisableRedirectsForApis());

services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("App_Data"));
services.AddAuthorization();
services.AddAntiforgery();

// $ dotnet ef migrations add CreateIdentitySchema
// $ dotnet ef database update
var connectionString = config.GetConnectionString("DefaultConnection");
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString /*, b => b.MigrationsAssembly(nameof(MyApp))*/ ));

services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, AdditionalUserClaimsPrincipalFactory>();

services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        policy.WithOrigins([
            "http://localhost:5173", //vite dev
            "https://docs.servicestack.net"
        ])
        .AllowCredentials();
    });
});

services.AddHttpUtilsClient();

services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

builder.Services.AddServiceStack(typeof(MyServices).Assembly, c => {
    c.AddSwagger(o => {
        o.AddBasicAuth();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.UseServiceStack(new AppHost(), options =>
{
    options.MapEndpoints();
});

app.Run();
