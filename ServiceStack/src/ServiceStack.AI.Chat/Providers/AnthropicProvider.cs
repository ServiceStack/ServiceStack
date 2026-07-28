using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Anthropic Messages API (port of llms/extensions/providers/anthropic.py).
/// Anthropic's wire format differs from OpenAI's in three places, each handled below:
/// the request (system prompt hoisted out, content blocks, input_schema tools), the streaming
/// protocol (typed events rather than choice deltas), and the response (content blocks).
///
/// Also registered for the `@ai-sdk/anthropic-cli` sdk id: upstream's CLI variant shells out to a
/// local `claude` binary to reuse a Claude Code subscription, which doesn't apply to a web host —
/// the API-key provider is used for both so the bundled `anthropic` config works out of the box.
/// </summary>
public class AnthropicProvider : OpenAiCompatibleProvider
{
    public override string Sdk => "@ai-sdk/anthropic";
    public override string ChatUrl => $"{Api}/messages";

    public override void Populate(JsonObject kwargs)
    {
        kwargs["api"] ??= "https://api.anthropic.com/v1";
        base.Populate(kwargs);

        // Anthropic authenticates with x-api-key, not Authorization: Bearer
        Headers.Remove("Authorization");
        if (!string.IsNullOrEmpty(ApiKey))
            Headers["x-api-key"] = ApiKey;
        Headers.TryAdd("anthropic-version", "2023-06-01");
    }

    // ── Request translation ──

    /// <summary>
    /// OpenAI messages → Anthropic (system, messages). System messages are hoisted to a top-level
    /// parameter; tool results must ride inside a user message (port of to_anthropic_messages).
    /// </summary>
    public static (string? System, JsonArray Messages) ToAnthropicMessages(JsonObject chat)
    {
        var systemParts = new List<string>();
        foreach (var messageNode in chat.GetArray("messages") ?? [])
        {
            if (messageNode is not JsonObject message || message.GetString("role") != "system")
                continue;
            if (message["content"] is JsonValue v && v.TryGetValue<string>(out var s))
            {
                systemParts.Add(s);
            }
            else if (message["content"] is JsonArray parts)
            {
                foreach (var partNode in parts)
                {
                    if (partNode is JsonObject part && part.GetString("type") == "text")
                        systemParts.Add(part.GetString("text") ?? "");
                }
            }
        }
        var systemPrompt = systemParts.Count > 0 ? string.Join("\n", systemParts) : null;

        var messages = new JsonArray();
        foreach (var messageNode in chat.GetArray("messages") ?? [])
        {
            if (messageNode is not JsonObject message)
                continue;
            var role = message.GetString("role");
            if (role == "system")
                continue;

            if (role == "tool")
            {
                var toolResult = new JsonObject
                {
                    ["type"] = "tool_result",
                    ["tool_use_id"] = message.GetString("tool_call_id"),
                    ["content"] = message["content"]?.DeepClone() ?? "",
                };
                // Anthropic requires tool results inside a user message
                if (messages.Count > 0 && messages[^1] is JsonObject last
                    && last.GetString("role") == "user" && last["content"] is JsonArray lastContent)
                {
                    lastContent.Add(toolResult);
                }
                else
                {
                    messages.Add(new JsonObject { ["role"] = "user", ["content"] = new JsonArray(toolResult) });
                }
                continue;
            }

            var blocks = new JsonArray();
            if (message.GetString("thinking") is { Length: > 0 } thinking)
            {
                blocks.Add(new JsonObject { ["type"] = "thinking", ["thinking"] = thinking });
            }

            var hasToolCalls = message.GetArray("tool_calls") is { Count: > 0 };
            JsonNode? contentNode = null;

            if (message["content"] is JsonValue cv && cv.TryGetValue<string>(out var text))
            {
                if (blocks.Count > 0 || hasToolCalls)
                {
                    if (!string.IsNullOrEmpty(text))
                        blocks.Add(new JsonObject { ["type"] = "text", ["text"] = text });
                }
                else
                {
                    contentNode = text; // plain string content when there's nothing else
                }
            }
            else if (message["content"] is JsonArray contentParts)
            {
                foreach (var partNode in contentParts)
                {
                    if (partNode is not JsonObject part)
                        continue;
                    switch (part.GetString("type"))
                    {
                        case "text":
                            blocks.Add(new JsonObject { ["type"] = "text", ["text"] = part.GetString("text") ?? "" });
                            break;
                        case "image_url" when part.GetObject("image_url")?.GetString("url") is { } imageUrl:
                            if (SplitDataUri(imageUrl) is var (declaredType, base64) && base64 != null)
                            {
                                blocks.Add(new JsonObject
                                {
                                    ["type"] = "image",
                                    ["source"] = new JsonObject
                                    {
                                        ["type"] = "base64",
                                        // the declared type can be stale after conversion — trust the bytes
                                        ["media_type"] = DetectImageMediaType(base64, declaredType),
                                        ["data"] = base64,
                                    },
                                });
                            }
                            break;
                        case "file" when part.GetObject("file")?.GetString("file_data") is { } fileData:
                            if (SplitDataUri(fileData) is var (fileType, fileB64) && fileB64 != null)
                            {
                                blocks.Add(new JsonObject
                                {
                                    ["type"] = "document",
                                    ["source"] = new JsonObject
                                    {
                                        ["type"] = "base64",
                                        ["media_type"] = fileType ?? "application/pdf",
                                        ["data"] = fileB64,
                                    },
                                });
                            }
                            break;
                    }
                }
            }

            if (hasToolCalls)
            {
                foreach (var toolCallNode in message.GetArray("tool_calls")!)
                {
                    if (toolCallNode is not JsonObject toolCall)
                        continue;
                    var fn = toolCall.GetObject("function");
                    JsonNode input;
                    try
                    {
                        input = ChatJson.TryParseObject(fn.GetString("arguments")) ?? new JsonObject();
                    }
                    catch (JsonException)
                    {
                        input = new JsonObject();
                    }
                    blocks.Add(new JsonObject
                    {
                        ["type"] = "tool_use",
                        ["id"] = toolCall.GetString("id"),
                        ["name"] = fn.GetString("name"),
                        ["input"] = input,
                    });
                }
            }

            messages.Add(new JsonObject
            {
                ["role"] = role,
                ["content"] = contentNode ?? blocks,
            });
        }
        return (systemPrompt, messages);
    }

