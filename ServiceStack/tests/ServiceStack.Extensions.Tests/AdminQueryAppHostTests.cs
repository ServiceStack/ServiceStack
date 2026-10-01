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
using ServiceStack.Admin;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace ServiceStack.Extensions.Tests;

public class AdminQueryItem
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Qty { get; set; }
}

public class RunAdminQueries : IReturn<EmptyResponse> {}

public class AdminQueryTestServices : Service
{
    public const string Reports = "reports";

    public object Any(RunAdminQueries request)
    {
        var dbFactory = TryResolve<IDbConnectionFactory>();
        using (var db = dbFactory.OpenDbConnection())
        {
            db.Insert(new AdminQueryItem { Name = "Main", Qty = 1 });
            db.Select<AdminQueryItem>(x => x.Qty >= 0);
        }
        using (var db = dbFactory.OpenDbConnection(Reports))
        {
            db.Select<AdminQueryItem>(x => x.Name.StartsWith("Report"));
        }
        return new EmptyResponse();
    }
}

/// <summary>
/// Explaining and re-running profiled OrmLite queries from the Admin UI, through a real ASP.NET Core AppHost with
/// both the ProfilingFeature and AdminDatabaseFeature plugins.
/// </summary>
[NonParallelizable]
public class AdminQueryAppHostTests
{
    private const string BaseUrl = "http://localhost:20047";
    private const string AuthSecret = "secret";

    private readonly ProfilingFeature profiling = new();
    private readonly string mainDb = Path.Combine(Path.GetTempPath(), $"adminquery-main-{Guid.NewGuid():N}.sqlite");
    private readonly string reportsDb = Path.Combine(Path.GetTempPath(), $"adminquery-reports-{Guid.NewGuid():N}.sqlite");
    private JsonApiClient client = null!;

