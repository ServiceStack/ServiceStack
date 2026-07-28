using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Message normalization before sending to a provider (port of process_chat, main.py:562-736):
/// reasoning-field normalization for assistant history + resolving image/audio/file content parts
/// (cache urls and http urls → base64 data URIs).
/// Security note: unlike the localhost Python app, raw local file paths in messages are only
/// resolved when inside the feature's resolved AllowedDirectories.
/// </summary>
public partial class OpenAiCompatibleProvider
{
    public virtual async Task<JsonObject> ProcessChatAsync(JsonObject chat, string? providerId = null)
    {
        chat["stream"] ??= false;

        // Some providers don't support empty tools
        if (chat.TryGetPropertyValue("tools", out var tools) && (tools is not JsonArray { Count: > 0 }))
        {
            chat.Remove("tools");
        }
        if (chat.GetArray("messages") is not { } messages)
            return chat;

        NormalizeReasoningFields(chat, messages, providerId);

        foreach (var messageNode in messages)
        {
            if (messageNode is not JsonObject message || !message.TryGetPropertyValue("content", out _))
                continue;

            // convert legacy "images" property to standard content parts
            if (message.GetArray("images") is { } images)
            {
                message.Remove("images");
                var content = new JsonArray();
                if (message.GetString("content") is { } strContent)
                {
                    content.Add(new JsonObject { ["type"] = "text", ["text"] = strContent });
                }
                foreach (var img in images.ToList())
                {
                    content.Add(img?.DeepClone());
                }
                message["content"] = content;
            }

            if (message["content"] is not JsonArray contentParts)
                continue;

            foreach (var partNode in contentParts)
            {
                if (partNode is not JsonObject item)
                    continue;
                var type = item.GetString("type");
                switch (type)
                {
                    case "image_url" when item.GetObject("image_url") is { } imageUrl:
                        await ResolveImageUrlAsync(imageUrl).ConfigAwait();
                        break;
                    case "input_audio" when item.GetObject("input_audio") is { } inputAudio:
                        await ResolveInputAudioAsync(inputAudio, providerId).ConfigAwait();
                        break;
                    case "file" when item.GetObject("file") is { } file:
                        await ResolveFileAsync(file).ConfigAwait();
                        break;
                }
            }
        }

        // strip UI-only message fields (Python: OpenAiCompatible.process_chat cleanup)
        var cleaned = new JsonArray();
        foreach (var messageNode in messages.ToList())
        {
            if (messageNode is JsonObject message)
            {
                var msg = message.Clone();
                msg.Remove("timestamp");
                msg.Remove("model");
                msg.Remove("usage");
                cleaned.Add(msg);
            }
            else
            {
                cleaned.Add(messageNode?.DeepClone());
            }
        }
        chat["messages"] = cleaned;

        if (providerId == "nvidia" || Id == "nvidia")
        {
            chat.Remove("modalities");
        }
        return chat;
    }

    /// <summary>Normalize reasoning/thinking fields on assistant history for the target model</summary>
    void NormalizeReasoningFields(JsonObject chat, JsonArray messages, string? providerId)
    {
        string? expectedField = null;
        var model = chat.GetString("model");
        if (model != null)
        {
            var modelInfo = ModelInfo(model);
            if (modelInfo?.TryGetPropertyValue("interleaved", out var interleaved) == true)
            {
                if (interleaved is JsonObject interleavedObj)
                    expectedField = interleavedObj.GetString("field");
                else if (interleaved is JsonValue v && v.TryGetValue<bool>(out var b) && b)
                    expectedField = "reasoning_content";
            }
            if (expectedField == null)
            {
                var modelLower = model.ToLowerInvariant();
                var providerLower = (providerId ?? Id).ToLowerInvariant();
                if (modelLower.Contains("deepseek") || providerLower.Contains("deepseek"))
                    expectedField = "reasoning_content";
                else if (providerLower.Contains("anthropic") || modelLower.Contains("claude")
                    || providerLower.Contains("minimax") || modelLower.Contains("minimax"))
                    expectedField = "thinking";
            }
        }

        string[] reasoningKeys = ["reasoning_content", "reasoning", "thinking", "reasoning_details"];
        foreach (var messageNode in messages)
        {
            if (messageNode is not JsonObject message || message.GetString("role") != "assistant")
                continue;
            JsonNode? thinkingVal = null;
            foreach (var key in reasoningKeys)
            {
                if (message.TryGetPropertyValue(key, out var val) && val != null)
                {
                    thinkingVal = val.DeepClone();
                    break;
                }
            }
            foreach (var key in reasoningKeys)
            {
                message.Remove(key);
            }
            if (thinkingVal != null && expectedField != null)
            {
                message[expectedField] = thinkingVal;
            }
        }
    }