    static (string? MediaType, string? Base64) SplitDataUri(string uri)
    {
        if (!uri.StartsWith("data:"))
            return (null, null);
        var idx = uri.IndexOf(";base64,", StringComparison.Ordinal);
        if (idx < 0)
            return (null, null);
        return (uri[5..idx], uri[(idx + ";base64,".Length)..]);
    }

    /// <summary>Identify an image from its magic bytes, falling back to the declared type</summary>
    public static string DetectImageMediaType(string base64Data, string? declaredType = null)
    {
        try
        {
            // decode a whole number of base64 quads from the start (4 chars → 3 bytes)
            var take = Math.Min(base64Data.Length, 32) / 4 * 4;
            if (take == 0)
                return declaredType ?? "image/png";
            var header = Convert.FromBase64String(base64Data[..take]);
            if (header.Length >= 12 && header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F'
                && header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P')
                return "image/webp";
            if (header.Length >= 8 && header[0] == 0x89 && header[1] == 'P' && header[2] == 'N' && header[3] == 'G')
                return "image/png";
            if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                return "image/jpeg";
            if (header.Length >= 4 && header[0] == 'G' && header[1] == 'I' && header[2] == 'F' && header[3] == '8')
                return "image/gif";
        }
        catch (Exception)
        {
            // fall through to the declared type
        }
        return declaredType ?? "image/png";
    }

    public override async Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context)
    {
        var model = chat.GetString("model") ?? throw new ArgumentException("Model not specified");
        chat["model"] = ProviderModel(model) ?? model;

        var isStream = chat.TryGetPropertyValue("stream", out _) ? chat.GetBool("stream") : Stream;
        chat = await ProcessChatAsync(chat, Id).ConfigAwait();

        var (systemPrompt, messages) = ToAnthropicMessages(chat);
        var body = new JsonObject
        {
            ["model"] = chat.GetString("model"),
            ["messages"] = messages,
            // max_tokens is required by Anthropic
            ["max_tokens"] = chat.GetInt("max_completion_tokens") ?? chat.GetInt("max_tokens") ?? 4096,
        };
        if (systemPrompt != null)
            body["system"] = systemPrompt;
        if (chat.GetDouble("temperature") is { } temperature)
            body["temperature"] = temperature;
        if (chat.GetDouble("top_p") is { } topP)
            body["top_p"] = topP;
        if (chat.GetInt("top_k") is { } topK)
            body["top_k"] = topK;
        if (chat["stop"] is { } stop)
            body["stop_sequences"] = stop is JsonArray ? stop.DeepClone() : new JsonArray(stop.DeepClone());
        if (isStream)
            body["stream"] = true;

        if (chat.GetArray("tools") is { Count: > 0 } tools)
        {
            var anthropicTools = new JsonArray();
            foreach (var toolNode in tools)
            {
                if (toolNode is not JsonObject tool || tool.GetString("type") != "function")
                    continue;
                var fn = tool.GetObject("function");
                anthropicTools.Add(new JsonObject
                {
                    ["name"] = fn.GetString("name"),
                    ["description"] = fn.GetString("description"),
                    ["input_schema"] = fn.GetObject("parameters")?.Clone(),
                });
            }
            if (anthropicTools.Count > 0)
                body["tools"] = anthropicTools;
        }
        if (chat["tool_choice"] is { } toolChoice)
            body["tool_choice"] = toolChoice.DeepClone();

        // structured output maps to Anthropic's output_config
        if (chat.GetObject("response_format") is { } responseFormat
            && responseFormat.GetString("type") == "json_schema"
            && responseFormat.GetObject("json_schema")?.GetObject("schema") is { } schema)
        {
            body["output_config"] = new JsonObject
            {
                ["format"] = new JsonObject
                {
                    ["type"] = "json_schema",
                    ["schema"] = schema.Clone(),
                },
            };
        }

        Log.LogInformation("POST {Url} (stream={Stream})", ChatUrl, isStream);

        var startedAt = DateTimeOffset.UtcNow;
        using var client = CreateHttpClient(streaming: isStream);
        var httpReq = new HttpRequestMessage(HttpMethod.Post, ChatUrl);
        foreach (var entry in Headers)
        {
            if (!entry.Key.EqualsIgnoreCase("Content-Type"))
                httpReq.Headers.TryAddWithoutValidation(entry.Key, entry.Value);
        }
        httpReq.Content = new StringContent(body.ToJsonString(ChatJson.Options), Encoding.UTF8, MimeTypes.Json);

        using var res = isStream
            ? await SendStreamingAsync(client, httpReq, context).ConfigAwait()
            : await client.SendAsync(httpReq, HttpCompletionOption.ResponseContentRead,
                context.CancellationToken).ConfigAwait();

        if (!isStream)
        {
            var response = await ReadJsonResponseAsync(res).ConfigAwait();
            return ToAnthropicResponse(response, chat, startedAt, context);
        }
        return await HandleAnthropicStreamAsync(res, chat, startedAt, context).ConfigAwait()
            ?? new JsonObject();
    }

