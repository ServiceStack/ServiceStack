#if NET8_0_OR_GREATER

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.Extensions.Tests;

public class SystemJsonIntegrationTests
{
    class AppHost() : AppHostBase(nameof(SystemJsonIntegrationTests), typeof(AutoQueryService).Assembly)
    {
        public override void Configure()
        {
            var log = ApplicationServices.GetRequiredService<ILogger<SystemJsonIntegrationTests>>();
            log.LogInformation("SystemJsonIntegrationTests.Configure()");

            var scopeFactory = ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.EnsureCreated();
            //dbContext.Database.Migrate(); // runs migrations twice

            // Only seed users if DB was just created
            if (!dbContext.Users.Any())
            {
                log.LogInformation("Adding Seed Users...");
                IdentityJwtAuthProviderTests.AddSeedUsers(scope.ServiceProvider).Wait();
            }

            log.LogInformation("Seeding Database...");
            using var db = GetDbConnection();
            AutoQueryAppHost.SeedDatabase(db);
            db.CreateTable<Booking>();
        }
    }

    public SystemJsonIntegrationTests()
    {
        var contentRootPath = "~/../../../".MapServerPath();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = contentRootPath,
            WebRootPath = contentRootPath,
        });
        var services = builder.Services;
        var config = builder.Configuration;

        services.AddAuthentication()
            .AddIdentityCookies(options => options.DisableRedirectsForApis());
        services.AddAuthorization();

        var dbPath = contentRootPath.CombineWith("App_Data/systemjson.sqlite");
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
        })));

        services.AddPlugin(AutoQueryAppHost.CreateAutoQueryFeature());

        var app = builder.Build();

        app.UseAuthorization();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseServiceStack(new AppHost(), options =>
        {
            options.MapEndpoints();
        });

        app.StartAsync(TestsConfig.ListeningOn);
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown() => AppHostBase.DisposeApp();

    private static readonly int TotalRockstars = AutoQueryAppHost.SeedRockstars.Length;
    private static readonly int TotalAlbums = AutoQueryAppHost.SeedAlbums.Length;

    public const string Username = "admin@email.com";
    public const string Password = "p@55wOrd";

    static JsonApiClient GetClient() => new(TestsConfig.ListeningOn);
    private readonly JsonApiClient client = GetClient();

    static async Task<JsonApiClient> CreateAuthClientAsync()
    {
        var authClient = GetClient();
        var response = await authClient.SendAsync(new Authenticate
        {
            provider = "credentials",
            UserName = Username,
            Password = Password,
        });
        return authClient;
    }

    [Test]
    public async Task SystemJson_can_execute_basic_query()
    {
        var response = await client.GetAsync(new QueryRockstars { Include = "Total" });

        Assert.That(response.Offset, Is.EqualTo(0));
        Assert.That(response.Total, Is.EqualTo(TotalRockstars));
        Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars));
    }

    [Test]
    public async Task SystemJson_can_execute_explicit_equality_condition_on_CustomRockstarSchema()
    {
        var response = await client.GetAsync(new QueryCustomRockstarsSchema { Age = 27, Include = "Total" });

        Assert.That(response.Total, Is.EqualTo(3));
        Assert.That(response.Results.Count, Is.EqualTo(3));
        Assert.That(response.Results[0].FirstName, Is.Not.Null);
        Assert.That(response.Results[0].LastName, Is.Not.Null);
        Assert.That(response.Results[0].Age, Is.EqualTo(27));
    }

    [Test]
    public async Task SystemJson_can_query_Movie_Ratings()
    {
        var response = await client.GetAsync(new QueryMovies { Ratings = ["G","PG-13"] });
        Assert.That(response.Results.Count, Is.EqualTo(5));

        response = await client.GetAsync(new QueryMovies {
            Ids = [1, 2],
            ImdbIds = ["tt0071562", "tt0060196"],
            Ratings = ["G", "PG-13"]
        });
        Assert.That(response.Results.Count, Is.EqualTo(9));
    }

    [Test]
    public async Task SystemJson_can_Query_Rockstars_with_References()
    {
        var response = await client.GetAsync(new QueryRockstarsWithReferences {
            Age = 27
        });
         
        Assert.That(response.Results.Count, Is.EqualTo(3));

        var jimi = response.Results.First(x => x.FirstName == "Jimi");
        Assert.That(jimi.Albums.Count, Is.EqualTo(1));
        Assert.That(jimi.Albums[0].Name, Is.EqualTo("Electric Ladyland"));

        var jim = response.Results.First(x => x.FirstName == "Jim");
        Assert.That(jim.Albums, Is.Null);

        var kurt = response.Results.First(x => x.FirstName == "Kurt");
        Assert.That(kurt.Albums.Count, Is.EqualTo(5));

        response = await client.GetAsync(new QueryRockstarsWithReferences
        {
            Age = 27,
            Fields = "Id,FirstName,Age"
        });
        Assert.That(response.Results.Count, Is.EqualTo(3));
        Assert.That(response.Results.All(x => x.Id > 0));
        Assert.That(response.Results.All(x => x.LastName == null));
        Assert.That(response.Results.All(x => x.Albums == null));

        response = await client.GetAsync(new QueryRockstarsWithReferences
        {
            Age = 27,
            Fields = "Id,FirstName,Age,Albums"
        });
        Assert.That(response.Results.Where(x => x.FirstName != "Jim").All(x => x.Albums != null));
    }

    [Test]
    public async Task SystemJson_can_CRUD_Booking()
    {
        var authClient = await CreateAuthClientAsync();
        
        var booking1Id = authClient.Post(new CreateBooking {
            RoomNumber = 1,
            RoomType = RoomType.Single,
            BookingStartDate = DateTime.Today.AddDays(1),
            BookingEndDate = DateTime.Today.AddDays(5),
            Cost = 100,
            Notes = nameof(Booking.Notes),
        }).Id.ToInt();
        Assert.That(booking1Id, Is.GreaterThan(0));
        
        var booking2Id = authClient.Post(new CreateBooking {
            RoomNumber = 2,
            RoomType = RoomType.Double,
            BookingStartDate = DateTime.Today.AddDays(2),
            BookingEndDate = DateTime.Today.AddDays(6),
            Cost = 200,
            Notes = nameof(Booking.Notes),
        }).Id.ToInt();
        Assert.That(booking2Id, Is.GreaterThan(0));

        var response = await authClient.SendAsync(new QueryBookings { OrderBy = nameof(Booking.Id) });
        response.PrintDump();
        
        Assert.That(response.Results.Count, Is.EqualTo(2));
        
        Assert.That(response.Results[0].Id, Is.EqualTo(1));
        Assert.That(response.Results[0].RoomType, Is.EqualTo(RoomType.Single));
        Assert.That(response.Results[0].Cost, Is.EqualTo(100));
        Assert.That(response.Results[0].Notes, Is.EqualTo(nameof(Booking.Notes)));
        Assert.That(response.Results[0].Cancelled, Is.Null);
        
        authClient.Patch(new UpdateBooking {
            Id = booking1Id,
            Cancelled = true,
            Notes = "Missed Flight",
        });
        
        response = await authClient.SendAsync(new QueryBookings { OrderBy = nameof(Booking.Id) });
        Assert.That(response.Results[0].Id, Is.EqualTo(1));
        Assert.That(response.Results[0].Notes, Is.EqualTo("Missed Flight"));
        Assert.That(response.Results[0].Cancelled, Is.True);
        
        await authClient.SendAsync(new DeleteBooking { Id = booking1Id });
        await authClient.SendAsync(new DeleteBooking { Id = booking2Id });
        
        response = await authClient.SendAsync(new QueryBookings { OrderBy = nameof(Booking.Id) });
        Assert.That(response.Results.Count, Is.EqualTo(0));
    }
}

#endif