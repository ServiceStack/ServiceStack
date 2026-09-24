#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Jobs;

[CompositeIndex(nameof(CompletedDate), nameof(RequestId), nameof(RunAfter))]
public abstract class BackgroundJobBase : IMeta
{
    /// <summary>Unique Id of the Job, shared by its JobSummary and its archived CompletedJob or FailedJob</summary>
    public virtual long Id { get; set; }
    /// <summary>Id of the Job this Job was queued from, e.g. the parent a dependent Job ran after</summary>
    public virtual long? ParentId { get; set; }
    /// <summary>
    /// Unique user-specified or system generated GUID for Job
    /// </summary>
    [Index(Unique = true)] public virtual string? RefId { get; set; }
    /// <summary>
    /// Named Worker Thread to execute Job on  
    /// </summary>
    public virtual string? Worker { get; set; }
    /// <summary>Logical queue used to separate classes of work.</summary>
    [StringLength(100)]
    [Default("'" + JobQueues.Default + "'")]
    public virtual string Queue { get; set; } = JobQueues.Default;
    /// <summary>Higher values are dispatched first within a queue.</summary>
    [Default(0)]
    public virtual int Priority { get; set; }
    /// <summary>
    /// Associate Job with a tag group
    /// </summary>
    public virtual string? Tag { get; set; }
    /// <summary>Id of the JobBatch this Job belongs to</summary>
    public virtual string? BatchId { get; set; }
    /// <summary>
    /// Command to Execute after successful completion of Job
    /// </summary>
    public virtual string? Callback { get; set; }
    /// <summary>
    /// Only execute job after successful completion of Parent Job
    /// </summary>
    [Index]
    public virtual long? DependsOn { get; set; }
    /// <summary>
    /// Only execute this Job after every Job in this Batch has finished
    /// </summary>
    [StringLength(100)]
    [Index]
    public virtual string? DependsOnBatch { get; set; }
    /// <summary>
    /// Jobs sharing a ConcurrencyKey are executed one at a time, while Jobs with different keys
    /// still run in parallel, e.g. to keep each tenant's Jobs in order without serialising everyone.
    /// </summary>
    [StringLength(200)]
    [Index]
    public virtual string? ConcurrencyKey { get; set; }
    /// <summary>
    /// Whether this Job only runs when the Job it DependsOn completes (default), or once it has
    /// finished in any state
    /// </summary>
    public virtual JobDependencyPolicy? DependsOnPolicy { get; set; }
    /// <summary>The tenant this Job belongs to, for multi-tenant filtering and reporting</summary>
    [StringLength(100)]
    public virtual string? TenantId { get; set; }
    /// <summary>
    /// W3C traceparent of the request that queued this Job, so its execution continues that trace
    /// </summary>
    [StringLength(100)]
    public virtual string? TraceId { get; set; }
    /// <summary>
    /// Only run Job after date
    /// </summary>
    public virtual DateTime? RunAfter { get; set; }
    /// <summary>When the Job was queued</summary>
    [Index] public virtual DateTime CreatedDate { get; set; }
    /// <summary>The user or process that queued the Job</summary>
    public virtual string? CreatedBy { get; set; }
    /// <summary>
    /// Batch Id for marking dispatched jobs
    /// </summary>
    public virtual string? RequestId { get; set; }
    /// <summary>
    /// API or CMD
    /// </summary>
    public virtual string RequestType { get; set; }
    /// <summary>
    /// The Command to Execute
    /// </summary>
    public virtual string? Command { get; set; }
    /// <summary>
    /// The Request DTO or Command Argument
    /// </summary>
    public virtual string Request { get; set; }

    /// <summary>
    /// JSON Body of Request
    /// </summary>
    [StringLength(StringLengthAttribute.MaxText)]
    public virtual string RequestBody { get; set; }

    /// <summary>
    /// The ASP .NET Identity Auth User Id to populate the IRequest Context ClaimsPrincipal and User Session
    /// </summary>
    public virtual string? UserId { get; set; }
    
    /// <summary>
    /// The Response DTO Name
    /// </summary>
    public virtual string? Response { get; set; }

    /// <summary>
    /// The Response DTO JSON Body
    /// </summary>
    [StringLength(StringLengthAttribute.MaxText)]
    public virtual string? ResponseBody { get; set; }
    /// <summary>
    /// The state the Job is in
    /// </summary>
    public virtual BackgroundJobState State { get; set; }

