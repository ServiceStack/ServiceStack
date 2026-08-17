using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>
/// OrmLite-backed IThreadApi used by the streaming writer and other extensions
/// (port of extensions/app's ThreadApi).
/// </summary>
public class DbThreadApi(ChatDb db, ThreadUpdates updates, ILogger log) : IThreadApi
{
    public ChatDb Db => db;
    public ThreadUpdates Updates => updates;

    /// <summary>Set by callers that legitimately rewrite history (edit, redo, delete, compact)</summary>
    public const string TruncateKey = "truncate";

    public JsonObject? GetThread(long threadId, string? user) =>
        db.GetThread(threadId, user)?.ToDto();

    public JsonObject? GetRequest(string requestId, string? user) =>
        long.TryParse(requestId, out var id) ? db.GetRequest(id, user)?.ToDto() : null;

    public async Task UpdateThreadAsync(long threadId, JsonObject thread, string? user = null)
    {
        await UpdateThreadInternalAsync(threadId, thread, user).ConfigAwait();
        updates.NotifyThreadUpdate(threadId);
    }

    /// <summary>
    /// Persist the in-flight assistant message. This writes only `streamingMessage`, never `messages`,
    /// so however a stream fails the conversation is untouched — the most that can be lost is the
    /// response currently being generated (port of ThreadApi.checkpoint_stream_async).
    /// </summary>
    public Task CheckpointStreamAsync(long threadId, JsonObject message, string? user = null)
    {
        db.UpdateThreadStreamingMessage(threadId, ChatDtos.ToJson(message), user);
        updates.NotifyThreadUpdate(threadId);
        return Task.CompletedTask;
    }

    /// <summary>Merge a partial thread DTO into the stored row (read-modify-write under a per-thread lock)</summary>
    public async Task<bool> UpdateThreadInternalAsync(long threadId, JsonObject thread, string? user)
    {
        using var _ = await updates.LockThreadAsync(threadId).ConfigAwait();
        if (!thread.ContainsKey("messages"))
        {
            var partial = new ChatThread { Id = threadId };
            partial.PopulateFrom(thread);
            partial.UpdatedAt = DateTime.Now;
            var partialFields = thread.Select(x => ChatDb.ThreadColumns.GetValueOrDefault(x.Key))
                .Where(x => x != null && x != nameof(ChatThread.Sig)).Select(x => x!).ToList();
            partialFields.Add(nameof(ChatThread.UpdatedAt));
            return db.UpdateThreadFields(partial, partialFields, user) > 0;
        }

        var row = db.GetThread(threadId, user);
        if (row == null)
            return false;

        var beforeMessages = ChatDtos.ParseJson(row.Messages) as JsonArray;
        var rewrite = thread.GetBool(TruncateKey);
        PrepareThreadUpdate(threadId, thread, row);
        row.PopulateFrom(thread);
        row.UpdatedAt = DateTime.Now;
        var fields = thread.Select(x => ChatDb.ThreadColumns.GetValueOrDefault(x.Key))
            .Where(x => x != null && x != nameof(ChatThread.Sig)).Select(x => x!).ToList();
        fields.Add(nameof(ChatThread.UpdatedAt));
        db.UpdateThreadFields(row, fields, user);
        if (ChatDtos.ParseJson(row.Messages) is JsonArray messages)
        {
            if (rewrite || beforeMessages == null)
            {
                db.SyncChatMessages(threadId, messages, rewrite);
            }
            else
            {
                // Reconcile only genuinely new identities plus the trailing turn whose usage/model
                // metadata may have changed. Never feed a 30k-message history into an IN clause.
                var known = beforeMessages.OfType<JsonObject>().Select(x => x.GetLong("timestamp"))
                    .Where(x => x != null).ToSet();
                var candidates = new JsonArray();
                for (var i = 0; i < messages.Count; i++)
                {
                    if (messages[i] is not JsonObject message) continue;
                    var timestamp = message.GetLong("timestamp");
                    if (timestamp == null || !known.Contains(timestamp) || i >= messages.Count - 2)
                        candidates.Add(message.Clone());
                }
                db.SyncChatMessages(threadId, candidates);
            }
        }
        return true;
    }

    /// <summary>
    /// `messages` is the durable conversation: it must never shrink as a side effect of an in-flight
    /// request. That is how an entire thread gets erased — one request carrying a single turn replaces
    /// hundreds of messages. Callers that legitimately rewrite history (editing a message, redo,
    /// deleting a message, compacting) opt in by passing `truncate: true`.
    ///
    /// A shrinking write that hasn't opted in is repaired rather than rejected: the stored history is
    /// kept and any genuinely new messages in the update are appended, so the worst case is a
    /// duplicated message rather than a lost conversation (port of AppDB.guard_messages).
    /// </summary>
    void PrepareThreadUpdate(long threadId, JsonObject thread, ChatThread row)
    {
        var truncate = thread.GetBool(TruncateKey);
        thread.Remove(TruncateKey);

        if (thread["messages"] is not JsonArray messages)
            return;

        // An in-flight message lives in `streamingMessage` and is only merged into `messages` on the
        // way out to clients. Never let one back in: it belongs to a stream still running and is
        // committed when it finishes. Filtered before the guard so it can't buy a shrink.
        messages = messages.WithoutStreamingMessages();
        thread["messages"] = messages;

        if (truncate)
            return;

        var stored = ChatDtos.ParseJson(row.Messages) as JsonArray;
        if (stored == null || stored.Count <= messages.Count)
            return;

        var known = stored.OfType<JsonObject>().Select(x => x.GetLong("timestamp"))
            .Where(x => x != null).ToSet();
        var kept = stored.Clone();
        var appended = 0;
        foreach (var node in messages)
        {
            if (node is JsonObject message && known.Contains(message.GetLong("timestamp")))
                continue;
            kept.Add(node?.DeepClone());
            appended++;
        }
        thread["messages"] = kept;

        log.LogWarning(
            "Refused to shrink thread {ThreadId} from {StoredCount} to {UpdateCount} messages, " +
            "kept history and appended {Appended}",
            threadId, stored.Count, messages.Count, appended);
    }
}
