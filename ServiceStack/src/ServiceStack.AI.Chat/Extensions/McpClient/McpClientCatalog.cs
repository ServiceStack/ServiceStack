using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace ServiceStack.AI;

internal sealed record McpClientTool(string Name, string Alias, string Description, JsonObject Schema,
    JsonObject? OutputSchema, string SchemaHash);

internal static class McpClientCatalog
{
    internal static async Task<IReadOnlyList<McpClientTool>> DiscoverAsync(IMcpClientSession session,
        McpClientServer server, McpClientLimits limits, CancellationToken token)
    {
        var tools = new List<McpClientTool>();
        var cursors = new HashSet<string>(StringComparer.Ordinal);
        var names = new HashSet<string>(StringComparer.Ordinal);
        var bytes = 0;
        string? cursor = null;
        for (var page = 0; page < limits.MaxPages; page++) {
            var result = await session.ListToolsAsync(cursor, token).ConfigureAwait(false);
            bytes += Encoding.UTF8.GetByteCount(result.ToJsonString());
            if (bytes > limits.MaxCatalogBytes) throw new McpClientException("catalog_limit", "Catalog is too large");
            foreach (var item in result["tools"]?.AsArray().OfType<JsonObject>() ?? []) {
                token.ThrowIfCancellationRequested();
                var name = item["name"]?.GetValue<string>() ?? "";
                if (name.Length is 0 or > 512 || !names.Add(name))
                    throw new McpClientException("invalid_catalog", "Catalog contains invalid or duplicate tool names");
                if (names.Count > limits.MaxTools) throw new McpClientException("catalog_limit", "Too many tools");
                if (!server.Allows(name)) continue;
                var schema = item["inputSchema"] as JsonObject ?? throw new McpClientException("invalid_schema", "Missing input schema");
                var output = item["outputSchema"] as JsonObject;
                McpClientSchema.Check(schema, limits.MaxSchemaBytes);
                if (output != null) McpClientSchema.Check(output, limits.MaxSchemaBytes);
                var description = item["description"]?.GetValue<string>() ?? name;
                if (description.Length > 4096) description = description[..4096];
                tools.Add(new(name, McpClientHash.Alias(server.Id, name), description, (JsonObject)schema.DeepClone(),
                    (JsonObject?)output?.DeepClone(), McpClientHash.Of(schema.ToJsonString() + "\n" + output?.ToJsonString())));
            }
            cursor = result["nextCursor"]?.GetValue<string>();
            if (string.IsNullOrEmpty(cursor)) return tools.OrderBy(x => x.Alias, StringComparer.Ordinal).ToArray();
            if (!cursors.Add(cursor)) throw new McpClientException("invalid_catalog", "Catalog cursor repeated");
        }
        throw new McpClientException("catalog_limit", "Too many catalog pages");
    }
}

internal static class McpClientSchema
{
    internal static bool GoogleCompatible(JsonObject schema)
    {
        static JsonNode? Normalize(JsonNode? node) {
            if (node is JsonObject obj) {
                var result = new JsonObject();
                foreach (var (key, value) in obj) {
                    if (key == "$schema" || key == "additionalProperties" && value is JsonValue flag && flag.TryGetValue<bool>(out var allowed) && allowed) continue;
                    result[key] = Normalize(value);
                }
                return result;
            }
            return node is JsonArray array ? new JsonArray(array.Select(Normalize).ToArray()) : node?.DeepClone();
        }
        return JsonNode.DeepEquals(Normalize(schema), Normalize(GoogleProvider.SanitizeParameters(schema)));
    }
    internal static void Check(JsonObject schema, int maxBytes)
    {
        if (Encoding.UTF8.GetByteCount(schema.ToJsonString()) > maxBytes)
            throw new McpClientException("schema_limit", "Schema exceeds the configured limit");
        Visit(schema, 0);
        var visited = 0;
        CheckReferences(schema, schema, new HashSet<JsonNode>(ReferenceEqualityComparer.Instance), 0, ref visited);
        _ = Build(schema);
    }
    static void CheckReferences(JsonNode? node, JsonObject root, HashSet<JsonNode> path, int depth, ref int visited)
    {
        if (node == null) return;
        if (++visited > 8192 || depth > 64 || !path.Add(node)) throw new McpClientException("unsupported_schema", "Recursive schema references are not supported");
        try {
            if (node is JsonObject obj) {
                if (obj["$ref"] is JsonValue reference) {
                    JsonNode? target = root;
                    foreach (var segment in reference.GetValue<string>()[2..].Split('/'))
                        target = (target as JsonObject)?[segment.Replace("~1", "/").Replace("~0", "~")];
                    if (target == null) throw new McpClientException("unsupported_schema", "Unresolved schema reference");
                    CheckReferences(target, root, path, depth + 1, ref visited);
                }
                foreach (var child in obj) CheckReferences(child.Value, root, path, depth + 1, ref visited);
            } else if (node is JsonArray array) foreach (var child in array) CheckReferences(child, root, path, depth + 1, ref visited);
        } finally { path.Remove(node); }
    }
    static void Visit(JsonNode? node, int depth)
    {
        if (depth > 32) throw new McpClientException("schema_limit", "Schema is too deeply nested");
        if (node is JsonObject obj) {
            foreach (var (key, value) in obj) {
                if (key is "$ref" && value?.GetValue<string>() is { } reference && !reference.StartsWith("#/"))
                    throw new McpClientException("unsupported_schema", "Only local JSON Pointer references are supported");
                // Reject recursive/dynamic and regular-expression schemas until bounded evaluation is available.
                if (key is "$dynamicRef" or "$recursiveRef" or "pattern" or "patternProperties" or "$id")
                    throw new McpClientException("unsupported_schema", "Schema uses an unsupported keyword");
                Visit(value, depth + 1);
            }
        } else if (node is JsonArray array) foreach (var value in array) Visit(value, depth + 1);
    }
    static JsonSchema Build(JsonObject schema) => JsonSchema.FromText(schema.ToJsonString(),
        // MCP schemas default to Draft 2020-12. JsonSchema.Net's newer default dialect
        // rejects extension annotations such as GitHub's x-mcp-header.
        new BuildOptions { Dialect = Dialect.Draft202012, SchemaRegistry = new SchemaRegistry { Fetch = (_, _) => null } });
    internal static void Validate(JsonObject schema, JsonNode? value, int maxBytes)
    {
        var json = value?.ToJsonString() ?? "null";
        if (Encoding.UTF8.GetByteCount(json) > maxBytes) throw new McpClientException("argument_limit", "Payload too large");
        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 });
        if (!Build(schema).Evaluate(doc.RootElement).IsValid)
            throw new McpClientException("schema_validation", "Payload does not match the tool schema");
    }
}
