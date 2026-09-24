#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using ServiceStack.Admin;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.Host;
using ServiceStack.IO;
using ServiceStack.Jobs;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Extensions.Tests;

public class MyRequest : IReturn<MyResponse>
{
    public int Id { get; set; }
    public int? WaitMs { get; set; }
    public string? Throw { get; set; }
}
public class MyResponse
{
    public required string Result { get; set; }
}

class MyJobCommand(ILogger<MyJobCommand> logger, IBackgroundJobs jobs) : AsyncCommandWithResult<MyRequest,MyResponse>
{
    public static long Count;
    public static IRequest? LastRequest { get; set; }
    public static List<MyRequest> Requests { get; set; } = new();

    protected override async Task<MyResponse> RunAsync(MyRequest request, CancellationToken token)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        log.UpdateStatus(0.1, "Started", "MyCommand Started...");
        Interlocked.Increment(ref Count);
        LastRequest = Request;
        Requests.Add(request);
        if (request.WaitMs != null)
        {
            var wait = TimeSpan.FromMilliseconds(request.WaitMs.Value);
            var startedAt = DateTime.UtcNow;
            var i = 0;
            log.UpdateStatus("Waiting");
            while (DateTime.UtcNow - startedAt < wait)
            {
                token.ThrowIfCancellationRequested();
                var waited = DateTime.UtcNow - startedAt;
                log.UpdateProgress(waited.TotalMilliseconds / wait.TotalMilliseconds);
                log.LogInformation("MyCommand {Count} Waited {Waited:g}...", i++, waited);
                await Task.Delay(ExecUtils.CalculateFullJitterBackOffDelay(++i), token);
            }
        }
        if (request.Throw != null)
            throw new Exception(request.Throw);
        log.UpdateStatus(0.9, "Finished", "MyCommand Finished");
        return new MyResponse { Result = $"Hello {request.Id}" };
    }
}

class MySyncCommand(ILogger<MySyncCommand> logger, IBackgroundJobs jobs) : SyncCommand
{
    public static long Count;
    public static IRequest? LastRequest { get; set; }
    protected override void Run()
    {
        var log = Request.CreateJobLogger(jobs, logger);
        log.UpdateStatus(0.1, "Started", "MyCommand Started...");
        Interlocked.Increment(ref Count);
        LastRequest = Request;
        log.UpdateStatus(0.9, "Finished", "MyCommand Finished");
    }
}

class MyJobCallback(IBackgroundJobs jobs) : SyncCommand<MyResponse>
{
    public static long Count;
    public static IRequest? LastRequest { get; set; }
    public static MyResponse? LastMyResponse { get; set; }
    protected override void Run(MyResponse request)
    {
        Interlocked.Increment(ref Count);
        LastRequest = Request;
        LastMyResponse = request;
    }
}

[IgnoreServices]
public class JobServices(IBackgroundJobs jobs) : Service
{
    public static long Count;
    public static IRequest? LastRequest { get; set; }
    public static List<MyRequest> Requests { get; set; } = new();

    public object Any(MyRequest request)
    {
        var log = Request!.CreateJobLogger(jobs);
        log.UpdateStatus(0.1, "Started", "My API Started...");
        Interlocked.Increment(ref Count);
        LastRequest = Request;
        Requests.Add(request);
        log.UpdateStatus(0.9, "Finished", "My API Finished");
        return new MyResponse { Result = $"Hello {request.Id}" };
    }

    public object Any(AlwaysFails request)
    {
        Interlocked.Increment(ref Count);
        throw new Exception("Always Fails: " + Count);
    }
}

public class AlwaysFails : IReturn<EmptyResponse> {}

public class AlwaysFailCommand : SyncCommand
{
    public static long Count;
    public static IRequest? LastRequest { get; set; }
    protected override void Run()
    {
        LastRequest = Request;
        Interlocked.Increment(ref Count);
        throw new Exception("Always Fails: " + Count);
    }
}

/// <summary>Fails its first request.Id attempts, then succeeds, like a flaky downstream service</summary>
public class SqliteFlakyCommand : SyncCommand<MyRequest>
{
    public static long Count;
    protected override void Run(MyRequest request)
    {
        Interlocked.Increment(ref Count);
        var job = Request.GetBackgroundJob();
        if (job.Attempts <= request.Id)
            throw new Exception($"Service unavailable, attempt {job.Attempts}");
    }
}

public class SqliteSlowRequest
{
    public string Key { get; set; } = "";
    public int Ms { get; set; }
}

/// <summary>Records how many Jobs ran at once, overall and per key, and whether it was cancelled</summary>
public class SqliteSlowCommand : AsyncCommand<SqliteSlowRequest>
{
    static readonly object sync = new();
    public static Dictionary<string, int> Running = new();
    public static Dictionary<string, int> MaxByKey = new();
    public static int RunningOverall, MaxOverall;
    public static bool ObservedCancellation;

    public static void Reset()
    {
        lock (sync)
        {
            Running.Clear();
            MaxByKey.Clear();
            RunningOverall = MaxOverall = 0;
            ObservedCancellation = false;
        }
    }

    protected override async Task RunAsync(SqliteSlowRequest request, CancellationToken token)
    {
        lock (sync)
        {
            Running[request.Key] = Running.GetValueOrDefault(request.Key) + 1;
            MaxByKey[request.Key] = Math.Max(MaxByKey.GetValueOrDefault(request.Key), Running[request.Key]);
            MaxOverall = Math.Max(MaxOverall, ++RunningOverall);
        }
        try
        {
            await Task.Delay(request.Ms, token);
        }
        catch (OperationCanceledException)
        {
            ObservedCancellation = true;
            throw;
        }
        finally
        {
            lock (sync)
            {
                Running[request.Key]--;
                RunningOverall--;
            }
        }
    }
}

public class SqliteBatchCallbackCommand : SyncCommand<JobBatch>
{
    public static long Count;
    public static JobBatch? LastBatch;
    protected override void Run(JobBatch request)
    {
        LastBatch = request;
        Interlocked.Increment(ref Count);
    }
}

public class SqliteBatchSuccessCommand : SyncCommand<JobBatch>
{
    public static long Count;
    protected override void Run(JobBatch request) => Interlocked.Increment(ref Count);
}

public class DependentJob
{
    public long Id { get; set; }
}
public class DependentJobResult
{
    public required long Id { get; set; }
    public CompletedJob? ParentJob { get; set; }
}

public class DependentJobCommand : SyncCommandWithResult<DependentJob,DependentJobResult>
{
    public static long Count;
    public static IRequest? LastRequest { get; set; }
    public static DependentJob? LastCommandRequest { get; set; }
    protected override DependentJobResult Run(DependentJob request)
    {
        Interlocked.Increment(ref Count);
        LastRequest = Request;
        LastCommandRequest = request;
        var job = Request.GetBackgroundJob();
        return new() { Id = request.Id, ParentJob = job.ParentJob };
    }
}

public class DependentJobCallbackCommand : SyncCommand<DependentJobResult>
{
    public static List<DependentJobResult> Results { get; set; } = new();
    protected override void Run(DependentJobResult request) => Results.Add(request);
}

public class ScopedRequest : IReturn<ApplicationUser>
{
    public string UserId { get; set; }
}
class MyScopedCommand(IBackgroundJobs jobs, UserManager<ApplicationUser> userManager) : AsyncCommandWithResult<ScopedRequest,ApplicationUser?>
{
    public static long Count;
    public static IRequest? LastRequest { get; set; }
    public static List<ScopedRequest> Requests { get; set; } = new();
    public static ClaimsPrincipal? LastUser { get; set; }
    public static IAuthSession? LastSession { get; set; }
    public static ApplicationUser? LastResult { get; set; }
    protected override async Task<ApplicationUser?> RunAsync(ScopedRequest request, CancellationToken token)
    {
        var log = Request.CreateJobLogger(jobs);
        log.UpdateStatus(0.1, "Started", "MyScopedCommand Started...");
        Interlocked.Increment(ref Count);
        LastRequest = Request;
        LastUser = Request.GetClaimsPrincipal();
        LastSession = await Request.GetSessionAsync(token: token);
        Requests.Add(request);
        log.UpdateStatus(0.9, "Finished", "MyScopedCommand Finished");
        return LastResult = await userManager.FindByIdAsync(request.UserId);
    }
}

[IgnoreServices]
public class JobScopedServices(IBackgroundJobs jobs, UserManager<ApplicationUser> userManager) : Service
{
    public static long Count;
    public static IRequest? LastRequest { get; set; }
    public static List<ScopedRequest> Requests { get; set; } = new();
    public static ClaimsPrincipal? LastUser { get; set; }
    public static IAuthSession? LastSession { get; set; }
    public static ApplicationUser? LastResult { get; set; }

