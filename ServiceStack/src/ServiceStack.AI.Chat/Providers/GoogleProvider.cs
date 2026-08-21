using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Google Gemini (port of llms/extensions/providers/google.py).
/// Gemini's generateContent API differs from OpenAI's in shape (contents/parts rather than
/// messages/content, systemInstruction hoisted out, generationConfig for tuning params) and it
/// only accepts a subset of JSON-schema for tool parameters — see <see cref="SanitizeParameters"/>.
/// Streaming uses `?alt=sse`, so the transport is ordinary `data:` lines with a Gemini payload.
/// </summary>
public class GoogleProvider : OpenAiCompatibleProvider
{
    public override string Sdk => "@ai-sdk/google";

    public JsonObject? SafetySettings { get; set; }
    public JsonObject? ThinkingConfig { get; set; }
    public JsonObject? SpeechConfig { get; set; }

    public override void Populate(JsonObject kwargs)
    {
        kwargs["api"] ??= "https://generativelanguage.googleapis.com";
        base.Populate(kwargs);

        SafetySettings = kwargs["safety_settings"]?.DeepClone() as JsonObject
            ?? (kwargs["safety_settings"] is JsonArray arr ? new JsonObject { ["items"] = arr.Clone() } : null);
        ThinkingConfig = kwargs.GetObject("thinking_config")?.Clone();
        SpeechConfig = kwargs.GetObject("speech_config")?.Clone();

        // Google authenticates with a ?key= query param and rejects the Authorization header
        Headers.Remove("Authorization");
    }

    /// <summary>Raw safety_settings (an array in llms.json) preserved for the request body</summary>
    public JsonArray? SafetySettingsArray =>
        Definition["safety_settings"] as JsonArray;

    public override string? ProviderModel(string model) =>
        model.StartsWith("gemini-", StringComparison.OrdinalIgnoreCase)
            ? model
            : base.ProviderModel(model);

    public override JsonObject? ModelInfo(string model)
    {
        var info = base.ModelInfo(model);
        if (info != null)
            return info;
        // allow any gemini-* model id even when it's not in the catalogue
        return model.StartsWith("gemini-", StringComparison.OrdinalIgnoreCase)
            ? new JsonObject
            {
                ["id"] = model,
                ["name"] = model,
                ["tool_call"] = true,
                ["cost"] = new JsonObject { ["input"] = 0, ["output"] = 0 },
            }
            : null;
    }

    /// <summary>
    /// Gemini rejects several standard JSON-schema keywords, so tool parameters must be stripped
    /// recursively before use (port of sanitize_parameters).
    /// </summary>
    public static JsonNode? SanitizeParameters(JsonNode? node)
    {
        if (node is not JsonObject obj)
            return node?.DeepClone();

        var p = obj.Clone();
        foreach (var forbidden in new[] { "$schema", "additionalProperties" })
        {
            p.Remove(forbidden);
        }

        if (p.GetObject("properties") is { } properties)
        {
            var sanitized = new JsonObject();
            foreach (var entry in properties)
                sanitized[entry.Key] = SanitizeParameters(entry.Value);
            p["properties"] = sanitized;
        }

        if (p["items"] is { } items)
        {
            p["items"] = items is JsonArray itemsArr
                ? new JsonArray(itemsArr.Select(SanitizeParameters).ToArray())
                : SanitizeParameters(items);
        }

        foreach (var combinator in new[] { "allOf", "anyOf", "oneOf" })
        {
            if (p[combinator] is JsonArray combined)
                p[combinator] = new JsonArray(combined.Select(SanitizeParameters).ToArray());
        }

        if (p["not"] is { } not)
            p["not"] = SanitizeParameters(not);

        foreach (var defKey in new[] { "definitions", "$defs" })
        {
            if (p.GetObject(defKey) is { } defs)
            {
                var sanitizedDefs = new JsonObject();
                foreach (var entry in defs)
                    sanitizedDefs[entry.Key] = SanitizeParameters(entry.Value);
                p[defKey] = sanitizedDefs;
            }
        }
        return p;
    }

