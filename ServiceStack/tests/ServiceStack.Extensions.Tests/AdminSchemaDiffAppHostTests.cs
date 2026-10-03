using System.Text;
using ServiceStack.Logging;
#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace ServiceStack.Extensions.Tests;

// The model of an AutoQuery API, whose table was created when its Sku was required and before it had a Stock
public class SchemaDiffProduct
{
    [AutoIncrement]
    public int Id { get; set; }

    [StringLength(100)]
    public string? Sku { get; set; }

    public string? Name { get; set; }

    [Index]
    public int Stock { get; set; }
}

[Alias(nameof(SchemaDiffProduct))]
public class SchemaDiffProductV1
{
    [AutoIncrement]
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string? Sku { get; set; }

    public string? Name { get; set; }
    public string? Barcode { get; set; }
}

public class QuerySchemaDiffProducts : QueryDb<SchemaDiffProduct> {}

// A model that's the same as its table
public class SchemaDiffCategory
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class QuerySchemaDiffCategories : QueryDb<SchemaDiffCategory> {}

// A table of ASP.NET Core Identity, which isn't compared as it isn't managed by OrmLite
[Alias("AspNetSchemaDiffUsers")]
public class SchemaDiffUser
{
    public string? Id { get; set; }
    public string? UserName { get; set; }
}

public class QuerySchemaDiffUsers : QueryDb<SchemaDiffUser> {}

// A model that isn't used by an AutoQuery API, which is compared as a migration creates its table
public class SchemaDiffInvoice
{
    public int Id { get; set; }
    public string? Number { get; set; }
}

// The migration's copy of the table, as it was when the migration was written
public class Migration1001 : MigrationBase
{
    public class SchemaDiffInvoice
    {
        public int Id { get; set; }
        public string? Number { get; set; }
    }

    public override void Up() => Db.CreateTable<SchemaDiffInvoice>();
    public override void Down() => Db.DropTable<SchemaDiffInvoice>();
}

// A model of another database that isn't used by an AutoQuery API
[NamedConnection(AdminSchemaDiffAppHostTests.Reports)]
public class SchemaDiffReport
{
    public int Id { get; set; }
    public string? Title { get; set; }
}

// AutoQuery APIs with their own implementation, as this host doesn't have the AutoQueryFeature
public class SchemaDiffServices : Service
{
    public object Any(QuerySchemaDiffProducts request) => new QueryResponse<SchemaDiffProduct>();
    public object Any(QuerySchemaDiffCategories request) => new QueryResponse<SchemaDiffCategory>();
    public object Any(QuerySchemaDiffUsers request) => new QueryResponse<SchemaDiffUser>();
}

/// <summary>
/// The Schema Diff of the Database Admin UI: the differences between an App's models and their tables, with the
/// migration that makes the database the same as its models.
/// </summary>
[NonParallelizable]
public class AdminSchemaDiffAppHostTests
{
    public const string Reports = "reports";
    private const string BaseUrl = "http://localhost:20048";
    private const string AuthSecret = "secret";

    private readonly string mainDb = Path.Combine(Path.GetTempPath(), $"schemadiff-main-{Guid.NewGuid():N}.sqlite");
    private readonly string reportsDb = Path.Combine(Path.GetTempPath(), $"schemadiff-reports-{Guid.NewGuid():N}.sqlite");
    private JsonApiClient client = null!;

    class AppHost() : AppHostBase(nameof(AdminSchemaDiffAppHostTests), typeof(AdminSchemaDiffAppHostTests).Assembly)
    {
        public override void Configure()
        {
            SetConfig(new HostConfig { AdminAuthSecret = AuthSecret });
        }
    }

