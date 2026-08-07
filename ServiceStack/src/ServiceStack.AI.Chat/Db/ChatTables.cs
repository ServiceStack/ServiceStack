using ServiceStack.DataAnnotations;

namespace ServiceStack.AI;

/// <summary>
/// A chat conversation (port of llms-py's app.sqlite "thread" table, extensions/app/db.py:34).
/// Complex fields are stored as raw JSON strings so the wire shape matches Python's dict columns
/// exactly; they're parsed to JsonNode only at the DTO boundary.
/// </summary>
public class ChatThread
{
    [AutoIncrement]
    public long Id { get; set; }

    /// <summary>Data partition key: the authenticated username (null/"default" when auth is disabled)</summary>
    [Alias("user"), Index]
    public string? User { get; set; }

    [Index]
    public DateTime CreatedAt { get; set; }
    [Index]
    public DateTime UpdatedAt { get; set; }

    public string? Title { get; set; }
    [StringLength(StringLengthAttribute.MaxText)]
    public string? SystemPrompt { get; set; }
    [Index]
    public string? Model { get; set; }

    [StringLength(StringLengthAttribute.MaxText)]
    public string? ModelInfo { get; set; }        // JSON
    public string? Modalities { get; set; }       // JSON
    [StringLength(StringLengthAttribute.MaxText)]
    public string? Messages { get; set; }         // JSON
    /// <summary>
    /// In-flight assistant message while streaming, kept out of <see cref="Messages"/> so a failed
    /// stream can never damage the durable conversation. Merged into the DTO's messages on the way out.
    /// </summary>
    [StringLength(StringLengthAttribute.MaxText)]
    public string? StreamingMessage { get; set; } // JSON
    public string? Args { get; set; }             // JSON
    [StringLength(StringLengthAttribute.MaxText)]
    public string? Tools { get; set; }            // JSON
    [StringLength(StringLengthAttribute.MaxText)]
    public string? ToolHistory { get; set; }      // JSON

    [Index]
    public double? Cost { get; set; }
    public long? InputTokens { get; set; }
    public long? OutputTokens { get; set; }
    public string? Stats { get; set; }            // JSON

    public string? Provider { get; set; }
    public string? ProviderModel { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? Metadata { get; set; }         // JSON
    public string? Status { get; set; }
    [StringLength(StringLengthAttribute.MaxText)]
    public string? Error { get; set; }
    public string? Ref { get; set; }
    [StringLength(StringLengthAttribute.MaxText)]
    public string? ProviderResponse { get; set; } // JSON
    public long? ContextTokens { get; set; }
    public long? ParentId { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? PublishedUrl { get; set; }

    /// <summary>
    /// Content signature the UI long-poll compares to detect changes (port of get_thread_signature).
    /// [Ignore] keeps it out of the OrmLite table — it's computed and surfaced only in the DTO.
    /// </summary>
    [Ignore]
    public string Sig => ChatSignature.Compute(Messages, StreamingMessage, Status, CompletedAt, Error);
}

/// <summary>
/// Cheap content signature for thread long-polling: a change in the messages (by length + tail), the
/// in-flight streaming message, status, completion or error yields a different value. Mirrors llms-py's
/// get_thread_signature — the exact bytes needn't match Python since the client only ever echoes the
/// server's value back. The streaming message is part of it because clients see the merged list, so
/// each stream checkpoint has to wake the long-poll.
/// </summary>
public static class ChatSignature
{
    public static string Compute(string? messagesJson, string? streamingMessageJson, string? status,
        DateTime? completedAt, string? error)
    {
        var msg = messagesJson ?? "";
        var msgLen = msg.Length;
        var msgTail = msgLen > 100 ? msg[^100..] : msg;
        var streaming = streamingMessageJson ?? "";
        var streamingTail = streaming.Length > 100 ? streaming[^100..] : streaming;
        var completed = completedAt != null ? ChatDb.ToDateString(completedAt.Value) : "";

        var raw = $"{msgLen}:{msgTail}:{streaming.Length}:{streamingTail}:{status ?? ""}:{completed}:{error ?? ""}";
        var hash = Convert.ToHexString(
            System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(raw)));
        return $"{msgLen}:{hash[..12].ToLowerInvariant()}";
    }
}

/// <summary>
/// Per-completion accounting used by the analytics dashboards
/// (port of the app.sqlite "request" table, extensions/app/db.py:66).
/// </summary>
public class ChatRequest
{
    [AutoIncrement]
    public long Id { get; set; }

    [Alias("user"), Index]
    public string? User { get; set; }

    [Index]
    public long? ThreadId { get; set; }
    [Index]
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string? Title { get; set; }
    [Index]
    public string? Model { get; set; }
    public long? Duration { get; set; }

    public double? Cost { get; set; }
    public double? InputPrice { get; set; }
    public long? InputTokens { get; set; }
    public long? InputCachedTokens { get; set; }
    public double? OutputPrice { get; set; }
    public long? OutputTokens { get; set; }
    public long? TotalTokens { get; set; }
    public string? Usage { get; set; }            // JSON

    public string? Provider { get; set; }
    public string? ProviderModel { get; set; }
    public string? ProviderRef { get; set; }
    public string? FinishReason { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    [StringLength(StringLengthAttribute.MaxText)]
    public string? Error { get; set; }
    [StringLength(StringLengthAttribute.MaxText)]
    public string? StackTrace { get; set; }
    public string? Ref { get; set; }
}

/// <summary>
/// Generated/uploaded media catalogue (port of gallery.sqlite "media" table, extensions/gallery/db.py:39).
/// </summary>
public class ChatMedia
{
    [AutoIncrement]
    public long Id { get; set; }

    [Alias("user"), Index]
    public string? User { get; set; }

    public string? Name { get; set; }
    /// <summary>image | audio | video</summary>
    [Index]
    public string? Type { get; set; }
    [StringLength(StringLengthAttribute.MaxText)]
    public string? Prompt { get; set; }
    public string? Model { get; set; }
    [Index]
    public DateTime Created { get; set; }
    public double? Cost { get; set; }
    public long? Seed { get; set; }
    public string? Url { get; set; }
    [Index]
    public string? Hash { get; set; }
    /// <summary>Closest configured ratio, e.g. "9:16" (drives the gallery's format filter)</summary>
    public string? AspectRatio { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? Size { get; set; }
    public double? Duration { get; set; }

    public string? Reactions { get; set; }        // JSON
    public string? Caption { get; set; }
    [StringLength(StringLengthAttribute.MaxText)]
    public string? Description { get; set; }
    public string? Phash { get; set; }
    public string? Color { get; set; }
    public string? Category { get; set; }         // JSON
    public string? Tags { get; set; }             // JSON
    /// <summary>Content rating, e.g. "G", "PG", "M"</summary>
    public string? Rating { get; set; }
    public string? Ratings { get; set; }          // JSON
    public string? Objects { get; set; }          // JSON
    public string? VariantId { get; set; }
    public string? VariantName { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? PublishedUrl { get; set; }
    public string? Metadata { get; set; }         // JSON
}

/// <summary>
/// Per-user request rollup returned by the Admin analytics views. Not a table — the result shape of
/// <see cref="ChatDb.GetUsersSummary"/>.
/// </summary>
public class ChatUserSummary
{
    public string? User { get; set; }
    public long Requests { get; set; }
    public double? Cost { get; set; }
    public long? InputTokens { get; set; }
    public long? OutputTokens { get; set; }
    public DateTime? LastActive { get; set; }
}
