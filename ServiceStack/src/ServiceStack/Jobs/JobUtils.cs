#if NET8_0_OR_GREATER
#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Jobs;

public static class JobUtils
{
    public static IBackgroundJobsQueues AssertQueues(this IBackgroundJobs jobs) =>
        jobs as IBackgroundJobsQueues
            ?? throw new NotSupportedException($"{jobs.GetType().Name} does not support Job Batch and Queue controls");

    /// <summary>Returns the progress of a Job Batch, or null if the batch doesn't exist</summary>
    public static JobBatch? GetJobBatch(this IBackgroundJobs jobs, string batchId) =>
        jobs.AssertQueues().GetJobBatch(batchId);

    /// <summary>
    /// Registers a Job Batch up-front. Setting Total lets the batch report accurate progress and
    /// is what makes its Callback fire reliably when Jobs are queued over time.
    /// </summary>
    /// <param name="callback">Command to run once every Job has finished, in any state</param>
    /// <param name="onSuccess">Command to run once every Job has completed successfully</param>
    public static JobBatch CreateJobBatch(this IBackgroundJobs jobs, string batchId,
        int? total = null, string? callback = null, string? description = null, string? createdBy = null,
        string? onSuccess = null, string? parentBatchId = null) =>
        jobs.AssertQueues().CreateJobBatch(new JobBatch {
            Id = batchId,
            Total = total,
            Callback = callback,
            OnSuccess = onSuccess,
            ParentBatchId = parentBatchId,
            Description = description,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow,
        });

    /// <summary>Cancels every active Job in a batch and prevents any more being added to it</summary>
    public static List<long> CancelJobBatch(this IBackgroundJobs jobs, string batchId) =>
        jobs.AssertQueues().CancelJobBatch(batchId);

    /// <summary>Returns the failed attempts of a Job, oldest first</summary>
    public static List<JobAttempt> GetJobAttempts(this IBackgroundJobs jobs, long jobId) =>
        jobs.AssertQueues().GetJobAttempts(jobId);

    /// <summary>Stops an App Server taking new Jobs while it finishes the ones it has</summary>
    public static bool SetJobNodeDraining(this IBackgroundJobs jobs, string serverId, bool draining) =>
        jobs.AssertQueues().SetJobNodeDraining(serverId, draining);

    /// <summary>
    /// Limits how many Jobs on a queue may start per window across every node, e.g. for work bound by
    /// a third-party quota. A rateLimit of 0 removes the limit.
    /// </summary>
    public static JobQueue SetJobQueueRateLimit(this IBackgroundJobs jobs, string name, int rateLimit,
        TimeSpan? window = null, string? modifiedBy = null) =>
        jobs.AssertQueues().SetJobQueue(name, modifiedBy:modifiedBy, rateLimit:rateLimit,
            rateLimitSecs: window != null ? Math.Max(1, (int)window.Value.TotalSeconds) : null);

    public static IBackgroundJobsTransactional AssertTransactional(this IBackgroundJobs jobs) =>
        jobs as IBackgroundJobsTransactional
            ?? throw new NotSupportedException($"{jobs.GetType().Name} does not support queueing Jobs in a transaction");

    /// <summary>
    /// Queues a Command as part of the caller's transaction on db, so it only runs if the transaction commits
    /// </summary>
    public static BackgroundJobRef EnqueueCommand<TCommand>(this IBackgroundJobs jobs, IDbConnection db,
        object request, BackgroundJobOptions? options = null) where TCommand : IAsyncCommand =>
        jobs.AssertTransactional().EnqueueCommand(db, typeof(TCommand).Name, request, options);

    /// <summary>
    /// Queues an API as part of the caller's transaction on db, so it only runs if the transaction commits
    /// </summary>
    public static BackgroundJobRef EnqueueApi(this IBackgroundJobs jobs, IDbConnection db,
        object requestDto, BackgroundJobOptions? options = null) =>
        jobs.AssertTransactional().EnqueueApi(db, requestDto, options);

    /// <summary>
    /// Registers a Job Batch that runs TCommand with the completed JobBatch once every Job in the
    /// batch has finished.
    /// </summary>
    public static JobBatch CreateJobBatch<TCommand>(this IBackgroundJobs jobs, string batchId,
        int? total = null, string? description = null, string? createdBy = null)
        where TCommand : IAsyncCommand<JobBatch> =>
        jobs.CreateJobBatch(batchId, total, typeof(TCommand).Name, description, createdBy);