    [OneTimeSetUp]
    public void TestFixtureSetUp()
    {
        var dbFactory = new OrmLiteConnectionFactory(mainDb, SqliteDialect.Provider);
        dbFactory.RegisterConnection(Reports, reportsDb, SqliteDialect.Provider);
        using (var db = dbFactory.OpenDbConnection())
        {
            db.DropAndCreateTable<SchemaDiffProductV1>();
            db.DropAndCreateTable<SchemaDiffCategory>();
            db.DropAndCreateTable<SchemaDiffInvoice>();
        }

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
        var services = builder.Services;
        services.AddLogging(o => o.ClearProviders());
        services.AddSingleton<IDbConnectionFactory>(dbFactory);
        services.AddPlugin(new AdminDatabaseFeature {
            ModelTypes = { typeof(SchemaDiffReport) },
            // The assembly has the models of other fixtures
            ModelTypesFilter = type => type.Name.StartsWith("SchemaDiff"),
            MigrationNamespace = "MyApp.Migrations",
        });
        services.AddServiceStack(typeof(AdminSchemaDiffAppHostTests).Assembly);

        var app = builder.Build();
        app.UseServiceStack(appHost);
        app.StartAsync(BaseUrl).Wait();

        client = new JsonApiClient(BaseUrl);
        client.AddHeader("authsecret", AuthSecret);
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown()
    {
        client.Dispose();
        AppHostBase.DisposeApp();
        foreach (var file in new[] { mainDb, reportsDb })
        {
            try { File.Delete(file); } catch (IOException) { /* still in use */ }
        }
    }

    [Test]
    public async Task Compares_the_models_of_AutoQuery_APIs_with_their_tables()
    {
        var api = await client.ApiAsync(new AdminSchemaDiff());
        api.ThrowIfError();
        var response = api.Response!;

        // The models of the default connection, not the model of the reports database. SchemaDiffInvoice is the
        // App's model of a table its migrations create.
        Assert.That(response.Models, Is.EqualTo(new[] { nameof(SchemaDiffCategory), nameof(SchemaDiffInvoice), nameof(SchemaDiffProduct) }));
        Assert.That(response.Warnings, Is.Empty);
        // AspNet* tables aren't compared
        Assert.That(response.Ignored, Is.EqualTo(new[] { "AspNetSchemaDiffUsers" }));

        // SchemaDiffCategory is the same as its table
        Assert.That(response.Results.Map(x => x.Model), Is.All.EqualTo(nameof(SchemaDiffProduct)));

        var changes = response.Results.ToDictionary(x => x.Name!);
        Assert.That(changes["Stock"].Type, Is.EqualTo("AddColumn"));
        Assert.That(changes["Stock"].Sql, Does.StartWith("ALTER TABLE"));
        Assert.That(changes["Stock"].IsDestructive, Is.Null);

        Assert.That(changes["Sku"].Type, Is.EqualTo("AlterColumn"));
        Assert.That(changes["Sku"].DatabaseColumn, Is.EqualTo("VARCHAR(20) NOT NULL"));
        Assert.That(changes["Sku"].ModelColumn, Is.EqualTo("VARCHAR(100) NULL"));
        Assert.That(changes["Sku"].Sql, Is.Null); // SQLite can't alter a column

        Assert.That(changes["Barcode"].Type, Is.EqualTo("DropColumn"));
        Assert.That(changes["Barcode"].IsDestructive, Is.True);
        Assert.That(changes["Barcode"].Description, Does.Contain("isn't in SchemaDiffProduct"));

        Assert.That(changes["idx_schemadiffproduct_stock"].Type, Is.EqualTo("CreateIndex"));
        Assert.That(response.Results.Count, Is.EqualTo(4));
    }

    [Test]
    public async Task Returns_a_migration_that_makes_the_changes()
    {
        var api = await client.ApiAsync(new AdminSchemaDiff());
        api.ThrowIfError();
        var response = api.Response!;

        Assert.That(response.MigrationName, Does.Match(@"^Migration\d+$"));
        Assert.That(response.MigrationNamespace, Is.EqualTo("MyApp.Migrations"));

        var migration = response.Migration!;
        Assert.That(migration, Does.Contain("namespace MyApp.Migrations;"));
        Assert.That(migration, Does.Contain($"public class {response.MigrationName} : MigrationBase"));
        Assert.That(migration, Does.Contain("Db.AddColumn<SchemaDiffProduct>(x => x.Stock);"));
        Assert.That(migration, Does.Contain("// Db.DropColumn<SchemaDiffProduct>(\"Barcode\");"));
        Assert.That(migration, Does.Contain("Db.DropIndex<SchemaDiffProduct>(\"idx_schemadiffproduct_stock\");"));

        // With the class name and namespace that's asked for
        var named = await client.ApiAsync(new AdminSchemaDiff { Migration = "Migration2000", Namespace = "Acme.Data" });
        Assert.That(named.Response!.Migration, Does.Contain("namespace Acme.Data;")
            .And.Contain("public class Migration2000 : MigrationBase"));
    }

    [Test]
    public async Task Compares_the_models_of_a_named_connection()
    {
        var api = await client.ApiAsync(new AdminSchemaDiff { Db = Reports });
        api.ThrowIfError();
        var response = api.Response!;

        Assert.That(response.Models, Is.EqualTo(new[] { nameof(SchemaDiffReport) }));
        var change = response.Results.Single();
        Assert.That(change.Type, Is.EqualTo("CreateTable"));
        Assert.That(change.Sql, Does.StartWith("CREATE TABLE"));
        Assert.That(response.Migration, Does.Contain("Db.CreateTable<SchemaDiffReport>();"));
    }

    [Test]
    public async Task Databases_that_are_the_same_as_their_models_have_no_migration()
    {
        var dbFactory = HostContext.Resolve<IDbConnectionFactory>();
        using (var db = dbFactory.OpenDbConnection(Reports))
        {
            db.CreateTable<SchemaDiffReport>();
        }
        try
        {
            var api = await client.ApiAsync(new AdminSchemaDiff { Db = Reports });
            api.ThrowIfError();
            Assert.That(api.Response!.Models, Is.EqualTo(new[] { nameof(SchemaDiffReport) }));
            Assert.That(api.Response!.Results, Is.Empty);
            Assert.That(api.Response!.Migration, Is.Null);
        }
        finally
        {
            using var db = dbFactory.OpenDbConnection(Reports);
            db.DropTable<SchemaDiffReport>();
        }
    }

    [Test]
    public void Logs_the_schema_diff_of_each_database()
    {
        var dbFactory = HostContext.Resolve<IDbConnectionFactory>();
        using (var db = dbFactory.OpenDbConnection(Reports))
        {
            db.CreateTable<SchemaDiffReport>();
        }
        try
        {
            var logs = new StringBuilder();
            HostContext.AppHost.GetPlugin<AdminDatabaseFeature>()
                .LogSchemaDiffs(HostContext.AppHost, new StringBuilderLog(typeof(AdminDatabaseFeature), logs));
            var text = logs.ToString();

            // A warning with the differences of the main database
            Assert.That(text, Does.Contain("WARN: Schema Diff: the tables of the main database aren't the same as their models"));
            Assert.That(text, Does.Contain("+ Stock"));
            Assert.That(text, Does.Contain("Ignored: AspNetSchemaDiffUsers"));
            // The reports database is the same as its model
            Assert.That(text, Does.Contain("INFO: Schema Diff: the tables of the reports database are the same as their models (1 model)"));
            Assert.That(text, Does.Not.Contain("ERROR"));
        }
        finally
        {
            using var db = dbFactory.OpenDbConnection(Reports);
            db.DropTable<SchemaDiffReport>();
        }
    }

    [Test]
    public void Writes_the_migration_of_a_database_to_the_Apps_migrations()
    {
        var feature = HostContext.AppHost.GetPlugin<AdminDatabaseFeature>();
        var dir = Path.Combine(Path.GetTempPath(), $"schemadiff-migrations-{Guid.NewGuid():N}");
        var migrationsPath = feature.MigrationsPath;
        feature.MigrationsPath = dir;
        try
        {
            var path = feature.WriteMigration(HostContext.AppHost)!;
            Assert.That(Path.GetDirectoryName(path), Is.EqualTo(dir));
            Assert.That(Path.GetFileName(path), Does.Match(@"^Migration\d+\.cs$"));
            var source = File.ReadAllText(path);
            Assert.That(source, Does.StartWith("// GENERATED BY SCHEMA DIFF"));
            Assert.That(source, Does.Contain("#warning Generated by Schema Diff"));
            Assert.That(source, Does.Contain("namespace MyApp.Migrations;"));
            Assert.That(source, Does.Contain("Db.AddColumn<SchemaDiffProduct>(x => x.Stock);"));

            // An existing migration isn't replaced
            Assert.Throws<InvalidOperationException>(() => feature.WriteMigration(HostContext.AppHost));

            // The migration of a named connection runs on its database
            File.Delete(path);
            var reports = File.ReadAllText(feature.WriteMigration(HostContext.AppHost, Reports)!);
            Assert.That(reports, Does.Contain($"[NamedConnection(\"{Reports}\")]\npublic class Migration"));
            Assert.That(reports, Does.Contain("Db.CreateTable<SchemaDiffReport>();"));

            Assert.Throws<ArgumentException>(() => feature.WriteMigration(HostContext.AppHost, "unknown"));
        }
        finally
        {
            feature.MigrationsPath = migrationsPath;
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task Does_not_change_the_database()
    {
        await client.ApiAsync(new AdminSchemaDiff());

        using var db = HostContext.Resolve<IDbConnectionFactory>().OpenDbConnection();
        Assert.That(db.ColumnExists("Barcode", nameof(SchemaDiffProduct)));
        Assert.That(db.ColumnExists("Stock", nameof(SchemaDiffProduct)), Is.False);
        Assert.That(db.GetTableNames().Any(x => x.StartsWith("ormlite_diff")), Is.False);
    }

    [Test]
    public async Task Rejects_names_that_are_not_valid_in_a_migration()
    {
        var api = await client.ApiAsync(new AdminSchemaDiff { Migration = "Migration1; }" });
        Assert.That(api.Error?.ErrorCode, Is.EqualTo(nameof(ArgumentException)));

        api = await client.ApiAsync(new AdminSchemaDiff { Namespace = "My App" });
        Assert.That(api.Error?.ErrorCode, Is.EqualTo(nameof(ArgumentException)));
    }

    [Test]
    public async Task Requires_the_admin_role()
    {
        using var anon = new JsonApiClient(BaseUrl);
        var api = await anon.ApiAsync(new AdminSchemaDiff());
        Assert.That(api.Error, Is.Not.Null);
        Assert.That(api.Failed);
        Assert.That(api.Response, Is.Null);
    }
}
