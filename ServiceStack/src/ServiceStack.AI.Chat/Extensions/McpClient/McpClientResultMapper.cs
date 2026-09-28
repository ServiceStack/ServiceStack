using System.Text;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

internal static class McpClientResultMapper
{
    internal static JsonObject Map(JsonObject response, McpClientTool tool, McpClientServer server, McpClientLimits limits)
    {
        if (Encoding.UTF8.GetByteCount(response.ToJsonString()) > limits.MaxResponseBytes)
            throw new McpClientException("response_limit", "Remote response exceeds the configured limit");
        if (response.ContainsKey("inputRequests") || response.ContainsKey("requestState"))
            throw new McpClientException("unsupported_operation", "Server requested an unsupported client interaction");
        var structured = response["structuredContent"];
        if (tool.OutputSchema != null && response["isError"]?.GetValue<bool>() != true)
            McpClientSchema.Validate(tool.OutputSchema, structured, limits.MaxResponseBytes);
        var content = new JsonArray();
        var resources = new JsonArray();
        foreach (var block in response["content"]?.AsArray().OfType<JsonObject>() ?? []) {
            var type = block["type"]?.GetValue<string>();
            switch (type) {
                case "text":
                    var text = block["text"]?.GetValue<string>() ?? "";
                    if (structured != null) {
                        try { if (JsonNode.DeepEquals(JsonNode.Parse(text), structured)) break; } catch { }
                    }
                    content.Add(new JsonObject { ["type"] = "text", ["text"] = text }); break;
                case "resource_link":
                    content.Add(new JsonObject { ["type"] = "reference", ["name"] = block["name"]?.DeepClone(), ["uri"] = block["uri"]?.DeepClone() }); break;
                case "resource" when (block["resource"] as JsonObject)?.ContainsKey("text") == true:
                    content.Add(new JsonObject { ["type"] = "text", ["text"] = block["resource"]!["text"]!.DeepClone(), ["uri"] = block["resource"]!["uri"]?.DeepClone() }); break;
                case "image":
                case "audio":
                    AddMedia(block, content, resources, limits); break;
                case "resource" when (block["resource"] as JsonObject)?.ContainsKey("blob") == true:
                    var embedded = block["resource"]!.AsObject();
                    AddMedia(new JsonObject { ["mimeType"] = embedded["mimeType"]?.DeepClone(),
                        ["data"] = embedded["blob"]?.DeepClone() }, content, resources, limits); break;
                default:
                    // Unsupported blocks are visible, never silently converted to success.
                    content.Add(new JsonObject { ["type"] = "unsupported", ["contentType"] = type,
                        ["message"] = "Unsupported remote content type" }); break;
            }
        }
        return new JsonObject {
            ["source"] = "mcp_client", ["serverId"] = server.Id, ["tool"] = tool.Name,
            ["isError"] = response["isError"]?.DeepClone() ?? JsonValue.Create(false),
            ["structuredContent"] = structured?.DeepClone(), ["content"] = content, ["resources"] = resources,
        };
    }
    static void AddMedia(JsonObject block, JsonArray content, JsonArray resources, McpClientLimits limits)
    {
        var mime = block["mimeType"]?.GetValue<string>() ?? "";
        if (mime is not ("image/png" or "image/jpeg" or "image/gif" or "image/webp" or "audio/mpeg" or "audio/wav" or "audio/ogg")) {
            content.Add(new JsonObject { ["type"] = "unsupported", ["message"] = "Unsupported media MIME type" }); return;
        }
        var encoded = block["data"]?.GetValue<string>() ?? "";
        if (encoded.Length > limits.MaxResponseBytes) throw new McpClientException("response_limit", "Encoded media is too large");
        byte[] bytes;
        try { bytes = Convert.FromBase64String(encoded); }
        catch { throw new McpClientException("invalid_media", "Invalid media encoding"); }
        if (bytes.Length > limits.MaxResponseBytes || bytes.Length < 4)
            throw new McpClientException("invalid_media", "Invalid media size");
        var valid = mime switch {
            "image/png" => bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            "image/jpeg" => bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255,
            "image/gif" => bytes.AsSpan().StartsWith("GIF8"u8),
            "image/webp" => bytes.Length >= 12 && bytes.AsSpan().StartsWith("RIFF"u8) && bytes.AsSpan(8).StartsWith("WEBP"u8),
            "audio/wav" => bytes.Length >= 12 && bytes.AsSpan().StartsWith("RIFF"u8) && bytes.AsSpan(8).StartsWith("WAVE"u8),
            "audio/ogg" => bytes.AsSpan().StartsWith("OggS"u8),
            "audio/mpeg" => bytes.AsSpan().StartsWith("ID3"u8) || bytes[0] == 255 && (bytes[1] & 0xe0) == 0xe0,
            _ => false,
        };
        if (!valid) throw new McpClientException("invalid_media", "Media signature does not match MIME type");
        var type = mime.StartsWith("image/", StringComparison.Ordinal) ? "image_url" : "audio_url";
        // Inline media lives only in the owned conversation/result. Never publish it in ~cache.
        resources.Add(new JsonObject { ["type"] = type, [type] = new JsonObject { ["url"] = "data:" + mime + ";base64," + Convert.ToBase64String(bytes) } });
        content.Add(new JsonObject { ["type"] = "media", ["mimeType"] = mime, ["resourceIndex"] = resources.Count - 1 });
    }
}
