#if NET8_0_OR_GREATER
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using ServiceStack.Admin;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Jobs;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class TestSimpleCmd : IAsyncCommand<TestSimpleRequest>
{
    public bool Executed { get; set; }
    public Task ExecuteAsync(TestSimpleRequest request)
    {
        Executed = true;
        return Task.CompletedTask;
    }
}

public class TestSimpleRequest
{
    public string Name { get; set; } = "Test";
}

public class MockBackgroundJobs : IBackgroundJobs
{
    public List<BackgroundJobStatusUpdate> StatusUpdates { get; } = [];

    public void UpdateJobStatus(BackgroundJobStatusUpdate update)
    {
        StatusUpdates.Add(update);
    }

    public System.Data.IDbConnection OpenDb() => throw new NotImplementedException();
    public System.Data.IDbConnection OpenMonthDb(DateTime month) => throw new NotImplementedException();
    public BackgroundJobRef EnqueueApi(object requestDto, BackgroundJobOptions? options = null) => throw new NotImplementedException();
    public BackgroundJobRef EnqueueCommand(string commandName, object arg, BackgroundJobOptions? options = null) => throw new NotImplementedException();
    public BackgroundJob RunCommand(string commandName, object arg, BackgroundJobOptions? options = null) => throw new NotImplementedException();
    public Task<object?> RunCommandAsync(string commandName, object arg, BackgroundJobOptions? options = null) => throw new NotImplementedException();
    public void RecurringCommand(string taskName, Schedule schedule, string commandName, object arg, BackgroundJobOptions? options = null) => throw new NotImplementedException();
    public void RecurringApi(string taskName, Schedule schedule, object requestDto, BackgroundJobOptions? options = null) => throw new NotImplementedException();
    public void DeleteRecurringTask(string taskName) => throw new NotImplementedException();
    public JobResult? GetJob(long id) => throw new NotImplementedException();
    public JobResult? GetJobByRefId(string refId) => throw new NotImplementedException();
    public Task ExecuteJobAsync(BackgroundJob job) => throw new NotImplementedException();
    public object CreateRequest(BackgroundJobBase job) => throw new NotImplementedException();
    public object? CreateResponse(BackgroundJobBase job) => throw new NotImplementedException();
    public bool CancelJob(long id) => throw new NotImplementedException();
    public List<long> CancelJobs(BackgroundJobState? state = null, string? worker = null) => throw new NotImplementedException();
    public void CancelWorker(string worker) => throw new NotImplementedException();
    public void RequeueFailedJob(long id) => throw new NotImplementedException();
    public void FailJob(BackgroundJob job, Exception ex) => throw new NotImplementedException();
    public void FailJob(BackgroundJob job, ResponseStatus error, bool shouldRetry) => throw new NotImplementedException();
    public void CompleteJob(BackgroundJob job, object? response = null) => throw new NotImplementedException();
    public Task StartAsync(CancellationToken stoppingToken) => Task.CompletedTask;
    public Task TickAsync() => Task.CompletedTask;
    public int? GetCommandEstimatedDurationMs(string command, string? worker = null) => null;
    public int? GetApiEstimatedDurationMs(string request, string? worker = null) => null;
    public List<WorkerStats> GetWorkerStats() => [];
    public Dictionary<string, int> GetWorkerQueueCounts() => [];
    public ICollection<ScheduledTask> ScheduledTasks { get; } = [];
}

[TestFixture]
public class CommandsModernizationTests
{
    [TearDown]
    public void TearDown()
    {
        HostContext.Reset();
    }

    [Test]
    public async Task CommandsFeature_ExecutesWithNullLogger_WithoutThrowingNRE()
    {
        var feature = new CommandsFeature
        {
            Log = null // explicitly null to test safe logging
        };

        var cmd = new TestSimpleCmd();
        await feature.ExecuteCommandAsync(cmd, new TestSimpleRequest { Name = "Safe" });

        Assert.That(cmd.Executed, Is.True);
        Assert.That(feature.CommandResults.Count, Is.EqualTo(1));
        Assert.That(feature.CommandResults.First().Error, Is.Null);
    }

    [Test]
    public void CommandsFeature_AddCommandResult_HandlesNullResultAndNullName()
    {
        var feature = new CommandsFeature
        {
            ResultsCapacity = 5,
            FailuresCapacity = 5
        };

        Assert.DoesNotThrow(() => feature.AddCommandResult(null!));

        var unnamedResult = new CommandResult { Name = null!, Ms = 12 };
        feature.AddCommandResult(unnamedResult);

        Assert.That(unnamedResult.Name, Is.EqualTo("Unknown"));
        Assert.That(feature.CommandResults.Count, Is.EqualTo(1));
    }

