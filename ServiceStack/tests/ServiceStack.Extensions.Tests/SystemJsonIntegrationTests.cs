#if NET8_0_OR_GREATER

#nullable enable

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.Extensions.Tests;

public class SystemJsonIntegrationTests
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : DbContext(options)
    {
    }
    
    class AppHost() : AppHostBase(nameof(SystemJsonIntegrationTests), typeof(AutoQueryService).Assembly)
    {
        public override void Configure()
        {
            using var db = GetDbConnection();
            AutoQueryAppHost.SeedDatabase(db);
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

        var dbPath = contentRootPath.CombineWith("App_Data/systemjson.sqlite");
        if (File.Exists(dbPath))
            File.Delete(dbPath);

        var connectionString = $"DataSource={dbPath};Cache=Shared";
        var dbFactory = new OrmLiteConnectionFactory(connectionString, SqliteDialect.Provider);
        services.AddSingleton<IDbConnectionFactory>(dbFactory);
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString /*, b => b.MigrationsAssembly(nameof(MyApp))*/));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        ServiceStackHost.InitOptions.ScriptContext.ScriptMethods.AddRange([
            new DbScriptsAsync(),
            new MyValidators(),
        ]);

        services.AddPlugin(AutoQueryAppHost.CreateAutoQueryFeature());

        var app = builder.Build();

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

    static JsonApiClient CreateClient() => new(TestsConfig.ListeningOn);
    private readonly JsonApiClient client = CreateClient();

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
    
}

#endif