#nullable enable
using System;
using System.Collections.Generic;
using ServiceStack.DataAnnotations;
using ServiceStack.Jobs;

namespace ServiceStack;

[ExcludeMetadata, Tag(TagNames.Jobs)]
public class AdminRequeueFailedJobs : IReturn<AdminRequeueFailedJobsJobsResponse>
{
    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<long>? Ids { get; set; }
    /// <summary>Requeue every failed Job with this tag</summary>
    public string? Tag { get; set; }
    /// <summary>Requeue every failed Job in this batch</summary>
    public string? BatchId { get; set; }
    /// <summary>Only requeue failed Jobs created on or after this date</summary>
    public DateTime? From { get; set; }
}
public class AdminRequeueFailedJobsJobsResponse
{
    public List<long> Results { get; set; } = new();
    public Dictionary<long,string> Errors { get; set; } = new();
    public ResponseStatus? ResponseStatus { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Jobs)]
public class AdminCancelJobs : IGet, IReturn<AdminCancelJobsResponse>
{
    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<long>? Ids { get; set; }
    public string? Worker { get; set; }
    public BackgroundJobState? State { get; set; }
    /// <summary>Cancel every Job on this queue</summary>
    public string? Queue { get; set; }
    /// <summary>Cancel every Job with this tag</summary>
    public string? Tag { get; set; }
    /// <summary>Cancel every Job in this batch</summary>
    public string? BatchId { get; set; }
    public string? CancelWorker { get; set; }
}
public class AdminCancelJobsResponse
{
    public List<long> Results { get; set; } = new();
    public Dictionary<long,string> Errors { get; set; } = new();
    public ResponseStatus? ResponseStatus { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Jobs)]
public class AdminUpdateScheduledTask : IPost, IReturn<AdminUpdateScheduledTaskResponse>
{
    [ValidateGreaterThan(0)]
    public long Id { get; set; }
    /// <summary>Pause or resume the task</summary>
    public bool? Enabled { get; set; }
    /// <summary>Make the task due immediately, without changing its schedule</summary>
    public bool? RunNow { get; set; }
}
public class AdminUpdateScheduledTaskResponse
{
    public ScheduledTask Result { get; set; } = null!;
    public ResponseStatus? ResponseStatus { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Jobs)]
public class AdminGetJobBatch : IGet, IReturn<AdminGetJobBatchResponse>
{
    [ValidateNotEmpty]
    public string BatchId { get; set; } = null!;
}
public class AdminGetJobBatchResponse
{
    public JobBatch? Result { get; set; }
    /// <summary>Counts by state of the Jobs actually recorded against this batch</summary>
    public Dictionary<string, int> StateCounts { get; set; } = new();
    public ResponseStatus? ResponseStatus { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Jobs)]
public class AdminGetJobQueues : IGet, IReturn<AdminGetJobQueuesResponse> {}
public class AdminGetJobQueuesResponse
{
    public List<JobQueueStatus> Results { get; set; } = new();
    public ResponseStatus? ResponseStatus { get; set; }
}

public class JobQueueStatus
{
    public string Name { get; set; } = null!;
    public bool Paused { get; set; }
    /// <summary>Max concurrent Jobs, including any runtime override</summary>
    public int Concurrency { get; set; }
    /// <summary>True when Concurrency was overridden at runtime instead of by configuration</summary>
    public bool ConcurrencyOverridden { get; set; }
    /// <summary>Jobs on this queue waiting to run</summary>
    public int Queued { get; set; }
    /// <summary>Jobs on this queue currently executing on this node</summary>
    public int Running { get; set; }
    /// <summary>Max Jobs that may start per RateLimitSecs across every node</summary>
    public int? RateLimit { get; set; }
    public int? RateLimitSecs { get; set; }
    /// <summary>How long the oldest waiting Job has been waiting</summary>
    public TimeSpan? OldestQueued { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Jobs)]