    /// <summary>
    /// The day the Job was started
    /// </summary>
    public virtual DateTime? StartedDate { get; set; }
    
    /// <summary>
    /// When the Job was completed
    /// </summary>
    public virtual DateTime? CompletedDate { get; set; }
    
    /// <summary>
    /// When the Job with Callback was notified
    /// </summary>
    public virtual DateTime? NotifiedDate { get; set; }
    
    /// <summary>
    /// How many times to attempt to retry Job on failure, default 2 (BackgroundsJobFeature.DefaultRetryLimit)
    /// </summary>
    public virtual int? RetryLimit { get; set; }
    /// <summary>How the delay between retries grows, defaults to the feature's DefaultRetryBackoff</summary>
    public virtual RetryBackoff? RetryBackoff { get; set; }
    /// <summary>Base delay before the first retry in milliseconds, defaults to the feature's DefaultRetryDelayMs</summary>
    public virtual int? RetryDelayMs { get; set; }
    /// <summary>The longest delay between retries in milliseconds, defaults to the feature's DefaultMaxRetryDelayMs</summary>
    public virtual int? MaxRetryDelayMs { get; set; }
    /// <summary>How many times the Job has been attempted, starting from 1</summary>
    public virtual int Attempts { get; set; }
    /// <summary>How long the Job took to execute in milliseconds</summary>
    public virtual int DurationMs { get; set; }
    /// <summary>Cancel the Job if it runs longer than this, defaults to the feature's DefaultTimeoutSecs</summary>
    public virtual int? TimeoutSecs { get; set; }
    /// <summary>Progress of the Job from 0-1, reported by the Job while it runs</summary>
    public virtual double? Progress { get; set; }
    /// <summary>Status message reported by the Job while it runs, e.g. Downloaded 2/10</summary>
    public virtual string? Status { get; set; }
    /// <summary>Log messages recorded by the Job, bounded by the feature's MaxJobLogChars</summary>
    [StringLength(StringLengthAttribute.MaxText)]
    public virtual string? Logs { get; set; }
    /// <summary>Set when Logs exceeded MaxJobLogChars and later messages were dropped</summary>
    public virtual bool? LogsTruncated { get; set; }
    /// <summary>When the Job last reported progress, status or logs, or its lease was renewed</summary>
    public virtual DateTime? LastActivityDate { get; set; }
    /// <summary>When cancellation of the Job was requested</summary>
    public virtual DateTime? CancelRequestedDate { get; set; }
    /// <summary>
    /// Don't execute this Job after this date. A Job that's still queued when it expires is
    /// cancelled with the JobExpired error code instead of being run late.
    /// </summary>
    public virtual DateTime? ExpiresAt { get; set; }
    /// <summary>Id of the server that last executed this Job. Only populated by the RDBMS provider.</summary>
    [StringLength(100)]
    public virtual string? LeaseOwner { get; set; }
    /// <summary>
    /// Where to send the Job's result when it completes, handled by BackgroundJobsFeature.OnJobReplyTo.
    /// Defaults to an MQ Queue Name, or an http:// or https:// URL to POST the result to.
    /// </summary>
    public virtual string? ReplyTo { get; set; }
    /// <summary>Error Code of the last failure</summary>
    public virtual string? ErrorCode { get; set; }

    /// <summary>Error details of the last failure</summary>
    public virtual ResponseStatus? Error { get; set; }
    /// <summary>Additional user-defined arguments for the Job</summary>
    public virtual Dictionary<string, string>? Args { get; set; }
    //[Exclude]
    /// <summary>Additional metadata recorded against the Job, e.g. JobMetaKeys.ResponseBodyOmitted</summary>
    public virtual Dictionary<string, string>? Meta { get; set; }    
}

// App DB
[Icon(Svg = SvgIcons.Tasks)]
public class BackgroundJob : BackgroundJobBase
{
    /// <summary>Unique auto-incrementing Id of the Job</summary>
    [AutoIncrement] public override long Id { get; set; }

    /// <summary>
    /// Only allow a single active Job with this key to be queued or running at a time.
    /// Enforced by a unique index on the active Jobs table.
    /// </summary>
    [StringLength(200)]
    public virtual string? SingletonKey { get; set; }

    /// <summary>Fencing token for the current execution claim. SQLite leaves lease fields unset.</summary>
    [StringLength(32)]
    public virtual string? LeaseToken { get; set; }
    /// <summary>When the current execution claim expires unless renewed, after which another node can recover the Job</summary>
    public virtual DateTime? LeaseExpiresAt { get; set; }

