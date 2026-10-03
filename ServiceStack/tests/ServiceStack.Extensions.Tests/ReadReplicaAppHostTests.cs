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

namespace ServiceStack.Extensions.Tests;

/// <summary>
/// A row of the primary or its read replica, which records which database it's in
/// </summary>
public class ReplicaRow
{
    [AutoIncrement]
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Database { get; set; } = "";
}

public class QueryReplicaRows : QueryDb<ReplicaRow> {}

/// <summary>
/// The same table in the reporting database
/// </summary>
[NamedConnection(ReadReplicaAppHostTests.Reporting), Alias(nameof(ReplicaRow))]
public class ReportingRow : ReplicaRow {}

public class QueryReportingRows : QueryDb<ReportingRow> {}

public class CreateReportingRow : ICreateDb<ReportingRow>, IReturn<IdResponse>
{
    public string Database { get; set; } = "";
}

public class GetOpenedReportingDatabases : IGet, IReturn<ReplicaDatabasesResponse> {}

public class CreateReplicaRow : ICreateDb<ReplicaRow>, IReturn<IdResponse>
{
    public string Database { get; set; } = "";
}

public class GetReplicaDatabases : IGet, IReturn<ReplicaDatabasesResponse> {}

[NamedConnection(ReadReplicaAppHostTests.Reporting)]
public class GetReportingDatabases : IGet, IReturn<ReplicaDatabasesResponse> {}

public class ReplicaDatabasesResponse
{
    public List<string> Db { get; set; } = [];
    public List<string> ReadDb { get; set; } = [];
    public List<string> OpenDb { get; set; } = [];
    public List<string> OpenReadOnlyDb { get; set; } = [];
    public List<string> OpenReadOnlyDbAsync { get; set; } = [];
}

public class ReplicaServices : Service
{
    static List<string> Databases(IDbConnection db) => db.Select<ReplicaRow>().Map(x => x.Database);

    public async Task<object> Any(GetReplicaDatabases request)
    {
        using var openDb = Request.OpenDb();
        using var openReadOnlyDb = Request.OpenReadOnlyDb();
        using var openReadOnlyDbAsync = await Request.OpenReadOnlyDbAsync();
        return new ReplicaDatabasesResponse {
            Db = Databases(Db),
            ReadDb = Databases(ReadDb),
            OpenDb = Databases(openDb),
            OpenReadOnlyDb = Databases(openReadOnlyDb),
            OpenReadOnlyDbAsync = Databases(openReadOnlyDbAsync),
        };
    }

    public object Any(GetOpenedReportingDatabases request)
    {
        using var db = OpenDbConnection(ReadReplicaAppHostTests.Reporting);
        return new ReplicaDatabasesResponse { Db = Databases(db) };
    }

    public object Any(GetReportingDatabases request) => new ReplicaDatabasesResponse {
        Db = Databases(Db),
        ReadDb = Databases(ReadDb),
    };
}

/// <summary>
/// Service.ReadDb, Request.OpenReadOnlyDb() and AutoQuery with UseReadReplica read from the read replica of the
/// connection a request uses, configured by the AppHost's DbConnectionRequestFilters like Db. Two SQLite databases
/// stand in for the primary and its replica, with rows that say which database they're in.
/// </summary>
[NonParallelizable]
public class ReadReplicaAppHostTests
{
    public const string Reporting = "replica-tests-reporting";
    private const string BaseUrl = "http://localhost:20050";
    private const string TenantHeader = "X-Tenant";

    private readonly List<string> files = [];
    private OrmLiteConnectionFactory dbFactory = null!;

    static readonly FilterSet<int> TenantFilters = FilterSet.Create<int>(f =>
        f.Ensure<ReplicaRow>(x => x.TenantId, tenantId => tenantId));

    // The named connections of the connections that were configured for requests, null for the main database
    private static readonly List<string?> ConfiguredConnections = [];

    class AppHost() : AppHostBase(nameof(ReadReplicaAppHostTests), typeof(ReplicaServices).Assembly)
    {
        public override void Configure()
        {
            DbConnectionRequestFilters.Add((db, req) => {
                lock (ConfiguredConnections) ConfiguredConnections.Add(((OrmLiteConnection)db).NamedConnection);
                if (req.GetHeader(TenantHeader) is { } tenantId)
                    db.UseFilters(TenantFilters.For(int.Parse(tenantId)));
            });
        }
    }

    // A database with rows of 2 tenants that say which database they're in
    private string CreateDatabase(string name)
    {
        var path = Path.Combine(Path.GetTempPath(), $"replica-apphost-{name}-{Guid.NewGuid():N}.sqlite");
        files.Add(path);
        using var db = new OrmLiteConnectionFactory(path, SqliteDialect.Provider, setGlobalDialectProvider: false)
            .OpenDbConnection();
        db.CreateTable<ReplicaRow>();
        db.InsertAll(new[] {
            new ReplicaRow { TenantId = 1, Database = name },
            new ReplicaRow { TenantId = 2, Database = name + "-other-tenant" },
        });
        return path;
    }

