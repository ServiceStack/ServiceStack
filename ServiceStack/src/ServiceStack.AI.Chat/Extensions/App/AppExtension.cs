using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.AI;

/// <summary>
/// The chat backend (port of llms-py's "app" extension): thread/request persistence, queued
/// background completions, the long-poll update channel that streams responses to the browser,
/// and the chat filters that record token/cost accounting.
/// </summary>
public partial class AppExtension() : ChatExtension("app")
{
    public ChatDb Db { get; private set; } = null!;
    public ThreadUpdates Updates { get; } = new();
    DbThreadApi threadApi = null!;

    public override void Install(ExtensionContext ctx)
    {
        Db = ctx.Feature.ChatDb ?? throw new Exception(
            "ChatFeature.ChatDb is required by the app extension — register an IDbConnectionFactory");
        if (ctx.Feature.AutoInitSchema)
        {
            Db.InitSchema();
        }

        threadApi = new DbThreadApi(Db, Updates, ctx.Log);
        ctx.Threads = threadApi;

        // let the orchestrator/streaming writer detect cancellation recorded in the DB
        ctx.Feature.CancelThreadCheck = context =>
        {
            if (context.ThreadId is not { } threadId)
                return false;
            var error = Db.GetThreadColumn<string>(threadId, nameof(ChatThread.Error), context.User);
            return error != null && error.Contains("cancel", StringComparison.OrdinalIgnoreCase);
        };

        RegisterThreadRoutes(ctx);
        RegisterRequestRoutes(ctx);
        RegisterAvatarRoutes(ctx);
        RegisterFilters(ctx);
        InstallDurableAgents(ctx);
    }

    // ── Threads ──

    void RegisterThreadRoutes(ExtensionContext ctx)
    {
        ctx.AddGet("threads", req =>
        {
            var query = QueryOf(req);
            var rows = Db.QueryThreads(query, GetTargetUser(req));
            // an Admin following a link to a specific thread they don't own still resolves it
            if (rows.Count == 0 && query.ContainsKey("id") && !query.ContainsKey("user")
                && Ctx.IsAdmin(req.Request))
            {
                rows = Db.QueryThreads(query, ChatDb.AllUsers);
            }
            return Task.FromResult<object?>(rows.ToDtos(ThreadListDto));
        });

        ctx.AddPost("threads", async req =>
        {
            var thread = await req.GetJsonBodyAsync().ConfigAwait();
            var user = req.UserName ?? ChatDb.DefaultUser;
            var now = DateTime.Now;
            var row = new ChatThread { User = user, CreatedAt = now, UpdatedAt = now };
            row.PopulateFrom(thread);
            row.Id = Db.InsertThread(row);
            var created = Db.GetThread(row.Id, user, includeMessages: false);
            return created != null ? ThreadWindowDto(created) : null;
        });

        // registered before threads/{id} so the literal route wins (aiohttp/registration order)
        ctx.AddGet("threads/sync", req =>
        {
            var query = QueryOf(req);
            var rows = Db.QueryThreads(query, req.UserName);
            return Task.FromResult<object?>(rows.ToDtos(ThreadListDto));
        });

        ctx.AddGet("threads/{id}", req =>
        {
            var thread = ResolveThread(req, ThreadId(req), out _, includeMessages: false);
            if (thread == null) return Task.FromResult<object?>((JsonNode)"");
            return Task.FromResult<object?>(req.QueryString("allMessages") == "true"
                ? FullThreadDto(thread) : ThreadWindowDto(thread));
        });

        ctx.AddGet("threads/{id}/messages", GetThreadMessagesAsync);

        ctx.AddPatch("threads/{id}", async req =>
        {
            var id = ThreadId(req);
            var thread = await req.GetJsonBodyAsync().ConfigAwait();
            if (ResolveThread(req, id, out var user, includeMessages: false) == null)
                throw new Exception("Thread not found");
            if (!await threadApi.UpdateThreadInternalAsync(id, thread, user).ConfigAwait())
                throw new Exception("Thread not found");
            Updates.NotifyThreadUpdate(id);
            var updated = Db.GetThread(id, user, includeMessages: false);
            return updated != null ? ThreadWindowDto(updated) : null;
        });

        ctx.AddDelete("threads/{id}", async req =>
        {
            var id = ThreadId(req);
            if (ResolveThread(req, id, out var user, includeMessages: false) != null)
            {
                if (Ctx.Feature.ToolApprovalCoordinator != null)
                    await Ctx.Feature.ToolApprovalCoordinator.CancelThreadAsync(id, user).ConfigAwait();
                Db.DeleteThread(id, user);
                Db.DeleteDurableThreadData(id);
            }
            return new JsonObject();
        });

        ctx.AddPost("threads/{id}/chat", QueueChatAsync);
        ctx.AddGet("threads/{id}/updates", GetThreadUpdatesAsync);
        ctx.AddGet("threads/{id}/updates/stream", GetThreadUpdatesStreamAsync);
        ctx.AddPost("threads/{id}/cancel", CancelThreadAsync);
        ctx.AddPost("threads/{id}/compact", CompactThreadAsync);
    }

