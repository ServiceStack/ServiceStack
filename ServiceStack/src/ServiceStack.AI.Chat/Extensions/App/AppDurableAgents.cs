using System.Text;
using System.Text.Json.Nodes;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.AI;

public partial class AppExtension
{
    public AgentScheduler Scheduler { get; private set; } = null!;

    void InstallDurableAgents(ExtensionContext ctx)
    {
        var config = ctx.GetConfigDefaults().GetObject("agent") ?? new JsonObject();
        Scheduler = new AgentScheduler(Db, ExecuteAgentSliceAsync, Updates, ctx.Log,
            config.GetInt("maxConcurrency") ?? 2,
            config.GetDouble("pollSeconds") ?? 1,
            config.GetInt("leaseSeconds") ?? 300);
        Scheduler.Start();
        ctx.RegisterShutdownHandler(Scheduler.Dispose);
        Updates.LongPollTimeout = TimeSpan.FromSeconds(EventsConfig().GetDouble("longPollTimeoutSeconds") ?? 25);
    }

    JsonObject EventsConfig()
    {
        var configured = Ctx.GetConfigDefaults().GetObject("events") ?? new JsonObject();
        return new JsonObject
        {
            ["transport"] = configured.GetString("transport") ?? "auto",
            ["longPollTimeoutSeconds"] = configured.GetDouble("longPollTimeoutSeconds") ?? 25,
            ["sseHeartbeatSeconds"] = configured.GetDouble("sseHeartbeatSeconds") ?? 15,
            ["sseConnectTimeoutSeconds"] = configured.GetDouble("sseConnectTimeoutSeconds") ?? 5,
            ["sseFailureThreshold"] = configured.GetInt("sseFailureThreshold") ?? 3,
            ["sseRetryDelaySeconds"] = configured.GetDouble("sseRetryDelaySeconds") ?? 10,
        };
    }

    JsonObject ThreadListDto(ChatThread row)
    {
        Db.EnsureChatMessages(row.Id);
        var bounds = Db.GetChatMessageBounds(row.Id);
        var dto = row.ToDto(includeMessages: false);
        dto["messages"] = new JsonArray(Db.GetChatMessagePage(row.Id,
            before: (bounds.Last ?? 0) + 1, take: 1).Select(x => (JsonNode)x).ToArray());
        dto["messageCount"] = bounds.Count;
        dto["sig"] = ChatSignature.Compute(bounds.Count, bounds.Last, row.StreamingMessage,
            row.Status, row.CompletedAt, row.Error);
        AttachRun(dto, row);
        return dto;
    }

    JsonObject ThreadWindowDto(ChatThread row, int head = 20, int tail = 100)
    {
        var bounds = Db.GetChatMessageBounds(row.Id);
        var window = Db.GetChatMessageWindow(row.Id, head, tail);
        var first = bounds.First ?? 1;
        var headRows = window.Where(x => (x.GetLong("_sequence") ?? 0) < first + head).ToList();
        var tailRows = window.Except(headRows).ToList();
        var messages = LimitMessagePayload(headRows, 128 * 1024)
            .Concat(LimitMessagePayload(tailRows, 384 * 1024, fromEnd: true)).ToList();
        var dto = row.ToDto(includeMessages: false);
        dto["messages"] = new JsonArray(messages.Select(x => (JsonNode)x).ToArray());
        if (ChatDtos.ParseJson(row.StreamingMessage) is JsonObject streaming)
        {
            streaming[ChatDtos.StreamingKey] = true;
            dto.GetArray("messages")!.Add(streaming);
        }
        dto["messageCount"] = bounds.Count;
        dto["sig"] = ChatSignature.Compute(bounds.Count, bounds.Last, row.StreamingMessage,
            row.Status, row.CompletedAt, row.Error);
        dto["messageWindow"] = new JsonObject
        {
            ["messageCount"] = bounds.Count,
            ["firstSequence"] = bounds.First,
            ["lastSequence"] = bounds.Last,
            ["ranges"] = MessageRanges(messages),
        };
        AttachRun(dto, row);
        return dto;
    }

