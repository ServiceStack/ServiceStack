using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>
/// Maps OrmLite POCOs to/from the JSON wire shape the synced llms-py UI expects:
/// camelCase keys, JSON columns parsed to nodes, and dates as sortable strings
/// (the UI string-compares updatedAt when long-polling for thread updates).
/// </summary>
public static class ChatDtos
{
    /// <summary>Marks the in-flight message merged into a thread DTO's messages while it streams</summary>
    public const string StreamingKey = "streaming";

    public static JsonObject ToDto(this ChatThread x)
    {
        var dto = new JsonObject
        {
            ["id"] = x.Id,
            ["user"] = x.User,
            ["createdAt"] = ChatDb.ToDateString(x.CreatedAt),
            ["updatedAt"] = ChatDb.ToDateString(x.UpdatedAt),
            ["title"] = x.Title,
            ["systemPrompt"] = x.SystemPrompt,
            ["model"] = x.Model,
            ["modelInfo"] = ParseJson(x.ModelInfo),
            ["modalities"] = ParseJson(x.Modalities),
            ["messages"] = ParseJson(x.Messages) ?? new JsonArray(),
            ["args"] = ParseJson(x.Args),
            ["tools"] = ParseJson(x.Tools),
            ["toolHistory"] = ParseJson(x.ToolHistory),
            ["cost"] = x.Cost,
            ["inputTokens"] = x.InputTokens,
            ["outputTokens"] = x.OutputTokens,
            ["stats"] = ParseJson(x.Stats),
            ["provider"] = x.Provider,
            ["providerModel"] = x.ProviderModel,
            ["startedAt"] = ChatDb.ToDateNode(x.StartedAt),
            ["completedAt"] = ChatDb.ToDateNode(x.CompletedAt),
            ["metadata"] = ParseJson(x.Metadata),
            ["status"] = x.Status,
            ["error"] = x.Error,
            ["ref"] = x.Ref,
            ["providerResponse"] = ParseJson(x.ProviderResponse),
            ["contextTokens"] = x.ContextTokens,
            ["parentId"] = x.ParentId,
            ["publishedAt"] = ChatDb.ToDateNode(x.PublishedAt),
            ["publishedUrl"] = x.PublishedUrl,
            ["sig"] = x.Sig,
        };

        // The in-flight message is stored separately so a failed stream can't damage `messages`,
        // but clients read one list, so present it merged on the way out.
        if (ParseJson(x.StreamingMessage) is JsonObject streaming)
        {
            var messages = dto.GetArray("messages")!;
            var timestamp = streaming.GetLong("timestamp");
            var role = streaming.GetString("role");
            var alreadyCommitted = timestamp != null && messages.OfType<JsonObject>().Any(message =>
                message.GetLong("timestamp") == timestamp && message.GetString("role") == role);
            if (!alreadyCommitted)
            {
                var message = streaming.Clone();
                message[StreamingKey] = true;
                messages.Add(message);
            }
        }
        return dto;
    }

    /// <summary>
    /// Drop any in-flight message a client echoed back from a merged DTO: it belongs to a stream
    /// still in progress and must never become durable history.
    /// </summary>
    public static JsonArray WithoutStreamingMessages(this JsonArray? messages)
    {
        var ret = new JsonArray();
        foreach (var node in messages ?? [])
        {
            if (node is JsonObject message && message.GetBool(StreamingKey))
                continue;
            ret.Add(node?.DeepClone());
        }
        return ret;
    }

    public static JsonObject ToDto(this ChatRequest x) => new()
    {
        ["id"] = x.Id,
        ["user"] = x.User,
        ["threadId"] = x.ThreadId,
        ["createdAt"] = ChatDb.ToDateString(x.CreatedAt),
        ["updatedAt"] = ChatDb.ToDateString(x.UpdatedAt),
        ["title"] = x.Title,
        ["model"] = x.Model,
        ["duration"] = x.Duration,
        ["cost"] = x.Cost,
        ["inputPrice"] = x.InputPrice,
        ["inputTokens"] = x.InputTokens,
        ["inputCachedTokens"] = x.InputCachedTokens,
        ["outputPrice"] = x.OutputPrice,
        ["outputTokens"] = x.OutputTokens,
        ["totalTokens"] = x.TotalTokens,
        ["usage"] = ParseJson(x.Usage),
        ["provider"] = x.Provider,
        ["providerModel"] = x.ProviderModel,
        ["providerRef"] = x.ProviderRef,
        ["finishReason"] = x.FinishReason,
        ["startedAt"] = ChatDb.ToDateNode(x.StartedAt),
        ["completedAt"] = ChatDb.ToDateNode(x.CompletedAt),
        ["error"] = x.Error,
        ["stackTrace"] = x.StackTrace,
        ["ref"] = x.Ref,
    };

