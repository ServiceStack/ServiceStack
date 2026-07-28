using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>
/// Helpers for reading/writing OpenAI chat message shapes, shared by the pipeline, the app extension
/// and the providers/generators (ports of llms-py's chat_to_system_prompt / last_user_prompt /
/// chat_to_aspect_ratio / chat_response_to_message in main.py).
/// </summary>
public static class ChatMessages
{
    /// <summary>The chat's system/developer prompt, if any</summary>
    public static string? ChatToSystemPrompt(JsonObject chat)
    {
        foreach (var messageNode in chat.GetArray("messages") ?? [])
        {
            if (messageNode is not JsonObject message)
                continue;
            var role = message.GetString("role");
            if (role is "system" or "developer")
                return ContentToText(message["content"]);
        }
        return null;
    }

    /// <summary>The most recent user message's text — the prompt image/audio generators render</summary>
    public static string? LastUserPrompt(JsonObject chat)
    {
        var messages = chat.GetArray("messages");
        if (messages == null)
            return null;
        for (var i = messages.Count - 1; i >= 0; i--)
        {
            if (messages[i] is JsonObject message && message.GetString("role") == "user")
                return ContentToText(message["content"]);
        }
        return null;
    }

    /// <summary>Requested image aspect ratio, e.g. "16:9" (port of chat_to_aspect_ratio)</summary>
    public static string? ChatToAspectRatio(JsonObject chat) =>
        chat.GetObject("image_config").GetString("aspect_ratio");

    /// <summary>Message content as text, whether it's a plain string or an array of parts</summary>
    public static string? ContentToText(JsonNode? content)
    {
        if (content is JsonValue v && v.TryGetValue<string>(out var s))
            return s;
        if (content is JsonArray parts)
        {
            foreach (var partNode in parts)
            {
                if (partNode is JsonObject part && part.GetString("type") == "text")
                    return part.GetString("text");
            }
        }
        return null;
    }

    /// <summary>Extract the assistant message from an OpenAI response (port of chat_response_to_message)</summary>
    public static JsonObject ChatResponseToMessage(JsonObject response)
    {
        // the UI renders each message's timestamp, so it must always be set (Python stamps epoch ms)
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (response.GetArray("choices") is { Count: > 0 } choices
            && (choices[0] as JsonObject).GetObject("message") is { } message)
        {
            var msg = message.Clone();
            msg["timestamp"] = timestamp;
            return msg;
        }
        return new JsonObject { ["role"] = "assistant", ["content"] = "", ["timestamp"] = timestamp };
    }
}
