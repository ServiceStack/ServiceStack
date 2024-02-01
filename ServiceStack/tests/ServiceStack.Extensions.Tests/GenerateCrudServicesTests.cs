#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.Html;
using ServiceStack.OrmLite;
using ServiceStack.Text;

#if NET8_0_OR_GREATER

namespace ServiceStack.Extensions.Tests;

public class GenerateCrudServicesTests
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : DbContext(options)
    {
    }
    
    class AppHost() : AppHostBase(nameof(IdentityJwtAuthProviderTests), typeof(AutoQueryService).Assembly)
    {
        public override void Configure()
        {
            
        }
    }

    public GenerateCrudServicesTests()
    {
        var contentRootPath = "~/../../../".MapServerPath();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = contentRootPath,
            WebRootPath = contentRootPath,
        });
        var services = builder.Services;
        var config = builder.Configuration;

        var dbPath = contentRootPath.CombineWith("App_Data/northwind.sqlite");

        var connectionString = $"DataSource={dbPath};Cache=Shared";
        var dbFactory = new OrmLiteConnectionFactory(connectionString, SqliteDialect.Provider);
        services.AddSingleton<IDbConnectionFactory>(dbFactory);
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString /*, b => b.MigrationsAssembly(nameof(MyApp))*/));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        
        services.AddTransient<TestTransientDisposable>();
        services.AddScoped<TestScopedDisposable>();
        services.AddSingleton<TestSingletonDisposable>();

        ServiceStackHost.InitOptions.ScriptContext.ScriptMethods.AddRange([
            new MyValidators(),
        ]);

        var autoQuery = new AutoQueryFeature
        {
            MaxLimit = 100,
            GenerateCrudServices = new GenerateCrudServices
            {
                DbFactory = dbFactory,
                AutoRegister = true,
                ServiceFilter = (op, req) =>
                {
                    op.Request.AddAttributeIfNotExists(new TagAttribute("Northwind"));
                    if (op.Request.Name.IndexOf("User", StringComparison.Ordinal) >= 0)
                        op.Request.AddAttributeIfNotExists(new ValidateIsAdminAttribute());
                },
                TypeFilter = (type, req) =>
                {
                    if (type.IsCrudCreateOrUpdate())
                    {
                        type.Properties?.Where(p => p.Name is "Notes" or "Description")
                            .Each(p => p.AddAttribute(new InputAttribute { Type = Input.Types.Textarea }));
                    }

                    if (type.IsCrudCreateOrUpdate("Employee"))
                    {
                        type.Property("PhotoPath")
                            .AddAttribute(new InputAttribute { Type = Input.Types.File })
                            .AddAttribute(new UploadToAttribute("employees"));
                    }
                },
            },
        }; 
        services.AddPlugin(autoQuery);

        services.AddServiceStack(typeof(MyServices).Assembly, c =>
        {
            c.AddSwagger(o =>
            {
                //o.AddJwtBearer();
                //o.AddBasicAuth();
            });
        });

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseServiceStack(new AppHost(), options => { options.MapEndpoints(); });

        app.StartAsync(TestsConfig.ListeningOn);
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown() => AppHostBase.DisposeApp();

    List<string> NorthwindTables =
    [
        "Employee",
        "Category",
        "Customer",
        "Shipper",
        "Supplier",
        "Order",
        "Product",
        "OrderDetail",
        "Region",
        "Territory",
        "EmployeeTerritory",
    ];

    [Test]
    public void Endpoints_does_AutoGen_Northwind_services()
    {
        using var db = HostContext.AppHost.GetDbConnection();
        var tableNames = db.GetTableNames();
        // "tableNames:".Print();
        // tableNames.PrintDump();

        var expectedApis = NorthwindTables.SelectMany(x => 
            new [] { $"Query{Words.Pluralize(x)}", $"Create{x}", $"Update{x}", $"Patch{x}", $"Delete{x}" }
        ).ToList();
        
        // "\nexpectedApis:".Print();
        // expectedApis.PrintDump();

        var exclude = new []{ nameof(QueryCaseInsensitiveOrderBy) };
        var apisWithTableNames = HostContext.AppHost.Metadata.GetOperationDtos()
            .Select(x => x.Name)
            .Where(x => NorthwindTables.Any(table => x.Contains(table) || x.Contains(Words.Pluralize(table)))
                && !exclude.Contains(x))
            .ToList(); 
        
        // "\napis:".Print();
        // apisWithTableNames.PrintDump();

        Assert.That(apisWithTableNames, Is.EquivalentTo(expectedApis));
    }

    [Test]
    public void Endpoints_does_generate_Swagger_endpoints_for_AutoGen_Services()
    {
        var url = TestsConfig.ListeningOn.CombineWith("/swagger/v1/swagger.json");
        var json = url.GetJsonFromUrl();
        // json.Print();

        var expectedPaths = NorthwindTables.SelectMany(x => 
            new [] { $"/api/Query{Words.Pluralize(x)}", $"/api/Create{x}", $"/api/Update{x}", $"/api/Patch{x}", $"/api/Delete{x}" }
        ).ToList();

        var obj = (Dictionary<string,object>)JSON.parse(json);
        var paths = (Dictionary<string,object>)obj["paths"];
        
        var autogenPaths = paths.Keys.Where(x => expectedPaths.Any(x.StartsWith)).ToList();

        Assert.That(autogenPaths, Is.EquivalentTo(expectedPaths));
    }

    record class PartialEmployee(int Id, string FirstName, string LastName);
    
    [Test]
    public void Endpoints_can_call_AutoGen_QueryEmployees_Service()
    {
        var url = TestsConfig.ListeningOn.CombineWith("/api/QueryEmployees")
            .AddQueryParam("Ids", "1,3,5")
            .AddQueryParam("OrderBy", "Id");
        
        var json = url.GetJsonFromUrl();
        var results = json.FromJson<QueryResponse<PartialEmployee>>().Results; 

        Assert.That(results.Count, Is.EqualTo(3));
        Assert.That(results[0], Is.EqualTo(new PartialEmployee(1, "Nancy", "Davolio")));
        Assert.That(results[1], Is.EqualTo(new PartialEmployee(3, "Janet", "Leverling")));
        Assert.That(results[2], Is.EqualTo(new PartialEmployee(5, "Steven", "Buchanan")));
    }

    [Test]
    public async Task Endpoints_does_dispose_of_property_injected_services()
    {
        TestTransientDisposable.IsDisposed = TestScopedDisposable.IsDisposed = TestSingletonDisposable.IsDisposed = false;
        
        var client = new JsonApiClient(TestsConfig.ListeningOn);
        var response = await client.SendAsync(new TestIoc());

        response.PrintDump();
        Assert.That(response.Results, Is.EquivalentTo((string[])[
            nameof(IAppSettings),
            nameof(TestTransientDisposable),
            nameof(TestScopedDisposable),
            nameof(TestSingletonDisposable),
        ]));
        
        Assert.That(TestTransientDisposable.IsDisposed);
        Assert.That(TestScopedDisposable.IsDisposed);
        Assert.That(TestSingletonDisposable.IsDisposed, Is.False);
    }
}

