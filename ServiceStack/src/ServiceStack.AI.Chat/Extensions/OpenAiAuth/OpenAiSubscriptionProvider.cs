using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>Request-local OAuth credentials. The base API provider and shared headers are never mutated.</summary>
public sealed class OpenAiSubscriptionProvider : OpenAiCompatibleProvider
{
    readonly OpenAiSubscriptionOptions options;
    readonly OpenAiSubscriptionFlow flows;
    readonly OpenAiSubscriptionStore credentials;
    readonly OpenAiSubscriptionHttp http;
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, JsonObject> discovered = new(StringComparer.OrdinalIgnoreCase);
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, JsonArray> accountModels = new(StringComparer.Ordinal);
    public void Discover(JsonArray models, string user)
    {
        accountModels[user] = models.Clone();
        foreach (var model in models.OfType<JsonObject>()) discovered[model.GetString("id")!] = model.Clone();
    }
    static bool Matches(JsonObject row, string model)
    {
        var value = model.StartsWith("openai/", StringComparison.OrdinalIgnoreCase) ? model[7..] : model;
        return string.Equals(row.GetString("id"), value, StringComparison.OrdinalIgnoreCase) || string.Equals(row.GetString("name"), value, StringComparison.OrdinalIgnoreCase);
    }
    // Provider qualification has no request identity. Resolve the actual outbound ID against the user's catalog below.
    public override string? ProviderModel(string model) => discovered.Values.FirstOrDefault(row => Matches(row, model)).GetString("id") ?? base.ProviderModel(model);
    public override JsonObject? ModelInfo(string model) => discovered.GetValueOrDefault(ProviderModel(model) ?? model)?.Clone() ?? base.ModelInfo(model);
    public ChatProvider? BaseProvider { get; }
    public OpenAiSubscriptionProvider(ExtensionContext ctx, OpenAiSubscriptionOptions options, OpenAiSubscriptionFlow flows, OpenAiSubscriptionStore credentials, ChatProvider? baseProvider)
    {
        this.options = options; this.flows = flows; this.credentials = credentials; http = new(options); BaseProvider = baseProvider;
        var definition = ctx.Feature.ProviderModels.GetObject("openai")?.Clone() ?? new();
        foreach (var pair in ctx.Feature.Config.GetObject("providers").GetObject("openai") ?? new()) definition[pair.Key] = pair.Value?.DeepClone();
        definition["id"] = "openai"; definition["api"] ??= "https://api.openai.com/v1"; definition["api_key"] = null;
        Populate(definition); Feature = ctx.Feature; Log = ctx.Log; HttpClientFactory = ctx.Feature.HttpClientFactory;
        Headers = new(); // never put user credentials on a shared provider object
        if (baseProvider != null)
        {
            foreach (var pair in baseProvider.Models) Models[pair.Key] = pair.Value.Clone();
            MapModels = new(baseProvider.MapModels); Modalities = new(baseProvider.Modalities);
            ReasoningEffort = baseProvider.ReasoningEffort;
        }
        foreach (var model in options.Models)
            if (!Models.ContainsKey(model)) Models[model] = new JsonObject { ["id"] = model, ["name"] = "ChatGPT " + model.ToUpperInvariant(), ["cost"] = new JsonObject { ["input"] = 0d, ["output"] = 0d } };
        foreach (var pair in Models) pair.Value["id"] ??= pair.Key;
    }
    public override string? Validate() => null;
    public string ResolveSubscriptionModel(string? model, string? user = null)
    {
        if (user != null && model != null && accountModels.TryGetValue(user, out var rows)
            && rows.OfType<JsonObject>().FirstOrDefault(row => Matches(row, model)) is { } owned) return owned.GetString("id")!;
        string? Match(string? id) => id == null ? null : options.Models.FirstOrDefault(x => string.Equals(x, id.StartsWith("openai/", StringComparison.OrdinalIgnoreCase) ? id[7..] : id, StringComparison.OrdinalIgnoreCase));
        return Match(model) ?? (model != null && discovered.ContainsKey(ProviderModel(model) ?? model) ? ProviderModel(model)! : model != null ? Match(ProviderModel(model)) : null) ?? options.DefaultModel;
    }
    public static JsonArray ConvertMessages(JsonArray messages)
    {
        var items = new JsonArray();
        foreach (var msg in messages.OfType<JsonObject>())
        {
            var role = msg.GetString("role"); var content = msg["content"];
            if (role is "system" or "developer") items.Add(new JsonObject { ["role"] = "system", ["content"] = content?.DeepClone() ?? JsonValue.Create("") });
            else if (role == "user")
            {
                if (content is not JsonArray parts) { items.Add(new JsonObject { ["role"] = "user", ["content"] = content?.DeepClone() ?? JsonValue.Create("") }); continue; }
                var mapped = new JsonArray();
                foreach (var part in parts)
                {
                    if (part is JsonValue text && text.TryGetValue<string>(out var value)) { mapped.Add(new JsonObject { ["type"] = "input_text", ["text"] = value }); continue; }
                    if (part is not JsonObject obj) continue;
                    var type = obj.GetString("type");
                    if (type == "text") mapped.Add(new JsonObject { ["type"] = "input_text", ["text"] = obj.GetString("text") ?? "" });
                    else if (type == "image_url") mapped.Add(new JsonObject { ["type"] = "input_image", ["image_url"] = obj["image_url"] is JsonObject image ? image["url"]?.DeepClone() : obj["image_url"]?.DeepClone() });
                    else if (type is "input_text" or "input_image" or "input_file" or "input_audio") mapped.Add(obj.Clone());
                    else mapped.Add(new JsonObject { ["type"] = "input_text", ["text"] = obj.GetString("text") ?? obj.ToJsonString(ChatJson.Options) });
                }
                items.Add(new JsonObject { ["role"] = "user", ["content"] = mapped });
            }
            else if (role == "assistant")
            {
                if (content != null && content.ToJsonString() != "\"\"") items.Add(new JsonObject { ["role"] = "assistant", ["content"] = content.DeepClone() });
                foreach (var call in msg.GetArray("tool_calls")?.OfType<JsonObject>() ?? [])
                {
                    var fn = call.GetObject("function"); items.Add(new JsonObject { ["type"] = "function_call", ["call_id"] = call.GetString("id") ?? call.GetString("call_id") ?? "",
                        ["name"] = fn.GetString("name") ?? call.GetString("name") ?? "", ["arguments"] = fn.GetString("arguments") ?? call.GetString("arguments") ?? "{}" });
                }
            }
            else if (role is "tool" or "function") items.Add(new JsonObject { ["type"] = "function_call_output", ["call_id"] = msg.GetString("tool_call_id") ?? msg.GetString("call_id") ?? msg.GetString("name") ?? "",
                ["output"] = msg.GetString("content") ?? content?.ToJsonString(ChatJson.Options) ?? "" });
        }
        return items;
    }
    public static JsonArray ConvertTools(JsonArray tools)
    {
        var output = new JsonArray();
        foreach (var tool in tools.OfType<JsonObject>())
        {
            if (tool.GetString("type") != "function") { output.Add(tool.Clone()); continue; }
            var fn = tool.GetObject("function") ?? tool; var converted = new JsonObject { ["type"] = "function", ["name"] = fn.GetString("name"), ["description"] = fn.GetString("description") ?? "", ["parameters"] = fn["parameters"]?.DeepClone() ?? new JsonObject() };
            if (fn.ContainsKey("strict")) converted["strict"] = fn["strict"]?.DeepClone(); output.Add(converted);
        }
        return output;
    }
    public JsonObject Payload(JsonObject chat, string? user = null)
    {
        var payload = new JsonObject { ["model"] = ResolveSubscriptionModel(chat.GetString("model"), user), ["input"] = ConvertMessages(chat.GetArray("messages") ?? []), ["stream"] = true, ["store"] = false };
        var tools = ConvertTools(chat.GetArray("tools") ?? []); if (tools.Count > 0) payload["tools"] = tools;
        if (chat["tool_choice"] is { } choice) payload["tool_choice"] = choice is JsonObject obj && obj.GetObject("function") is { } fn ? new JsonObject { ["type"] = "function", ["name"] = fn.GetString("name") ?? "" } : choice.DeepClone();
        if ((chat.GetString("reasoning_effort") ?? ReasoningEffort) is { } effort && effort is "low" or "medium" or "high") payload["reasoning"] = new JsonObject { ["effort"] = effort };
        if (chat["instructions"] != null) payload["instructions"] = chat["instructions"]!.DeepClone();
        return payload;
    }
    public override async Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context)
    {
        var user = context.User ?? "default";
        if (credentials.Load(user) == null)
        {
            if (!string.IsNullOrEmpty(BaseProvider?.ApiKey)) return await BaseProvider.ChatAsync(chat.Clone(), context).ConfigureAwait(false);
            throw HttpError.Unauthorized("ChatGPT subscription is not connected. Sign in in Settings or configure the OpenAI API key.");
        }
        if (chat.GetArray("modalities")?.Any(m => m?.GetValue<string>() != "text") == true)
        {
            if (!string.IsNullOrEmpty(BaseProvider?.ApiKey)) return await BaseProvider.ChatAsync(chat.Clone(), context).ConfigureAwait(false);
            throw HttpError.BadRequest("This ChatGPT subscription provider supports text responses. Configure an API-key provider for other modalities.");
        }
        try { return await SubscriptionChatAsync(chat, context, user).ConfigureAwait(false); }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested) { throw; }
        catch (Exception error) { throw new ChatProviderRequestException(error); }
    }
    async Task<JsonObject> SubscriptionChatAsync(JsonObject chat, ChatContext context, string user)
    {
        var grant = await flows.ValidCredentialsAsync(user, context.CancellationToken).ConfigureAwait(false)
            ?? throw HttpError.Unauthorized("The ChatGPT subscription was disconnected.");
        var payload = Payload(chat, user); var started = options.Clock.GetUtcNow();
        using var client = http.Client();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            using var request = new HttpRequestMessage(HttpMethod.Post, OpenAiSubscriptionHttp.Endpoint(options.ResponsesUrl)) { Content = new StringContent(payload.ToJsonString(ChatJson.Options), Encoding.UTF8, "application/json") };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", grant.GetString("access_token"));
            request.Headers.UserAgent.ParseAdd("ServiceStack.AI.Chat/1.0"); request.Headers.Accept.ParseAdd("text/event-stream");
            if (!string.IsNullOrEmpty(grant.GetString("account_id"))) request.Headers.TryAddWithoutValidation("chatgpt-account-id", grant.GetString("account_id"));
            using var response = await SendStreamingAsync(client, request, context).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
            {
                grant = await flows.ValidCredentialsAsync(user, context.CancellationToken, grant.GetString("access_token")).ConfigureAwait(false)
                    ?? throw HttpError.Unauthorized("The ChatGPT subscription was disconnected."); continue;
            }
            if (!response.IsSuccessStatusCode) throw new HttpError((int)response.StatusCode, "SubscriptionError", $"ChatGPT subscription request failed (HTTP {(int)response.StatusCode}). Check your account or sign in again.");
            if (response.RequestMessage?.RequestUri is { } final && final != request.RequestUri) throw new HttpError(502, "SubscriptionRedirect", "Subscription redirects are not supported.");
            return await ReadResponsesAsync(response, chat, context, started, grant).ConfigureAwait(false);
        }
        throw HttpError.Unauthorized("ChatGPT subscription credentials were rejected after refresh. Sign in again.");
    }
    public async Task<JsonObject> ReadResponsesAsync(HttpResponseMessage response, JsonObject chat, ChatContext context, DateTimeOffset started, JsonObject? originatingGrant = null)
    {
        var content = new StringBuilder(); var reasoning = new StringBuilder(); var calls = new SortedDictionary<int, JsonObject>();
        string? id = null, model = null; long? created = null; JsonObject usage = new(); var complete = false;
        var writer = context.NoStore ? new StreamCheckpointWriter(null, null) : CreateStreamWriter(context);
        JsonObject Message(bool partial)
        {
            var msg = new JsonObject { ["role"] = "assistant", ["content"] = content.Length > 0 || calls.Count == 0 ? JsonValue.Create(content.ToString()) : null };
            if (partial) { msg["model"] = model ?? chat.GetString("model"); msg["timestamp"] = started.ToUnixTimeMilliseconds(); }
            if (reasoning.Length > 0) msg["reasoning_content"] = reasoning.ToString();
            if (calls.Count > 0) msg["tool_calls"] = new JsonArray(calls.Values.Select(c => (JsonNode)c.Clone()).ToArray()); return msg;
        }
        async Task Event(string data)
        {
            if (data.Length == 0 || data == "[DONE]") return;
            var evt = ChatJson.TryParseObject(data) ?? throw new HttpError(502, "SubscriptionStream", "ChatGPT returned an unreadable stream event.");
            var type = evt.GetString("type"); var detail = evt.GetObject("response");
            if (type == "response.created") { id = detail.GetString("id"); model = detail.GetString("model"); created = detail.GetLong("created_at"); }
            else if (type == "response.output_text.delta" || type == "response.refusal.delta") content.Append(evt.GetString("delta"));
            else if (type is "response.reasoning_summary_text.delta" or "response.reasoning_text.delta") reasoning.Append(evt.GetString("delta"));
            else if (type == "response.output_item.added" && evt.GetObject("item") is { } item && item.GetString("type") == "function_call")
                calls[evt.GetInt("output_index") ?? 0] = new JsonObject { ["id"] = item.GetString("call_id") ?? "", ["type"] = "function", ["function"] = new JsonObject { ["name"] = item.GetString("name") ?? "", ["arguments"] = item.GetString("arguments") ?? "" } };
            else if (type is "response.function_call_arguments.delta" or "response.function_call_arguments.done")
            {
                if (calls.TryGetValue(evt.GetInt("output_index") ?? 0, out var call))
                {
                    var fn = call.GetObject("function")!; fn["arguments"] = type.EndsWith(".done") ? evt.GetString("arguments") ?? fn.GetString("arguments") : fn.GetString("arguments") + evt.GetString("delta");
                }
            }
            else if (type == "response.completed")
            {
                if (detail.GetString("status") is { } status && status != "completed") throw new HttpError(502, "SubscriptionIncomplete", "ChatGPT did not complete the response.");
                complete = true; usage = detail.GetObject("usage")?.Clone() ?? new(); id ??= detail.GetString("id"); model ??= detail.GetString("model");
            }
            else if (type is "response.failed" or "response.incomplete" or "error")
            {
                var code = detail.GetObject("error").GetString("code") ?? evt.GetString("code");
                var message = code is "subscription_sharing_usage_limit_exceeded" or "subscription_sharing_usage_unavailable" ? "ChatGPT plan usage is unavailable or its limit has been reached. Review your ChatGPT account settings." : "ChatGPT did not complete the response. Review your account or try again.";
                throw new HttpError(502, "SubscriptionIncomplete", message);
            }
            context.CancellationToken.ThrowIfCancellationRequested(); if (Feature?.ShouldCancelThread(context) == true) throw new OperationCanceledException("The chat was cancelled.", context.CancellationToken);
            if (originatingGrant != null)
            {
                var current = credentials.Load(context.User ?? "default");
                if (current == null || current.GetString("subject") != originatingGrant.GetString("subject") || current.GetString("client_id") != originatingGrant.GetString("client_id")) throw HttpError.Unauthorized("The ChatGPT subscription changed during the response.");
            }
            await writer.WriteAsync(Message(true)).ConfigureAwait(false);
        }
        await using var source = await response.Content.ReadAsStreamAsync(context.CancellationToken).ConfigureAwait(false);
        await using var bounded = new BoundedStream(source, 32 * 1024 * 1024);
        await using var sse = new SseReader(bounded, context.CancellationToken, StreamReadTimeout, Name);
        var frame = new StringBuilder();
        try
        {
            while (await sse.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                if (line.Length == 0) { await Event(frame.ToString()).ConfigureAwait(false); frame.Clear(); if (complete) break; continue; }
                if (line.StartsWith("data:", StringComparison.Ordinal)) { if (frame.Length > 0) frame.Append('\n'); frame.Append(line[5..].TrimStart()); }
                if (frame.Length > 2 * 1024 * 1024) throw new HttpError(502, "SubscriptionStream", "ChatGPT stream event exceeds the size limit.");
            }
            if (!complete && frame.Length > 0) await Event(frame.ToString()).ConfigureAwait(false);
            if (!complete) throw new HttpError(502, "SubscriptionInterrupted", "ChatGPT stream ended before completion. The request has not been retried.");
            await writer.WriteAsync(Message(true), final: true).ConfigureAwait(false);
        }
        catch { await writer.FlushAsync().ConfigureAwait(false); throw; }
        var input = usage.GetLong("input_tokens") ?? 0; var output = usage.GetLong("output_tokens") ?? 0;
        var result = new JsonObject { ["id"] = id ?? "resp_" + Guid.NewGuid().ToString("N"), ["object"] = "chat.completion", ["created"] = created ?? started.ToUnixTimeSeconds(), ["model"] = model ?? ResolveSubscriptionModel(chat.GetString("model")),
            ["choices"] = new JsonArray(new JsonObject { ["index"] = 0, ["message"] = Message(false), ["finish_reason"] = calls.Count > 0 ? "tool_calls" : "stop" }),
            ["usage"] = new JsonObject { ["prompt_tokens"] = input, ["completion_tokens"] = output, ["total_tokens"] = usage.GetLong("total_tokens") ?? input + output,
                ["prompt_tokens_details"] = new JsonObject { ["cached_tokens"] = usage.GetObject("input_tokens_details").GetLong("cached_tokens") ?? 0 },
                ["completion_tokens_details"] = new JsonObject { ["reasoning_tokens"] = usage.GetObject("output_tokens_details").GetLong("reasoning_tokens") ?? 0 } }, ["cost"] = 0d };
        ToResponse(result, chat, started, context); result.GetObject("metadata")!["pricing"] = "0/0"; return result;
    }
    sealed class BoundedStream(Stream inner, long maximum) : Stream
    {
        long read;
        int Count(int count) { read += count; if (read > maximum) throw new HttpError(502, "SubscriptionStream", "ChatGPT response exceeds the size limit."); return count; }
        public override int Read(byte[] buffer, int offset, int count) => Count(inner.Read(buffer, offset, count));
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default) => Count(await inner.ReadAsync(buffer, token).ConfigureAwait(false));
        public override bool CanRead => true; public override bool CanSeek => false; public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException(); public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException(); public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException(); public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
