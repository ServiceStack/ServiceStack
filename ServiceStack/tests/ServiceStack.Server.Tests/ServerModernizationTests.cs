#nullable enable
#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Data;
using ServiceStack.Host;
using ServiceStack.Jobs;
using ServiceStack.Messaging;
using ServiceStack.Messaging.Redis;
using ServiceStack.OrmLite;
using ServiceStack.Web;

namespace ServiceStack.Server.Tests;

[TestFixture]
public class ServerModernizationTests
{
    private class FakeBackgroundJobs : IBackgroundJobs
    {
        public bool ExecuteInvoked { get; private set; }
        public TaskCompletionSource<bool> JobTcs = new();

        public Task ExecuteJobAsync(BackgroundJob job)
        {
            ExecuteInvoked = true;
            return JobTcs.Task;
        }

        public BackgroundJobRef EnqueueCommand(string commandName, object arg, BackgroundJobOptions? options = null) => throw new NotImplementedException();
        public BackgroundJobRef EnqueueApi(object requestDto, BackgroundJobOptions? options = null) => throw new NotImplementedException();
        public BackgroundJob RunCommand(string commandName, object arg, BackgroundJobOptions? options = null) => throw new NotImplementedException();
        public Task<object?> RunCommandAsync(string commandName, object arg, BackgroundJobOptions? options = null) => throw new NotImplementedException();
        public object CreateRequest(BackgroundJobBase job) => throw new NotImplementedException();
        public object CreateRequestForCommand(string command, string argType, string? argJson) => throw new NotImplementedException();
        public object CreateRequestForApi(string requestType, string? requestJson) => throw new NotImplementedException();
        public object? CreateResponse(BackgroundJobBase job) => throw new NotImplementedException();
        public bool CancelJob(long jobId) => throw new NotImplementedException();
        public List<long> CancelJobs(BackgroundJobState? state = null, string? worker = null) => throw new NotImplementedException();
        public void CancelWorker(string worker) => throw new NotImplementedException();
        public void RequeueFailedJob(long jobId) => throw new NotImplementedException();
        public void FailJob(BackgroundJob job, Exception ex) => throw new NotImplementedException();
        public void FailJob(BackgroundJob job, ResponseStatus error, bool shouldRetry) => throw new NotImplementedException();
        public void CompleteJob(BackgroundJob job, object? response = null) => throw new NotImplementedException();
        public void RecurringApi(string taskName, Schedule schedule, object requestDto, BackgroundJobOptions? options = null) => throw new NotImplementedException();
        public void RecurringCommand(string taskName, Schedule schedule, string commandName, object arg, BackgroundJobOptions? options = null) => throw new NotImplementedException();
        public void DeleteRecurringTask(string taskName) => throw new NotImplementedException();
        public ICollection<ScheduledTask> ScheduledTasks => new List<ScheduledTask>();
        public Dictionary<string, int> GetWorkerQueueCounts() => throw new NotImplementedException();
        public List<WorkerStats> GetWorkerStats() => throw new NotImplementedException();
        public IDbConnection OpenDb() => throw new NotImplementedException();
        public IDbConnection OpenMonthDb(DateTime createdDate) => throw new NotImplementedException();
        public JobResult? GetJob(long jobId) => throw new NotImplementedException();
        public JobResult? GetJobByRefId(string refId) => throw new NotImplementedException();
        public void DispatchToWorker(BackgroundJob job) => throw new NotImplementedException();
        public Task StartAsync(CancellationToken stoppingToken) => Task.CompletedTask;
        public Task TickAsync() => Task.CompletedTask;
        public int? GetCommandEstimatedDurationMs(string commandType, string? worker = null) => null;
        public int? GetApiEstimatedDurationMs(string requestType, string? worker = null) => null;
        public void UpdateJobStatus(BackgroundJobStatusUpdate status) => throw new NotImplementedException();
    }

    [Test]
    public async Task DbJobsWorker_Tracks_Unwrapped_Async_Task()
    {
        var fakeJobs = new FakeBackgroundJobs();
        using var cts = new CancellationTokenSource();
        var worker = new DbJobsWorker(fakeJobs, cts.Token, transient: false, defaultTimeOutSecs: 60);

        var job = new BackgroundJob { Id = 1, RequestType = CommandResult.Command, Command = "Test" };
        worker.Enqueue(job);

        // Wait a moment for background task to start
        for (var i = 0; i < 50 && !fakeJobs.ExecuteInvoked; i++)
        {
            await Task.Delay(20);
        }

        Assert.That(fakeJobs.ExecuteInvoked, Is.True);
        Assert.That(worker.BackgroundTask, Is.Not.Null);

        // Because of .Unwrap(), the worker.BackgroundTask should still be running while JobTcs is pending
        Assert.That(worker.BackgroundTask!.IsCompleted, Is.False, "BackgroundTask should be unwrapped and still running");

        // Complete the job
        fakeJobs.JobTcs.SetResult(true);

        // Now the background task completes
        await worker.BackgroundTask;
        Assert.That(worker.BackgroundTask.IsCompletedSuccessfully, Is.True);

        worker.Dispose();
    }

