using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>
/// Preflights a model-generated tool call before its executable handler runs. Returning an approval
/// request pauses an interactive thread; returning null lets the call execute normally.
/// </summary>
public delegate Task<ChatToolApprovalRequest?> ChatToolApprovalHandler(JsonObject args, ChatContext context);

/// <summary>UI-neutral description of a tool call that needs a human decision.</summary>
public class ChatToolApprovalRequest
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public ToolSafety Safety { get; init; }
    public required JsonObject Schema { get; init; }
    public required JsonObject Arguments { get; init; }
    public JsonObject Metadata { get; init; } = new();
}

/// <summary>A pending call plus the provider's correlation id.</summary>
public class PendingChatToolCall
{
    public required string ToolCallId { get; init; }
    public required string ToolName { get; init; }
    public required JsonObject Arguments { get; init; }
    public required ChatToolApprovalRequest Approval { get; init; }
    public int Sequence { get; init; }
}

/// <summary>
/// Durable approval coordinator supplied by an interactive host. The core completion pipeline stays
/// usable without one; unsafe calls then fail closed instead of waiting for a UI that does not exist.
/// </summary>
public interface IChatToolApprovalCoordinator
{
    Task PauseAsync(IReadOnlyList<PendingChatToolCall> calls, ChatContext context);
    bool HasPending(long threadId, string? user);
    Task CancelThreadAsync(long threadId, string? user);
}
