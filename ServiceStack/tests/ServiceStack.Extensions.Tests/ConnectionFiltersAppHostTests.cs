#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Script;
using ServiceStack.Web;

namespace ServiceStack.Extensions.Tests;

public interface IHasTenant
{
    int TenantId { get; }
}

public interface IHasAudit
{
    string? CreatedBy { get; }
    string? ModifiedBy { get; }
}

public class TenantItem : IHasTenant, IHasAudit
{
    [AutoIncrement]
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = "";
    public int Qty { get; set; }
    [IgnoreOnUpdate]
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

public class QueryTenantItems : QueryDb<TenantItem>
{
    public string? Name { get; set; }
    public int? QtyGreaterThan { get; set; }
}

public class CreateTenantItem : ICreateDb<TenantItem>, IReturn<IdResponse>
{
    public string Name { get; set; } = "";
    public int Qty { get; set; }
    public int? TenantId { get; set; }
}

public class UpdateTenantItem : IPatchDb<TenantItem>, IReturn<IdResponse>
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int? Qty { get; set; }
}

public class DeleteTenantItem : IDeleteDb<TenantItem>, IReturnVoid
{
    public int Id { get; set; }
}

public class GetTenantItem : IGet, IReturn<TenantItem>
{
    public int Id { get; set; }
}

public class GetTenantItemAsync : IGet, IReturn<TenantItem>
{
    public int Id { get; set; }
}

public class CountTenantItems : IGet, IReturn<StringResponse> {}

public static class TenantDbExtensions
{
    /// <summary>
    /// The App's rules for a tenant's user, defined once
    /// </summary>
    public static IDbConnection ForUser(this IDbConnection db, int tenantId, string userId)
    {
        db.EnsureFilter<IHasTenant>(x => x.TenantId == tenantId);
        db.EnsureWrites<IHasTenant>(x => x.TenantId, tenantId);
        db.OnInsert<IHasAudit>(x => x.CreatedBy, userId);
        db.OnWrite<IHasAudit>(x => x.ModifiedBy, userId);
        return db;
    }
}

public class TenantItemServices : Service
{
    // Service.Db opens its connection with AppHost.GetDbConnection()
    public object Any(GetTenantItem request) =>
        Db.SingleById<TenantItem>(request.Id) ?? throw HttpError.NotFound("Item not found");

    public async Task<object> Any(GetTenantItemAsync request)
    {
        using var db = await HostContext.AppHost.GetDbConnectionAsync(Request);
        return await db.SingleByIdAsync<TenantItem>(request.Id) ?? throw HttpError.NotFound("Item not found");
    }

    public object Any(CountTenantItems request) => new StringResponse {
        Result = $"{Db.Count<TenantItem>()},{Db.WithoutFilters().Count<TenantItem>()}",
    };
}

/// <summary>
/// Overriding AppHost.GetDbConnection() applies OrmLite's connection filters and write rules to every connection
/// ServiceStack opens for a request, i.e. Db in Services, AutoQuery and AutoQuery CRUD.
/// </summary>
[NonParallelizable]
public class ConnectionFiltersAppHostTests
{
    private const string BaseUrl = "http://localhost:20048";
    private const string TenantHeader = "X-Tenant";
    private const string UserHeader = "X-User";

    private readonly string dbPath = Path.Combine(Path.GetTempPath(), $"connfilters-{Guid.NewGuid():N}.sqlite");
    private OrmLiteConnectionFactory dbFactory = null!;

    class AppHost() : AppHostBase(nameof(ConnectionFiltersAppHostTests), typeof(TenantItemServices).Assembly)
    {
        public override void Configure() {}

        public override IDbConnection GetDbConnection(IRequest? req, Action<IDbConnection> configure) =>
            ConfigureDb(base.GetDbConnection(req, configure), req);

        public override async Task<IDbConnection> GetDbConnectionAsync(IRequest? req, Action<IDbConnection> configure) =>
            ConfigureDb(await base.GetDbConnectionAsync(req, configure), req);

        // e.g. Apps would typically use the tenant and user of the authenticated session
        static IDbConnection ConfigureDb(IDbConnection db, IRequest? req)
        {
            var tenantId = req?.GetHeader(TenantHeader);
            if (tenantId != null)
                db.ForUser(int.Parse(tenantId), req!.GetHeader(UserHeader) ?? "anon");
            return db;
        }
    }

