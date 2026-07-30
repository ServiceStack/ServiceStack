using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Chat orchestration (port of g_chat_completion, main.py:2132): candidate provider selection,
/// retry/failover, the tool-execution loop, usage/cost aggregation and filter invocation.
/// </summary>
public partial class ChatFeature
{
    public virtual async Task<JsonObject> ChatCompletionAsync(JsonObject chat, ChatContext context)
    {
        List<string> candidateProviders;
        string model;
        try
        {
            model = chat.GetString("model") ?? throw new Exception("Model not specified");

            candidateProviders = Providers
                .Where(x => x.Value.ProviderModel(model) != null)
                .Select(x => x.Key)
                .ToList();
            if (candidateProviders.Count == 0)
                throw HttpError.NotFound($"Model {model} not found");

            // pre-populate provider/model info for pre-chat filters
            var firstProvider = Providers[candidateProviders[0]];
            context.Provider ??= firstProvider;
            context.ModelInfo ??= firstProvider.ModelInfo(model);
            context.ModelCost ??= context.ModelInfo.GetObject("cost")
                ?? firstProvider.ModelCost(model)
                ?? new JsonObject { ["input"] = 0, ["output"] = 0 };
        }
        catch (Exception e)
        {
            await Filters.OnChatErrorAsync(e, context).ConfigAwait();
            throw;
        }

        var startedAt = DateTimeOffset.UtcNow;
        Exception? firstException = null;
        var providerName = "Unknown";

        var retries = Limits.Retries;
        var maxIterations = context.Items.GetValueOrDefault("max_iterations") as int? ?? Limits.MaxIterations;

        // inject global tools + apply pre-chat filters ONCE
        var baseChat = CreateChatWithTools(chat, context.Tools);
        context.Chat = baseChat;
        await Filters.OnChatRequestAsync(baseChat, context).ConfigAwait();

        var attemptRound = 0;
        var candidateIndex = 0;

        while (attemptRound < retries)
        {
            if (candidateIndex >= candidateProviders.Count)
            {
                candidateIndex = 0;
                attemptRound++;
                continue;
            }

            var name = candidateProviders[candidateIndex];
            try
            {
                providerName = name;
                var provider = Providers[name];
                Log.LogInformation("provider: {Name} {Type}", name, provider.GetType().Name);

                context.Items["startedAt"] = DateTime.UtcNow;
                context.Provider = provider;
                var modelInfo = provider.ModelInfo(model);
                context.ModelInfo = modelInfo;
                context.ModelCost = modelInfo.GetObject("cost")
                    ?? provider.ModelCost(model)
                    ?? new JsonObject { ["input"] = 0, ["output"] = 0 };

                // deep copy per provider attempt, reset tool history
                var currentChat = baseChat.Clone();
                var toolHistory = new JsonArray();
                JsonObject? finalResponse = null;

                long totalCompletionTokens = 0;
                long lastPromptTokens = 0;
                var accumulatedCost = 0d;

                for (var requestCount = 0; requestCount < maxIterations; requestCount++)
                {
                    if (ShouldCancelThread(context))
                        return CancelledResponse(model);

                    var response = await provider.ChatAsync(currentChat, context).ConfigAwait();

                    if (ShouldCancelThread(context))
                        return CancelledResponse(model);

                    // aggregate usage across turns
                    if (response.GetObject("usage") is { } usage)
                    {
                        if (usage.GetLong("prompt_tokens") is { } promptTokens)
                            lastPromptTokens = promptTokens;
                        totalCompletionTokens += usage.GetLong("completion_tokens") ?? 0;

                        if (response.GetDouble("cost") is { } responseCost)
                            accumulatedCost += responseCost;
                        else if (usage.GetDouble("cost") is { } usageCost)
                            accumulatedCost += usageCost;
                    }

                    var choice = response.GetArray("choices") is { Count: > 0 } choices
                        ? choices[0] as JsonObject
                        : null;
                    var message = choice.GetObject("message");
                    var toolCalls = message.GetArray("tool_calls");
                    var supportsToolCalls = modelInfo.GetBool("tool_call");

                    if (toolCalls is { Count: > 0 } && supportsToolCalls && message != null)
                    {
                        var assistantMsg = message.Clone();
                        assistantMsg["timestamp"] ??= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        currentChat.GetArray("messages")!.Add(assistantMsg.Clone());
                        toolHistory.Add(assistantMsg);

                        await Filters.OnChatToolAsync(currentChat, context).ConfigAwait();

                        var execTasks = toolCalls
                            .Select(tc => ExecuteToolCallAsync(tc as JsonObject, context))
                            .ToList();
                        var toolResults = await Task.WhenAll(execTasks).ConfigAwait();

                        foreach (var (toolCallId, toolResult, resources) in toolResults)
                        {
                            var toolMsg = new JsonObject
                            {
                                ["role"] = "tool",
                                ["tool_call_id"] = toolCallId,
                                ["content"] = toolResult,
                            };
                            foreach (var entry in GroupResources(resources))
                            {
                                toolMsg[entry.Key] = entry.Value;
                            }
                            currentChat.GetArray("messages")!.Add(toolMsg.Clone());
                            toolHistory.Add(toolMsg);

                            await Filters.OnChatToolAsync(currentChat, context).ConfigAwait();
                        }

                        if (ShouldCancelThread(context))
                            return CancelledResponse(model);

                        continue; // send tool results back to the LLM
                    }

                    // no tool calls: this is the final response
                    if (toolHistory.Count > 0)
                        response["tool_history"] = toolHistory.Clone();

                    var finalUsage = response.GetObject("usage");
                    if (finalUsage == null)
                    {
                        finalUsage = new JsonObject();
                        response["usage"] = finalUsage;
                    }
                    var duration = (long)(DateTimeOffset.UtcNow - startedAt).TotalSeconds;
                    context.Items["duration"] = duration;
                    finalUsage["prompt_tokens"] = lastPromptTokens;
                    finalUsage["completion_tokens"] = totalCompletionTokens;
                    finalUsage["total_tokens"] = lastPromptTokens + totalCompletionTokens;
                    finalUsage["duration"] = duration;
                    if (accumulatedCost > 0)
                        response["cost"] = accumulatedCost;

                    finalResponse = response;
                    break;
                }

                if (finalResponse == null)
                    throw new Exception($"Reached maximum tool iterations ({maxIterations}) without receiving final response");

                await Filters.OnChatResponseAsync(finalResponse, context).ConfigAwait();
                return finalResponse;
            }
            catch (Exception e)
            {
                firstException ??= e;
                Log.LogError(e, "Provider {Provider} failed: {Message}", providerName, e.Message);
                await Filters.OnChatStatusAsync(
                    $"Provider {providerName} failed: {ChatJson.ToErrorMessage(firstException)} " +
                    $"({candidateIndex + 1}/{candidateProviders.Count} x {attemptRound + 1} attempts)",
                    context).ConfigAwait();
                candidateIndex++;
            }
        }

        firstException ??= new Exception("All providers failed");
        await Filters.OnChatErrorAsync(firstException, context).ConfigAwait();
        throw firstException;
    }