    [OneTimeSetUp]
    public void TestFixtureSetUp()
    {
        dbFactory = new OrmLiteConnectionFactory(CreateDatabase("primary"), SqliteDialect.Provider);
        new OrmLiteConfigurationBuilder(dbFactory)
            .AddReadReplica(CreateDatabase("replica"))
            .AddConnection(Reporting, CreateDatabase("reporting"), SqliteDialect.Provider)
            .AddReadReplica(Reporting, CreateDatabase("reporting-replica"));

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
        services.AddPlugin(new AutoQueryFeature { UseReadReplica = true });
        services.AddServiceStack(typeof(ReplicaServices).Assembly);

        var app = builder.Build();
        app.UseServiceStack(appHost);
        app.StartAsync(BaseUrl).Wait();
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown()
    {
        AppHostBase.DisposeApp();
        OrmLiteConnectionFactory.NamedConnections.Remove(Reporting);
        foreach (var file in files)
        {
            try { File.Delete(file); } catch (IOException) { /* still in use */ }
        }
    }

    [SetUp]
    public void SetUp()
    {
        lock (ConfiguredConnections) ConfiguredConnections.Clear();
    }

    private static JsonApiClient CreateClient(int tenantId)
    {
        var client = new JsonApiClient(BaseUrl);
        client.AddHeader(TenantHeader, tenantId.ToString());
        return client;
    }

    [Test]
    public void ReadDb_reads_from_the_replica_with_the_requests_filters()
    {
        using var client = CreateClient(1);
        var response = client.Get(new GetReplicaDatabases());

        // Each only has tenant 1's rows, as DbConnectionRequestFilters are applied to every connection
        Assert.That(response.Db, Is.EqualTo(new[] { "primary" }));
        Assert.That(response.ReadDb, Is.EqualTo(new[] { "replica" }));
        Assert.That(response.OpenDb, Is.EqualTo(new[] { "primary" }));
        Assert.That(response.OpenReadOnlyDb, Is.EqualTo(new[] { "replica" }));
        Assert.That(response.OpenReadOnlyDbAsync, Is.EqualTo(new[] { "replica" }));
    }

    [Test]
    public void ReadDb_reads_from_the_replica_of_the_requests_named_connection()
    {
        using var client = CreateClient(1);
        var response = client.Get(new GetReportingDatabases());
        Assert.That(response.Db, Is.EqualTo(new[] { "reporting" }));
        Assert.That(response.ReadDb, Is.EqualTo(new[] { "reporting-replica" }));
    }

    [Test]
    public void AutoQuery_queries_read_from_the_replica_and_CRUD_writes_to_the_primary()
    {
        using var client = CreateClient(2);
        var results = client.Get(new QueryReplicaRows()).Results;
        Assert.That(results.Map(x => x.Database), Is.EqualTo(new[] { "replica-other-tenant" }));

        var created = client.Post(new CreateReplicaRow { Database = "written" });
        using (var primary = dbFactory.OpenDbConnection())
        {
            var row = primary.SingleById<ReplicaRow>(created.Id.ConvertTo<int>());
            Assert.That(row.Database, Is.EqualTo("written"));
            Assert.That(row.TenantId, Is.EqualTo(2)); // written with the request's filters
            primary.DeleteById<ReplicaRow>(row.Id);
        }
        using (var replica = dbFactory.ReadReplica.OpenDbConnection())
            Assert.That(replica.Select<ReplicaRow>(x => x.Database == "written"), Is.Empty);
    }

    [Test]
    public void Named_connections_are_configured_by_the_requests_filters()
    {
        using var client = CreateClient(1);

        // AutoQuery over a named connection reads its replica, with only the tenant's rows
        var results = client.Get(new QueryReportingRows()).Results;
        Assert.That(results.Map(x => x.Database), Is.EqualTo(new[] { "reporting-replica" }));

        // Service.OpenDbConnection(namedConnection)
        Assert.That(client.Get(new GetOpenedReportingDatabases()).Db, Is.EqualTo(new[] { "reporting" }));

        // AutoQuery CRUD writes to the named connection's primary with the tenant's write rules
        var created = client.Post(new CreateReportingRow { Database = "written" });
        using (var reporting = dbFactory.OpenDbConnection(Reporting))
        {
            var row = reporting.SingleById<ReplicaRow>(created.Id.ConvertTo<int>());
            Assert.That(row.TenantId, Is.EqualTo(1));
            reporting.DeleteById<ReplicaRow>(row.Id);
        }

        // Filters can tell which database they're configuring
        lock (ConfiguredConnections)
            Assert.That(ConfiguredConnections, Is.EqualTo(new[] { Reporting, Reporting, Reporting }));
    }
}