    /// <summary>Whether the Job is executed in-memory without being persisted, e.g. from RunCommand()</summary>
    [Ignore, IgnoreDataMember] public bool Transient { get; set; }
    /// <summary>The Request DTO of a Transient Job</summary>
    [Ignore, IgnoreDataMember] public object? TransientRequest { get; set; }
    /// <summary>The Response of a Transient Job</summary>
    [Ignore, IgnoreDataMember] public object? TransientResponse { get; set; }
    /// <summary>The completed Job this Job DependsOn, available when it executes</summary>
    [Ignore, IgnoreDataMember] public CompletedJob? ParentJob { get; set; }
    /// <summary>Callback invoked with the Response when the Job completes, only available in the process that queued it</summary>
    [Ignore, IgnoreDataMember] public Action<object?>? OnSuccess { get; set; }
    /// <summary>Callback invoked with the Exception when the Job fails, only available in the process that queued it</summary>
    [Ignore, IgnoreDataMember] public Action<Exception>? OnFailed { get; set; }
    /// <summary>Cancellation Token to cancel the Job with, only available in the process that queued it</summary>
    [Ignore, IgnoreDataMember] public CancellationToken? Token { get; set; }
    /// <summary>Whether queueing a Job with a RefId already in use throws or returns the existing Job</summary>
    [Ignore, IgnoreDataMember] public DuplicateRefIdBehavior DuplicateRefIdBehavior { get; set; }
}

[Icon(Svg = SvgIcons.Stats)]
public class JobSummary
{
    /// <summary>Id of the Job this summarises</summary>
    public virtual long Id { get; set; }
    /// <summary>Id of the Job this Job was queued from</summary>
    public virtual long? ParentId { get; set; }
    /// <summary>Unique user-specified or system generated GUID for Job</summary>
    [Index(Unique = true)] public virtual string? RefId { get; set; }
    /// <summary>Named Worker Thread the Job executes on</summary>
    public virtual string? Worker { get; set; }
    /// <summary>Logical queue the Job was queued on</summary>
    [StringLength(100)]
    [Default("'" + JobQueues.Default + "'")]
    public virtual string Queue { get; set; } = JobQueues.Default;
    /// <summary>Higher values are dispatched first within a queue</summary>
    [Default(0)]
    public virtual int Priority { get; set; }
    /// <summary>Tag group the Job is associated with</summary>
    public virtual string? Tag { get; set; }
    /// <summary>Id of the JobBatch this Job belongs to</summary>
    [Index]
    public virtual string? BatchId { get; set; }
    /// <summary>When the Job was queued</summary>
    [Index]
    public virtual DateTime CreatedDate { get; set; }
    /// <summary>The user or process that queued the Job</summary>
    public virtual string? CreatedBy { get; set; }
    /// <summary>API or CMD</summary>
    public virtual string RequestType { get; set; }
    /// <summary>The Command to Execute</summary>
    public virtual string? Command { get; set; }
    /// <summary>The Request DTO or Command Argument Type Name</summary>
    public virtual string Request { get; set; }
    /// <summary>The Response DTO Name</summary>
    public virtual string? Response { get; set; }
    /// <summary>The ASP .NET Identity Auth User Id the Job executes as</summary>
    public virtual string? UserId { get; set; }
    /// <summary>Command to Execute after successful completion of Job</summary>
    public virtual string? Callback { get; set; }
    /// <summary>When the Job was started</summary>
    public virtual DateTime? StartedDate { get; set; }
    /// <summary>When the Job finished</summary>
    public virtual DateTime? CompletedDate { get; set; }
    /// <summary>The state the Job is in</summary>
    public virtual BackgroundJobState State { get; set; }
    /// <summary>How long the Job took to execute in milliseconds</summary>
    public virtual int DurationMs { get; set; }
    /// <summary>How many times the Job has been attempted</summary>
    public virtual int Attempts { get; set; }
    /// <summary>Only run Job after date</summary>
    public virtual DateTime? RunAfter { get; set; }
    /// <summary>When cancellation of the Job was requested</summary>
    public virtual DateTime? CancelRequestedDate { get; set; }
    /// <summary>Don't execute the Job after this date</summary>
    public virtual DateTime? ExpiresAt { get; set; }
    /// <summary>Jobs sharing a ConcurrencyKey are executed one at a time</summary>
    [StringLength(200)]
    public virtual string? ConcurrencyKey { get; set; }
    /// <summary>SingletonKey the Job was queued with, kept after it finishes</summary>
    [StringLength(200)]
    [Index]
    public virtual string? SingletonKey { get; set; }
    /// <summary>The tenant this Job belongs to, for multi-tenant filtering and reporting</summary>
    [StringLength(100)]
    [Index]
    public virtual string? TenantId { get; set; }
    /// <summary>W3C traceparent of the request that queued this Job</summary>
    [StringLength(100)]
    public virtual string? TraceId { get; set; }
    /// <summary>Id of the server that last executed this Job</summary>
    [StringLength(100)]
    public virtual string? LeaseOwner { get; set; }
    /// <summary>Set when the Job's Logs exceeded MaxJobLogChars and later messages were dropped</summary>
    public virtual bool? LogsTruncated { get; set; }
    /// <summary>Error Code of the last failure</summary>
    public virtual string? ErrorCode { get; set; }
    /// <summary>Error Message of the last failure</summary>
    public virtual string? ErrorMessage { get; set; }
    /// <summary>Additional metadata recorded against the Job</summary>
    public virtual Dictionary<string, string>? Meta { get; set; }
}

