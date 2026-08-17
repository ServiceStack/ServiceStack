using System.Text.Json.Nodes;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.AI;

public partial class ChatDb
{
    static readonly System.Collections.Concurrent.ConcurrentDictionary<long, object> MessageBackfillLocks = new();

    public long CreateAgentRun(long threadId, string? user, string? model, int maxSteps = 250)
    {
        var now = DateTime.Now;
        using var db = OpenDb();
        return db.Insert(new AgentRun
        {
            ThreadId = threadId,
            User = user ?? DefaultUser,
            Status = AgentRunStatus.Queued,
            NextAction = "model",
            Model = model,
            MaxSteps = Math.Max(1, maxSteps),
            NextAttemptAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        }, selectIdentity: true);
    }

    public AgentRun? GetAgentRun(long runId, string? user = null)
    {
        using var db = OpenDb();
        var q = db.From<AgentRun>().Where(x => x.Id == runId);
        if (user != null && !IsAllUsers(user)) q.And(x => x.User == user);
        return db.Single(q);
    }

    public AgentRun? GetActiveAgentRun(long threadId, string? user = null)
    {
        using var db = OpenDb();
        var q = db.From<AgentRun>().Where(x => x.ThreadId == threadId &&
            (x.Status == AgentRunStatus.Queued || x.Status == AgentRunStatus.Running ||
             x.Status == AgentRunStatus.WaitingApproval));
        if (user != null && !IsAllUsers(user)) q.And(x => x.User == user);
        q.OrderByDescending(x => x.Id).Limit(1);
        return db.Single(q);
    }

    public int RequeueInterruptedAgentRuns()
    {
        using var db = OpenDb();
        var now = DateTime.Now;
        return db.UpdateOnly(() => new AgentRun
        {
            Status = AgentRunStatus.Queued,
            LeaseOwner = null,
            LeaseExpiresAt = null,
            UpdatedAt = now,
        }, x => x.Status == AgentRunStatus.Running);
    }

    public List<AgentRun> ClaimAgentRuns(string owner, int limit, int leaseSeconds)
    {
        if (limit <= 0) return [];
        using var db = OpenDb();
        using var tx = db.OpenTransaction();
        var now = DateTime.Now;
        var ids = db.Column<long>(db.From<AgentRun>()
            .Where(x => x.Status == AgentRunStatus.Queued &&
                (x.NextAttemptAt == null || x.NextAttemptAt <= now))
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
            .Limit(limit)
            .Select(x => x.Id));
        var claimed = new List<long>();
        foreach (var id in ids)
        {
            var changed = db.UpdateOnly(() => new AgentRun
            {
                Status = AgentRunStatus.Running,
                LeaseOwner = owner,
                LeaseExpiresAt = now.AddSeconds(Math.Max(30, leaseSeconds)),
                UpdatedAt = now,
            }, x => x.Id == id && x.Status == AgentRunStatus.Queued);
            if (changed > 0) claimed.Add(id);
        }
        var runs = claimed.Count == 0 ? [] : db.Select<AgentRun>(x => claimed.Contains(x.Id));
        tx.Commit();
        return runs;
    }

    public bool RenewAgentRunLease(long runId, string owner, int leaseSeconds)
    {
        using var db = OpenDb();
        var now = DateTime.Now;
        return db.UpdateOnly(() => new AgentRun
        {
            LeaseExpiresAt = now.AddSeconds(Math.Max(30, leaseSeconds)), UpdatedAt = now,
        }, x => x.Id == runId && x.Status == AgentRunStatus.Running && x.LeaseOwner == owner) > 0;
    }

    public void UpdateAgentRun(AgentRun run)
    {
        run.UpdatedAt = DateTime.Now;
        using var db = OpenDb();
        db.Update(run);
    }

    public long CreateAgentStep(long runId, int sequence, JsonObject? input = null)
    {
        var now = DateTime.Now;
        using var db = OpenDb();
        return db.Insert(new AgentStep
        {
            RunId = runId,
            Sequence = sequence,
            IdempotencyKey = $"run:{runId}:step:{sequence}",
            Status = AgentRunStatus.Running,
            Input = ChatDtos.ToJson(input),
            CreatedAt = now,
            StartedAt = now,
        }, selectIdentity: true);
    }