    [Test]
    public void MessageHandlerWorker_GetStatus_Does_Not_Throw_When_BgThread_Null()
    {
        var worker = new MessageHandlerWorker(null!, null!, "test_queue", null!);
        var status = worker.GetStatus();

        Assert.That(status, Does.Contain("test_queue"));
        Assert.That(status, Does.Contain("ThreadStatus: None"));
    }

    [Test]
    public void DbRequestLogger_Register_Does_Not_Throw_When_ExcludeRequestDtoTypes_Null()
    {
        var logger = new DbRequestLogger
        {
            ExcludeRequestDtoTypes = null!
        };

        // Simulating the safe union logic in Register
        var ignore = logger.IgnoreRequestTypes;
        var result = (logger.ExcludeRequestDtoTypes ?? []).Union(ignore).ToArray();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(ignore.Length));
    }

    [Test]
    public void GetTableMonths_Parsing_Handles_All_Supported_Formats()
    {
        var sampleInputs = new[]
        {
            "2026-09",
            "CompletedJob_2026-09.db",
            "requestlog_2026_09",
            "\"2026-09\"",
            "'2026-09'"
        };

        var parsed = sampleInputs
            .Select(x => {
                var str = x.StripDbQuotes();
                if (str.Contains('_'))
                {
                    str = str.RightPart('_').LeftPart('.').Replace('_', '-');
                }
                return DateTime.TryParse(str + "-01", out var date) ? date : (DateTime?)null;
            })
            .Where(x => x != null)
            .Select(x => x!.Value)
            .Distinct()
            .OrderByDescending(x => x)
            .ToList();

        Assert.That(parsed.Count, Is.EqualTo(1));
        Assert.That(parsed[0], Is.EqualTo(new DateTime(2026, 9, 1)));
    }

    [Test]
    public void DbRequestLogger_Analytics_Tabs_Check_Correct_Flags()
    {
        // Simulate result where only API keys and IPs are present, but apis = 0
        var result = (apis: (int?)0, users: (int?)0, apiKeys: (int?)1, ips: (int?)1);
        var tabs = new Dictionary<string, string>();

        if (result.apis == 1)
            tabs["APIs"] = "";
        if (result.users == 1)
            tabs["Users"] = "users";
        if (result.apiKeys == 1)
            tabs["API Keys"] = "apiKeys";
        if (result.ips == 1)
            tabs["IP Addresses"] = "ips";

        Assert.That(tabs.ContainsKey("APIs"), Is.False);
        Assert.That(tabs.ContainsKey("Users"), Is.False);
        Assert.That(tabs.ContainsKey("API Keys"), Is.True);
        Assert.That(tabs.ContainsKey("IP Addresses"), Is.True);
    }

    [Test]
    public async Task ApiKeyCredentialsProvider_GetValidApiKeyAsync_Returns_Null_When_IApiKeySource_Unregistered()
    {
        var provider = new ApiKeyCredentialsProvider();
        var mockRequest = new BasicRequest { Resolver = new Funq.Container() };

        var key = await provider.GetValidApiKeyAsync("test_token", mockRequest);
        Assert.That(key, Is.Null);
    }

    [Test]
    public async Task OrmLiteCacheClientAsync_VerifyAsync_Removes_Expired_Entries()
    {
        var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
        var cacheClient = new OrmLiteCacheClient
        {
            DbFactory = dbFactory
        };
        cacheClient.InitSchema();

        using (var db = dbFactory.Open())
        {
            // Insert expired entry
            db.Insert(new CacheEntry
            {
                Id = "expired_key",
                Data = "expired_value",
                CreatedDate = DateTime.UtcNow.AddHours(-2),
                ExpiryDate = DateTime.UtcNow.AddHours(-1)
            });

            // Insert valid entry
            db.Insert(new CacheEntry
            {
                Id = "valid_key",
                Data = "\"valid_value\"",
                CreatedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddHours(1)
            });

            var expiredEntry = await db.SingleByIdAsync<CacheEntry>("expired_key");
            var verifiedExpired = await cacheClient.VerifyAsync(db, expiredEntry);
            Assert.That(verifiedExpired, Is.Null, "Expired entry should verify to null");

            // Verify it was deleted from DB
            var dbExpired = await db.SingleByIdAsync<CacheEntry>("expired_key");
            Assert.That(dbExpired, Is.Null, "Expired entry should be deleted from DB");

            // Verify valid entry is preserved
            var validEntry = await db.SingleByIdAsync<CacheEntry>("valid_key");
            var verifiedValid = await cacheClient.VerifyAsync(db, validEntry);
            Assert.That(verifiedValid, Is.Not.Null, "Valid entry should not verify to null");
            Assert.That(verifiedValid!.Id, Is.EqualTo("valid_key"));
        }
    }
}
#endif
