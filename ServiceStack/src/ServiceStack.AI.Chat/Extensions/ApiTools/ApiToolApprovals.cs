using System.Data;
using System.Text.Json.Nodes;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.AI;

[UniqueConstraint(nameof(ThreadId), nameof(ToolCallId))]
public class ChatToolApproval
{
    [AutoIncrement]
    public long Id { get; set; }
    [Index]
    public string BatchId { get; set; } = null!;
    [Index]
    public long ThreadId { get; set; }
    [Alias("user"), Index]
    public string? User { get; set; }
    [Index]
    public string ToolCallId { get; set; } = null!;
    public string ToolName { get; set; } = null!;
    public string ApiName { get; set; } = null!;
    public string? RequestType { get; set; }
    public string? Method { get; set; }
    public string? Route { get; set; }
    public string Safety { get; set; } = null!;
    [Index]
    public string Status { get; set; } = ApiToolApprovalStatus.Pending;
    public int Sequence { get; set; }
    public string? Description { get; set; }
    [StringLength(StringLengthAttribute.MaxText)]
    public string Schema { get; set; } = "{}";
    [StringLength(StringLengthAttribute.MaxText)]
    public string ProposedArgs { get; set; } = "{}";
    [StringLength(StringLengthAttribute.MaxText)]
    public string? EffectiveArgs { get; set; }
    [StringLength(StringLengthAttribute.MaxText)]
    public string? Result { get; set; }
    [StringLength(StringLengthAttribute.MaxText)]
    public string? ToolResult { get; set; }
    [StringLength(StringLengthAttribute.MaxText)]
    public string? Error { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class ChatToolApprovalBatch
{
    [PrimaryKey]
    public string Id { get; set; } = null!;
    [Index]
    public long ThreadId { get; set; }
    [Alias("user"), Index]
    public string? User { get; set; }
    [Index]
    public string Status { get; set; } = ApiToolApprovalBatchStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public static class ApiToolApprovalStatus
{
    public const string Pending = "pending";
    public const string Executing = "executing";
    public const string Completed = "completed";
    public const string Rejected = "rejected";
    public const string Failed = "failed";
    public const string Canceled = "canceled";

    public static bool IsTerminal(string status) =>
        status is Completed or Rejected or Failed or Canceled;
}

public static class ApiToolApprovalBatchStatus
{
    public const string Pending = "pending";
    public const string Resuming = "resuming";
    public const string Completed = "completed";
    public const string Canceled = "canceled";
}

/// <summary>Persistence, authenticated routes, execution, and continuation for api_call approvals.</summary>
public class ApiToolApprovalCoordinator(ApiToolsExtension apiTools, ExtensionContext ctx)
    : IChatToolApprovalCoordinator
{
    readonly ChatDb db = ctx.Feature.ChatDb!;
    IThreadApi Threads => ctx.Threads;

    public void Install()
    {
        if (ctx.Feature.AutoInitSchema)
        {
            using var conn = db.OpenDb();
            conn.CreateTableIfNotExists<ChatToolApprovalBatch>();
            conn.CreateTableIfNotExists<ChatToolApproval>();
            ChatDb.AddMissingColumns<ChatToolApprovalBatch>(conn);
            ChatDb.AddMissingColumns<ChatToolApproval>(conn);
        }

        ctx.AddGet("approvals/{threadId}", ListAsync);
        ctx.AddPost("approvals/{id}/approve", ApproveAsync);
        ctx.AddPost("approvals/{id}/reject", RejectAsync);
        ctx.AddPost("approval-batches/{id}/continue", ContinueAsync);
    }

    public async Task PauseAsync(IReadOnlyList<PendingChatToolCall> calls, ChatContext context)
    {
        if (context.ThreadId is not { } threadId)
            throw new InvalidOperationException("A durable thread is required for tool approval");
        var user = Partition(context.User);
        var now = DateTime.Now;
        var batch = new ChatToolApprovalBatch
        {
            Id = Guid.NewGuid().ToString("n"),
            ThreadId = threadId,
            User = user,
            CreatedAt = now,
            UpdatedAt = now,
        };

        using (var conn = db.OpenDb())
        using (var trans = conn.OpenTransaction())
        {
            conn.Insert(batch);
            foreach (var call in calls)
            {
                var meta = call.Approval.Metadata;
                var proposedArgs = NormalizeArguments(call.Approval.Schema, call.Approval.Arguments);
                conn.Insert(new ChatToolApproval
                {
                    BatchId = batch.Id,
                    ThreadId = threadId,
                    User = user,
                    ToolCallId = call.ToolCallId,
                    ToolName = call.ToolName,
                    ApiName = meta.GetString("apiName") ?? call.Approval.Title,
                    RequestType = meta.GetString("requestType"),
                    Method = meta.GetString("method"),
                    Route = meta.GetString("route"),
                    Safety = call.Approval.Safety.ToString().ToLowerInvariant(),
                    Sequence = call.Sequence,
                    Description = call.Approval.Description,
                    Schema = call.Approval.Schema.ToJsonString(ChatJson.Options),
                    ProposedArgs = proposedArgs.ToJsonString(ChatJson.Options),
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
            trans.Commit();
        }

        await Threads.UpdateThreadAsync(threadId, new JsonObject
        {
            ["status"] = ApprovalStatus(calls.Count),
            ["completedAt"] = null,
            // OnChatTool has already committed the assistant tool-call message. Leaving its
            // streaming checkpoint would make ToDto merge and render the same api_call twice.
            ["streamingMessage"] = null,
        }, user).ConfigAwait();
    }

    public async Task CancelThreadAsync(long threadId, string? user)
    {
        user = Partition(user);
        var now = DateTime.Now;
        using var conn = db.OpenDb();
        var approvals = conn.From<ChatToolApproval>()
            .Where(x => x.ThreadId == threadId && x.User == user
                && x.Status == ApiToolApprovalStatus.Pending);
        conn.UpdateOnly(() => new ChatToolApproval
        {
            Status = ApiToolApprovalStatus.Canceled,
            Error = "Thread was canceled",
            UpdatedAt = now,
            ResolvedAt = now,
        }, approvals);
        var batches = conn.From<ChatToolApprovalBatch>()
            .Where(x => x.ThreadId == threadId && x.User == user
                && (x.Status == ApiToolApprovalBatchStatus.Pending || x.Status == ApiToolApprovalBatchStatus.Resuming));
        conn.UpdateOnly(() => new ChatToolApprovalBatch
        {
            Status = ApiToolApprovalBatchStatus.Canceled,
            UpdatedAt = now,
            CompletedAt = now,
        }, batches);
        await Task.CompletedTask;
    }

    public bool HasPending(long threadId, string? user)
    {
        user = Partition(user);
        using var conn = db.OpenDb();
        return conn.Exists(conn.From<ChatToolApprovalBatch>().Where(x => x.ThreadId == threadId
            && x.User == user && (x.Status == ApiToolApprovalBatchStatus.Pending
                || x.Status == ApiToolApprovalBatchStatus.Resuming)));
    }

    Task<object?> ListAsync(ChatRequestContext req)
    {
        var threadId = long.TryParse(req.GetPathParam("threadId"), out var id)
            ? id
            : throw new ArgumentException("Invalid thread id");
        var user = Partition(req.UserName);
        if (db.GetThread(threadId, user) == null)
            throw HttpError.NotFound("Thread not found");

        using var conn = db.OpenDb();
        var rows = conn.Select(conn.From<ChatToolApproval>()
            .Where(x => x.ThreadId == threadId && x.User == user)
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.Sequence));
        var batches = conn.Select(conn.From<ChatToolApprovalBatch>()
                .Where(x => x.ThreadId == threadId && x.User == user))
            .ToDictionary(x => x.Id);
        return Task.FromResult<object?>(new JsonArray(rows.Select(x =>
            (JsonNode)ToDto(x, batches.GetValueOrDefault(x.BatchId))).ToArray()));
    }

    async Task<object?> ApproveAsync(ChatRequestContext req)
    {
        var id = ApprovalId(req);
        var user = Partition(req.UserName);
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var row = GetApproval(id, user) ?? throw HttpError.NotFound("Approval not found");
        AssertThreadActive(row.ThreadId, user);

        if (row.Status == ApiToolApprovalStatus.Pending && Claim(id, user, ApiToolApprovalStatus.Executing))
        {
            var proposedArgs = ParseObject(row.ProposedArgs);
            var effectiveArgs = body["args"] is JsonObject args
                ? args.Clone()
                : proposedArgs.Clone();
            try
            {
                var tool = apiTools.GetTool(row.ApiName, req.Request)
                    ?? throw HttpError.Forbidden($"API '{row.ApiName}' is no longer available to this user");
                var response = await apiTools.ExecuteAsync(tool, effectiveArgs, req.Request).ConfigAwait();
                var result = apiTools.FormatResult(response);
                var content = ToolResult("approved", row.ApiName, proposedArgs, effectiveArgs, ResultNode(result));
                Complete(id, user, ApiToolApprovalStatus.Completed, effectiveArgs, result, content, null, null);
            }
            catch (Exception e)
            {
                var error = ChatJson.ToErrorMessage(e);
                var content = ToolResult("error", row.ApiName, proposedArgs, effectiveArgs, null, error);
                Complete(id, user, ApiToolApprovalStatus.Failed, effectiveArgs, null, content, error, null);
            }
        }

        row = GetApproval(id, user)!;
        await AfterDecisionAsync(row.BatchId, row.ThreadId, user, req.Request).ConfigAwait();
        return ToDto(GetApproval(id, user)!, GetBatch(row.BatchId, user));
    }

    async Task<object?> RejectAsync(ChatRequestContext req)
    {
        var id = ApprovalId(req);
        var user = Partition(req.UserName);
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var row = GetApproval(id, user) ?? throw HttpError.NotFound("Approval not found");
        if (row.Status == ApiToolApprovalStatus.Pending)
        {
            var reason = body.GetString("reason") ?? "User declined; API was not executed";
            var args = ParseObject(row.ProposedArgs);
            var content = ToolResult("rejected", row.ApiName, args, null, null, reason);
            Reject(id, user, args, reason, content);
        }

        row = GetApproval(id, user)!;
        await AfterDecisionAsync(row.BatchId, row.ThreadId, user, req.Request).ConfigAwait();
        return ToDto(GetApproval(id, user)!, GetBatch(row.BatchId, user));
    }

    async Task<object?> ContinueAsync(ChatRequestContext req)
    {
        var batchId = req.GetPathParam("id");
        var user = Partition(req.UserName);
        var batch = GetBatch(batchId, user) ?? throw HttpError.NotFound("Approval batch not found");
        await AfterDecisionAsync(batch.Id, batch.ThreadId, user, req.Request).ConfigAwait();
        return BatchDto(GetBatch(batch.Id, user)!);
    }

    async Task AfterDecisionAsync(string batchId, long threadId, string user, IRequest request)
    {
        using (var conn = db.OpenDb())
        {
            var remaining = conn.Count(conn.From<ChatToolApproval>()
                .Where(x => x.BatchId == batchId
                    && (x.Status == ApiToolApprovalStatus.Pending || x.Status == ApiToolApprovalStatus.Executing)));
            if (remaining > 0)
            {
                await Threads.UpdateThreadAsync(threadId,
                    new JsonObject { ["status"] = ApprovalStatus((int)remaining) }, user).ConfigAwait();
                return;
            }
        }

        if (!ClaimBatch(batchId, user))
            return;

        try
        {
            List<ChatToolApproval> approvals;
            using (var conn = db.OpenDb())
            {
                approvals = conn.Select(conn.From<ChatToolApproval>()
                    .Where(x => x.BatchId == batchId).OrderBy(x => x.Sequence));
            }
            var thread = db.GetThread(threadId, user)?.ToDto() ?? throw new Exception("Thread not found");
            var messages = thread.GetArray("messages").WithoutStreamingMessages();
            var existing = messages.OfType<JsonObject>()
                .Where(x => x.GetString("role") == "tool")
                .Select(x => x.GetString("tool_call_id")).Where(x => x != null).ToSet();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var approval in approvals)
            {
                if (existing.Contains(approval.ToolCallId))
                    continue;
                messages.Add(new JsonObject
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = approval.ToolCallId,
                    ["content"] = approval.ToolResult ?? ToolResult("error", approval.ApiName,
                        ParseObject(approval.ProposedArgs),
                        approval.EffectiveArgs != null ? ParseObject(approval.EffectiveArgs) : null, null,
                        approval.Error ?? "Approval did not produce a result"),
                    ["timestamp"] = timestamp++,
                });
            }
            await Threads.UpdateThreadAsync(threadId, new JsonObject { ["messages"] = messages }, user).ConfigAwait();
            await ctx.Feature.App.QueueContinuationAsync(threadId, user, request).ConfigAwait();
            CompleteBatch(batchId, user);
        }
        catch
        {
            ResetBatch(batchId, user);
            throw;
        }
    }

    ChatToolApproval? GetApproval(long id, string user)
    {
        using var conn = db.OpenDb();
        return conn.Single(conn.From<ChatToolApproval>().Where(x => x.Id == id && x.User == user));
    }

    ChatToolApprovalBatch? GetBatch(string id, string user)
    {
        using var conn = db.OpenDb();
        return conn.Single(conn.From<ChatToolApprovalBatch>().Where(x => x.Id == id && x.User == user));
    }

    bool Claim(long id, string user, string status)
    {
        using var conn = db.OpenDb();
        return conn.UpdateOnly(() => new ChatToolApproval { Status = status, UpdatedAt = DateTime.Now },
            conn.From<ChatToolApproval>().Where(x => x.Id == id && x.User == user
                && x.Status == ApiToolApprovalStatus.Pending)) == 1;
    }

    void Complete(long id, string user, string status, JsonObject args, string? result, string toolResult,
        string? error, string? reason)
    {
        var now = DateTime.Now;
        using var conn = db.OpenDb();
        conn.UpdateOnly(() => new ChatToolApproval
        {
            Status = status,
            EffectiveArgs = args.ToJsonString(ChatJson.Options),
            Result = result,
            ToolResult = toolResult,
            Error = error,
            Reason = reason,
            UpdatedAt = now,
            ResolvedAt = now,
        }, conn.From<ChatToolApproval>().Where(x => x.Id == id && x.User == user
            && x.Status == ApiToolApprovalStatus.Executing));
    }

    void Reject(long id, string user, JsonObject args, string reason, string content)
    {
        var now = DateTime.Now;
        using var conn = db.OpenDb();
        conn.UpdateOnly(() => new ChatToolApproval
        {
            Status = ApiToolApprovalStatus.Rejected,
            EffectiveArgs = args.ToJsonString(ChatJson.Options),
            ToolResult = content,
            Reason = reason,
            UpdatedAt = now,
            ResolvedAt = now,
        }, conn.From<ChatToolApproval>().Where(x => x.Id == id && x.User == user
            && x.Status == ApiToolApprovalStatus.Pending));
    }

    bool ClaimBatch(string id, string user)
    {
        using var conn = db.OpenDb();
        return conn.UpdateOnly(() => new ChatToolApprovalBatch
        {
            Status = ApiToolApprovalBatchStatus.Resuming,
            UpdatedAt = DateTime.Now,
        }, conn.From<ChatToolApprovalBatch>().Where(x => x.Id == id && x.User == user
            && x.Status == ApiToolApprovalBatchStatus.Pending)) == 1;
    }

    void CompleteBatch(string id, string user)
    {
        var now = DateTime.Now;
        using var conn = db.OpenDb();
        conn.UpdateOnly(() => new ChatToolApprovalBatch
        {
            Status = ApiToolApprovalBatchStatus.Completed,
            UpdatedAt = now,
            CompletedAt = now,
        }, conn.From<ChatToolApprovalBatch>().Where(x => x.Id == id && x.User == user));
    }

    void ResetBatch(string id, string user)
    {
        using var conn = db.OpenDb();
        conn.UpdateOnly(() => new ChatToolApprovalBatch
        {
            Status = ApiToolApprovalBatchStatus.Pending,
            UpdatedAt = DateTime.Now,
        }, conn.From<ChatToolApprovalBatch>().Where(x => x.Id == id && x.User == user
            && x.Status == ApiToolApprovalBatchStatus.Resuming));
    }

    static long ApprovalId(ChatRequestContext req) => long.TryParse(req.GetPathParam("id"), out var id)
        ? id
        : throw new ArgumentException("Invalid approval id");

    static string Partition(string? user) => user ?? ChatDb.DefaultUser;

    void AssertThreadActive(long threadId, string user)
    {
        var thread = db.GetThread(threadId, user) ?? throw HttpError.NotFound("Thread not found");
        if (thread.CompletedAt != null || thread.Error != null)
            throw HttpError.Conflict("Thread is no longer waiting for this approval");
    }

    static string ApprovalStatus(int remaining) => remaining == 1
        ? "Approval required"
        : $"Approval required ({remaining} remaining)";
    static JsonObject ParseObject(string? json) => ChatJson.TryParseObject(json) ?? new JsonObject();
    static JsonNode ResultNode(string json)
    {
        try { return ChatJson.Parse(json); }
        catch { return JsonValue.Create(json)!; }
    }

    static string ToolResult(string status, string apiName, JsonObject proposedArgs,
        JsonObject? effectiveArgs, JsonNode? response,
        string? message = null)
    {
        var arguments = effectiveArgs ?? proposedArgs;
        var modified = effectiveArgs != null && !JsonNode.DeepEquals(proposedArgs, effectiveArgs);
        var decision = status == "rejected" ? "rejected"
            : effectiveArgs != null ? "approved"
            : "not_executed";
        var result = new JsonObject
        {
            ["status"] = status,
            ["api"] = apiName,
            // Retained as the canonical arguments for consumers of the original result shape.
            ["arguments"] = arguments.Clone(),
            ["proposedArguments"] = proposedArgs.Clone(),
            ["approval"] = new JsonObject
            {
                ["decision"] = decision,
                ["argumentsModifiedByUser"] = modified,
                ["message"] = ApprovalMessage(decision, modified),
            },
        };
        if (effectiveArgs != null) result["effectiveArguments"] = effectiveArgs.Clone();
        if (response != null) result["response"] = response.DeepClone();
        if (message != null) result[status == "error" ? "error" : "reason"] = message;
        return result.ToJsonString(ChatJson.Options);
    }

    static string ApprovalMessage(string decision, bool modified) => decision switch
    {
        "rejected" => "The user rejected the proposed API call. The API was not executed.",
        "approved" when modified => "The user changed the proposed arguments during approval. "
            + "effectiveArguments are the user-approved arguments and supersede proposedArguments "
            + "and the assistant's original tool-call arguments.",
        "approved" => "The user approved the proposed arguments without changes. "
            + "effectiveArguments are the arguments used to execute the API.",
        _ => "The approval did not execute this API.",
    };

    static JsonObject NormalizeArguments(JsonObject schema, JsonObject args) =>
        NormalizeSchemaValue(schema, args) as JsonObject ?? args.Clone();

    static JsonNode? NormalizeSchemaValue(JsonObject schema, JsonNode? value)
    {
        if (schema.GetString("type") == "array" && value is JsonArray array)
        {
            var itemSchema = schema["items"] as JsonObject ?? new JsonObject();
            return new JsonArray(array.Select(x => NormalizeSchemaValue(itemSchema, x)).ToArray());
        }
        if (schema.GetString("type") != "object" || value is not JsonObject obj)
            return value?.DeepClone();

        var properties = schema["properties"] as JsonObject ?? new JsonObject();
        var to = new JsonObject();
        foreach (var (key, childValue) in obj)
        {
            var canonicalName = properties.Select(x => x.Key)
                .FirstOrDefault(x => x.Equals(key, StringComparison.OrdinalIgnoreCase)) ?? key;
            var childSchema = properties[canonicalName] as JsonObject ?? new JsonObject();
            to[canonicalName] = NormalizeSchemaValue(childSchema, childValue);
        }
        return to;
    }

    static JsonObject ToDto(ChatToolApproval row, ChatToolApprovalBatch? batch) => new()
    {
        ["id"] = row.Id,
        ["batchId"] = row.BatchId,
        ["batchStatus"] = batch?.Status,
        ["threadId"] = row.ThreadId,
        ["toolCallId"] = row.ToolCallId,
        ["toolName"] = row.ToolName,
        ["apiName"] = row.ApiName,
        ["requestType"] = row.RequestType,
        ["method"] = row.Method,
        ["route"] = row.Route,
        ["safety"] = row.Safety,
        ["status"] = row.Status,
        ["description"] = row.Description,
        ["schema"] = ChatDtos.ParseJson(row.Schema),
        ["proposedArgs"] = ChatDtos.ParseJson(row.ProposedArgs),
        ["effectiveArgs"] = ChatDtos.ParseJson(row.EffectiveArgs),
        ["result"] = ResultDto(row.Result),
        ["error"] = row.Error,
        ["reason"] = row.Reason,
        ["createdAt"] = ChatDb.ToDateString(row.CreatedAt),
        ["updatedAt"] = ChatDb.ToDateString(row.UpdatedAt),
        ["resolvedAt"] = ChatDb.ToDateNode(row.ResolvedAt),
    };

    static JsonNode? ResultDto(string? result)
    {
        if (result == null) return null;
        try { return ChatJson.Parse(result); }
        catch { return result; }
    }

    static JsonObject BatchDto(ChatToolApprovalBatch row) => new()
    {
        ["id"] = row.Id,
        ["threadId"] = row.ThreadId,
        ["status"] = row.Status,
        ["createdAt"] = ChatDb.ToDateString(row.CreatedAt),
        ["updatedAt"] = ChatDb.ToDateString(row.UpdatedAt),
        ["completedAt"] = ChatDb.ToDateNode(row.CompletedAt),
    };
}
