using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

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
    }

    // ── Threads ──

    void RegisterThreadRoutes(ExtensionContext ctx)
    {
        ctx.AddGet("threads", req =>
        {
            var query = QueryOf(req);
            var rows = Db.QueryThreads(query, req.UserName);
            return Task.FromResult<object?>(rows.ToDtos(x => x.ToDto()));
        });

        ctx.AddPost("threads", async req =>
        {
            var thread = await req.GetJsonBodyAsync().ConfigAwait();
            var user = req.UserName ?? ChatDb.DefaultUser;
            var now = DateTime.Now;
            var row = new ChatThread { User = user, CreatedAt = now, UpdatedAt = now };
            row.PopulateFrom(thread);
            row.Id = Db.InsertThread(row);
            return Db.GetThread(row.Id, user)?.ToDto();
        });

        // registered before threads/{id} so the literal route wins (aiohttp/registration order)
        ctx.AddGet("threads/sync", req =>
        {
            var query = QueryOf(req);
            var rows = Db.QueryThreads(query, req.UserName);
            return Task.FromResult<object?>(rows.ToDtos(x => x.ToDto()));
        });

        ctx.AddGet("threads/{id}", req =>
        {
            var thread = Db.GetThread(ThreadId(req), req.UserName);
            return Task.FromResult<object?>(thread != null ? thread.ToDto() : (JsonNode)"");
        });

        ctx.AddPatch("threads/{id}", async req =>
        {
            var id = ThreadId(req);
            var thread = await req.GetJsonBodyAsync().ConfigAwait();
            var user = req.UserName;
            if (!await threadApi.UpdateThreadInternalAsync(id, thread, user).ConfigAwait())
                throw new Exception("Thread not found");
            Updates.NotifyThreadUpdate(id);
            return Db.GetThread(id, user)?.ToDto();
        });

        ctx.AddDelete("threads/{id}", req =>
        {
            Db.DeleteThread(ThreadId(req), req.UserName);
            return Task.FromResult<object?>(new JsonObject());
        });

        ctx.AddPost("threads/{id}/chat", QueueChatAsync);
        ctx.AddGet("threads/{id}/updates", GetThreadUpdatesAsync);
        ctx.AddPost("threads/{id}/cancel", CancelThreadAsync);
        ctx.AddPost("threads/{id}/compact", CompactThreadAsync);
    }

    /// <summary>
    /// Queue a completion for this thread: merge the request into the thread, start the
    /// orchestrator on a background task, and return immediately — the UI then long-polls
    /// threads/{id}/updates to receive the streamed response.
    /// </summary>
    async Task<object?> QueueChatAsync(ChatRequestContext req)
    {
        var (isAuthenticated, _) = Ctx.CheckAuth(req.Request);
        if (!isAuthenticated)
            return ChatResult.Unauthorized(Ctx.Feature.ErrorAuthRequired());

        var id = ThreadId(req);
        var user = req.UserName;
        var chatReq = await req.GetJsonBodyAsync().ConfigAwait();

        var messages = TimestampMessages(chatReq.GetArray("messages"));
        if (messages.Count == 0)
            throw new Exception("messages required");

        var thread = Db.GetThread(id, user)?.ToDto()
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
        thread = Db.GetThread(id, user)?.ToDto() ?? throw new Exception("Thread not found");

        var metadata = thread.GetObject("metadata")?.Clone() ?? new JsonObject();
        var chat = new JsonObject
        {
            ["model"] = thread.GetString("model"),
            // an in-flight message merged into the DTO must not be sent to the provider either
            ["messages"] = thread.GetArray("messages").WithoutStreamingMessages(),
            ["modalities"] = thread.GetArray("modalities")?.Clone(),
            ["tools"] = thread.GetArray("tools")?.Clone(),
            ["metadata"] = metadata.Clone(),
        };
        foreach (var entry in thread.GetObject("args") ?? [])
        {
            if (ChatFeature.RequestArgs.Contains(entry.Key))
                chat[entry.Key] = entry.Value?.DeepClone();
        }

        var cts = Updates.StartChatTask(id);
        var context = new ChatContext
        {
            Chat = chat,
            User = user,
            // the completion runs on a background task, outliving this HTTP request
            Request = ChatContext.DetachRequest(req.Request),
            ThreadId = id,
            Tools = metadata.GetString("tools") ?? "all",
            CancellationToken = cts.Token,
        };

        _ = Task.Run(async () =>
        {
            try
            {
                await Ctx.ChatCompletionAsync(chat, context).ConfigAwait();
            }
            catch (OperationCanceledException)
            {
                Ctx.Log.LogDebug("Chat cancelled for thread {ThreadId}", id);
            }
            catch (Exception e)
            {
                Ctx.Log.LogError(e, "Chat failed for thread {ThreadId}", id);
                // chat_error filter normally records this; ensure the thread isn't left running
                var current = Db.GetThread(id, user);
                if (current != null && current.Error == null)
                {
                    await OnChatErrorAsync(e, context).ConfigAwait();
                }
            }
            finally
            {
                Updates.CompleteChatTask(id, cts);
                Updates.NotifyThreadUpdate(id);
            }
        }, CancellationToken.None);

        return thread;
    }

    /// <summary>
    /// Long-poll for thread changes (port of get_thread_updates): the client sends the signature
    /// of the thread it last rendered; if the stored thread already differs (or is terminal) it's
    /// returned at once, otherwise the request parks for up to 10s, re-checking the signature each
    /// time the streaming writer signals an update.
    /// </summary>
    async Task<object?> GetThreadUpdatesAsync(ChatRequestContext req)
    {
        var id = ThreadId(req);
        var user = req.UserName;
        var clientSig = req.QueryString("sig") ?? "";

        var thread = Db.GetThread(id, user) ?? throw new Exception("Thread not found");
        var dto = thread.ToDto();

        // already terminal, or changed since the client last saw it → return immediately
        if (IsTerminal(thread) || (clientSig.Length > 0 && thread.Sig != clientSig))
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

            var updated = Db.GetThread(id, user);
            if (updated == null)
                break;
            if (IsTerminal(updated) || updated.Sig != clientSig)
                return updated.ToDto();
        }
        return dto;
    }

    static bool IsTerminal(ChatThread thread) => thread.CompletedAt != null || thread.Error != null;

    async Task<object?> CancelThreadAsync(ChatRequestContext req)
    {
        var id = ThreadId(req);
        var user = req.UserName;
        Updates.CancelChatTask(id);
        await threadApi.UpdateThreadInternalAsync(id, new JsonObject
        {
            ["completedAt"] = ChatDb.ToDateString(DateTime.Now),
            ["error"] = "Request was canceled",
        }, user).ConfigAwait();
        Updates.NotifyThreadUpdate(id);
        return Db.GetThread(id, user)?.ToDto();
    }

    /// <summary>Summarize a long conversation using the "compact" template from llms.json</summary>
    async Task<object?> CompactThreadAsync(ChatRequestContext req)
    {
        var id = ThreadId(req);
        var user = req.UserName;
        var thread = Db.GetThread(id, user)?.ToDto() ?? throw new Exception("Thread not found");

        var compactTemplate = Ctx.GetConfigDefaults().GetObject("compact")
            ?? throw new Exception("No 'compact' template configured in llms.json defaults");

        var messages = thread.GetArray("messages") ?? new JsonArray();
        var messagesJson = messages.ToJsonString(ChatJson.Options);
        var contextTokens = thread.GetLong("contextTokens") ?? CountTokensApprox(messagesJson);

        var chat = compactTemplate.Clone();
        // the bundled template names a specific model; fall back to the thread's own model
        // when that provider isn't enabled on this host
        if (chat.GetString("model") is not { } compactModel
            || !Ctx.Feature.Providers.Values.Any(x => x.ProviderModel(compactModel) != null))
        {
            chat["model"] = thread.GetString("model");
        }

        var vars = new Dictionary<string, string>
        {
            ["{message_count}"] = messages.Count.ToString(),
            ["{token_count}"] = contextTokens.ToString(),
            ["{target_tokens}"] = (contextTokens / 4).ToString(),
            ["{messages_json}"] = messagesJson,
        };
        SubstituteVars(chat, vars);

        var context = new ChatContext { Chat = chat, User = user, Tools = "none", NoHistory = true, NoStore = true };
        var response = await Ctx.ChatCompletionAsync(chat, context).ConfigAwait();

        var summary = response.GetArray("choices") is { Count: > 0 } choices
            ? (choices[0] as JsonObject).GetObject("message").GetString("content")
            : null;
        if (summary == null)
            throw new Exception("Failed to compact thread");

        var compacted = new JsonArray(new JsonObject
        {
            ["role"] = "user",
            ["content"] = summary,
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        });
        await threadApi.UpdateThreadAsync(id, new JsonObject
        {
            ["messages"] = compacted,
            [DbThreadApi.TruncateKey] = true, // compacting is an explicit rewrite of history
        }, user).ConfigAwait();
        return Db.GetThread(id, user)?.ToDto();
    }

    // ── Requests (accounting + analytics) ──

    void RegisterRequestRoutes(ExtensionContext ctx)
    {
        Ctx.AddGet("requests/summary", req =>
            Task.FromResult<object?>(BuildSummary(Db.GetRequestsForSummary(req.UserName))));

        Ctx.AddGet("requests/summary/{day}", req =>
        {
            var day = req.GetPathParam("day");
            var rows = Db.GetRequestsForSummary(req.UserName)
                .Where(x => ChatDb.ToDateString(x.CreatedAt).StartsWith(day, StringComparison.Ordinal))
                .ToList();
            return Task.FromResult<object?>(BuildDailySummary(rows));
        });

        Ctx.AddGet("requests", req =>
        {
            var rows = Db.QueryRequests(QueryOf(req), req.UserName);
            return Task.FromResult<object?>(rows.ToDtos(x => x.ToDto()));
        });

        Ctx.AddDelete("requests/{id}", req =>
        {
            if (long.TryParse(req.GetPathParam("id"), out var id))
                Db.DeleteRequest(id, req.UserName);
            return Task.FromResult<object?>(new JsonObject());
        });
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
        ctx.RegisterChatStatusFilter(OnChatStatusAsync);
        ctx.RegisterChatResponseFilter(OnChatResponseAsync);
        ctx.RegisterChatErrorFilter(OnChatErrorAsync);
    }

    async Task OnChatRequestAsync(JsonObject chat, ChatContext context)
    {
        if (context.NoHistory || context.ThreadId is not { } threadId)
            return;
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
        await threadApi.UpdateThreadAsync(threadId, new JsonObject
        {
            ["messages"] = chat.GetArray("messages")?.Clone() ?? new JsonArray(),
            ["status"] = Ctx.NextLoadingMessage(),
        }, context.User).ConfigAwait();

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
        var stored = Db.GetThread(id, user)?.ToDto().GetArray("messages");
        var messages = stored is { Count: > 0 }
            ? stored.WithoutStreamingMessages()
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
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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
    public static JsonNode? TruncateLongStrings(JsonNode? node, int maxLength = 10000)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(x => x.Key).ToList())
                {
                    if (LongStringLength(obj[key], maxLength) is { } len)
                        obj[key] = $"({len})";
                    else
                        TruncateLongStrings(obj[key], maxLength);
                }
                return obj;
            case JsonArray arr:
                for (var i = 0; i < arr.Count; i++)
                {
                    if (LongStringLength(arr[i], maxLength) is { } len)
                        arr[i] = $"({len})";
                    else
                        TruncateLongStrings(arr[i], maxLength);
                }
                return arr;
            default:
                return LongStringLength(node, maxLength) is { } rootLen ? $"({rootLen})" : node;
        }
    }

    static int? LongStringLength(JsonNode? node, int maxLength) =>
        node is JsonValue v && v.TryGetValue<string>(out var s) && s.Length > maxLength
            ? s.Length
            : null;
}