    /// <summary>Returns the runtime controls of every known Job Queue</summary>
    public static List<JobQueue> GetJobQueues(this IBackgroundJobs jobs) =>
        jobs.AssertQueues().GetJobQueues();

    /// <summary>Stops dispatching Jobs on this queue. Queued Jobs stay queued until it's resumed.</summary>
    public static JobQueue PauseJobQueue(this IBackgroundJobs jobs, string name, string? modifiedBy = null) =>
        jobs.AssertQueues().SetJobQueue(name, paused:true, modifiedBy:modifiedBy);

    /// <summary>Resumes dispatching Jobs on this queue</summary>
    public static JobQueue ResumeJobQueue(this IBackgroundJobs jobs, string name, string? modifiedBy = null) =>
        jobs.AssertQueues().SetJobQueue(name, paused:false, modifiedBy:modifiedBy);

    /// <summary>Whether Jobs on this queue are currently being dispatched</summary>
    public static bool IsQueuePaused(this IBackgroundJobs jobs, string queue) =>
        jobs.AssertQueues().IsQueuePaused(queue);

    /// <summary>Max concurrent Jobs for this queue, including any runtime override</summary>
    public static int GetQueueConcurrency(this IBackgroundJobs jobs, string queue) =>
        jobs.AssertQueues().GetQueueConcurrency(queue);

    /// <summary>Changes how many Jobs this queue runs concurrently, without a redeploy</summary>
    public static JobQueue SetJobQueueConcurrency(this IBackgroundJobs jobs, string name, int concurrency,
        string? modifiedBy = null) =>
        jobs.AssertQueues().SetJobQueue(name, concurrency:concurrency, modifiedBy:modifiedBy);

    /// <summary>States a Job can no longer transition out of</summary>
    public static bool IsFinished(this BackgroundJobState state) =>
        state is BackgroundJobState.Completed or BackgroundJobState.Failed or BackgroundJobState.Cancelled;

    /// <summary>
    /// Waits for a durable Job to finish and returns its result, e.g. to accept a request, queue
    /// the work, then return its result on the same connection.
    /// </summary>
    /// <exception cref="TimeoutException">The Job didn't finish within the timeout</exception>
    public static async Task<JobResult> WaitForJobAsync(this IBackgroundJobs jobs, long jobId,
        TimeSpan? timeout = null, CancellationToken token = default)
    {
        var deadline = timeout != null ? DateTime.UtcNow.Add(timeout.Value) : (DateTime?)null;
        var attempt = 0;
        while (true)
        {
            var result = jobs.GetJob(jobId)
                ?? throw new ArgumentException($"Job {jobId} does not exist", nameof(jobId));
            if (result.Summary.State.IsFinished())
                return result;

            token.ThrowIfCancellationRequested();
            if (deadline != null && DateTime.UtcNow >= deadline)
                throw new TimeoutException($"Job {jobId} did not complete within {timeout}");

            // Back off from 100ms to 1s so a long Job doesn't poll the database needlessly
            var delayMs = Math.Min(1000, 100 * (1 + attempt++));
            await Task.Delay(delayMs, token).ConfigAwait();
        }
    }

    /// <summary>Waits for a queued Job to finish and returns its result</summary>
    public static Task<JobResult> WaitForJobAsync(this IBackgroundJobs jobs, BackgroundJobRef jobRef,
        TimeSpan? timeout = null, CancellationToken token = default) =>
        jobs.WaitForJobAsync(jobRef.Id, timeout, token);

    public static IBackgroundJobsScheduler AssertScheduler(this IBackgroundJobs jobs) =>
        jobs as IBackgroundJobsScheduler
            ?? throw new NotSupportedException($"{jobs.GetType().Name} does not support recurring task controls");

    public static bool SetRecurringTaskEnabled(this IBackgroundJobs jobs, long taskId, bool enabled) =>
        jobs.AssertScheduler().SetRecurringTaskEnabled(taskId, enabled);

    public static bool SetRecurringTaskEnabled(this IBackgroundJobs jobs, string taskName, bool enabled)
    {
        var task = jobs.GetScheduledTask(taskName);
        return task != null && jobs.AssertScheduler().SetRecurringTaskEnabled(task.Id, enabled);
    }

    public static bool RunRecurringTaskNow(this IBackgroundJobs jobs, long taskId) =>
        jobs.AssertScheduler().RunRecurringTaskNow(taskId);