    JsonObject FullThreadDto(ChatThread row)
    {
        var dto = row.ToDto(includeMessages: false);
        var messages = Db.GetActiveMessagesAfter(row.Id, 0);
        dto["messages"] = new JsonArray(messages.Select(x => (JsonNode)x).ToArray());
        dto["messageCount"] = messages.Count;
        var last = messages.LastOrDefault()?.GetLong("_sequence");
        dto["sig"] = ChatSignature.Compute(messages.Count, last, row.StreamingMessage,
            row.Status, row.CompletedAt, row.Error);
        AttachRun(dto, row);
        return dto;
    }

    void AttachRun(JsonObject dto, ChatThread row)
    {
        var run = Db.GetActiveAgentRun(row.Id, ChatDb.AllUsers);
        if (run != null)
        {
            dto["run"] = run.ToDto();
            dto["contextTokens"] = run.ContextTokens ?? row.ContextTokens;
            dto["contextLimit"] = run.ContextLimit;
        }
    }

    static List<JsonObject> LimitMessagePayload(IEnumerable<JsonObject> rows, int maxBytes, bool fromEnd = false)
    {
        var groups = new List<List<JsonObject>>();
        foreach (var message in rows)
        {
            if (message.GetString("role") == "tool" && groups.Count > 0 &&
                groups[^1][0].GetArray("tool_calls") is { Count: > 0 })
                groups[^1].Add(message);
            else
                groups.Add([message]);
        }
        var selected = new List<List<JsonObject>>();
        var used = 0;
        foreach (var group in fromEnd ? groups.AsEnumerable().Reverse() : groups)
        {
            // JsonNode has single-parent ownership. Size a cloned projection so the original rows
            // remain available for the response JsonArray constructed by ThreadWindowDto.
            var size = Encoding.UTF8.GetByteCount(new JsonArray(group.Select(x => (JsonNode)x.Clone()).ToArray())
                .ToJsonString(ChatJson.Options));
            if (selected.Count > 0 && used + size > maxBytes) break;
            selected.Add(group); used += size;
        }
        if (fromEnd) selected.Reverse();
        return selected.SelectMany(x => x).ToList();
    }

    static JsonArray MessageRanges(IEnumerable<JsonObject> messages)
    {
        var ranges = new JsonArray();
        long? from = null, to = null;
        foreach (var sequence in messages.Select(x => x.GetLong("_sequence"))
                     .Where(x => x != null).Select(x => x!.Value).Distinct().Order())
        {
            if (from == null || sequence > to + 1)
            {
                if (from != null) ranges.Add(new JsonObject { ["from"] = from, ["to"] = to });
                from = to = sequence;
            }
            else to = sequence;
        }
        if (from != null) ranges.Add(new JsonObject { ["from"] = from, ["to"] = to });
        return ranges;
    }

    Task<object?> GetThreadMessagesAsync(ChatRequestContext req)
    {
        var id = ThreadId(req);
        if (ResolveThread(req, id, out _, includeMessages: false) == null)
            throw HttpError.NotFound("Thread not found");
        var take = Math.Clamp(int.TryParse(req.QueryString("take"), out var parsed) ? parsed : 100, 1, 200);
        var maxBytes = Math.Clamp(int.TryParse(req.QueryString("maxBytes"), out parsed) ? parsed : 512 * 1024,
            64 * 1024, 2 * 1024 * 1024);
        long? before = long.TryParse(req.QueryString("before"), out var b) ? b : null;
        long? after = long.TryParse(req.QueryString("after"), out var a) ? a : 0;
        var rows = Db.GetChatMessagePage(id, before, after, take);
        rows = LimitMessagePayload(rows, maxBytes, before != null);
        var bounds = Db.GetChatMessageBounds(id);
        return Task.FromResult<object?>(new JsonObject
        {
            ["messages"] = new JsonArray(rows.Select(x => (JsonNode)x).ToArray()),
            ["messageCount"] = bounds.Count, ["firstSequence"] = bounds.First,
            ["lastSequence"] = bounds.Last, ["ranges"] = MessageRanges(rows),
        });
    }