    /// <summary>
    /// Load a thread as its owner, falling back to any user's for an Admin. <paramref name="user"/>
    /// receives the user the follow-up operation must run as — the owner when an Admin resolved
    /// someone else's thread, so an update or delete still targets the right row.
    /// </summary>
    ChatThread? ResolveThread(ChatRequestContext req, long id, out string? user, bool includeMessages = true)
    {
        user = req.UserName;
        var thread = Db.GetThread(id, user, includeMessages);
        if (thread != null || !Ctx.IsAdmin(req.Request))
            return thread;

        thread = Db.GetThread(id, ChatDb.AllUsers, includeMessages);
        if (thread != null)
            user = thread.User ?? ChatDb.AllUsers;
        return thread;
    }

    /// <summary>
    /// Queue a completion for this thread: merge and persist the request, create a durable run,
    /// wake the bounded async scheduler, and return immediately. SSE or long-poll then delivers it.
    /// </summary>
    async Task<object?> QueueChatAsync(ChatRequestContext req)
    {
        var (isAuthenticated, _) = Ctx.CheckAuth(req.Request);
        if (!isAuthenticated)
            return ChatResult.Unauthorized(Ctx.Feature.ErrorAuthRequired());

        var id = ThreadId(req);
        ResolveThread(req, id, out var user, includeMessages: false);
        if (Ctx.Feature.ToolApprovalCoordinator?.HasPending(id, user) == true)
            return ChatResult.Json(ChatJson.CreateErrorResponse(
                "Resolve or cancel the pending tool approval before sending another message", "ApprovalRequired"), 409);
        var chatReq = await req.GetJsonBodyAsync().ConfigAwait();

        if (Db.GetActiveAgentRun(id, user) is { } activeRun)
            return ChatResult.Json(ChatJson.CreateErrorResponse(
                $"Agent run {activeRun.Id} is already {activeRun.Status}", "RunActive"), 409);

        var messages = TimestampMessages(chatReq.GetArray("messages"));
        if (messages.Count == 0)
            throw new Exception("messages required");

        var thread = Db.GetThread(id, user, includeMessages: false)?.ToDto(includeMessages: false)
            ?? throw new Exception("Thread not found");

        var update = new JsonObject
        {
            ["messages"] = messages,
            // editing/redoing a message deliberately rewrites history, everything else may only
            // extend it (see DbThreadApi.PrepareThreadUpdate)
            [DbThreadApi.TruncateKey] = chatReq.GetBool(DbThreadApi.TruncateKey),
            ["tools"] = (chatReq.GetArray("tools") ?? thread.GetArray("tools"))?.Clone() ?? new JsonArray(),
            ["startedAt"] = ChatDb.ToDateString(DateTime.Now),
            ["completedAt"] = null,
            ["error"] = null,
            ["streamingMessage"] = null,
        };

        if (chatReq.GetString("model") is { } model)
            update["model"] = model;
        if (chatReq.GetObject("metadata") is { Count: > 0 } reqMetadata)
            update["metadata"] = reqMetadata.Clone();
        if (chatReq.GetArray("modalities") is { } modalities)
            update["modalities"] = modalities.Clone();
        else if (thread.GetArray("modalities") is not { Count: > 0 })
            update["modalities"] = new JsonArray("text");
        if (ChatToSystemPrompt(chatReq) is { } systemPrompt)
            update["systemPrompt"] = systemPrompt;

        // carry whitelisted per-thread request args
        var args = thread.GetObject("args")?.Clone() ?? new JsonObject();
        foreach (var entry in chatReq)
        {
            if (ChatFeature.RequestArgs.Contains(entry.Key))
                args[entry.Key] = entry.Value?.DeepClone();
        }
        update["args"] = args;

        var title = chatReq.GetString("title");
        if (title != null)
            update["title"] = title;
        else if (thread.GetString("title").IsNullOrEmpty())
            update["title"] = PromptToTitle(LastUserPrompt(chatReq));

        await threadApi.UpdateThreadInternalAsync(id, update, user).ConfigAwait();
        thread = Db.GetThread(id, user, includeMessages: false)?.ToDto(includeMessages: false)
            ?? throw new Exception("Thread not found");

        // Materialize the bounded response before creating/waking the run. A serialization failure
        // must not return HTTP 500 after the durable side effect has already started executing.
        var row = Db.GetThread(id, user, includeMessages: false)!;
        var result = ThreadWindowDto(row);
        var maxSteps = chatReq.GetObject("metadata").GetInt("maxSteps") ?? 250;
        var runId = Db.CreateAgentRun(id, user, thread.GetString("model"), maxSteps);
        result["run"] = Db.GetAgentRun(runId, user)?.ToDto();
        Scheduler.Enqueue(runId, ChatContext.DetachRequest(req.Request));

        return result;
    }

    /// <summary>Continue a paused tool-call turn without inventing another user message.</summary>
    internal async Task QueueContinuationAsync(long id, string? user, IRequest request)
    {
        var row = Db.GetThread(id, user, includeMessages: false) ?? throw new Exception("Thread not found");
        if (row.CompletedAt != null || row.Error != null)
            throw new Exception("Thread is no longer active");

        await threadApi.UpdateThreadAsync(id, new JsonObject
        {
            ["startedAt"] = ChatDb.ToDateString(DateTime.Now),
            ["completedAt"] = null,
            ["status"] = Ctx.NextLoadingMessage(),
            ["streamingMessage"] = null,
        }, user).ConfigAwait();

        var run = Db.GetActiveAgentRun(id, user);
        if (run == null)
        {
            run = Db.GetAgentRun(Db.CreateAgentRun(id, user, row.Model), user)!;
        }
        else
        {
            run.Status = AgentRunStatus.Queued;
            run.CompletedAt = null;
            run.Error = null;
            run.LeaseOwner = null;
            run.LeaseExpiresAt = null;
            Db.UpdateAgentRun(run);
        }
        Scheduler.Enqueue(run.Id, ChatContext.DetachRequest(request));
    }

