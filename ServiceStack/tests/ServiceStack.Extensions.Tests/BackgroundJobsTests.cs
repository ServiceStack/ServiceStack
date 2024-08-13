#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.IO;
using ServiceStack.Jobs;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Extensions.Tests;

public class MyRequest 
{
    public int Id { get; set; }
}
public class MyResponse
{
    public required string Result { get; set; }
}

class MyJobCommand(IBackgroundJobs jobs) : IAsyncCommand<MyRequest,MyResponse>, IRequiresRequest
{
    public static long Count;
    public static IRequest? LastRequest { get; set; }
    public static MyRequest? LastCommandRequest { get; set; }
    public IRequest? Request { get; set; }

    public MyResponse? Result { get; set; }

    public async Task ExecuteAsync(MyRequest request)
    {
        jobs.UpdateBackgroundJobStatus(Request, 0.1, "Started", "MyCommand Started...");
        Interlocked.Increment(ref Count);
        LastRequest = Request;
        LastCommandRequest = request;
        Result = new MyResponse { Result = $"Hello {request.Id}" };
        jobs.UpdateBackgroundJobStatus(Request, 0.9, "Finished", "MyCommand Finished");
    }
}

class MyJobCallback(IBackgroundJobs jobs) : IAsyncCommand<MyResponse>, IRequiresRequest
{
    public static long Count;
    public static IRequest? LastRequest { get; set; }
    public static MyResponse? LastMyResponse { get; set; }
    public IRequest? Request { get; set; }

    public async Task ExecuteAsync(MyResponse request)
    {
        Interlocked.Increment(ref Count);
        LastRequest = Request;
        LastMyResponse = request;
    }
}

public class JobServices : Service
{
    public static long Count;
    public static IRequest? LastRequest { get; set; }
    public static MyRequest? LastCommandRequest { get; set; }
 
    public object Any(MyRequest request)
    {
        Interlocked.Increment(ref Count);
        LastRequest = Request;
        LastCommandRequest = request;
        return new MyResponse { Result = $"Hello {request.Id}" };
    }
}

public class AlwaysFail
{
    public int Id { get; set; }
}

public class AlwaysFailCommand : IAsyncCommand<AlwaysFail>
{
    public static long Count;
    public static IRequest? LastRequest { get; set; }
    public static AlwaysFail? LastCommandRequest { get; set; }
    public IRequest? Request { get; set; }

    public Task ExecuteAsync(AlwaysFail request)
    {
        Interlocked.Increment(ref Count);
        LastRequest = Request;
        LastCommandRequest = request;
        throw new Exception("Always Fails: " + Count);
    }
}

public class JobsHostedService(IBackgroundJobs jobs) : IHostedService, IDisposable
{
    private Timer? timer;
    public Task StartAsync(CancellationToken stoppingToken)
    {
        timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        return Task.CompletedTask;
    }
    private void DoWork(object? state)
    {
        jobs.Tick();
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        timer?.Change(Timeout.Infinite, 0);
        jobs.Dispose();
        return Task.CompletedTask;
    }
    public void Dispose() => timer?.Dispose();
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

        var dbFactory = new OrmLiteConnectionFactory("DataSource=App_Data/app.db;Cache=Shared", SqliteDialect.Provider);
        services.AddSingleton<IDbConnectionFactory>(dbFactory);

        services.AddPlugin(new CommandsFeature());
        services.AddPlugin(feature);

        services.AddServiceStack(typeof(JobServices).Assembly);
        
        services.AddHostedService<JobsHostedService>();

        var app = builder.Build();