    // ── Response translation ──

    /// <summary>Anthropic content blocks → an OpenAI chat.completion (port of to_response)</summary>
    public JsonObject ToAnthropicResponse(JsonObject response, JsonObject chat,
        DateTimeOffset startedAt, ChatContext? context)
    {
        if (context != null)
            context.ProviderResponse = response;

        var contentParts = new List<string>();
        var thinkingParts = new List<string>();
        var toolCalls = new JsonArray();

        foreach (var blockNode in response.GetArray("content") ?? [])
        {
            if (blockNode is not JsonObject block)
                continue;
            switch (block.GetString("type"))
            {
                case "text":
                    contentParts.Add(block.GetString("text") ?? "");
                    break;
                case "thinking":
                    thinkingParts.Add(block.GetString("thinking") ?? "");
                    break;
                case "tool_use":
                    toolCalls.Add(new JsonObject
                    {
                        ["id"] = block.GetString("id"),
                        ["type"] = "function",
                        ["function"] = new JsonObject
                        {
                            ["name"] = block.GetString("name"),
                            ["arguments"] = (block["input"] ?? new JsonObject()).ToJsonString(ChatJson.Options),
                        },
                    });
                    break;
            }
        }

        var message = new JsonObject
        {
            ["role"] = "assistant",
            ["content"] = string.Join("\n", contentParts),
        };
        if (thinkingParts.Count > 0)
            message["thinking"] = string.Join("\n", thinkingParts);
        if (toolCalls.Count > 0)
            message["tool_calls"] = toolCalls;

        var usage = response.GetObject("usage");
        var inputTokens = usage.GetLong("input_tokens") ?? 0;
        var outputTokens = usage.GetLong("output_tokens") ?? 0;

        var ret = new JsonObject
        {
            ["id"] = response.GetString("id") ?? "",
            ["object"] = "chat.completion",
            ["created"] = startedAt.ToUnixTimeSeconds(),
            ["model"] = response.GetString("model") ?? chat.GetString("model"),
            ["choices"] = new JsonArray(new JsonObject
            {
                ["index"] = 0,
                ["message"] = message,
                ["finish_reason"] = response.GetString("stop_reason") ?? "stop",
            }),
            ["usage"] = new JsonObject
            {
                ["prompt_tokens"] = inputTokens,
                ["completion_tokens"] = outputTokens,
                ["total_tokens"] = inputTokens + outputTokens,
            },
        };
        AddMetadata(ret, chat, startedAt);
        return ret;
    }