public class TestTransientDisposable : IDisposable
{
    public static bool IsDisposed { get; set; }
    public void Dispose() => IsDisposed = true;
}

public class TestScopedDisposable : IDisposable
{
    public static bool IsDisposed { get; set; }
    public void Dispose() => IsDisposed = true;
}

public class TestSingletonDisposable : IDisposable
{
    public static bool IsDisposed { get; set; }
    public void Dispose() => IsDisposed = true;
}

public class TestIoc : IReturn<StringsResponse> { }

public class TestIocService : Service 
{
    [FromServices]
    public IAppSettings? AppSettings { get; set; }
    
    [FromServices]
    public TestTransientDisposable? TestTransientDisposable { get; set; }
    
    [FromServices]
    public TestScopedDisposable? TestScopedDisposable { get; set; }
    
    [FromServices]
    public TestSingletonDisposable? TestSingletonDisposable { get; set; }

    public object Any(TestIoc request)
    {
        var to = new StringsResponse();
        if (AppSettings != null)
            to.Results.Add(nameof(IAppSettings));
        if (TestTransientDisposable != null)
            to.Results.Add(nameof(TestTransientDisposable));
        if (TestScopedDisposable != null)
            to.Results.Add(nameof(TestScopedDisposable));
        if (TestSingletonDisposable != null)
            to.Results.Add(nameof(TestSingletonDisposable));
        return to;
    }
}

#endif