using System.Text.Json.Nodes;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Persists the in-flight assistant message while a response streams in (port of StreamCheckpointWriter).
///
/// It writes only the thread's `streamingMessage`, never `messages`. The durable conversation is
/// therefore untouchable from the streaming path: however a stream fails — provider dying, timeout,
/// retry, cancel, process crash — the most that can be lost is the response currently being generated,
/// never the thread.
///
/// Writes are checkpoints, not per-chunk: a thread's history can be megabytes and the old per-chunk
/// write rewrote all of it ~10x/second. Chunks accumulate in memory and reach the db every
/// <see cref="Interval"/>, plus once when the stream ends.
/// </summary>
public class StreamCheckpointWriter(
    IThreadApi? threadsApi,
    long? threadId,
    string? user = null,
    TimeSpan? interval = null)
{
    public IThreadApi? ThreadsApi { get; } = threadsApi;
    public long? ThreadId { get; } = threadId;
    public string? User { get; } = user;

    /// <summary>Minimum time between checkpoints, which is also how often the UI sees the response</summary>
    public TimeSpan Interval { get; set; } = interval ?? TimeSpan.FromMilliseconds(250);

    DateTimeOffset lastUpdate = DateTimeOffset.MinValue;
    JsonObject? pending;

    public bool Enabled => ThreadsApi != null && ThreadId != null;

    public bool Due() => Enabled && DateTimeOffset.UtcNow - lastUpdate >= Interval;

    /// <summary>Size of everything a streaming assistant message can accumulate</summary>
    public static int PayloadLength(JsonObject message)
    {
        var total = message["content"] is JsonArray parts
            ? parts.Count
            : (message.GetString("content") ?? "").Length;
        foreach (var key in OpenAiCompatibleProvider.ReasoningFields)
        {
            total += (message.GetString(key) ?? "").Length;
        }
        foreach (var node in message.GetArray("tool_calls") ?? [])
        {
            var fn = (node as JsonObject).GetObject("function");
            total += (fn.GetString("name") ?? "").Length + (fn.GetString("arguments") ?? "").Length;
        }
        return total;
    }

    /// <summary>
    /// Checkpoint the in-flight message, returns whether it reached the db. <paramref name="final"/>
    /// forces a write regardless of the interval, so the last chunks of a completed stream are never
    /// left only in memory.
    /// </summary>
    public async Task<bool> WriteAsync(JsonObject assistantMessage, bool final = false)
    {
        if (!Enabled)
            return false;
        // An empty message says nothing and would only blank out a partial that a previous
        // attempt already produced.
        if (PayloadLength(assistantMessage) == 0)
            return false;

        pending = assistantMessage;
        if (!final && !Due())
            return false;

        lastUpdate = DateTimeOffset.UtcNow;
        pending = null;
        await ThreadsApi!.CheckpointStreamAsync(ThreadId!.Value, assistantMessage, User).ConfigAwait();
        return true;
    }

    /// <summary>Checkpoint whatever hasn't reached the db yet, e.g. after a stream fails</summary>
    public Task<bool> FlushAsync() =>
        pending == null ? Task.FromResult(false) : WriteAsync(pending, final: true);
}
