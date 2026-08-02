using System.Text.Json.Nodes;
using ServiceStack.Text;
using System.Text.RegularExpressions;
using ServiceStack.Web;

namespace ServiceStack.AI;

/// <summary>
/// Asking a model to write something for an extension, rather than for a conversation: the pdf
/// designer's template edits and core_tools' JSON Schema generation both send a one-shot prompt
/// that never touches the user's chat history, then read code blocks back out of the answer.
/// </summary>
public static partial class ModelPrompt
{
    /// <summary>A ``` fenced block: its info string and its content</summary>
    [GeneratedRegex(@"```([^\n`]*)\n(.*?)[ \t]*```", RegexOptions.Singleline)]
    public static partial Regex CodeBlockRegex();

    /// <summary>The first fenced block, or the whole answer when the model didn't use one</summary>
    public static string FirstCodeBlock(string? answer)
    {
        var match = CodeBlockRegex().Match(answer ?? "");
        return (match.Success ? match.Groups[2].Value : answer ?? "").Trim();
    }

    public static string StripCodeBlocks(string? answer) => CodeBlockRegex().Replace(answer ?? "", "").Trim();

    /// <summary>The model's metadata, from whichever provider serves it</summary>
    public static JsonObject? FindModel(ChatFeature feature, string modelId)
    {
        foreach (var provider in feature.Providers.Values)
        {
            if (provider.ModelInfo(modelId) is { } info)
                return info;
        }
        return null;
    }

    /// <summary>Codegen needs a model that answers with text, not an image/audio generation model</summary>
    public static void AssertTextModel(ChatFeature feature, string modelId, string use)
    {
        // unknown to us (custom/proxied model), let the provider decide
        if (Modalities(feature, modelId, "output") is not { Count: > 0 } output)
            return;
        if (!output.Contains("text"))
            throw new ArgumentException($"'{modelId}' outputs {string.Join('/', output)}, not text. " +
                                        $"Select a text model{use}.");
    }

    /// <summary>Attachments are sent as images, so the model has to be able to see them</summary>
    public static void AssertImageModel(ChatFeature feature, string modelId)
    {
        if (Modalities(feature, modelId, "input") is not { Count: > 0 } input)
            return;
        if (!input.Contains("image"))
            throw new ArgumentException($"'{modelId}' accepts {string.Join('/', input)}, not images. " +
                                        "Select a vision model to use attachments.");
    }

    static List<string>? Modalities(ChatFeature feature, string modelId, string direction) =>
        FindModel(feature, modelId)?.GetObject("modalities")?.GetArray(direction)
            ?.Select(x => x?.ToString()).Where(x => x != null).ToList()!;

    /// <summary>
    /// One-shot completion outside the user's chat history: no tools, nothing stored, nothing
    /// added to a thread — the same context llms-py passes for its own internal prompts.
    /// </summary>
    public static async Task<(string Answer, JsonNode? Usage)> AskAsync(ChatFeature feature, string? user,
        string model, JsonArray messages, IRequest? request = null, CancellationToken token = default)
    {
        var chat = new JsonObject
        {
            ["model"] = model,
            ["messages"] = messages.Clone(),
        };
        var context = new ChatContext
        {
            User = user,
            Request = request,
            Tools = "none",
            NoStore = true,
            NoHistory = true,
            CancellationToken = token,
        };

        var response = await feature.ChatCompletionAsync(chat, context).ConfigAwait();
        var answer = (response.GetArray("choices")?.FirstOrDefault() as JsonObject)
            .GetObject("message").GetString("content") ?? "";
        if (string.IsNullOrWhiteSpace(answer))
            throw new Exception("The model returned an empty response");
        return (answer, response.GetObject("usage")?.Clone());
    }

    /// <summary>A system + user message pair, the shape both codegen prompts use</summary>
    public static JsonArray Messages(string systemPrompt, JsonNode userContent) =>
    [
        new JsonObject { ["role"] = "system", ["content"] = systemPrompt },
        new JsonObject { ["role"] = "user", ["content"] = userContent },
    ];
}
