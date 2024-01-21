#nullable enable
#if NET8_0_OR_GREATER

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Extensions.Tests;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
}

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser, IRequireRefreshToken
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? ProfileUrl { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
}

public class CustomUserSession : AuthUserSession
{
    public override void PopulateFromClaims(IRequest httpReq, ClaimsPrincipal principal)
    {
        // Populate Session with data from Identity Auth Claims
        ProfileUrl = principal.FindFirstValue(JwtClaimTypes.Picture);
    }
}

public class Roles
{
    public const string Admin = nameof(Admin);
    public const string Manager = nameof(Manager);
    public const string Employee = nameof(Employee);
}

public class IdentityJwtAuthProviderTests
{
    private static readonly int TotalRockstars = AutoQueryAppHost.SeedRockstars.Length;
    private static readonly int TotalAlbums = AutoQueryAppHost.SeedAlbums.Length;

    class AppHost() : AppHostBase(nameof(IdentityJwtAuthProviderTests), typeof(AutoQueryService).Assembly)
    {
        public override void Configure()
        {
            var log = ApplicationServices.GetRequiredService<ILogger<IdentityJwtAuthProviderTests>>();
            log.LogInformation("IdentityJwtAuthProviderTests.Configure()");

            var scopeFactory = ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.EnsureCreated();
            //dbContext.Database.Migrate(); // runs migrations twice

            // Only seed users if DB was just created
            if (!dbContext.Users.Any())
            {
                log.LogInformation("Adding Seed Users...");
                AddSeedUsers(scope.ServiceProvider).Wait();
            }

            log.LogInformation("Seeding Database...");
            using var db = GetDbConnection();
            AutoQueryAppHost.SeedDatabase(db);
        }

        private async Task AddSeedUsers(IServiceProvider services)
        {
            //initializing custom roles 
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            string[] allRoles = [Roles.Admin, Roles.Manager, Roles.Employee];

            void assertResult(IdentityResult result)
            {
                if (!result.Succeeded)
                    throw new Exception(result.Errors.First().Description);
            }

            async Task EnsureUserAsync(ApplicationUser user, string password, string[]? roles = null)
            {
                var existingUser = await userManager.FindByEmailAsync(user.Email!);
                if (existingUser != null) return;

                await userManager!.CreateAsync(user, password);
                if (roles?.Length > 0)
                {
                    var newUser = await userManager.FindByEmailAsync(user.Email!);
                    assertResult(await userManager.AddToRolesAsync(user, roles));
                }
            }

            foreach (var roleName in allRoles)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    //Create the roles and seed them to the database
                    assertResult(await roleManager.CreateAsync(new IdentityRole(roleName)));
                }
            }

            await EnsureUserAsync(new ApplicationUser
            {
                DisplayName = "Test User",
                Email = "test@email.com",
                UserName = "test@email.com",
                FirstName = "Test",
                LastName = "User",
                EmailConfirmed = true,
                ProfileUrl = "/img/profiles/user1.svg",
            }, "p@55wOrd");

            await EnsureUserAsync(new ApplicationUser
            {
                DisplayName = "Test Employee",
                Email = "employee@email.com",
                UserName = "employee@email.com",
                FirstName = "Test",
                LastName = "Employee",
                EmailConfirmed = true,
                ProfileUrl = "/img/profiles/user2.svg",
            }, "p@55wOrd", [Roles.Employee]);

            await EnsureUserAsync(new ApplicationUser
            {
                DisplayName = "Test Manager",
                Email = "manager@email.com",
                UserName = "manager@email.com",
                FirstName = "Test",
                LastName = "Manager",
                EmailConfirmed = true,
                ProfileUrl = "/img/profiles/user3.svg",
            }, "p@55wOrd", [Roles.Manager, Roles.Employee]);

