#nullable enable
using System;
using System.Collections.Generic;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Jobs;

public abstract class BackgroundJobBase : IMeta
{
    public abstract long Id { get; set; }
    public long? ParentId { get; set; }

    [Index(Unique = true)] public string? RefId { get; set; } // Unique Guid
    public string? Worker { get; set; } // Logical Thread or BG Thread if null
    public string? Tag { get; set; }
    public string? Callback { get; set; } //CreateOpenAiChat or CreateOpenAiChatTask
    public DateTime? RunAfter { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? RequestId { get; set; }

    public string RequestType { get; set; } // API or CMD

    public string? Command { get; set; }
    //CreateOpenAiChatTaskCommand
    public string Request { get; set; } //CreateOpenAiChat or CreateOpenAiChatTask

    //OpenAiChatTask
    //[Exclude]
    public string RequestBody { get; set; }

    public string? UserId { get; set; } // IdentityAuth ApplicationUser.Id
    public string Response { get; set; }

    //[Exclude]
    public string ResponseBody { get; set; }
    public BackgroundJobState State { get; set; }

    [Index] public DateTime? StartedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public DateTime? NotifiedDate { get; set; }
    public int DurationMs { get; set; }
    public int? TimeoutSecs { get; set; }
    public int? RetryLimit { get; set; }
    public int Attempts { get; set; }
    public double? Progress { get; set; } // 0-1
    public string? Status { get; set; }   // e.g. Downloaded 2/10
    public string? Logs { get; set; }     // Append recorded logs
    public DateTime? LastActivityDate { get; set; }
    public string? ReplyTo { get; set; }

    public string? ErrorCode { get; set; }

    public ResponseStatus? Error { get; set; }
    public Dictionary<string, string>? Args { get; set; }
    //[Exclude]
    public Dictionary<string, string>? Meta { get; set; }    
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
public class CompletedJob : BackgroundJobBase
{
    public override long Id { get; set; }
}

[Icon(Svg = SvgIcons.Failed)]
public class FailedJob : BackgroundJobBase
{
    public override long Id { get; set; }
}