    public AgentStep? GetAgentStep(long id)
    {
        using var db = OpenDb();
        return db.SingleById<AgentStep>(id);
    }

    public void UpdateAgentStep(AgentStep step)
    {
        using var db = OpenDb();
        db.Update(step);
    }

    public void EnsureChatMessages(long threadId)
    {
        lock (MessageBackfillLocks.GetOrAdd(threadId, static _ => new object()))
        {
            using var db = OpenDb();
            using var tx = db.OpenTransaction();
            if (db.Exists<ChatMessage>(x => x.ThreadId == threadId && x.Active))
            {
                tx.Commit();
                return;
            }
            var thread = db.SingleById<ChatThread>(threadId);
            if (ChatDtos.ParseJson(thread?.Messages) is JsonArray messages)
                InsertMessages(db, threadId, messages, null, null);
            tx.Commit();
        }
    }

    public void SyncChatMessages(long threadId, JsonArray messages, bool rewrite = false,
        long? runId = null, long? stepId = null)
    {
        using var db = OpenDb();
        using var tx = db.OpenTransaction();
        var hasRows = db.Exists<ChatMessage>(x => x.ThreadId == threadId && x.Active);
        if (rewrite && hasRows)
        {
            db.UpdateOnly(() => new ChatMessage { Active = false },
                x => x.ThreadId == threadId && x.Active);
            db.Delete<ContextSnapshot>(x => x.ThreadId == threadId);
            hasRows = false;
        }

        var incoming = messages.WithoutStreamingMessages().OfType<JsonObject>().ToList();
        var incomingTimestamps = incoming.Select(x => x.GetLong("timestamp"))
            .Where(x => x != null).Select(x => x!.Value).Distinct().ToList();
        var known = new Dictionary<long, ChatMessage>();
        if (hasRows)
        {
            foreach (var timestamps in incomingTimestamps.Chunk(500))
            {
                foreach (var row in db.Select(db.From<ChatMessage>().Where(x => x.ThreadId == threadId &&
                             x.Active && Sql.In(x.Timestamp, timestamps))))
                    if (row.Timestamp != null) known[row.Timestamp.Value] = row;
            }
        }
        var append = new JsonArray();
        foreach (var message in incoming)
        {
            var timestamp = message.GetLong("timestamp");
            if (timestamp != null && known.TryGetValue(timestamp.Value, out var existing))
            {
                var persisted = PersistedMessage(message);
                var persistedJson = persisted.ToJsonString(ChatJson.Options);
                if (existing.Message != persistedJson)
                {
                    db.UpdateOnly(() => new ChatMessage
                    {
                        Role = persisted.GetString("role") ?? "user",
                        Message = persistedJson,
                        ToolCallId = persisted.GetString("tool_call_id"),
                        ToolName = persisted.GetString("name"),
                        TokenCount = DurableAgentUtils.CountTokensApprox(persisted),
                    }, x => x.Id == existing.Id);
                }
                if (runId != null && existing.RunId == null)
                    db.UpdateOnly(() => new ChatMessage { RunId = runId }, x => x.ThreadId == threadId &&
                        x.Active && x.Timestamp == timestamp && x.RunId == null);
                if (stepId != null && existing.StepId == null)
                    db.UpdateOnly(() => new ChatMessage { StepId = stepId }, x => x.ThreadId == threadId &&
                        x.Active && x.Timestamp == timestamp && x.StepId == null);
                continue;
            }
            append.Add(message.Clone());
            if (timestamp != null) known[timestamp.Value] = new ChatMessage { Timestamp = timestamp };
        }
        InsertMessages(db, threadId, append, runId, stepId);
        tx.Commit();
    }