    async Task ResolveImageUrlAsync(JsonObject imageUrl)
    {
        var url = imageUrl.GetString("url");
        if (url == null)
            return;
        var (content, mimeType) = await ResolveContentAsync(url, "image/png").ConfigAwait();
        if (content == null)
        {
            if (url.StartsWith("data:"))
            {
                // existing data URI: re-process through image conversion if configured
                if (Feature?.ImageTransformer != null && url.IndexOf(";base64,") >= 0)
                {
                    // conversions are host-configurable; leave data URIs as-is by default
                }
                return;
            }
            throw new Exception($"Invalid image: {url.SafeSubstring(0, 100)}");
        }
        imageUrl["url"] = $"data:{mimeType};base64,{Convert.ToBase64String(content)}";
    }

    async Task ResolveInputAudioAsync(JsonObject inputAudio, string? providerId)
    {
        var url = inputAudio.GetString("data");
        if (url == null)
            return;
        if (IsBase64(url))
            return; // use base64 data as-is
        var (content, mimeType) = await ResolveContentAsync(url, "audio/mp3").ConfigAwait();
        if (content == null)
            throw new Exception($"Invalid audio: {url.SafeSubstring(0, 100)}");
        var base64 = Convert.ToBase64String(content);
        inputAudio["data"] = providerId == "alibaba" || Id == "alibaba"
            ? $"data:{mimeType};base64,{base64}"
            : base64;
        inputAudio["format"] = mimeType.LastRightPart('/');
    }

    async Task ResolveFileAsync(JsonObject file)
    {
        var url = file.GetString("file_data");
        if (url == null)
            return;
        if (url.StartsWith("data:"))
        {
            file["filename"] ??= "file";
            return;
        }
        var (content, mimeType, name) = await ResolveContentWithNameAsync(url, "application/pdf").ConfigAwait();
        if (content == null)
            throw new Exception($"Invalid file: {url.SafeSubstring(0, 100)}");
        if (name != null)
            file["filename"] = name;
        file["file_data"] = $"data:{mimeType};base64,{Convert.ToBase64String(content)}";
    }

    async Task<(byte[]? Content, string MimeType)> ResolveContentAsync(string url, string defaultMimeType)
    {
        var (content, mimeType, _) = await ResolveContentWithNameAsync(url, defaultMimeType).ConfigAwait();
        return (content, mimeType);
    }

    /// <summary>Resolve a content ref: /~cache/ path, http(s) URL, or an allowed local file path</summary>
    async Task<(byte[]? Content, string MimeType, string? Name)> ResolveContentWithNameAsync(string url, string defaultMimeType)
    {
        if (url.StartsWith("/~cache/"))
        {
            var cachePath = Feature?.AppData.GetCachePath(url["/~cache/".Length..]);
            if (cachePath != null && File.Exists(cachePath))
            {
                Log.LogInformation("Reading cached file: {Url}", url);
                var infoPath = Path.ChangeExtension(cachePath, null) + ".info.json";
                var info = ChatJson.TryParseObject(File.Exists(infoPath) ? await File.ReadAllTextAsync(infoPath).ConfigAwait() : null);
                var bytes = await File.ReadAllBytesAsync(cachePath).ConfigAwait();
                return (bytes, info.GetString("type") ?? MimeTypes.GetMimeType(cachePath), info.GetString("name"));
            }
            return (null, defaultMimeType, null);
        }

        if (url.StartsWith("http://") || url.StartsWith("https://"))
        {
            Feature?.ValidateDownloadUrl?.Invoke(url);
            Log.LogInformation("Downloading: {Url}", url);
            using var client = CreateHttpClient();
            using var res = await client.GetAsync(url).ConfigAwait();
            res.EnsureSuccessStatusCode();
            var bytes = await res.Content.ReadAsByteArrayAsync().ConfigAwait();
            var mimeType = res.Content.Headers.ContentType?.MediaType ?? defaultMimeType;
            var name = res.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                ?? url.LastRightPart('/').LeftPart('?');
            return (bytes, mimeType, name.IsNullOrEmpty() ? null : name);
        }

        // local file paths only when explicitly allowed by the host
        if ((url.StartsWith('/') || url.StartsWith("~/")) && Feature != null)
        {
            var fullPath = Path.GetFullPath(url.StartsWith("~/")
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), url[2..])
                : url);
            var allowed = Feature.ResolveAllowedDirectories()
                .Any(dir => fullPath.StartsWith(dir, StringComparison.Ordinal));
            if (allowed && File.Exists(fullPath))
            {
                Log.LogInformation("Reading file: {Path}", fullPath);
                var bytes = await File.ReadAllBytesAsync(fullPath).ConfigAwait();
                return (bytes, MimeTypes.GetMimeType(fullPath), Path.GetFileName(fullPath));
            }
        }

        return (null, defaultMimeType, null);
    }

    public static bool IsBase64(string data)
    {
        if (string.IsNullOrEmpty(data) || data.Length % 4 != 0)
            return false;
        var span = data.AsSpan(0, Math.Min(data.Length, 1024));
        foreach (var c in span)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '+' && c != '/' && c != '=')
                return false;
        }
        return true;
    }
}