    Task<object?> GetThreadUpdatesStreamAsync(ChatRequestContext req)
    {
        var config = EventsConfig();
        if (config.GetString("transport") == "long-poll")
            return Task.FromResult<object?>(ChatResult.NotFound("SSE transport is disabled"));
        var id = ThreadId(req);
        var user = req.UserName;
        if (Db.GetThread(id, user, includeMessages: false) == null) throw HttpError.NotFound("Thread not found");
        var clientSig = req.QueryString("sig") ?? "";
        return Task.FromResult<object?>(new ChatStreamResult(async response =>
        {
            try
            {
                response.StatusCode = 200;
                response.ContentType = "text/event-stream";
                response.AddHeader("Cache-Control", "no-cache, no-transform");
                response.AddHeader("X-Accel-Buffering", "no");
                var retry = (int)((config.GetDouble("sseRetryDelaySeconds") ?? 10) * 1000);
                await WriteSseAsync(response, null, $"retry: {retry}\n\n").ConfigAwait();
                var current = Db.GetThread(id, user, includeMessages: false);
                if (current == null) return;
                var currentDto = ThreadWindowDto(current);
                await WriteSseAsync(response, "connected", currentDto.ToJsonString(ChatJson.Options)).ConfigAwait();
                var sig = clientSig.Length == 0 ? currentDto.GetString("sig")! : clientSig;
                var heartbeat = TimeSpan.FromSeconds(config.GetDouble("sseHeartbeatSeconds") ?? 15);
                while (!IsTerminal(current))
                {
                    var signal = Updates.NextSignalAsync(id);
                    var completed = await Task.WhenAny(signal, Task.Delay(heartbeat)).ConfigAwait();
                    if (completed != signal)
                    {
                        await WriteSseAsync(response, "heartbeat", "{}").ConfigAwait();
                        continue;
                    }
                    current = Db.GetThread(id, user, includeMessages: false);
                    if (current == null) break;
                    currentDto = ThreadWindowDto(current);
                    var currentSig = currentDto.GetString("sig")!;
                    if (currentSig == sig && !IsTerminal(current)) continue;
                    sig = currentSig;
                    await WriteSseAsync(response, "thread", currentDto.ToJsonString(ChatJson.Options)).ConfigAwait();
                }
            }
            catch (IOException) { /* browser/proxy disconnected */ }
            catch (ObjectDisposedException) { /* response was closed */ }
            catch (OperationCanceledException) { /* host is stopping */ }
        }));
    }

    static async Task WriteSseAsync(IResponse response, string? eventName, string data)
    {
        var payload = eventName == null ? data : $"event: {eventName}\ndata: {data}\n\n";
        var bytes = Encoding.UTF8.GetBytes(payload);
        await response.OutputStream.WriteAsync(bytes).ConfigAwait();
        await response.OutputStream.FlushAsync().ConfigAwait();
    }