    static void InsertMessages(System.Data.IDbConnection db, long threadId, JsonArray messages,
        long? runId, long? stepId)
    {
        var next = db.Scalar<long>(db.From<ChatMessage>()
            .Where(x => x.ThreadId == threadId)
            .Select(x => Sql.Max(x.Sequence))) + 1;
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var maxTimestamp = db.Scalar<long?>(db.From<ChatMessage>()
            .Where(x => x.ThreadId == threadId && x.Timestamp != null)
            .Select(x => Sql.Max(x.Timestamp))) ?? 0;
        var timestamp = Math.Max(nowMs, maxTimestamp + 1);
        foreach (var node in messages)
        {
            if (node is not JsonObject message) continue;
            message = PersistedMessage(message);
            message["timestamp"] ??= timestamp++;
            var toolCallId = message.GetString("tool_call_id");
            var toolName = message.GetString("name");
            db.Insert(new ChatMessage
            {
                ThreadId = threadId,
                Sequence = next++,
                RunId = runId,
                StepId = stepId,
                Role = message.GetString("role") ?? "user",
                Message = message.ToJsonString(ChatJson.Options),
                Timestamp = message.GetLong("timestamp"),
                ToolCallId = toolCallId,
                ToolName = toolName,
                TokenCount = DurableAgentUtils.CountTokensApprox(message),
                Active = true,
                CreatedAt = DateTime.Now,
            });
        }
    }

    static JsonObject PersistedMessage(JsonObject message)
    {
        var clone = message.Clone();
        clone.Remove("_sequence");
        clone.Remove(ChatDtos.StreamingKey);
        return clone;
    }

    public List<JsonObject> GetChatMessagePage(long threadId, long? before = null,
        long? after = 0, int take = 100)
    {
        EnsureChatMessages(threadId);
        using var db = OpenDb();
        var q = db.From<ChatMessage>().Where(x => x.ThreadId == threadId && x.Active);
        if (before != null)
        {
            q.And(x => x.Sequence < before.Value).OrderByDescending(x => x.Sequence).Limit(take);
            var rows = db.Select(q); rows.Reverse();
            return ExpandToolMessageBoundaries(db, threadId, rows).Map(ToMessageDto);
        }
        q.And(x => x.Sequence > (after ?? 0)).OrderBy(x => x.Sequence).Limit(take);
        return ExpandToolMessageBoundaries(db, threadId, db.Select(q)).Map(ToMessageDto);
    }

    public List<JsonObject> GetChatMessageWindow(long threadId, int head = 20, int tail = 100)
    {
        EnsureChatMessages(threadId);
        using var db = OpenDb();
        var headRows = db.Select(db.From<ChatMessage>()
            .Where(x => x.ThreadId == threadId && x.Active).OrderBy(x => x.Sequence).Limit(head));
        var tailRows = db.Select(db.From<ChatMessage>()
            .Where(x => x.ThreadId == threadId && x.Active).OrderByDescending(x => x.Sequence).Limit(tail));
        tailRows.Reverse();
        headRows = ExpandToolMessageBoundaries(db, threadId, headRows);
        tailRows = ExpandToolMessageBoundaries(db, threadId, tailRows);
        return headRows.Concat(tailRows).GroupBy(x => x.Sequence).Select(x => ToMessageDto(x.First()))
            .OrderBy(x => x.GetLong("_sequence")).ToList();
    }

    /// <summary>Never return a tool result without its assistant tool call, or half a result group.</summary>
    static List<ChatMessage> ExpandToolMessageBoundaries(System.Data.IDbConnection db, long threadId,
        List<ChatMessage> rows)
    {
        if (rows.Count == 0) return rows;
        while (rows[0].Role == "tool")
        {
            var firstSequence = rows[0].Sequence;
            var previous = db.Single(db.From<ChatMessage>().Where(x => x.ThreadId == threadId && x.Active &&
                    x.Sequence < firstSequence).OrderByDescending(x => x.Sequence).Limit(1));
            if (previous == null) break;
            rows.Insert(0, previous);
            if (previous.Role != "tool") break;
        }

        var lastMessage = ChatDtos.ParseJson(rows[^1].Message) as JsonObject;
        var needsFollowingResults = rows[^1].Role == "tool" ||
            lastMessage?.GetArray("tool_calls") is { Count: > 0 };
        while (needsFollowingResults)
        {
            var lastSequence = rows[^1].Sequence;
            var next = db.Single(db.From<ChatMessage>().Where(x => x.ThreadId == threadId && x.Active &&
                    x.Sequence > lastSequence).OrderBy(x => x.Sequence).Limit(1));
            if (next?.Role != "tool") break;
            rows.Add(next);
            needsFollowingResults = true;
        }
        return rows;
    }

