using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// HTTP chat + SSE streaming for OpenAI-compatible providers
/// (port of OpenAiCompatible.chat/init_chat/handle_stream_response, main.py:1341-1595).
/// </summary>
public partial class OpenAiCompatibleProvider
{
    public override async Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context)
    {
        var model = chat.GetString("model") ?? throw new ArgumentException("Model not specified");
        chat["model"] = ProviderModel(model) ?? model;

        // delegate non-text modalities to their sub-generators
        if (chat.GetArray("modalities") is { Count: > 0 } modalities)
        {
            foreach (var m in modalities)
            {
                var modality = m?.GetValue<string>();
                if (modality == null || modality == "text")
                    continue;
                if (Modalities.TryGetValue(modality, out var modalityProvider))
                    return await modalityProvider.ChatAsync(chat, context).ConfigAwait();
                throw new Exception($"Provider {Name} does not support '{modality}' modality");
            }
        }

        var isStream = chat.TryGetPropertyValue("stream", out _)
            ? chat.GetBool("stream")
            : Stream;

        InitChat(chat);
        chat = await ProcessChatAsync(chat, Id).ConfigAwait();

        Log.LogInformation("POST {Url} (stream={Stream})", ChatUrl, isStream);

        // remove metadata (conflicts with some providers, e.g. Z.ai); restored after send
        JsonNode? metadata = null;
        if (chat.TryGetPropertyValue("metadata", out metadata))
            chat.Remove("metadata");

        var startedAt = DateTimeOffset.UtcNow;
        using var client = CreateHttpClient(streaming: isStream);

        if (!isStream)
        {
            chat["stream"] = false;
            using var httpRes = await SendChatAsync(client, chat, HttpCompletionOption.ResponseContentRead, context).ConfigAwait();
            if (metadata != null)
                chat["metadata"] = metadata;
            var response = await ReadJsonResponseAsync(httpRes).ConfigAwait();
            return ToResponse(response, chat, startedAt, context);
        }

        chat["stream"] = true;
        chat["stream_options"] ??= new JsonObject { ["include_usage"] = true };

        using var streamRes = await SendChatAsync(client, chat, HttpCompletionOption.ResponseHeadersRead, context).ConfigAwait();
        if (metadata != null)
            chat["metadata"] = metadata;
        return await HandleStreamResponseAsync(streamRes, chat, startedAt, context).ConfigAwait()
            ?? new JsonObject(); // cancelled
    }

    /// <summary>
    /// HttpClient.Timeout is a total timeout that also covers reading the response body, which kills
    /// long streamed responses mid-stream; streaming requests opt out of it and cap the time between
    /// chunks in <see cref="SseReader"/> instead.
    /// </summary>
    protected HttpClient CreateHttpClient(bool streaming = false)
    {
        var client = HttpClientFactory?.CreateClient() ?? new HttpClient();
        var timeout = Feature?.Limits.ClientTimeout ?? 120;
        client.Timeout = streaming ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(timeout);
        return client;
    }

    protected async Task<HttpResponseMessage> SendChatAsync(HttpClient client, JsonObject chat,
        HttpCompletionOption completionOption, ChatContext context)
    {
        var httpReq = new HttpRequestMessage(HttpMethod.Post, ChatUrl);
        foreach (var entry in Headers)
        {
            if (entry.Key.EqualsIgnoreCase("Content-Type"))
                continue;
            httpReq.Headers.TryAddWithoutValidation(entry.Key, entry.Value);
        }
        httpReq.Content = new StringContent(chat.ToJsonString(ChatJson.Options), Encoding.UTF8, MimeTypes.Json);
        // ResponseHeadersRead is only used for streaming, which bounds the headers phase separately
        return completionOption == HttpCompletionOption.ResponseHeadersRead
            ? await SendStreamingAsync(client, httpReq, context).ConfigAwait()
            : await client.SendAsync(httpReq, completionOption, context.CancellationToken).ConfigAwait();
    }

    /// <summary>Port of response_json/http_error_to_message: surface provider error messages</summary>
    public static async Task<JsonObject> ReadJsonResponseAsync(HttpResponseMessage httpRes)
    {
        var text = await httpRes.Content.ReadAsStringAsync().ConfigAwait();
        if ((int)httpRes.StatusCode >= 400)
        {
            throw new Exception(HttpErrorToMessage(httpRes, text));
        }
        return ChatJson.TryParseObject(text)
            ?? throw new Exception($"Invalid JSON response: {text.SafeSubstring(0, 200)}");
    }

    public static string HttpErrorToMessage(HttpResponseMessage response, string text)
    {
        var obj = ChatJson.TryParseObject(text);
        if (obj != null)
        {
            var error = obj.GetObject("error");
            var message = error.GetString("message") ?? obj.GetString("message") ?? obj.GetString("error");
            if (message != null)
                return message;
        }
        return $"Failed chat completion {(int)response.StatusCode}: {text.SafeSubstring(0, 500)}";
    }

    /// <summary>Attach duration + pricing metadata (port of OpenAiCompatible.to_response)</summary>
    public virtual JsonObject ToResponse(JsonObject response, JsonObject chat, DateTimeOffset startedAt, ChatContext? context)
    {
        var metadata = response.GetObject("metadata");
        if (metadata == null)
        {
            metadata = new JsonObject();
            response["metadata"] = metadata;
        }
        metadata["duration"] = ((long)(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds).ToString();
        if (chat.GetString("model") is { } model)
        {
            var pricing = ModelCost(model);
            if (pricing != null && pricing.GetDouble("input") is { } input && pricing.GetDouble("output") is { } output)
            {
                metadata["pricing"] = $"{input}/{output}";
            }
        }
        if (context != null)
        {
            context.ProviderResponse = response;
        }
        return response;
    }

    /// <summary>Apply configured provider params to the request (port of init_chat)</summary>
    public void InitChat(JsonObject chat)
    {
        if (FrequencyPenalty != null) chat["frequency_penalty"] = FrequencyPenalty;
        if (MaxCompletionTokens != null) chat["max_completion_tokens"] = MaxCompletionTokens;
        if (N != null) chat["n"] = N;
        if (ParallelToolCalls != null) chat["parallel_tool_calls"] = ParallelToolCalls;
        if (PresencePenalty != null) chat["presence_penalty"] = PresencePenalty;
        if (PromptCacheKey != null) chat["prompt_cache_key"] = PromptCacheKey;
        if (ReasoningEffort != null) chat["reasoning_effort"] = ReasoningEffort;
        if (SafetyIdentifier != null) chat["safety_identifier"] = SafetyIdentifier;
        if (Seed != null) chat["seed"] = Seed;
        if (ServiceTier != null) chat["service_tier"] = ServiceTier;
        if (Stop != null) chat["stop"] = Stop.DeepClone();
        if (Store != null) chat["store"] = Store;
        if (Temperature != null) chat["temperature"] = Temperature;
        if (TopLogprobs != null) chat["top_logprobs"] = TopLogprobs;
        if (TopP != null) chat["top_p"] = TopP;
        if (Verbosity != null) chat["verbosity"] = Verbosity;
        if (EnableThinking != null) chat["enable_thinking"] = EnableThinking;
    }

    /// <summary>
    /// Parse the provider's SSE stream, accumulating content/reasoning/tool-call deltas and usage,
    /// writing throttled partial updates to the thread (long-polled by the UI), and reassembling
    /// a standard non-streaming OpenAI response (port of handle_stream_response).
    /// </summary>
    public async Task<JsonObject?> HandleStreamResponseAsync(HttpResponseMessage httpRes, JsonObject chat,
        DateTimeOffset startedAt, ChatContext context)
    {
        if ((int)httpRes.StatusCode >= 300)
        {
            var text = await httpRes.Content.ReadAsStringAsync().ConfigAwait();
            throw new Exception(HttpErrorToMessage(httpRes, text));
        }

        string? responseId = null;
        long? createdTime = null;
        string? modelName = null;
        var contentAcc = new StringBuilder();
        var reasoningAcc = new StringBuilder();
        string? reasoningField = null;
        var toolCallsDict = new SortedDictionary<int, JsonObject>();
        string? finishReason = null;
        var usageAcc = new JsonObject();
        var writer = CreateStreamWriter(context);
        // stable timestamp for the in-progress message so the UI doesn't render "Invalid Date"
        // while it streams; kept constant across partial updates
        var msgTimestamp = startedAt.ToUnixTimeMilliseconds();

        JsonObject BuildAssistantMsg(bool includeModel)
        {
            var msg = new JsonObject
            {
                ["role"] = "assistant",
                ["content"] = contentAcc.ToString(),
                ["timestamp"] = msgTimestamp,
            };
            if (includeModel && chat.GetString("model") is { } m)
                msg["model"] = m;
            if (reasoningAcc.Length > 0)
                msg[reasoningField ?? "reasoning_content"] = reasoningAcc.ToString();
            if (toolCallsDict.Count > 0)
            {
                var arr = new JsonArray();
                foreach (var tc in toolCallsDict.Values)
                    arr.Add(tc.Clone());
                msg["tool_calls"] = arr;
            }
            return msg;
        }

        await using var sse = await OpenSseAsync(httpRes, context).ConfigAwait();

        try
        {
            while (await sse.ReadLineAsync().ConfigAwait() is { } line)
            {
                var lineStr = line.Trim();
                if (lineStr.Length == 0 || lineStr.StartsWith(':'))
                    continue;
                if (!lineStr.StartsWith("data: "))
                    continue;

                var dataContent = lineStr[6..].Trim();
                if (dataContent == "[DONE]")
                    break;

                JsonObject? chunk;
                try
                {
                    chunk = ChatJson.TryParseObject(dataContent);
                }
                catch (JsonException)
                {
                    continue;
                }
                if (chunk == null)
                    continue;

                // Providers report an upstream failure as an error chunk mid-stream (e.g. OpenRouter
                // rate-limiting). Surface it instead of ending the stream quietly, which leaves a
                // truncated answer looking complete.
                if (chunk["error"] is { } chunkError)
                    throw new Exception(StreamErrorMessage(chunkError));

                if (chunk.GetString("id") is { } id) responseId = id;
                if (chunk.GetLong("created") is { } created) createdTime = created;
                if (chunk.GetString("model") is { } chunkModel) modelName = chunkModel;

                if (chunk.GetObject("usage") is { } usage)
                {
                    foreach (var entry in usage)
                        usageAcc[entry.Key] = entry.Value?.DeepClone();
                }
                if (chunk.GetDouble("cost") is { } chunkCost)
                {
                    usageAcc["cost"] = chunkCost;
                }

                var choices = chunk.GetArray("choices");
                if (choices is not { Count: > 0 })
                    continue;
                var choice = choices[0] as JsonObject;
                if (choice?["error"] is { } choiceError)
                    throw new Exception(StreamErrorMessage(choiceError));
                if (choice.GetString("finish_reason") is { } fr)
                    finishReason = fr;

                var delta = choice.GetObject("delta");
                if (delta == null)
                    continue;

                if (delta.GetString("content") is { Length: > 0 } content)
                    contentAcc.Append(content);

                foreach (var rKey in ReasoningFields)
                {
                    if (delta.GetString(rKey) is { Length: > 0 } reasoning)
                    {
                        reasoningAcc.Append(reasoning);
                        reasoningField = rKey;
                        break;
                    }
                }

                if (delta.GetArray("tool_calls") is { } toolCalls)
                {
                    foreach (var tcNode in toolCalls)
                    {
                        if (tcNode is not JsonObject tc)
                            continue;
                        var idx = tc.GetInt("index") ?? 0;
                        var fn = tc.GetObject("function");
                        if (!toolCallsDict.TryGetValue(idx, out var existing))
                        {
                            toolCallsDict[idx] = new JsonObject
                            {
                                ["id"] = tc.GetString("id") ?? "",
                                ["type"] = tc.GetString("type") ?? "function",
                                ["function"] = new JsonObject
                                {
                                    ["name"] = fn.GetString("name") ?? "",
                                    ["arguments"] = fn.GetString("arguments") ?? "",
                                },
                            };
                        }
                        else
                        {
                            if (tc.GetString("id") is { } tcId)
                                existing["id"] = existing.GetString("id") + tcId;
                            if (tc.GetString("type") is { } tcType)
                                existing["type"] = tcType;
                            var existingFn = existing.GetObject("function")!;
                            if (fn.GetString("name") is { } fnName)
                                existingFn["name"] = existingFn.GetString("name") + fnName;
                            if (fn.GetString("arguments") is { } fnArgs)
                                existingFn["arguments"] = existingFn.GetString("arguments") + fnArgs;
                        }
                    }
                }

                if (Feature?.ShouldCancelThread(context) == true)
                    break;

                // Hand every chunk to the writer: it keeps the latest in memory and only
                // reaches the db on its checkpoint interval.
                await writer.WriteAsync(BuildAssistantMsg(includeModel: true)).ConfigAwait();
            }
        }
        catch
        {
            // Keep whatever streamed before the failure instead of losing the tail of it,
            // the conversation itself is never at risk here.
            await writer.FlushAsync().ConfigAwait();
            throw;
        }

        if (Feature?.ShouldCancelThread(context) == true)
        {
            Log.LogInformation("Stream cancelled for thread {ThreadId}", writer.ThreadId);
            return null;
        }

        // final thread update for the completed stream
        await writer.WriteAsync(BuildAssistantMsg(includeModel: true), final: true).ConfigAwait();

        var choiceObj = new JsonObject
        {
            ["index"] = 0,
            ["message"] = BuildAssistantMsg(includeModel: false),
            ["finish_reason"] = finishReason ?? "stop",
        };
        var openAiResponse = new JsonObject
        {
            ["id"] = responseId ?? $"gen-{startedAt.ToUnixTimeSeconds()}",
            ["object"] = "chat.completion",
            ["created"] = createdTime ?? startedAt.ToUnixTimeSeconds(),
            ["model"] = modelName ?? chat.GetString("model"),
            ["choices"] = new JsonArray(choiceObj),
            ["usage"] = usageAcc.Count > 0
                ? usageAcc.Clone()
                : new JsonObject { ["prompt_tokens"] = 0, ["completion_tokens"] = 0, ["total_tokens"] = 0 },
        };
        if (usageAcc.GetDouble("cost") is { } cost)
        {
            openAiResponse["cost"] = cost;
        }

        return ToResponse(openAiResponse, chat, startedAt, context);
    }

    public static readonly string[] ReasoningFields = ["reasoning_content", "reasoning", "thinking"];
}
