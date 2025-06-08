#nullable enable
#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Data.Common;
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
using ServiceStack.Messaging;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Extensions.Tests;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IDbConnectionFactory dbFactory)
    : IdentityDbContext<ApplicationUser>(options)
{
    private DbConnection? db;
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(db ??= dbFactory.CreateSingleWriterDb());
        base.OnConfiguring(optionsBuilder);
    }

    public override void Dispose()
    {
        db?.Dispose();
        db = null;
        base.Dispose();
    }
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

[ValidateIsAuthenticated]
public class MqBearerToken : IHasBearerToken, IReturn<MqBearerToken>
{
    public int Id { get; set; }
    public string? BearerToken { get; set; }
}

public class BackgroundAuthServices : Service
{
    public object Any(MqBearerToken request)
    {
        request.Id++;
        return request;
    }
}

public class IdentityJwtAuthProviderTests
{
    private static readonly int TotalRockstars = AutoQueryAppHost.SeedRockstars.Length;
    private static readonly int TotalAlbums = AutoQueryAppHost.SeedAlbums.Length;

    public static async Task AddSeedUsers(IServiceProvider services)
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

    public static void CreateIdentityUsers(IServiceProvider services)
    {
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.EnsureCreated();
        //dbContext.Database.Migrate(); // runs migrations twice

        // Only seed users if DB was just created
        if (!dbContext.Users.Any())
        {
            AddSeedUsers(scope.ServiceProvider).Wait();
        }
    }

    class AppHost() : AppHostBase(nameof(IdentityJwtAuthProviderTests), typeof(AutoQueryService).Assembly)
    {
        
        public override void Configure()
        {
            var log = ApplicationServices.GetRequiredService<ILogger<IdentityJwtAuthProviderTests>>();
            log.LogInformation("IdentityJwtAuthProviderTests.Configure()");

            CreateIdentityUsers(ApplicationServices);

            log.LogInformation("Seeding Database...");
            using var db = GetDbConnection();
            AutoQueryAppHost.SeedDatabase(db);

            var mqService = Resolve<IMessageService>();
            mqService.RegisterHandler<MqBearerToken>(ExecuteMessage);
            mqService.Start();
        }
    }

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
        services.AddDbContext<ApplicationDbContext>();
        // services.AddDbContext<ApplicationDbContext>(options =>
        //     options.UseSqlite(connectionString /*, b => b.MigrationsAssembly(nameof(MyApp))*/));

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
            options.JwtAuth(x =>
            {
                x.ExtendRefreshTokenExpiryAfterUsage = TimeSpan.FromDays(90);
                x.IncludeConvertSessionToTokenService = true;
            });
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

        services.AddSingleton<IMessageService>(c => new BackgroundMqService());

        var app = builder.Build();

        app.UseAuthorization();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.MapAdditionalIdentityEndpoints();
        app.UseServiceStack(new AppHost(), options => { options.MapEndpoints(); });

        app.StartAsync(TestsConfig.ListeningOn);
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown() => AppHostBase.DisposeApp();

    public const string Username = "admin@email.com";
    public const string Password = "p@55wOrd";

    private static JsonApiClient GetClient() => new(TestsConfig.ListeningOn);

    private async Task<string> CreateExpiredTokenAsync()
    {
        var jwtProvider = HostContext.AppHost.Resolve<IIdentityJwtAuthProvider>();
        var userClaims = await jwtProvider.GetUserClaimsAsync(Username);
        var jwt = jwtProvider.CreateJwtBearerToken(
            userClaims, jwtProvider.Audience, DateTime.UtcNow.AddDays(-1));
        return jwt;
    }
    
    private async Task<string> GetBearerTokenAsync()
    {
        var authClient = GetClient();
        var response = await authClient.SendAsync(new Authenticate
        {
            provider = "credentials",
            UserName = Username,
            Password = Password,
        });
        return authClient.GetTokenCookie();
    }

    private async Task<string> GetRefreshTokenAsync()
    {
        var authClient = GetClient();
        var response = await authClient.SendAsync(new Authenticate
        {
            provider = "credentials",
            UserName = Username,
            Password = Password,
        });
        return authClient.GetRefreshTokenCookie();
    }

    protected virtual async Task<JsonApiClient> GetClientWithRefreshToken(string? refreshToken = null, string? accessToken = null)
    {
        refreshToken ??= await GetRefreshTokenAsync();
        var client = GetClient();
        if (accessToken != null)
            client.SetTokenCookie(accessToken);
        client.SetRefreshTokenCookie(refreshToken);
        return client;
    }

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

