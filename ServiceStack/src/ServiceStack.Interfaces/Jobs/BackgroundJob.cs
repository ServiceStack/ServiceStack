#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Jobs;

public abstract class BackgroundJobBase : IMeta
{
    public virtual long Id { get; set; }
    public virtual long? ParentId { get; set; }
    /// <summary>
    /// Unique user-specified or system generated GUID for Job
    /// </summary>
    [Index(Unique = true)] public virtual string? RefId { get; set; }
    /// <summary>
    /// Named Worker Thread to execute Job ob  
    /// </summary>
    public virtual string? Worker { get; set; }
    /// <summary>
    /// Associate Job with a tag group
    /// </summary>
    public virtual string? Tag { get; set; }
    /// <summary>
    /// Command to Execute after successful completion of Job
    /// </summary>
    public virtual string? Callback { get; set; }
    /// <summary>
    /// Only execute job after successful completion of Parent Job
    /// </summary>
    public virtual long? DependsOn { get; set; }
    /// <summary>
    /// Only run Job after date
    /// </summary>
    public virtual DateTime? RunAfter { get; set; }
    public virtual DateTime CreatedDate { get; set; }
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
    public virtual string? ResponseBody { get; set; }
    /// <summary>
    /// The state the Job is in
    /// </summary>
    public virtual BackgroundJobState State { get; set; }

    /// <summary>
    /// The day the Job was started
    /// </summary>
    [Index] public virtual DateTime? StartedDate { get; set; }
    
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
    public virtual int Attempts { get; set; }
    public virtual int DurationMs { get; set; }
    public virtual int? TimeoutSecs { get; set; }
    public virtual double? Progress { get; set; } // 0-1
    public virtual string? Status { get; set; }   // e.g. Downloaded 2/10
    public virtual string? Logs { get; set; }     // Append recorded logs
    public virtual DateTime? LastActivityDate { get; set; }
    public virtual string? ReplyTo { get; set; }
    public virtual string? ErrorCode { get; set; }

    public virtual ResponseStatus? Error { get; set; }
    public virtual Dictionary<string, string>? Args { get; set; }
    //[Exclude]
    public virtual Dictionary<string, string>? Meta { get; set; }    
}

// App DB
[Icon(Svg = SvgIcons.Tasks)]
public class BackgroundJob : BackgroundJobBase
{
    [AutoIncrement] public override long Id { get; set; }

    [Ignore, IgnoreDataMember] public bool Transient { get; set; }
    [Ignore, IgnoreDataMember] public CompletedJob? ParentJob { get; set; }
    [Ignore, IgnoreDataMember] public Action<object?>? OnSuccess { get; set; }
    [Ignore, IgnoreDataMember] public Action<Exception>? OnFailed { get; set; }
    [Ignore, IgnoreDataMember] public CancellationToken? Token { get; set; }
}

[Icon(Svg = SvgIcons.Stats)]
public class JobSummary
{
    public virtual long Id { get; set; }
    public virtual long? ParentId { get; set; }
    [Index(Unique = true)] public virtual string? RefId { get; set; }
    public virtual string? Worker { get; set; }
    public virtual string? Tag { get; set; }
    public virtual DateTime CreatedDate { get; set; }
    public virtual string? CreatedBy { get; set; }
    public virtual string RequestType { get; set; } // API or CMD
    public virtual string? Command { get; set; }
    public virtual string Request { get; set; }
    public virtual string? Response { get; set; }
    public virtual string? UserId { get; set; }
    public virtual string? Callback { get; set; }
    public virtual DateTime? StartedDate { get; set; }
    public virtual DateTime? CompletedDate { get; set; }
    public virtual BackgroundJobState State { get; set; }
    public virtual int DurationMs { get; set; }
    public virtual int Attempts { get; set; }
    public virtual string? ErrorCode { get; set; }
    public virtual string? ErrorMessage { get; set; }
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
    public JobSummary Summary { get; set; } = null!;
    public BackgroundJob? Queued { get; set; }
    public CompletedJob? Completed { get; set; }
    public FailedJob? Failed { get; set; }
    [IgnoreDataMember] public BackgroundJobBase? Job => 
        (BackgroundJobBase?)Queued ?? (BackgroundJobBase?)Completed ?? Failed;
}