    public static JsonObject ToDto(this ChatMedia x) => new()
    {
        ["id"] = x.Id,
        ["user"] = x.User,
        ["name"] = x.Name,
        ["type"] = x.Type,
        ["prompt"] = x.Prompt,
        ["model"] = x.Model,
        ["created"] = ChatDb.ToDateString(x.Created),
        ["cost"] = x.Cost,
        ["seed"] = x.Seed,
        ["url"] = x.Url,
        ["hash"] = x.Hash,
        ["aspect_ratio"] = x.AspectRatio,
        ["width"] = x.Width,
        ["height"] = x.Height,
        ["size"] = x.Size,
        ["duration"] = x.Duration,
        ["reactions"] = ParseJson(x.Reactions),
        ["caption"] = x.Caption,
        ["description"] = x.Description,
        ["phash"] = x.Phash,
        ["color"] = x.Color,
        ["category"] = ParseJson(x.Category),
        ["tags"] = ParseJson(x.Tags),
        ["rating"] = x.Rating,
        ["ratings"] = ParseJson(x.Ratings),
        ["objects"] = ParseJson(x.Objects),
        ["variantId"] = x.VariantId,
        ["variantName"] = x.VariantName,
        ["publishedAt"] = ChatDb.ToDateNode(x.PublishedAt),
        ["publishedUrl"] = x.PublishedUrl,
        ["metadata"] = ParseJson(x.Metadata),
    };

    public static JsonObject ToDto(this ChatUserSummary x) => new()
    {
        ["user"] = x.User ?? ChatDb.DefaultUser,
        ["requests"] = x.Requests,
        ["cost"] = x.Cost ?? 0,
        ["inputTokens"] = x.InputTokens ?? 0,
        ["outputTokens"] = x.OutputTokens ?? 0,
        ["lastActive"] = ChatDb.ToDateNode(x.LastActive),
    };

    public static JsonArray ToDtos<T>(this IEnumerable<T> rows, Func<T, JsonObject> toDto)
    {
        var arr = new JsonArray();
        foreach (var row in rows)
            arr.Add(toDto(row));
        return arr;
    }

    /// <summary>Apply a partial thread DTO onto a POCO (only keys present are written)</summary>
    public static ChatThread PopulateFrom(this ChatThread to, JsonObject dto)
    {
        if (dto.ContainsKey("title")) to.Title = dto.GetString("title");
        if (dto.ContainsKey("systemPrompt")) to.SystemPrompt = dto.GetString("systemPrompt");
        if (dto.ContainsKey("model")) to.Model = dto.GetString("model");
        if (dto.ContainsKey("modelInfo")) to.ModelInfo = ToJson(dto["modelInfo"]);
        if (dto.ContainsKey("modalities")) to.Modalities = ToJson(dto["modalities"]);
        if (dto.ContainsKey("messages")) to.Messages = ToJson(dto["messages"]);
        if (dto.ContainsKey("streamingMessage")) to.StreamingMessage = ToJson(dto["streamingMessage"]);
        if (dto.ContainsKey("args")) to.Args = ToJson(dto["args"]);
        if (dto.ContainsKey("tools")) to.Tools = ToJson(dto["tools"]);
        if (dto.ContainsKey("toolHistory")) to.ToolHistory = ToJson(dto["toolHistory"]);
        if (dto.ContainsKey("cost")) to.Cost = dto.GetDouble("cost");
        if (dto.ContainsKey("inputTokens")) to.InputTokens = dto.GetLong("inputTokens");
        if (dto.ContainsKey("outputTokens")) to.OutputTokens = dto.GetLong("outputTokens");
        if (dto.ContainsKey("stats")) to.Stats = ToJson(dto["stats"]);
        if (dto.ContainsKey("provider")) to.Provider = dto.GetString("provider");
        if (dto.ContainsKey("providerModel")) to.ProviderModel = dto.GetString("providerModel");
        if (dto.ContainsKey("startedAt")) to.StartedAt = ParseDate(dto["startedAt"]);
        if (dto.ContainsKey("completedAt")) to.CompletedAt = ParseDate(dto["completedAt"]);
        if (dto.ContainsKey("metadata")) to.Metadata = ToJson(dto["metadata"]);
        if (dto.ContainsKey("status")) to.Status = dto.GetString("status");
        if (dto.ContainsKey("error")) to.Error = dto.GetString("error");
        if (dto.ContainsKey("ref")) to.Ref = dto.GetString("ref");
        if (dto.ContainsKey("providerResponse")) to.ProviderResponse = ToJson(dto["providerResponse"]);
        if (dto.ContainsKey("contextTokens")) to.ContextTokens = dto.GetLong("contextTokens");
        if (dto.ContainsKey("parentId")) to.ParentId = dto.GetLong("parentId");
        if (dto.ContainsKey("publishedAt")) to.PublishedAt = ParseDate(dto["publishedAt"]);
        if (dto.ContainsKey("publishedUrl")) to.PublishedUrl = dto.GetString("publishedUrl");
        return to;
    }

    public static JsonNode? ParseJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;
        try
        {
            return JsonNode.Parse(json);
        }
        catch (System.Text.Json.JsonException)
        {
            return json;
        }
    }

    public static string? ToJson(JsonNode? node) => node?.ToJsonString(ChatJson.Options);

    public static DateTime? ParseDate(JsonNode? node)
    {
        if (node is not JsonValue v)
            return null;
        if (v.TryGetValue<DateTime>(out var date))
            return date;
        if (v.TryGetValue<string>(out var s) && DateTime.TryParse(s, out var parsed))
            return parsed;
        return null;
    }
}