    [Test]
    public async Task Endpoints_Can_ConvertSessionToToken()
    {
        var authClient = GetClient();
        var authResponse = await authClient.SendAsync(new Authenticate
        {
            provider = "credentials",
            UserName = Username,
            Password = Password,
            Meta = new() { [Keywords.Ignore] = "jwt" },
        });
        Assert.That(authResponse.UserName, Is.EqualTo(Username));
        Assert.That(authResponse.BearerToken, Is.Null);
        Assert.That(authClient.GetTokenCookie(), Is.Null);

        var response = await authClient.SendAsync(new HelloJwt { Name = "from auth service" });
        Assert.That(response.Result, Is.EqualTo("Hello, from auth service"));

        await authClient.SendAsync(new ConvertSessionToToken());
        Assert.That(authClient.GetTokenCookie(), Is.Not.Null);
        
        response = await authClient.SendAsync(new HelloJwt { Name = "from auth service" });
        Assert.That(response.Result, Is.EqualTo("Hello, from auth service"));
        
        authClient.DeleteTokenCookies();
        Assert.That(authClient.GetTokenCookie(), Is.Null);
        var result = await authClient.ApiAsync(new HelloJwt { Name = "from auth service" });
        Assert.That(result.Failed);
    }

    [Test]
    public async Task Endpoints_Invalid_RefreshToken_throws_RefreshToken_Exception()
    {
        var client = await GetClientWithRefreshToken("Invalid.Refresh.Token");
        try
        {
            var request = new Secured { Name = "test" };
            var response = await client.SendAsync(request);
            Assert.Fail("Should throw");
        }
        catch (WebServiceException ex)
        {
            Assert.That(ex.ErrorCode, Is.EqualTo(nameof(HttpStatusCode.Unauthorized)));
            Assert.That(ex.ErrorMessage, Does.Contain("RefreshToken"));
        }
    }

    [Test]
    public async Task Endpoints_Can_Auto_reconnect_with_just_RefreshToken()
    {
        var client = await GetClientWithRefreshToken();
        Assert.That(client.GetTokenCookie(), Is.Null);

        var request = new Secured { Name = "test" };
        var response = await client.SendAsync(request);
        Assert.That(response.Result, Is.EqualTo("Hello, test"));
        
        Assert.That(client.GetTokenCookie(), Is.Not.Null);

        response = await client.SendAsync(request);
        Assert.That(response.Result, Is.EqualTo("Hello, test"));
    }

    [Test]
    public async Task Endpoints_Can_Auto_reconnect_with_RefreshToken_after_expired_token()
    {
        var client = await GetClientWithRefreshToken(await GetRefreshTokenAsync(), await CreateExpiredTokenAsync());

        var request = new Secured { Name = "test" };
        var response = await client.SendAsync(request);
        Assert.That(response.Result, Is.EqualTo("Hello, test"));
        
        Assert.That(client.GetTokenCookie(), Is.Not.Null);

        response = await client.SendAsync(request);
        Assert.That(response.Result, Is.EqualTo("Hello, test"));
    }

    [Test]
    public async Task Can_authenticate_with_BearerToken_in_MQ()
    {
        var bearerToken = await GetBearerTokenAsync();

        await ServiceStackHost.Instance.ExecuteMessageAsync(new Message<MqBearerToken>(new MqBearerToken
        {
            BearerToken = bearerToken,
        }));
    }

    [Test]
    public void Can_use_IdentityUsers_to_query_AspNetUsers()
    {
        using var db = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IDbConnectionFactory>().Open();
        string[] userNames = ["manager@email.com", "admin@email.com"];

        Assert.That(db.TableExists(IdentityUsers.TableName));
        
        var users = IdentityUsers.GetByUserNames(db, userNames);
        Assert.That(users.Count, Is.EqualTo(2));
        
        var userIds = users.Map(x => x.Id);
        var emails = users.Map(x => x.Email!);
        
        Assert.That(userIds.All(x => x != null));
        Assert.That(emails.All(x => x != null));

        var user = IdentityUsers.GetByUserId(db, userIds[0]);
        Assert.That(user!.Email!.EndsWith("@email.com"));

        user = IdentityUsers.GetByEmail(db, emails[0]);
        Assert.That(user!.Email!.EndsWith("@email.com"));
        
        users = IdentityUsers.GetByUserIds(db, userIds);
        Assert.That(users.Count, Is.EqualTo(2));
        
        users = IdentityUsers.GetByEmails(db, emails);
        Assert.That(users.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task Can_use_IdentityUsers_to_query_AspNetUsers_Async()
    {
        using var db = await ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IDbConnectionFactory>().OpenAsync();
        string[] userNames = ["manager@email.com", "admin@email.com"];
        
        Assert.That(await db.TableExistsAsync(IdentityUsers.TableName));

        var users = await IdentityUsers.GetByUserNamesAsync(db, userNames);
        Assert.That(users.Count, Is.EqualTo(2));
        
        var userIds = users.Map(x => x.Id);
        var emails = users.Map(x => x.Email!);
        
        Assert.That(userIds.All(x => x != null));
        Assert.That(emails.All(x => x != null));

        var user = await IdentityUsers.GetByUserIdAsync(db, userIds[0]);
        Assert.That(user!.Email!.EndsWith("@email.com"));

        user = await IdentityUsers.GetByEmailAsync(db, emails[0]);
        Assert.That(user!.Email!.EndsWith("@email.com"));
        
        users = await IdentityUsers.GetByUserIdsAsync(db, userIds);
        Assert.That(users.Count, Is.EqualTo(2));
        
        users = await IdentityUsers.GetByEmailsAsync(db, emails);
        Assert.That(users.Count, Is.EqualTo(2));
    }
}

#endif