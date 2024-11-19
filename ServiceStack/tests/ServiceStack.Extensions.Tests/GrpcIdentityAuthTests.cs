using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.Jobs;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace ServiceStack.Extensions.Tests;

public class GrpcIdentityAuthTests
{
    public const string Username = "test@email.com";
    public const string Password = "p@55wOrd";
    
    class AppHost() : AppHostBase(nameof(IdentityJwtAuthProviderTests), typeof(AutoQueryService).Assembly)
    {
        public override void Configure()
        {
            var log = ApplicationServices.GetRequiredService<ILogger<IdentityJwtAuthProviderTests>>();
            log.LogInformation("IdentityJwtAuthProviderTests.Configure()");

            IdentityJwtAuthProviderTests.CreateIdentityUsers(ApplicationServices);
            

            log.LogInformation("Seeding Database...");
            using var db = GetDbConnection();
            AutoQueryAppHost.SeedDatabase(db);

            var mqService = Resolve<IMessageService>();
            mqService.RegisterHandler<MqBearerToken>(ExecuteMessage);
            mqService.Start();
        }
    }

    private readonly BackgroundsJobFeature feature = new();
    private readonly ServiceStackHost appHost;
    public GrpcIdentityAuthTests()
    {
        var contentRootPath = "~/../../../".MapServerPath();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = contentRootPath,
            WebRootPath = contentRootPath,
        });
        
        // Configure Kestrel to listen on a specific HTTP port 
        builder.WebHost.ConfigureKestrel(options =>
        {
            // options.ListenAnyIP(8080);
            options.ListenAnyIP(TestsConfig.Port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });
        });
        
        var services = builder.Services;
        var config = builder.Configuration;
        
        services.AddServiceStackGrpc();
        services.AddPlugin(new GrpcFeature());

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
            options.BasicAuth();
        })));
        //services.AddBasicAuth<ApplicationUser>();

        services.AddPlugin(AutoQueryAppHost.CreateAutoQueryFeature());

        services.AddPlugin(new CommandsFeature());
        services.AddPlugin(feature);

        services.AddServiceStack(typeof(MyServices).Assembly);

        services.AddSingleton<IMessageService>(c => new BackgroundMqService());

        var app = builder.Build();

        app.UseRouting();

        app.UseAuthorization();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.MapAdditionalIdentityEndpoints();
        app.UseServiceStack(new AppHost(), options => { options.MapEndpoints(); });

        app.StartAsync(TestsConfig.ListeningOn);
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown() => AppHostBase.DisposeApp();

    private static GrpcServiceClient GetClient() => TestsConfig.GetInsecureClient();

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
        var jwtProvider = HostContext.AppHost.Resolve<IIdentityJwtAuthProvider>();
        var userClaims = await jwtProvider.GetUserClaimsAsync(Username);
        var jwt = jwtProvider.CreateJwtBearerToken(
            userClaims, jwtProvider.Audience, DateTime.UtcNow.AddDays(1));
        return jwt;
    }

    protected virtual async Task<GrpcServiceClient> GetClientWithBearerTokenAsync(string refreshToken = null, string accessToken = null)
    {
        accessToken ??= await GetBearerTokenAsync();
        var client = GetClient();
        client.RefreshToken = refreshToken;
        client.BearerToken = accessToken;
        return client;
    }

    // private async Task<string> GetRefreshTokenAsync()
    // {
    //     var authClient = GetClient();
    //     var response = await authClient.SendAsync(new Authenticate
    //     {
    //         provider = "credentials",
    //         UserName = Username,
    //         Password = Password,
    //     });
    //     return authClient.GetRefreshTokenCookie();
    // }

    // protected virtual async Task<GrpcServiceClient> GetClientWithRefreshToken(string refreshToken = null, string accessToken = null)
    // {
    //     refreshToken ??= await GetRefreshTokenAsync();
    //     var client = GetClient();
    //     client.RefreshToken = refreshToken;
    //     client.BearerToken = accessToken;
    //     return client;
    // }

    protected virtual GrpcServiceClient GetClientWithBasicAuthCredentials()
    {
        var client = GetClient();
        client.SetCredentials(Username, Password);
        return client;
    }

    [Test]
    public async Task Can_not_access_Secured_without_Auth()
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
            Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            Assert.That(ex.ErrorCode, Is.EqualTo(nameof(HttpStatusCode.Unauthorized)));
        }
    }

    [Test]
    public async Task Can_access_Secured_using_BasicAuth()
    {
        var client = GetClientWithBasicAuthCredentials();

        var request = new Secured { Name = "test" };

        var response = await client.SendAsync(request);
        Assert.That(response.Result, Is.EqualTo("Hello, test"));

        response = await client.PostAsync(request);
        Assert.That(response.Result, Is.EqualTo("Hello, test"));
    }

    [Test]
    public async Task Can_access_Secured_using_JWT()
    {
        var client = await GetClientWithBearerTokenAsync();

        var request = new Secured { Name = "test" };

        var response = await client.SendAsync(request);
        Assert.That(response.Result, Is.EqualTo("Hello, test"));

        response = await client.PostAsync(request);
        Assert.That(response.Result, Is.EqualTo("Hello, test"));
    }

    [Test]
    public async Task Can_not_access_Secured_with_expired_JWT()
    {
        var client = await GetClientWithBearerTokenAsync(accessToken:await CreateExpiredTokenAsync());
            
        try
        {
            var request = new Secured { Name = "test" };
            var response = await client.SendAsync(request);
            Assert.Fail("Should throw");
        }
        catch (WebServiceException ex)
        {
            Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            Assert.That(ex.ErrorCode, Is.EqualTo(nameof(SecurityTokenExpiredException)));
        }
    }

    [Test]
    public async Task Can_ConvertSessionToToken()
    {
        var authClient = GetClient();
        var authResponse = await authClient.SendAsync(new Authenticate
        {
            provider = "credentials",
            UserName = Username,
            Password = Password,
        });
        Assert.That(authResponse.UserName, Is.EqualTo(Username));
        Assert.That(authResponse.BearerToken, Is.Not.Null);
    
        authClient.SessionId = authResponse.SessionId;
    
        var response = await authClient.SendAsync(new HelloJwt { Name = "from auth service" });
        Assert.That(response.Result, Is.EqualTo("Hello, from auth service"));
    
        authClient.BearerToken = (await authClient.SendAsync(new ConvertSessionToToken())).AccessToken;
        Assert.That(authClient.BearerToken, Is.Not.Null);
    
        authClient.SessionId = null;
    
        response = await authClient.SendAsync(new HelloJwt { Name = "from auth service" });
        Assert.That(response.Result, Is.EqualTo("Hello, from auth service"));
    }

    // [Test]
    // public async Task Invalid_RefreshToken_throws_RefreshTokenException()
    // {
    //     var client = await GetClientWithRefreshToken("Invalid.Refresh.Token");
    //     try
    //     {
    //         var request = new Secured { Name = "test" };
    //         var response = await client.SendAsync(request);
    //         Assert.Fail("Should throw");
    //     }
    //     catch (RefreshTokenException ex)
    //     {
    //         ex.Message.Print();
    //         Assert.That(ex.ErrorCode, Is.EqualTo(nameof(ArgumentException)));
    //     }
    // }
    //
    // [Test]
    // public async Task Can_Auto_reconnect_with_just_RefreshToken()
    // {
    //     var client = await GetClientWithRefreshToken();
    //
    //     var request = new Secured { Name = "test" };
    //     var response = await client.SendAsync(request);
    //     Assert.That(response.Result, Is.EqualTo("Hello, test"));
    //
    //     response = await client.SendAsync(request);
    //     Assert.That(response.Result, Is.EqualTo("Hello, test"));
    // }
    //
    // [Test]
    // public async Task Can_Auto_reconnect_with_RefreshToken_after_expired_token()
    // {
    //     var client = await GetClientWithRefreshToken(await GetRefreshToken(), CreateExpiredToken());
    //
    //     var request = new Secured { Name = "test" };
    //     var response = await client.SendAsync(request);
    //     Assert.That(response.Result, Is.EqualTo("Hello, test"));
    //
    //     response = await client.SendAsync(request);
    //     Assert.That(response.Result, Is.EqualTo("Hello, test"));
    // }
    //
    // [Test]
    // public async Task Does_return_token_on_subsequent_BasicAuth_Authentication_requests()
    // {
    //     var client = GetClientWithBasicAuthCredentials();
    //
    //     var response = await client.PostAsync(new Authenticate());
    //     Assert.That(response.BearerToken, Is.Not.Null);
    //     Assert.That(response.RefreshToken, Is.Not.Null);
    //
    //     response = await client.PostAsync(new Authenticate());
    //     Assert.That(response.BearerToken, Is.Not.Null);
    //     Assert.That(response.RefreshToken, Is.Not.Null);
    // }

}
