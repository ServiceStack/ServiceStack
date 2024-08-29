#nullable enable

using System;
using System.Data;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Jobs;

public interface IBackgroundJobs
{
    BackgroundJobRef EnqueueApi(object requestDto, BackgroundJobOptions? options = null);
    BackgroundJobRef EnqueueCommand(string commandName, object arg, BackgroundJobOptions? options = null);
    BackgroundJob RunCommand(string commandName, object arg, BackgroundJobOptions? options = null);
    Task<object?> RunCommandAsync(string commandName, object arg, BackgroundJobOptions? options = null);
    Task ExecuteJobAsync(BackgroundJob job);
    void CancelJob(long jobId);
    void CancelWorker(string worker);
    void FailJob(BackgroundJob job, Exception ex);
    void FailJob(BackgroundJob job, ResponseStatus error, bool shouldRetry);
    void CompleteJob(BackgroundJob job, object? response = null);
    void UpdateJobStatus(BackgroundJobStatusUpdate status);
    Task StartAsync(CancellationToken stoppingToken);
    Task TickAsync();
    Dictionary<string, int> GetWorkerQueueCounts();
    List<WorkerStats> GetWorkerStats();
    IDbConnection OpenJobsDb();
    IDbConnection OpenJobsMonthDb(DateTime createdDate);
    JobResult? GetJob(long jobId);
    JobResult? GetJobByRefId(string refId);
    object CreateRequest(BackgroundJobBase job);
    object? CreateResponse(BackgroundJobBase job);

    void RecurringApi(string taskName, Schedule schedule, object requestDto, BackgroundJobOptions? options = null);
    void RecurringCommand(string taskName, Schedule schedule, string commandName, object arg, BackgroundJobOptions? options = null);
    void DeleteRecurringTask(string taskName);
    int? GetCommandEstimatedDurationMs(string commandType);
    int? GetApiEstimatedDurationMs(string requestType);
}

public class BackgroundJobRef(long id, string refId)
{
    public long Id { get; } = id;
    public string RefId { get; } = refId;
    public void Deconstruct(out long id, out string refId)
    {
        id = this.Id;
        refId = this.RefId;
    }
}

public struct BackgroundJobStatusUpdate(BackgroundJob job, double? progress=null, string? status=null, string? log=null)
{
    public BackgroundJob Job { get; } = job;
    public double? Progress { get; } = progress;
    public string? Status { get; } = status;
    public string? Log { get; } = log;

    public void Deconstruct(out BackgroundJob job, out double? progress, out string? status, out string? log)
    {
        job = this.Job;
        progress = this.Progress;
        status = this.Status;
        log = this.Log;
    }
}

public class BackgroundJobOptions
{
    /// <summary>
    /// Specify a user-defined UUID for the Job
    /// </summary>
    public string? RefId { get; set; }
    /// <summary>
    /// Maintain a Reference to a parent Job
    /// </summary>
    public long? ParentId { get; set; }
    /// <summary>
    /// Named Worker Thread to execute Job ob  
    /// </summary>
    public string? Worker { get; set; }
    /// <summary>
    /// Only run Job after date
    /// </summary>
    public DateTime? RunAfter { get; set; }
    /// <summary>
    /// Command to Execute after successful completion of Job
    /// </summary>
    public string? Callback { get; set; }
    /// <summary>
    /// Only execute job after successful completion of Parent Job
    /// </summary>
    public long? DependsOn { get; set; }
    /// <summary>
    /// The ASP .NET Identity Auth User Id to populate the IRequest Context ClaimsPrincipal and User Session
    /// </summary>
    public string? UserId { get; set; }
    /// <summary>
    /// How many times to attempt to retry Job on failure, default 2 (BackgroundsJobFeature.DefaultRetryLimit)
    /// </summary>
    public virtual int? RetryLimit { get; set; }
    /// <summary>
    /// Maintain a reference to a callback URL
    /// </summary>
    public string? ReplyTo { get; set; }
    /// <summary>
    /// Associate Job with a tag group
    /// </summary>
    public string? Tag { get; set; }
    public virtual string? BatchId { get; set; }
    public string? CreatedBy { get; set; }
    public int? TimeoutSecs { get; set; }
    public Dictionary<string, string>? Args { get; set; } //= Provider

    /// <summary>
    /// Whether command should be run and not persisted
    /// </summary>
    public bool? RunCommand { get; set; }
    [IgnoreDataMember]
    public Action<object?>? OnSuccess { get; set; }
    [IgnoreDataMember]
    public Action<Exception>? OnFailed { get; set; }
    [IgnoreDataMember]
    public CancellationToken? Token { get; set; } 
}

public class WorkerStats
{
    public string Name { get; set; } = null!;
    public long Queued { get; set; }
    public long Received { get; set; }
    public long Completed { get; set; }
    public long Retries { get; set; }
    public long Failed { get; set; }
    public TimeSpan? RunningTime { get; set; }
}