public class AdminUpdateJobQueue : IPost, IReturn<AdminUpdateJobQueueResponse>
{
    [ValidateNotEmpty]
    public string Name { get; set; } = null!;
    /// <summary>Pause or resume dispatching Jobs on this queue</summary>
    public bool? Paused { get; set; }
    /// <summary>Override how many Jobs this queue runs concurrently</summary>
    public int? Concurrency { get; set; }
    /// <summary>Max Jobs that may start per RateLimitSecs across every node, 0 to remove the limit</summary>
    public int? RateLimit { get; set; }
    /// <summary>The window RateLimit applies over</summary>
    public int? RateLimitSecs { get; set; }
}
public class AdminUpdateJobQueueResponse
{
    public JobQueue Result { get; set; } = null!;
    public ResponseStatus? ResponseStatus { get; set; }
}

/// <summary>
/// The App Servers processing Jobs and when they were last seen
/// </summary>
[ExcludeMetadata, Tag(TagNames.Jobs)]
public class AdminGetJobNodes : IGet, IReturn<AdminGetJobNodesResponse> {}
public class AdminGetJobNodesResponse
{
    public List<JobNode> Results { get; set; } = new();
    public ResponseStatus? ResponseStatus { get; set; }
}

/// <summary>
/// Drain an App Server so it finishes the Jobs it has without taking any more, e.g. before a deploy
/// </summary>
[ExcludeMetadata, Tag(TagNames.Jobs)]
public class AdminUpdateJobNode : IPost, IReturn<AdminUpdateJobNodeResponse>
{
    [ValidateNotEmpty]
    public string ServerId { get; set; } = null!;
    public bool? Draining { get; set; }
}
public class AdminUpdateJobNodeResponse
{
    public JobNode? Result { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

/// <summary>
/// The failed attempts of a Job, oldest first
/// </summary>
[ExcludeMetadata, Tag(TagNames.Jobs)]
public class AdminGetJobAttempts : IGet, IReturn<AdminGetJobAttemptsResponse>
{
    [ValidateGreaterThan(0)]
    public long Id { get; set; }
}
public class AdminGetJobAttemptsResponse
{
    public List<JobAttempt> Results { get; set; } = new();
    public ResponseStatus? ResponseStatus { get; set; }
}

/// <summary>
/// Queues a completed Job to run again with the same arguments
/// </summary>
[ExcludeMetadata, Tag(TagNames.Jobs)]
public class AdminReplayJob : IPost, IReturn<AdminReplayJobResponse>
{
    [ValidateGreaterThan(0)]
    public long Id { get; set; }
}
public class AdminReplayJobResponse
{
    /// <summary>Id of the newly queued Job</summary>
    public long JobId { get; set; }
    public string RefId { get; set; } = null!;
    public ResponseStatus? ResponseStatus { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Jobs)]
public class AdminJobDashboard : IGet, IReturn<AdminJobDashboardResponse>
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public class JobStat
{
    public string Name { get; set; } 
    public BackgroundJobState State { get; set; }
    public bool Retries { get; set; }
    public int Count { get; set; }
}

public class JobStatSummary
{
    public string Name { get; set; }
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Retries { get; set; }
    public int Failed { get; set; }
    public int Cancelled { get; set; }
}

public class AdminJobDashboardResponse
{
    public List<JobStatSummary> Commands { get; set; } = new();
    public List<JobStatSummary> Apis { get; set; } = new();
    public List<JobStatSummary> Workers { get; set; } = new();
    public List<JobStatSummary> Queues { get; set; } = new();
    public List<HourSummary> Today { get; set; } = new();
    /// <summary>How long Jobs waited between being queued and starting</summary>
    public JobWaitTimes WaitTimes { get; set; } = new();
    public ResponseStatus? ResponseStatus { get; set; }
}

/// <summary>
/// Time Jobs spent waiting to start, the first thing to check when Jobs appear to be running late
/// </summary>
public class JobWaitTimes
{
    public int Count { get; set; }
    public int AvgMs { get; set; }
    public int MaxMs { get; set; }
    /// <summary>Longest a Job is currently still waiting to start</summary>
    public int? WaitingMs { get; set; }
}

public class HourStat
{
    public string Hour { get; set; }
    public BackgroundJobState State { get; set; }
    public int Count { get; set; }
}

public class HourSummary
{
    public string Hour { get; set; }
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Failed { get; set; }
    public int Cancelled { get; set; }
}

// Logging
[ExcludeMetadata, Tag(TagNames.Admin), ExplicitAutoQuery]
public class AdminQueryRequestLogs : QueryDb<RequestLog>
{
    public DateTime? Month { get; set; }
}
