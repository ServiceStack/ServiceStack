using System.Text.Json.Nodes;
using ServiceStack.Text;

namespace ServiceStack.AI;

public partial class AppExtension
{
    async Task<JsonArray> CompactContextAsync(ChatThread thread, AgentRun run, JsonArray current,
        CancellationToken token)
    {
        var metadata = ChatDtos.ParseJson(thread.Metadata) as JsonObject ?? new JsonObject();
        var recentCount = Math.Max(4, metadata.GetInt("compactRecentMessages") ?? 12);
        var rows = Db.GetActiveMessagesAfter(thread.Id, 0);
        var snapshot = Db.GetLatestContextSnapshot(thread.Id);
        var after = snapshot?.ToSequence ?? 0;
        var tail = rows.Where(x => (x.GetLong("_sequence") ?? 0) > after).ToList();
        if (tail.Count <= recentCount) return current;

        var cutoff = Math.Max(1, tail.Count - recentCount);
        while (cutoff > 0 && tail[cutoff].GetString("role") == "tool") cutoff--;
        if (cutoff > 0 && tail[cutoff - 1].GetArray("tool_calls") is { Count: > 0 }) cutoff--;
        if (cutoff <= 0) return current;

        var source = new JsonArray();
        if (snapshot != null && ChatDtos.ParseJson(snapshot.Summary) is JsonArray prior)
            foreach (var message in prior) source.Add(message?.DeepClone());
        foreach (var message in tail.Take(cutoff))
        {
            var clone = message.Clone(); clone.Remove("_sequence"); source.Add(clone);
        }
        var recent = tail.Skip(cutoff).Select(x => { var clone = x.Clone(); clone.Remove("_sequence"); return clone; }).ToList();
        foreach (var message in recent) source.Add(message);

        var workingTokens = DurableAgentUtils.CountTokensApprox(current);
        var threshold = metadata.GetLong("compactThreshold")
            ?? (run.ContextLimit is > 0 ? Math.Max(8_000, (long)(run.ContextLimit.Value * .8)) : 80_000);
        var target = Math.Max(2_000, threshold / 4);
        var compacted = await CompactMessagesAsync(source, target,
            Math.Max(8_000, metadata.GetLong("compactChunkTokens") ?? 60_000), run.User,
            recent.Count, async (part, total) =>
            {
                await threadApi.UpdateThreadAsync(thread.Id, new JsonObject
                {
                    ["status"] = $"Reducing context · {workingTokens:N0} tokens · part {part}/{total}",
                }, run.User).ConfigAwait();
            }, token).ConfigAwait();

        var summaryCount = Math.Max(1, compacted.Count - recent.Count);
        var summary = new JsonArray(compacted.Take(summaryCount).Select(x => x?.DeepClone()).ToArray());
        var fromSequence = snapshot?.FromSequence ?? (tail[0].GetLong("_sequence") ?? 1);
        var toSequence = tail[cutoff - 1].GetLong("_sequence") ?? after;
        Db.CreateContextSnapshot(thread.Id, run.Id, fromSequence, toSequence, summary,
            Ctx.GetConfigDefaults().GetObject("compact").GetString("model"));
        return compacted;
    }

    internal async Task<JsonArray> CompactMessagesAsync(JsonArray messages, long targetTokens,
        long chunkTokens, string? user, int recentCount,
        Func<int, int, Task>? progress, CancellationToken token)
    {
        var template = Ctx.GetConfigDefaults().GetObject("compact")
            ?? throw new Exception("'compact' template not found in llms.json defaults");
        recentCount = Math.Clamp(recentCount, 0, messages.Count);

        var protectedMessages = new List<JsonObject>();
        var index = 0;
        while (index < messages.Count && messages[index] is JsonObject leading &&
               leading.GetString("role") is "system" or "developer" && !leading.GetBool("_compaction"))
        {
            protectedMessages.Add(BoundMessage(leading, 100_000)); index++;
        }
        var history = messages.Skip(index).OfType<JsonObject>().Select(x => BoundMessage(x, 200_000)).ToList();
        var recentStart = Math.Max(0, history.Count - recentCount);
        while (recentStart > 0 && history[recentStart].GetString("role") == "tool") recentStart--;
        if (recentStart > 0 && history[recentStart - 1].GetArray("tool_calls") is { Count: > 0 }) recentStart--;
        var source = history.Take(recentStart).ToList();
        var recent = history.Skip(recentStart).ToList();
        var fixedTokens = DurableAgentUtils.CountTokensApprox(new JsonArray(
            protectedMessages.Concat(recent).Select(x => (JsonNode)x).ToArray()));
        var summaryTarget = Math.Max(1_000, targetTokens - fixedTokens);

        var summary = source;
        var summarized = false;
        for (var pass = 0; pass < 4 && Tokens(summary) > summaryTarget; pass++)
        {
            var batches = PartitionGroups(summary, chunkTokens);
            var reduced = new List<JsonObject>();
            var before = Tokens(summary);
            for (var part = 0; part < batches.Count; part++)
            {
                token.ThrowIfCancellationRequested();
                if (progress != null) await progress(part + 1, batches.Count).ConfigAwait();
                var perBatch = Math.Max(1_000, summaryTarget / Math.Max(1, batches.Count));
                reduced.AddRange(await SummarizeBatchAsync(template, batches[part], perBatch, user, token).ConfigAwait());
            }
            summarized = true;
            if (Tokens(reduced) >= before)
                throw new Exception("Compaction model did not reduce the context");
            summary = reduced;
        }
        if (Tokens(summary) > Math.Max(summaryTarget * 5 / 4, summaryTarget + 1_000))
            throw new Exception("Compaction result exceeded its context budget");

        if (summarized && summary.Count > 0)
        {
            var content = string.Join("\n\n", summary.Select(x => x.GetString("content")).Where(x => !x.IsNullOrEmpty()));
            summary = [new JsonObject
            {
                ["role"] = "system", ["content"] = content, ["_compaction"] = true,
            }];
        }
        return new JsonArray(protectedMessages.Concat(summary).Concat(recent)
            .Select(x => (JsonNode)x.Clone()).ToArray());
    }