/// <summary>
/// Tracks the progress of a group of Jobs queued with the same BatchId
/// </summary>
[Icon(Svg = SvgIcons.Stats)]
public class JobBatch
{
    /// <summary>User-specified or generated Id shared by every Job in the batch</summary>
    [PrimaryKey]
    [StringLength(100)]
    public virtual string Id { get; set; }
    /// <summary>Description of the batch shown in the Admin UI</summary>
    [StringLength(200)]
    public virtual string? Description { get; set; }
    /// <summary>Total Jobs expected in this batch, when known up-front</summary>
    public virtual int? Total { get; set; }
    /// <summary>Jobs in the batch that haven't finished yet</summary>
    public virtual int Queued { get; set; }
    /// <summary>Jobs in the batch that completed successfully</summary>
    public virtual int Completed { get; set; }
    /// <summary>Jobs in the batch that failed</summary>
    public virtual int Failed { get; set; }
    /// <summary>Jobs in the batch that were cancelled</summary>
    public virtual int Cancelled { get; set; }
    /// <summary>Command to execute once every Job in the batch has finished, in any state</summary>
    public virtual string? Callback { get; set; }
    /// <summary>Command to execute once every Job in the batch has completed successfully</summary>
    public virtual string? OnSuccess { get; set; }
    /// <summary>Groups this batch under a parent batch</summary>
    [StringLength(100)]
    [Index]
    public virtual string? ParentBatchId { get; set; }
    /// <summary>Set once the Callback has been queued, so it only ever runs once</summary>
    public virtual DateTime? NotifiedDate { get; set; }
    /// <summary>When the batch was created</summary>
    [Index]
    public virtual DateTime CreatedDate { get; set; }
    /// <summary>The user or process that created the batch</summary>
    public virtual string? CreatedBy { get; set; }
    /// <summary>Set when the last Job in the batch finished</summary>
    public virtual DateTime? CompletedDate { get; set; }
    /// <summary>Set when the batch was cancelled, after which no more Jobs can be added to it</summary>
    public virtual DateTime? CancelledDate { get; set; }
    /// <summary>Additional user-defined metadata for the batch</summary>
    public virtual Dictionary<string, string>? Meta { get; set; }

    /// <summary>Jobs in this batch that have finished, whether they succeeded or not</summary>
    [Ignore] public int Finished => Completed + Failed + Cancelled;
    /// <summary>Progress from 0-1 when the batch Total is known</summary>
    [Ignore] public double? Progress => Total is > 0 ? (double)Finished / Total.Value : null;
}

/// <summary>
/// Holds the exclusive execution slot for a ConcurrencyKey. The primary key is what guarantees
/// only one Job per key runs at a time, across every node.
/// </summary>
public class JobConcurrencyLock
{
    /// <summary>The ConcurrencyKey this slot is held for</summary>
    [PrimaryKey]
    [StringLength(200)]
    public virtual string Key { get; set; }
    /// <summary>Id of the Job holding the slot</summary>
    public virtual long JobId { get; set; }
    /// <summary>Id of the server that took the slot</summary>
    [StringLength(100)]
    public virtual string? LeaseOwner { get; set; }
    /// <summary>Released automatically once this passes, so a dead node can't hold a key forever</summary>
    [Index]
    public virtual DateTime ExpiresAt { get; set; }
    /// <summary>When the slot was taken</summary>
    public virtual DateTime AcquiredDate { get; set; }
}