    async Task ExecuteAgentSliceAsync(AgentRun claimed, IRequest? request, CancellationToken token)
    {
        var run = Db.GetAgentRun(claimed.Id, ChatDb.AllUsers);
        if (run == null) return;
        if (run.StepCount >= run.MaxSteps)
            throw new Exception($"Agent run reached its maximum step budget ({run.MaxSteps})");

        var row = Db.GetThread(run.ThreadId, run.User, includeMessages: false);
        if (row == null) throw new Exception("Thread not found");
        if (row.CompletedAt != null)
        {
            run.Status = row.Error == null ? AgentRunStatus.Completed : AgentRunStatus.Failed;
            run.Error = row.Error; run.CompletedAt = row.CompletedAt;
            run.LeaseOwner = null; run.LeaseExpiresAt = null; Db.UpdateAgentRun(run); return;
        }

        var messages = await ContextForRunAsync(row, run, token).ConfigAwait();
        var thread = row.ToDto();
        var chat = new JsonObject
        {
            ["model"] = row.Model,
            ["messages"] = messages,
            ["modalities"] = thread.GetArray("modalities")?.Clone(),
            ["tools"] = thread.GetArray("tools")?.Clone() ?? new JsonArray(),
            ["metadata"] = thread.GetObject("metadata")?.Clone() ?? new JsonObject(),
        };
        foreach (var entry in thread.GetObject("args") ?? [])
            if (ChatFeature.RequestArgs.Contains(entry.Key)) chat[entry.Key] = entry.Value?.DeepClone();

        var sequence = run.StepCount + 1;
        var stepId = Db.CreateAgentStep(run.Id, sequence,
            new JsonObject { ["messageCount"] = messages.Count });
        run.StepCount = sequence; run.SliceCount++;
        run.NextAction = "model"; Db.UpdateAgentRun(run);

        var context = new ChatContext
        {
            Chat = chat, User = run.User, Request = request ?? new Host.BasicRequest(),
            ThreadId = run.ThreadId, RunId = run.Id, StepId = stepId,
            Tools = chat.GetObject("metadata").GetString("tools") ?? "all",
            ProjectedContext = true, CancellationToken = token,
        };
        context.SeedMessageTimestamps(messages);
        try
        {
            var response = await Ctx.ChatCompletionAsync(chat, context).ConfigAwait();
            var step = Db.GetAgentStep(stepId)!;
            step.Status = AgentRunStatus.Completed;
            step.Output = new JsonObject { ["responseId"] = response.GetString("id") }.ToJsonString(ChatJson.Options);
            step.CompletedAt = DateTime.Now; Db.UpdateAgentStep(step);
            run = Db.GetAgentRun(run.Id, ChatDb.AllUsers)!;
            if (run.Status == AgentRunStatus.WaitingApproval)
            {
                step.Output = new JsonObject { ["requiresApproval"] = true, ["responseId"] = response.GetString("id") }
                    .ToJsonString(ChatJson.Options);
                Db.UpdateAgentStep(step);
                return;
            }
            run.Status = AgentRunStatus.Completed; run.NextAction = null; run.CompletedAt = DateTime.Now;
            run.LeaseOwner = null; run.LeaseExpiresAt = null; Db.UpdateAgentRun(run);
        }
        catch (AgentSliceYieldException yielded)
        {
            var step = Db.GetAgentStep(stepId)!;
            step.Status = AgentRunStatus.Completed;
            step.Output = new JsonObject { ["yielded"] = true, ["iterations"] = yielded.Iterations }
                .ToJsonString(ChatJson.Options);
            step.CompletedAt = DateTime.Now; Db.UpdateAgentStep(step);
            run = Db.GetAgentRun(run.Id, ChatDb.AllUsers)!;
            run.Status = AgentRunStatus.Queued; run.NextAction = "model";
            run.LeaseOwner = null; run.LeaseExpiresAt = null; Db.UpdateAgentRun(run);
            await threadApi.UpdateThreadAsync(run.ThreadId,
                new JsonObject { ["status"] = "Continuing…", ["streamingMessage"] = null }, run.User).ConfigAwait();
        }
        catch (Exception e)
        {
            var step = Db.GetAgentStep(stepId)!;
            step.Status = AgentRunStatus.Failed; step.Error = ChatJson.ToErrorMessage(e);
            step.CompletedAt = DateTime.Now; Db.UpdateAgentStep(step);
            throw;
        }
    }

    async Task<JsonArray> ContextForRunAsync(ChatThread thread, AgentRun run, CancellationToken token)
    {
        var snapshot = Db.GetLatestContextSnapshot(thread.Id);
        var messages = new JsonArray();
        var after = 0L;
        if (snapshot != null)
        {
            if (ChatDtos.ParseJson(snapshot.Summary) is JsonArray summary)
                foreach (var message in summary) messages.Add(message?.DeepClone());
            after = snapshot.ToSequence;
        }
        foreach (var message in Db.GetActiveMessagesAfter(thread.Id, after)) messages.Add(message);

        var contextTokens = DurableAgentUtils.CountTokensApprox(messages);
        var modelInfo = Ctx.Feature.Providers.Values.Select(x => x.ModelInfo(thread.Model ?? ""))
            .FirstOrDefault(x => x != null);
        var contextLimit = modelInfo.GetObject("limit").GetLong("context");
        run.ContextTokens = contextTokens; run.ContextLimit = contextLimit;
        Db.UpdateAgentRun(run);
        Db.UpdateThreadContextTokens(thread.Id, contextTokens, run.User);

        var metadata = ChatDtos.ParseJson(thread.Metadata) as JsonObject ?? new JsonObject();
        var threshold = metadata.GetLong("compactThreshold")
            ?? (contextLimit is > 0 ? (long)(contextLimit.Value * .8) : 80_000);
        if (contextTokens >= threshold && messages.Count > 16)
        {
            messages = await CompactContextAsync(thread, run, messages, token).ConfigAwait();
            contextTokens = DurableAgentUtils.CountTokensApprox(messages);
            run.ContextTokens = contextTokens; Db.UpdateAgentRun(run);
        }
        await threadApi.UpdateThreadAsync(thread.Id,
            new JsonObject { ["contextTokens"] = contextTokens, ["status"] = $"Continuing · {contextTokens:N0} context tokens" },
            run.User).ConfigAwait();
        return messages;
    }
}