    public (long Count, long? First, long? Last) GetChatMessageBounds(long threadId)
    {
        EnsureChatMessages(threadId);
        using var db = OpenDb();
        var q = db.From<ChatMessage>().Where(x => x.ThreadId == threadId && x.Active)
            .Select(x => new
            {
                Count = Sql.Count("*"),
                First = Sql.Min(x.Sequence),
                Last = Sql.Max(x.Sequence),
            });
        var bounds = db.Single<MessageBounds>(q);
        return bounds == null || bounds.Count == 0
            ? (0, null, null)
            : (bounds.Count, bounds.First, bounds.Last);
    }

    sealed class MessageBounds
    {
        public long Count { get; set; }
        public long? First { get; set; }
        public long? Last { get; set; }
    }

    public List<JsonObject> GetActiveMessagesAfter(long threadId, long sequence)
    {
        EnsureChatMessages(threadId);
        using var db = OpenDb();
        return db.Select(db.From<ChatMessage>().Where(x => x.ThreadId == threadId && x.Active &&
            x.Sequence > sequence).OrderBy(x => x.Sequence)).Map(ToMessageDto);
    }

    public ContextSnapshot? GetLatestContextSnapshot(long threadId)
    {
        using var db = OpenDb();
        return db.Single(db.From<ContextSnapshot>().Where(x => x.ThreadId == threadId)
            .OrderByDescending(x => x.Version).Limit(1));
    }

    public long CreateContextSnapshot(long threadId, long? runId, long fromSequence, long toSequence,
        JsonArray summary, string? model)
    {
        using var db = OpenDb();
        var version = (db.Scalar<int?>(db.From<ContextSnapshot>()
            .Where(x => x.ThreadId == threadId).Select(x => Sql.Max(x.Version))) ?? 0) + 1;
        return db.Insert(new ContextSnapshot
        {
            ThreadId = threadId, RunId = runId, Version = version, FromSequence = fromSequence,
            ToSequence = toSequence, Summary = summary.ToJsonString(ChatJson.Options),
            TokenCount = DurableAgentUtils.CountTokensApprox(summary), Model = model, CreatedAt = DateTime.Now,
        }, selectIdentity: true);
    }

    static JsonObject ToMessageDto(ChatMessage row)
    {
        var message = ChatDtos.ParseJson(row.Message) as JsonObject ?? new JsonObject();
        message["_sequence"] = row.Sequence;
        return message;
    }

    public void DeleteDurableThreadData(long threadId)
    {
        using var db = OpenDb();
        var runIds = db.Column<long>(db.From<AgentRun>().Where(r => r.ThreadId == threadId).Select(r => r.Id));
        if (runIds.Count > 0)
            db.Delete<AgentStep>(x => Sql.In(x.RunId, runIds));
        db.Delete<AgentRun>(x => x.ThreadId == threadId);
        db.Delete<ContextSnapshot>(x => x.ThreadId == threadId);
        db.Delete<ChatMessage>(x => x.ThreadId == threadId);
    }
}

public static class DurableAgentDtos
{
    public static JsonObject ToDto(this AgentRun x) => new()
    {
        ["id"] = x.Id, ["threadId"] = x.ThreadId, ["user"] = x.User, ["status"] = x.Status,
        ["nextAction"] = x.NextAction, ["model"] = x.Model, ["stepCount"] = x.StepCount,
        ["sliceCount"] = x.SliceCount, ["maxSteps"] = x.MaxSteps,
        ["contextTokens"] = x.ContextTokens, ["contextLimit"] = x.ContextLimit,
        ["error"] = x.Error, ["createdAt"] = ChatDb.ToDateString(x.CreatedAt),
        ["updatedAt"] = ChatDb.ToDateString(x.UpdatedAt), ["completedAt"] = ChatDb.ToDateNode(x.CompletedAt),
    };
}