/// <summary>
/// An App Server processing Jobs, so operators can see which nodes are alive and what they're doing
/// </summary>
[Icon(Svg = SvgIcons.Stats)]
public class JobNode
{
    /// <summary>Unique Id of the App Server, from the feature's ServerId</summary>
    [PrimaryKey]
    [StringLength(100)]
    public virtual string ServerId { get; set; }
    /// <summary>Name of the machine the App Server runs on</summary>
    [StringLength(100)]
    public virtual string? MachineName { get; set; }
    /// <summary>Process Id of the App Server</summary>
    public virtual int ProcessId { get; set; }
    /// <summary>ServiceStack version the App Server is running</summary>
    [StringLength(50)]
    public virtual string? Version { get; set; }
    /// <summary>When the App Server started processing Jobs</summary>
    public virtual DateTime StartedDate { get; set; }
    /// <summary>When the App Server last reported it was alive</summary>
    [Index]
    public virtual DateTime LastHeartbeat { get; set; }
    /// <summary>Jobs this node is currently executing</summary>
    public virtual int RunningJobs { get; set; }
    /// <summary>Max concurrent Jobs this node is configured to run</summary>
    public virtual int Concurrency { get; set; }
    /// <summary>Set when the node shut down cleanly</summary>
    public virtual DateTime? StoppedDate { get; set; }
    /// <summary>
    /// When true the node finishes the Jobs it has but doesn't take any more, e.g. before a deploy
    /// </summary>
    [Default("{FALSE}")]
    public virtual bool Draining { get; set; }
    /// <summary>Queues this node processes, null for every queue</summary>
    public virtual List<string>? Queues { get; set; }
    /// <summary>Additional metadata for the App Server</summary>
    public virtual Dictionary<string, string>? Meta { get; set; }

    /// <summary>Whether this node's heartbeat is recent enough for it to be considered alive</summary>
    public bool IsAlive(TimeSpan within) =>
        StoppedDate == null && DateTime.UtcNow - LastHeartbeat <= within;
}

/// <summary>
/// Runtime controls for a Job Queue, allowing a queue to be paused or throttled without a redeploy
/// </summary>
[Icon(Svg = SvgIcons.Tasks)]
public class JobQueue
{
    /// <summary>Queue Name, e.g. `default`</summary>
    [PrimaryKey]
    [StringLength(100)]
    public virtual string Name { get; set; }
    /// <summary>When true, Jobs on this queue are not dispatched to a Worker</summary>
    [Default("{FALSE}")]
    public virtual bool Paused { get; set; }
    /// <summary>Overrides the configured concurrency for this queue when set</summary>
    public virtual int? Concurrency { get; set; }
    /// <summary>
    /// Max Jobs to start per RateLimitSecs, for work bound by a third-party quota. Unlike
    /// Concurrency, which limits simultaneous Jobs, this limits how often they may start.
    /// </summary>
    public virtual int? RateLimit { get; set; }
    /// <summary>The window RateLimit applies over, defaults to 1 second</summary>
    public virtual int? RateLimitSecs { get; set; }
    /// <summary>Start of the current RateLimit window, shared by every node</summary>
    public virtual DateTime? RateLimitWindowStart { get; set; }
    /// <summary>Jobs started in the current RateLimit window across every node</summary>
    public virtual int? RateLimitCount { get; set; }
    /// <summary>Notes about the queue, e.g. why it was paused</summary>
    [StringLength(200)]
    public virtual string? Notes { get; set; }
    /// <summary>When the queue's controls were last changed</summary>
    public virtual DateTime? ModifiedDate { get; set; }
    /// <summary>Who last changed the queue's controls</summary>
    [StringLength(100)]
    public virtual string? ModifiedBy { get; set; }
    /// <summary>Additional metadata for the queue</summary>
    public virtual Dictionary<string, string>? Meta { get; set; }
}