    [Test]
    public void CommandsFeature_AddCommandResult_MaintainsCapacityUnderBounds()
    {
        var feature = new CommandsFeature
        {
            ResultsCapacity = 3,
            FailuresCapacity = 3
        };

        for (var i = 0; i < 10; i++)
        {
            feature.AddCommandResult(new CommandResult { Name = "Cmd", Ms = i });
        }

        Assert.That(feature.CommandResults.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task CommandsService_AllowsAnonymousAccess_WhenAccessRoleIsAllowAnon()
    {
        using var appHost = new BasicAppHost
        {
            ConfigureAppHost = host => host.Plugins.Add(new CommandsFeature { AccessRole = RoleNames.AllowAnon })
        }.Init();

        var service = new CommandsService(null!)
        {
            Request = new MockHttpRequest()
        };

        var response = await service.Any(new ViewCommands
        {
            Skip = -10, // negative skip should clamp to 0
            Take = -5   // negative take should clamp to 0
        }) as ViewCommandsResponse;

        Assert.That(response, Is.Not.Null);
        Assert.That(response.LatestCommands.Count, Is.EqualTo(0));
    }

    [Test]
    public void CommandsService_Any_ExecuteCommand_ThrowsArgumentNullOnEmptyCommand()
    {
        using var appHost = new BasicAppHost
        {
            ConfigureAppHost = host => host.Plugins.Add(new CommandsFeature { AccessRole = RoleNames.AllowAnon })
        }.Init();

        var service = new CommandsService(null!)
        {
            Request = new MockHttpRequest()
        };

        Assert.ThrowsAsync<ArgumentNullException>(async () => await service.Any((ExecuteCommand)null!));
        Assert.ThrowsAsync<ArgumentNullException>(async () => await service.Any(new ExecuteCommand { Command = "" }));
    }

    [Test]
    public void JobLogger_UsesFormatterAndPropagatesToBackgroundJobs()
    {
        var mockJobs = new MockBackgroundJobs();
        var job = new BackgroundJob { Id = 42, RefId = "job-42" };
        var jobLogger = new JobLogger(mockJobs, job);

        jobLogger.Log(LogLevel.Information, new EventId(1), "Hello {0}", null, (state, ex) => "Formatted Message");

        Assert.That(mockJobs.StatusUpdates.Count, Is.EqualTo(1));
        Assert.That(mockJobs.StatusUpdates[0].Log, Is.EqualTo("Formatted Message"));
    }

    [Test]
    public void JobLogger_HandlesNullJobsSafely()
    {
        var job = new BackgroundJob { Id = 42 };
        var jobLogger = new JobLogger(null!, job);

        Assert.DoesNotThrow(() => jobLogger.UpdateProgress(0.5));
        Assert.DoesNotThrow(() => jobLogger.UpdateStatus("Running", "In progress"));
        Assert.DoesNotThrow(() => jobLogger.UpdateLog("Processing complete"));
    }

    [Test]
    public void JobUtils_PopulateAndSummary_HandlesNullSafely()
    {
        BackgroundJob? nullJob = null;
        Assert.That(nullJob.ToJobSummary(), Is.Null);

        BackgroundJobBase? nullFrom = null;
        var to = new BackgroundJob();
        Assert.That(nullFrom.PopulateJob(to), Is.SameAs(to));

        var req = new MockHttpRequest();
        Assert.That(req.GetCancellationToken(), Is.EqualTo(default(CancellationToken)));
    }

    [Test]
    public void ApiToolRegistry_CanAccess_HandlesNullRequestAndOpenTools()
    {
        var config = new ApiToolsConfig();
        var registry = new ApiToolRegistry(config);

        var publicTool = new ApiTool
        {
            Name = "PublicApi",
            RequiresAuth = false,
            RequiresApiKey = false
        };

        var secureTool = new ApiTool
        {
            Name = "SecureApi",
            RequiresAuth = true
        };

        Assert.That(registry.CanAccess(publicTool, null!), Is.True);
        Assert.That(registry.CanAccess(secureTool, null!), Is.False);
        Assert.That(registry.CanAccess(null!, null!), Is.False);
    }

    [Test]
    public void AdminJobServiceExtensions_ToSummaries_HandlesNullCollections()
    {
        List<JobStat>? nullStats = null;
        List<HourStat>? nullHours = null;

        Assert.That(nullStats.ToSummaries(), Is.Empty);
        Assert.That(nullHours.ToSummaries(), Is.Empty);
    }
}
#endif