    void AddMetadata(JsonObject response, JsonObject chat, DateTimeOffset startedAt)
    {
        var metadata = new JsonObject
        {
            ["duration"] = (long)(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds,
        };
        if (chat.GetString("model") is { } model && ModelCost(model) is { } cost
            && cost.GetDouble("input") is { } input && cost.GetDouble("output") is { } output)
        {
            metadata["pricing"] = $"{input}/{output}";
        }
        response["metadata"] = metadata;
    }

    // ── Streaming ──

    /// <summary>
    /// Anthropic streams *typed events* rather than OpenAI-style choice deltas, so this can't reuse
    /// the base SSE accumulator. Events: message_start / content_block_start / content_block_delta
    /// (text_delta | thinking_delta | input_json_delta) / message_delta / message_stop / error.
    /// Partial thread writes are throttled exactly as the OpenAI path does, so the UI streams identically.
    /// </summary>
    public async Task<JsonObject?> HandleAnthropicStreamAsync(HttpResponseMessage httpRes, JsonObject chat,
        DateTimeOffset startedAt, ChatContext context)
    {
        if ((int)httpRes.StatusCode >= 300)
        {
            var errorText = await httpRes.Content.ReadAsStringAsync().ConfigAwait();
            throw new Exception(HttpErrorToMessage(httpRes, errorText));
        }

        string? responseId = null, modelName = null, finishReason = null, reasoningField = null;
        var contentAcc = new StringBuilder();
        var reasoningAcc = new StringBuilder();
        var toolCallsDict = new SortedDictionary<int, JsonObject>();
        var usageAcc = new JsonObject();
        var writer = CreateStreamWriter(context);
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
                msg[reasoningField ?? "thinking"] = reasoningAcc.ToString();
            if (toolCallsDict.Count > 0)
            {
                var arr = new JsonArray();
                foreach (var tc in toolCallsDict.Values)
                    arr.Add(tc.Clone());
                msg["tool_calls"] = arr;
            }
            return msg;
        }

        void SetUsage(JsonObject usage)
        {
            if (usage.GetLong("input_tokens") is { } inputTokens)
                usageAcc["prompt_tokens"] = inputTokens;
            if (usage.GetLong("output_tokens") is { } outputTokens)
                usageAcc["completion_tokens"] = outputTokens;
            usageAcc["total_tokens"] = (usageAcc.GetLong("prompt_tokens") ?? 0)
                + (usageAcc.GetLong("completion_tokens") ?? 0);
        }

        await using var sse = await OpenSseAsync(httpRes, context).ConfigAwait();

        try
        {
            while (await sse.ReadLineAsync().ConfigAwait() is { } line)
            {
                var lineStr = line.Trim();
                if (lineStr.Length == 0 || lineStr.StartsWith(':') || !lineStr.StartsWith("data: "))
                    continue;
                var dataContent = lineStr[6..].Trim();
                if (dataContent == "[DONE]")
                    break;

                var chunk = ChatJson.TryParseObject(dataContent);
                if (chunk == null)
                    continue;

                var stop = false;
                switch (chunk.GetString("type"))
                {
                    case "message_start":
                        var msg = chunk.GetObject("message");
                        responseId = msg.GetString("id") ?? responseId;
                        modelName = msg.GetString("model") ?? modelName;
                        if (msg.GetObject("usage") is { } startUsage)
                            SetUsage(startUsage);
                        break;

                    case "content_block_start":
                    {
                        var idx = chunk.GetInt("index") ?? 0;
                        var block = chunk.GetObject("content_block");
                        switch (block.GetString("type"))
                        {
                            case "tool_use":
                                toolCallsDict[idx] = new JsonObject
                                {
                                    ["id"] = block.GetString("id") ?? "",
                                    ["type"] = "function",
                                    ["function"] = new JsonObject
                                    {
                                        ["name"] = block.GetString("name") ?? "",
                                        ["arguments"] = block.GetObject("input") is { Count: > 0 } input
                                            ? input.ToJsonString(ChatJson.Options)
                                            : "",
                                    },
                                };
                                break;
                            case "thinking" when block.GetString("thinking") is { Length: > 0 } t:
                                reasoningAcc.Append(t);
                                reasoningField = "thinking";
                                break;
                            case "text" when block.GetString("text") is { Length: > 0 } bt:
                                contentAcc.Append(bt);
                                break;
                        }
                        break;
                    }

                    case "content_block_delta":
                    {
                        var idx = chunk.GetInt("index") ?? 0;
                        var delta = chunk.GetObject("delta");
                        switch (delta.GetString("type"))
                        {
                            case "text_delta":
                                contentAcc.Append(delta.GetString("text") ?? "");
                                break;
                            case "thinking_delta":
                                reasoningAcc.Append(delta.GetString("thinking") ?? "");
                                reasoningField = "thinking";
                                break;
                            case "input_json_delta":
                                if (!toolCallsDict.TryGetValue(idx, out var existing))
                                {
                                    existing = new JsonObject
                                    {
                                        ["id"] = "",
                                        ["type"] = "function",
                                        ["function"] = new JsonObject { ["name"] = "", ["arguments"] = "" },
                                    };
                                    toolCallsDict[idx] = existing;
                                }
                                var fn = existing.GetObject("function")!;
                                fn["arguments"] = fn.GetString("arguments") + (delta.GetString("partial_json") ?? "");
                                break;
                        }
                        break;
                    }

                    case "message_delta":
                        if (chunk.GetObject("delta")?.GetString("stop_reason") is { } sr)
                            finishReason = sr;
                        if (chunk.GetObject("usage") is { } deltaUsage)
                            SetUsage(deltaUsage);
                        break;

                    case "message_stop":
                        stop = true;
                        break;

                    case "error":
                        throw new Exception(StreamErrorMessage(chunk["error"], "Anthropic streaming error"));
                }
                if (stop)
                    break;

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

        await writer.WriteAsync(BuildAssistantMsg(includeModel: true), final: true).ConfigAwait();

        var ret = new JsonObject
        {
            ["id"] = responseId ?? $"gen-{startedAt.ToUnixTimeSeconds()}",
            ["object"] = "chat.completion",
            ["created"] = startedAt.ToUnixTimeSeconds(),
            ["model"] = modelName ?? chat.GetString("model"),
            ["choices"] = new JsonArray(new JsonObject
            {
                ["index"] = 0,
                ["message"] = BuildAssistantMsg(includeModel: false),
                ["finish_reason"] = finishReason ?? "stop",
            }),
            ["usage"] = usageAcc.Count > 0
                ? usageAcc.Clone()
                : new JsonObject { ["prompt_tokens"] = 0, ["completion_tokens"] = 0, ["total_tokens"] = 0 },
        };
        AddMetadata(ret, chat, startedAt);
        context.ProviderResponse = ret;
        return ret;
    }
}
