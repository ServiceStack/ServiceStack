using System.Text;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>
/// Merges Gemini grounding payloads from streamed responses and validates their UTF-8 byte spans
/// against the final assistant text. Source chunks are retained even when a span cannot safely be
/// rendered as an inline citation.
/// </summary>
public static class GeminiGrounding
{
    static string ChunkKey(JsonObject chunk)
    {
        var source = chunk.GetObject("retrievedContext")
            ?? chunk.GetObject("web")
            ?? chunk.GetObject("maps")
            ?? new JsonObject();
        var text = source.GetString("text") ?? "";
        if (text.Length > 200)
            text = text[..200];
        return string.Join('\u001f',
            source.GetString("uri") ?? "",
            source.GetString("title") ?? "",
            source.GetString("documentName") ?? "",
            source.GetString("fileSearchStore") ?? "",
            text);
    }

    /// <summary>Union one candidate's grounding metadata into an accumulator.</summary>
    public static JsonObject? Merge(JsonObject? accumulator, JsonObject? metadata)
    {
        if (metadata == null)
            return accumulator;

        accumulator ??= new JsonObject
        {
            ["groundingChunks"] = new JsonArray(),
            ["groundingSupports"] = new JsonArray(),
        };
        var chunks = accumulator.GetArray("groundingChunks") ?? new JsonArray();
        var supports = accumulator.GetArray("groundingSupports") ?? new JsonArray();
        accumulator["groundingChunks"] = chunks;
        accumulator["groundingSupports"] = supports;

        var chunkIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < chunks.Count; i++)
        {
            if (chunks[i] is JsonObject chunk)
                chunkIndexes.TryAdd(ChunkKey(chunk), i);
        }

        var indexMap = new Dictionary<int, int>();
        var incomingChunks = metadata.GetArray("groundingChunks") ?? [];
        for (var i = 0; i < incomingChunks.Count; i++)
        {
            if (incomingChunks[i] is not JsonObject chunk)
                continue;
            var key = ChunkKey(chunk);
            if (!chunkIndexes.TryGetValue(key, out var mergedIndex))
            {
                mergedIndex = chunks.Count;
                chunkIndexes[key] = mergedIndex;
                chunks.Add(chunk.DeepClone());
            }
            indexMap[i] = mergedIndex;
        }

        var supportKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var supportNode in supports)
        {
            if (supportNode is JsonObject support)
                supportKeys.Add(SupportKey(support));
        }

        foreach (var supportNode in metadata.GetArray("groundingSupports") ?? [])
        {
            if (supportNode is not JsonObject support || support.GetObject("segment") is not { } segment)
                continue;
            var end = segment.GetLong("endIndex");
            if (end == null)
                continue;
            var start = segment.GetLong("startIndex") ?? 0;
            var remapped = new JsonArray();
            foreach (var indexNode in support.GetArray("groundingChunkIndices") ?? [])
            {
                if (indexNode is JsonValue value && value.TryGetValue<int>(out var index)
                    && indexMap.TryGetValue(index, out var mergedIndex))
                    remapped.Add(mergedIndex);
            }
            if (remapped.Count == 0)
                continue;

            var merged = new JsonObject
            {
                ["segment"] = new JsonObject
                {
                    ["startIndex"] = start,
                    ["endIndex"] = end.Value,
                    ["text"] = segment["text"]?.DeepClone(),
                },
                ["groundingChunkIndices"] = remapped,
            };
            if (supportKeys.Add(SupportKey(merged)))
                supports.Add(merged);
        }

        foreach (var key in new[] { "webSearchQueries", "searchEntryPoint", "retrievalMetadata" })
        {
            if (metadata[key] is { } value)
                accumulator[key] = value.DeepClone();
        }
        return accumulator;
    }

    static string SupportKey(JsonObject support)
    {
        var segment = support.GetObject("segment");
        var indexes = support.GetArray("groundingChunkIndices")?
            .Select(x => x?.ToJsonString() ?? "") ?? [];
        return $"{segment.GetLong("startIndex") ?? 0}:{segment.GetLong("endIndex") ?? 0}:{string.Join(',', indexes)}";
    }

    /// <summary>Remove spans that cannot be placed in the final answer while retaining sources.</summary>
    public static JsonObject? Finalize(JsonObject? accumulator, string? text, bool trustSpans = true)
    {
        if (accumulator?.GetArray("groundingChunks") is not { Count: > 0 })
            return null;

        var valid = new JsonArray();
        if (trustSpans)
        {
            var maxBytes = Encoding.UTF8.GetByteCount(text ?? "");
            foreach (var supportNode in accumulator.GetArray("groundingSupports") ?? [])
            {
                if (supportNode is not JsonObject support)
                    continue;
                var segment = support.GetObject("segment");
                var start = segment.GetLong("startIndex") ?? 0;
                var end = segment.GetLong("endIndex") ?? 0;
                if (0 <= start && start < end && end <= maxBytes)
                    valid.Add(support.DeepClone());
            }
        }
        accumulator["groundingSupports"] = valid;
        return accumulator;
    }
}
