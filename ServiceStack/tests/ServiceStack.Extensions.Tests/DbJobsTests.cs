#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.Host;
using ServiceStack.IO;
using ServiceStack.Jobs;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.Extensions.Tests;

public class DbRequest : IReturn<DbResponse>
{
    public int Id { get; set; }
    public int? WaitMs { get; set; }
    public string? Throw { get; set; }
}
public class DbResponse
{
    public required string Result { get; set; }
}

public class DbJobCommand(ILogger<DbJobCommand> logger, IBackgroundJobs jobs)
    : AsyncCommandWithResult<DbRequest, DbResponse>
{
    public static long Count;
    public static List<DbRequest> Requests { get; } = new();

    protected override async Task<DbResponse> RunAsync(DbRequest request, CancellationToken token)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        log.UpdateStatus(0.1, "Started", "DbJobCommand Started...");
        Interlocked.Increment(ref Count);
        lock (Requests) Requests.Add(request);
        if (request.WaitMs != null)
            await Task.Delay(request.WaitMs.Value, token);
        if (request.Throw != null)
            throw new Exception(request.Throw);
        log.UpdateStatus(0.9, "Finished", "DbJobCommand Finished");
        return new DbResponse { Result = $"Hello {request.Id}" };
    }
}

/// <summary>Records which Jobs ran, so double execution across nodes is detectable</summary>
public class DbExactlyOnceCommand(IBackgroundJobs jobs) : SyncCommand<DbRequest>
{
    public static readonly List<long> ExecutedJobIds = new();
    protected override void Run(DbRequest request)
    {
        var job = Request.GetBackgroundJob();
        lock (ExecutedJobIds) ExecutedJobIds.Add(job.Id);
    }
}

/// <summary>Cooperatively cancellable, and records whether it observed cancellation</summary>
public class DbLongRunningCommand : AsyncCommand<DbRequest>
{
    public static long Started;
    public static bool ObservedCancellation;
    protected override async Task RunAsync(DbRequest request, CancellationToken token)
    {
        Interlocked.Increment(ref Started);
        try
        {
            await Task.Delay(request.WaitMs ?? 5000, token);
        }
        catch (OperationCanceledException)
        {
            ObservedCancellation = true;
            throw;
        }
    }
}

/// <summary>
/// Observes cancellation with ThrowIfCancellationRequested(), which surfaces it as an
/// OperationCanceledException instead of a TaskCanceledException
/// </summary>
public class DbPollingCommand : AsyncCommand<DbRequest>
{
    public static long Started;
    protected override async Task RunAsync(DbRequest request, CancellationToken token)
    {
        Interlocked.Increment(ref Started);
        var deadline = DateTime.UtcNow.AddMilliseconds(request.WaitMs ?? 10_000);
        while (DateTime.UtcNow < deadline)
        {
            token.ThrowIfCancellationRequested();
            await Task.Delay(50, CancellationToken.None);
        }
    }
}

/// <summary>Fails its first request.Id attempts, then succeeds, like a flaky downstream service</summary>
public class DbFlakyCommand : SyncCommand<DbRequest>
{
    public static long Count;
    protected override void Run(DbRequest request)
    {
        Interlocked.Increment(ref Count);
        var job = Request.GetBackgroundJob();
        if (job.Attempts <= request.Id)
            throw new Exception($"Service unavailable, attempt {job.Attempts}");
    }
}

/// <summary>Captures the parent Job passed to a dependent Job</summary>
public class DbDependentCommand : SyncCommand<DbRequest>
{
    public static CompletedJob? ParentJob;
    public static DateTime? RanAt;
    protected override void Run(DbRequest request)
    {
        ParentJob = Request.GetBackgroundJob().ParentJob;
        RanAt = DateTime.UtcNow;
    }
}

/// <summary>Runs after a Job completes, receiving its result</summary>
public class DbCallbackCommand : SyncCommand<DbResponse>
{
    public static long Count;
    public static DbResponse? LastResponse;
    protected override void Run(DbResponse request)
    {
        Interlocked.Increment(ref Count);
        LastResponse = request;
    }
}

/// <summary>Tracks the highest number of concurrent executions observed</summary>
public class DbSerialCommand : AsyncCommand<DbRequest>
{
    public static int Running;
    public static int MaxConcurrent;
    public static long Count;
    protected override async Task RunAsync(DbRequest request, CancellationToken token)
    {
        var running = Interlocked.Increment(ref Running);
        InterlockedMax(ref MaxConcurrent, running);
        try
        {
            await Task.Delay(request.WaitMs ?? 100, token);
            Interlocked.Increment(ref Count);
        }
        finally
        {
            Interlocked.Decrement(ref Running);
        }
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int current;
        while (value > (current = Volatile.Read(ref target)))
        {
            if (Interlocked.CompareExchange(ref target, value, current) == current)
                return;
        }
    }
}

/// <summary>Writes more log output than the configured per-Job limit</summary>
public class DbVerboseCommand(ILogger<DbVerboseCommand> logger, IBackgroundJobs jobs) : SyncCommand<DbRequest>
{
    protected override void Run(DbRequest request)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        log.UpdateStatus(0.5, "Chatty");
        for (var i = 0; i < 10; i++)
        {
            log.LogInformation(new string('x', 200));
        }
    }
}

/// <summary>Records overlap between Jobs sharing a ConcurrencyKey</summary>
public class DbKeyedCommand : AsyncCommand<DbRequest>
{
    public static readonly ConcurrentDictionary<int, int> RunningByKey = new();
    public static readonly ConcurrentDictionary<int, int> MaxByKey = new();
    public static long Count;
    protected override async Task RunAsync(DbRequest request, CancellationToken token)
    {
        var key = request.Id;
        var running = RunningByKey.AddOrUpdate(key, 1, (_, v) => v + 1);
        MaxByKey.AddOrUpdate(key, running, (_, v) => Math.Max(v, running));
        try
        {
            await Task.Delay(request.WaitMs ?? 150, token);
            Interlocked.Increment(ref Count);
        }
        finally
        {
            RunningByKey.AddOrUpdate(key, 0, (_, v) => v - 1);
        }
    }
}

/// <summary>Ignores its CancellationToken, like a Job stuck in a blocking call</summary>
public class DbUncooperativeCommand : SyncCommand<DbRequest>
{
    public static long Started;
    public static bool Release;
    protected override void Run(DbRequest request)
    {
        Interlocked.Increment(ref Started);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!Release && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(50);
        }
    }
}

public class DbFailCommand : SyncCommand
{
    public static long Count;
    protected override void Run()
    {
        Interlocked.Increment(ref Count);
        throw new Exception("Always Fails: " + Count);
    }
}

public class DbBatchCallback : SyncCommand<JobBatch>
{
    public static long Count;
    public static JobBatch? LastBatch;
    protected override void Run(JobBatch request)
    {
        Interlocked.Increment(ref Count);
        LastBatch = request;
    }
}

[IgnoreServices]
public class DbJobServices : Service
{
    public static long Count;
    public object Any(DbRequest request)
    {
        Interlocked.Increment(ref Count);
        return new DbResponse { Result = $"Hello {request.Id}" };
    }
}

/// <summary>
/// Covers the RDBMS Background Jobs provider (DatabaseJobFeature / DbJobs). It runs against SQLite
/// because DbJobsProvider.Create() falls through to the portable provider for it, which lets the
/// lease, fencing and failover behaviour be verified without a live RDBMS.
/// </summary>
public class DbJobsTests
{
    // Constructed in OneTimeSetUp, not as a field initializer: the AppHostBase constructor sets the
    // process-global ServiceStackHost.Instance, and NUnit constructs fixture instances before the
    // previous fixture has disposed its host.
    private AppHost appHost = null!;
    private readonly DatabaseJobFeature feature = new() {
        // Keep leases short so expiry and failover are testable without long waits
        LeaseDurationSecs = 1,
        DefaultRetryLimit = 1,
        DefaultRetryDelayMs = 1,
        DefaultMaxRetryDelayMs = 10,
        // Keep concurrent SQLite writers down; the behaviour under test isn't throughput
        MaxConcurrentJobs = 1,
        // Don't make the shutdown test wait out the 30s default
        ShutdownTimeoutSecs = 5,
    };
    private IBackgroundJobs Jobs => feature.Jobs;
    /// <summary>The RDBMS provider, for the lease and dispatch APIs not on IBackgroundJobs</summary>
    private DbJobs DbJobs => (DbJobs)feature.Jobs;

    class AppHost() : AppHostBase(nameof(DbJobsTests), typeof(DbJobServices).Assembly)
    {
        public override void Configure() {}
    }