    class AppHost() : AppHostBase(nameof(AdminQueryAppHostTests), typeof(AdminQueryTestServices).Assembly)
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
        dbFactory.RegisterConnection(AdminQueryTestServices.Reports, reportsDb, SqliteDialect.Provider);
        using (var db = dbFactory.OpenDbConnection())
        {
            db.DropAndCreateTable<AdminQueryItem>();
        }
        using (var db = dbFactory.OpenDbConnection(AdminQueryTestServices.Reports))
        {
            db.DropAndCreateTable<AdminQueryItem>();
            db.InsertAll(new[] {
                new AdminQueryItem { Name = "Report A", Qty = 10 },
                new AdminQueryItem { Name = "Report B", Qty = 20 },
                new AdminQueryItem { Name = "Report C", Qty = 30 },
            });
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
        services.AddPlugin(profiling);
        services.AddPlugin(new AdminDatabaseFeature { RunQueryLimit = 2 });
        services.AddServiceStack(typeof(AdminQueryTestServices).Assembly);

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

    private async Task<DiagnosticEntry[]> GetSlowestQueriesAsync()
    {
        await client.ApiAsync(new RunAdminQueries());
        var api = await client.ApiAsync(new AdminProfiling { Slow = true });
        api.ThrowIfError();
        return api.Response!.Results.ToArray();
    }

    [Test]
    public async Task Slowest_queries_are_OrmLite_commands_with_their_connection()
    {
        var slowest = await GetSlowestQueriesAsync();

        Assert.That(slowest, Is.Not.Empty);
        Assert.That(slowest.All(x => x.Source == "OrmLite" && x.Command != null && x.Duration != null));
        // Slowest first
        Assert.That(slowest.Map(x => x.Duration), Is.Ordered.Descending);

        var report = slowest.First(x => x.NamedConnection == AdminQueryTestServices.Reports);
        Assert.That(report.Command, Does.StartWith("SELECT"));
        Assert.That(slowest.Any(x => x.NamedConnection == null && x.Command.StartsWith("INSERT")));
    }

    [Test]
    public async Task Can_explain_a_profiled_query_on_its_named_connection()
    {
        var slowest = await GetSlowestQueriesAsync();
        var report = slowest.First(x => x.NamedConnection == AdminQueryTestServices.Reports);

        var api = await client.ApiAsync(new AdminExplainQuery { Id = report.Id });
        api.ThrowIfError();
        Assert.That(api.Response!.Plan, Does.Contain(nameof(AdminQueryItem)));
    }

    [Test]
    public async Task Can_rerun_a_profiled_query_with_its_params()
    {
        var slowest = await GetSlowestQueriesAsync();
        var report = slowest.First(x => x.NamedConnection == AdminQueryTestServices.Reports);

        var api = await client.ApiAsync(new AdminRunQuery { Id = report.Id });
        api.ThrowIfError();
        var response = api.Response!;
        Assert.That(response.Columns, Is.EqualTo(new[] { "Id", "Name", "Qty" }));
        // Limited to the AdminDatabaseFeature's RunQueryLimit
        Assert.That(response.Results.Count, Is.EqualTo(2));
        Assert.That(response.Truncated);
        Assert.That(response.Results.Map(x => $"{x["Name"]}"), Is.EqualTo(new[] { "Report A", "Report B" }));

        var one = await client.ApiAsync(new AdminRunQuery { Id = report.Id, Take = 1 });
        Assert.That(one.Response!.Results.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task Databases_report_whether_they_can_analyze_queries()
    {
        Assert.That(new AdminDatabaseFeature().RunQueryLimit, Is.EqualTo(20));

        var api = await client.ApiAsync(new MetadataApp());
        api.ThrowIfError();
        var info = api.Response!.Plugins.AdminDatabase;
        Assert.That(info.RunQueryLimit, Is.EqualTo(2));
        // SQLite can explain queries, but not analyze them
        Assert.That(info.Databases.Map(x => x.Name), Is.EquivalentTo(new[] { "main", AdminQueryTestServices.Reports }));
        Assert.That(info.Databases.All(x => x.SupportsAnalyze != true));
    }

    [Test]
    public async Task Explaining_and_rerunning_queries_isnt_profiled()
    {
        var slowest = await GetSlowestQueriesAsync();
        var report = slowest.First(x => x.NamedConnection == AdminQueryTestServices.Reports);
        var total = profiling.GetSlowestQueries().Count;

        (await client.ApiAsync(new AdminExplainQuery { Id = report.Id })).ThrowIfError();
        (await client.ApiAsync(new AdminRunQuery { Id = report.Id })).ThrowIfError();

        Assert.That(profiling.GetSlowestQueries().Count, Is.EqualTo(total));
    }

    [Test]
    public async Task Only_SELECT_queries_in_the_profiling_history_can_be_run()
    {
        var slowest = await GetSlowestQueriesAsync();
        var insert = slowest.First(x => x.Command.StartsWith("INSERT"));

        var explain = await client.ApiAsync(new AdminExplainQuery { Id = insert.Id });
        Assert.That(explain.Error, Is.Not.Null);
        var run = await client.ApiAsync(new AdminRunQuery { Id = insert.Id });
        Assert.That(run.Error, Is.Not.Null);

        var missing = await client.ApiAsync(new AdminExplainQuery { Id = long.MaxValue });
        Assert.That(missing.Error!.ErrorCode, Is.EqualTo(nameof(HttpStatusCode.NotFound)));
    }

    [Test]
    public async Task Requires_the_admin_role()
    {
        var slowest = await GetSlowestQueriesAsync();
        var report = slowest.First(x => x.NamedConnection == AdminQueryTestServices.Reports);

        using var anon = new JsonApiClient(BaseUrl);
        Assert.That((await anon.ApiAsync(new AdminExplainQuery { Id = report.Id })).Error, Is.Not.Null);
        Assert.That((await anon.ApiAsync(new AdminRunQuery { Id = report.Id })).Error, Is.Not.Null);
    }
}