    [OneTimeSetUp]
    public void TestFixtureSetUp()
    {
        dbFactory = new OrmLiteConnectionFactory(dbPath, SqliteDialect.Provider);

        var appHost = new AppHost();
        var contentRootPath = "~/../../../".MapServerPath();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            ContentRootPath = contentRootPath,
            WebRootPath = contentRootPath,
        });
        // The AppHost scans the whole assembly, which includes services from other fixtures
        // that depend on services this host doesn't register. They're never resolved here.
        builder.Host.UseDefaultServiceProvider(o => {
            o.ValidateOnBuild = false;
            o.ValidateScopes = false;
        });
        // Validators used by Request DTOs of other fixtures in this assembly
        ServiceStackHost.InitOptions.ScriptContext.ScriptMethods.AddRange([
            new DbScriptsAsync(),
            new MyValidators(),
        ]);
        var services = builder.Services;
        services.AddLogging(o => o.ClearProviders());
        services.AddSingleton<IDbConnectionFactory>(dbFactory);
        services.AddPlugin(new AutoQueryFeature());
        services.AddServiceStack(typeof(TenantItemServices).Assembly);

        var app = builder.Build();
        app.UseServiceStack(appHost);
        app.StartAsync(BaseUrl).Wait();
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown()
    {
        AppHostBase.DisposeApp();
        try { File.Delete(dbPath); } catch (IOException) { /* still in use */ }
    }

    // Items of 2 tenants: ids 1-2 are tenant 1's, id 3 is tenant 2's
    [SetUp]
    public void SetUp()
    {
        using var db = dbFactory.OpenDbConnection();
        db.DropAndCreateTable<TenantItem>();
        db.InsertAll(new[] {
            new TenantItem { TenantId = 1, Name = "Apple", Qty = 10, CreatedBy = "seed" },
            new TenantItem { TenantId = 1, Name = "Banana", Qty = 20, CreatedBy = "seed" },
            new TenantItem { TenantId = 2, Name = "Cherry", Qty = 30, CreatedBy = "seed" },
        });
    }

    private static JsonApiClient CreateClient(int tenantId, string userId)
    {
        var client = new JsonApiClient(BaseUrl);
        client.AddHeader(TenantHeader, tenantId.ToString());
        client.AddHeader(UserHeader, userId);
        return client;
    }

    private Dictionary<int, TenantItem> AllItems()
    {
        using var db = dbFactory.OpenDbConnection();
        return db.Select<TenantItem>().ToDictionary(x => x.Id);
    }

    [Test]
    public async Task AutoQuery_only_returns_the_tenants_rows()
    {
        using var client = CreateClient(1, "alice");

        var all = await client.ApiAsync(new QueryTenantItems());
        all.ThrowIfError();
        Assert.That(all.Response!.Results.Map(x => x.Name), Is.EquivalentTo(new[] { "Apple", "Banana" }));

        // Conditions from the request only filter the tenant's rows
        var byName = await client.ApiAsync(new QueryTenantItems { Name = "Cherry" });
        Assert.That(byName.Response!.Results, Is.Empty);
        var byQty = await client.ApiAsync(new QueryTenantItems { QtyGreaterThan = 15 });
        Assert.That(byQty.Response!.Results.Map(x => x.Name), Is.EqualTo(new[] { "Banana" }));

        using var otherTenant = CreateClient(2, "bob");
        var other = await otherTenant.ApiAsync(new QueryTenantItems());
        Assert.That(other.Response!.Results.Map(x => x.Name), Is.EqualTo(new[] { "Cherry" }));
    }

    [Test]
    public async Task Services_using_Db_only_see_the_tenants_rows()
    {
        using var client = CreateClient(1, "alice");

        Assert.That((await client.ApiAsync(new GetTenantItem { Id = 1 })).Response!.Name, Is.EqualTo("Apple"));
        Assert.That((await client.ApiAsync(new GetTenantItem { Id = 3 })).Error, Is.Not.Null);

        // Connections opened with GetDbConnectionAsync()
        Assert.That((await client.ApiAsync(new GetTenantItemAsync { Id = 2 })).Response!.Name, Is.EqualTo("Banana"));
        Assert.That((await client.ApiAsync(new GetTenantItemAsync { Id = 3 })).Error, Is.Not.Null);

        // WithoutFilters() for admin tasks
        Assert.That((await client.ApiAsync(new CountTenantItems())).Response!.Result, Is.EqualTo("2,3"));
    }

    [Test]
    public async Task AutoQuery_CRUD_writes_the_tenant_and_audit_columns()
    {
        using var client = CreateClient(1, "alice");

        var create = await client.ApiAsync(new CreateTenantItem { Name = "Date", Qty = 5 });
        create.ThrowIfError();
        var id = int.Parse(create.Response!.Id);

        var created = AllItems()[id];
        Assert.That(created.TenantId, Is.EqualTo(1));
        Assert.That(created.CreatedBy, Is.EqualTo("alice"));
        Assert.That(created.ModifiedBy, Is.EqualTo("alice"));

        using var bob = CreateClient(1, "bob");
        (await bob.ApiAsync(new UpdateTenantItem { Id = id, Qty = 6 })).ThrowIfError();

        var updated = AllItems()[id];
        Assert.That(updated.Qty, Is.EqualTo(6));
        Assert.That(updated.Name, Is.EqualTo("Date"));
        Assert.That(updated.CreatedBy, Is.EqualTo("alice"));
        Assert.That(updated.ModifiedBy, Is.EqualTo("bob"));
    }

    [Test]
    public async Task AutoQuery_CRUD_cant_write_for_another_tenant()
    {
        using var client = CreateClient(1, "alice");

        var create = await client.ApiAsync(new CreateTenantItem { Name = "Elderberry", TenantId = 2 });
        Assert.That(create.Error, Is.Not.Null);
        Assert.That(AllItems().Values.Any(x => x.Name == "Elderberry"), Is.False);
    }

    [Test]
    public async Task AutoQuery_CRUD_cant_change_another_tenants_rows()
    {
        using var client = CreateClient(1, "alice");

        // Another tenant's row is treated like a row that doesn't exist
        await client.ApiAsync(new UpdateTenantItem { Id = 3, Name = "Changed", Qty = 0 });
        await client.ApiAsync(new DeleteTenantItem { Id = 3 });

        var cherry = AllItems()[3];
        Assert.That(cherry.Name, Is.EqualTo("Cherry"));
        Assert.That(cherry.Qty, Is.EqualTo(30));
        Assert.That(cherry.ModifiedBy, Is.Null);

        // The tenant's own rows can be deleted
        (await client.ApiAsync(new DeleteTenantItem { Id = 1 })).ThrowIfError();
        Assert.That(AllItems().Keys, Is.EquivalentTo(new[] { 2, 3 }));
    }

    [Test]
    public async Task Requests_without_a_tenant_arent_filtered()
    {
        using var client = new JsonApiClient(BaseUrl);
        var all = await client.ApiAsync(new QueryTenantItems());
        Assert.That(all.Response!.Results.Count, Is.EqualTo(3));
    }
}