    /// <summary>
    /// Long-poll for thread changes (port of get_thread_updates): the client sends the signature
    /// of the thread it last rendered; if the stored thread already differs (or is terminal) it's
    /// returned at once, otherwise the request parks for the configured timeout, re-checking the signature each
    /// time the streaming writer signals an update.
    /// </summary>
    async Task<object?> GetThreadUpdatesAsync(ChatRequestContext req)
    {
        var id = ThreadId(req);
        var user = req.UserName;
        var clientSig = req.QueryString("sig") ?? "";

        var thread = Db.GetThread(id, user, includeMessages: false) ?? throw new Exception("Thread not found");
        var dto = ThreadWindowDto(thread);
        var sig = dto.GetString("sig")!;

        // already terminal, or changed since the client last saw it → return immediately
        if (IsTerminal(thread) || (clientSig.Length > 0 && sig != clientSig))
            return dto;

        var deadline = DateTime.UtcNow + Updates.LongPollTimeout;
        while (DateTime.UtcNow < deadline)
        {
            // register the wakeup BEFORE re-reading so a signal can't slip through the gap
            var signal = Updates.NextSignalAsync(id);

            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
                break;

            using var timeoutCts = new CancellationTokenSource();
            var timeout = Task.Delay(remaining, timeoutCts.Token);
            var woke = await Task.WhenAny(signal, timeout).ConfigAwait();
            timeoutCts.Cancel(); // stop the pending timer
            if (woke == timeout)
                break;

            var updated = Db.GetThread(id, user, includeMessages: false);
            if (updated == null)
                break;
            var updatedDto = ThreadWindowDto(updated);
            if (IsTerminal(updated) || updatedDto.GetString("sig") != clientSig)
                return updatedDto;
        }
        return ThreadWindowDto(thread);
    }

    static bool IsTerminal(ChatThread thread) => thread.CompletedAt != null || thread.Error != null;

    async Task<object?> CancelThreadAsync(ChatRequestContext req)
    {
        var id = ThreadId(req);
        var user = req.UserName;
        var run = Db.GetActiveAgentRun(id, user);
        if (run != null)
        {
            run.Status = AgentRunStatus.Cancelled;
            run.CompletedAt = DateTime.Now;
            run.Error = "Request was canceled";
            run.LeaseOwner = null;
            run.LeaseExpiresAt = null;
            Db.UpdateAgentRun(run);
            Scheduler.Cancel(run.Id);
        }
        if (Ctx.Feature.ToolApprovalCoordinator != null)
            await Ctx.Feature.ToolApprovalCoordinator.CancelThreadAsync(id, user).ConfigAwait();
        await threadApi.UpdateThreadInternalAsync(id, new JsonObject
        {
            ["completedAt"] = ChatDb.ToDateString(DateTime.Now),
            ["error"] = "Request was canceled",
        }, user).ConfigAwait();
        Updates.NotifyThreadUpdate(id);
        var cancelled = Db.GetThread(id, user, includeMessages: false);
        return cancelled != null ? ThreadWindowDto(cancelled) : null;
    }

    /// <summary>Manual compaction uses the same engine as automatic snapshots, creating a child thread.</summary>
    async Task<object?> CompactThreadAsync(ChatRequestContext req)
    {
        var id = ThreadId(req);
        var user = req.UserName;
        var row = Db.GetThread(id, user, includeMessages: false) ?? throw new Exception("Thread not found");
        var messages = new JsonArray(Db.GetActiveMessagesAfter(id, 0)
            .Select(x => { x.Remove("_sequence"); return (JsonNode)x; }).ToArray());
        var metadata = ChatDtos.ParseJson(row.Metadata) as JsonObject ?? new JsonObject();
        var tokens = DurableAgentUtils.CountTokensApprox(messages);
        var contextLimit = (ChatDtos.ParseJson(row.ModelInfo) as JsonObject).GetObject("limit").GetLong("context");
        var target = Math.Max(2_000, Math.Min((long)(tokens * .3), contextLimit is > 0 ? contextLimit.Value / 2 : tokens));
        var compacted = await CompactMessagesAsync(messages, target,
            Math.Max(8_000, metadata.GetLong("compactChunkTokens") ?? 60_000), user,
            Math.Max(4, metadata.GetInt("compactRecentMessages") ?? 12), null, CancellationToken.None).ConfigAwait();
        foreach (var message in compacted.OfType<JsonObject>())
            message.Remove("_compaction");
        var now = DateTime.Now;
        var child = new ChatThread
        {
            User = row.User, Title = row.Title, SystemPrompt = row.SystemPrompt, Model = row.Model,
            ModelInfo = row.ModelInfo, Modalities = row.Modalities,
            Messages = compacted.ToJsonString(ChatJson.Options), Args = row.Args, Tools = row.Tools,
            ToolHistory = row.ToolHistory, Provider = row.Provider, ProviderModel = row.ProviderModel,
            Metadata = row.Metadata, Ref = row.Ref, ParentId = row.Id,
            ContextTokens = DurableAgentUtils.CountTokensApprox(compacted),
            CompletedAt = now, CreatedAt = now, UpdatedAt = now,
        };
        child.Id = Db.InsertThread(child);
        Db.SyncChatMessages(child.Id, compacted, rewrite: true);
        return new JsonObject { ["id"] = child.Id };
    }