            await EnsureUserAsync(new ApplicationUser
            {
                DisplayName = "Admin User",
                Email = "admin@email.com",
                UserName = "admin@email.com",
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true,
            }, "p@55wOrd", allRoles);
        }
    }

    private WebApplication? app;
    private AppHostBase? appHost;

    public IdentityJwtAuthProviderTests()
    {
        var contentRootPath = "~/../../../".MapServerPath();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = contentRootPath,
            WebRootPath = contentRootPath,
        });
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
                    ValidIssuer = TestsConfig.ListeningOn,
                    ValidAudience = TestsConfig.ListeningOn,
                    IssuerSigningKey = new SymmetricSecurityKey("a47e02ff-a88b-4480-b791-67aae6b1076a"u8.ToArray()),
                    ValidateIssuerSigningKey = true,
                };
            })
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler<ApplicationUser>>(
                BasicAuthenticationHandler.Scheme, null)
            .AddIdentityCookies(options => options.DisableRedirectsForApis());
        services.AddAuthorization();

        var dbPath = contentRootPath.CombineWith("App_Data/endpoints.sqlite");
        if (File.Exists(dbPath))
            File.Delete(dbPath);
        var connectionString = $"DataSource={dbPath};Cache=Shared";
        var dbFactory = new OrmLiteConnectionFactory(connectionString, SqliteDialect.Provider);
        services.AddSingleton<IDbConnectionFactory>(dbFactory);
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString /*, b => b.MigrationsAssembly(nameof(MyApp))*/));

        services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        ServiceStackHost.InitOptions.ScriptContext.ScriptMethods.AddRange([
            new DbScriptsAsync(),
            new MyValidators(),
        ]);

        services.AddPlugin(new AuthFeature(IdentityAuth.For<ApplicationUser>(options =>
        {
            options.SessionFactory = () => new CustomUserSession();
            options.CredentialsAuth();
            options.JwtAuth(x => { x.ExtendRefreshTokenExpiryAfterUsage = TimeSpan.FromDays(90); });
        })));

        services.AddPlugin(AutoQueryAppHost.CreateAutoQueryFeature());

        services.AddServiceStack(typeof(MyServices).Assembly, c =>
        {
            c.AddSwagger(o =>
            {
                o.AddJwtBearer();
                //o.AddBasicAuth();
            });
        });

        app = builder.Build();

        app.UseAuthorization();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.MapAdditionalIdentityEndpoints();
        appHost = new AppHost();
        app.UseServiceStack(appHost, options => { options.MapEndpoints(); });

        app.StartAsync(TestsConfig.ListeningOn);
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown() => appHost.DisposeApp();

    public const string Username = "admin@email.com";
    public const string Password = "p@55wOrd";

    private static JsonApiClient GetClient() => new(TestsConfig.ListeningOn);

    protected virtual JsonApiClient GetClientWithBasicAuthCredentials()
    {
        var client = GetClient();
        client.SetCredentials(Username, Password);
        return client;
    }

    [Test]
    public async Task Endpoints_execute_basic_query()
    {
        var client = GetClient();
        var response = await client.GetAsync(new QueryRockstars { Include = "Total" });

        Assert.That(response.Offset, Is.EqualTo(0));
        Assert.That(response.Total, Is.EqualTo(TotalRockstars));
        Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars));
    }

    [Test]
    public async Task Endpoints_Can_not_access_Secured_without_Auth()
    {
        var client = GetClient();

        try
        {
            var request = new Secured { Name = "test" };
            var response = await client.SendAsync(request);
            Assert.Fail("Should throw");
        }
        catch (WebServiceException ex)
        {
            ex.Message.Print();
            Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            Assert.That(ex.ErrorCode, Is.EqualTo(nameof(HttpStatusCode.Unauthorized)));
        }
    }

    [Test]
    public async Task Endpoints_Can_access_Secured_using_BasicAuth()
    {
        var client = GetClientWithBasicAuthCredentials();

        var request = new Secured { Name = "test" };

        var response = await client.SendAsync(request);
        Assert.That(response.Result, Is.EqualTo("Hello, test"));

        response = await client.PostAsync(request);
        Assert.That(response.Result, Is.EqualTo("Hello, test"));
    }
}

#endif