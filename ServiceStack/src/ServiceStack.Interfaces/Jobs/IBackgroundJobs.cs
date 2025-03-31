 #nullable enable

using System;
using System.Data;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Jobs;

/// <summary>
/// Provides methods for managing background jobs, including enqueueing, running, canceling,
/// and monitoring jobs.
/// </summary>
public interface IBackgroundJobs
{
    /// <summary>
    /// Enqueues an API request as a background job.
    /// </summary>
    BackgroundJobRef EnqueueApi(object requestDto, BackgroundJobOptions? options = null);
    /// <summary>
    /// Enqueues a command as a background job.
    /// </summary>
    BackgroundJobRef EnqueueCommand(string commandName, object arg, BackgroundJobOptions? options = null);
    /// <summary>
    /// Executes a transient (i.e. non-durable) command and returns immediately with a Reference
    /// to the Executing Job
    /// </summary>
    BackgroundJob RunCommand(string commandName, object arg, BackgroundJobOptions? options = null);
    /// <summary>
    /// Executes a transient (i.e. non-durable) command that waits until the command is executed
    /// and returns the command result if any
    /// </summary>
    Task<object?> RunCommandAsync(string commandName, object arg, BackgroundJobOptions? options = null);
    /// <summary>
    /// Used by Background Workers to execute a Job 
    /// </summary>
    Task ExecuteJobAsync(BackgroundJob job);
    /// <summary>
    /// Cancels a running job by its Id, returns true if a running job was cancelled
    /// </summary>
    bool CancelJob(long jobId);
    /// <summary>
    /// Cancel all Jobs with the specified state or the specified worker
    /// </summary>
    /// <returns>How many jobs were cancelled</returns>
    List<long> CancelJobs(BackgroundJobState? state = null, string? worker = null);
    /// <summary>
    /// Cancels a named Background Worker and transfers any pending queues to a new worker
    /// </summary>
    void CancelWorker(string worker);
    /// <summary>
    /// Requeues a failed job.
    /// </summary>
    void RequeueFailedJob(long jobId);
    /// <summary>
    /// Marks a job as failed due to an exception.
    /// </summary>
    void FailJob(BackgroundJob job, Exception ex);
    /// <summary>
    /// Marks a job as failed that can optionally not be retried
    /// </summary>
    void FailJob(BackgroundJob job, ResponseStatus error, bool shouldRetry);
    /// <summary>
    /// Marks a job as completed and transfers it from Job Queue to CompletedJob table in Monthly DB
    /// </summary>
    void CompleteJob(BackgroundJob job, object? response = null);
    /// <summary>
    /// Update a running jobs status
    /// </summary>
    /// <param name="status"></param>
    void UpdateJobStatus(BackgroundJobStatusUpdate status);
    /// <summary>
    /// Run Startup tasks to populate Job Queue with incomplete tasks 
    /// </summary>
    Task StartAsync(CancellationToken stoppingToken);
    /// <summary>
    /// Runs monitoring and periodic tasks 
    /// </summary>
    Task TickAsync();
    /// <summary>
    /// Get all named workers with their active queue counts 
    /// </summary>
    Dictionary<string, int> GetWorkerQueueCounts();
    /// <summary>
    /// Get execution stats of all named workers
    /// </summary>
    List<WorkerStats> GetWorkerStats();
    /// <summary>
    /// Returns an open ADO .NET Connection to the jobs.db
    /// </summary>
    IDbConnection OpenDb();
    /// <summary>
    /// Returns an open ADO .NET Connection to the monthly jobs.db indicated by CreatedDate
    /// </summary>
    IDbConnection OpenMonthDb(DateTime createdDate);
    /// <summary>
    /// Retrieves a job by its Job Id
    /// </summary>
    JobResult? GetJob(long jobId);
    /// <summary>
    /// Retrieves a job by its unique Ref Id
    /// </summary>
    JobResult? GetJobByRefId(string refId);
    /// <summary>
    /// Rehydrates the Request DTO from a persisted Background Job
    /// </summary>
    object CreateRequest(BackgroundJobBase job);
    /// <summary>
    /// Rehydrates the Response from a persisted Background Job
    /// </summary>
    object? CreateResponse(BackgroundJobBase job);
    /// <summary>
    /// Schedules a recurring API task.
    /// </summary>
    void RecurringApi(string taskName, Schedule schedule, object requestDto, BackgroundJobOptions? options = null);
    /// <summary>
    /// Schedules a recurring command task.
    /// </summary>
    void RecurringCommand(string taskName, Schedule schedule, string commandName, object arg, BackgroundJobOptions? options = null);
    /// <summary>
    /// Deletes a recurring task.
    /// </summary>
    void DeleteRecurringTask(string taskName);
    /// <summary>
    /// Returns the estimated duration of a Command Job in milliseconds
    /// </summary>
    int? GetCommandEstimatedDurationMs(string commandType, string? worker=null);
    /// <summary>
    /// Returns the estimated duration of an API Job in milliseconds
    /// </summary>
    int? GetApiEstimatedDurationMs(string requestType, string? worker=null);
}

/// <summary>
/// Reference of a Queued Job 
/// </summary>
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

/// <summary>
/// Status Update of a Job
/// </summary>
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

/// <summary>
/// Customize Queued Job Options
/// </summary>
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
    /// Named Worker Thread to execute Job on  
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
    public TimeSpan? Timeout
    {
        get => TimeoutSecs.HasValue ? TimeSpan.FromSeconds(TimeoutSecs.Value) : null;
        set => TimeoutSecs = value.HasValue ? (int)value.Value.TotalSeconds : null;
    }
    
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

/// <summary>
/// Captures Stats of a Background Job Worker
/// </summary>
public class WorkerStats
{
    public string Name { get; set; } = null!;
    public long Queued { get; set; }
    public long Received { get; set; }
    public long Completed { get; set; }
    public long Retries { get; set; }
    public long Failed { get; set; }
    public long? RunningJob { get; set; }
    public TimeSpan? RunningTime { get; set; }
}
