using CheckIdentity;
using CheckIdentity.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ServiceStack;
using ServiceStack.Auth;

var builder = WebApplication.CreateBuilder(args);
//builder.Services.AddModularStartup<AppHost>(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
//builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services
    .AddAuthentication()
    .AddJwtBearer(x => {
        // secretKey contains a secret passphrase only your server knows
        x.TokenValidationParameters = new TokenValidationParameters {
            IssuerSigningKey = new SymmetricSecurityKey("mysupers3cr3tsharedkey!".ToUtf8Bytes()),
            ValidAudience = "ExampleAudience",
            ValidIssuer = "ExampleIssuer",
        }.UseStandardJwtClaims();
    });

builder.Services.ConfigureNonBreakingSameSiteCookies(builder.Environment);


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

app.UseCookiePolicy().UseJwtCookie(IdentityAuth.TokenCookie);
app.UseAuthentication();
app.UseAuthorization();

app.UseServiceStack(new AppHost());

app.MapRazorPages();

app.Run();