    // ── Requests (accounting + analytics) ──

    /// <summary>
    /// Whose rows a request is asking for. ?user= is honoured only for Admins — for everyone else it's
    /// ignored and they get their own, so the parameter can't be used to read another user's history.
    /// </summary>
    string? GetTargetUser(ChatRequestContext req)
    {
        var requested = req.QueryString("user");
        return !string.IsNullOrEmpty(requested) && Ctx.IsAdmin(req.Request)
            ? requested
            : req.UserName;
    }

    void RegisterRequestRoutes(ExtensionContext ctx)
    {
        // registered before "requests" so the literal paths win over the generic query route
        Ctx.AddGet("requests/users", req =>
        {
            if (!Ctx.IsAdmin(req.Request))
                return Task.FromResult<object?>(ChatResult.Json(
                    ChatJson.CreateErrorResponse("Admin role required", "Forbidden"), 403));
            return Task.FromResult<object?>(Db.GetUsersSummary().ToDtos(x => x.ToDto()));
        });

        Ctx.AddGet("requests/users/list", req =>
        {
            if (!Ctx.IsAdmin(req.Request))
                return Task.FromResult<object?>(ChatResult.Json(
                    ChatJson.CreateErrorResponse("Admin role required", "Forbidden"), 403));
            return Task.FromResult<object?>(GetKnownUserNamesAsync(req));
        });

        Ctx.AddGet("requests/summary", req =>
            Task.FromResult<object?>(BuildSummary(Db.GetRequestsForSummary(GetTargetUser(req)))));

        Ctx.AddGet("requests/summary/{day}", req =>
        {
            var day = req.GetPathParam("day");
            var rows = Db.GetRequestsForSummary(GetTargetUser(req))
                .Where(x => ChatDb.ToDateString(x.CreatedAt).StartsWith(day, StringComparison.Ordinal))
                .ToList();
            return Task.FromResult<object?>(BuildDailySummary(rows));
        });

        Ctx.AddGet("requests", req =>
        {
            var rows = Db.QueryRequests(QueryOf(req), GetTargetUser(req));
            return Task.FromResult<object?>(rows.ToDtos(x => x.ToDto()));
        });

        Ctx.AddDelete("requests/{id}", req =>
        {
            if (long.TryParse(req.GetPathParam("id"), out var id))
                Db.DeleteRequest(id, req.UserName);
            return Task.FromResult<object?>(new JsonObject());
        });
    }

    /// <summary>
    /// Users an Admin can filter by: everyone who has made a request, plus whatever the host adds via
    /// ChatFeature.UserNamesResolver (llms-py merges its own users.json here; an Identity Auth host
    /// would resolve its user store instead, so it stays the host's call).
    /// </summary>
    JsonArray GetKnownUserNamesAsync(ChatRequestContext req)
    {
        var users = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var user in Db.GetUsersList())
            users.Add(user);

