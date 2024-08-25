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
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.IO;
using ServiceStack.Jobs;
using ServiceStack.OrmLite;
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
    public BackgroundJobsTests()
    {
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
        if (File.Exists(dbPath))
            File.Delete(dbPath);
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

    private readonly AppHost appHost = new();
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

        using var db = feature.OpenJobsDb();
        db.DeleteAllAsync<BackgroundJob>();
        db.DeleteAllAsync<JobSummary>();
        db.DeleteAllAsync<ScheduledTask>();
        using var monthDb = feature.OpenJobsMonthDb(DateTime.UtcNow);
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

        using var db = feature.OpenJobsDb();
        Assert.That(ExecUtils.WaitUntilTrue(() => db.SingleById<BackgroundJob>(job.Id) == null), "job != null");
        var dbJobSummary = db.SingleById<JobSummary>(job.Id);
        Assert.That(dbJobSummary, Is.Not.Null);
        Assert.That(dbJobSummary.CompletedDate, Is.Not.Null);
        Assert.That(dbJobSummary.Response, Is.EqualTo(job.Response));
        using var monthDb = feature.OpenJobsMonthDb(job.CreatedDate);
        
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

        using var db = feature.OpenJobsDb();
        
        using var monthDb = feature.OpenJobsMonthDb(DateTime.UtcNow);

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

        using var db = feature.OpenJobsDb();
        
        using var monthDb = feature.OpenJobsMonthDb(DateTime.UtcNow);

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

        using var db = feature.Jobs.OpenJobsDb();
        var taskName = "My Command Every Minute";
        var options = new BackgroundJobOptions { Tag = "test" };
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
            return task;
        }

        feature.Jobs.RecurringCommand<MySyncCommand>(taskName, Schedule.EveryMinute, options);

        var tasks = await db.SelectAsync<ScheduledTask>();
        Assert.That(tasks.Count, Is.EqualTo(1));
        var task = AssertTask(tasks[0]);
        Assert.That(task.Options, Is.Not.Null);
        
        feature.Jobs.RecurringCommand<MySyncCommand>(taskName, Schedule.EveryMinute);
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
    }

    [Test]
    public async Task Can_Schedule_Reoccurring_Api()
    {
        ResetState();

        using var db = feature.Jobs.OpenJobsDb();
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
    }
}