/// <summary>
/// A failed attempt at executing a Job. A Job only records its latest error, so this keeps the
/// history of every failure, e.g. to diagnose a Job that fails intermittently.
/// </summary>
[Icon(Svg = SvgIcons.Stats)]
public class JobAttempt
{
    /// <summary>Unique auto-incrementing Id of the attempt</summary>
    [AutoIncrement]
    public virtual long Id { get; set; }
    /// <summary>Id of the Job that was attempted</summary>
    [Index]
    public virtual long JobId { get; set; }
    /// <summary>Which attempt failed, starting from 1</summary>
    public virtual int Attempt { get; set; }
    /// <summary>What happened to the Job after this attempt, e.g. Queued to be retried or Failed</summary>
    public virtual BackgroundJobState State { get; set; }
    /// <summary>The server that executed the attempt</summary>
    [StringLength(100)]
    public virtual string? ServerId { get; set; }
    /// <summary>When the attempt started</summary>
    public virtual DateTime? StartedDate { get; set; }
    /// <summary>How long the attempt ran for in milliseconds</summary>
    public virtual int DurationMs { get; set; }
    /// <summary>Error Code the attempt failed with</summary>
    [StringLength(100)]
    public virtual string? ErrorCode { get; set; }
    /// <summary>Error details the attempt failed with</summary>
    public virtual ResponseStatus? Error { get; set; }
    /// <summary>When the attempt was recorded</summary>
    [Index]
    public virtual DateTime CreatedDate { get; set; }
}

public enum BackgroundJobState
{
    Queued,
    Started,
    Executed,
    Completed, // Callback Notified
    Failed,
    Cancelled,
}

// Month DB
[Icon(Svg = SvgIcons.Completed)]
public class CompletedJob : BackgroundJobBase {}

[Icon(Svg = SvgIcons.Failed)]
public class FailedJob : BackgroundJobBase {}

public class JobResult
{
    /// <summary>Summary of the Job</summary>
    public JobSummary Summary { get; set; } = null!;
    /// <summary>The Job while it's still queued or running</summary>
    public BackgroundJob? Queued { get; set; }
    /// <summary>The archived Job once it completed</summary>
    public CompletedJob? Completed { get; set; }
    /// <summary>The archived Job once it failed or was cancelled</summary>
    public FailedJob? Failed { get; set; }
    /// <summary>Whichever of Queued, Completed or Failed holds the Job</summary>
    [IgnoreDataMember] public BackgroundJobBase? Job => 
        (BackgroundJobBase?)Queued ?? (BackgroundJobBase?)Completed ?? Failed;
}

public enum RetryBackoff
{
    Fixed,
    Linear,
    Exponential,
    ExponentialJitter,
}

public enum JobDependencyPolicy
{
    /// <summary>Only run when the Job it depends on completes successfully, otherwise it's cancelled</summary>
    OnSuccess,
    /// <summary>Run once the Job it depends on has finished, whether it completed, failed or was cancelled</summary>
    OnFinished,
}

public enum DuplicateRefIdBehavior
{
    Throw,
    ReturnExisting,
}

public static class JobQueues
{
    public const string Default = "default";
}

/// <summary>
/// Error Codes used by Background Jobs that customers can branch on
/// </summary>
public static class JobErrorCodes
{
    /// <summary>The Job wasn't started before its ExpiresAt</summary>
    public const string JobExpired = nameof(JobExpired);
    /// <summary>The Job was abandoned mid-execution more times than its RetryLimit allows</summary>
    public const string LeaseExpired = nameof(LeaseExpired);
    /// <summary>The Job was cleared by the Background Jobs schema upgrade</summary>
    public const string QueueClearedOnUpgrade = nameof(QueueClearedOnUpgrade);
}

/// <summary>
/// A point-in-time view of the Job backlog, for health checks and monitoring
/// </summary>
public class JobsStatus
{
    /// <summary>Jobs waiting to run, including ones scheduled to run later</summary>
    public int Queued { get; set; }
    /// <summary>Jobs currently executing</summary>
    public int Running { get; set; }
    /// <summary>How long the oldest due Job has been waiting to start</summary>
    public TimeSpan? OldestQueued { get; set; }
    /// <summary>App Servers that have processed Jobs</summary>
    public int Nodes { get; set; }
    /// <summary>App Servers with a recent heartbeat</summary>
    public int NodesAlive { get; set; }
}

public static class JobMetaKeys
{
    /// <summary>Set to the length of a Response too large to persist</summary>
    public const string ResponseBodyOmitted = nameof(ResponseBodyOmitted);
}

/// <summary>
/// Thrown when a Job is queued with a RefId that's already in use by a different Job
/// </summary>
public class DuplicateRefIdException(string refId)
    : Exception($"RefId '{refId}' already belongs to a different Job")
{
    /// <summary>The RefId that's already in use</summary>
    public string RefId { get; } = refId;
}