    public static bool RunRecurringTaskNow(this IBackgroundJobs jobs, string taskName)
    {
        var task = jobs.GetScheduledTask(taskName);
        return task != null && jobs.AssertScheduler().RunRecurringTaskNow(task.Id);
    }

    /// <summary>
    /// Scheduled times are kept to the whole second. MySQL DATETIME and SQL Server DATETIME drop or
    /// round sub-second precision, which breaks the NextRun compare-and-set that claims an occurrence
    /// and would run it twice.
    /// </summary>
    public static DateTime ToScheduleTime(this DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);

    /// <summary>The first time a Scheduled Task may run from now, respecting its StartDate</summary>
    public static DateTime GetFirstRun(this ScheduledTask task, DateTime now) =>
        (task.StartDate is { } startDate && startDate > now ? startDate : now).ToScheduleTime();

    /// <summary>Whether a Scheduled Task has reached its EndDate or MaxRuns</summary>
    public static bool HasEnded(this ScheduledTask task, DateTime now) =>
        (task.MaxRuns != null && task.RunCount >= task.MaxRuns) || (task.EndDate != null && now > task.EndDate);

    /// <summary>Prefix of the deterministic RefId + SingletonKey used by Scheduled Task occurrences</summary>
    public const string ScheduledPrefix = "scheduled:";

    /// <summary>
    /// A Scheduled Task occurrence uses a deterministic RefId so that a node which fails between
    /// enqueueing the Job and recording it doesn't create a duplicate Job for the same occurrence.
    /// </summary>
    public static string CreateScheduledRefId(long taskId, DateTime occurrence) =>
        $"{ScheduledPrefix}{taskId}:{occurrence.Ticks}";

    /// <summary>Key used to enforce that only 1 Job per Scheduled Task is active at a time</summary>
    public static string CreateScheduledSingletonKey(long taskId) => $"{ScheduledPrefix}{taskId}";

    public static bool TryGetScheduledTaskId(string? refId, out long taskId)
    {
        taskId = 0;
        if (refId == null || !refId.StartsWith(ScheduledPrefix))
            return false;
        var rest = refId.Substring(ScheduledPrefix.Length);
        var endPos = rest.IndexOf(':');
        if (endPos >= 0)
            rest = rest.Substring(0, endPos);
        return long.TryParse(rest, out taskId);
    }

    public static ScheduledTask? GetScheduledTask(this IBackgroundJobs jobs, string taskName)
    {
        foreach (var task in jobs.ScheduledTasks)
        {
            if (task.Name == taskName)
                return task;
        }
        return null;
    }

    public static BackgroundJobOptions Copy(this BackgroundJobOptions? from) => new()
    {
        RefId = from?.RefId,
        DuplicateRefIdBehavior = from?.DuplicateRefIdBehavior ?? DuplicateRefIdBehavior.Throw,
        SingletonKey = from?.SingletonKey,
        ParentId = from?.ParentId,
        Worker = from?.Worker,
        Queue = from?.Queue,
        Priority = from?.Priority,
        RunAfter = from?.RunAfter,
        ExpiresAt = from?.ExpiresAt,
        ExpiresIn = from?.ExpiresIn,
        Callback = from?.Callback,
        DependsOn = from?.DependsOn,
        DependsOnBatch = from?.DependsOnBatch,
        DependsOnPolicy = from?.DependsOnPolicy,
        ConcurrencyKey = from?.ConcurrencyKey,
        TenantId = from?.TenantId,
        UserId = from?.UserId,
        RetryLimit = from?.RetryLimit,
        RetryBackoff = from?.RetryBackoff,
        RetryDelayMs = from?.RetryDelayMs,
        MaxRetryDelayMs = from?.MaxRetryDelayMs,
        ReplyTo = from?.ReplyTo,
        Tag = from?.Tag,
        BatchId = from?.BatchId,
        CreatedBy = from?.CreatedBy,
        TimeoutSecs = from?.TimeoutSecs,
        Args = from?.Args,
        RunCommand = from?.RunCommand,
        OnSuccess = from?.OnSuccess,
        OnFailed = from?.OnFailed,
        Token = from?.Token,
    };

    public static BackgroundJobRef? GetExistingJobRef(this IBackgroundJobs jobs, BackgroundJob job)
    {
        if (job.DuplicateRefIdBehavior != DuplicateRefIdBehavior.ReturnExisting || job.RefId == null)
            return null;
        var existing = jobs.GetJobByRefId(job.RefId);
        if (existing == null)
            return null;
        if (existing.Summary.RequestType != job.RequestType ||
            existing.Summary.Command != job.Command || existing.Summary.Request != job.Request ||
            (existing.Job != null && existing.Job.RequestBody != job.RequestBody))
            throw new DuplicateRefIdException(job.RefId);
        return new BackgroundJobRef(existing.Summary.Id, job.RefId);
    }

