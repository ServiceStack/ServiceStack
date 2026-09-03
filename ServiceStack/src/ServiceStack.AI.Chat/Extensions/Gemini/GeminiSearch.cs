using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ServiceStack.AI;

/// <summary>Configuration and heading-aware extraction for the independent local Search feature.</summary>
public static partial class GeminiSearch
{
    public const string IndexVersion = "2";
    public static readonly string[] ScopeFields = ["category", "docType", "status", "locale", "product", "versions", "tags"];
    static readonly HashSet<string> Themes = ["auto", "light", "dark", "nord", "matrix", "soft-pink"];

    static int Bounded(int? value, int fallback, int min, int max) => Math.Clamp(value ?? fallback, min, max);

    public static JsonObject NormalizeConfig(JsonObject? supplied = null)
    {
        supplied ??= new JsonObject();
        var identity = supplied.GetObject("identity") ?? new JsonObject();
        var rawScope = supplied.GetObject("scope") ?? new JsonObject();
        var rawBehavior = supplied.GetObject("behavior") ?? new JsonObject();
        var rawAppearance = supplied.GetObject("appearance") ?? new JsonObject();
        var rawHosting = supplied.GetObject("hosting") ?? new JsonObject();
        var scope = new JsonObject();
        foreach (var field in ScopeFields)
            if (rawScope.GetString(field)?.Trim() is { Length: > 0 } value) scope[field] = value.SafeSubstring(0, 300);
        var theme = rawAppearance.GetString("theme") ?? "auto";
        if (!Themes.Contains(theme)) theme = "auto";
        var highlightColor = rawAppearance.GetString("highlightColor")?.Trim() ?? "";
        if (!Regex.IsMatch(highlightColor, "^#[0-9a-fA-F]{6}$")) highlightColor = "";
        var legacyShortcut = rawBehavior.TryGetPropertyValue("keyboardShortcut", out _)
            ? rawBehavior.GetBool("keyboardShortcut") : (bool?)null;
        var origins = rawHosting.GetArray("allowedOrigins")?.Select(x => x?.GetValue<string>()?.Trim().TrimEnd('/'))
            .Where(x => !string.IsNullOrEmpty(x)).Distinct().Take(100).ToArray() ?? [];
        return new JsonObject
        {
            ["identity"] = new JsonObject
            {
                ["title"] = (identity.GetString("title") ?? "Search documentation").Trim().SafeSubstring(0, 200),
                ["placeholder"] = (identity.GetString("placeholder") ?? "Search docs").Trim().SafeSubstring(0, 120),
                ["emptyText"] = (identity.GetString("emptyText") ?? "No matching documents found.").Trim().SafeSubstring(0, 300),
            },
            ["scope"] = scope,
            ["behavior"] = new JsonObject
            {
                ["commandKShortcut"] = rawBehavior.TryGetPropertyValue("commandKShortcut", out _)
                    ? rawBehavior.GetBool("commandKShortcut") : legacyShortcut ?? true,
                ["slashShortcut"] = rawBehavior.TryGetPropertyValue("slashShortcut", out _)
                    ? rawBehavior.GetBool("slashShortcut") : legacyShortcut ?? true,
                ["minChars"] = Bounded(rawBehavior.GetInt("minChars"), 2, 1, 10),
                ["maxResults"] = Bounded(rawBehavior.GetInt("maxResults"), 30, 5, 100),
                ["groupLimit"] = Bounded(rawBehavior.GetInt("groupLimit"), 8, 1, 30),
            },
            ["appearance"] = new JsonObject
            {
                ["theme"] = theme,
                ["highlightColor"] = highlightColor,
                ["width"] = Bounded(rawAppearance.GetInt("width"), 420, 240, 900),
                ["dialogWidth"] = Bounded(rawAppearance.GetInt("dialogWidth"), 760, 420, 1200),
            },
            ["hosting"] = new JsonObject
            {
                ["allowedOrigins"] = new JsonArray(origins.Select(x => (JsonNode)x!).ToArray()),
                ["requestsPerMinute"] = Bounded(rawHosting.GetInt("requestsPerMinute"), 120, 1, 5000),
            },
        };
    }

