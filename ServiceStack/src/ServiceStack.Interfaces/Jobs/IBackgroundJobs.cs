#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceStack.Jobs;

public interface IBackgroundJobs : IDisposable
{
    BackgroundJobRef EnqueueApi(string requestDto, object request, BackgroundJobOptions? options = null);
    BackgroundJobRef EnqueueCommand(string commandName, object arg, BackgroundJobOptions? options = null);
    BackgroundJob ExecuteTransientCommand(string commandName, object arg, BackgroundJobOptions? options = null);
    Task ExecuteJobAsync(BackgroundJob job);
    Task CancelJobAsync(BackgroundJob job);
    Task FailJobAsync(BackgroundJob job, Exception ex);
    Task CompleteJobAsync(BackgroundJob job, object? response=null);
    void UpdateJobStatus(BackgroundJobStatusUpdate status);
    void Start();
    void Tick();
    Dictionary<string, int> GetWorkerQueueCounts();
    List<WorkerStats> GetWorkerStats();
}

public class BackgroundJobRef(long id, string refId, string requestId)
{
    public long Id { get; } = id;
    public string RefId { get; } = refId;
    public string RequestId { get; } = requestId;

    public void Deconstruct(out long id, out string refId, out string requestId)
    {
        id = this.Id;
        refId = this.RefId;
        requestId = this.RequestId;
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
    public string? RefId { get; set; }
    public long? ParentId { get; set; }
    public string? Worker { get; set; } // named or null for Queue BG Thread

    //public int? NoOfThreads { get; set; } // v1 ignore
    public DateTime? RunAfter { get; set; }
    public string? Callback { get; set; }
    public string? ReplyTo { get; set; }
    public string? Tag { get; set; }
    public string? CreatedBy { get; set; }
    public int? TimeoutSecs { get; set; }
    public Dictionary<string, string>? Args { get; set; } //= Provider
    public Action<object>? OnSuccess { get; set; }
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