    /// <summary>
    /// Calculates the delay before the next attempt. Jitter is "equal jitter": half the calculated
    /// delay plus a random amount of the other half, so a retry is never scheduled immediately.
    /// </summary>
    public static TimeSpan GetRetryDelay(BackgroundJob job, RetryBackoff defaultBackoff,
        int defaultDelayMs, int defaultMaxDelayMs, double jitterSample)
    {
        if (jitterSample < 0 || jitterSample > 1)
            throw new ArgumentOutOfRangeException(nameof(jitterSample));

        var baseMs = Math.Max(0, job.RetryDelayMs ?? defaultDelayMs);
        var maxMs = Math.Max(baseMs, job.MaxRetryDelayMs ?? defaultMaxDelayMs);
        var retryNumber = Math.Max(1, job.Attempts);
        var strategy = job.RetryBackoff ?? defaultBackoff;
        double multiplier = strategy switch
        {
            RetryBackoff.Fixed => 1,
            RetryBackoff.Linear => retryNumber,
            RetryBackoff.Exponential or RetryBackoff.ExponentialJitter => Math.Pow(2, Math.Min(30, retryNumber - 1)),
            _ => throw new ArgumentOutOfRangeException(nameof(job.RetryBackoff)),
        };
        var cappedMs = Math.Min(maxMs, baseMs * multiplier);
        if (strategy == RetryBackoff.ExponentialJitter)
            cappedMs = cappedMs / 2 + cappedMs / 2 * jitterSample;
        return TimeSpan.FromMilliseconds(cappedMs);
    }