    public async Task<object?> Any(ScopedRequest request)
    {
        var log = Request!.CreateJobLogger(jobs);
        log.UpdateStatus(0.1, "Started", "MyScopedCommand Started...");
        Interlocked.Increment(ref Count);
        LastRequest = Request;
        LastUser = Request.GetClaimsPrincipal();
        LastSession = await Request.GetSessionAsync();
        Requests.Add(request);
        LastResult = await userManager.FindByIdAsync(request.UserId);;
        log.UpdateStatus(0.9, "Finished", "MyScopedCommand Finished");
        return LastResult;
    }
}
public class JobsHostedService(ILogger<JobsHostedService> log, IBackgroundJobs jobs) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await jobs.StartAsync(stoppingToken);
        
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        var tick = 0;
        var errors = 0;
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                tick++;
                await jobs.TickAsync();
            }
            catch (Exception e)
            {
                log.LogError(e, "JOBS {Errors}/{Tick} Error in JobsHostedService: {Message}", 
                    ++errors, tick, e.Message);
            }
        }
    }
}
public class BackgroundJobsTests
{
    /// <summary>
    /// Built in OneTimeSetUp rather than the constructor: the AppHostBase constructor sets the
    /// process-global ServiceStackHost.Instance, and NUnit constructs fixture instances before the
    /// fixture runs, so a field-initialized AppHost collides with another fixture's host.
    /// </summary>
    [OneTimeSetUp]
    public void TestFixtureSetUp()
    {
        appHost = new AppHost();
        var contentRootPath = "~/../../../".MapServerPath();
        FileSystemVirtualFiles.DeleteDirectory(contentRootPath.CombineWith("App_Data/jobs"));

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = contentRootPath,
            WebRootPath = contentRootPath,
        });
        var services = builder.Services;

        // Enable console logging
        services.AddLogging(o =>
        {
            o.ClearProviders();
            o.AddProvider(new NUnitLoggerProvider());
        });

        var config = builder.Configuration;

        // Configure Auth
        var dbPath = contentRootPath.CombineWith("App_Data/app.db");
        // Delete the WAL/SHM sidecars too: removing only the main database leaves SQLite to open
        // against an inconsistent write-ahead log, which fails with "disk I/O error".
        foreach (var suffix in new[] { "", "-journal", "-wal", "-shm" })
        {
            if (File.Exists(dbPath + suffix))
                File.Delete(dbPath + suffix);
        }
        var connectionString = $"DataSource={dbPath};Cache=Shared";
        var dbFactory = new OrmLiteConnectionFactory(connectionString, SqliteDialect.Provider);
        services.AddSingleton<IDbConnectionFactory>(dbFactory);
        services.AddAuthentication(options => {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        });
        services.AddAuthorization();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString /*, b => b.MigrationsAssembly(nameof(MyApp))*/));
        services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();
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

        services.AddPlugin(new CommandsFeature());
        services.AddPlugin(feature);

        services.AddServiceStack(typeof(JobServices).Assembly);
        
        services.AddHostedService<JobsHostedService>();

        var app = builder.Build();

        app.UseServiceStack(appHost, options => { options.MapEndpoints(); });
        app.UseAuthorization();
        app.StartAsync($"http://localhost:20000");        
    }

    private AppHost appHost = null!;
    private readonly BackgroundsJobFeature feature = new();
    class AppHost() : AppHostBase(nameof(BackgroundJobsTests), typeof(JobServices).Assembly)
    {
        public override void Configure()
        {
            IdentityJwtAuthProviderTests.CreateIdentityUsers(ApplicationServices);
        }
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown() => AppHostBase.DisposeApp();

    /// <summary>Runs an Admin Jobs API with the role check relaxed, which needs an Admin session</summary>
    object ExecAdmin(Func<AdminJobServices, object> fn)
    {
        var accessRole = feature.AccessRole;
        feature.AccessRole = ServiceStack.Configuration.RoleNames.AllowAnon;
        try
        {
            using var service = appHost.Container.Resolve<AdminJobServices>();
            service.Request = new BasicRequest();
            return fn(service);
        }
        finally
        {
            feature.AccessRole = accessRole;
        }
    }

    void ResetState()
    {
        MyJobCommand.Count = 0;
        MyJobCommand.LastRequest = null;
        MyJobCommand.Requests.Clear();
        MyJobCallback.Count = 0;
        MyJobCallback.LastRequest = null;
        MyJobCallback.LastMyResponse = null;
        
        JobServices.Count = 0;
        JobServices.LastRequest = null;
        JobServices.Requests.Clear();

        MySyncCommand.Count = 0;
        AlwaysFailCommand.Count = 0;
        
        DependentJobCommand.Count = 0;
        DependentJobCommand.LastRequest = null;
        DependentJobCommand.LastCommandRequest = null;
        DependentJobCallbackCommand.Results.Clear();
        
        SqliteFlakyCommand.Count = 0;
        SqliteSlowCommand.Reset();
        SqliteBatchCallbackCommand.Count = 0;
        SqliteBatchCallbackCommand.LastBatch = null;
        SqliteBatchSuccessCommand.Count = 0;

        MyScopedCommand.Count = 0;
        MyScopedCommand.LastRequest = null;
        MyScopedCommand.LastUser = null;
        MyScopedCommand.LastSession = null;
        MyScopedCommand.LastResult = null;
        MyScopedCommand.Requests.Clear();
        
        JobScopedServices.Count = 0;
        JobScopedServices.LastRequest = null;
        JobScopedServices.LastUser = null;
        JobScopedServices.LastSession = null;
        JobScopedServices.LastResult = null;
        JobScopedServices.Requests.Clear();

        using var db = feature.OpenDb();
        db.DeleteAllAsync<BackgroundJob>();
        db.DeleteAllAsync<JobSummary>();
        db.DeleteAllAsync<ScheduledTask>();
        using var monthDb = feature.OpenMonthDb(DateTime.UtcNow);
        monthDb.DeleteAllAsync<CompletedJob>();
        monthDb.DeleteAllAsync<FailedJob>();
        ((BackgroundJobs)feature.Jobs).Clear();
    }

    void AssertNotNulls(BackgroundJob job)
    {
        Assert.That(job, Is.Not.Null);
        Assert.That(job.Id, Is.GreaterThan(0));
        Assert.That(job.CreatedDate, Is.Not.Null);
        Assert.That(job.RequestId, Is.Not.Null);
        Assert.That(job.Request, Is.EqualTo(nameof(MyRequest)).Or.EqualTo(nameof(NoArgs)));
        Assert.That(job.RequestBody, Is.Not.Null);
        Assert.That(job.LastActivityDate, Is.Not.Null);
    }

    [Test]
    public void Does_execute_MyCommand()
    {
        ResetState();
        OrmLiteUtils.PrintSql();
        feature.Jobs.StartAsync(default);
        feature.Jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = 1 });
        
        Assert.That(ExecUtils.WaitUntilTrue(() => MyJobCommand.LastRequest != null), "LastRequest == null");
        Assert.That(MyJobCommand.Requests.Count, Is.GreaterThan(0));
        var job = MyJobCommand.LastRequest.GetBackgroundJob();
        Assert.That(job!.RequestType, Is.EqualTo(CommandResult.Command));
        AssertNotNulls(job);
        Assert.That(ExecUtils.WaitUntilTrue(() => job.Status == "Finished"), "Status != Finished");
        Assert.That(ExecUtils.WaitUntilTrue(() => job.Progress >= 1), "job.Progress != 1");
        Assert.That(job.Logs, Is.EqualTo("MyCommand Started...\nMyCommand Finished"));
    }

    [Test]
    public void Does_execute_MyRequest_Api()
    {
        ResetState();
        var jobRef = feature.Jobs.EnqueueApi(new MyRequest { Id = 2 });
        
        Assert.That(ExecUtils.WaitUntilTrue(() => JobServices.LastRequest != null), "LastRequest == null");
        Assert.That(JobServices.Requests.Count, Is.GreaterThan(0));
        var job = JobServices.LastRequest.GetBackgroundJob();
        Assert.That(job.RequestType, Is.EqualTo(CommandResult.Api));
        AssertNotNulls(job);

        JobResult? jobResult = null;
        Assert.That(ExecUtils.WaitUntilTrue(() =>
        {
            jobResult = feature.Jobs.GetJob(jobRef.Id);
            return jobResult?.Completed != null;
        }), "jobResult.Completed == null");
        Assert.That(jobResult!.Summary, Is.Not.Null);
        Assert.That(jobResult!.Completed, Is.Not.Null);
        
        var request = feature.Jobs.CreateRequest(jobResult) as MyRequest;
        Assert.That(request, Is.Not.Null);
        Assert.That(request!.Id, Is.EqualTo(2));

        var response = feature.Jobs.CreateResponse(jobResult) as MyResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Result, Is.EqualTo($"Hello 2"));
    }

    [Test]
    public void Does_Run_Transient_Command()
    {
        ResetState();
        List<MyResponse> responses = new();
        List<Exception> errors = new();
        var job = feature.Jobs.RunCommand<MyJobCommand>(new MyRequest { Id = 1 }, new() {
            Worker = Workers.AppDb,
            OnSuccess = r => responses.Add((MyResponse)r!),
            OnFailed = e => errors.Add(e),
        });
        Assert.That(job.Worker, Is.EqualTo(Workers.AppDb));
        
        Assert.That(ExecUtils.WaitUntilTrue(() => MyJobCommand.LastRequest != null), "LastRequest == null");
        Assert.That(MyJobCommand.Requests.Count, Is.GreaterThan(0));
        job = MyJobCommand.LastRequest.GetBackgroundJob();
        Assert.That(job.RequestType, Is.EqualTo(CommandResult.Command));
        Assert.That(job.Id, Is.EqualTo(0));
        job.Id = int.MaxValue;
        AssertNotNulls(job);
        Assert.That(job.Status, Is.EqualTo("Finished"));
        Assert.That(ExecUtils.WaitUntilTrue(() => job.Progress >= 1), "job.Progress != 1");
        Assert.That(job.Logs, Is.EqualTo("MyCommand Started...\nMyCommand Finished"));
        Assert.That(responses.Count, Is.EqualTo(1));
        Assert.That(errors.Count, Is.EqualTo(0));

        job = feature.Jobs.RunCommand<AlwaysFailCommand>(new()
        {
            Worker = Workers.AppDb,
            OnSuccess = r => responses.Add((MyResponse)r!),
            OnFailed = e => errors.Add(e),
        });
        Assert.That(ExecUtils.WaitUntilTrue(() => errors.Count == 1), "errors.Count != 1");
    }

    [Test]
    public async Task Does_Run_Transient_Command_with_Async_callback()
    {
        ResetState();
        List<MyResponse> responses = new();
        List<Exception> errors = new();
        var response = await feature.Jobs.RunCommandAsync<MyJobCommand>(new MyRequest { Id = 1 }, new() {
            Worker = Workers.AppDb,
            OnSuccess = r => responses.Add((MyResponse)r!),
            OnFailed = e => errors.Add(e),
        });
        
        var myResponse = response as MyResponse;
        Assert.That(myResponse, Is.Not.Null);
        Assert.That(myResponse!.Result, Is.EqualTo("Hello 1"));
        Assert.That(responses.Count, Is.EqualTo(1));
        Assert.That(errors.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task Does_Run_Transient_Command_with_Async_callback_Exception()
    {
        ResetState();
        List<MyResponse> responses = new();
        List<Exception> errors = new();
        try
        {
            await feature.Jobs.RunCommandAsync<MyJobCommand>(new MyRequest { Throw = "Throw 1" }, new() {
                Worker = Workers.AppDb,
                OnSuccess = r => responses.Add((MyResponse)r!),
                OnFailed = e => errors.Add(e),
            });
            Assert.Fail("Should throw");
        }
        catch (Exception e)
        {
            Assert.That(e.Message, Is.EqualTo("Throw 1"));
        }
        Assert.That(responses.Count, Is.EqualTo(0));
        Assert.That(errors.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task Does_Run_Transient_Command_with_Async_callback_Timeout()
    {
        ResetState();
        List<MyResponse> responses = new();
        List<Exception> errors = new();
        try
        {
            await feature.Jobs.RunCommandAsync<MyJobCommand>(new MyRequest { WaitMs = 2000 }, new() {
                Worker = Workers.AppDb,
                OnSuccess = r => responses.Add((MyResponse)r!),
                OnFailed = e => errors.Add(e),
                TimeoutSecs = 1,
            });
            Assert.Fail("Should throw");
        }
        catch (TaskCanceledException) {}
        Assert.That(responses.Count, Is.EqualTo(0));
        Assert.That(errors.Count, Is.EqualTo(1));
    }

    [Test]
    public void Does_execute_MyCommand_with_options()
    {
        ResetState();
        var refId = Guid.NewGuid().ToString("N");
        feature.Jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = 1 }, new() {
            RefId = refId,
            Worker = "worker",
            Callback = nameof(MyJobCallback),
            ReplyTo = "replyTo",
            Tag = "tag",
            CreatedBy = "createdBy",
            TimeoutSecs = 60,
            ParentId = 1,
            Args = new() { ["key"] = "value" },
        });
        Assert.That(ExecUtils.WaitUntilTrue(() => MyJobCommand.LastRequest != null), "LastRequest == null");
        
        Assert.That(MyJobCommand.LastRequest, Is.Not.Null);
        Assert.That(MyJobCommand.Requests.Count, Is.GreaterThan(0));
        var job = MyJobCommand.LastRequest.GetBackgroundJob();
        Assert.That(job!.RequestType, Is.EqualTo(CommandResult.Command));
        AssertNotNulls(job!);
        Assert.That(job.RefId, Is.EqualTo(refId));
        Assert.That(job.Worker, Is.EqualTo("worker"));
        Assert.That(job.Callback, Is.EqualTo(nameof(MyJobCallback)));
        Assert.That(job.ReplyTo, Is.EqualTo("replyTo"));
        Assert.That(job.Tag, Is.EqualTo("tag"));
        Assert.That(job.CreatedBy, Is.EqualTo("createdBy"));
        Assert.That(job.TimeoutSecs, Is.EqualTo(60));
        Assert.That(job.ParentId, Is.EqualTo(1));
        Assert.That(job.Args, Is.EquivalentTo(new Dictionary<string,string>() { ["key"] = "value" }));

        Assert.That(ExecUtils.WaitUntilTrue(() => job.Status == "Finished"), "Status != Finished");
        Assert.That(ExecUtils.WaitUntilTrue(() => job.Progress >= 1), "job.Progress != 1");
        Assert.That(job.Logs, Is.EqualTo("MyCommand Started...\nMyCommand Finished"));

        Assert.That(ExecUtils.WaitUntilTrue(() => job.NotifiedDate != null), "job.NotifiedDate == null");
        Assert.That(ExecUtils.WaitUntilTrue(() => MyJobCallback.LastRequest != null), "MyCallback.LastRequest == null");
        Assert.That(MyJobCallback.LastMyResponse, Is.Not.Null);
        Assert.That(MyJobCallback.LastRequest, Is.Not.Null);
        Assert.That(MyJobCallback.LastRequest.GetBackgroundJob(), Is.Not.Null);
        Assert.That(job.Response, Is.EqualTo(MyJobCallback.LastMyResponse!.GetType().Name));
        Assert.That(job.ResponseBody, Is.EqualTo(ClientConfig.ToJson(MyJobCallback.LastMyResponse)));

        using var db = feature.OpenDb();
        Assert.That(ExecUtils.WaitUntilTrue(() => db.SingleById<BackgroundJob>(job.Id) == null), "job != null");
        var dbJobSummary = db.SingleById<JobSummary>(job.Id);
        Assert.That(dbJobSummary, Is.Not.Null);
        Assert.That(dbJobSummary.CompletedDate, Is.Not.Null);
        Assert.That(dbJobSummary.Response, Is.EqualTo(job.Response));
        using var monthDb = feature.OpenMonthDb(job.CreatedDate);
        
        var dbCompletedJob = monthDb.SingleById<CompletedJob>(job.Id);
        Assert.That(dbCompletedJob.CompletedDate, Is.Not.Null);
        Assert.That(dbCompletedJob.NotifiedDate, Is.Not.Null);
        Assert.That(dbCompletedJob.Response, Is.EqualTo(job.Response));
        Assert.That(dbCompletedJob.ResponseBody, Is.EqualTo(job.ResponseBody));
    }

    [Test]
    public void Does_execute_Multiple_MyCommand_10_Jobs()
    {
        ResetState();
        for (var i = 0; i < 10; i++)
        {
            feature.Jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = i });
        }
        ExecUtils.WaitUntilTrue(() => MyJobCommand.Count >= 10);
        Assert.That(MyJobCommand.Count, Is.EqualTo(10));
    }

    [Test]
    public void Does_execute_Multiple_MyCommand_10_Jobs_with_Callbacks()
    {
        ResetState();
        for (var i = 0; i < 10; i++)
        {
            feature.Jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = i }, new() {
                Callback = nameof(MyJobCallback),
            });
        }
        ExecUtils.WaitUntilTrue(() => MyJobCommand.Count >= 10);
        Assert.That(MyJobCommand.Count, Is.EqualTo(10));
        ExecUtils.WaitUntilTrue(() => MyJobCallback.Count >= 10);
        Assert.That(MyJobCallback.Count, Is.EqualTo(10));
    }

    [Test]
    public void Does_execute_dependent_Jobs()
    {
        ResetState();
        var jobRef = feature.Jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = 1 });

        var depJob1 = feature.Jobs.EnqueueCommand<DependentJobCommand>(
            new DependentJob { Id = 1 }, new() {
                DependsOn = jobRef.Id,
                Callback = nameof(DependentJobCallbackCommand),
            });
        
        Thread.Sleep(2000);
        
        var depJob2 = feature.Jobs.EnqueueCommand<DependentJobCommand>(
            new DependentJob { Id = 2 }, new() {
                DependsOn = jobRef.Id,
                Callback = nameof(DependentJobCallbackCommand),
            });
     
        Assert.That(ExecUtils.WaitUntilTrue(() => DependentJobCallbackCommand.Results.Count == 2), "Count != 2");
        Assert.That(DependentJobCallbackCommand.Results.Map(x => x.Id), 
            Is.EquivalentTo(new[] { 1, 2 }));
        Assert.That(DependentJobCallbackCommand.Results.All(x => x.ParentJob!.Id == jobRef.Id));
    }

    [Test]
    public void Does_execute_RunAfter_Jobs()
    {
        ResetState();
        var now = DateTime.UtcNow;
        var cmdRef1 = feature.Jobs.ScheduleCommand<MyJobCommand>(new MyRequest { Id = 1 }, now.AddSeconds(1));
        var cmdRef2 = feature.Jobs.ScheduleCommand<MyJobCommand>(new MyRequest { Id = 2 }, TimeSpan.FromSeconds(1));
        var cmdRef3 = feature.Jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = 3 });

        var apiRef1 = feature.Jobs.ScheduleApi(new MyRequest { Id = 1 }, now.AddSeconds(1));
        var apiRef2 = feature.Jobs.ScheduleApi(new MyRequest { Id = 2 }, TimeSpan.FromSeconds(1));
        var apiRef3 = feature.Jobs.EnqueueApi(new MyRequest { Id = 3 });

        Assert.That(ExecUtils.WaitUntilTrue(() => MyJobCommand.Requests.Count == 3), "Count != 3");
        Assert.That(MyJobCommand.Requests[0].Id, Is.EqualTo(3));
        Assert.That(MyJobCommand.Requests[1].Id, Is.EqualTo(1).Or.EqualTo(2));
        Assert.That(MyJobCommand.Requests[2].Id, Is.EqualTo(2).Or.EqualTo(1));
        
        Assert.That(ExecUtils.WaitUntilTrue(() => JobServices.Requests.Count == 3), "Count != 3");
        Assert.That(JobServices.Requests[0].Id, Is.EqualTo(3));
        Assert.That(JobServices.Requests[1].Id, Is.EqualTo(1).Or.EqualTo(2));
        Assert.That(JobServices.Requests[2].Id, Is.EqualTo(2).Or.EqualTo(1));
    }

    [Test]
    public async Task Does_retry_and_fail_AlwaysFailCommand()
    {
        ResetState();
        var timeout = TimeSpan.FromMinutes(1);
        var callbackCount = 0;
        Exception? callbackEx = null;
        var jobRef = feature.Jobs.EnqueueCommand<AlwaysFailCommand>(new() {
            OnFailed = ex => {
                callbackCount++;
                callbackEx = ex;
            }
        });
        Assert.That(await ExecUtils.WaitUntilTrueAsync(() => AlwaysFailCommand.Count >= 3, timeout), "AlwaysFailCommand.Count < 3");
        Assert.That(AlwaysFailCommand.Count, Is.EqualTo(3));

        using var db = feature.OpenDb();
        
        using var monthDb = feature.OpenMonthDb(DateTime.UtcNow);

        FailedJob? failedJob = null;
        Assert.That(await ExecUtils.WaitUntilTrueAsync(() => {
            failedJob = monthDb.SingleById<FailedJob>(jobRef.Id);
            return failedJob != null;
        }, timeout), "failedJob == null");
        Assert.That(failedJob!.RefId, Is.EqualTo(jobRef.RefId));
        Assert.That(failedJob.Command, Is.EqualTo(nameof(AlwaysFailCommand)));
        Assert.That(failedJob.Request, Is.EqualTo(nameof(NoArgs)));
        Assert.That(failedJob.RequestBody, Is.EqualTo("{}"));
        Assert.That(failedJob.Attempts, Is.EqualTo(3));
        Assert.That(failedJob.State, Is.EqualTo(BackgroundJobState.Failed));
        Assert.That(failedJob.ErrorCode, Is.EqualTo(nameof(Exception)));
        Assert.That(failedJob.Error!.Message, Is.EqualTo("Always Fails: 3"));

        var summary = db.SingleById<JobSummary>(jobRef.Id);
        Assert.That(summary!.RefId, Is.EqualTo(jobRef.RefId));
        Assert.That(summary.Request, Is.EqualTo(nameof(NoArgs)));
        Assert.That(summary.Attempts, Is.EqualTo(3));
        Assert.That(summary.State, Is.EqualTo(BackgroundJobState.Failed));
        Assert.That(summary.ErrorCode, Is.EqualTo(nameof(Exception)));
        Assert.That(summary.ErrorMessage, Is.EqualTo("Always Fails: 3"));
        
        Assert.That(callbackCount, Is.EqualTo(1));
        // Only first exception is executed
        Assert.That(callbackEx!.Message, Is.EqualTo("Always Fails: 1"));
    }

    [Test]
    public async Task Does_retry_and_fail_AlwaysFails_Api()
    {
        ResetState();
        var timeout = TimeSpan.FromMinutes(1);
        var callbackCount = 0;
        Exception? callbackEx = null;
        var jobRef = feature.Jobs.EnqueueApi(new AlwaysFails(), new() {
            OnFailed = ex => {
                callbackCount++;
                callbackEx = ex;
            }
        });
        Assert.That(await ExecUtils.WaitUntilTrueAsync(() => JobServices.Count >= 3, timeout), "JobServices.Count < 3");
        Assert.That(JobServices.Count, Is.EqualTo(3));

        using var db = feature.OpenDb();
        
        using var monthDb = feature.OpenMonthDb(DateTime.UtcNow);

        FailedJob? failedJob = null;
        Assert.That(await ExecUtils.WaitUntilTrueAsync(() => {
            failedJob = monthDb.SingleById<FailedJob>(jobRef.Id);
            return failedJob != null;
        }, timeout), "failedJob == null");
        Assert.That(failedJob!.RefId, Is.EqualTo(jobRef.RefId));
        Assert.That(failedJob.Command, Is.Null);
        Assert.That(failedJob.Request, Is.EqualTo(nameof(AlwaysFails)));
        Assert.That(failedJob.RequestBody, Is.EqualTo("{}"));
        Assert.That(failedJob.Attempts, Is.EqualTo(3));
        Assert.That(failedJob.State, Is.EqualTo(BackgroundJobState.Failed));
        Assert.That(failedJob.ErrorCode, Is.EqualTo(nameof(Exception)));
        Assert.That(failedJob.Error!.Message, Is.EqualTo("Always Fails: 3"));

        var summary = db.SingleById<JobSummary>(jobRef.Id);
        Assert.That(summary!.RefId, Is.EqualTo(jobRef.RefId));
        Assert.That(summary.Request, Is.EqualTo(nameof(AlwaysFails)));
        Assert.That(summary.Attempts, Is.EqualTo(3));
        Assert.That(summary.State, Is.EqualTo(BackgroundJobState.Failed));
        Assert.That(summary.ErrorCode, Is.EqualTo(nameof(Exception)));
        Assert.That(summary.ErrorMessage, Is.EqualTo("Always Fails: 3"));
        
        Assert.That(callbackCount, Is.EqualTo(1));
        // Only first exception is executed
        Assert.That(callbackEx!.Message, Is.EqualTo("Always Fails: 1"));
    }

    [Test]
    public void Does_execute_job_with_User_Context_Command()
    {
        ResetState();
        
        using var db = feature.DbFactory.OpenDbConnection();
        var testUser = IdentityUsers.GetByUserName(db, "manager@email.com")!;
        ApplicationUser? callbackResponse = null;

        var jobRef = feature.Jobs.EnqueueCommand<MyScopedCommand>(new ScopedRequest { UserId = testUser.Id }, new() {
            UserId = testUser.Id,
            OnSuccess = res => callbackResponse = (ApplicationUser)res,
        });

        var jobResult = feature.Jobs.GetJob(jobRef.Id);
        Assert.That(jobResult?.Summary, Is.Not.Null);
        Assert.That(jobResult!.Queued, Is.Not.Null);

        Assert.That(ExecUtils.WaitUntilTrue(() => MyScopedCommand.LastRequest != null), "LastRequest == null");
        Assert.That(MyScopedCommand.Requests.Count, Is.GreaterThan(0));

        var job = MyScopedCommand.LastRequest.GetBackgroundJob();
        Assert.That(job.RequestType, Is.EqualTo(CommandResult.Command));
        Assert.That(job.Request, Is.EqualTo(nameof(ScopedRequest)));

        var principal = MyScopedCommand.LastUser!;
        Assert.That(principal, Is.Not.Null);
        Assert.That(principal.GetUserId(), Is.EqualTo(testUser.Id));
        Assert.That(principal.GetUserName(), Is.EqualTo(testUser.UserName));
        Assert.That(principal.GetRoles(), Is.EquivalentTo(new[] { "Manager", "Employee" }));
        
        var session = MyScopedCommand.LastSession!;
        Assert.That(session, Is.Not.Null);
        Assert.That(session.IsAuthenticated);
        Assert.That(session.UserAuthId, Is.EqualTo(testUser.Id));
        Assert.That(session.UserAuthName, Is.EqualTo(testUser.UserName));
        Assert.That(session.Roles, Is.EquivalentTo(new[] { "Manager", "Employee" }));

        var user = MyScopedCommand.LastResult!;
        Assert.That(user.Id, Is.EqualTo(testUser.Id));
        Assert.That(user.UserName, Is.EqualTo(testUser.UserName));
        Assert.That(user, Is.EqualTo(callbackResponse));

        Assert.That(ExecUtils.WaitUntilTrue(() =>
        {
            jobResult = feature.Jobs.GetJob(jobRef.Id);
            return jobResult?.Completed != null;
        }), "jobResult.Completed == null");
        Assert.That(jobResult!.Summary, Is.Not.Null);
        Assert.That(jobResult!.Completed, Is.Not.Null);
        
        var request = feature.Jobs.CreateRequest(jobResult) as ScopedRequest;
        Assert.That(request, Is.Not.Null);
        Assert.That(request!.UserId, Is.EqualTo(testUser.Id));

        var response = feature.Jobs.CreateResponse(jobResult) as ApplicationUser;
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Id, Is.EqualTo(testUser.Id));
        Assert.That(response.UserName, Is.EqualTo(testUser.UserName));
    }

    [Test]
    public void Does_execute_job_with_User_Context_Api()
    {
        ResetState();
        
        using var db = feature.DbFactory.OpenDbConnection();
        var testUser = IdentityUsers.GetByUserName(db, "manager@email.com")!;
        ApplicationUser? callbackResponse = null;
        
        var jobRef = feature.Jobs.EnqueueApi(new ScopedRequest { UserId = testUser.Id }, new() {
            UserId = testUser.Id,
            OnSuccess = res => callbackResponse = (ApplicationUser)res,
        });

        var jobResult = feature.Jobs.GetJob(jobRef.Id);
        Assert.That(jobResult?.Summary, Is.Not.Null);
        Assert.That(jobResult!.Queued, Is.Not.Null);
        
        Assert.That(ExecUtils.WaitUntilTrue(() => JobScopedServices.LastRequest != null), "LastRequest == null");
        Assert.That(JobScopedServices.Requests.Count, Is.GreaterThan(0));

        var job = JobScopedServices.LastRequest.GetBackgroundJob();
        Assert.That(job.RequestType, Is.EqualTo(CommandResult.Api));
        Assert.That(job.Request, Is.EqualTo(nameof(ScopedRequest)));
        
        Assert.That(ExecUtils.WaitUntilTrue(() => JobScopedServices.LastResult != null), "LastResult == null");

        var principal = JobScopedServices.LastUser!;
        Assert.That(principal, Is.Not.Null);
        Assert.That(principal.GetUserId(), Is.EqualTo(testUser.Id));
        Assert.That(principal.GetUserName(), Is.EqualTo(testUser.UserName));
        Assert.That(principal.GetRoles(), Is.EquivalentTo(new[] { "Manager", "Employee" }));
        
        var session = JobScopedServices.LastSession!;
        Assert.That(session, Is.Not.Null);
        Assert.That(session.IsAuthenticated);
        Assert.That(session.UserAuthId, Is.EqualTo(testUser.Id));
        Assert.That(session.UserAuthName, Is.EqualTo(testUser.UserName));
        Assert.That(session.Roles, Is.EquivalentTo(new[] { "Manager", "Employee" }));

        var user = JobScopedServices.LastResult!;
        Assert.That(user.Id, Is.EqualTo(testUser.Id));
        Assert.That(user.UserName, Is.EqualTo(testUser.UserName));
        Assert.That(user, Is.EqualTo(callbackResponse));

        Assert.That(ExecUtils.WaitUntilTrue(() =>
        {
            jobResult = feature.Jobs.GetJob(jobRef.Id);
            return jobResult?.Completed != null;
        }), "jobResult.Completed == null");
        Assert.That(jobResult!.Summary, Is.Not.Null);
        Assert.That(jobResult!.Completed, Is.Not.Null);
        
        var request = feature.Jobs.CreateRequest(jobResult) as ScopedRequest;
        Assert.That(request, Is.Not.Null);
        Assert.That(request!.UserId, Is.EqualTo(testUser.Id));

        var response = feature.Jobs.CreateResponse(jobResult) as ApplicationUser;
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Id, Is.EqualTo(testUser.Id));
        Assert.That(response.UserName, Is.EqualTo(testUser.UserName));
    }


    [Test]
    public async Task Can_Schedule_Reoccurring_Command()
    {
        ResetState();

        using var db = feature.Jobs.OpenDb();
        var taskName = "My Command Every Minute";
        var options = new BackgroundJobOptions { Tag = "test" };
        var schedule = Schedule.EveryMinute;
        schedule.TimeZoneId = TimeZoneInfo.Utc.Id;
        schedule.MisfirePolicy = ScheduleMisfirePolicy.Skip;
        schedule.OverlapPolicy = ScheduleOverlapPolicy.Skip;
        var startedAt = DateTime.UtcNow;

        ScheduledTask AssertTask(ScheduledTask task)
        {
            Assert.That(task.Id, Is.GreaterThan(0));
            Assert.That(task.Name, Is.EqualTo(taskName));
            Assert.That(task.RequestType, Is.EqualTo(CommandResult.Command));
            Assert.That(task.Request, Is.EqualTo(nameof(NoArgs)));
            Assert.That(task.RequestBody, Is.EqualTo("{}"));
            Assert.That(task.Interval, Is.Null);
            Assert.That(task.CronExpression, Is.EqualTo("* * * * *"));
            Assert.That(task.Enabled, Is.True);
            Assert.That(task.NextRun, Is.Not.Null);
            Assert.That(task.TimeZoneId, Is.EqualTo(TimeZoneInfo.Utc.Id));
            Assert.That(task.MisfirePolicy, Is.EqualTo(ScheduleMisfirePolicy.Skip));
            Assert.That(task.OverlapPolicy, Is.EqualTo(ScheduleOverlapPolicy.Skip));
            return task;
        }

        feature.Jobs.RecurringCommand<MySyncCommand>(taskName, schedule, options);

        var tasks = await db.SelectAsync<ScheduledTask>();
        Assert.That(tasks.Count, Is.EqualTo(1));
        var task = AssertTask(tasks[0]);
        Assert.That(task.Options, Is.Not.Null);
        
        feature.Jobs.RecurringCommand<MySyncCommand>(taskName, schedule);
        tasks = await db.SelectAsync<ScheduledTask>();
        Assert.That(tasks.Count, Is.EqualTo(1));
        task = AssertTask(tasks[0]);
        Assert.That(task.Options, Is.Null);

        // First Scheduled Task should execute immediately
        Assert.That(await ExecUtils.WaitUntilTrueAsync(() => MySyncCommand.Count == 1), "Count != 1");
        var job = MySyncCommand.LastRequest.GetBackgroundJob();
        Assert.That(job.RequestType, Is.EqualTo(CommandResult.Command));
        AssertNotNulls(job);
        Assert.That(await ExecUtils.WaitUntilTrueAsync(() => job.Status == "Finished"), "Status != Finished");
        Assert.That(await ExecUtils.WaitUntilTrueAsync(() => job.Progress >= 1), "job.Progress != 1");
        Assert.That(job.Logs, Is.EqualTo("MyCommand Started...\nMyCommand Finished"));
        
        task = await db.SingleAsync<ScheduledTask>(x => x.Name == taskName);
        Assert.That(task.LastRun, Is.GreaterThan(startedAt));
        Assert.That(task.LastJobId, Is.EqualTo(job.Id));
        Assert.That(job.RefId, Does.StartWith($"scheduled:{task.Id}:"));

        Assert.That(feature.Jobs.SetRecurringTaskEnabled(taskName, false), Is.True);
        task = await db.SingleAsync<ScheduledTask>(x => x.Name == taskName);
        Assert.That(task.Enabled, Is.False);
        Assert.That(task.NextRun, Is.Null);

        Assert.That(feature.Jobs.SetRecurringTaskEnabled(taskName, true), Is.True);
        task = await db.SingleAsync<ScheduledTask>(x => x.Name == taskName);
        Assert.That(task.Enabled, Is.True);
        Assert.That(task.NextRun, Is.Not.Null);
        Assert.That(feature.Jobs.SetRecurringTaskEnabled(taskName, false), Is.True);
    }

    [Test]
    public async Task Can_Schedule_Reoccurring_Api()
    {
        ResetState();

        using var db = feature.Jobs.OpenDb();
        var taskName = "My API Every Minute";
        var options = new BackgroundJobOptions { Tag = "test" };
        var startedAt = DateTime.UtcNow;

        ScheduledTask AssertTask(ScheduledTask task, MyRequest request)
        {
            Assert.That(task.Id, Is.GreaterThan(0));
            Assert.That(task.Name, Is.EqualTo(taskName));
            Assert.That(task.RequestType, Is.EqualTo(CommandResult.Api));
            Assert.That(task.Request, Is.EqualTo(nameof(MyRequest)));
            Assert.That(task.RequestBody, Is.EqualTo(ClientConfig.ToJson(request)));
            Assert.That(task.Interval, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(task.CronExpression, Is.Null);
            Assert.That(task.Enabled, Is.True);
            Assert.That(task.NextRun, Is.Not.Null);
            return task;
        }

        feature.Jobs.RecurringApi(taskName, Schedule.Interval(TimeSpan.FromSeconds(1)), 
            new MyRequest { Id = 1 }, options);

        var tasks = await db.SelectAsync<ScheduledTask>();
        Assert.That(tasks.Count, Is.EqualTo(1));
        var task = AssertTask(tasks[0], new MyRequest { Id = 1 });
        Assert.That(task.Options, Is.Not.Null);
        
        feature.Jobs.RecurringApi(taskName, Schedule.Interval(TimeSpan.FromSeconds(1)), new MyRequest { Id = 2 });
        tasks = await db.SelectAsync<ScheduledTask>();
        Assert.That(tasks.Count, Is.EqualTo(1));
        task = AssertTask(tasks[0], new MyRequest { Id = 2 });
        Assert.That(task.Options, Is.Null);

        // First Scheduled Task should execute immediately
        Assert.That(await ExecUtils.WaitUntilTrueAsync(() => JobServices.LastRequest != null), "LastRequest == null");
        Assert.That(JobServices.Requests.Count, Is.GreaterThanOrEqualTo(1));
        var job = JobServices.LastRequest.GetBackgroundJob();
        Assert.That(job.RequestType, Is.EqualTo(CommandResult.Api));
        AssertNotNulls(job);
        Assert.That(await ExecUtils.WaitUntilTrueAsync(() => job.Status == "Finished"), "Status != Finished");
        Assert.That(await ExecUtils.WaitUntilTrueAsync(() => job.Progress >= 1), "job.Progress != 1");
        Assert.That(job.Logs, Is.EqualTo("My API Started...\nMy API Finished"));
        
        task = await db.SingleAsync<ScheduledTask>(x => x.Name == taskName);
        Assert.That(task.LastRun, Is.GreaterThan(startedAt));
        Assert.That(task.LastJobId, Is.EqualTo(job.Id));
        Assert.That(job.RefId, Does.StartWith($"scheduled:{task.Id}:"));
    }

    [Test]
    public void GetTableMonths_Handles_NonExistent_Directory()
    {
        var dummyFeature = new BackgroundsJobFeature
        {
            DbDir = Path.Combine(Path.GetTempPath(), "jobs_nonexistent_" + Guid.NewGuid().ToString("N"))
        };
        using var db = feature.Jobs.OpenDb();
        var months = dummyFeature.GetTableMonths(db);
        Assert.That(months, Is.Not.Null);
        Assert.That(months, Is.Empty);
    }

    [Test]
    public void SqliteRequestLogger_GetAnalyticInfo_Tabs_Correctly_Identified()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "requests_" + Guid.NewGuid().ToString("N"));
        try
        {
            var logger = new SqliteRequestLogger
            {
                DbDir = tempDir,
                AppHost = appHost,
                DbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider),
                AutoInitSchema = true,
            };
            logger.Register(appHost);

            using var db = logger.OpenMonthDb(DateTime.UtcNow);
            // Insert only an IP entry (no operation name, user, or api key)
            db.Insert(new RequestLog
            {
                Id = 1,
                DateTime = DateTime.UtcNow,
                IpAddress = "127.0.0.1",
            });

            var info = logger.GetAnalyticInfo(new AnalyticsConfig());
            Assert.That(info.Tabs.ContainsKey("IP Addresses"), Is.True);
            Assert.That(info.Tabs.ContainsKey("APIs"), Is.False);
            Assert.That(info.Tabs.ContainsKey("Users"), Is.False);
            Assert.That(info.Tabs.ContainsKey("API Keys"), Is.False);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch {}
        }
    }

    [Test]
    public void SqliteRequestLogger_GetIpAnalytics_Caches_When_Only_Ip_Exists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "requests_" + Guid.NewGuid().ToString("N"));
        try
        {
            var logger = new SqliteRequestLogger
            {
                DbDir = tempDir,
                AppHost = appHost,
                DbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider),
                AutoInitSchema = true,
            };
            logger.Register(appHost);

            using var db = logger.OpenMonthDb(DateTime.UtcNow);
            db.Insert(new RequestLog
            {
                Id = 1,
                DateTime = DateTime.UtcNow,
                IpAddress = "192.168.1.100",
                OperationName = "TestOp",
                StatusCode = 200,
                RequestDuration = TimeSpan.FromMilliseconds(50),
            });

            var report = logger.GetIpAnalytics(new AnalyticsConfig(), DateTime.UtcNow, "192.168.1.100");
            Assert.That(report, Is.Not.Null);

            // Verify it was cached in the database table IpAnalytics
            var cached = db.Single<IpAnalytics>(x => x.Ip == "192.168.1.100");
            Assert.That(cached, Is.Not.Null);
            Assert.That(cached.Ip, Is.EqualTo("192.168.1.100"));
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch {}
        }
    }

    [Test]
    public async Task Concurrent_EnqueueCommand_Is_ThreadSafe()
    {
        ResetState();
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        var tasks = Enumerable.Range(0, 20).Select(i => Task.Run(() =>
        {
            try
            {
                feature.Jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = i });
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        await Task.WhenAll(tasks);
        Assert.That(exceptions, Is.Empty);
    }

    [Test]
    public void RetryBackoff_Uses_Bounded_Exponential_Jitter()
    {
        // Equal jitter: never less than half the calculated delay, so a retry is never immediate
        var job = new BackgroundJob { Attempts = 3, RetryBackoff = RetryBackoff.ExponentialJitter };
        Assert.That(JobUtils.GetRetryDelay(job, RetryBackoff.Fixed, 5_000, 30_000, 0).TotalMilliseconds,
            Is.EqualTo(10_000));
        Assert.That(JobUtils.GetRetryDelay(job, RetryBackoff.Fixed, 5_000, 30_000, 0.5).TotalMilliseconds,
            Is.EqualTo(15_000));
        Assert.That(JobUtils.GetRetryDelay(job, RetryBackoff.Fixed, 5_000, 30_000, 1).TotalMilliseconds,
            Is.EqualTo(20_000));
        job.Attempts = 20;
        Assert.That(JobUtils.GetRetryDelay(job, RetryBackoff.Fixed, 5_000, 30_000, 1).TotalMilliseconds,
            Is.EqualTo(30_000));
        Assert.That(JobUtils.GetRetryDelay(job, RetryBackoff.Fixed, 5_000, 30_000, 0).TotalMilliseconds,
            Is.EqualTo(15_000));
    }

    [Test]
    public void SingletonKey_Returns_Active_Job_Instead_Of_Queueing_A_Duplicate()
    {
        ResetState();
        var options = new BackgroundJobOptions { SingletonKey = "nightly-import" };

        var first = feature.Jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = 1 }, options);
        var second = feature.Jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = 2 }, options);

        Assert.That(second.Id, Is.EqualTo(first.Id));
        using var db = feature.Jobs.OpenDb();
        Assert.That(db.Count<BackgroundJob>(x => x.SingletonKey == options.SingletonKey), Is.EqualTo(1));
    }

    [Test]
    public void Jobs_Default_To_The_Default_Queue_With_Zero_Priority()
    {
        ResetState();
        var jobRef = feature.Jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = 1 });

        using var db = feature.Jobs.OpenDb();
        var summary = db.SingleById<JobSummary>(jobRef.Id);
        Assert.That(summary.Queue, Is.EqualTo(JobQueues.Default));
        Assert.That(summary.Priority, Is.EqualTo(0));
    }

    [Test]
    public void Scheduled_RefIds_Round_Trip_Their_Task_Id()
    {
        var occurrence = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var refId = JobUtils.CreateScheduledRefId(42, occurrence);

        Assert.That(JobUtils.TryGetScheduledTaskId(refId, out var taskId), Is.True);
        Assert.That(taskId, Is.EqualTo(42));
        Assert.That(JobUtils.TryGetScheduledTaskId(JobUtils.CreateScheduledSingletonKey(42), out taskId), Is.True);
        Assert.That(taskId, Is.EqualTo(42));
        Assert.That(JobUtils.TryGetScheduledTaskId("customer-order-42", out _), Is.False);
        Assert.That(JobUtils.TryGetScheduledTaskId(null, out _), Is.False);
    }

    [Test]
    public void Idempotent_Enqueue_Returns_Existing_Job()
    {
        ResetState();
        var options = new BackgroundJobOptions
        {
            RefId = "customer-order-42",
            DuplicateRefIdBehavior = DuplicateRefIdBehavior.ReturnExisting,
        };

        var first = feature.Jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = 42 }, options);
        var second = feature.Jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = 42 }, options);

        Assert.That(second.Id, Is.EqualTo(first.Id));
        Assert.That(second.RefId, Is.EqualTo(first.RefId));
        using var db = feature.Jobs.OpenDb();
        Assert.That(db.Count<JobSummary>(x => x.RefId == options.RefId), Is.EqualTo(1));
    }

    [Test]
    public void Additive_Jobs_Upgrade_Preserves_History_And_Clears_Incomplete_Queue()
    {
        using var db = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider).OpenDbConnection();
        db.ExecuteSql("CREATE TABLE BackgroundJob (Id INTEGER PRIMARY KEY)");
        db.ExecuteSql("CREATE TABLE JobSummary (Id INTEGER PRIMARY KEY, State TEXT, CompletedDate TEXT, ErrorCode TEXT, ErrorMessage TEXT)");
        db.ExecuteSql("CREATE TABLE ScheduledTask (Id INTEGER PRIMARY KEY, Name TEXT)");
        db.ExecuteSql("INSERT INTO BackgroundJob (Id) VALUES (42)");
        db.ExecuteSql("INSERT INTO JobSummary (Id, State) VALUES (42, 'Queued'), (43, 'Completed')");
        db.ExecuteSql("INSERT INTO ScheduledTask (Id, Name) VALUES (1, 'existing')");

        BackgroundJobSchema.UpgradeMainDb(db);
        BackgroundJobSchema.UpgradeMainDb(db); // startup upgrade is idempotent

        Assert.That(db.SqlScalar<long>("SELECT COUNT(*) FROM BackgroundJob"), Is.Zero);
        Assert.That(db.SqlScalar<string>("SELECT ErrorCode FROM JobSummary WHERE Id=42"),
            Is.EqualTo("QueueClearedOnUpgrade"));
        Assert.That(db.SqlScalar<string>("SELECT State FROM JobSummary WHERE Id=42"),
            Is.EqualTo(nameof(BackgroundJobState.Cancelled)));
        Assert.That(db.SqlScalar<string>("SELECT State FROM JobSummary WHERE Id=43"),
            Is.EqualTo(nameof(BackgroundJobState.Completed)));
        Assert.That(db.ColumnExists<BackgroundJob>(x => x.Queue), Is.True);
        Assert.That(db.ColumnExists<JobSummary>(x => x.Priority), Is.True);
        Assert.That(db.ColumnExists<ScheduledTask>(x => x.NextRun), Is.True);
        Assert.That(db.SqlScalar<bool>("SELECT Enabled FROM ScheduledTask WHERE Id=1"), Is.True);
        Assert.That(db.SqlScalar<string>("SELECT MisfirePolicy FROM ScheduledTask WHERE Id=1"),
            Is.EqualTo(nameof(ScheduleMisfirePolicy.RunOnce)));
        Assert.That(db.SqlScalar<string>("SELECT OverlapPolicy FROM ScheduledTask WHERE Id=1"),
            Is.EqualTo(nameof(ScheduleOverlapPolicy.Allow)));
        Assert.That(db.SqlScalar<string>("SELECT Queue FROM JobSummary WHERE Id=43"),
            Is.EqualTo(JobQueues.Default));
        Assert.That(db.SqlScalar<long>("SELECT Priority FROM JobSummary WHERE Id=43"), Is.Zero);
    }

    [Test]
    public void Dashboard_Reports_Queue_Stats_And_Wait_Times()
    {
        ResetState();
        feature.Jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = 1 });
        feature.Jobs.TickAsync().Wait();

        var response = (AdminJobDashboardResponse)ExecAdmin(x => x.Any(new AdminJobDashboard()));

        // Exercises the wait-time SQL, which has no portable form across dialects
        Assert.That(response.WaitTimes, Is.Not.Null);
        Assert.That(response.WaitTimes.Count, Is.GreaterThanOrEqualTo(0));
        Assert.That(response.WaitTimes.AvgMs, Is.GreaterThanOrEqualTo(0));
        Assert.That(response.Queues, Is.Not.Null);
    }

    [Test]
    public void Job_Queues_Report_Their_Backlog()
    {
        ResetState();
        var response = (AdminGetJobQueuesResponse)ExecAdmin(x => x.Any(new AdminGetJobQueues()));

        Assert.That(response.Results, Is.Not.Empty);
        var defaultQueue = response.Results.FirstOrDefault(x => x.Name == JobQueues.Default);
        Assert.That(defaultQueue, Is.Not.Null);
        Assert.That(defaultQueue!.Concurrency, Is.GreaterThan(0));
    }

    [Test]
    public void Job_Batches_Track_Progress_And_Complete()
    {
        ResetState();
        var batchId = "import-" + Guid.NewGuid().ToString("N");
        feature.Jobs.CreateJobBatch(batchId, total:3, callback:nameof(MyJobCommand), description:"Import");

        var batch = feature.Jobs.GetJobBatch(batchId);
        Assert.That(batch, Is.Not.Null);
        Assert.That(batch!.Total, Is.EqualTo(3));
        Assert.That(batch.Description, Is.EqualTo("Import"));
        Assert.That(batch.Progress, Is.EqualTo(0));

        for (var i = 0; i < 3; i++)
        {
            feature.Jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = i }, new() { BatchId = batchId });
        }

        batch = feature.Jobs.GetJobBatch(batchId)!;
        Assert.That(batch.Queued, Is.EqualTo(3));
        Assert.That(batch.Finished, Is.EqualTo(0));
        Assert.That(batch.CompletedDate, Is.Null);

        using var db = feature.Jobs.OpenDb();
        Assert.That(db.Count<JobSummary>(x => x.BatchId == batchId), Is.EqualTo(3));
    }

    [Test]
    public void Job_Batch_Is_Complete_Once_Every_Job_Has_Finished()
    {
        ResetState();
        var batchId = "batch-" + Guid.NewGuid().ToString("N");
        var jobs = (IBackgroundJobsQueues)feature.Jobs;
        jobs.CreateJobBatch(new JobBatch { Id = batchId, Total = 2, CreatedDate = DateTime.UtcNow });

        // Simulate both Jobs finishing
        using var db = feature.Jobs.OpenDb();
        db.UpdateOnly(() => new JobBatch { Queued = 0, Completed = 2 }, where: x => x.Id == batchId);

        var batch = jobs.GetJobBatch(batchId)!;
        Assert.That(batch.Finished, Is.EqualTo(2));
        Assert.That(batch.Progress, Is.EqualTo(1));
    }

    [Test]
    public void ValidateReplyTo_rejects_a_Job_with_a_ReplyTo_that_is_not_allowed()
    {
        ResetState();
        var validate = feature.ValidateReplyTo;
        feature.ValidateReplyTo = JobReplyTo.AllowUrlPrefixes(["https://hooks.example.org/"]);
        try
        {
            Assert.Throws<ArgumentException>(() => feature.Jobs.EnqueueCommand<MyJobCommand>(
                new MyRequest { Id = 1 }, new() { ReplyTo = "http://169.254.169.254/latest" }));
        }
        finally
        {
            feature.ValidateReplyTo = validate;
        }
    }

    /// <summary>Ticks the Jobs pipeline like the hosted service until the predicate holds</summary>
    bool TickUntil(Func<bool> untilTrue, int timeoutMs = 10_000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (untilTrue())
                return true;
            feature.Jobs.TickAsync().Wait();
            Thread.Sleep(100);
        }
        return untilTrue();
    }

    [Test]
    public void OnFinished_dependents_run_after_a_failed_parent_and_failed_attempts_are_recorded()
    {
        ResetState();
        feature.Jobs.StartAsync(default);
        var parent = feature.Jobs.EnqueueCommand<AlwaysFailCommand>(new() { RetryLimit = 1, RetryDelayMs = 1 });
        var dependent = feature.Jobs.EnqueueCommand<DependentJobCommand>(new DependentJob { Id = 1 },
            new() { DependsOn = parent.Id, DependsOnPolicy = JobDependencyPolicy.OnFinished });

        Assert.That(TickUntil(() => feature.Jobs.GetJob(dependent.Id)?.Summary.State == BackgroundJobState.Completed),
            Is.True, "A dependent with the OnFinished policy runs after its parent failed");
        Assert.That(feature.Jobs.GetJob(parent.Id)!.Summary.State, Is.EqualTo(BackgroundJobState.Failed));
        Assert.That(DependentJobCommand.LastCommandRequest?.Id, Is.EqualTo(1));

        var attempts = feature.Jobs.GetJobAttempts(parent.Id);
        Assert.That(attempts.Select(x => x.Attempt), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(attempts.Last().State, Is.EqualTo(BackgroundJobState.Failed));
    }

    JobSummary? GetSummary(long jobId) => feature.Jobs.GetJob(jobId)?.Summary;
    bool IsFinished(long jobId) => GetSummary(jobId)?.State.IsFinished() == true;

    [Test]
    public void A_transiently_failing_Job_is_retried_until_it_succeeds()
    {
        ResetState();
        var jobRef = feature.Jobs.EnqueueCommand<SqliteFlakyCommand>(new MyRequest { Id = 2 }, new() {
            RetryLimit = 3,
            RetryBackoff = RetryBackoff.ExponentialJitter,
            RetryDelayMs = 1,
        });

        Assert.That(TickUntil(() => IsFinished(jobRef.Id), 15_000), Is.True);
        Assert.That(GetSummary(jobRef.Id)!.State, Is.EqualTo(BackgroundJobState.Completed));
        Assert.That(GetSummary(jobRef.Id)!.Attempts, Is.EqualTo(3));
        Assert.That(Interlocked.Read(ref SqliteFlakyCommand.Count), Is.EqualTo(3));
        Assert.That(feature.Jobs.GetJobAttempts(jobRef.Id).Select(x => x.Attempt), Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void A_running_Job_can_be_cancelled()
    {
        ResetState();
        var jobRef = feature.Jobs.EnqueueCommand<SqliteSlowCommand>(new SqliteSlowRequest { Key = "cancel", Ms = 30_000 });
        Assert.That(TickUntil(() => GetSummary(jobRef.Id)?.State == BackgroundJobState.Started), Is.True);

        Assert.That(feature.Jobs.CancelJob(jobRef.Id), Is.True);

        Assert.That(TickUntil(() => IsFinished(jobRef.Id)), Is.True);
        Assert.That(GetSummary(jobRef.Id)!.State, Is.EqualTo(BackgroundJobState.Cancelled));
        Assert.That(SqliteSlowCommand.ObservedCancellation, Is.True, "The running Job's token should be cancelled");
    }

    [Test]
    public void A_failed_workflow_step_cancels_the_rest_of_the_workflow_but_OnFinished_steps_still_run()
    {
        ResetState();
        var jobs = feature.Jobs;
        // charge -> reserve (fails) -> ship -> notify (runs however ship finished)
        var charge = jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = 1 });
        var reserve = jobs.EnqueueCommand<AlwaysFailCommand>(new() { DependsOn = charge.Id, RetryLimit = 0 });
        var ship = jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = 3 }, new() { DependsOn = reserve.Id });
        var notify = jobs.EnqueueCommand<DependentJobCommand>(new DependentJob { Id = 4 },
            new() { DependsOn = ship.Id, DependsOnPolicy = JobDependencyPolicy.OnFinished });

        Assert.That(TickUntil(() => IsFinished(notify.Id), 15_000), Is.True);
        Assert.That(GetSummary(charge.Id)!.State, Is.EqualTo(BackgroundJobState.Completed));
        Assert.That(GetSummary(reserve.Id)!.State, Is.EqualTo(BackgroundJobState.Failed));
        Assert.That(GetSummary(ship.Id)!.State, Is.EqualTo(BackgroundJobState.Cancelled));
        Assert.That(GetSummary(notify.Id)!.State, Is.EqualTo(BackgroundJobState.Completed));
        var parentJob = DependentJobCommand.LastRequest?.GetBackgroundJob().ParentJob;
        Assert.That(parentJob?.Id, Is.EqualTo(ship.Id));
        Assert.That(parentJob?.State, Is.EqualTo(BackgroundJobState.Cancelled));
    }

    [Test]
    public void A_Batch_fans_out_then_runs_its_callbacks_and_fan_in_Job_once()
    {
        ResetState();
        var jobs = feature.Jobs;
        var batchId = "gallery-" + Guid.NewGuid().ToString("N")[..8];
        jobs.CreateJobBatch(batchId, total: 3,
            callback: nameof(SqliteBatchCallbackCommand), onSuccess: nameof(SqliteBatchSuccessCommand));
        var images = Enumerable.Range(1, 3)
            .Select(i => jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = i }, new() { BatchId = batchId }))
            .ToList();
        var zip = jobs.EnqueueCommand<DependentJobCommand>(new DependentJob { Id = 99 },
            new() { DependsOnBatch = batchId });

        Assert.That(TickUntil(() => IsFinished(zip.Id)
            && Interlocked.Read(ref SqliteBatchCallbackCommand.Count) > 0
            && Interlocked.Read(ref SqliteBatchSuccessCommand.Count) > 0, 20_000), Is.True);

        Assert.That(GetSummary(zip.Id)!.State, Is.EqualTo(BackgroundJobState.Completed));
        var lastImageCompleted = images.Max(x => GetSummary(x.Id)!.CompletedDate);
        Assert.That(GetSummary(zip.Id)!.StartedDate, Is.GreaterThanOrEqualTo(lastImageCompleted),
            "The fan-in Job only starts once every Job in the batch has finished");
        Assert.That(SqliteBatchCallbackCommand.LastBatch?.Completed, Is.EqualTo(3));

        // The callbacks run exactly once
        TickUntil(() => false, 1000);
        Assert.That(Interlocked.Read(ref SqliteBatchCallbackCommand.Count), Is.EqualTo(1));
        Assert.That(Interlocked.Read(ref SqliteBatchSuccessCommand.Count), Is.EqualTo(1));
    }

    [Test]
    public void Jobs_sharing_a_ConcurrencyKey_run_one_at_a_time()
    {
        ResetState();
        var jobRefs = new List<BackgroundJobRef>();
        foreach (var tenant in new[] { "acme", "globex" })
        {
            for (var i = 0; i < 3; i++)
            {
                jobRefs.Add(feature.Jobs.EnqueueCommand<SqliteSlowCommand>(
                    new SqliteSlowRequest { Key = tenant, Ms = 400 },
                    new() { ConcurrencyKey = $"tenant:{tenant}", TenantId = tenant }));
            }
        }

        Assert.That(TickUntil(() => jobRefs.All(x => IsFinished(x.Id)), 30_000), Is.True);
        Assert.That(jobRefs.All(x => GetSummary(x.Id)!.State == BackgroundJobState.Completed), Is.True);
        Assert.That(SqliteSlowCommand.MaxByKey["acme"], Is.EqualTo(1));
        Assert.That(SqliteSlowCommand.MaxByKey["globex"], Is.EqualTo(1));
        if (feature.MaxConcurrentJobs > 1)
            Assert.That(SqliteSlowCommand.MaxOverall, Is.GreaterThan(1), "Different keys should still run in parallel");
        Assert.That(GetSummary(jobRefs[0].Id)!.TenantId, Is.EqualTo("acme"));
    }

    [Test]
    public void A_rate_limited_queue_only_starts_RateLimit_Jobs_per_window()
    {
        ResetState();
        var jobs = feature.Jobs;
        var queue = "limited-" + Guid.NewGuid().ToString("N")[..8];
        jobs.SetJobQueueRateLimit(queue, 2, TimeSpan.FromHours(1));
        var jobRefs = Enumerable.Range(1, 4)
            .Select(i => jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = i }, new() { Queue = queue }))
            .ToList();
        try
        {
            TickUntil(() => jobRefs.Count(x => IsFinished(x.Id)) >= 2);
            TickUntil(() => false, 1500);
            Assert.That(jobRefs.Count(x => GetSummary(x.Id)!.State == BackgroundJobState.Completed), Is.EqualTo(2));
        }
        finally
        {
            jobs.SetJobQueueRateLimit(queue, 0);
            jobRefs.ForEach(x => jobs.CancelJob(x.Id));
        }
    }

    [Test]
    public void A_Recurring_Task_is_disabled_once_it_reaches_MaxRuns()
    {
        ResetState();
        var taskName = "max-runs-" + Guid.NewGuid().ToString("N")[..8];
        var schedule = Schedule.Interval(TimeSpan.FromSeconds(1));
        schedule.MaxRuns = 2;
        feature.Jobs.RecurringCommand<MyJobCommand>(taskName, schedule, new MyRequest { Id = 1 });
        try
        {
            Assert.That(TickUntil(() => feature.Jobs.GetScheduledTask(taskName)?.Enabled == false, 15_000), Is.True);
            using var db = feature.OpenDb();
            var task = db.Single<ScheduledTask>(x => x.Name == taskName);
            Assert.That(task.RunCount, Is.EqualTo(2));
            Assert.That(task.NextRun, Is.Null);
        }
        finally
        {
            feature.Jobs.DeleteRecurringTask(taskName);
        }
    }

    [Test]
    public void Paused_Queues_Do_Not_Dispatch_Jobs()
    {
        ResetState();
        var queue = "paused-" + Guid.NewGuid().ToString("N")[..8];

        var jobQueue = feature.Jobs.PauseJobQueue(queue);
        Assert.That(jobQueue.Paused, Is.True);
        Assert.That(feature.Jobs.IsQueuePaused(queue), Is.True);

        var jobRef = feature.Jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = 1 },
            new() { Queue = queue });

        using var db = feature.Jobs.OpenDb();
        var job = db.SingleById<BackgroundJob>(jobRef.Id);
        // A paused queue leaves the Job unclaimed so it isn't dispatched to a Worker
        Assert.That(job.RequestId, Is.Null);
        Assert.That(job.State, Is.EqualTo(BackgroundJobState.Queued));

        feature.Jobs.ResumeJobQueue(queue);
        Assert.That(feature.Jobs.IsQueuePaused(queue), Is.False);
    }

    [Test]
    public void Queue_Concurrency_Can_Be_Overridden_At_Runtime()
    {
        ResetState();
        var queue = "throttled-" + Guid.NewGuid().ToString("N")[..8];
        var configured = feature.Jobs.GetQueueConcurrency(queue);
        Assert.That(configured, Is.EqualTo(feature.MaxConcurrentJobs));

        feature.Jobs.SetJobQueueConcurrency(queue, 1);
        Assert.That(feature.Jobs.GetQueueConcurrency(queue), Is.EqualTo(1));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            feature.Jobs.SetJobQueueConcurrency(queue, 0));
    }

    [Test]
    public void Expired_Jobs_Are_Cancelled_Instead_Of_Run_Late()
    {
        ResetState();
        // Queued in the past with a deadline that's already passed
        var jobRef = feature.Jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = 1 }, new() {
            RunAfter = DateTime.UtcNow.AddHours(1),
            ExpiresIn = TimeSpan.FromMilliseconds(1),
        });

        using var db = feature.Jobs.OpenDb();
        var job = db.SingleById<BackgroundJob>(jobRef.Id);
        Assert.That(job.ExpiresAt, Is.Not.Null);

        Thread.Sleep(20);
        feature.Jobs.TickAsync().Wait();

        var summary = db.SingleById<JobSummary>(jobRef.Id);
        Assert.That(summary.State, Is.EqualTo(BackgroundJobState.Cancelled));
        Assert.That(summary.ErrorCode, Is.EqualTo(JobErrorCodes.JobExpired));
    }

    [Test]
    public void ExpiresIn_Is_Resolved_To_An_Absolute_ExpiresAt()
    {
        var options = new BackgroundJobOptions { ExpiresIn = TimeSpan.FromMinutes(5) };
        var job = options.ToBackgroundJob(CommandResult.Command, new MyRequest());
        Assert.That(job.ExpiresAt, Is.Not.Null);
        Assert.That(job.ExpiresAt!.Value, Is.GreaterThan(DateTime.UtcNow.AddMinutes(4)));
        Assert.That(job.ExpiresAt!.Value, Is.LessThan(DateTime.UtcNow.AddMinutes(6)));

        // An explicit ExpiresAt wins over ExpiresIn
        var at = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var job2 = new BackgroundJobOptions { ExpiresAt = at, ExpiresIn = TimeSpan.FromMinutes(5) }
            .ToBackgroundJob(CommandResult.Command, new MyRequest());
        Assert.That(job2.ExpiresAt, Is.EqualTo(at));
    }

    [Test]
    public async Task WaitForJob_Returns_The_Completed_Job_Result()
    {
        ResetState();
        var jobRef = feature.Jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = 99 });

        var result = await feature.Jobs.WaitForJobAsync(jobRef, TimeSpan.FromSeconds(30));

        Assert.That(result.Summary.State.IsFinished());
        Assert.That(result.Summary.State, Is.EqualTo(BackgroundJobState.Completed));
        var response = (MyResponse)feature.Jobs.CreateResponse(result.Job!)!;
        Assert.That(response.Result, Is.EqualTo("Hello 99"));
    }

    [Test]
    public void WaitForJob_Throws_For_An_Unknown_Job()
    {
        Assert.That(async () => await feature.Jobs.WaitForJobAsync(int.MaxValue, TimeSpan.FromSeconds(1)),
            Throws.ArgumentException);
    }

    [Test]
    public void Job_States_Know_When_They_Are_Finished()
    {
        Assert.That(BackgroundJobState.Completed.IsFinished(), Is.True);
        Assert.That(BackgroundJobState.Failed.IsFinished(), Is.True);
        Assert.That(BackgroundJobState.Cancelled.IsFinished(), Is.True);
        Assert.That(BackgroundJobState.Queued.IsFinished(), Is.False);
        Assert.That(BackgroundJobState.Started.IsFinished(), Is.False);
        // Executed still has its Callback to run
        Assert.That(BackgroundJobState.Executed.IsFinished(), Is.False);
    }

    [Test]
    public void ReplyTo_Posts_To_A_Url_And_Publishes_Everything_Else_To_MQ()
    {
        var job = new BackgroundJob {
            Id = 1,
            RefId = "abc",
            State = BackgroundJobState.Completed,
            RequestType = CommandResult.Command,
            Request = nameof(MyRequest),
        };
        var ctx = new JobReplyToContext(feature.Jobs, job, "https://example.org/hook", null);
        var headers = JobReplyTo.GetHeaders(ctx);
        Assert.That(headers["X-Job-Id"], Is.EqualTo("1"));
        Assert.That(headers["X-Job-RefId"], Is.EqualTo("abc"));
        Assert.That(headers["X-Job-State"], Is.EqualTo(nameof(BackgroundJobState.Completed)));
    }

    [Test]
    public void Upgrade_Adds_Indexes_Missing_From_An_Existing_Database()
    {
        using var db = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider).OpenDbConnection();
        // Columns as they existed before this release, i.e. without any of the new columns
        db.ExecuteSql("CREATE TABLE BackgroundJob (Id INTEGER PRIMARY KEY, State TEXT, RunAfter TEXT, CreatedDate TEXT)");
        db.ExecuteSql("CREATE TABLE JobSummary (Id INTEGER PRIMARY KEY, State TEXT, CreatedDate TEXT, CompletedDate TEXT, ErrorCode TEXT, ErrorMessage TEXT)");
        db.ExecuteSql("CREATE TABLE ScheduledTask (Id INTEGER PRIMARY KEY, Name TEXT)");

        BackgroundJobSchema.UpgradeMainDb(db);
        BackgroundJobSchema.UpgradeMainDb(db); // creating indexes is idempotent

        var indexes = db.Column<string>("SELECT name FROM sqlite_master WHERE type='index'");
        Assert.That(indexes, Does.Contain("idx_backgroundjob_claim"));
        Assert.That(indexes, Does.Contain("idx_backgroundjob_lease"));
        Assert.That(indexes, Does.Contain("idx_backgroundjob_owner"));
        Assert.That(indexes, Does.Contain("uidx_backgroundjob_singleton"));
        Assert.That(indexes, Does.Contain("idx_jobsummary_state"));
        Assert.That(indexes, Does.Contain("idx_jobsummary_queue"));
        Assert.That(indexes, Does.Contain("idx_scheduledtask_due"));
    }

    [Test]
    public void SingletonKey_Index_Allows_Multiple_Nulls_On_Every_Dialect()
    {
        var model = typeof(BackgroundJob).GetModelMetadata();
        List<string> Sql(IOrmLiteDialectProvider dialect) =>
            [BackgroundJobSchema.GetCreateIndexSql(dialect, "uidx_backgroundjob_singleton", model,
                [dialect.GetQuotedColumnName(nameof(BackgroundJob.SingletonKey))], unique:true, ignoreNulls:true)];

        // SQL Server treats NULLs as equal in a UNIQUE index, so it needs a filtered index,
        // otherwise only a single Job without a SingletonKey could ever be queued
        Assert.That(Sql(SqlServerDialect.Provider)[0], Does.Contain("IS NOT NULL"));

        // Every other RDBMS allows duplicate NULLs. SQLite shares PostgreSQL's IF NOT EXISTS branch.
        Assert.That(Sql(SqliteDialect.Provider)[0], Does.Not.Contain("IS NOT NULL"));
        Assert.That(Sql(SqliteDialect.Provider)[0], Does.Contain("CREATE UNIQUE INDEX IF NOT EXISTS"));
        // MySQL has no IF NOT EXISTS for CREATE INDEX, it's guarded by an information_schema check
        Assert.That(Sql(MySqlDialect.Provider)[0], Does.Not.Contain("IF NOT EXISTS"));
        Assert.That(Sql(MySqlDialect.Provider)[0], Does.Not.Contain("IS NOT NULL"));
    }

    [Test]
    public void SingletonKey_Uniqueness_Is_Enforced_By_The_Database()
    {
        using var db = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider).OpenDbConnection();
        BackgroundJobSchema.UpgradeMainDb(db);

        BackgroundJob NewJob(string? singletonKey) => new() {
            RefId = Guid.NewGuid().ToString("N"),
            SingletonKey = singletonKey,
            RequestType = CommandResult.Command,
            Request = nameof(MyRequest),
            RequestBody = "{}",
            CreatedDate = DateTime.UtcNow,
        };

        db.Insert(NewJob("nightly-import"));
        Assert.That(() => db.Insert(NewJob("nightly-import")), Throws.Exception);

        // Jobs without a SingletonKey are never treated as duplicates of each other
        db.Insert(NewJob(null));
        db.Insert(NewJob(null));
        Assert.That(db.Count<BackgroundJob>(x => x.SingletonKey == null), Is.EqualTo(2));
    }
}