    async Task<List<JsonObject>> SummarizeBatchAsync(JsonObject template, List<JsonObject> batch,
        long target, string? user, CancellationToken token)
    {
        var chat = template.Clone();
        if (chat.GetString("model") is not { } compactModel ||
            !Ctx.Feature.Providers.Values.Any(x => x.ProviderModel(compactModel) != null))
            chat["model"] = Ctx.Feature.Providers.Values.SelectMany(x => x.Models.Keys).FirstOrDefault()
                ?? throw new Exception("No model is available for compaction");
        SubstituteVars(chat, new Dictionary<string, string>
        {
            ["{message_count}"] = batch.Count.ToString(),
            ["{token_count}"] = Tokens(batch).ToString(),
            ["{target_tokens}"] = target.ToString(),
            ["{messages_json}"] = new JsonArray(batch.Select(x => (JsonNode)x.Clone()).ToArray())
                .ToJsonString(ChatJson.Options),
        });
        var context = new ChatContext
        {
            Chat = chat, User = user, Tools = "none", NoHistory = true, NoStore = true,
            CancellationToken = token,
        };
        var response = await Ctx.ChatCompletionAsync(chat, context).ConfigAwait();
        var content = response.GetArray("choices")?.FirstOrDefault() is JsonObject choice
            ? choice.GetObject("message").GetString("content") : null;
        var parsed = ChatJson.TryParseObject(content);
        if (parsed == null && content != null)
        {
            var start = content.IndexOf('{'); var end = content.LastIndexOf('}');
            if (start >= 0 && end > start) parsed = ChatJson.TryParseObject(content[start..(end + 1)]);
        }
        var result = parsed.GetArray("messages")?.OfType<JsonObject>().ToList();
        if (result is not { Count: > 0 } || result.Any(x =>
                x.GetString("role") is not ("system" or "user" or "assistant") || x.GetString("content") == null))
            throw new Exception("Invalid compaction response: expected non-empty text messages");
        return result.Select(x => new JsonObject
        {
            ["role"] = x.GetString("role"), ["content"] = x.GetString("content"),
        }).ToList();
    }

    static List<List<JsonObject>> PartitionGroups(List<JsonObject> messages, long tokenLimit)
    {
        var groups = new List<List<JsonObject>>();
        foreach (var message in messages)
        {
            if (message.GetString("role") == "tool" && groups.Count > 0 &&
                groups[^1][0].GetArray("tool_calls") is { Count: > 0 }) groups[^1].Add(message);
            else groups.Add([message]);
        }
        var batches = new List<List<JsonObject>>();
        var batch = new List<JsonObject>(); var tokens = 0L;
        foreach (var group in groups)
        {
            var size = Tokens(group);
            if (batch.Count > 0 && tokens + size > tokenLimit)
            {
                batches.Add(batch); batch = []; tokens = 0;
            }
            batch.AddRange(group); tokens += size;
        }
        if (batch.Count > 0) batches.Add(batch);
        return batches;
    }

    static long Tokens(IEnumerable<JsonObject> messages) => DurableAgentUtils.CountTokensApprox(
        new JsonArray(messages.Select(x => (JsonNode)x.Clone()).ToArray()));

    static JsonObject BoundMessage(JsonObject message, int maxChars)
    {
        var clone = message.Clone();
        BoundNode(clone, maxChars);
        return clone;
    }

    static void BoundNode(JsonNode? node, int maxChars)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(x => x.Key).ToList())
            {
                if (obj[key] is JsonValue value && value.TryGetValue<string>(out var text) && text.Length > maxChars)
                {
                    var head = maxChars * 3 / 4; var tail = maxChars - head;
                    obj[key] = text[..head] + $"\n… [{text.Length - maxChars:N0} characters omitted from model context] …\n" + text[^tail..];
                }
                else BoundNode(obj[key], maxChars);
            }
        }
        else if (node is JsonArray array)
            foreach (var child in array) BoundNode(child, maxChars);
    }
}
