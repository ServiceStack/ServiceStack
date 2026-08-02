using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// AI template editing: ask the selected model to rewrite the template (and its data files) to
/// satisfy a request, then compile what came back and give the model one shot at fixing its own
/// output before returning it.
/// </summary>
public partial class PdfExtension
{
    /// <summary>Attachments: screenshots or rasterised PDF pages the model reads to build a template from</summary>
    public int MaxImages { get; set; } = 8;
    public int MaxImageBytes { get; set; } = 20 * 1024 * 1024;

    [GeneratedRegex(@"^data:image/(png|jpeg|jpg|webp|gif);base64,[A-Za-z0-9+/=\s]+$")]
    private static partial Regex ImageDataUrlRegex();

    /// <summary>```typst path=invoice.typ blocks returned by the AI edit prompt</summary>
    [GeneratedRegex(@"path\s*=\s*[""']?([^\s""']+)")]
    private static partial Regex BlockPathRegex();

    /// <summary>Fenced languages a model is likely to tag a block with, and the extension they imply</summary>
    static readonly Dictionary<string, string> BlockLangs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["typst"] = ".typ",
        ["typ"] = ".typ",
        ["json"] = ".json",
        ["yaml"] = ".yaml",
        ["yml"] = ".yml",
        ["toml"] = ".toml",
        ["csv"] = ".csv",
        ["xml"] = ".xml",
    };

    /// <summary>Pull code blocks out of the model's answer, keyed by relative path</summary>
    public static Dictionary<string, string> ParseEditedFiles(string? answer, string defaultPath,
        IEnumerable<string>? knownPaths = null)
    {
        var edits = new Dictionary<string, string>();
        var known = knownPaths?.ToList() ?? [];
        var blocks = ModelPrompt.CodeBlockRegex().Matches(answer ?? "");

        foreach (Match block in blocks)
        {
            var info = block.Groups[1].Value;
            var content = block.Groups[2].Value;

            var match = BlockPathRegex().Match(info);
            string? path = match.Success ? match.Groups[1].Value : null;

            if (path == null)
            {
                // tolerate ```typst invoice.typ
                path = info.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(word => word.Contains('.') && !word.StartsWith('.'));
            }

            if (path == null)
            {
                // untagged block: fall back to the file we sent whose extension matches the fence
                // language, so a bare ```typst block is still recognised as an edit to the template
                var lang = info.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                var ext = BlockLangs.GetValueOrDefault(lang);
                if (ext == ".typ")
                    path = !edits.ContainsKey(defaultPath) ? defaultPath : null;
                else if (ext != null)
                    path = known.FirstOrDefault(p => p.EndsWith(ext, StringComparison.OrdinalIgnoreCase)
                                                     && !edits.ContainsKey(p));
                else if (blocks.Count == 1)
                    path = defaultPath;
            }

            if (path != null)
                edits[path.Replace('\\', '/').TrimStart('/')] = content;
        }
        return edits;
    }

    /// <summary>Data URLs for the screenshots (and rasterised PDF pages) the user attached</summary>
    List<string> ReadAttachments(JsonObject body)
    {
        if (body["images"] is null)
            return [];
        if (body["images"] is not JsonArray images)
            throw new ArgumentException("'images' must be a list of data URLs");
        if (images.Count > MaxImages)
            throw new ArgumentException($"Too many attachments: {images.Count}, at most {MaxImages}");

        var to = new List<string>();
        var total = 0;
        foreach (var image in images)
        {
            var url = (image as JsonValue)?.TryGetValue<string>(out var s) == true ? s : null;
            if (url == null || !ImageDataUrlRegex().IsMatch(url))
                throw new ArgumentException("Attachments must be image data URLs");
            total += url.Length;
            to.Add(url);
        }
        if (total > MaxImageBytes)
            throw new ArgumentException(
                $"Attachments are too large: {total / 1024}KB, at most {MaxImageBytes / 1024}KB");
        return to;
    }

    async Task<object?> AiEditAsync(ChatRequestContext req)
    {
        var user = req.AssertUserName();
        var root = PdfRoot(user);
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var relPath = body.GetString("path");
        var prompt = (body.GetString("prompt") ?? "").Trim();
        var model = body.GetString("model");
        var files = body.GetObject("files") ?? new JsonObject();
        var images = ReadAttachments(body);

        if (prompt.Length == 0)
            throw new ArgumentException("Missing 'prompt'");
        if (string.IsNullOrEmpty(model))
            throw new ArgumentException("No model selected");
        Resolve(root, relPath);
        ModelPrompt.AssertTextModel(Feature, model!, " to edit templates");
        if (images.Count > 0)
            ModelPrompt.AssertImageModel(Feature, model!);

        var systemPrompt = Ctx.GetBundledText("prompts/edit-template.md")
            ?? throw new Exception("Missing prompts/edit-template.md");

        var sb = StringBuilderCache.Allocate();
        sb.AppendLine($"Template: `{relPath}`").AppendLine();
        foreach (var entry in files)
        {
            Resolve(root, entry.Key);
            var lang = entry.Key.EndsWith(".typ", StringComparison.OrdinalIgnoreCase)
                ? "typst"
                : Path.GetExtension(entry.Key).TrimStart('.');
            sb.AppendLine($"```{lang} path={entry.Key}");
            sb.AppendLine((entry.Value as JsonValue)?.TryGetValue<string>(out var content) == true ? content : "");
            sb.AppendLine("```").AppendLine();
        }
        if (images.Count > 0)
        {
            var noun = images.Count == 1 ? "screenshot" : "screenshots/pages";
            sb.AppendLine($"The user attached {images.Count} {noun} of the document they want. Reproduce that " +
                          "layout in the template and put any data you can read from it in the data file.")
                .AppendLine();
        }
        sb.AppendLine($"Requested change:\n{prompt}");
        var text = StringBuilderCache.ReturnAndFree(sb);

        // OpenAI style content parts — the pipeline normalises these for every provider
        JsonNode content2 = images.Count > 0
            ? new JsonArray([
                new JsonObject { ["type"] = "text", ["text"] = text },
                ..images.Select(image => (JsonNode)new JsonObject
                {
                    ["type"] = "image_url",
                    ["image_url"] = new JsonObject { ["url"] = image },
                }),
            ])
            : text;

        var messages = ModelPrompt.Messages(systemPrompt, content2);

        async Task<(string Answer, Dictionary<string, string> Edits, JsonNode? Usage)> AskAsync()
        {
            var (answer, usage) = await ModelPrompt
                .AskAsync(Feature, user, model!, messages, req.Request).ConfigAwait();
            var edits = new Dictionary<string, string>();
            foreach (var edit in ParseEditedFiles(answer, relPath!, files.Select(x => x.Key)))
            {
                Resolve(root, edit.Key); // never let the model write outside the templates folder
                edits[edit.Key] = edit.Value;
            }
            return (answer, edits, usage);
        }

        var (answer, edits, usage) = await AskAsync().ConfigAwait();

        // compile what came back and, if typst rejects it, give the model one shot at fixing its own output
        var diagnostics = new List<JsonObject>();
        var repaired = false;
        if (edits.Count > 0)
        {
            byte[]? pdf;
            (pdf, diagnostics) = await CompilePdfAsync(user, relPath, Overlay(files, edits)).ConfigAwait();
            if (pdf == null)
            {
                var errors = string.Join('\n', diagnostics
                    .Where(x => x.GetString("severity") == "error")
                    .Select(x => $"{x.GetString("file") ?? relPath}:{x.GetInt("line")?.ToString() ?? "?"}:" +
                                 $"{x.GetInt("col")?.ToString() ?? "?"}: {x.GetString("message")}"));
                Log.LogInformation("AI edit failed to compile, asking {Model} to fix:\n{Errors}", model, errors);

                messages.Add(new JsonObject { ["role"] = "assistant", ["content"] = answer });
                messages.Add(new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = "That does not compile. typst reports:\n\n"
                                  + errors + "\n\n"
                                  + "Remember: inside a function's argument list you are already in code, so no "
                                  + "`#` prefix, and content is passed positionally. Fix it and return the "
                                  + "complete corrected files in `path=` tagged code blocks as before.",
                });

                var (retryAnswer, retryEdits, retryUsage) = await AskAsync().ConfigAwait();
                if (retryEdits.Count > 0)
                {
                    var merged = new Dictionary<string, string>(edits);
                    foreach (var entry in retryEdits)
                        merged[entry.Key] = entry.Value;

                    var (retryPdf, retryDiagnostics) = await CompilePdfAsync(user, relPath, Overlay(files, merged))
                        .ConfigAwait();
                    if (retryPdf != null || retryDiagnostics.Count < diagnostics.Count)
                    {
                        (answer, edits, diagnostics, repaired) = (retryAnswer, merged, retryDiagnostics, true);
                        usage = retryUsage;
                    }
                }
            }
        }

        var editsJson = new JsonObject();
        foreach (var entry in edits)
            editsJson[entry.Key] = entry.Value;

        return new JsonObject
        {
            ["files"] = editsJson,
            ["message"] = ModelPrompt.StripCodeBlocks(answer),
            ["model"] = model,
            ["usage"] = usage,
            ["repaired"] = repaired,
            ["diagnostics"] = new JsonArray(diagnostics
                .Where(x => x.GetString("severity") == "error")
                .Select(x => (JsonNode)x.Clone()).ToArray()),
        };
    }

    /// <summary>The unsaved buffers plus the model's edits, which win where they overlap</summary>
    static JsonObject Overlay(JsonObject files, Dictionary<string, string> edits)
    {
        var to = files.Clone();
        foreach (var entry in edits)
            to[entry.Key] = entry.Value;
        return to;
    }
}
