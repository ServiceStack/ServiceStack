using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>Lightweight UI history and reconciliation with server-owned provider replay state.</summary>
internal static class ChatClientHistory
{
    public static JsonObject Message(JsonObject message)
    {
        var result = message.DeepClone().AsObject();
        result.Remove("provider_metadata");
        var values = new List<JsonObject> { result };
        if (result.GetArray("tool_calls") is { } calls)
            values.AddRange(calls.OfType<JsonObject>());
        foreach (var value in values)
        {
            foreach (var obj in new[] { value, value.GetObject("function"), value.GetObject("extra_content")?.GetObject("google") })
            {
                obj?.Remove("thoughtSignature");
                obj?.Remove("thought_signature");
            }
            if (value.GetObject("extra_content") is { } extra && extra.GetObject("google") is { Count: 0 })
            {
                extra.Remove("google");
                if (extra.Count == 0) value.Remove("extra_content");
            }
        }
        return result;
    }

    public static JsonObject Thread(JsonObject thread)
    {
        var result = thread.DeepClone().AsObject();
        foreach (var key in new[] { "messages", "toolHistory" })
            if (result.GetArray(key) is { } messages)
                result[key] = Project(messages);
        if (result.GetObject("streamingMessage") is { } streaming)
            result["streamingMessage"] = Message(streaming);
        if (result.GetObject("providerResponse") is { } response)
        {
            if (response.GetArray("choices") is { } choices)
                foreach (var choice in choices.OfType<JsonObject>())
                    if (choice.GetObject("message") is { } message)
                        choice["message"] = Message(message);
            if (response.GetArray("tool_history") is { } history)
                response["tool_history"] = Project(history);
        }
        return result;
    }

    static JsonArray Project(JsonArray messages) => new(messages.Select(m =>
        m is JsonObject message ? (JsonNode)Message(message) : m?.DeepClone()).ToArray());

    static JsonObject Clean(JsonObject message)
    {
        var result = message.DeepClone().AsObject();
        result.Remove("_sequence");
        result.Remove("streaming");
        return result;
    }

    public static JsonArray Reconcile(IReadOnlyList<JsonObject> stored, JsonArray incoming,
        bool rewrite = false, bool keepPrefix = false)
    {
        var timestamps = new Dictionary<long, int>();
        var sequences = new Dictionary<long, int>();
        for (var i = 0; i < stored.Count; i++)
        {
            if (stored[i].GetLong("timestamp") is { } timestamp) timestamps[timestamp] = i;
            if (stored[i].GetLong("_sequence") is { } sequence) sequences[sequence] = i;
        }
        int? Index(JsonObject message) => message.GetLong("timestamp") is { } timestamp
            ? timestamps.TryGetValue(timestamp, out var i) ? i : null
            : message.GetLong("_sequence") is { } sequence && sequences.TryGetValue(sequence, out var j) ? j : null;

        var messages = incoming.OfType<JsonObject>().Where(m => !m.GetBool("streaming")).ToList();
        if (!rewrite)
        {
            var result = new JsonArray(stored.Select(m => (JsonNode)Clean(m)).ToArray());
            var seen = new HashSet<long>(timestamps.Keys);
            foreach (var message in messages)
            {
                if (Index(message) != null) continue;
                if (message.GetLong("timestamp") is { } timestamp && !seen.Add(timestamp)) continue;
                result.Add(Clean(message));
            }
            return result;
        }
        JsonObject Hydrate(JsonObject message)
        {
            if (Index(message) is not { } position) return Clean(message);
            var original = Clean(stored[position]);
            var visible = Message(original);
            var updates = Clean(Message(message));
            // Omitted fields from older clients are not edits. Explicit changes invalidate
            // old signatures: replay content must match the signed message exactly.
            if (updates.All(entry => JsonNode.DeepEquals(visible[entry.Key], entry.Value))) return original;
            foreach (var entry in updates) visible[entry.Key] = entry.Value?.DeepClone();
            return visible;
        }
        if (keepPrefix)
        {
            var end = messages.Select(Index).Where(i => i != null).Select(i => i!.Value).DefaultIfEmpty(-1).Max();
            var result = new JsonArray(stored.Take(end + 1).Select(m => (JsonNode)Clean(m)).ToArray());
            foreach (var message in messages)
            {
                if (Index(message) is { } position) result[position] = Hydrate(message);
                else result.Add(Hydrate(message));
            }
            return result;
        }
        return new JsonArray(messages.Select(m => (JsonNode)Hydrate(m)).ToArray());
    }
}