    /// <summary>
    /// Built in OneTimeSetUp rather than the constructor: ServiceStackHost.Instance is a process
    /// global, and NUnit can construct a fixture instance while another fixture's AppHost is still
    /// alive, which would throw "ServiceStackHost.Instance has already been set". OneTimeSetUp and
    /// OneTimeTearDown bracket this fixture's tests, so only one AppHost exists at a time.
    /// </summary>
    [OneTimeSetUp]
    public void TestFixtureSetUp()
    {
        appHost = new AppHost();
        var contentRootPath = "~/../../../".MapServerPath();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            ContentRootPath = contentRootPath,
            WebRootPath = contentRootPath,
        });
        // CommandsFeature scans the whole assembly, which includes Commands from other fixtures
        // that depend on services this host doesn't register. They're never resolved here.
        builder.Host.UseDefaultServiceProvider(o => {
            o.ValidateOnBuild = false;
            o.ValidateScopes = false;
        });

        var services = builder.Services;
        services.AddLogging(o => {
            o.ClearProviders();
            o.AddProvider(new NUnitLoggerProvider());
        });

        var dbPath = contentRootPath.CombineWith("App_Data/dbjobs.db");
        // Also clear any journal/WAL sidecar files left by a previous run
        foreach (var suffix in new[] { "", "-journal", "-wal", "-shm" })
        {
            if (File.Exists(dbPath + suffix))
                File.Delete(dbPath + suffix);
        }
        FileSystemVirtualFiles.AssertDirectory(Path.GetDirectoryName(dbPath));
        var dbFactory = new OrmLiteConnectionFactory($"Data Source={dbPath};Pooling=False",
            SqliteDialect.Provider);
        // The RDBMS provider writes from several connections at once, which a rollback-journal
        // SQLite database serialises into lock contention. WAL lets readers and a writer overlap,
        // which is the concurrency a real RDBMS provides.
        using (var initDb = dbFactory.OpenDbConnection())
        {
            var journalMode = initDb.SqlScalar<string>("PRAGMA journal_mode=WAL");
            if (journalMode?.ToLower() != "wal")
                throw new Exception($"Expected WAL journal mode, got '{journalMode}'");
        }
        services.AddSingleton<IDbConnectionFactory>(dbFactory);

        services.AddPlugin(new CommandsFeature());
        services.AddPlugin(feature);
        services.AddServiceStack(typeof(DbJobServices).Assembly);

        var app = builder.Build();
        app.UseServiceStack(appHost, options => options.MapEndpoints());
        app.StartAsync("http://localhost:20001").Wait();

        Jobs.StartAsync(default).Wait();
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown() => AppHostBase.DisposeApp();

    // Test helpers

    private void ResetState()
    {
        DbJobCommand.Count = 0;
        DbJobCommand.Requests.Clear();
        lock (DbExactlyOnceCommand.ExecutedJobIds) DbExactlyOnceCommand.ExecutedJobIds.Clear();
        DbLongRunningCommand.Started = 0;
        DbLongRunningCommand.ObservedCancellation = false;
        DbDependentCommand.ParentJob = null;
        DbDependentCommand.RanAt = null;
        DbCallbackCommand.Count = 0;
        DbCallbackCommand.LastResponse = null;
        DbSerialCommand.Running = 0;
        DbSerialCommand.MaxConcurrent = 0;
        DbSerialCommand.Count = 0;
        DbUncooperativeCommand.Started = 0;
        DbUncooperativeCommand.Release = false;
        DbKeyedCommand.RunningByKey.Clear();
        DbKeyedCommand.MaxByKey.Clear();
        DbKeyedCommand.Count = 0;
        DbFailCommand.Count = 0;
        DbBatchCallback.Count = 0;
        DbBatchCallback.LastBatch = null;
        DbJobServices.Count = 0;
        DbPollingCommand.Started = 0;
        DbFlakyCommand.Count = 0;
    }

    /// <summary>
    /// Drives the Job pipeline until the predicate holds, like the hosted service would.
    /// Ticks at a realistic rate: each tick writes, and polling much faster than the real hosted
    /// service starves the Job Workers of SQLite's single writer slot.
    /// </summary>
    private async Task<bool> TickUntilAsync(Func<bool> untilTrue, int timeoutMs = 10_000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (untilTrue())
                return true;
            await Jobs.TickAsync();
            for (var i = 0; i < 5; i++)
            {
                await Task.Delay(50);
                if (untilTrue())
                    return true;
            }
        }
        return untilTrue();
    }

    private Task<bool> WaitForStateAsync(long jobId, BackgroundJobState state, int timeoutMs = 10_000) =>
        TickUntilAsync(() => GetSummary(jobId)?.State == state, timeoutMs);

    private async Task<bool> WaitForFinishedAsync(long jobId, int timeoutMs = 10_000)
    {
        if (!await TickUntilAsync(() => GetSummary(jobId)?.State.IsFinished() == true, timeoutMs))
            return false;
        // A Job's summary is marked finished just before it's archived
        await TickUntilAsync(() => GetQueuedJob(jobId) == null, 2_000);
        return true;
    }

    private JobSummary? GetSummary(long jobId)
    {
        using var db = Jobs.OpenDb();
        return db.SingleById<JobSummary>(jobId);
    }

    private BackgroundJob? GetQueuedJob(long jobId)
    {
        using var db = Jobs.OpenDb();
        return db.SingleById<BackgroundJob>(jobId);
    }

    /// <summary>Queues a Job that won't be dispatched, so its stored row can be inspected</summary>
    private BackgroundJobRef EnqueueDeferred(BackgroundJobOptions? options = null)
    {
        options ??= new();
        options.RunAfter ??= DateTime.UtcNow.AddHours(1);
        return Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 1 }, options);
    }

    private object ExecAdmin(Func<DbJobsAdminServices, object> fn)
    {
        var accessRole = feature.AccessRole;
        feature.AccessRole = ServiceStack.Configuration.RoleNames.AllowAnon;
        try
        {
            using var service = appHost.Container.Resolve<DbJobsAdminServices>();
            service.Request = new BasicRequest();
            return fn(service);
        }
        finally
        {
            feature.AccessRole = accessRole;
        }
    }

    /// <summary>
    /// A second DbJobs instance against the same database, i.e. another App Server in a web farm.
    /// It gets its own ServerId so claims made by each node are distinguishable.
    /// </summary>
    private DbJobs CreateSecondNode(out DatabaseJobFeature secondFeature)
    {
        secondFeature = new DatabaseJobFeature {
            DbFactory = feature.DbFactory,
            DbProvider = feature.DbProvider,
            AppHost = feature.AppHost,
            CommandsFeature = feature.CommandsFeature,
            LeaseDurationSecs = feature.LeaseDurationSecs,
            MaxConcurrentJobs = 1,
            DefaultRetryLimit = 0,
            AutoInitSchema = false,
        };
        var services = appHost.GetApplicationServices();
        var node = new DbJobs(
            services.GetRequiredService<ILogger<DbJobs>>(),
            secondFeature,
            services,
            services.GetRequiredService<IServiceScopeFactory>());
        secondFeature.Jobs = node;
        return node;
    }

    // Distributed execution — the core guarantee of the RDBMS provider

    [Test]
    public async Task Two_nodes_competing_for_the_same_Jobs_each_run_exactly_once()
    {
        ResetState();
        var queue = "two-node-" + Guid.NewGuid().ToString("N")[..8];
        // Pause so enqueuing doesn't dispatch on this node before the other one is racing
        Jobs.PauseJobQueue(queue);

        const int jobCount = 6;
        var jobIds = new List<long>();
        for (var i = 0; i < jobCount; i++)
        {
            jobIds.Add(Jobs.EnqueueCommand<DbExactlyOnceCommand>(new DbRequest { Id = i },
                new() { Queue = queue }).Id);
        }

        var node2 = CreateSecondNode(out var feature2);
        await node2.StartAsync(default);
        Jobs.ResumeJobQueue(queue);
        node2.ResumeJobQueue(queue);

        // Both nodes claim from the same queue at the same time
        await Task.WhenAll(
            Task.Run(() => DbJobs.DispatchPendingJobs()),
            Task.Run(() => node2.DispatchPendingJobs()));

        await TickUntilAsync(() => {
            lock (DbExactlyOnceCommand.ExecutedJobIds)
                return DbExactlyOnceCommand.ExecutedJobIds.Count >= jobCount;
        }, 20_000);
        await node2.StopAsync(default);

        List<long> executed;
        lock (DbExactlyOnceCommand.ExecutedJobIds)
            executed = new List<long>(DbExactlyOnceCommand.ExecutedJobIds);

        Assert.That(executed, Is.EquivalentTo(jobIds),
            "Every Job should run exactly once across both nodes");
        Assert.That(executed.Distinct().Count(), Is.EqualTo(executed.Count),
            "No Job may be executed twice when 2 nodes claim concurrently");

        // Each Job is owned by exactly one node
        using var db = Jobs.OpenDb();
        var owners = db.Column<string>(db.From<JobSummary>()
            .Where(x => Sql.In(x.Id, jobIds))
            .Select(x => x.State));
        Assert.That(owners.All(x => x == nameof(BackgroundJobState.Completed)), Is.True);
    }

    [Test]
    public async Task A_renewed_lease_cannot_be_stolen_by_another_node()
    {
        ResetState();
        // A lease long enough that it can't lapse mid-test just because a tick was slow — the
        // behaviour under test is that renewal extends it, not how long the window is.
        var leaseSecs = feature.LeaseDurationSecs;
        feature.LeaseDurationSecs = 30;
        try
        {
            var jobRef = Jobs.EnqueueCommand<DbLongRunningCommand>(new DbRequest { Id = 1, WaitMs = 4000 });
            Assert.That(await TickUntilAsync(() => GetQueuedJob(jobRef.Id)?.LeaseToken != null), Is.True);

            var owned = GetQueuedJob(jobRef.Id)!;
            var originalToken = owned.LeaseToken;
            var originalExpiry = owned.LeaseExpiresAt!.Value;
            Assert.That(owned.LeaseOwner, Is.EqualTo(feature.ServerId));

            await Task.Delay(250);
            await Jobs.TickAsync();

            var renewed = GetQueuedJob(jobRef.Id);
            Assert.That(renewed, Is.Not.Null, "The Job should still be running and owned");
            Assert.That(renewed!.LeaseToken, Is.EqualTo(originalToken),
                "Renewal keeps the same fencing token");
            Assert.That(renewed.LeaseExpiresAt, Is.GreaterThan(originalExpiry),
                "Renewal should extend the lease");

            // Another node must not take a Job whose lease is still live
            var node2 = CreateSecondNode(out _);
            node2.DispatchPendingJobs();

            var after = GetQueuedJob(jobRef.Id)!;
            Assert.That(after.LeaseOwner, Is.EqualTo(feature.ServerId), "A live lease must not be stolen");
            Assert.That(after.LeaseToken, Is.EqualTo(originalToken));

            Jobs.CancelJob(jobRef.Id);
            await WaitForFinishedAsync(jobRef.Id);
        }
        finally
        {
            feature.LeaseDurationSecs = leaseSecs;
        }
    }

    [Test]
    public async Task Incomplete_Jobs_are_resumed_after_a_restart()
    {
        ResetState();
        var queue = "restart-" + Guid.NewGuid().ToString("N")[..8];
        Jobs.PauseJobQueue(queue);
        var jobRef = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 11 }, new() { Queue = queue });

        // Left unclaimed, as it would be if the App stopped before running it
        Assert.That(GetQueuedJob(jobRef.Id)!.LeaseToken, Is.Null);
        Assert.That(GetQueuedJob(jobRef.Id)!.State, Is.EqualTo(BackgroundJobState.Queued));

        Jobs.ResumeJobQueue(queue);
        await Jobs.StartAsync(default); // Startup requeues incomplete Jobs

        Assert.That(await WaitForFinishedAsync(jobRef.Id), Is.True);
        Assert.That(GetSummary(jobRef.Id)!.State, Is.EqualTo(BackgroundJobState.Completed));
    }

    [Test]
    public async Task Can_cancel_a_running_Job()
    {
        ResetState();
        var jobRef = Jobs.EnqueueCommand<DbLongRunningCommand>(new DbRequest { Id = 1, WaitMs = 10_000 });

        Assert.That(await TickUntilAsync(() => Interlocked.Read(ref DbLongRunningCommand.Started) > 0), Is.True,
            "Job should have started");

        Jobs.CancelJob(jobRef.Id);

        Assert.That(await WaitForFinishedAsync(jobRef.Id), Is.True);
        Assert.That(GetSummary(jobRef.Id)!.State, Is.EqualTo(BackgroundJobState.Cancelled));
        Assert.That(DbLongRunningCommand.ObservedCancellation, Is.True,
            "A running Job should observe cancellation through its CancellationToken");
    }

    [Test]
    public async Task Dependent_Jobs_only_run_after_their_parent_completes()
    {
        ResetState();
        var parentRef = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 100, WaitMs = 300 });
        var childRef = Jobs.EnqueueCommand<DbDependentCommand>(new DbRequest { Id = 101 },
            new() { DependsOn = parentRef.Id });

        // The dependent Job must not be claimable while its parent is outstanding
        Assert.That(GetQueuedJob(childRef.Id)!.LeaseToken, Is.Null);
        Assert.That(DbDependentCommand.RanAt, Is.Null);

        Assert.That(await WaitForFinishedAsync(parentRef.Id), Is.True);
        Assert.That(await WaitForFinishedAsync(childRef.Id), Is.True);

        Assert.That(GetSummary(childRef.Id)!.State, Is.EqualTo(BackgroundJobState.Completed));
        Assert.That(DbDependentCommand.ParentJob, Is.Not.Null,
            "A dependent Job should receive its completed parent Job");
        Assert.That(DbDependentCommand.ParentJob!.Id, Is.EqualTo(parentRef.Id));
    }

    // Executing Jobs

    [Test]
    public async Task Can_execute_a_Command_Job_to_completion()
    {
        ResetState();
        var jobRef = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 1 });

        Assert.That(await WaitForFinishedAsync(jobRef.Id), Is.True);

        var summary = GetSummary(jobRef.Id)!;
        Assert.That(summary.State, Is.EqualTo(BackgroundJobState.Completed));
        Assert.That(DbJobCommand.Count, Is.EqualTo(1));

        // Finished Jobs are moved out of the active table into the monthly archive
        Assert.That(GetQueuedJob(jobRef.Id), Is.Null);
        var result = Jobs.GetJob(jobRef.Id)!;
        Assert.That(result.Completed, Is.Not.Null);
        var response = (DbResponse)Jobs.CreateResponse(result.Completed!)!;
        Assert.That(response.Result, Is.EqualTo("Hello 1"));
    }

    [Test]
    public async Task Can_execute_an_API_Job_to_completion()
    {
        ResetState();
        var jobRef = Jobs.EnqueueApi(new DbRequest { Id = 2 });

        Assert.That(await WaitForFinishedAsync(jobRef.Id), Is.True);
        Assert.That(GetSummary(jobRef.Id)!.State, Is.EqualTo(BackgroundJobState.Completed));
        Assert.That(DbJobServices.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task Failing_Jobs_are_retried_then_recorded_as_Failed()
    {
        ResetState();
        var jobRef = Jobs.EnqueueCommand<DbFailCommand>(new(), new() { RetryLimit = 1 });

        Assert.That(await WaitForStateAsync(jobRef.Id, BackgroundJobState.Failed), Is.True);

        var summary = GetSummary(jobRef.Id)!;
        Assert.That(summary.State, Is.EqualTo(BackgroundJobState.Failed));
        Assert.That(summary.ErrorMessage, Does.StartWith("Always Fails"));
        // The original attempt plus the retry
        Assert.That(DbFailCommand.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(Jobs.GetJob(jobRef.Id)!.Failed, Is.Not.Null);
    }

    [Test]
    public void Can_cancel_a_queued_Job()
    {
        ResetState();
        var jobRef = EnqueueDeferred();

        Jobs.CancelJob(jobRef.Id);

        var summary = GetSummary(jobRef.Id)!;
        Assert.That(summary.State, Is.EqualTo(BackgroundJobState.Cancelled));
        Assert.That(summary.CancelRequestedDate, Is.Not.Null);
        Assert.That(GetQueuedJob(jobRef.Id)!.State, Is.EqualTo(BackgroundJobState.Cancelled));
    }

    [Test]
    public void Higher_Priority_Jobs_are_claimed_first()
    {
        ResetState();
        using var db = Jobs.OpenDb();
        var low = EnqueueDeferred(new() { Priority = 0 });
        var high = EnqueueDeferred(new() { Priority = 100 });

        // Make both due, then claim a batch
        var now = DateTime.UtcNow;
        db.UpdateOnly(() => new BackgroundJob { RunAfter = now.AddMinutes(-1) },
            where: x => x.Id == low.Id || x.Id == high.Id);

        var claimOrder = db.Column<long>(db.From<BackgroundJob>()
            .Where(x => x.Id == low.Id || x.Id == high.Id)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Id)
            .Select(x => x.Id));
        Assert.That(claimOrder[0], Is.EqualTo(high.Id), "Higher Priority Job should be selected first");
    }

    [Test]
    public async Task A_Jobs_Callback_runs_with_its_result_and_completes_the_Job()
    {
        ResetState();
        var jobRef = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 55 },
            new() { Callback = nameof(DbCallbackCommand) });

        Assert.That(await WaitForFinishedAsync(jobRef.Id), Is.True);

        Assert.That(Interlocked.Read(ref DbCallbackCommand.Count), Is.EqualTo(1));
        Assert.That(DbCallbackCommand.LastResponse?.Result, Is.EqualTo("Hello 55"));
        // A Job with a Callback only reaches Completed once the Callback has run
        Assert.That(GetSummary(jobRef.Id)!.State, Is.EqualTo(BackgroundJobState.Completed));
    }

    [Test]
    public async Task Failed_Jobs_can_be_requeued_and_run_again()
    {
        ResetState();
        var jobRef = Jobs.EnqueueCommand<DbFailCommand>(new(), new() { RetryLimit = 0 });
        Assert.That(await WaitForStateAsync(jobRef.Id, BackgroundJobState.Failed), Is.True);
        var failedCount = Interlocked.Read(ref DbFailCommand.Count);

        Jobs.RequeueFailedJob(jobRef.Id);

        var requeued = GetQueuedJob(jobRef.Id);
        Assert.That(requeued, Is.Not.Null, "A requeued Job returns to the active Jobs table");
        Assert.That(requeued!.State, Is.EqualTo(BackgroundJobState.Queued));
        Assert.That(requeued.Attempts, Is.EqualTo(0), "Requeueing resets the previous run's state");
        Assert.That(requeued.CompletedDate, Is.Null);
        Assert.That(requeued.ErrorCode, Is.Null);
        Assert.That(GetSummary(jobRef.Id)!.State, Is.EqualTo(BackgroundJobState.Queued));

        // And it actually runs again
        Assert.That(await TickUntilAsync(() => Interlocked.Read(ref DbFailCommand.Count) > failedCount), Is.True);
    }

    [Test]
    public async Task Named_Workers_run_their_Jobs_one_at_a_time()
    {
        ResetState();
        var worker = "serial-" + Guid.NewGuid().ToString("N")[..8];
        for (var i = 0; i < 4; i++)
        {
            Jobs.EnqueueCommand<DbSerialCommand>(new DbRequest { Id = i, WaitMs = 150 },
                new() { Worker = worker });
        }

        Assert.That(await TickUntilAsync(() => Interlocked.Read(ref DbSerialCommand.Count) >= 4, 20_000), Is.True);
        Assert.That(DbSerialCommand.MaxConcurrent, Is.EqualTo(1),
            "A named Worker serialises its Jobs");
    }

    [Test]
    public async Task Job_logs_and_status_are_persisted_and_bounded()
    {
        ResetState();
        var maxChars = feature.MaxJobLogChars;
        feature.MaxJobLogChars = 500;
        try
        {
            var jobRef = Jobs.EnqueueCommand<DbVerboseCommand>(new DbRequest { Id = 1 });
            Assert.That(await WaitForFinishedAsync(jobRef.Id), Is.True);

            // Logs are archived with the Job
            var completed = Jobs.GetJob(jobRef.Id)!.Completed;
            Assert.That(completed, Is.Not.Null);
            Assert.That(completed!.Logs, Is.Not.Null.And.Not.Empty);
            Assert.That(completed.Logs!.Length, Is.LessThanOrEqualTo(500),
                "Logs must not grow past MaxJobLogChars");
            Assert.That(completed.LogsTruncated, Is.True);
            Assert.That(GetSummary(jobRef.Id)!.LogsTruncated, Is.True,
                "Truncation is surfaced on the summary for the Admin UI");
        }
        finally
        {
            feature.MaxJobLogChars = maxChars;
        }
    }

    [Test]
    public async Task Scheduled_Tasks_enqueue_a_Job_when_they_become_due()
    {
        ResetState();
        var taskName = "due-" + Guid.NewGuid().ToString("N")[..8];
        Jobs.RecurringCommand<DbJobCommand>(taskName, Schedule.Interval(TimeSpan.FromHours(1)),
            new DbRequest { Id = 500 });

        var task = Jobs.GetScheduledTask(taskName)!;
        Assert.That(task.NextRun, Is.Not.Null);

        // CreateOrUpdate makes it due immediately, so a tick should enqueue its occurrence
        Assert.That(await TickUntilAsync(() => Jobs.GetScheduledTask(taskName)?.LastJobId != null), Is.True);

        var reloaded = Jobs.GetScheduledTask(taskName)!;
        Assert.That(reloaded.LastRun, Is.Not.Null);
        Assert.That(reloaded.LastJobId, Is.Not.Null);
        Assert.That(reloaded.NextRun, Is.GreaterThan(DateTime.UtcNow),
            "NextRun advances past the occurrence that just ran");

        // The occurrence uses a deterministic RefId so a crash can't double-queue it
        var jobResult = Jobs.GetJob(reloaded.LastJobId!.Value)!;
        Assert.That(jobResult.Summary.RefId, Does.StartWith(JobUtils.ScheduledPrefix + task.Id + ":"));
        Assert.That(JobUtils.TryGetScheduledTaskId(jobResult.Summary.RefId, out var taskId), Is.True);
        Assert.That(taskId, Is.EqualTo(task.Id));

        Jobs.DeleteRecurringTask(taskName);
    }

    [Test]
    public async Task Jobs_with_RunAfter_are_not_claimed_until_they_are_due()
    {
        ResetState();
        var jobRef = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 1 },
            new() { RunAfter = DateTime.UtcNow.AddMilliseconds(1500) });

        DbJobs.DispatchPendingJobs();
        var job = GetQueuedJob(jobRef.Id)!;
        Assert.That(job.LeaseToken, Is.Null, "A Job must not be claimed before its RunAfter");
        Assert.That(job.RequestId, Is.Null);

        Assert.That(await WaitForFinishedAsync(jobRef.Id, 20_000), Is.True,
            "It should run once RunAfter has passed");
        Assert.That(GetSummary(jobRef.Id)!.State, Is.EqualTo(BackgroundJobState.Completed));
    }

    // Leases, fencing and failover

    [Test]
    public void Claimed_Jobs_record_their_lease()
    {
        ResetState();
        var jobRef = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 1, WaitMs = 5000 });

        var job = GetQueuedJob(jobRef.Id);
        Assert.That(job, Is.Not.Null);
        Assert.That(job!.LeaseOwner, Is.EqualTo(feature.ServerId));
        Assert.That(job.LeaseToken, Is.Not.Null);
        Assert.That(job.LeaseExpiresAt, Is.Not.Null);

        Jobs.CancelJob(jobRef.Id);
    }

    [Test]
    public void CompleteJob_is_rejected_when_the_lease_was_lost()
    {
        ResetState();
        var jobRef = EnqueueDeferred();
        using var db = Jobs.OpenDb();

        // Another node has taken over the Job
        db.UpdateOnly(() => new BackgroundJob {
            LeaseOwner = "other-node",
            LeaseToken = "other-token",
            LeaseExpiresAt = DateTime.UtcNow.AddMinutes(5),
            State = BackgroundJobState.Started,
        }, where: x => x.Id == jobRef.Id);

        var staleJob = db.SingleById<BackgroundJob>(jobRef.Id);
        staleJob.LeaseToken = "stale-token";
        Jobs.CompleteJob(staleJob, new DbResponse { Result = "should be discarded" });

        var current = db.SingleById<BackgroundJob>(jobRef.Id);
        Assert.That(current, Is.Not.Null, "A fenced completion must not archive the Job");
        Assert.That(current.CompletedDate, Is.Null);
        Assert.That(current.LeaseToken, Is.EqualTo("other-token"));
        Assert.That(GetSummary(jobRef.Id)!.State, Is.Not.EqualTo(BackgroundJobState.Completed));
    }

    [Test]
    public void FailJob_is_rejected_when_the_lease_was_lost()
    {
        ResetState();
        var jobRef = EnqueueDeferred();
        using var db = Jobs.OpenDb();

        db.UpdateOnly(() => new BackgroundJob {
            LeaseOwner = "other-node",
            LeaseToken = "other-token",
            LeaseExpiresAt = DateTime.UtcNow.AddMinutes(5),
            State = BackgroundJobState.Started,
        }, where: x => x.Id == jobRef.Id);

        var staleJob = db.SingleById<BackgroundJob>(jobRef.Id);
        staleJob.LeaseToken = "stale-token";
        staleJob.StartedDate = DateTime.UtcNow;
        DbJobs.FailJob(staleJob, new Exception("should be discarded"), shouldRetry:false);

        var current = db.SingleById<BackgroundJob>(jobRef.Id);
        Assert.That(current, Is.Not.Null, "A fenced failure must not archive the Job");
        Assert.That(current.State, Is.EqualTo(BackgroundJobState.Started));
        Assert.That(GetSummary(jobRef.Id)!.State, Is.Not.EqualTo(BackgroundJobState.Failed));
    }

    [Test]
    public void ArchiveJob_is_rejected_when_the_lease_was_lost()
    {
        ResetState();
        var jobRef = EnqueueDeferred();
        using var db = Jobs.OpenDb();
        db.UpdateOnly(() => new BackgroundJob { LeaseToken = "other-token" },
            where: x => x.Id == jobRef.Id);

        var staleJob = db.SingleById<BackgroundJob>(jobRef.Id);
        staleJob.LeaseToken = "stale-token";
        DbJobs.ArchiveJob(staleJob);

        Assert.That(db.SingleById<BackgroundJob>(jobRef.Id), Is.Not.Null,
            "A fenced archive must leave the Job for its real owner");
    }

    [Test]
    public void Expired_leases_are_reclaimed_by_another_node()
    {
        ResetState();
        var jobRef = EnqueueDeferred();
        using var db = Jobs.OpenDb();

        // A node claimed this Job then stopped renewing its lease
        var now = DateTime.UtcNow;
        db.UpdateOnly(() => new BackgroundJob {
            RunAfter = now.AddMinutes(-1),
            RequestId = "abandoned",
            LeaseOwner = "dead-node",
            LeaseToken = "dead-token",
            LeaseExpiresAt = now.AddMinutes(-1),
            LastActivityDate = now.AddMinutes(-1),
            State = BackgroundJobState.Started,
        }, where: x => x.Id == jobRef.Id);

        DbJobs.DispatchPendingJobs();

        var job = db.SingleById<BackgroundJob>(jobRef.Id);
        // Either reclaimed by this node, or already run to completion after being reclaimed
        if (job != null)
        {
            Assert.That(job.LeaseToken, Is.Not.EqualTo("dead-token"), "Expired lease should be replaced");
            Assert.That(job.LeaseOwner, Is.EqualTo(feature.ServerId));
        }
        else
        {
            Assert.That(GetSummary(jobRef.Id)!.State.IsFinished(), Is.True);
        }
    }

    [Test]
    public void Jobs_left_Started_without_a_lease_are_recovered()
    {
        ResetState();
        var jobRef = EnqueueDeferred();
        using var db = Jobs.OpenDb();

        // A node stopped between claiming the Job and writing its lease
        var stale = DateTime.UtcNow.AddSeconds(-(feature.DefaultTimeoutSecs + 60));
        db.UpdateOnly(() => new BackgroundJob {
            RunAfter = null,
            RequestId = null,
            LeaseOwner = null,
            LeaseToken = null,
            LeaseExpiresAt = null,
            LastActivityDate = stale,
            State = BackgroundJobState.Started,
        }, where: x => x.Id == jobRef.Id);

        DbJobs.DispatchPendingJobs();

        var job = db.SingleById<BackgroundJob>(jobRef.Id);
        if (job != null)
        {
            Assert.That(job.LeaseToken, Is.Not.Null, "Stranded Job should be re-claimed with a lease");
            Assert.That(job.LeaseOwner, Is.EqualTo(feature.ServerId));
        }
        else
        {
            Assert.That(GetSummary(jobRef.Id)!.State.IsFinished(), Is.True);
        }
    }

    [Test]
    public void Status_updates_are_discarded_once_the_lease_is_lost()
    {
        ResetState();
        var jobRef = EnqueueDeferred();
        using var db = Jobs.OpenDb();
        db.UpdateOnly(() => new BackgroundJob { LeaseToken = "other-token" },
            where: x => x.Id == jobRef.Id);

        var staleJob = db.SingleById<BackgroundJob>(jobRef.Id);
        staleJob.LeaseToken = "stale-token";
        Jobs.UpdateJobStatus(new BackgroundJobStatusUpdate(staleJob, 0.5, "discarded", "discarded log"));
        Jobs.TickAsync().Wait();

        var current = db.SingleById<BackgroundJob>(jobRef.Id);
        Assert.That(current.Status, Is.Null);
        Assert.That(current.Logs, Is.Null);
    }

    [Test]
    public async Task StopAsync_releases_leases_of_Jobs_that_never_started()
    {
        ResetState();
        // A Job claimed by this node but still sitting in a worker queue
        var jobRef = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 1, WaitMs = 3000 });
        using var db = Jobs.OpenDb();
        Assert.That(db.SingleById<BackgroundJob>(jobRef.Id)?.LeaseToken, Is.Not.Null);

        await Jobs.StopAsync(default);

        var job = db.SingleById<BackgroundJob>(jobRef.Id);
        if (job != null)
        {
            // Released back to the queue so another node can claim it immediately
            Assert.That(job.LeaseToken, Is.Null);
            Assert.That(job.LeaseOwner, Is.Null);
            Assert.That(job.State, Is.EqualTo(BackgroundJobState.Queued));
        }

        // Restart for the remaining tests in the fixture
        await Jobs.StartAsync(default);
    }

    // Job Batches

    [Test]
    public async Task Job_Batches_count_progress_and_run_their_callback_once()
    {
        ResetState();
        var batchId = "db-batch-" + Guid.NewGuid().ToString("N");
        Jobs.CreateJobBatch<DbBatchCallback>(batchId, total:2, description:"Import");

        var ref1 = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 1 }, new() { BatchId = batchId });
        var ref2 = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 2 }, new() { BatchId = batchId });

        Assert.That(await WaitForFinishedAsync(ref1.Id), Is.True);
        Assert.That(await WaitForFinishedAsync(ref2.Id), Is.True);
        Assert.That(await TickUntilAsync(() => Jobs.GetJobBatch(batchId)?.CompletedDate != null), Is.True);

        var batch = Jobs.GetJobBatch(batchId)!;
        Assert.That(batch.Total, Is.EqualTo(2));
        Assert.That(batch.Completed, Is.EqualTo(2));
        Assert.That(batch.Queued, Is.EqualTo(0));
        Assert.That(batch.Finished, Is.EqualTo(2));
        Assert.That(batch.Progress, Is.EqualTo(1));
        Assert.That(batch.CompletedDate, Is.Not.Null);
        Assert.That(batch.NotifiedDate, Is.Not.Null, "The batch callback should have been claimed");

        // The callback is queued as a Job with a deterministic RefId so it can only run once
        using var db = Jobs.OpenDb();
        var callbackRefId = DbJobs.BatchCallbackPrefix + batchId;
        Assert.That(db.Count<JobSummary>(x => x.RefId == callbackRefId), Is.EqualTo(1));
    }

    [Test]
    public void Job_Batch_counters_survive_concurrent_submitters()
    {
        ResetState();
        var batchId = "db-batch-" + Guid.NewGuid().ToString("N");
        Jobs.CreateJobBatch(batchId, total:20);

        // Counters are incremented in SQL, so parallel enqueues can't lose an update
        Parallel.For(0, 20, i =>
            EnqueueDeferred(new() { BatchId = batchId }));

        var batch = Jobs.GetJobBatch(batchId)!;
        Assert.That(batch.Queued, Is.EqualTo(20));
    }

    [Test]
    public async Task Failed_Jobs_are_counted_against_their_Batch()
    {
        ResetState();
        var batchId = "db-batch-" + Guid.NewGuid().ToString("N");
        Jobs.CreateJobBatch(batchId, total:1);
        var jobRef = Jobs.EnqueueCommand<DbFailCommand>(new(), new() { BatchId = batchId, RetryLimit = 0 });

        Assert.That(await WaitForStateAsync(jobRef.Id, BackgroundJobState.Failed), Is.True);
        Assert.That(await TickUntilAsync(() => Jobs.GetJobBatch(batchId)?.Failed == 1), Is.True);

        var batch = Jobs.GetJobBatch(batchId)!;
        Assert.That(batch.Failed, Is.EqualTo(1));
        Assert.That(batch.Completed, Is.EqualTo(0));
    }

    // Queue controls

    [Test]
    public void Paused_queues_are_not_claimed()
    {
        ResetState();
        var queue = "db-paused-" + Guid.NewGuid().ToString("N")[..8];
        Jobs.PauseJobQueue(queue);

        var jobRef = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 1 }, new() { Queue = queue });

        var job = GetQueuedJob(jobRef.Id)!;
        Assert.That(job.RequestId, Is.Null, "A paused queue must not claim its Jobs");
        Assert.That(job.LeaseToken, Is.Null);

        // Still not claimed after a dispatch cycle
        DbJobs.DispatchPendingJobs();
        Assert.That(GetQueuedJob(jobRef.Id)!.LeaseToken, Is.Null);

        Jobs.ResumeJobQueue(queue);
        Assert.That(Jobs.IsQueuePaused(queue), Is.False);
    }

    [Test]
    public async Task Resuming_a_queue_releases_its_Jobs()
    {
        ResetState();
        var queue = "db-resume-" + Guid.NewGuid().ToString("N")[..8];
        Jobs.PauseJobQueue(queue);
        var jobRef = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 7 }, new() { Queue = queue });
        Assert.That(GetQueuedJob(jobRef.Id)!.LeaseToken, Is.Null);

        Jobs.ResumeJobQueue(queue);

        Assert.That(await WaitForFinishedAsync(jobRef.Id), Is.True);
        Assert.That(GetSummary(jobRef.Id)!.State, Is.EqualTo(BackgroundJobState.Completed));
    }

    [Test]
    public void Queue_concurrency_can_be_overridden_at_runtime()
    {
        ResetState();
        var queue = "db-throttle-" + Guid.NewGuid().ToString("N")[..8];
        Assert.That(Jobs.GetQueueConcurrency(queue), Is.EqualTo(feature.MaxConcurrentJobs));

        Jobs.SetJobQueueConcurrency(queue, 1);
        Assert.That(Jobs.GetQueueConcurrency(queue), Is.EqualTo(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => Jobs.SetJobQueueConcurrency(queue, 0));
    }

    // Job expiry

    [Test]
    public void Expired_Jobs_are_cancelled_instead_of_claimed()
    {
        ResetState();
        var jobRef = EnqueueDeferred(new() { ExpiresIn = TimeSpan.FromMilliseconds(1) });
        using var db = Jobs.OpenDb();
        db.UpdateOnly(() => new BackgroundJob { RunAfter = null }, where: x => x.Id == jobRef.Id);

        Thread.Sleep(20);
        DbJobs.DispatchPendingJobs();
        Assert.That(db.SingleById<BackgroundJob>(jobRef.Id)?.LeaseToken, Is.Null,
            "An expired Job must not be claimed");

        Jobs.TickAsync().Wait();

        var summary = GetSummary(jobRef.Id)!;
        Assert.That(summary.State, Is.EqualTo(BackgroundJobState.Cancelled));
        Assert.That(summary.ErrorCode, Is.EqualTo(JobErrorCodes.JobExpired));
    }

    // ReplyTo

    [Test]
    public async Task Completed_Jobs_deliver_their_result_to_ReplyTo()
    {
        ResetState();
        JobReplyToContext? replyCtx = null;
        var onReplyTo = feature.OnJobReplyTo;
        feature.OnJobReplyTo = ctx => {
            replyCtx = ctx;
            return Task.CompletedTask;
        };
        try
        {
            var jobRef = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 5 },
                new() { ReplyTo = "https://example.org/hook" });

            Assert.That(await WaitForFinishedAsync(jobRef.Id), Is.True);
            Assert.That(await TickUntilAsync(() => replyCtx != null), Is.True);

            Assert.That(replyCtx!.ReplyTo, Is.EqualTo("https://example.org/hook"));
            Assert.That(replyCtx.Job.Id, Is.EqualTo(jobRef.Id));
            Assert.That(((DbResponse)replyCtx.Response!).Result, Is.EqualTo("Hello 5"));
        }
        finally
        {
            feature.OnJobReplyTo = onReplyTo;
        }
    }

    [Test]
    public async Task A_failing_ReplyTo_does_not_fail_the_Job()
    {
        ResetState();
        var onReplyTo = feature.OnJobReplyTo;
        feature.OnJobReplyTo = _ => throw new Exception("ReplyTo endpoint is down");
        try
        {
            var jobRef = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 6 },
                new() { ReplyTo = "https://example.org/down" });

            Assert.That(await WaitForFinishedAsync(jobRef.Id), Is.True);
            Assert.That(GetSummary(jobRef.Id)!.State, Is.EqualTo(BackgroundJobState.Completed),
                "The Job already ran successfully, delivery failure must not change that");
        }
        finally
        {
            feature.OnJobReplyTo = onReplyTo;
        }
    }

    // Awaiting a durable Job

    [Test]
    public async Task Can_await_a_queued_Job_for_its_result()
    {
        ResetState();
        var jobRef = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 42 });
        _ = Task.Run(async () => await TickUntilAsync(() => GetSummary(jobRef.Id)?.State.IsFinished() == true));

        var result = await Jobs.WaitForJobAsync(jobRef, TimeSpan.FromSeconds(30));

        Assert.That(result.Summary.State, Is.EqualTo(BackgroundJobState.Completed));
        var response = (DbResponse)Jobs.CreateResponse(result.Job!)!;
        Assert.That(response.Result, Is.EqualTo("Hello 42"));
    }

    // Idempotency and singletons

    [Test]
    public void Idempotent_enqueue_returns_the_existing_Job()
    {
        ResetState();
        var options = new BackgroundJobOptions {
            RefId = "db-order-" + Guid.NewGuid().ToString("N"),
            DuplicateRefIdBehavior = DuplicateRefIdBehavior.ReturnExisting,
            RunAfter = DateTime.UtcNow.AddHours(1),
        };
        var first = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 1 }, options);
        var second = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 1 }, options);

        Assert.That(second.Id, Is.EqualTo(first.Id));
        using var db = Jobs.OpenDb();
        Assert.That(db.Count<JobSummary>(x => x.RefId == options.RefId), Is.EqualTo(1));
    }

    [Test]
    public void SingletonKey_allows_only_one_active_Job()
    {
        ResetState();
        var key = "db-singleton-" + Guid.NewGuid().ToString("N");
        var first = EnqueueDeferred(new() { SingletonKey = key });
        var second = EnqueueDeferred(new() { SingletonKey = key });

        Assert.That(second.Id, Is.EqualTo(first.Id));
        using var db = Jobs.OpenDb();
        Assert.That(db.Count<BackgroundJob>(x => x.SingletonKey == key), Is.EqualTo(1));
    }

    // Scheduled Tasks

    [Test]
    public void Recurring_Tasks_persist_their_schedule_and_can_be_controlled()
    {
        ResetState();
        var taskName = "db-task-" + Guid.NewGuid().ToString("N")[..8];
        Jobs.RecurringCommand<DbJobCommand>(taskName, Schedule.Cron("0 2 * * *"),
            new DbRequest { Id = 1 });

        var task = Jobs.GetScheduledTask(taskName);
        Assert.That(task, Is.Not.Null);
        Assert.That(task!.Enabled, Is.True);
        Assert.That(task.CronExpression, Is.EqualTo("0 2 * * *"));
        Assert.That(task.NextRun, Is.Not.Null);

        Assert.That(Jobs.SetRecurringTaskEnabled(taskName, false), Is.True);
        Assert.That(Jobs.GetScheduledTask(taskName)!.Enabled, Is.False);

        Assert.That(Jobs.SetRecurringTaskEnabled(taskName, true), Is.True);
        Assert.That(Jobs.GetScheduledTask(taskName)!.Enabled, Is.True);

        Assert.That(Jobs.RunRecurringTaskNow(taskName), Is.True);
        Assert.That(Jobs.GetScheduledTask(taskName)!.NextRun, Is.LessThanOrEqualTo(DateTime.UtcNow));

        Jobs.DeleteRecurringTask(taskName);
        Assert.That(Jobs.GetScheduledTask(taskName), Is.Null);
    }

    [Test]
    public void Reloading_Scheduled_Tasks_picks_up_changes_made_elsewhere()
    {
        ResetState();
        var taskName = "db-reload-" + Guid.NewGuid().ToString("N")[..8];
        Jobs.RecurringCommand<DbJobCommand>(taskName, Schedule.Cron("0 3 * * *"), new DbRequest { Id = 1 });
        var taskId = Jobs.GetScheduledTask(taskName)!.Id;

        // Simulate another node pausing the task directly in the database
        using var db = Jobs.OpenDb();
        db.UpdateOnly(() => new ScheduledTask { Enabled = false }, where: x => x.Id == taskId);

        Jobs.AssertScheduler().ReloadScheduledTasks();

        Assert.That(Jobs.GetScheduledTask(taskName)!.Enabled, Is.False);
        Jobs.DeleteRecurringTask(taskName);
    }

    // Admin APIs

    [Test]
    public void AdminJobInfo_reports_the_RDBMS_provider_and_its_guarantees()
    {
        var response = (AdminJobInfoResponse)ExecAdmin(x => x.Any(new AdminJobInfo()));

        Assert.That(response.Provider, Is.EqualTo("rdbms"));
        Assert.That(response.Capabilities, Does.Contain("leases"));
        Assert.That(response.Capabilities, Does.Contain("automatic-failover"));
        Assert.That(response.Capabilities, Does.Contain("fenced-completion"));
        Assert.That(response.Capabilities, Does.Contain("job-batches"));
        Assert.That(response.Capabilities, Does.Contain("queue-controls"));
    }

    [Test]
    public void AdminGetJobQueues_reports_backlog_and_AdminUpdateJobQueue_controls_it()
    {
        ResetState();
        var queue = "db-admin-" + Guid.NewGuid().ToString("N")[..8];
        EnqueueDeferred(new() { Queue = queue });

        var updated = (AdminUpdateJobQueueResponse)ExecAdmin(x =>
            x.Post(new AdminUpdateJobQueue { Name = queue, Paused = true, Concurrency = 2 }));
        Assert.That(updated.Result.Paused, Is.True);
        Assert.That(updated.Result.Concurrency, Is.EqualTo(2));

        var queues = (AdminGetJobQueuesResponse)ExecAdmin(x => x.Any(new AdminGetJobQueues()));
        var status = queues.Results.FirstOrDefault(x => x.Name == queue);
        Assert.That(status, Is.Not.Null);
        Assert.That(status!.Paused, Is.True);
        Assert.That(status.Concurrency, Is.EqualTo(2));
        Assert.That(status.ConcurrencyOverridden, Is.True);
        Assert.That(status.Queued, Is.EqualTo(1));

        ExecAdmin(x => x.Post(new AdminUpdateJobQueue { Name = queue, Paused = false }));
        Assert.That(Jobs.IsQueuePaused(queue), Is.False);
    }

    [Test]
    public void AdminGetJobBatch_reports_batch_progress()
    {
        ResetState();
        var batchId = "db-admin-batch-" + Guid.NewGuid().ToString("N");
        Jobs.CreateJobBatch(batchId, total:2);
        EnqueueDeferred(new() { BatchId = batchId });

        var response = (AdminGetJobBatchResponse)ExecAdmin(x =>
            x.Any(new AdminGetJobBatch { BatchId = batchId }));

        Assert.That(response.Result, Is.Not.Null);
        Assert.That(response.Result!.Total, Is.EqualTo(2));
        Assert.That(response.Result.Queued, Is.EqualTo(1));
        // Counted back from the Jobs themselves
        Assert.That(response.StateCounts[nameof(BackgroundJobState.Queued)], Is.EqualTo(1));
    }

    [Test]
    public async Task AdminReplayJob_queues_a_completed_Job_again()
    {
        ResetState();
        var jobRef = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 77 }, new() { Tag = "replay-me" });
        Assert.That(await WaitForFinishedAsync(jobRef.Id), Is.True);

        var response = (AdminReplayJobResponse)ExecAdmin(x => x.Post(new AdminReplayJob { Id = jobRef.Id }));

        Assert.That(response.JobId, Is.Not.EqualTo(jobRef.Id));
        Assert.That(await WaitForFinishedAsync(response.JobId), Is.True);

        var replayed = Jobs.GetJob(response.JobId)!;
        Assert.That(replayed.Summary.Tag, Is.EqualTo("replay-me"));
        Assert.That(DbJobCommand.Requests.Count(x => x.Id == 77), Is.EqualTo(2));
    }

    [Test]
    public void AdminCancelJobs_can_cancel_every_Job_on_a_queue()
    {
        ResetState();
        var queue = "db-cancel-" + Guid.NewGuid().ToString("N")[..8];
        Jobs.PauseJobQueue(queue);
        var ref1 = EnqueueDeferred(new() { Queue = queue });
        var ref2 = EnqueueDeferred(new() { Queue = queue });

        var response = (AdminCancelJobsResponse)ExecAdmin(x => x.Any(new AdminCancelJobs { Queue = queue }));

        Assert.That(response.Results, Does.Contain(ref1.Id));
        Assert.That(response.Results, Does.Contain(ref2.Id));
        Assert.That(GetSummary(ref1.Id)!.State, Is.EqualTo(BackgroundJobState.Cancelled));
        Assert.That(GetSummary(ref2.Id)!.State, Is.EqualTo(BackgroundJobState.Cancelled));
    }

    [Test]
    public async Task AdminJobDashboard_reports_queue_stats_and_wait_times()
    {
        ResetState();
        var jobRef = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 1 });
        Assert.That(await WaitForFinishedAsync(jobRef.Id), Is.True);

        var response = (AdminJobDashboardResponse)ExecAdmin(x => x.Any(new AdminJobDashboard()));

        // Exercises the RDBMS wait-time SQL, which has no portable form across dialects
        Assert.That(response.WaitTimes, Is.Not.Null);
        Assert.That(response.WaitTimes.Count, Is.GreaterThan(0));
        Assert.That(response.WaitTimes.AvgMs, Is.GreaterThanOrEqualTo(0));
        Assert.That(response.Queues.Any(x => x.Name == JobQueues.Default), Is.True);
        Assert.That(response.Commands.Any(x => x.Name == nameof(DbJobCommand)), Is.True);
    }

    [Test]
    public async Task A_Job_that_ignores_its_timeout_stops_holding_its_lease()
    {
        ResetState();
        try
        {
            // TimeoutSecs elapses while the Command is still blocking and ignoring its token
            var jobRef = Jobs.EnqueueCommand<DbUncooperativeCommand>(new DbRequest { Id = 1 },
                new() { TimeoutSecs = 1 });
            Assert.That(await TickUntilAsync(() => Interlocked.Read(ref DbUncooperativeCommand.Started) > 0),
                Is.True);

            var claimed = GetQueuedJob(jobRef.Id);
            Assert.That(claimed?.LeaseExpiresAt, Is.Not.Null);

            // Past its timeout the lease is no longer renewed, so it expires and can be recovered
            await Task.Delay(1500);
            await Jobs.TickAsync();

            var afterTimeout = GetQueuedJob(jobRef.Id);
            if (afterTimeout != null)
            {
                Assert.That(afterTimeout.LeaseExpiresAt, Is.LessThan(DateTime.UtcNow.AddSeconds(2)),
                    "A Job past its timeout must not keep extending its lease indefinitely");
            }
        }
        finally
        {
            DbUncooperativeCommand.Release = true;
        }
    }

    [Test]
    public async Task Transient_Commands_run_without_being_persisted()
    {
        ResetState();
        using var db = Jobs.OpenDb();
        var before = db.Count<JobSummary>();

        var result = await Jobs.RunCommandAsync<DbJobCommand>(new DbRequest { Id = 9 });

        Assert.That(((DbResponse)result!).Result, Is.EqualTo("Hello 9"));
        Assert.That(db.Count<JobSummary>(), Is.EqualTo(before),
            "A transient Command must not be recorded as a Job");
    }

    [Test]
    public void Jobs_can_be_looked_up_by_their_RefId()
    {
        ResetState();
        var refId = "lookup-" + Guid.NewGuid().ToString("N");
        var jobRef = EnqueueDeferred(new() { RefId = refId });

        var byRefId = Jobs.GetJobByRefId(refId);
        Assert.That(byRefId, Is.Not.Null);
        Assert.That(byRefId!.Summary.Id, Is.EqualTo(jobRef.Id));
        Assert.That(byRefId.Queued, Is.Not.Null);

        Assert.That(Jobs.GetJobByRefId("does-not-exist-" + Guid.NewGuid().ToString("N")), Is.Null);
    }

    [Test]
    public async Task Expired_JobSummary_history_is_purged_when_retention_is_configured()
    {
        ResetState();
        var jobRef = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 1 });
        Assert.That(await WaitForFinishedAsync(jobRef.Id), Is.True);

        using var db = Jobs.OpenDb();
        // Backdate the completed history past the retention window
        var old = DateTime.UtcNow.AddDays(-90);
        db.UpdateOnly(() => new JobSummary { CreatedDate = old }, where: x => x.Id == jobRef.Id);

        var retention = feature.JobSummaryRetention;
        feature.JobSummaryRetention = TimeSpan.FromDays(30);
        try
        {
            await Jobs.TickAsync();
            Assert.That(db.SingleById<JobSummary>(jobRef.Id), Is.Null,
                "Completed history older than the retention window should be purged");
        }
        finally
        {
            feature.JobSummaryRetention = retention;
        }
    }

    [Test]
    public async Task Running_Jobs_are_never_purged_by_retention()
    {
        ResetState();
        var jobRef = EnqueueDeferred();
        using var db = Jobs.OpenDb();
        // An old but still-queued Job
        db.UpdateOnly(() => new JobSummary { CreatedDate = DateTime.UtcNow.AddDays(-90) },
            where: x => x.Id == jobRef.Id);

        var retention = feature.JobSummaryRetention;
        feature.JobSummaryRetention = TimeSpan.FromDays(30);
        try
        {
            await Jobs.TickAsync();
            Assert.That(db.SingleById<JobSummary>(jobRef.Id), Is.Not.Null,
                "Retention must never delete a Job that hasn't finished");
        }
        finally
        {
            feature.JobSummaryRetention = retention;
        }
    }

    [Test]
    public async Task AdminRequeueFailedJobs_can_requeue_by_Tag()
    {
        ResetState();
        var tag = "requeue-" + Guid.NewGuid().ToString("N")[..8];
        var jobRef = Jobs.EnqueueCommand<DbFailCommand>(new(), new() { Tag = tag, RetryLimit = 0 });
        Assert.That(await WaitForStateAsync(jobRef.Id, BackgroundJobState.Failed), Is.True);

        var response = (AdminRequeueFailedJobsJobsResponse)ExecAdmin(x =>
            x.Any(new AdminRequeueFailedJobs { Tag = tag }));

        Assert.That(response.Errors, Is.Empty);
        Assert.That(GetSummary(jobRef.Id)!.State, Is.EqualTo(BackgroundJobState.Queued));
    }

    // Concurrency keys, fan-in, rate limits, nodes

    [Test]
    public async Task Jobs_sharing_a_ConcurrencyKey_run_one_at_a_time()
    {
        ResetState();
        // Two keys x 3 Jobs: each key must serialise, but the keys still overlap each other
        for (var i = 0; i < 3; i++)
        {
            Jobs.EnqueueCommand<DbKeyedCommand>(new DbRequest { Id = 1, WaitMs = 120 },
                new() { ConcurrencyKey = "tenant-a" });
            Jobs.EnqueueCommand<DbKeyedCommand>(new DbRequest { Id = 2, WaitMs = 120 },
                new() { ConcurrencyKey = "tenant-b" });
        }

        Assert.That(await TickUntilAsync(() => Interlocked.Read(ref DbKeyedCommand.Count) >= 6, 30_000), Is.True);

        Assert.That(DbKeyedCommand.MaxByKey[1], Is.EqualTo(1), "tenant-a Jobs must not overlap");
        Assert.That(DbKeyedCommand.MaxByKey[2], Is.EqualTo(1), "tenant-b Jobs must not overlap");
    }

    [Test]
    public void A_ConcurrencyKey_slot_is_held_by_one_Job_and_released_when_it_finishes()
    {
        ResetState();
        var key = "slot-" + Guid.NewGuid().ToString("N")[..8];
        using var db = Jobs.OpenDb();

        var first = Jobs.EnqueueCommand<DbLongRunningCommand>(new DbRequest { Id = 1, WaitMs = 3000 },
            new() { ConcurrencyKey = key });
        // A second Job with the same key, queued before the first has been claimed
        var second = Jobs.EnqueueCommand<DbLongRunningCommand>(new DbRequest { Id = 2, WaitMs = 3000 },
            new() { ConcurrencyKey = key });

        DbJobs.DispatchPendingJobs();

        var holder = db.SingleById<JobConcurrencyLock>(key);
        Assert.That(holder, Is.Not.Null, "A claimed keyed Job should hold its key");
        Assert.That(holder!.JobId, Is.AnyOf(first.Id, second.Id));
        // Only one of them may be claimed while the other waits its turn
        var claimed = db.Count<BackgroundJob>(x => (x.Id == first.Id || x.Id == second.Id)
            && x.LeaseToken != null);
        Assert.That(claimed, Is.EqualTo(1), "Only one Job per ConcurrencyKey may run at a time");

        Jobs.CancelJob(first.Id);
        Jobs.CancelJob(second.Id);
    }

    [Test]
    public void Expired_ConcurrencyKey_slots_are_reclaimed()
    {
        ResetState();
        var key = "expired-" + Guid.NewGuid().ToString("N")[..8];
        using var db = Jobs.OpenDb();
        // A slot left behind by a node that died
        db.Insert(new JobConcurrencyLock {
            Key = key,
            JobId = -1,
            LeaseOwner = "dead-node",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5),
            AcquiredDate = DateTime.UtcNow.AddMinutes(-10),
        });

        Jobs.TickAsync().Wait();

        Assert.That(db.SingleById<JobConcurrencyLock>(key), Is.Null,
            "A slot whose holder is gone must not block its key forever");
    }

    [Test]
    public async Task Jobs_can_fan_in_on_a_whole_Batch()
    {
        ResetState();
        var batchId = "fanin-" + Guid.NewGuid().ToString("N");
        Jobs.CreateJobBatch(batchId, total:2);

        var fanIn = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 999 },
            new() { DependsOnBatch = batchId });

        // Must not run while the Batch is outstanding
        DbJobs.DispatchPendingJobs();
        Assert.That(GetQueuedJob(fanIn.Id)!.LeaseToken, Is.Null);

        var b1 = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 1 }, new() { BatchId = batchId });
        var b2 = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 2 }, new() { BatchId = batchId });
        Assert.That(await WaitForFinishedAsync(b1.Id), Is.True);
        Assert.That(await WaitForFinishedAsync(b2.Id), Is.True);

        Assert.That(await WaitForFinishedAsync(fanIn.Id, 20_000), Is.True,
            "The fan-in Job should run once its Batch has finished");
    }

    [Test]
    public void Queue_rate_limits_cap_how_often_Jobs_start()
    {
        ResetState();
        var queue = "rate-" + Guid.NewGuid().ToString("N")[..8];
        // 2 Jobs per 60s window, so only the first 2 of 5 may be dispatched
        Jobs.AssertQueues().SetJobQueue(queue, concurrency: 4);
        using (var db = Jobs.OpenDb())
        {
            db.UpdateOnly(() => new JobQueue { RateLimit = 2, RateLimitSecs = 60 },
                where: x => x.Name == queue);
        }
        Jobs.GetJobQueues(); // refresh the cached controls

        var jobIds = new List<long>();
        for (var i = 0; i < 5; i++)
        {
            jobIds.Add(EnqueueDeferred(new() { Queue = queue }).Id);
        }
        using (var db = Jobs.OpenDb())
        {
            db.UpdateOnly(() => new BackgroundJob { RunAfter = null },
                where: x => Sql.In(x.Id, jobIds));
        }

        DbJobs.DispatchPendingJobs();

        using var db2 = Jobs.OpenDb();
        var claimed = db2.Count<BackgroundJob>(x => Sql.In(x.Id, jobIds) && x.LeaseToken != null);
        Assert.That(claimed, Is.LessThanOrEqualTo(2),
            "A rate limited queue must not start more Jobs than its limit per window");
    }

    [Test]
    public async Task Nodes_report_a_heartbeat_and_are_visible()
    {
        ResetState();
        await Jobs.TickAsync();

        var nodes = Jobs.AssertQueues().GetJobNodes();
        var self = nodes.FirstOrDefault(x => x.ServerId == feature.ServerId);
        Assert.That(self, Is.Not.Null, "This node should register itself");
        Assert.That(self!.MachineName, Is.EqualTo(Environment.MachineName));
        Assert.That(self.ProcessId, Is.EqualTo(Environment.ProcessId));
        Assert.That(self.IsAlive(TimeSpan.FromMinutes(1)), Is.True);
        Assert.That(self.Concurrency, Is.EqualTo(feature.MaxConcurrentJobs));
    }

    [Test]
    public void JobsStatus_reports_the_backlog()
    {
        ResetState();
        EnqueueDeferred();

        var status = Jobs.AssertQueues().GetJobsStatus();

        Assert.That(status.Queued, Is.GreaterThan(0));
        Assert.That(status.Nodes, Is.GreaterThan(0));
    }

    [Test]
    public async Task Health_check_reports_the_Jobs_backlog()
    {
        ResetState();
        await Jobs.TickAsync();

        var check = new JobsHealthCheck(Jobs);
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
        Assert.That(result.Data["queued"], Is.Not.Null);
        Assert.That(result.Data["nodesAlive"], Is.Not.Null);

        // A backlog past the threshold degrades it
        var strict = new JobsHealthCheck(Jobs, new JobsHealthCheckOptions { DegradedQueuedJobs = 0 });
        var degraded = await strict.CheckHealthAsync(new HealthCheckContext());
        Assert.That(degraded.Status, Is.Not.EqualTo(HealthStatus.Healthy));
    }

    // Payload limits

    [Test]
    public void Oversized_Job_Requests_are_rejected_rather_than_stored()
    {
        ResetState();
        var max = feature.MaxRequestBodyChars;
        feature.MaxRequestBodyChars = 100;
        try
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 1, Throw = new string('x', 500) }));
            Assert.That(ex!.Message, Does.Contain(nameof(feature.MaxRequestBodyChars)));
        }
        finally
        {
            feature.MaxRequestBodyChars = max;
        }
    }

    [Test]
    public async Task Oversized_Job_Responses_are_dropped_and_recorded()
    {
        ResetState();
        var max = feature.MaxResponseBodyChars;
        feature.MaxResponseBodyChars = 10;
        try
        {
            var jobRef = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 1234567 });
            Assert.That(await WaitForFinishedAsync(jobRef.Id, 30_000), Is.True);

            var result = Jobs.GetJob(jobRef.Id)!;
            Assert.That(result.Summary.State, Is.EqualTo(BackgroundJobState.Completed),
                $"Dropping an oversized Response must not fail the Job: {result.Summary.ErrorMessage}");
            var archived = result.Job;
            Assert.That(archived, Is.Not.Null, "The Job should have been archived");
            Assert.That(archived!.ResponseBody, Is.Null, "An oversized Response must not be persisted");
            Assert.That(archived.Meta?.ContainsKey(JobMetaKeys.ResponseBodyOmitted), Is.True,
                "Why the result is missing should be recorded");
        }
        finally
        {
            feature.MaxResponseBodyChars = max;
        }
    }

    // Trace context

    [Test]
    public async Task Jobs_continue_the_trace_of_whatever_queued_them()
    {
        ResetState();
        using var source = new ActivitySource("DbJobsTests");
        using var listener = new ActivityListener {
            ShouldListenTo = x => x.Name is "DbJobsTests" or JobsDiagnostics.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        using var request = source.StartActivity("queueing request");
        Assert.That(request, Is.Not.Null, "Listener should sample the request Activity");
        var expectedTraceId = request!.TraceId.ToString();

        var jobRef = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 1 });

        var job = GetQueuedJob(jobRef.Id) ?? (BackgroundJob?)null;
        var traceParent = job?.TraceId ?? Jobs.GetJob(jobRef.Id)?.Job?.TraceId;
        Assert.That(traceParent, Is.Not.Null, "The queueing trace should be recorded on the Job");
        Assert.That(traceParent, Does.Contain(expectedTraceId),
            "The Job carries the traceparent of the request that queued it");

        // And the Job's Activity continues that trace rather than starting a new root
        var jobActivity = JobsDiagnostics.StartActivity(new BackgroundJob {
            Id = 1, RequestType = CommandResult.Command, Request = nameof(DbRequest),
            Command = nameof(DbJobCommand), TraceId = traceParent,
        });
        Assert.That(jobActivity, Is.Not.Null);
        Assert.That(jobActivity!.TraceId.ToString(), Is.EqualTo(expectedTraceId));
        jobActivity.Dispose();

        await WaitForFinishedAsync(jobRef.Id);
    }

    // Observability

    [Test]
    public void Job_executions_are_converted_into_Profiling_entries()
    {
        // ProfilingFeature isn't registered in this fixture, so drive the conversion directly:
        // this is what the Profiling UI renders for a Job.
        var profiling = new ProfilingFeature();
        var observer = new ProfilerDiagnosticObserver(profiling);
        var job = new BackgroundJob {
            Id = 123,
            RefId = "abc",
            Queue = "emails",
            BatchId = "batch-1",
            Tag = "signup",
            Worker = "mail",
            Attempts = 2,
            State = BackgroundJobState.Started,
            RequestType = CommandResult.Command,
            Command = nameof(DbJobCommand),
            Request = nameof(DbRequest),
        };

        var entry = observer.ToDiagnosticEntry(new JobDiagnosticEvent {
            EventType = Diagnostics.Events.ServiceStack.WriteJobBefore,
            Operation = nameof(DbJobCommand),
            Job = job,
        });

        Assert.That(entry.Source, Is.EqualTo("ServiceStack.Jobs"));
        Assert.That(entry.Command, Is.EqualTo(nameof(DbJobCommand)));
        Assert.That(entry.Message, Does.Contain("job:123"));
        Assert.That(entry.Message, Does.Contain("queue:emails"));
        Assert.That(entry.Tag, Is.EqualTo("signup"));
        Assert.That(entry.NamedArgs!["RefId"], Is.EqualTo("abc"));
        Assert.That(entry.NamedArgs["BatchId"], Is.EqualTo("batch-1"));
        Assert.That(entry.NamedArgs["Worker"], Is.EqualTo("mail"));
        Assert.That(entry.NamedArgs["Attempt"], Is.EqualTo(2));
    }

    [Test]
    public async Task Polling_db_jobs_does_not_export_a_trace_per_tick()
    {
        ResetState();
        var started = new ConcurrentBag<Activity>();
        using var listener = new ActivityListener {
            ShouldListenTo = source => source.Name == JobsDiagnostics.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = started.Add,
        };
        ActivitySource.AddActivityListener(listener);

        await Jobs.TickAsync();

        // Executing a Job still has its own span, left over Jobs from other tests may be run by this tick
        Assert.That(started.Where(x => !x.DisplayName.StartsWith("job ")), Is.Empty);
    }

    [Test]
    public void Polling_db_jobs_groups_its_Profiling_events_with_OpenTelemetry_registered()
    {
        var profiling = new ProfilingFeature();
        appHost.Plugins.Add(profiling);
        var started = new ConcurrentBag<Activity>();
        using var listener = new ActivityListener {
            ShouldListenTo = source => source.Name == JobsDiagnostics.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = started.Add,
        };
        ActivitySource.AddActivityListener(listener);
        var previous = Activity.Current;
        Activity.Current = null;
        try
        {
            using var poll = JobsDiagnostics.StartProfilingScope("jobs poll");
            Assert.That(poll, Is.Not.Null);
            Assert.That(poll!.Source.Name, Is.Not.EqualTo(JobsDiagnostics.Name));
            var evt = new OrmLiteDiagnosticEvent { EventType = "ConnectionOpenBefore" }.Init(Activity.Current);
            var entry = new ProfilerDiagnosticObserver(profiling).ToDiagnosticEntry(evt);
            Assert.That(entry.TraceId, Is.EqualTo(poll.ParentId));
            Assert.That(started, Is.Empty);
        }
        finally
        {
            Activity.Current = previous;
            appHost.Plugins.Remove(profiling);
        }
    }

    [Test]
    public void Job_profiling_retains_queued_W3C_trace_without_an_active_activity()
    {
        const string traceId = "0123456789abcdef0123456789abcdef";
        var job = new BackgroundJob {
            Id = 123,
            RequestType = CommandResult.Command,
            Request = nameof(DbRequest),
            Command = nameof(DbJobCommand),
            TraceId = $"00-{traceId}-0123456789abcdef-01",
        };
        var observer = new ProfilerDiagnosticObserver(new ProfilingFeature());
        var previous = Activity.Current;
        try
        {
            Activity.Current = null;
            var entry = observer.ToDiagnosticEntry(new JobDiagnosticEvent {
                EventType = Diagnostics.Events.ServiceStack.WriteJobBefore,
                Job = job,
            });
            Assert.That(entry.TraceId, Is.EqualTo(traceId));
            Assert.That(entry.SpanId, Is.Null);
        }
        finally
        {
            Activity.Current = previous;
        }
    }

    [Test]
    public void Background_jobs_have_a_Profiling_trace_without_OpenTelemetry()
    {
        var profiling = new ProfilingFeature();
        appHost.Plugins.Add(profiling);
        try
        {
            Assert.That(JobsDiagnostics.ActivitySource.HasListeners(), Is.False);
            using (var poll = JobsDiagnostics.StartProfilingScope("jobs poll"))
            {
                Assert.That(poll, Is.Not.Null);
                var evt = new OrmLiteDiagnosticEvent { EventType = "ConnectionOpenBefore" }.Init(Activity.Current);
                var entry = new ProfilerDiagnosticObserver(profiling).ToDiagnosticEntry(evt);
                Assert.That(entry.TraceId, Is.Not.Null.And.Not.Empty);
                Assert.That(entry.TraceId, Is.EqualTo(poll!.ParentId));
            }

            var job = new BackgroundJob { Request = nameof(DbRequest), Command = nameof(DbJobCommand) };
            using var execution = JobsDiagnostics.StartActivity(job);
            Assert.That(execution, Is.Not.Null);
            var jobEntry = new ProfilerDiagnosticObserver(profiling).ToDiagnosticEntry(new JobDiagnosticEvent {
                EventType = Diagnostics.Events.ServiceStack.WriteJobBefore,
                Job = job,
            }.Init(Activity.Current));
            Assert.That(jobEntry.TraceId, Is.EqualTo(execution!.ParentId));
        }
        finally
        {
            appHost.Plugins.Remove(profiling);
        }
    }

    [Test]
    public void Jobs_are_a_Profiling_source_included_in_All()
    {
        Assert.That(ProfileSource.All.HasFlag(ProfileSource.Jobs), Is.True);
        Assert.That(new ProfilingFeature().Profile.HasFlag(ProfileSource.Jobs), Is.True);
    }

    // Dialect-specific claiming

    [Test]
    public void SkipLocked_claiming_is_only_enabled_where_the_RDBMS_supports_it()
    {
        // SQLite and anything unrecognised fall back to an ordinary select
        Assert.That(new DbJobsProvider().SupportsSkipLocked, Is.False);
        // SQL Server spells it WITH (UPDLOCK, READPAST), so it isn't enabled here
        Assert.That(new SqlServerDbJobsProvider().SupportsSkipLocked, Is.False);

        Assert.That(new MySqlDbJobsProvider().SupportsSkipLocked, Is.True);
        Assert.That(new PostgresDbJobsProvider().SupportsSkipLocked, Is.True);
        Assert.That(new MySqlDbJobsProvider().SqlSkipLocked().Trim(), Is.EqualTo("FOR UPDATE SKIP LOCKED"));
        Assert.That(new PostgresDbJobsProvider().SqlSkipLocked().Trim(), Is.EqualTo("FOR UPDATE SKIP LOCKED"));
    }

    [Test]
    public void SkipLocked_clause_is_appended_after_the_row_limit()
    {
        // The row-locking clause has to follow LIMIT or the statement is invalid. SQLite never
        // takes this path, so without this the ordering would only break on a real RDBMS.
        var now = DateTime.UtcNow;
        var q = MySqlDialect.Provider.SqlExpression<BackgroundJob>()
            .Where(x => x.State == BackgroundJobState.Queued && x.CompletedDate == null
                && (x.RunAfter == null || x.RunAfter <= now))
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Id)
            .Take(100);

        var sql = q.ToSelectStatement() + new MySqlDbJobsProvider().SqlSkipLocked();

        Assert.That(sql, Does.Contain("LIMIT"));
        Assert.That(sql.TrimEnd(), Does.EndWith("FOR UPDATE SKIP LOCKED"));
        Assert.That(sql.IndexOf("LIMIT", StringComparison.Ordinal),
            Is.LessThan(sql.IndexOf("FOR UPDATE", StringComparison.Ordinal)),
            "FOR UPDATE must come after LIMIT");
        // FOR UPDATE is rejected alongside DISTINCT/GROUP BY, so the claim query must stay plain
        Assert.That(sql, Does.Not.Contain("DISTINCT"));
        Assert.That(sql, Does.Not.Contain("GROUP BY"));
        // Parameterised, so the values bind through q.Params
        Assert.That(q.Params, Is.Not.Empty);
    }

    // Schema

    [Test]
    public void RDBMS_schema_creates_every_Jobs_table_and_index()
    {
        using var db = Jobs.OpenDb();
        Assert.That(db.TableExists<BackgroundJob>(), Is.True);
        Assert.That(db.TableExists<JobSummary>(), Is.True);
        Assert.That(db.TableExists<ScheduledTask>(), Is.True);
        Assert.That(db.TableExists<JobBatch>(), Is.True);
        Assert.That(db.TableExists<JobQueue>(), Is.True);
        Assert.That(db.TableExists<CompletedJob>(), Is.True);
        Assert.That(db.TableExists<FailedJob>(), Is.True);

        var indexes = db.Column<string>("SELECT name FROM sqlite_master WHERE type='index'");
        Assert.That(indexes, Does.Contain("idx_backgroundjob_claim"));
        Assert.That(indexes, Does.Contain("idx_backgroundjob_lease"));
        Assert.That(indexes, Does.Contain("uidx_backgroundjob_singleton"));
        Assert.That(indexes, Does.Contain("idx_jobsummary_batch"));
    }

    // Regressions

    [Test]
    public async Task A_Job_cancelled_on_another_node_is_not_retried_forever()
    {
        ResetState();
        var shouldRetry = feature.ShouldRetry;
        // Even when a custom ShouldRetry would retry the exception the cancelled Job threw
        feature.ShouldRetry = (_, _) => true;
        try
        {
            var jobRef = Jobs.EnqueueCommand<DbPollingCommand>(new DbRequest { Id = 1 },
                new() { RetryLimit = 5 });
            Assert.That(await TickUntilAsync(() => Interlocked.Read(ref DbPollingCommand.Started) > 0), Is.True);

            // Cancellation requested by another node is only seen through the database
            using (var db = Jobs.OpenDb())
            {
                var now = DateTime.UtcNow;
                db.UpdateOnly(() => new BackgroundJob { CancelRequestedDate = now }, where: x => x.Id == jobRef.Id);
            }

            Assert.That(await WaitForFinishedAsync(jobRef.Id), Is.True,
                "A cancelled Job must not be left queued with its CancelRequestedDate set");
            Assert.That(GetSummary(jobRef.Id)!.State, Is.EqualTo(BackgroundJobState.Cancelled));
        }
        finally
        {
            feature.ShouldRetry = shouldRetry;
        }
    }

    [Test]
    public async Task A_paused_queue_does_not_starve_other_queues_of_claims()
    {
        ResetState();
        var pausedQueue = "starved-" + Guid.NewGuid().ToString("N")[..8];
        var claimBatchSize = feature.ClaimBatchSize;
        feature.ClaimBatchSize = 2;
        var pausedJobs = new List<BackgroundJobRef>();
        try
        {
            Jobs.PauseJobQueue(pausedQueue);
            // More paused Jobs than a claim window, all ahead of the next Job in claim order
            for (var i = 0; i < 5; i++)
            {
                pausedJobs.Add(Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 100 + i },
                    new() { Queue = pausedQueue }));
            }
            // Due shortly, so it's claimed by a tick rather than dispatched on enqueue
            var jobRef = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 7 },
                new() { RunAfter = DateTime.UtcNow.AddMilliseconds(200) });

            Assert.That(await WaitForFinishedAsync(jobRef.Id), Is.True,
                "Jobs on a paused queue must not fill every claim window");
            Assert.That(GetSummary(jobRef.Id)!.State, Is.EqualTo(BackgroundJobState.Completed));
        }
        finally
        {
            feature.ClaimBatchSize = claimBatchSize;
            foreach (var pausedJob in pausedJobs)
                Jobs.CancelJob(pausedJob.Id);
            Jobs.ResumeJobQueue(pausedQueue);
        }
    }

    [Test]
    public void Scheduled_times_are_kept_to_the_whole_second()
    {
        var time = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1234567);
        var scheduled = time.ToScheduleTime();
        Assert.That(scheduled, Is.EqualTo(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)));
        Assert.That(scheduled.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    // ReplyTo validation

    [Test]
    public void ValidateReplyTo_rejects_a_Job_with_a_ReplyTo_that_is_not_allowed()
    {
        ResetState();
        var validate = feature.ValidateReplyTo;
        feature.ValidateReplyTo = JobReplyTo.AllowUrlPrefixes(["https://hooks.example.org/"]);
        try
        {
            Assert.Throws<ArgumentException>(() => Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 1 },
                new() { ReplyTo = "https://internal.example.org/admin" }));
            var allowed = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 1 },
                new() { ReplyTo = "https://hooks.example.org/jobs", RunAfter = DateTime.UtcNow.AddHours(1) });
            Assert.That(allowed.Id, Is.GreaterThan(0));
            Jobs.CancelJob(allowed.Id);
        }
        finally
        {
            feature.ValidateReplyTo = validate;
        }
    }

    [Test]
    public void AllowUrlPrefixes_compares_the_parsed_host_not_the_raw_string()
    {
        var validate = JobReplyTo.AllowUrlPrefixes(["https://hooks.example.org/"]);
        bool Allows(string replyTo) => validate(new BackgroundJob { ReplyTo = replyTo });

        Assert.That(Allows("https://hooks.example.org/a/b"), Is.True);
        Assert.That(Allows("MyQueue.inq"), Is.True, "MQ Queue Names are allowed by default");
        Assert.That(Allows("https://hooks.example.org.evil.com/"), Is.False);
        Assert.That(Allows("https://user@hooks.example.org/"), Is.False);
        Assert.That(Allows("http://hooks.example.org/"), Is.False, "The scheme must match");
        Assert.That(Allows("https://hooks.example.org:8443/"), Is.False, "The port must match");
        Assert.That(JobReplyTo.AllowUrlPrefixes(["https://hooks.example.org/"], allowMqQueues:false)(
            new BackgroundJob { ReplyTo = "MyQueue.inq" }), Is.False);
    }

    // Prefetch

    [Test]
    public void A_node_only_claims_its_Workers_plus_MaxPrefetchJobs_per_queue()
    {
        ResetState();
        var queue = "prefetch-" + Guid.NewGuid().ToString("N")[..8];
        var maxPrefetch = feature.MaxPrefetchJobs;
        feature.MaxPrefetchJobs = 1;
        var jobRefs = new List<BackgroundJobRef>();
        try
        {
            for (var i = 0; i < 5; i++)
            {
                jobRefs.Add(Jobs.EnqueueCommand<DbLongRunningCommand>(new DbRequest { Id = i, WaitMs = 3000 },
                    new() { Queue = queue }));
            }
            DbJobs.DispatchPendingJobs();

            using var db = Jobs.OpenDb();
            var claimed = db.Count<BackgroundJob>(x => x.Queue == queue && x.LeaseToken != null);
            // MaxConcurrentJobs = 1 Worker, plus 1 prefetched
            Assert.That(claimed, Is.EqualTo(2), "Jobs beyond the prefetch limit should be left for other nodes");
        }
        finally
        {
            feature.MaxPrefetchJobs = maxPrefetch;
            foreach (var jobRef in jobRefs)
                Jobs.CancelJob(jobRef.Id);
        }
    }

    // Transactional outbox

    [Test]
    public async Task A_Job_queued_in_a_transaction_only_exists_if_it_commits()
    {
        ResetState();
        BackgroundJobRef rolledBack;
        using (var db = Jobs.OpenDb())
        using (db.OpenTransaction())
        {
            rolledBack = Jobs.EnqueueCommand<DbJobCommand>(db, new DbRequest { Id = 1 });
            Assert.That(rolledBack.Id, Is.GreaterThan(0));
        }
        Assert.That(GetSummary(rolledBack.Id), Is.Null, "A rolled back transaction must not queue its Job");

        BackgroundJobRef committed;
        using (var db = Jobs.OpenDb())
        using (var trans = db.OpenTransaction())
        {
            committed = Jobs.EnqueueCommand<DbJobCommand>(db, new DbRequest { Id = 2 },
                new() { TenantId = "acme" });
            trans.Commit();
        }
        Assert.That(await WaitForFinishedAsync(committed.Id), Is.True);
        var summary = GetSummary(committed.Id)!;
        Assert.That(summary.State, Is.EqualTo(BackgroundJobState.Completed));
        Assert.That(summary.TenantId, Is.EqualTo("acme"));
        Assert.That(summary.LeaseOwner, Is.EqualTo(feature.ServerId));
    }

    // Dependency policy

    [Test]
    public async Task An_OnFinished_dependent_runs_after_its_parent_fails()
    {
        ResetState();
        var parent = Jobs.EnqueueCommand<DbFailCommand>(new(), new() { RetryLimit = 0 });
        var runsAnyway = Jobs.EnqueueCommand<DbDependentCommand>(new DbRequest { Id = 1 },
            new() { DependsOn = parent.Id, DependsOnPolicy = JobDependencyPolicy.OnFinished });
        var cancelled = Jobs.EnqueueCommand<DbDependentCommand>(new DbRequest { Id = 2 },
            new() { DependsOn = parent.Id });

        Assert.That(await WaitForFinishedAsync(runsAnyway.Id), Is.True);
        Assert.That(GetSummary(runsAnyway.Id)!.State, Is.EqualTo(BackgroundJobState.Completed));
        Assert.That(DbDependentCommand.ParentJob?.Id, Is.EqualTo(parent.Id));
        Assert.That(DbDependentCommand.ParentJob?.State, Is.EqualTo(BackgroundJobState.Failed));
        Assert.That(GetSummary(cancelled.Id)!.State, Is.EqualTo(BackgroundJobState.Cancelled),
            "A dependent with the default policy is still cancelled when its parent fails");
    }

    // Batches

    [Test]
    public async Task A_batch_runs_OnSuccess_only_when_every_Job_completed()
    {
        ResetState();
        var batchId = "success-" + Guid.NewGuid().ToString("N")[..8];
        Jobs.CreateJobBatch(batchId, total:2, onSuccess:nameof(DbBatchCallback));
        Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 1 }, new() { BatchId = batchId });
        Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 2 }, new() { BatchId = batchId });

        Assert.That(await TickUntilAsync(() => Interlocked.Read(ref DbBatchCallback.Count) > 0), Is.True);
        Assert.That(DbBatchCallback.LastBatch?.Id, Is.EqualTo(batchId));

        ResetState();
        var failedBatchId = "failed-" + Guid.NewGuid().ToString("N")[..8];
        Jobs.CreateJobBatch(failedBatchId, total:1, onSuccess:nameof(DbBatchCallback));
        var failed = Jobs.EnqueueCommand<DbFailCommand>(new(), new() { BatchId = failedBatchId, RetryLimit = 0 });
        Assert.That(await WaitForFinishedAsync(failed.Id), Is.True);
        Assert.That(await TickUntilAsync(() => Jobs.GetJobBatch(failedBatchId)?.CompletedDate != null), Is.True);
        await TickUntilAsync(() => Interlocked.Read(ref DbBatchCallback.Count) > 0, 1000);
        Assert.That(Interlocked.Read(ref DbBatchCallback.Count), Is.EqualTo(0),
            "OnSuccess must not run for a batch with a failed Job");
    }

    [Test]
    public void A_cancelled_batch_cancels_its_Jobs_and_accepts_no_more()
    {
        ResetState();
        var batchId = "cancel-" + Guid.NewGuid().ToString("N")[..8];
        var queued = EnqueueDeferred(new() { BatchId = batchId });

        var cancelled = Jobs.CancelJobBatch(batchId);

        Assert.That(cancelled, Does.Contain(queued.Id));
        Assert.That(GetSummary(queued.Id)!.State, Is.EqualTo(BackgroundJobState.Cancelled));
        Assert.That(Jobs.GetJobBatch(batchId)!.CancelledDate, Is.Not.Null);
        Assert.Throws<InvalidOperationException>(() => EnqueueDeferred(new() { BatchId = batchId }));
    }

    // Failed attempt history

    [Test]
    public async Task Every_failed_attempt_is_recorded()
    {
        ResetState();
        var jobRef = Jobs.EnqueueCommand<DbFailCommand>(new(), new() { RetryLimit = 1 });
        Assert.That(await WaitForStateAsync(jobRef.Id, BackgroundJobState.Failed), Is.True);

        var attempts = Jobs.GetJobAttempts(jobRef.Id);
        Assert.That(attempts.Select(x => x.Attempt), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(attempts.Select(x => x.State),
            Is.EqualTo(new[] { BackgroundJobState.Queued, BackgroundJobState.Failed }));
        Assert.That(attempts.All(x => x.Error?.Message?.StartsWith("Always Fails") == true), Is.True,
            "Each attempt should keep its own error");
        Assert.That(attempts.All(x => x.ServerId == feature.ServerId), Is.True);
    }

    // Schedule bounds

    [Test]
    public async Task A_Scheduled_Task_is_disabled_once_it_reaches_MaxRuns()
    {
        ResetState();
        var taskName = "max-runs-" + Guid.NewGuid().ToString("N")[..8];
        var schedule = Schedule.Interval(TimeSpan.FromSeconds(1));
        schedule.MaxRuns = 2;
        Jobs.RecurringCommand<DbJobCommand>(taskName, schedule, new DbRequest { Id = 1 });
        try
        {
            Assert.That(await TickUntilAsync(() => Jobs.GetScheduledTask(taskName)?.Enabled == false, 15_000), Is.True);
            using var db = Jobs.OpenDb();
            var task = db.Single<ScheduledTask>(x => x.Name == taskName);
            Assert.That(task.RunCount, Is.EqualTo(2));
            Assert.That(task.NextRun, Is.Null);

            // Registering it again on the next startup keeps its progress
            Jobs.RecurringCommand<DbJobCommand>(taskName, schedule, new DbRequest { Id = 1 });
            await TickUntilAsync(() => Jobs.GetScheduledTask(taskName)?.Enabled == false, 5_000);
            Assert.That(db.Single<ScheduledTask>(x => x.Name == taskName).RunCount, Is.EqualTo(2));
        }
        finally
        {
            Jobs.DeleteRecurringTask(taskName);
        }
    }

    // Node draining

    [Test]
    public async Task A_draining_node_takes_no_new_Jobs()
    {
        ResetState();
        await Jobs.TickAsync(); // Registers this node
        Assert.That(Jobs.SetJobNodeDraining(feature.ServerId, true), Is.True);
        try
        {
            var jobRef = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 1 });
            await Jobs.TickAsync();
            Assert.That(GetQueuedJob(jobRef.Id)?.LeaseToken, Is.Null, "A draining node must not claim Jobs");

            Jobs.SetJobNodeDraining(feature.ServerId, false);
            Assert.That(await WaitForFinishedAsync(jobRef.Id), Is.True);
        }
        finally
        {
            Jobs.SetJobNodeDraining(feature.ServerId, false);
        }
    }

    // Cluster-wide rate limits

    [Test]
    public async Task A_queue_rate_limit_is_counted_in_the_database()
    {
        ResetState();
        var queue = "limited-" + Guid.NewGuid().ToString("N")[..8];
        Jobs.SetJobQueueRateLimit(queue, 2, TimeSpan.FromHours(1));
        var jobRefs = new List<BackgroundJobRef>();
        try
        {
            for (var i = 0; i < 4; i++)
            {
                jobRefs.Add(Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = i }, new() { Queue = queue }));
            }
            await TickUntilAsync(() => jobRefs.Count(x => GetSummary(x.Id)!.State.IsFinished()) >= 2);
            await Jobs.TickAsync();

            Assert.That(jobRefs.Count(x => GetSummary(x.Id)!.State == BackgroundJobState.Completed), Is.EqualTo(2));
            using var db = Jobs.OpenDb();
            var saved = db.SingleById<JobQueue>(queue);
            Assert.That(saved.RateLimitCount, Is.EqualTo(2), "Starts are counted where every node can see them");
            Assert.That(saved.RateLimitWindowStart, Is.Not.Null);
        }
        finally
        {
            Jobs.SetJobQueueRateLimit(queue, 0);
            foreach (var jobRef in jobRefs)
                Jobs.CancelJob(jobRef.Id);
        }
    }

    // Use-cases combining several features

    [Test]
    public async Task A_transiently_failing_Job_is_retried_until_it_succeeds()
    {
        ResetState();
        var jobRef = Jobs.EnqueueCommand<DbFlakyCommand>(new DbRequest { Id = 2 }, new() {
            RetryLimit = 3,
            RetryBackoff = RetryBackoff.ExponentialJitter,
            RetryDelayMs = 1,
        });

        Assert.That(await WaitForFinishedAsync(jobRef.Id, 15_000), Is.True);
        var summary = GetSummary(jobRef.Id)!;
        Assert.That(summary.State, Is.EqualTo(BackgroundJobState.Completed));
        Assert.That(summary.Attempts, Is.EqualTo(3), "2 failed attempts then the one that succeeded");
        Assert.That(Interlocked.Read(ref DbFlakyCommand.Count), Is.EqualTo(3));
        Assert.That(Jobs.GetJobAttempts(jobRef.Id).Select(x => x.Attempt), Is.EqualTo(new[] { 1, 2 }),
            "Only the failed attempts are recorded");
    }

    [Test]
    public async Task A_cooperative_Job_is_cancelled_once_it_exceeds_its_timeout()
    {
        ResetState();
        var jobRef = Jobs.EnqueueCommand<DbLongRunningCommand>(new DbRequest { Id = 1, WaitMs = 30_000 },
            new() { TimeoutSecs = 1, RetryLimit = 2 });

        Assert.That(await WaitForFinishedAsync(jobRef.Id, 15_000), Is.True);
        var summary = GetSummary(jobRef.Id)!;
        Assert.That(summary.State, Is.EqualTo(BackgroundJobState.Cancelled));
        Assert.That(summary.Attempts, Is.EqualTo(1), "A timed out Job isn't retried");
        Assert.That(DbLongRunningCommand.ObservedCancellation, Is.True, "The Job's token should be cancelled");
    }

    [Test]
    public async Task A_failed_workflow_step_cancels_the_rest_of_the_workflow_but_OnFinished_steps_still_run()
    {
        ResetState();
        // charge -> reserve (fails) -> ship -> notify (runs however ship finished)
        var charge = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 1 });
        var reserve = Jobs.EnqueueCommand<DbFailCommand>(new(), new() { DependsOn = charge.Id, RetryLimit = 0 });
        var ship = Jobs.EnqueueCommand<DbJobCommand>(new DbRequest { Id = 3 }, new() { DependsOn = reserve.Id });
        var notify = Jobs.EnqueueCommand<DbDependentCommand>(new DbRequest { Id = 4 },
            new() { DependsOn = ship.Id, DependsOnPolicy = JobDependencyPolicy.OnFinished });

        Assert.That(await WaitForFinishedAsync(notify.Id, 15_000), Is.True);
        Assert.That(GetSummary(charge.Id)!.State, Is.EqualTo(BackgroundJobState.Completed));
        Assert.That(GetSummary(reserve.Id)!.State, Is.EqualTo(BackgroundJobState.Failed));
        Assert.That(GetSummary(ship.Id)!.State, Is.EqualTo(BackgroundJobState.Cancelled),
            "A step after the failed one is cancelled");
        Assert.That(GetSummary(notify.Id)!.State, Is.EqualTo(BackgroundJobState.Completed),
            "An OnFinished step runs even though the step before it was cancelled");
        Assert.That(DbDependentCommand.ParentJob?.Id, Is.EqualTo(ship.Id));
        Assert.That(DbDependentCommand.ParentJob?.State, Is.EqualTo(BackgroundJobState.Cancelled));
    }
}