        if (Feature.UserNamesResolver is { } resolver)
        {
            try
            {
                foreach (var user in resolver(req.Request))
                    users.Add(user);
            }
            catch (Exception e)
            {
                Log.LogError(e, "UserNamesResolver failed");
            }
        }
        return new JsonArray(users.Select(x => (JsonNode)x).ToArray());
    }

    static JsonObject BuildSummary(List<ChatRequest> rows)
    {
        var dailyData = new JsonObject();
        var years = new SortedSet<int>();
        var totalCost = 0d;
        long totalInput = 0, totalOutput = 0;

        foreach (var row in rows)
        {
            var date = row.CreatedAt.ToString("yyyy-MM-dd");
            years.Add(row.CreatedAt.Year);

            var day = dailyData.GetObject(date);
            if (day == null)
            {
                day = new JsonObject
                {
                    ["cost"] = 0d, ["requests"] = 0, ["inputTokens"] = 0L, ["outputTokens"] = 0L,
                };
                dailyData[date] = day;
            }
            day["cost"] = (day.GetDouble("cost") ?? 0) + (row.Cost ?? 0);
            day["requests"] = (day.GetInt("requests") ?? 0) + 1;
            day["inputTokens"] = (day.GetLong("inputTokens") ?? 0) + (row.InputTokens ?? 0);
            day["outputTokens"] = (day.GetLong("outputTokens") ?? 0) + (row.OutputTokens ?? 0);

            totalCost += row.Cost ?? 0;
            totalInput += row.InputTokens ?? 0;
            totalOutput += row.OutputTokens ?? 0;
        }

        return new JsonObject
        {
            ["dailyData"] = dailyData,
            ["years"] = new JsonArray(years.Select(x => (JsonNode)x).ToArray()),
            ["totalCost"] = totalCost,
            ["totalRequests"] = rows.Count,
            ["totalInputTokens"] = totalInput,
            ["totalOutputTokens"] = totalOutput,
        };
    }

    /// <summary>
    /// Model/Provider rollups for a single day's pie charts
    /// (port of get_daily_request_summary, extensions/app/db.py:520).
    /// </summary>
    static JsonObject BuildDailySummary(List<ChatRequest> rows) => new()
    {
        ["modelData"] = GroupSummary(rows, x => x.Model),
        ["providerData"] = GroupSummary(rows, x => x.Provider),
    };

    static JsonObject GroupSummary(List<ChatRequest> rows, Func<ChatRequest, string?> keyFn)
    {
        var to = new JsonObject();
        foreach (var group in rows.Where(x => keyFn(x) != null).GroupBy(keyFn))
        {
            to[group.Key!] = new JsonObject
            {
                ["cost"] = group.Sum(x => x.Cost ?? 0),
                ["count"] = group.Count(),
                ["duration"] = group.Sum(x => x.Duration ?? 0),
                ["tokens"] = group.Sum(x => (x.InputTokens ?? 0) + (x.OutputTokens ?? 0)),
                ["inputTokens"] = group.Sum(x => x.InputTokens ?? 0),
                ["outputTokens"] = group.Sum(x => x.OutputTokens ?? 0),
            };
        }
        return to;
    }

    // ── Chat pipeline filters (persistence + token/cost accounting) ──

    void RegisterFilters(ExtensionContext ctx)
    {
        ctx.RegisterChatRequestFilter(OnChatRequestAsync);
        ctx.RegisterChatToolFilter(OnChatToolAsync);
        ctx.RegisterChatApprovalFilter(OnChatApprovalAsync);
        ctx.RegisterChatStatusFilter(OnChatStatusAsync);
        ctx.RegisterChatResponseFilter(OnChatResponseAsync);
        ctx.RegisterChatErrorFilter(OnChatErrorAsync);
    }

    async Task OnChatRequestAsync(JsonObject chat, ChatContext context)
    {
        if (context.NoHistory || context.ThreadId is not { } threadId)
            return;
        context.SeedMessageTimestamps(chat.GetArray("messages"));
        if (context.ProjectedContext)
        {
            // Snapshot summaries are projection-only and normally have no timestamp. Give every
            // baseline item an identity before tool checkpoint reconciliation so a later filter
            // cannot mistake synthetic context for a newly produced canonical message.
            foreach (var message in chat.GetArray("messages")?.OfType<JsonObject>() ?? [])
                message["timestamp"] ??= context.NextMessageTimestamp();
            context.SeedMessageTimestamps(chat.GetArray("messages"));
        }
        await threadApi.UpdateThreadAsync(threadId, new JsonObject
        {
            ["status"] = Ctx.NextLoadingMessage(),
            ["streamingMessage"] = null, // a previous attempt's partial is stale now
        }, context.User).ConfigAwait();
    }

    async Task OnChatToolAsync(JsonObject chat, ChatContext context)
    {
        if (context.NoHistory || context.ThreadId is not { } threadId)
            return;
        if (context.ProjectedContext)
        {
            var append = new JsonArray();
            foreach (var node in chat.GetArray("messages") ?? [])
            {
                if (node is not JsonObject message) continue;
                message["timestamp"] ??= context.NextMessageTimestamp();
                var timestamp = message.GetLong("timestamp")!.Value;
                if (context.ProjectedKnownTimestamps.Add(timestamp)) append.Add(message.Clone());
            }
            if (append.Count > 0)
            {
                Db.SyncChatMessages(threadId, append, runId: context.RunId, stepId: context.StepId);
                var row = Db.GetThread(threadId, context.User, includeMessages: false);
                if (row != null)
                {
                    var canonical = Db.GetActiveMessagesAfter(threadId, 0);
                    row.Messages = new JsonArray(canonical.Select(x => { x.Remove("_sequence"); return (JsonNode)x; }).ToArray())
                        .ToJsonString(ChatJson.Options);
                    row.Status = Ctx.NextLoadingMessage();
                    row.UpdatedAt = DateTime.Now;
                    // The turn that produced these messages has finished streaming and is now
                    // durable; its checkpoint is stale. Leaving it behind renders the message twice
                    // (committed + partial) until the next turn's first chunk overwrites it.
                    row.StreamingMessage = null;
                    Db.UpdateThreadFields(row,
                        [nameof(ChatThread.Messages), nameof(ChatThread.Status),
                            nameof(ChatThread.StreamingMessage), nameof(ChatThread.UpdatedAt)],
                        context.User);
                    Updates.NotifyThreadUpdate(threadId);
                }
            }
        }
        else
        {
            await threadApi.UpdateThreadAsync(threadId, new JsonObject
            {
                ["messages"] = chat.GetArray("messages")?.Clone() ?? new JsonArray(),
                ["status"] = Ctx.NextLoadingMessage(),
                ["streamingMessage"] = null, // the streamed turn is committed, its partial is stale
            }, context.User).ConfigAwait();
        }

        var completedAt = Db.GetThreadColumn<DateTime?>(threadId, nameof(ChatThread.CompletedAt), context.User);
        if (completedAt != null)
            context.Items["completed"] = true;
    }

    async Task OnChatStatusAsync(string status, ChatContext context)
    {
        if (context.NoHistory || context.ThreadId is not { } threadId)
            return;
        await threadApi.UpdateThreadAsync(threadId, new JsonObject { ["status"] = status }, context.User).ConfigAwait();
    }

    /// <summary>Account for a provider turn that stopped at a durable approval boundary.</summary>
    async Task OnChatApprovalAsync(JsonObject response, ChatContext context)
    {
        var chat = context.Chat;
        var usage = response.GetObject("usage");
        if (usage == null || chat == null)
            return;

        var user = context.User;
        var threadId = context.ThreadId;
        var modelInfo = context.ModelInfo ?? new JsonObject();
        var modelCost = context.ModelCost ?? modelInfo.GetObject("cost")
            ?? new JsonObject { ["input"] = 0, ["output"] = 0 };
        var duration = context.Items.GetValueOrDefault("duration") as long? ?? 0;
        var completedAt = DateTime.Now;
        var inputPrice = modelCost.GetDouble("input") ?? 0;
        var outputPrice = modelCost.GetDouble("output") ?? 0;
        var inputTokens = usage.GetLong("prompt_tokens") ?? 0;
        var outputTokens = usage.GetLong("completion_tokens") ?? 0;
        var isPerRequest = modelCost.GetString("type") == "request";
        var cost = usage.GetDouble("cost") ?? response.GetDouble("cost")
            ?? (inputPrice * inputTokens + outputPrice * outputTokens) / 1_000_000;
        if (isPerRequest)
            cost = usage.GetDouble("cost") ?? (outputPrice > 0 ? outputPrice : cost);

        if (!context.NoStore)
        {
            Db.InsertRequest(new ChatRequest
            {
                User = user ?? ChatDb.DefaultUser,
                Model = modelInfo.GetString("name") ?? modelInfo.GetString("id") ?? chat.GetString("model"),
                Duration = duration,
                Cost = cost,
                InputPrice = inputPrice,
                InputTokens = inputTokens,
                InputCachedTokens = usage.GetLong("inputCachedTokens") ?? 0,
                OutputPrice = outputPrice,
                OutputTokens = outputTokens,
                TotalTokens = usage.GetLong("total_tokens") ?? inputTokens + outputTokens,
                Usage = ChatDtos.ToJson(usage),
                FinishReason = "requires_action",
                Provider = context.Provider?.Id,
                ProviderModel = response.GetString("model"),
                ProviderRef = response.GetString("provider"),
                ThreadId = threadId,
                Title = context.Items.GetValueOrDefault("title") as string ?? PromptToTitle(LastUserPrompt(chat)),
                StartedAt = context.Items.GetValueOrDefault("startedAt") as DateTime?,
                CompletedAt = completedAt,
                CreatedAt = completedAt,
                UpdatedAt = completedAt,
                Ref = response.GetString("id"),
            });
        }

        if (threadId is { } id && !context.NoHistory)
            await UpdateThreadTotalsAsync(id, user, duration, isPerRequest).ConfigAwait();
        if (context.RunId is { } runId && Db.GetAgentRun(runId, ChatDb.AllUsers) is { } run)
        {
            run.Status = AgentRunStatus.WaitingApproval;
            run.LeaseOwner = null;
            run.LeaseExpiresAt = null;
            Db.UpdateAgentRun(run);
        }
    }

    /// <summary>Record the completed request + write the assistant message and cost stats to the thread</summary>
    async Task OnChatResponseAsync(JsonObject response, ChatContext context)
    {
        var chat = context.Chat;
        var usage = response.GetObject("usage");
        if (usage == null && chat == null)
            return;

        var user = context.User;
        var threadId = context.ThreadId;
        var modelInfo = context.ModelInfo ?? new JsonObject();
        var modelCost = context.ModelCost ?? modelInfo.GetObject("cost")
            ?? new JsonObject { ["input"] = 0, ["output"] = 0 };
        var duration = context.Items.GetValueOrDefault("duration") as long? ?? 0;
        var completedAt = DateTime.Now;

        var model = modelInfo.GetString("name") ?? modelInfo.GetString("id") ?? chat.GetString("model");
        var finishReason = response.GetArray("choices") is { Count: > 0 } choices
            ? (choices[0] as JsonObject).GetString("finish_reason")
            : null;
        var inputPrice = modelCost.GetDouble("input") ?? 0;
        var outputPrice = modelCost.GetDouble("output") ?? 0;
        var inputTokens = usage.GetLong("prompt_tokens") ?? 0;
        var outputTokens = usage.GetLong("completion_tokens") ?? 0;
        var totalTokens = usage.GetLong("total_tokens") ?? inputTokens + outputTokens;

        var isPerRequest = modelCost.GetString("type") == "request";
        var cost = usage.GetDouble("cost")
            ?? response.GetDouble("cost")
            ?? (inputPrice * inputTokens + outputPrice * outputTokens) / 1_000_000;
        if (isPerRequest)
            cost = usage.GetDouble("cost") ?? (outputPrice > 0 ? outputPrice : cost);

        var title = context.Items.GetValueOrDefault("title") as string
            ?? PromptToTitle(chat != null ? LastUserPrompt(chat) : null);

        if (!context.NoStore)
        {
            Db.InsertRequest(new ChatRequest
            {
                User = user ?? ChatDb.DefaultUser,
                Model = model,
                Duration = duration,
                Cost = cost,
                InputPrice = inputPrice,
                InputTokens = inputTokens,
                InputCachedTokens = usage.GetLong("inputCachedTokens") ?? 0,
                OutputPrice = outputPrice,
                OutputTokens = outputTokens,
                TotalTokens = totalTokens,
                Usage = ChatDtos.ToJson(usage),
                FinishReason = finishReason,
                Provider = context.Provider?.Id,
                ProviderModel = response.GetString("model"),
                ProviderRef = response.GetString("provider"),
                ThreadId = threadId,
                Title = title,
                StartedAt = context.Items.GetValueOrDefault("startedAt") as DateTime?,
                CompletedAt = completedAt,
                CreatedAt = completedAt,
                UpdatedAt = completedAt,
                Ref = response.GetString("id"),
            });
        }

        if (threadId is not { } id || context.NoHistory)
            return;

        // Append to the conversation the thread already has rather than rebuilding it from the
        // request: the request's copy is missing anything appended while it was in flight (tool
        // call/result messages) and has been rewritten for the provider, so writing it back would
        // drop messages.
        var canonical = Db.GetActiveMessagesAfter(id, 0);
        var messages = canonical.Count > 0
            ? new JsonArray(canonical.Select(x => { x.Remove("_sequence"); return (JsonNode)x; }).ToArray())
            : chat.GetArray("messages").WithoutStreamingMessages();
        var outputCost = isPerRequest ? cost : inputPrice * inputTokens / 1_000_000;
        var lastRole = messages.Count > 0 ? (messages[^1] as JsonObject).GetString("role") : null;
        if (lastRole is "user" or "tool")
        {
            var userMessage = (messages[^1] as JsonObject)!;
            userMessage["model"] = model;
            userMessage["usage"] = new JsonObject
            {
                ["tokens"] = inputTokens,
                ["price"] = inputPrice,
                ["cost"] = outputCost,
            };
        }

        var assistantMessage = ChatResponseToMessage(response);
        assistantMessage["timestamp"] ??= context.NextMessageTimestamp();
        assistantMessage["model"] = model;
        var assistantUsage = new JsonObject
        {
            ["tokens"] = outputTokens,
            ["price"] = outputPrice,
            ["cost"] = outputCost,
            ["duration"] = duration,
        };
        if (isPerRequest)
            assistantUsage["type"] = "request";
        assistantMessage["usage"] = assistantUsage;
        messages.Add(assistantMessage);

        var update = new JsonObject
        {
            ["model"] = model,
            ["provider"] = context.Provider?.Id,
            ["providerModel"] = response.GetString("model"),
            ["modelInfo"] = modelInfo.Clone(),
            ["messages"] = messages,
            ["tools"] = chat.GetArray("tools")?.Clone() ?? new JsonArray(),
            ["completedAt"] = ChatDb.ToDateString(completedAt),
            ["status"] = null,
            ["streamingMessage"] = null, // the in-flight message is now committed
        };
        if (response.GetArray("tool_history") is { } toolHistory)
            update["toolHistory"] = toolHistory.Clone();
        if (context.ProviderResponse is { } providerResponse)
            update["providerResponse"] = TruncateLongStrings(providerResponse.Clone());

        await threadApi.UpdateThreadAsync(id, update, user).ConfigAwait();
        // The compatibility history update inserts the assistant row. Attach durable provenance to
        // that canonical row without duplicating it (the timestamp is its stable identity).
        if (context.RunId != null || context.StepId != null)
            Db.SyncChatMessages(id, new JsonArray(assistantMessage.Clone()),
                runId: context.RunId, stepId: context.StepId);

        await UpdateThreadTotalsAsync(id, user, duration, isPerRequest).ConfigAwait();
    }

    async Task UpdateThreadTotalsAsync(long id, string? user, long duration, bool isPerRequest)
    {
        // roll thread totals up from its recorded requests
        var threadRequests = Db.QueryRequests(new JsonObject { ["threadId"] = id, ["take"] = 10000 }, user);
        var totalCost = threadRequests.Sum(x => x.Cost ?? 0);
        var totalInput = threadRequests.Sum(x => x.InputTokens ?? 0);
        var totalOutput = threadRequests.Sum(x => x.OutputTokens ?? 0);
        var stats = new JsonObject
        {
            ["inputTokens"] = totalInput,
            ["outputTokens"] = totalOutput,
            ["cost"] = totalCost,
            ["duration"] = duration,
            ["requests"] = threadRequests.Count,
        };
        if (isPerRequest)
            stats["type"] = "request";

        await threadApi.UpdateThreadAsync(id, new JsonObject
        {
            ["inputTokens"] = totalInput,
            ["outputTokens"] = totalOutput,
            ["cost"] = totalCost,
            ["stats"] = stats,
        }, user).ConfigAwait();
    }

    async Task OnChatErrorAsync(Exception e, ChatContext context)
    {
        var error = ChatJson.ToErrorMessage(e);
        var chat = context.Chat;
        var completedAt = DateTime.Now;
        var user = context.User;
        var title = context.Items.GetValueOrDefault("title") as string
            ?? PromptToTitle(chat != null ? LastUserPrompt(chat) : null);

        if (context.ThreadId is { } threadId && !context.NoHistory)
        {
            await threadApi.UpdateThreadAsync(threadId, new JsonObject
            {
                ["completedAt"] = ChatDb.ToDateString(completedAt),
                ["error"] = error,
                ["status"] = null,
            }, user).ConfigAwait();
        }

        if (!context.NoStore)
        {
            Db.InsertRequest(new ChatRequest
            {
                User = user ?? ChatDb.DefaultUser,
                Model = chat.GetString("model"),
                Title = title,
                ThreadId = context.ThreadId,
                StartedAt = context.Items.GetValueOrDefault("startedAt") as DateTime?,
                CompletedAt = completedAt,
                CreatedAt = completedAt,
                UpdatedAt = completedAt,
                Error = error,
                StackTrace = e.StackTrace,
            });
        }
    }

    // ── Helpers ──

    static long ThreadId(ChatRequestContext req) =>
        long.TryParse(req.GetPathParam("id"), out var id)
            ? id
            : throw new ArgumentException($"Invalid thread id: {req.GetPathParam("id")}");

    static JsonObject QueryOf(ChatRequestContext req)
    {
        var query = new JsonObject();
        foreach (var key in req.Request.QueryString.AllKeys)
        {
            if (key != null)
                query[key] = req.Request.QueryString[key];
        }
        return query;
    }

    /// <summary>
    /// Assign unique, increasing timestamps to messages that don't have one, dropping any in-flight
    /// message a client echoed back from a merged DTO (it belongs to a stream still in progress and
    /// must not become durable history).
    /// </summary>
    static JsonArray TimestampMessages(JsonArray? messages)
    {
        var ret = messages.WithoutStreamingMessages();
        var maxExisting = ret.OfType<JsonObject>().Select(x => x.GetLong("timestamp") ?? 0).DefaultIfEmpty().Max();
        var timestamp = Math.Max(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), maxExisting + 1);
        foreach (var messageNode in ret)
        {
            if (messageNode is JsonObject message && !message.ContainsKey("timestamp"))
            {
                message["timestamp"] = timestamp++;
            }
        }
        return ret;
    }

    // message helpers live in ChatMessages so providers/generators can share them
    public static string? ChatToSystemPrompt(JsonObject chat) => ChatMessages.ChatToSystemPrompt(chat);
    public static string? LastUserPrompt(JsonObject chat) => ChatMessages.LastUserPrompt(chat);
    public static JsonObject ChatResponseToMessage(JsonObject response) => ChatMessages.ChatResponseToMessage(response);

    public static string? PromptToTitle(string? prompt)
    {
        if (string.IsNullOrEmpty(prompt))
            return null;
        var title = prompt.Trim().Replace('\n', ' ');
        return title.Length > 60 ? title[..60] + "..." : title;
    }

    /// <summary>Rough token estimate used for compaction targets (port of count_tokens_approx)</summary>
    public static long CountTokensApprox(string text) => text.Length / 4;

    static void SubstituteVars(JsonNode? node, Dictionary<string, string> vars)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(x => x.Key).ToList())
                {
                    if (obj[key] is JsonValue v && v.TryGetValue<string>(out var s))
                    {
                        foreach (var entry in vars)
                            s = s.Replace(entry.Key, entry.Value);
                        obj[key] = s;
                    }
                    else
                    {
                        SubstituteVars(obj[key], vars);
                    }
                }
                break;
            case JsonArray arr:
                for (var i = 0; i < arr.Count; i++)
                {
                    if (arr[i] is JsonValue v && v.TryGetValue<string>(out var s))
                    {
                        foreach (var entry in vars)
                            s = s.Replace(entry.Key, entry.Value);
                        arr[i] = s;
                    }
                    else
                    {
                        SubstituteVars(arr[i], vars);
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Replace very long strings with "(length)" before persisting provider responses.
    /// Mutates in place — reassigning an unchanged child back into its own parent is rejected
    /// by System.Text.Json ("the node already has a parent").
    /// </summary>
    public static JsonNode? TruncateLongStrings(JsonNode? node, int maxLength = 10000,
        bool displayed = false)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(x => x.Key).ToList())
                {
                    var childDisplayed = displayed || key == "groundingMetadata";
                    if (LongStringLength(obj[key], maxLength) is { } len)
                        obj[key] = childDisplayed
                            ? obj[key]!.GetValue<string>()[..maxLength] + "…"
                            : $"({len})";
                    else
                        TruncateLongStrings(obj[key], maxLength, childDisplayed);
                }
                return obj;
            case JsonArray arr:
                for (var i = 0; i < arr.Count; i++)
                {
                    if (LongStringLength(arr[i], maxLength) is { } len)
                        arr[i] = displayed
                            ? arr[i]!.GetValue<string>()[..maxLength] + "…"
                            : $"({len})";
                    else
                        TruncateLongStrings(arr[i], maxLength, displayed);
                }
                return arr;
            default:
                if (LongStringLength(node, maxLength) is not { } rootLen)
                    return node;
                return displayed ? node!.GetValue<string>()[..maxLength] + "…" : $"({rootLen})";
        }
    }

    static int? LongStringLength(JsonNode? node, int maxLength) =>
        node is JsonValue v && v.TryGetValue<string>(out var s) && s.Length > maxLength
            ? s.Length
            : null;
}
