using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>
/// A detached provider-native assistant replay projection. Filters mutate Content in place;
/// identity and user-turn boundaries are fixed. No user prompt means all replay is current.
/// </summary>
public sealed class ProviderReplayContext(ChatProvider provider, string model, int messageIndex,
    int lastUserMessageIndex, JsonObject content)
{
    public ChatProvider Provider { get; } = provider;
    public string Model { get; } = model;
    public int MessageIndex { get; } = messageIndex;
    public int LastUserMessageIndex { get; } = lastUserMessageIndex;
    public bool IsCurrentTurn => MessageIndex >= LastUserMessageIndex;
    public JsonObject Content { get; } = content;
}

/// <summary>Optional Gemini replay customizations. None are enabled by default.</summary>
public static class GeminiReplayFilters
{
    /// <summary>
    /// Omit completed turns' built-in tool steps while keeping other signed parts and the current
    /// tool loop intact. This workaround differs from Google's documented full replay contract
    /// and does not guarantee citations. Stored history is never modified.
    /// </summary>
    public static void ExcludeCompletedTurnToolInvocations(ProviderReplayContext replay)
    {
        if (replay.Provider is not GoogleProvider || replay.IsCurrentTurn) return;
        if (replay.Content.GetArray("parts") is not { } parts) return;
        for (var i = parts.Count - 1; i >= 0; i--)
        {
            if (parts[i] is JsonObject part && (part.ContainsKey("toolCall") || part.ContainsKey("toolResponse")))
                parts.RemoveAt(i);
        }
    }
}
