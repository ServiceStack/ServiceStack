#nullable enable

using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceStack.Jobs;

public interface IBackgroundJobs
{
    BackgroundJobRef EnqueueApi(string requestDto, object request, BackgroundJobOptions? options = null);
    BackgroundJobRef EnqueueCommand(string commandName, object arg, BackgroundJobOptions? options = null);
    BackgroundJob ExecuteTransientCommand(string commandName, object arg, BackgroundJobOptions? options = null);
    Task ExecuteJobAsync(BackgroundJob job);
    Task CancelJobAsync(BackgroundJob job);
    Task FailJobAsync(BackgroundJob job, Exception ex);
    Task FailJobAsync(BackgroundJob job, ResponseStatus error, bool shouldRetry);
    Task CompleteJobAsync(BackgroundJob job, object? response=null);
    void UpdateJobStatus(BackgroundJobStatusUpdate status);
    Task StartAsync(System.Threading.CancellationToken stoppingToken);
    Task TickAsync();
    Dictionary<string, int> GetWorkerQueueCounts();
    List<WorkerStats> GetWorkerStats();
    IDbConnection OpenJobsDb();
    IDbConnection OpenJobsMonthDb(DateTime createdDate);
    JobResult? GetJob(long jobId);
    Task<JobResult?> GetJobAsync(long jobId);
    object CreateRequest(BackgroundJobBase job);
    object? CreateResponse(BackgroundJobBase job);
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

public class BackgroundJobStatusUpdate(BackgroundJob job, double? progress=null, string? status=null, string? log=null)
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
    /// <summary>
    /// Associate Job with a tag group
    /// </summary>
    public string? CreatedBy { get; set; }
    public int? TimeoutSecs { get; set; }
    public Dictionary<string, string>? Args { get; set; } //= Provider
    public Action<object?>? OnSuccess { get; set; }
    public Action<Exception>? OnFailed { get; set; }
}

public class WorkerStats
{
    public string Name { get; set; } = null!;
    public long Queued { get; set; }
    public long Received { get; set; }
    public long Completed { get; set; }
    public long Retries { get; set; }
    public long Failed { get; set; }
}

/// <summary>
/// Execute AutoQuery Create/Update/Delete Request DTO in a background thread
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class WorkerAttribute : AttributeBase
{
    public string Name { get; set; }
    public WorkerAttribute(string name) => Name = name;
}
