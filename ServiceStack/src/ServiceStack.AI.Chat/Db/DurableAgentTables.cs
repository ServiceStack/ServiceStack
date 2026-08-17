using ServiceStack.DataAnnotations;

namespace ServiceStack.AI;

public static class AgentRunStatus
{
    public const string Queued = "queued";
    public const string Running = "running";
    public const string WaitingApproval = "waiting_approval";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";

    public static bool IsActive(string? status) => status is Queued or Running or WaitingApproval;
}

public class AgentRun
{
    [AutoIncrement] public long Id { get; set; }
    [Index] public long ThreadId { get; set; }
    [Alias("user"), Index] public string? User { get; set; }
    [Index] public string Status { get; set; } = AgentRunStatus.Queued;
    public string? NextAction { get; set; }
    public string? Model { get; set; }
    public int StepCount { get; set; }
    public int SliceCount { get; set; }
    public int MaxSteps { get; set; } = 250;
    public long? ContextTokens { get; set; }
    public long? ContextLimit { get; set; }
    public string? LeaseOwner { get; set; }
    [Index] public DateTime? LeaseExpiresAt { get; set; }
    [Index] public DateTime? NextAttemptAt { get; set; }
    [StringLength(StringLengthAttribute.MaxText)] public string? Error { get; set; }
    [Index] public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

[UniqueConstraint(nameof(RunId), nameof(Sequence))]
public class AgentStep
{
    [AutoIncrement] public long Id { get; set; }
    [Index] public long RunId { get; set; }
    public int Sequence { get; set; }
    public string Type { get; set; } = "model";
    [Index] public string Status { get; set; } = AgentRunStatus.Running;
    [StringLength(StringLengthAttribute.MaxText)] public string? Input { get; set; }
    [StringLength(StringLengthAttribute.MaxText)] public string? Output { get; set; }
    [Index(Unique = true)] public string IdempotencyKey { get; set; } = null!;
    public int Attempt { get; set; } = 1;
    [StringLength(StringLengthAttribute.MaxText)] public string? Error { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

[UniqueConstraint(nameof(ThreadId), nameof(Sequence))]
public class ChatMessage
{
    [AutoIncrement] public long Id { get; set; }
    [Index] public long ThreadId { get; set; }
    public long Sequence { get; set; }
    [Index] public long? RunId { get; set; }
    public long? StepId { get; set; }
    [Index] public string Role { get; set; } = null!;
    [StringLength(StringLengthAttribute.MaxText)] public string Message { get; set; } = "{}";
    [Index] public long? Timestamp { get; set; }
    [Index] public string? ToolCallId { get; set; }
    public string? ToolName { get; set; }
    public long? TokenCount { get; set; }
    [Index] public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

[UniqueConstraint(nameof(ThreadId), nameof(Version))]
public class ContextSnapshot
{
    [AutoIncrement] public long Id { get; set; }
    [Index] public long ThreadId { get; set; }
    public long? RunId { get; set; }
    public int Version { get; set; }
    public long FromSequence { get; set; }
    public long ToSequence { get; set; }
    [StringLength(StringLengthAttribute.MaxText)] public string Summary { get; set; } = "[]";
    public long? TokenCount { get; set; }
    public string? Model { get; set; }
    public DateTime CreatedAt { get; set; }
}