    public static BackgroundJobRef EnqueueCommand<TCommand>(this IBackgroundJobs jobs, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand<NoArgs> =>
        jobs.EnqueueCommand(typeof(TCommand).Name, NoArgs.Value, options);

    public static BackgroundJobRef EnqueueCommand<TCommand>(this IBackgroundJobs jobs, object request, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand =>
        jobs.EnqueueCommand(typeof(TCommand).Name, request, options);

    public static BackgroundJobRef ScheduleApi<T>(this IBackgroundJobs jobs, T request, DateTime at, BackgroundJobOptions? options = null) 
        where T : class
    {
        options ??= new();
        options.RunAfter = at;
        return jobs.EnqueueApi(request, options);
    }

    public static BackgroundJobRef ScheduleApi<T>(this IBackgroundJobs jobs, T request, TimeSpan after, BackgroundJobOptions? options = null) 
        where T : class
    {
        options ??= new();
        options.RunAfter = DateTime.UtcNow.Add(after);
        return jobs.EnqueueApi(request, options);
    }

    public static BackgroundJobRef ScheduleCommand<TCommand>(this IBackgroundJobs jobs, DateTime at, BackgroundJobOptions? options = null)
        where TCommand : IAsyncCommand<NoArgs> => jobs.ScheduleCommand<TCommand>(NoArgs.Value, at, options);

    public static BackgroundJobRef ScheduleCommand<TCommand>(this IBackgroundJobs jobs, object request, DateTime at, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand
    {
        options ??= new();
        options.RunAfter = at;
        return jobs.EnqueueCommand(typeof(TCommand).Name, request, options);
    }

    public static BackgroundJobRef ScheduleCommand<TCommand>(this IBackgroundJobs jobs, TimeSpan after, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand<NoArgs> => jobs.ScheduleCommand<TCommand>(NoArgs.Value, after, options);

    public static BackgroundJobRef ScheduleCommand<TCommand>(this IBackgroundJobs jobs, object request, TimeSpan after, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand
    {
        options ??= new();
        options.RunAfter = DateTime.UtcNow.Add(after);
        return jobs.EnqueueCommand(typeof(TCommand).Name, request, options);
    }

    public static BackgroundJob RunCommand<TCommand>(this IBackgroundJobs jobs, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand<NoArgs> => jobs.RunCommand(typeof(TCommand).Name, NoArgs.Value, options);
    public static Task<object?> RunCommandAsync<TCommand>(this IBackgroundJobs jobs, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand<NoArgs> => jobs.RunCommandAsync(typeof(TCommand).Name, NoArgs.Value, options);
    public static BackgroundJob RunCommand<TCommand>(this IBackgroundJobs jobs, object request, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand => jobs.RunCommand(typeof(TCommand).Name, request, options);
    public static Task<object?> RunCommandAsync<TCommand>(this IBackgroundJobs jobs, object request, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand => jobs.RunCommandAsync(typeof(TCommand).Name, request, options);
    
    public static void RecurringCommand<TCommand>(this IBackgroundJobs jobs, Schedule schedule, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand<NoArgs> => jobs.RecurringCommand(typeof(TCommand).Name, schedule, typeof(TCommand).Name, NoArgs.Value, options);
    public static void RecurringCommand<TCommand>(this IBackgroundJobs jobs, string taskName, Schedule schedule, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand<NoArgs> => jobs.RecurringCommand(taskName, schedule, typeof(TCommand).Name, NoArgs.Value, options);
    public static void RecurringCommand<TCommand>(this IBackgroundJobs jobs, Schedule schedule, object request, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand => jobs.RecurringCommand(typeof(TCommand).Name, schedule, typeof(TCommand).Name, request, options);
    public static void RecurringCommand<TCommand>(this IBackgroundJobs jobs, string taskName, Schedule schedule, object request, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand => jobs.RecurringCommand(taskName, schedule, typeof(TCommand).Name, request, options);

    public static void RecurringApi(this IBackgroundJobs jobs, Schedule schedule, object requestDto, BackgroundJobOptions? options = null) =>
        jobs.RecurringApi(requestDto.GetType().Name, schedule, requestDto, options);
    
    public static BackgroundJob ToBackgroundJob(this BackgroundJobOptions? options, string requestType, object arg)
    {
        ArgumentNullException.ThrowIfNull(arg);
        return new BackgroundJob
        {
            State = BackgroundJobState.Queued,
            Attempts = 1,
            RefId = options?.RefId ?? Guid.NewGuid().ToString("N"),
            DuplicateRefIdBehavior = options?.DuplicateRefIdBehavior ?? DuplicateRefIdBehavior.Throw,
            SingletonKey = options?.SingletonKey,
            ParentId = options?.ParentId,
            Worker = options?.Worker,
            Queue = options?.Queue ?? JobQueues.Default,
            Priority = options?.Priority ?? 0,
            Tag = options?.Tag,
            BatchId = options?.BatchId,
            Callback = options?.Callback,
            RunAfter = options?.RunAfter,
            DependsOnBatch = options?.DependsOnBatch,
            DependsOnPolicy = options?.DependsOnPolicy,
            ConcurrencyKey = options?.ConcurrencyKey,
            TenantId = options?.TenantId,
            // Continue the trace of whatever queued this Job, so its execution isn't an orphan root
            TraceId = Activity.Current?.Id,
            ExpiresAt = options?.ExpiresAt ?? (options?.ExpiresIn != null
                ? DateTime.UtcNow.Add(options.ExpiresIn.Value)
                : null),
            DependsOn = options?.DependsOn,
            UserId = options?.UserId,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = options?.CreatedBy,
            RequestType = requestType,
            Request = arg.GetType().Name,
            RequestBody = ClientConfig.ToJson(arg),
            RetryLimit = options?.RetryLimit,
            RetryBackoff = options?.RetryBackoff,
            RetryDelayMs = options?.RetryDelayMs,
            MaxRetryDelayMs = options?.MaxRetryDelayMs,
            TimeoutSecs = options?.TimeoutSecs,
            ReplyTo = options?.ReplyTo,
            Args = options?.Args,
            OnSuccess = options?.OnSuccess,
            OnFailed = options?.OnFailed,
            Token = options?.Token,
            LastActivityDate = DateTime.UtcNow,
        };
    }

    public static T PopulateJob<T>(this BackgroundJobBase from, T to) where T : BackgroundJobBase
    {
        if (from == null || to == null)
            return to;

        to.Id = from.Id;
        to.ParentId = from.ParentId;
        to.RefId = from.RefId;
        to.Worker = from.Worker;
        to.Queue = from.Queue;
        to.Priority = from.Priority;
        to.Tag = from.Tag;
        to.BatchId = from.BatchId;
        to.Callback = from.Callback;
        to.RunAfter = from.RunAfter;
        to.ExpiresAt = from.ExpiresAt;
        to.DependsOn = from.DependsOn;
        to.DependsOnBatch = from.DependsOnBatch;
        to.DependsOnPolicy = from.DependsOnPolicy;
        to.ConcurrencyKey = from.ConcurrencyKey;
        to.TenantId = from.TenantId;
        to.TraceId = from.TraceId;
        to.CreatedDate = from.CreatedDate;
        to.CreatedBy = from.CreatedBy;
        to.RequestId = from.RequestId;
        to.RequestType = from.RequestType;
        to.Command = from.Command;
        to.Request = from.Request;
        to.RequestBody = from.RequestBody;
        to.UserId = from.UserId;
        to.Response = from.Response;
        to.ResponseBody = from.ResponseBody;
        to.State = from.State;
        to.StartedDate = from.StartedDate;
        to.CompletedDate = from.CompletedDate;
        to.NotifiedDate = from.NotifiedDate;
        to.DurationMs = from.DurationMs;
        to.TimeoutSecs = from.TimeoutSecs;
        to.RetryLimit = from.RetryLimit;
        to.RetryBackoff = from.RetryBackoff;
        to.RetryDelayMs = from.RetryDelayMs;
        to.MaxRetryDelayMs = from.MaxRetryDelayMs;
        to.Attempts = from.Attempts;
        to.Progress = from.Progress;
        to.Status = from.Status;
        to.Logs = from.Logs;
        to.LogsTruncated = from.LogsTruncated;
        to.LastActivityDate = from.LastActivityDate;
        to.CancelRequestedDate = from.CancelRequestedDate;
        to.LeaseOwner = from.LeaseOwner;
        to.ReplyTo = from.ReplyTo;
        to.ErrorCode = from.ErrorCode;
        to.Error = from.Error;
        to.Args = from.Args;
        to.Meta = from.Meta;
        return to;
    }

    public static JobSummary? ToJobSummary(this BackgroundJob? from)
    {
        if (from == null)
            return null;

        return new JobSummary {
            Id = from.Id,
            ParentId = from.ParentId,
            RefId = from.RefId,
            Worker = from.Worker,
            Queue = from.Queue,
            Priority = from.Priority,
            Tag = from.Tag,
            BatchId = from.BatchId,
            CreatedDate = from.CreatedDate,
            CreatedBy = from.CreatedBy,
            RequestType = from.RequestType,
            Command = from.Command,
            Request = from.Request,
            Response = from.Response,
            UserId = from.UserId,
            Callback = from.Callback,
            StartedDate = from.StartedDate,
            CompletedDate = from.CompletedDate,
            State = from.State,
            DurationMs = from.DurationMs,
            Attempts = from.Attempts,
            RunAfter = from.RunAfter,
            ExpiresAt = from.ExpiresAt,
            ConcurrencyKey = from.ConcurrencyKey,
            SingletonKey = from.SingletonKey,
            TenantId = from.TenantId,
            TraceId = from.TraceId,
            LeaseOwner = from.LeaseOwner,
            CancelRequestedDate = from.CancelRequestedDate,
            LogsTruncated = from.LogsTruncated,
            ErrorMessage = from.Error?.Message,
            ErrorCode = from.ErrorCode,
            Meta = from.Meta,
        };
    }

    public static void SetCancellationToken(this IRequest req, CancellationToken token) => req.SetItem(nameof(CancellationToken), token);

    public static CancellationToken GetCancellationToken(this IRequest? req) =>
        req?.Items.TryGetValue(nameof(CancellationToken), out var oToken) == true && oToken is CancellationToken token
            ? token
            : default;

    public static BackgroundJob GetBackgroundJob(this IRequest? req) => req?.TryGetBackgroundJob()
        ?? throw new Exception("BackgroundJob not found");

    public static void SetBackgroundJob(this IRequest req, BackgroundJob job) => req.SetItem(nameof(BackgroundJob), job);

    public static BackgroundJob? TryGetBackgroundJob(this IRequest? req)
    {
        return req?.Items.TryGetValue(nameof(BackgroundJob), out var oJob) == true
            ? oJob as BackgroundJob
            : null;
    }

    public static object? CreateRequest(this IBackgroundJobs jobs, JobResult? result)
    {
        var job = result?.Job;
        return job != null ? jobs.CreateRequest(job) : null;
    }
    public static object? CreateResponse(this IBackgroundJobs jobs, JobResult? result)
    {
        var job = result?.Job;
        return job != null ? jobs.CreateResponse(job) : null;
    }
    public static JobLogger CreateJobLogger(this IRequest req, IBackgroundJobs jobs, ILogger? log = null)
    {
        ArgumentNullException.ThrowIfNull(req);
        return new(jobs, req.GetBackgroundJob(), log);
    }
}
#endif
