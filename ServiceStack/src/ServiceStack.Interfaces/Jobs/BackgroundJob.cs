#nullable enable
using System;
using System.Collections.Generic;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Jobs;

public abstract class BackgroundJobBase : IMeta
{
    public virtual long Id { get; set; }
    public virtual long? ParentId { get; set; }

    [Index(Unique = true)] public virtual string? RefId { get; set; } // Unique Guid
    public virtual string? Worker { get; set; } // Logical Thread or BG Thread if null
    public virtual string? Tag { get; set; }
    public virtual string? Callback { get; set; } //CreateOpenAiChat or CreateOpenAiChatTask
    public virtual DateTime? RunAfter { get; set; }
    public virtual DateTime CreatedDate { get; set; }
    public virtual string? CreatedBy { get; set; }
    public virtual string? RequestId { get; set; }

    public virtual string RequestType { get; set; } // API or CMD

    public virtual string? Command { get; set; }
    //CreateOpenAiChatTaskCommand
    public virtual string Request { get; set; } //CreateOpenAiChat or CreateOpenAiChatTask

    //OpenAiChatTask
    //[Exclude]
    public virtual string RequestBody { get; set; }

    public virtual string? UserId { get; set; } // IdentityAuth ApplicationUser.Id
    public virtual string Response { get; set; }

    //[Exclude]
    public virtual string ResponseBody { get; set; }
    public virtual BackgroundJobState State { get; set; }

    [Index] public virtual DateTime? StartedDate { get; set; }
    public virtual DateTime? CompletedDate { get; set; }
    public virtual DateTime? NotifiedDate { get; set; }
    public virtual int DurationMs { get; set; }
    public virtual int? TimeoutSecs { get; set; }
    public virtual int? RetryLimit { get; set; }
    public virtual int Attempts { get; set; }
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

    [Ignore] public bool Transient { get; set; }
    [Ignore] public Action<object>? OnSuccess { get; set; }
    [Ignore] public Action<Exception>? OnFailed { get; set; }
}

[Icon(Svg = SvgIcons.Stats)]
public class JobSummary
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    [Index(Unique = true)] public virtual string? RefId { get; set; }
    public string? Worker { get; set; }
    public string? Tag { get; set; }
    public virtual DateTime CreatedDate { get; set; }
    public virtual string? CreatedBy { get; set; }
    public virtual string? RequestId { get; set; }
    public virtual string RequestType { get; set; } // API or CMD
    public virtual string Request { get; set; }
    public virtual string Response { get; set; }
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
