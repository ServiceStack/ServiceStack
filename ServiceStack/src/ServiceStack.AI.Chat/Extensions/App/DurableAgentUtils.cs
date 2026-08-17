using System.Text.Json.Nodes;

namespace ServiceStack.AI;

public static class DurableAgentUtils
{
    public static long CountTokensApprox(JsonNode? value)
    {
        if (value == null) return 0;
        // A deliberately conservative provider-neutral estimate. Exact tokenizers remain provider work.
        var chars = value.ToJsonString(ChatJson.Options).Length;
        return Math.Max(1, (chars + 2) / 3);
    }

    public static JsonArray CloneMessages(IEnumerable<JsonObject> messages) =>
        new(messages.Select(x => (JsonNode)x.Clone()).ToArray());
}