    static JsonObject CancelledResponse(string model) => new()
    {
        ["id"] = "cancelled",
        ["object"] = "chat.completion",
        ["created"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        ["model"] = model,
        ["choices"] = new JsonArray(new JsonObject
        {
            ["index"] = 0,
            ["message"] = new JsonObject { ["role"] = "assistant", ["content"] = "" },
            ["finish_reason"] = "cancelled",
        }),
    };

    /// <summary>Inject registered tool definitions into the chat (port of create_chat_with_tools)</summary>
    public JsonObject CreateChatWithTools(JsonObject chat, string useTools)
    {
        var currentChat = chat.Clone();
        currentChat["messages"] ??= new JsonArray();

        // don't inject tools for structured output requests
        if (currentChat.TryGetPropertyValue("response_format", out var responseFormat) && responseFormat != null)
            return currentChat;

        var selected = Tools.SelectTools(useTools);
        if (selected.Count == 0)
            return currentChat;

        var chatTools = currentChat.GetArray("tools");
        if (chatTools == null)
        {
            chatTools = new JsonArray();
            currentChat["tools"] = chatTools;
        }

        var existingTools = chatTools
            .Select(x => (x as JsonObject).GetObject("function").GetString("name"))
            .Where(x => x != null)
            .ToSet();

        foreach (var tool in selected)
        {
            if (existingTools.Add(tool.Name))
                chatTools.Add(tool.Definition.Clone());
        }
        return currentChat;
    }

    /// <summary>Execute one tool call, returning (toolCallId, content, resources) (port of _exec_single_tool)</summary>
    async Task<(string ToolCallId, string Content, List<JsonObject> Resources)> ExecuteToolCallAsync(
        JsonObject? toolCall, ChatContext context)
    {
        var toolCallId = toolCall.GetString("id") ?? "";
        var fn = toolCall.GetObject("function");
        var fnName = fn.GetString("name") ?? "";

        JsonObject args;
        try
        {
            args = ChatJson.TryParseObject(fn.GetString("arguments")) ?? new JsonObject();
        }
        catch (Exception e)
        {
            return (toolCallId, $"Error: Failed to parse JSON arguments for tool '{fnName}': {e.Message}", []);
        }

        var (content, resources) = await ExecToolAsync(fnName, args, context).ConfigAwait();
        return (toolCallId, content, resources);
    }

    /// <summary>Execute a registered tool by name (port of g_exec_tool). Errors become the result text.</summary>
    public async Task<(string Content, List<JsonObject> Resources)> ExecToolAsync(
        string fnName, JsonObject args, ChatContext context)
    {
        var tool = Tools.GetTool(fnName);
        if (tool == null)
            return ($"Error: Tool '{fnName}' not found", []);

        try
        {
            // tools declaring a "user" param receive the authenticated username
            if (context.User != null && tool.Definition.GetObject("function").GetObject("parameters")
                    .GetObject("properties")?.ContainsKey("user") == true)
            {
                args["user"] = context.User;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
            cts.CancelAfter(ToolsConfig.ToolTimeout);
            var toolContext = new ChatContext
            {
                Chat = context.Chat,
                User = context.User,
                Request = context.Request,
                ThreadId = context.ThreadId,
                Tools = context.Tools,
                Provider = context.Provider,
                CancellationToken = cts.Token,
            };

            Log.LogInformation("Executing tool '{Tool}'", fnName);
            var result = await tool.Handler(args, toolContext).ConfigAwait();
            return ToToolResult(result, fnName, args, context);
        }
        catch (Exception e)
        {
            return ($"Error executing tool '{fnName}':\n{ChatJson.ToErrorMessage(e)}", []);
        }
    }

    /// <summary>
    /// Convert a tool result to message content + resource parts, caching any returned
    /// image/audio/file data (port of g_tool_result/tool_result_part).
    /// </summary>
    (string Content, List<JsonObject> Resources) ToToolResult(object? result, string fnName, JsonObject args, ChatContext context)
    {
        var contents = new List<string>();
        var resources = new List<JsonObject>();

        void AddPart(JsonNode? node)
        {
            if (node is JsonObject obj && obj.GetString("type") is { } type
                && type is "text" or "image" or "audio" or "file")
            {
                var (text, resource) = ToolResultPart(obj, type, fnName, args, context);
                if (text != null) contents.Add(text);
                if (resource != null) resources.Add(resource);
            }
            else if (node != null)
            {
                contents.Add(node is JsonValue v && v.TryGetValue<string>(out var s)
                    ? s
                    : node.ToJsonString(ChatJson.Options));
            }
        }

        switch (result)
        {
            case null:
                break;
            case string str:
                contents.Add(str);
                break;
            case JsonArray arr:
                foreach (var item in arr)
                    AddPart(item);
                break;
            case JsonNode node:
                AddPart(node);
                break;
            default:
                contents.Add(ChatJson.Serialize(result));
                break;
        }

        return (string.Join("\n", contents), resources);
    }

    (string? Text, JsonObject? Resource) ToolResultPart(JsonObject result, string type, string fnName,
        JsonObject args, ChatContext context)
    {
        var prompt = args.GetString("prompt") ?? args.GetString("text") ?? args.GetString("message");

        if (type == "text")
            return (result.GetString("text"), null);

        var base64Data = result.GetString("data");
        if (base64Data == null)
        {
            Log.LogDebug("{Type} data not found for {Tool}", type, fnName);
            return (null, null);
        }

        var format = result.GetString("format") ?? args.GetString("format")
            ?? type switch { "image" => "png", "audio" => "mp3", _ => "txt" };
        var filename = result.GetString("filename") ?? args.GetString("filename")
            ?? result.GetString("name") ?? args.GetString("name")
            ?? $"{fnName}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.{format}";
        var mimeType = MimeTypes.GetMimeType(filename);

        var extraInfo = new JsonObject();
        if (prompt != null) extraInfo["prompt"] = prompt;
        if (args.GetString("model") is { } argModel) extraInfo["model"] = argModel;
        if (type == "image" && args.GetString("aspect_ratio") is { } aspectRatio)
            extraInfo["aspect_ratio"] = aspectRatio;

        var bytes = Convert.FromBase64String(base64Data);
        var info = SaveToCache(bytes, filename, mimeType, context.User, extraInfo);
        var url = info.GetString("url")!;
        var label = prompt ?? filename;

        return type switch
        {
            "image" => ($"![{label}]({url})\n",
                new JsonObject { ["type"] = "image_url", ["image_url"] = new JsonObject { ["url"] = url } }),
            "audio" => ($"[{label}]({url})\n",
                new JsonObject { ["type"] = "audio_url", ["audio_url"] = new JsonObject { ["url"] = url } }),
            _ => ($"[{label}]({url})\n",
                new JsonObject
                {
                    ["type"] = "file",
                    ["file"] = new JsonObject
                    {
                        ["file_data"] = url,
                        ["filename"] = info.GetString("name") ?? filename,
                    }
                }),
        };
    }

    /// <summary>Group resource parts by kind: images/audios/files/texts/others (port of group_resources)</summary>
    public static Dictionary<string, JsonArray> GroupResources(List<JsonObject> resources)
    {
        var grouped = new Dictionary<string, JsonArray>();
        foreach (var res in resources)
        {
            var type = res.GetString("type");
            if (type == null)
                continue;
            var group = type switch
            {
                "image_url" => "images",
                "audio_url" => "audios",
                "file" or "file_urls" => "files",
                "text" => "texts",
                _ => "others",
            };
            grouped.GetOrAdd(group, _ => []).Add(res.Clone());
        }
        return grouped;
    }
}