    public override async Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context)
    {
        var model = chat.GetString("model") ?? throw new ArgumentException("Model not specified");
        chat["model"] = ProviderModel(model) ?? model;
        var modelInfo = context.ModelInfo ?? ModelInfo(chat.GetString("model")!) ?? new JsonObject();

        var isStream = chat.TryGetPropertyValue("stream", out _) ? chat.GetBool("stream") : Stream;
        chat = await ProcessChatAsync(chat, Id).ConfigAwait();

        var modalities = chat.GetArray("modalities");
        var hasMediaModality = modalities?.Any(x =>
            x?.GetValue<string>() is "image" or "audio") == true;
        if (hasMediaModality)
            isStream = false; // Gemini can't stream media responses

        var body = ToGeminiRequest(chat, modelInfo, hasMediaModality);

        var action = isStream ? "streamGenerateContent" : "generateContent";
        var url = $"{Api}/v1beta/models/{chat.GetString("model")}:{action}?key={ApiKey}"
            + (isStream ? "&alt=sse" : "");
        Log.LogInformation("POST {Url} (stream={Stream}, tools={Tools})",
            url.LeftPart("?key="), isStream, body.GetArray("tools")?.Count ?? 0);

        var startedAt = DateTimeOffset.UtcNow;
        using var client = CreateHttpClient(streaming: isStream);
        var httpReq = new HttpRequestMessage(HttpMethod.Post, url);
        httpReq.Content = new StringContent(body.ToJsonString(ChatJson.Options), Encoding.UTF8, MimeTypes.Json);

        using var res = isStream
            ? await SendStreamingAsync(client, httpReq, context).ConfigAwait()
            : await client.SendAsync(httpReq, HttpCompletionOption.ResponseContentRead,
                context.CancellationToken).ConfigAwait();

        if (!isStream)
        {
            var response = await ReadJsonResponseAsync(res).ConfigAwait();
            return ToGeminiResponse(response, chat, startedAt, context);
        }
        return await HandleGeminiStreamAsync(res, chat, startedAt, context).ConfigAwait() ?? new JsonObject();
    }

    /// <summary>
    /// Build the Gemini generateContent request body from an OpenAI-shaped chat.
    /// Separated from the HTTP call so the translation (especially tools + schema sanitization)
    /// can be unit tested.
    /// </summary>
    public JsonObject ToGeminiRequest(JsonObject chat, JsonObject modelInfo, bool hasMediaModality)
    {
        var modalities = chat.GetArray("modalities");
        // Gemini models support tool calls unless the catalogue explicitly says otherwise
        var supportsToolCalls = !hasMediaModality && modelInfo.GetBool("tool_call", defaultValue: true);

        // File Search is a Gemini built-in tool and currently cannot be combined reliably with
        // function declarations. Keep the user's selected tools intact in the thread, but isolate
        // File Search in the provider-specific outgoing payload.
        JsonArray? tools = null;
        if (supportsToolCalls && chat.GetArray("tools") is { Count: > 0 } chatTools)
        {
            var fileSearch = chatTools.OfType<JsonObject>()
                .FirstOrDefault(x => x.GetString("type") == "file_search")?["file_search"];
            if (fileSearch != null)
            {
                tools = new JsonArray(new JsonObject { ["file_search"] = fileSearch.DeepClone() });
            }
            else
            {
                var functionDeclarations = new JsonArray();
                foreach (var tool in chatTools.OfType<JsonObject>())
                {
                    if (tool.GetString("type") != "function" || tool.GetObject("function") is not { } fn)
                        continue;
                    functionDeclarations.Add(new JsonObject
                    {
                        ["name"] = fn.GetString("name"),
                        ["description"] = fn.GetString("description"),
                        ["parameters"] = SanitizeParameters(fn["parameters"]),
                    });
                }
                if (functionDeclarations.Count > 0)
                    tools = new JsonArray(new JsonObject { ["function_declarations"] = functionDeclarations });
            }
        }

        var (contents, systemPrompt) = ToGeminiContents(chat);
        if (hasMediaModality)
            systemPrompt = null;

        var body = new JsonObject { ["contents"] = contents };
        if (tools != null)
            body["tools"] = tools;
        if (SafetySettingsArray is { } safety)
            body["safetySettings"] = safety.Clone();
        if (systemPrompt != null)
        {
            body["systemInstruction"] = new JsonObject
            {
                ["parts"] = new JsonArray(new JsonObject { ["text"] = systemPrompt }),
            };
        }

        var generationConfig = new JsonObject();
        if (chat.GetInt("max_completion_tokens") is { } maxTokens)
            generationConfig["maxOutputTokens"] = maxTokens;
        if (chat["stop"] is { } stop)
            generationConfig["stopSequences"] = stop is JsonArray ? stop.DeepClone() : new JsonArray(stop.DeepClone());
        if (chat.GetDouble("temperature") is { } temperature)
            generationConfig["temperature"] = temperature;
        if (chat.GetDouble("top_p") is { } topP)
            generationConfig["topP"] = topP;
        if (chat.GetInt("top_logprobs") is { } topK)
            generationConfig["topK"] = topK;

        // thinking is opt-in per model unless explicitly requested
        var modelLower = (chat.GetString("model") ?? "").ToLowerInvariant();
        var isThinkingModel = modelLower.Contains("thinking")
            || modelInfo.GetBool("thinking")
            || modelInfo.TryGetPropertyValue("thinking_budget", out _);
        var enableThinking = chat.TryGetPropertyValue("enable_thinking", out _)
            ? chat.GetBool("enable_thinking")
            : (bool?)null;

        if (chat.GetObject("thinkingConfig") is { } explicitThinking)
            generationConfig["thinkingConfig"] = explicitThinking.Clone();
        else if ((enableThinking == true || (enableThinking != false && isThinkingModel)) && ThinkingConfig != null)
            generationConfig["thinkingConfig"] = ThinkingConfig.Clone();

        if (chat.GetObject("response_format") is { } responseFormat)
        {
            var formatType = responseFormat.GetString("type");
            if (formatType == "json_object")
            {
                generationConfig["responseMimeType"] = MimeTypes.Json;
            }
            else if (formatType == "json_schema"
                && responseFormat.GetObject("json_schema")?["schema"] is { } schema)
            {
                generationConfig["responseMimeType"] = MimeTypes.Json;
                generationConfig["responseJsonSchema"] = SanitizeParameters(schema);
            }
        }

        if (modalities is { Count: > 0 })
        {
            generationConfig["responseModalities"] = new JsonArray(modalities
                .Select(x => (JsonNode)(x?.GetValue<string>() ?? "").ToUpperInvariant()).ToArray());

            if (modalities.Any(x => x?.GetValue<string>() == "image")
                && chat.GetObject("image_config") is { } imageConfig)
            {
                generationConfig.Remove("thinkingConfig");
                var configMap = new Dictionary<string, string>
                {
                    ["aspect_ratio"] = "aspectRatio",
                    ["image_size"] = "imageSize",
                };
                var geminiImageConfig = new JsonObject();
                foreach (var entry in imageConfig)
                {
                    if (configMap.TryGetValue(entry.Key, out var mapped))
                        geminiImageConfig[mapped] = entry.Value?.DeepClone();
                }
                generationConfig["imageConfig"] = geminiImageConfig;
            }
            if (modalities.Any(x => x?.GetValue<string>() == "audio") && SpeechConfig != null)
            {
                generationConfig.Remove("thinkingConfig");
                generationConfig["speechConfig"] = SpeechConfig.Clone();
                // Google's audio models currently only accept AUDIO
                generationConfig["responseModalities"] = new JsonArray("AUDIO");
            }
        }

        if (generationConfig.Count > 0)
            body["generationConfig"] = generationConfig;

        return body;
    }

    /// <summary>OpenAI messages → Gemini contents[] (+ the hoisted system prompt)</summary>
    public static (JsonArray Contents, string? SystemPrompt) ToGeminiContents(JsonObject chat)
    {
        var contents = new JsonArray();
        string? systemPrompt = null;
        var toolIdMap = new Dictionary<string, string>();

        foreach (var messageNode in chat.GetArray("messages") ?? [])
        {
            if (messageNode is not JsonObject message)
                continue;
            var msgRole = message.GetString("role");

            if (msgRole == "system")
            {
                systemPrompt = ChatMessages.ContentToText(message["content"]) ?? systemPrompt;
                continue;
            }
            if (!message.TryGetPropertyValue("content", out _) && message.GetArray("tool_calls") == null)
                continue;

            var role = msgRole switch
            {
                "assistant" => "model",
                "tool" => "function",
                _ => "user",
            };
            var parts = new JsonArray();

            // assistant tool calls → functionCall parts
            if (msgRole == "assistant" && message.GetArray("tool_calls") is { } toolCalls)
            {
                foreach (var toolCallNode in toolCalls)
                {
                    if (toolCallNode is not JsonObject toolCall)
                        continue;
                    var fn = toolCall.GetObject("function");
                    var name = fn.GetString("name") ?? "";
                    if (toolCall.GetString("id") is { } id)
                        toolIdMap[id] = name;

                    var part = new JsonObject
                    {
                        ["functionCall"] = new JsonObject
                        {
                            ["name"] = name,
                            ["args"] = ChatJson.TryParseObject(fn.GetString("arguments")) ?? new JsonObject(),
                        },
                    };
                    // Gemini requires the thought signature to be echoed back
                    var signature = toolCall.GetString("thoughtSignature")
                        ?? toolCall.GetString("thought_signature")
                        ?? toolCall.GetObject("extra_content")?.GetObject("google")?.GetString("thought_signature")
                        ?? fn.GetString("thoughtSignature");
                    if (signature != null)
                        part["thoughtSignature"] = signature;
                    parts.Add(part);
                }
            }

            // tool results → functionResponse parts (Gemini needs the function name, not the id)
            if (msgRole == "tool" && message.GetString("tool_call_id") is { } toolCallId
                && toolIdMap.TryGetValue(toolCallId, out var fnName))
            {
                var content = message.GetString("content") ?? "";
                var responseData = ChatJson.TryParseObject(content)
                    ?? new JsonObject { ["content"] = content };
                parts.Add(new JsonObject
                {
                    ["functionResponse"] = new JsonObject
                    {
                        ["name"] = fnName,
                        ["response"] = responseData,
                    },
                });
            }

            if (message["content"] is JsonArray contentParts)
            {
                foreach (var partNode in contentParts)
                {
                    if (partNode is not JsonObject item)
                        continue;
                    switch (item.GetString("type"))
                    {
                        case "image_url" when item.GetObject("image_url")?.GetString("url") is { } imageUrl:
                            parts.Add(InlineDataPart(imageUrl, "image/png", "Image was not downloaded: "));
                            break;
                        case "input_audio" when item.GetObject("input_audio") is { } inputAudio:
                            if (inputAudio.GetString("data") is { } audioData)
                            {
                                parts.Add(new JsonObject
                                {
                                    ["inline_data"] = new JsonObject
                                    {
                                        ["mime_type"] = $"audio/{inputAudio.GetString("format") ?? "mp3"}",
                                        ["data"] = audioData.StartsWith("data:") ? audioData.RightPart(',') : audioData,
                                    },
                                });
                            }
                            break;
                        case "file" when item.GetObject("file")?.GetString("file_data") is { } fileData:
                            parts.Add(InlineDataPart(fileData, "application/octet-stream", "File was not downloaded: "));
                            break;
                    }
                    if (item.GetString("text") is { } text)
                        parts.Add(new JsonObject { ["text"] = text });
                }
            }
            else if (message.GetString("content") is { Length: > 0 } stringContent)
            {
                parts.Add(new JsonObject { ["text"] = stringContent });
            }

            if (parts.Count > 0)
            {
                contents.Add(new JsonObject { ["role"] = role, ["parts"] = parts });
            }
        }
        return (contents, systemPrompt);
    }

    static JsonObject InlineDataPart(string dataUri, string defaultMimeType, string error)
    {
        if (!dataUri.StartsWith("data:"))
            throw new Exception(error + dataUri.SafeSubstring(0, 100));
        var mimeType = dataUri.Contains(';')
            ? dataUri.LeftPart(';').RightPart(':')
            : defaultMimeType;
        return new JsonObject
        {
            ["inline_data"] = new JsonObject
            {
                ["mime_type"] = mimeType,
                ["data"] = dataUri.RightPart(','),
            },
        };
    }

    /// <summary>Gemini candidates → an OpenAI chat.completion</summary>
    public JsonObject ToGeminiResponse(JsonObject response, JsonObject chat,
        DateTimeOffset startedAt, ChatContext? context)
    {
        if (context != null)
            context.ProviderResponse = response;

        var textParts = new List<string>();
        var reasoningParts = new List<string>();
        var toolCalls = new JsonArray();
        var images = new JsonArray();
        JsonObject? groundingAcc = null;
        string? finishReason = null;

        foreach (var candidateNode in response.GetArray("candidates") ?? [])
        {
            if (candidateNode is not JsonObject candidate)
                continue;
            finishReason ??= candidate.GetString("finishReason");
            groundingAcc = GeminiGrounding.Merge(groundingAcc, candidate.GetObject("groundingMetadata"));

            foreach (var partNode in candidate.GetObject("content")?.GetArray("parts") ?? [])
            {
                if (partNode is not JsonObject part)
                    continue;

                if (part.GetString("text") is { } text)
                {
                    if (part.GetBool("thought"))
                        reasoningParts.Add(text);
                    else
                        textParts.Add(text);
                }
                if (part.GetObject("functionCall") is { } fc)
                {
                    var toolCall = new JsonObject
                    {
                        ["id"] = $"call_{Guid.NewGuid().ToString("n")[..8]}",
                        ["type"] = "function",
                        ["function"] = new JsonObject
                        {
                            ["name"] = fc.GetString("name"),
                            ["arguments"] = (fc["args"] ?? new JsonObject()).ToJsonString(ChatJson.Options),
                        },
                    };
                    if ((part.GetString("thoughtSignature") ?? part.GetString("thought_signature")) is { } signature)
                    {
                        toolCall["thoughtSignature"] = signature;
                        toolCall["extra_content"] = new JsonObject
                        {
                            ["google"] = new JsonObject { ["thought_signature"] = signature },
                        };
                    }
                    toolCalls.Add(toolCall);
                }
                if (part.GetObject("inlineData") is { } inlineData
                    && inlineData.GetString("data") is { } base64Data)
                {
                    var mimeType = inlineData.GetString("mimeType") ?? "image/png";
                    var ext = MimeTypes.GetExtension(mimeType).TrimStart('.');
                    if (string.IsNullOrEmpty(ext)) ext = "png";
                    var fileName = $"{chat.GetString("model")}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.{ext}";
                    var info = GeneratorUtils.ToFileInfo(chat);
                    var saved = Feature!.SaveToCache(Convert.FromBase64String(base64Data), fileName,
                        mimeType, context?.User, info);
                    var group = mimeType.StartsWith("audio/") ? "audios" : "images";
                    images.Add(new JsonObject
                    {
                        ["type"] = group == "audios" ? "audio_url" : "image_url",
                        [group == "audios" ? "audio_url" : "image_url"] = new JsonObject
                        {
                            ["url"] = saved.GetString("url"),
                        },
                    });
                }
            }
        }

        var message = new JsonObject
        {
            ["role"] = "assistant",
            ["content"] = string.Join("", textParts),
        };
        if (reasoningParts.Count > 0)
            message["reasoning_content"] = string.Join("", reasoningParts);
        if (toolCalls.Count > 0)
            message["tool_calls"] = toolCalls;
        if (images.Count > 0)
            message["images"] = images;
        var grounding = GeminiGrounding.Finalize(groundingAcc, message.GetString("content"),
            trustSpans: textParts.Count <= 1);
        if (grounding != null)
            message["groundingMetadata"] = grounding;

        var usageMetadata = response.GetObject("usageMetadata");
        var inputTokens = usageMetadata.GetLong("promptTokenCount") ?? 0;
        var outputTokens = usageMetadata.GetLong("candidatesTokenCount") ?? 0;

        var ret = new JsonObject
        {
            ["id"] = response.GetString("responseId") ?? $"gen-{startedAt.ToUnixTimeSeconds()}",
            ["object"] = "chat.completion",
            ["created"] = startedAt.ToUnixTimeSeconds(),
            ["model"] = response.GetString("modelVersion") ?? chat.GetString("model"),
            ["choices"] = new JsonArray(new JsonObject
            {
                ["index"] = 0,
                ["message"] = message,
                ["finish_reason"] = (finishReason ?? "stop").ToLowerInvariant() switch
                {
                    "max_tokens" => "length",
                    var f => f == "stop" ? "stop" : f,
                },
            }),
            ["usage"] = new JsonObject
            {
                ["prompt_tokens"] = inputTokens,
                ["completion_tokens"] = outputTokens,
                ["total_tokens"] = inputTokens + outputTokens,
            },
        };
        return ToResponse(ret, chat, startedAt, context);
    }

    /// <summary>
    /// Gemini's `?alt=sse` stream: `data:` lines carrying generateContent payloads, accumulated into
    /// the same shape the OpenAI path produces (incl. throttled partial thread writes for the UI).
    /// </summary>
    public async Task<JsonObject?> HandleGeminiStreamAsync(HttpResponseMessage httpRes, JsonObject chat,
        DateTimeOffset startedAt, ChatContext context)
    {
        if ((int)httpRes.StatusCode >= 300)
        {
            var errorText = await httpRes.Content.ReadAsStringAsync().ConfigAwait();
            throw new Exception(HttpErrorToMessage(httpRes, errorText));
        }

        string? responseId = null, modelName = null, finishReason = null;
        var contentAcc = new StringBuilder();
        var reasoningAcc = new StringBuilder();
        var toolCalls = new JsonArray();
        var usageAcc = new JsonObject();
        JsonObject? groundingAcc = null;
        JsonObject? grounding = null;
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
                msg["reasoning_content"] = reasoningAcc.ToString();
            if (toolCalls.Count > 0)
                msg["tool_calls"] = toolCalls.Clone();
            if (grounding != null)
                msg["groundingMetadata"] = grounding.DeepClone();
            return msg;
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

                // Gemini reports an upstream failure as an error chunk mid-stream; surface it instead of
                // ending the stream quietly, which leaves a truncated answer looking complete
                if (chunk["error"] is { } chunkError)
                    throw new Exception(StreamErrorMessage(chunkError, "Google Gemini streaming error"));

                responseId ??= chunk.GetString("responseId");
                modelName ??= chunk.GetString("modelVersion");

                if (chunk.GetObject("usageMetadata") is { } usage)
                {
                    var inputTokens = usage.GetLong("promptTokenCount") ?? 0;
                    var outputTokens = usage.GetLong("candidatesTokenCount") ?? 0;
                    usageAcc["prompt_tokens"] = inputTokens;
                    usageAcc["completion_tokens"] = outputTokens;
                    usageAcc["total_tokens"] = inputTokens + outputTokens;
                }

                foreach (var candidateNode in chunk.GetArray("candidates") ?? [])
                {
                    if (candidateNode is not JsonObject candidate)
                        continue;
                    if (candidate.GetString("finishReason") is { } fr)
                        finishReason = fr;
                    groundingAcc = GeminiGrounding.Merge(groundingAcc, candidate.GetObject("groundingMetadata"));

                    foreach (var partNode in candidate.GetObject("content")?.GetArray("parts") ?? [])
                    {
                        if (partNode is not JsonObject part)
                            continue;
                        if (part.GetString("text") is { } text)
                        {
                            if (part.GetBool("thought"))
                                reasoningAcc.Append(text);
                            else
                                contentAcc.Append(text);
                        }
                        if (part.GetObject("functionCall") is { } fc)
                        {
                            toolCalls.Add(new JsonObject
                            {
                                ["id"] = $"call_{Guid.NewGuid().ToString("n")[..8]}",
                                ["type"] = "function",
                                ["function"] = new JsonObject
                                {
                                    ["name"] = fc.GetString("name"),
                                    ["arguments"] = (fc["args"] ?? new JsonObject()).ToJsonString(ChatJson.Options),
                                },
                            });
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

        grounding = GeminiGrounding.Finalize(groundingAcc, contentAcc.ToString());
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
                ["finish_reason"] = (finishReason ?? "stop").ToLowerInvariant() == "max_tokens" ? "length" : "stop",
            }),
            ["usage"] = usageAcc.Count > 0
                ? usageAcc.Clone()
                : new JsonObject { ["prompt_tokens"] = 0, ["completion_tokens"] = 0, ["total_tokens"] = 0 },
        };
        return ToResponse(ret, chat, startedAt, context);
    }
}