    public static JsonObject ValidateConfig(JsonObject? supplied = null)
    {
        var config = NormalizeConfig(supplied);
        foreach (var origin in config.GetObject("hosting")!.GetArray("allowedOrigins")!.Select(x => x!.GetValue<string>()))
        {
            if (origin == "*") continue;
            var wildcard = origin.Contains("*", StringComparison.Ordinal);
            var candidate = wildcard ? origin.Replace("*.", "wildcard.", StringComparison.Ordinal) : origin;
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")
                || uri.PathAndQuery != "/" || !string.IsNullOrEmpty(uri.Fragment)
                || wildcard && (!origin.Contains("://*.", StringComparison.Ordinal) || origin.Count(x => x == '*') != 1))
                throw new ArgumentException($"Invalid allowed origin '{origin}'. Use an exact HTTP(S) origin or a wildcard subdomain.");
        }
        return config;
    }

    public static string NewPublicId() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(18))
        .Replace("+", "").Replace("/", "").Replace("=", "");

    public static string DesiredHash(ChatDocument doc)
    {
        var value = new JsonObject
        {
            ["contentHash"] = doc.ContentHash ?? doc.Hash, ["metadataHash"] = doc.MetadataHash,
            ["displayName"] = doc.DisplayName, ["sourceUrl"] = doc.SourceUrl,
            ["extractorVer"] = doc.ExtractorVer, ["indexVersion"] = IndexVersion,
            ["category"] = doc.Category, ["docType"] = doc.DocType, ["status"] = doc.Status,
            ["locale"] = doc.Locale, ["product"] = doc.Product,
            ["versions"] = ChatDtos.ParseJson(doc.Versions), ["tags"] = ChatDtos.ParseJson(doc.Tags),
        };
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.ToJsonString(ChatJson.Options)))).ToLowerInvariant();
    }

    static string Plain(string? value, bool preserveUnderscores = false)
    {
        var text = value ?? "";
        text = Regex.Replace(text, "`([^`]*)`", "$1");
        text = Regex.Replace(text, "!\\[([^]]*)\\]\\([^)]+\\)", "$1");
        text = Regex.Replace(text, "\\[([^]]+)\\]\\([^)]+\\)", "$1");
        text = Regex.Replace(text, "[*~]", "");
        if (!preserveUnderscores) text = text.Replace("_", "");
        return Regex.Replace(text, "\\s+", " ").Trim();
    }

    static string Slugify(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var ascii = new string(decomposed.Where(x => CharUnicodeInfo.GetUnicodeCategory(x) != UnicodeCategory.NonSpacingMark).ToArray());
        ascii = Regex.Replace(Plain(ascii).ToLowerInvariant(), "[^a-z0-9\\s-]", "");
        return Regex.Replace(ascii, "[-\\s]+", "-").Trim('-');
    }

    public static List<ChatSearchSection> SplitSections(string? text, ChatDocument doc, int chunkChars = 1400,
        string? documentTitle = null)
    {
        var title = Regex.Replace(Plain(documentTitle ?? doc.DisplayName ?? doc.SourceKey ?? "Document", preserveUnderscores: true),
            @"\.(?:md|mdx|markdown|html?|txt)$", "", RegexOptions.IgnoreCase);
        var baseUrl = doc.SourceUrl ?? doc.Url ?? "";
        var headings = new List<(int Level, string Text, string Anchor)>();
        var anchors = new Dictionary<string, int>(StringComparer.Ordinal);
        var rows = new List<ChatSearchSection>();
        var paragraph = new List<string>();
        var inFence = false;

        void Append(string content = "", string kind = "content")
        {
            content = Plain(content);
            if (content.Length == 0 && kind == "content") return;
            (int Level, string Text, string Anchor) heading = headings.Count > 0 ? headings[^1] : (0, title, "");
            var url = baseUrl + (heading.Anchor.Length > 0 && baseUrl.Length > 0 ? "#" + heading.Anchor : "");
            rows.Add(new ChatSearchSection
            {
                DocumentId = doc.Id, FilestoreId = doc.FilestoreId, User = doc.User, Ordinal = rows.Count,
                DocumentTitle = title, Heading = heading.Text, HeadingLevel = heading.Level,
                Hierarchy = new JsonArray(headings.Select(x => (JsonNode)x.Text).ToArray()).ToJsonString(ChatJson.Options),
                Anchor = heading.Anchor.Length == 0 ? null : heading.Anchor, Url = url,
                Kind = headings.Count == 0 ? "doc" : kind, Content = content,
                Category = doc.Category, DocType = doc.DocType, Status = doc.Status, Locale = doc.Locale,
                Product = doc.Product, Versions = doc.Versions, Tags = doc.Tags,
            });
        }
        void Flush()
        {
            var content = string.Join(' ', paragraph).Trim(); paragraph.Clear();
            while (content.Length > chunkChars)
            {
                var cut = content.LastIndexOf(' ', chunkChars - 1, chunkChars);
                if (cut < chunkChars / 2) cut = chunkChars;
                Append(content[..cut]); content = content[cut..].Trim();
            }
            Append(content);
        }
        foreach (var raw in (text ?? "").Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.TrimStart().StartsWith("```")) { inFence = !inFence; paragraph.Add(line); continue; }
            var match = inFence ? Match.Empty : Regex.Match(line, @"^\s*(#{1,6})\s+(.+?)\s*#*\s*$");
            if (match.Success)
            {
                Flush(); var level = match.Groups[1].Value.Length; var heading = Plain(match.Groups[2].Value);
                while (headings.Count > 0 && headings[^1].Level >= level) headings.RemoveAt(headings.Count - 1);
                var slug = Slugify(heading); if (slug.Length == 0) slug = $"section-{rows.Count + 1}";
                var number = anchors.GetValueOrDefault(slug); anchors[slug] = number + 1;
                headings.Add((level, heading, number == 0 ? slug : $"{slug}-{number}")); Append("", "heading");
            }
            else if (string.IsNullOrWhiteSpace(line)) Flush();
            else paragraph.Add(line.Trim());
        }
        Flush();
        if (rows.Count == 0) Append(text ?? "");
        return rows;
    }
}