        app.UseServiceStack(appHost, options => { options.MapEndpoints(); });
        app.StartAsync($"http://localhost:20000");        
    }

    private readonly AppHost appHost = new();
    private readonly BackgroundsJobFeature feature = new();
    class AppHost() : AppHostBase(nameof(BackgroundJobsTests), typeof(JobServices).Assembly)
    {
        public override void Configure()
        {
        }

        public override void OnAfterInit()
        {
            base.OnAfterInit();
            GetPlugin<BackgroundsJobFeature>().Start();
        }
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown() => AppHostBase.DisposeApp();

    void ResetState()
    {
        MyJobCommand.Count = 0;
        MyJobCommand.LastRequest = null;
        MyJobCommand.LastCommandRequest = null;
        MyJobCallback.Count = 0;
        MyJobCallback.LastRequest = null;
        MyJobCallback.LastMyResponse = null;
        JobServices.Count = 0;
        JobServices.LastRequest = null;
        JobServices.LastCommandRequest = null;
        AlwaysFailCommand.Count = 0;
        AlwaysFailCommand.LastRequest = null;
        AlwaysFailCommand.LastCommandRequest = null;

        using var db = feature.OpenJobsDb();
        db.DeleteAllAsync<BackgroundJob>();
        db.DeleteAllAsync<JobSummary>();
        using var monthDb = feature.OpenJobsMonthDb(DateTime.UtcNow);
        monthDb.DeleteAllAsync<CompletedJob>();
        monthDb.DeleteAllAsync<FailedJob>();
    }

    void AssertNotNulls(BackgroundJob job)
    {
        Assert.That(job, Is.Not.Null);
        Assert.That(job.Id, Is.GreaterThan(0));
        Assert.That(job.CreatedDate, Is.Not.Null);
        Assert.That(job.RequestId, Is.Not.Null);
        Assert.That(job.Request, Is.EqualTo(nameof(MyRequest)));
        Assert.That(job.RequestBody, Is.Not.Null);
        Assert.That(job.LastActivityDate, Is.Not.Null);
    }

    [Test]
    public void Does_execute_MyCommand()
    {
        ResetState();
        feature.Jobs.EnqueueCommand<MyJobCommand>(new MyRequest { Id = 1 });
        
        Assert.That(ExecUtils.WaitUntilTrue(() => MyJobCommand.LastRequest != null), "LastRequest == null");
        Assert.That(MyJobCommand.LastCommandRequest, Is.Not.Null);
        var job = MyJobCommand.LastRequest.GetBackgroundJob();
        Assert.That(job!.RequestType, Is.EqualTo(CommandResult.Command));
        AssertNotNulls(job);
        Assert.That(job.Status, Is.EqualTo("Finished"));
        Assert.That(ExecUtils.WaitUntilTrue(() => job.Progress >= 1), "job.Progress != 1");
        Assert.That(job.Logs, Is.EqualTo("MyCommand Started...\nMyCommand Finished"));
    }

    [Test]
    public void Does_execute_MyRequest_Api()
    {
        ResetState();
        feature.Jobs.EnqueueApi(new MyRequest { Id = 2 });
        
        Assert.That(ExecUtils.WaitUntilTrue(() => JobServices.LastRequest != null), "LastRequest == null");
        Assert.That(JobServices.LastCommandRequest, Is.Not.Null);
        var job = JobServices.LastRequest.GetBackgroundJob();
        Assert.That(job!.RequestType, Is.EqualTo(CommandResult.Api));
        AssertNotNulls(job);
    }

    [Test]
    public void Does_execute_Transient_Command()
    {
        ResetState();
        var job = feature.Jobs.ExecuteTransientCommand<MyJobCommand>(new MyRequest { Id = 1 }, new() { Worker = "app.db" });
        Assert.That(job.Worker, Is.EqualTo("app.db"));
        
        Assert.That(ExecUtils.WaitUntilTrue(() => MyJobCommand.LastRequest != null), "LastRequest == null");
        Assert.That(MyJobCommand.LastCommandRequest, Is.Not.Null);
        job = MyJobCommand.LastRequest.GetBackgroundJob();
        Assert.That(job!.RequestType, Is.EqualTo(CommandResult.Command));
        Assert.That(job.Id, Is.EqualTo(0));
        job!.Id = int.MaxValue;
        AssertNotNulls(job);
        Assert.That(job.Status, Is.EqualTo("Finished"));
        Assert.That(ExecUtils.WaitUntilTrue(() => job.Progress >= 1), "job.Progress != 1");
        Assert.That(job.Logs, Is.EqualTo("MyCommand Started...\nMyCommand Finished"));
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
        Assert.That(MyJobCommand.LastCommandRequest, Is.Not.Null);
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
        var dbJob = db.SingleById<BackgroundJob>(job.Id);
        Assert.That(dbJob, Is.Null);
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
    public void Does_retry_and_fail_AlwaysFailCommand()
    {
        ResetState();
        var jobRef = feature.Jobs.EnqueueCommand<AlwaysFailCommand>(new AlwaysFail { Id = 1 });
        Assert.That(ExecUtils.WaitUntilTrue(() => AlwaysFailCommand.Count >= 3, TimeSpan.FromMinutes(2)), "AlwaysFailCommand.Count < 3");
        Assert.That(AlwaysFailCommand.Count, Is.EqualTo(3));

        using var db = feature.OpenJobsDb();
        
        using var monthDb = feature.OpenJobsMonthDb(DateTime.UtcNow);

        FailedJob? failedJob = null;
        Assert.That(ExecUtils.WaitUntilTrue(() => {
            failedJob = monthDb.SingleById<FailedJob>(jobRef.Id);
            return failedJob != null;
        }), "failedJob == null");
        Assert.That(failedJob!.RefId, Is.EqualTo(jobRef.RefId));
        Assert.That(failedJob.Command, Is.EqualTo(nameof(AlwaysFailCommand)));
        Assert.That(failedJob.Request, Is.EqualTo(nameof(AlwaysFail)));
        Assert.That(failedJob.RequestBody, Is.EqualTo("{\"id\":1}"));
        Assert.That(failedJob.Attempts, Is.EqualTo(3));
        Assert.That(failedJob.State, Is.EqualTo(BackgroundJobState.Failed));
        Assert.That(failedJob.ErrorCode, Is.EqualTo(nameof(Exception)));
        Assert.That(failedJob.Error!.Message, Is.EqualTo("Always Fails: 3"));

        var summary = db.SingleById<JobSummary>(jobRef.Id);
        Assert.That(summary!.RefId, Is.EqualTo(jobRef.RefId));
        Assert.That(summary.Request, Is.EqualTo(nameof(AlwaysFail)));
        Assert.That(summary.Attempts, Is.EqualTo(3));
        Assert.That(summary.State, Is.EqualTo(BackgroundJobState.Failed));
        Assert.That(summary.ErrorCode, Is.EqualTo(nameof(Exception)));
        Assert.That(summary.ErrorMessage, Is.EqualTo("Always Fails: 3"));
    }
}
