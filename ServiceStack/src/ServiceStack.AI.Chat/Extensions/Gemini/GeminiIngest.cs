using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ServiceStack.AI;

public sealed record GeminiIngestItem(string Key, string Title, string? Etag, long Size,
    Func<byte[]> Fetch, JsonObject Native);

public sealed class GeminiIngestEntry
{
    public long? Id { get; set; }
    public string SourceKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Text { get; set; } = "";
    public long Size { get; set; }
    public string ContentHash { get; set; } = "";
    public string MetadataHash { get; set; } = "";
    public string? SourceEtag { get; set; }
    public string ExtractorVer { get; set; } = GeminiIngest.ExtractorVersion;
    public JsonObject Metadata { get; set; } = new();

    public JsonObject ToDto(bool includeText = false)
    {
        var ret = Metadata.Clone();
        ret["id"] = Id; ret["sourceKey"] = SourceKey; ret["displayName"] = DisplayName;
        ret["size"] = Size; ret["contentHash"] = ContentHash; ret["metadataHash"] = MetadataHash;
        ret["sourceEtag"] = SourceEtag; ret["extractorVer"] = ExtractorVer;
        if (includeText) ret["text"] = Text;
        return ret;
    }
}

public sealed class GeminiIngestPlan
{
    public List<GeminiIngestEntry> Added { get; } = [];
    public List<GeminiIngestEntry> Changed { get; } = [];
    public List<GeminiIngestEntry> MetadataOnly { get; } = [];
    public List<GeminiIngestEntry> Unchanged { get; } = [];
    public List<ChatDocument> Removed { get; } = [];
    public List<JsonObject> Skipped { get; } = [];
    public List<JsonObject> Failed { get; } = [];
    public Dictionary<string, int> RulesMatched { get; } = [];
    public long Bytes { get; set; }
    public int Discovered => Added.Count + Changed.Count + MetadataOnly.Count + Unchanged.Count + Skipped.Count + Failed.Count;
    public int Embeds => Added.Count + Changed.Count + MetadataOnly.Count;

    public JsonObject Summary(int sample = 5)
    {
        JsonArray Keys<T>(IEnumerable<T> items, Func<T, string?> fn) =>
            new(items.Take(sample).Select(x => (JsonNode?)fn(x)).ToArray());
        return new JsonObject
        {
            ["discovered"] = Discovered, ["added"] = Added.Count, ["changed"] = Changed.Count,
            ["metadataOnly"] = MetadataOnly.Count, ["unchanged"] = Unchanged.Count,
            ["removed"] = Removed.Count, ["skipped"] = Skipped.Count, ["failed"] = Failed.Count,
            ["bytes"] = Bytes, ["embeds"] = Embeds,
            ["rulesMatched"] = new JsonObject(RulesMatched.Select(x =>
                KeyValuePair.Create<string, JsonNode?>(x.Key, JsonValue.Create(x.Value)))),
            ["samples"] = new JsonObject
            {
                ["added"] = Keys(Added, x => x.SourceKey), ["changed"] = Keys(Changed, x => x.SourceKey),
                ["removed"] = Keys(Removed, x => x.SourceKey),
                ["skipped"] = new JsonArray(Skipped.Take(sample).Select(x => (JsonNode)x.DeepClone()).ToArray()),
                ["failed"] = new JsonArray(Failed.Take(sample).Select(x => (JsonNode)x.DeepClone()).ToArray()),
            },
            ["preview"] = new JsonArray(Added.Concat(Changed).Take(sample)
                .Select(x => (JsonNode)x.ToDto()).ToArray()),
        };
    }
}

/// <summary>Pure discovery, extraction, metadata derivation and change planning for Gemini imports.</summary>
public static class GeminiIngest
{
    public const string ExtractorVersion = "1";
    public const int DefaultMinWords = 25;
    static readonly HashSet<string> HtmlExts = new(StringComparer.OrdinalIgnoreCase) { "html", "htm" };
    static readonly HashSet<string> BinaryExts = new(StringComparer.OrdinalIgnoreCase) { "pdf", "docx", "pptx", "xlsx" };
    static readonly HashSet<string> CodeExts = new(StringComparer.OrdinalIgnoreCase)
        { "cs", "js", "mjs", "ts", "tsx", "jsx", "py", "java", "go", "rs", "sh", "sql", "json", "xml", "yaml", "yml", "css", "scss", "vue", "svelte" };
    static readonly HashSet<string> TextExts = new(StringComparer.OrdinalIgnoreCase)
        { "", "txt", "md", "mdx", "rst", "csv", "log", "ini", "toml" };
    public static readonly string[] DefaultExcludes =
    [
        ".git/**", "**/.git/**", "node_modules/**", "**/node_modules/**", "__pycache__/**",
        "**/__pycache__/**", ".venv/**", "venv/**", "dist/**", "build/**", "**/.DS_Store",
        "**/*.lock", "**/.*/*", "import.json", "**/import.json",
    ];

    public static Regex GlobRegex(string pattern)
    {
        var sb = new StringBuilder("\\A");
        for (var i = 0; i < pattern.Length;)
        {
            if (i + 2 < pattern.Length && pattern.AsSpan(i, 3).SequenceEqual("**/"))
            { sb.Append("(?:.*/)?"); i += 3; }
            else if (i + 1 < pattern.Length && pattern.AsSpan(i, 2).SequenceEqual("**"))
            { sb.Append(".*"); i += 2; }
            else if (pattern[i] == '*') { sb.Append("[^/]*"); i++; }
            else if (pattern[i] == '?') { sb.Append("[^/]"); i++; }
            else if (pattern[i] == '[' && pattern.IndexOf(']', i + 1) is var end && end >= 0)
            { sb.Append(pattern[i..(end + 1)]); i = end + 1; }
            else { sb.Append(Regex.Escape(pattern[i].ToString())); i++; }
        }
        sb.Append("\\z");
        return new Regex(sb.ToString(), RegexOptions.Singleline | RegexOptions.CultureInvariant);
    }

    public static bool GlobMatch(string path, string pattern) => GlobRegex(pattern).IsMatch(path);
    public static bool MatchesAny(string path, IEnumerable<string>? patterns) => patterns?.Any(x => GlobMatch(path, x)) == true;

    public static string ResolvePath(string path)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path.StartsWith("~/")
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[2..]) : path);
        var full = Path.GetFullPath(expanded);
        // Resolve links component-by-component: ResolveLinkTarget() on /root/link/child does not
        // notice that an intermediate directory is a link. This prevents an allowed-looking path
        // from escaping a trusted root through a symlink.
        var root = Path.GetPathRoot(full) ?? "";
        var current = root;
        foreach (var part in full[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            FileSystemInfo info = Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);
            try
            {
                if (info.LinkTarget != null && info.ResolveLinkTarget(returnFinalTarget: true) is { } target)
                    current = target.FullName;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return Path.GetFullPath(current);
    }

    public static bool WithinRoots(string path, IEnumerable<string> roots)
    {
        var full = ResolvePath(path).TrimEnd(Path.DirectorySeparatorChar);
        return roots.Any(root =>
        {
            var resolved = ResolvePath(root).TrimEnd(Path.DirectorySeparatorChar);
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return full.Equals(resolved, comparison) || full.StartsWith(resolved + Path.DirectorySeparatorChar, comparison);
        });
    }

    public static string? DeriveCategory(string sourceKey, string? root = null, int? maxDepth = null, string? prefix = null)
    {
        var path = sourceKey.Replace('\\', '/').TrimStart('/');
        var cleanRoot = root?.Trim('/');
        if (!string.IsNullOrEmpty(cleanRoot))
        {
            if (path == cleanRoot) path = "";
            else if (path.StartsWith(cleanRoot + "/", StringComparison.Ordinal)) path = path[(cleanRoot.Length + 1)..];
            else return null;
        }
        var directory = path.Contains('/') ? path[..path.LastIndexOf('/')] : "";
        var parts = directory.Split('/', StringSplitOptions.RemoveEmptyEntries).AsEnumerable();
        if (maxDepth != null) parts = parts.Take(maxDepth.Value);
        if (!string.IsNullOrWhiteSpace(prefix))
            parts = prefix.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).Concat(parts);
        return string.Join('/', parts);
    }

    public static bool WithinMaxDepth(string sourceKey, string? root = null, int? maxDepth = null)
    {
        var path = sourceKey.Replace('\\', '/').TrimStart('/');
        var cleanRoot = root?.Trim('/');
        if (!string.IsNullOrEmpty(cleanRoot))
        {
            if (path == cleanRoot) path = "";
            else if (path.StartsWith(cleanRoot + "/", StringComparison.Ordinal)) path = path[(cleanRoot.Length + 1)..];
            else return true; // DeriveCategory reports this separately as outside the category root.
        }
        if (maxDepth == null) return true;
        if (maxDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth), "maxDepth must be zero or greater");
        var directory = path.Contains('/') ? path[..path.LastIndexOf('/')] : "";
        var depth = directory.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;
        return depth <= maxDepth.Value;
    }

    public static string NormalizeText(string? text, IEnumerable<string>? volatilePatterns = null)
    {
        var value = (text ?? "").Normalize(NormalizationForm.FormC);
        foreach (var pattern in volatilePatterns ?? [])
        {
            try { value = Regex.Replace(value, pattern, ""); } catch (ArgumentException) { }
        }
        value = value.Replace("\r\n", "\n").Replace('\r', '\n');
        value = string.Join('\n', value.Split('\n').Select(x => x.TrimEnd()));
        return Regex.Replace(value, "\\n{3,}", "\n\n").Trim();
    }

    public static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    public static string ContentHash(string text, IEnumerable<string>? volatilePatterns = null) => Sha256(NormalizeText(text, volatilePatterns));

    public static string MetadataHash(JsonObject metadata)
    {
        var canonical = new JsonObject();
        foreach (var (key, value) in metadata.OrderBy(x => x.Key))
        {
            if (value == null || value is JsonValue scalar && scalar.ToString() == "") continue;
            canonical[key] = value is JsonArray array
                ? new JsonArray(array.Select(x => x?.DeepClone()).OrderBy(x => x?.ToJsonString()).ToArray())
                : value.DeepClone();
        }
        return Sha256(canonical.ToJsonString(ChatJson.Options));
    }

    public static (JsonObject Metadata, string Body) ParseFrontmatter(string text)
    {
        var match = Regex.Match(text ?? "", "\\A---[ \\t]*\\n(.*?)\\n---[ \\t]*\\n", RegexOptions.Singleline);
        if (!match.Success) return (new JsonObject(), text);
        var metadata = new JsonObject(); string? key = null;
        foreach (var raw in match.Groups[1].Value.Split('\n'))
        {
            var line = raw.TrimEnd();
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#')) continue;
            if (line.TrimStart().StartsWith("- ") && key != null)
            {
                var list = metadata[key] as JsonArray ?? new JsonArray(); metadata[key] = list;
                list.Add(Scalar(line.TrimStart()[2..])); continue;
            }
            var colon = line.IndexOf(':'); if (colon < 0) continue;
            key = line[..colon].Trim(); var value = line[(colon + 1)..].Trim();
            if (value.StartsWith('[') && value.EndsWith(']'))
                metadata[key] = new JsonArray(value[1..^1].Split(',').Where(x => x.Trim().Length > 0)
                    .Select(x => Scalar(x.Trim())).ToArray());
            else metadata[key] = value.Length == 0 ? new JsonArray() : Scalar(value);
        }
        return (metadata, text[match.Length..]);
    }

    static JsonNode Scalar(string value)
    {
        value = value.Trim().Trim('\'', '"');
        if (bool.TryParse(value, out var boolean)) return boolean;
        if (long.TryParse(value, out var number)) return number;
        return value;
    }

    public static (string? Text, JsonObject Frontmatter, string? Skip) Extract(byte[] content, string filename, JsonObject? options = null)
    {
        options ??= new JsonObject(); var ext = Path.GetExtension(filename).TrimStart('.').ToLowerInvariant();
        if (BinaryExts.Contains(ext)) return (null, new JsonObject(), $"unsupported type .{ext}");
        string text;
        try { text = new UTF8Encoding(false, true).GetString(content); }
        catch (DecoderFallbackException) { text = Encoding.Latin1.GetString(content); }
        var front = new JsonObject();
        if (HtmlExts.Contains(ext))
        {
            if (options.GetString("selector") is { Length: > 0 } selector)
                text = SelectHtml(text, selector);
            text = new HtmlToMarkdownParser().Parse(text);
            text = string.Join('\n', text.Split('\n').Where(line => !Regex.IsMatch(line.Trim(),
                "^(edit this page.*|was this (page )?helpful\\??|on this page|table of contents|copyright .*|all rights reserved.*|we use cookies.*|skip to (main )?content)$",
                RegexOptions.IgnoreCase)));
        }
        else if (TextExts.Contains(ext) || CodeExts.Contains(ext)) (front, text) = ParseFrontmatter(text);
        else return (null, front, $"unsupported type .{ext}");
        foreach (var pattern in options.GetArray("strip")?.Select(x => x?.GetValue<string>()).Where(x => x != null).Cast<string>() ?? [])
        { try { text = Regex.Replace(text, pattern, ""); } catch (ArgumentException) { } }
        var minWords = options.GetInt("minWords") ?? DefaultMinWords;
        if (!CodeExts.Contains(ext) && minWords > 0 && text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length < minWords)
            return (null, front, $"under {minWords} words");
        return (text, front, null);
    }

    /// <summary>Scope HTML extraction to the first matching tag, .class or #id selector.</summary>
    static string SelectHtml(string html, string selectors)
    {
        var wanted = selectors.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var tags = Regex.Matches(html, "<(?<close>/)?(?<tag>[a-zA-Z][\\w:-]*)(?<attrs>[^>]*)>");
        for (var i = 0; i < tags.Count; i++)
        {
            var match = tags[i];
            if (match.Groups["close"].Success) continue;
            var tag = match.Groups["tag"].Value;
            var attrs = match.Groups["attrs"].Value;
            if (!wanted.Any(selector => SelectorMatches(tag, attrs, selector))) continue;
            var depth = 1;
            for (var j = i + 1; j < tags.Count; j++)
            {
                var next = tags[j];
                if (!next.Groups["tag"].Value.Equals(tag, StringComparison.OrdinalIgnoreCase)) continue;
                if (next.Groups["close"].Success) depth--;
                else if (!next.Groups["attrs"].Value.TrimEnd().EndsWith('/')) depth++;
                if (depth == 0)
                    return html[match.Index..(next.Index + next.Length)];
            }
            return html[match.Index..];
        }
        return "";
    }

    static bool SelectorMatches(string tag, string attrs, string selector)
    {
        if (selector.StartsWith('#'))
            return Regex.IsMatch(attrs, $"\\bid\\s*=\\s*(['\"]){Regex.Escape(selector[1..])}\\1",
                RegexOptions.IgnoreCase);
        if (selector.StartsWith('.'))
        {
            var match = Regex.Match(attrs, "\\bclass\\s*=\\s*(['\"])(?<value>.*?)\\1", RegexOptions.IgnoreCase);
            return match.Success && match.Groups["value"].Value.Split((char[]?)null,
                StringSplitOptions.RemoveEmptyEntries).Contains(selector[1..]);
        }
        return tag.Equals(selector, StringComparison.OrdinalIgnoreCase);
    }

    static readonly string[] MetadataFields = ["docType", "status", "locale", "product", "versions", "tags", "sourceUrl", "sourceUpdatedAt"];
    static readonly string[] ListFields = ["versions", "tags"];

    public static (JsonObject? Metadata, List<string> Matched) DeriveMetadata(string sourceKey, JsonObject? rules,
        JsonObject? frontmatter, JsonObject? native, JsonObject? overrides)
    {
        rules ??= new JsonObject(); var metadata = new JsonObject(); var matched = new List<string>();
        foreach (var (key, value) in rules.GetObject("defaults") ?? [])
            if (MetadataFields.Contains(key)) metadata[key] = value?.DeepClone();
        foreach (var rule in rules.GetArray("rules")?.OfType<JsonObject>() ?? [])
        {
            var pattern = rule.GetString("match");
            if (pattern == null || !GlobMatch(sourceKey, pattern)) continue;
            matched.Add(pattern); if (rule.GetBool("skip")) return (null, matched);
            foreach (var (key, value) in rule.GetObject("set") ?? [])
            {
                if (!MetadataFields.Contains(key)) continue;
                if (ListFields.Contains(key))
                {
                    var list = new JsonArray(GeminiMetadata.AsList(metadata[key]).Select(x => (JsonNode)x).ToArray());
                    foreach (var item in GeminiMetadata.AsList(value))
                        if (!GeminiMetadata.AsList(list).Contains(item)) list.Add(item);
                    metadata[key] = list;
                }
                else if (metadata[key] == null || rules.GetObject("defaults")?.ContainsKey(key) == true)
                    metadata[key] = value?.DeepClone();
            }
        }
        var allow = rules.GetObject("frontmatter")?.GetArray("allow")?.Select(x => x?.GetValue<string>()).ToHashSet();
        foreach (var (key, value) in frontmatter ?? [])
            if (MetadataFields.Contains(key) && (allow == null || allow.Contains(key))) metadata[key] = value?.DeepClone();
        foreach (var (key, value) in native ?? [])
            if (MetadataFields.Contains(key) && metadata[key] == null && value != null) metadata[key] = value.DeepClone();
        foreach (var (key, value) in overrides ?? [])
            if (MetadataFields.Contains(key) && value != null && (value is not JsonValue scalar || scalar.ToString() != ""))
                metadata[key] = value.DeepClone();
        foreach (var key in ListFields)
            if (metadata[key] != null && metadata[key] is not JsonArray)
                metadata[key] = new JsonArray(metadata[key]!.DeepClone());
        return (metadata, matched);
    }

    public static JsonObject TemplateValues(string sourceKey, string? category = null, string? title = null, string? root = null)
    {
        var key = sourceKey.Replace('\\', '/').TrimStart('/'); var slash = key.LastIndexOf('/');
        var dir = slash >= 0 ? key[..slash] : ""; var filename = slash >= 0 ? key[(slash + 1)..] : key;
        var ext = Path.GetExtension(filename).TrimStart('.'); var name = ext.Length > 0 ? filename[..^(ext.Length + 1)] : filename;
        var cleanRoot = root?.Replace('\\', '/').Trim('/'); var relative = key;
        if (!string.IsNullOrEmpty(cleanRoot))
            relative = key == cleanRoot ? "" : key.StartsWith(cleanRoot + "/") ? key[(cleanRoot.Length + 1)..] : key;
        return new JsonObject
        {
            ["fullpath"] = key, ["path"] = relative,
            ["pathnoext"] = ext.Length > 0 ? relative[..^(ext.Length + 1)] : relative,
            ["dir"] = dir, ["filename"] = filename, ["name"] = name, ["ext"] = ext,
            ["category"] = category ?? "", ["title"] = title ?? name,
        };
    }

    public static string ExpandTemplate(string template, JsonObject values)
    {
        var output = Regex.Replace(template, "\\{(\\w+)\\}", match =>
            values.GetString(match.Groups[1].Value.ToLowerInvariant()) ?? match.Value, RegexOptions.IgnoreCase);
        var scheme = output.IndexOf("://", StringComparison.Ordinal);
        if (scheme >= 0) return output[..(scheme + 3)] + Regex.Replace(output[(scheme + 3)..], "/{2,}", "/");
        return Regex.Replace(output, "/{2,}", "/");
    }

    public static List<GeminiIngestItem> Discover(JsonObject config, string type)
    {
        var path = ResolvePath(config.GetString("path") ?? "");
        var includes = GeminiMetadata.AsList(config["include"]); var excludes = GeminiMetadata.AsList(config["exclude"]).Concat(DefaultExcludes).ToList();
        if (type == "folder")
        {
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException($"Not a directory: {path}");
            var enumeration = new EnumerationOptions
            {
                RecurseSubdirectories = true, IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint,
            };
            return Directory.EnumerateFiles(path, "*", enumeration).OrderBy(x => x).Select(full =>
            {
                if (!WithinRoots(full, [path])) return null;
                var key = Path.GetRelativePath(path, full).Replace('\\', '/');
                if (MatchesAny(key, excludes) || includes.Count > 0 && !MatchesAny(key, includes)) return null;
                var info = new FileInfo(full); var native = new JsonObject { ["sourceUpdatedAt"] = new DateTimeOffset(info.LastWriteTimeUtc).ToUnixTimeSeconds() };
                return new GeminiIngestItem(key, Path.GetFileName(key), $"{info.LastWriteTimeUtc.Ticks}:{info.Length}", info.Length,
                    () => File.ReadAllBytes(full), native);
            }).Where(x => x != null).Cast<GeminiIngestItem>().ToList();
        }
        if (type == "zip")
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"Archive not found: {path}");
            using var archive = ZipFile.OpenRead(path);
            return archive.Entries.Where(x => !string.IsNullOrEmpty(x.Name)).OrderBy(x => x.FullName).Select(entry =>
            {
                var key = entry.FullName.Replace('\\', '/').TrimStart('/');
                if (key.Contains("__MACOSX/") || MatchesAny(key, excludes) || includes.Count > 0 && !MatchesAny(key, includes)) return null;
                var bytes = ReadZipEntry(path, entry.FullName); var native = new JsonObject();
                if (entry.LastWriteTime.Year >= 1980) native["sourceUpdatedAt"] = entry.LastWriteTime.ToUnixTimeSeconds();
                return new GeminiIngestItem(key, Path.GetFileName(key), $"{entry.Crc32}:{entry.Length}", entry.Length,
                    () => bytes, native);
            }).Where(x => x != null).Cast<GeminiIngestItem>().ToList();
        }
        throw new ArgumentException($"Unknown source type '{type}'");
    }

    static byte[] ReadZipEntry(string archivePath, string entryName)
    {
        using var archive = ZipFile.OpenRead(archivePath); var entry = archive.GetEntry(entryName)!;
        using var stream = entry.Open(); using var ms = new MemoryStream(); stream.CopyTo(ms); return ms.ToArray();
    }

    static JsonObject RulesForItem(JsonObject config, string type, string key, JsonObject baseRules)
    {
        JsonObject inherited;
        var path = ResolvePath(config.GetString("path") ?? "");
        if (type == "folder")
        {
            inherited = GeminiExtension.EffectiveImportMetadata(path, key);
        }
        else if (type == "zip")
        {
            inherited = new JsonObject();
            using var archive = ZipFile.OpenRead(path);
            var directory = Path.GetDirectoryName(key.Replace('/', Path.DirectorySeparatorChar)) ?? "";
            var parts = directory.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            var current = "";
            foreach (var part in new string?[] { null }.Concat(parts))
            {
                if (part != null) current = current.Length == 0 ? part : current + "/" + part;
                var manifest = current.Length == 0 ? "import.json" : current + "/import.json";
                var entry = archive.GetEntry(manifest); if (entry == null) continue;
                try { using var reader = new StreamReader(entry.Open(), Encoding.UTF8); var cfg = ChatJson.TryParseObject(reader.ReadToEnd()) ?? new JsonObject(); inherited = GeminiExtension.MergeImportMetadata(inherited, cfg.GetObject("metadata")); }
                catch (Exception e) { throw new ArgumentException($"Invalid {manifest}: {e.Message}", e); }
            }
        }
        else return baseRules;
        return GeminiExtension.MergeImportMetadata(inherited, baseRules);
    }

    public static GeminiIngestPlan BuildPlan(ChatSource source, IEnumerable<ChatDocument> existingDocs, JsonObject? overrides = null)
    {
        var plan = new GeminiIngestPlan(); var existing = existingDocs.Where(x => x.SourceKey != null).ToDictionary(x => x.SourceKey!); var seen = new HashSet<string>();
        var config = ChatDtos.ParseJson(source.Config) as JsonObject ?? new JsonObject();
        var categoryConfig = ChatDtos.ParseJson(source.Category) as JsonObject ?? new JsonObject();
        var rules = ChatDtos.ParseJson(source.Rules) as JsonObject ?? new JsonObject();
        var extract = ChatDtos.ParseJson(source.Extract) as JsonObject ?? new JsonObject();
        var volatilePatterns = GeminiMetadata.AsList(ChatDtos.ParseJson(source.Volatile));
        var categoryRoot = categoryConfig.GetString("root"); var maxDepth = categoryConfig.GetInt("maxDepth");
        foreach (var item in Discover(config, source.Type ?? "folder"))
        {
            if (!WithinMaxDepth(item.Key, categoryRoot, maxDepth)) continue;
            seen.Add(item.Key); var category = DeriveCategory(item.Key, categoryRoot, maxDepth, categoryConfig.GetString("prefix"));
            if (category == null) { plan.Skipped.Add(new JsonObject { ["sourceKey"] = item.Key, ["reason"] = "outside root" }); continue; }
            try
            {
                var raw = item.Fetch(); var extracted = Extract(raw, item.Key, extract);
                if (extracted.Skip != null) { plan.Skipped.Add(new JsonObject { ["sourceKey"] = item.Key, ["reason"] = extracted.Skip }); continue; }
                var itemRules = RulesForItem(config, source.Type ?? "folder", item.Key, rules);
                var derived = DeriveMetadata(item.Key, itemRules, extracted.Frontmatter, item.Native, overrides);
                if (derived.Metadata == null) { plan.Skipped.Add(new JsonObject { ["sourceKey"] = item.Key, ["reason"] = "excluded by rule" }); continue; }
                foreach (var pattern in derived.Matched) plan.RulesMatched[pattern] = plan.RulesMatched.GetValueOrDefault(pattern) + 1;
                derived.Metadata["category"] = category; derived.Metadata["categoryPath"] = new JsonArray(GeminiMetadata.CategoryAncestors(category).Select(x => (JsonNode)x).ToArray());
                if (derived.Metadata.GetString("sourceUrl") is { } sourceUrl)
                    derived.Metadata["sourceUrl"] = ExpandTemplate(sourceUrl, TemplateValues(item.Key, category, item.Title, categoryConfig.GetString("root")));
                var contentHash = ContentHash(extracted.Text!, volatilePatterns); var metadataHash = MetadataHash(derived.Metadata);
                var entry = new GeminiIngestEntry { SourceKey = item.Key, DisplayName = extracted.Frontmatter.GetString("title") ?? item.Title, Text = extracted.Text!, Size = raw.Length,
                    ContentHash = contentHash, MetadataHash = metadataHash, SourceEtag = item.Etag, ExtractorVer = source.ExtractorVer ?? ExtractorVersion,
                    Metadata = derived.Metadata };
                if (!existing.TryGetValue(item.Key, out var prior)) { plan.Added.Add(entry); plan.Bytes += raw.Length; }
                else if (prior.ContentHash != contentHash || prior.ExtractorVer != entry.ExtractorVer) { entry.Id = prior.Id; plan.Changed.Add(entry); plan.Bytes += raw.Length; }
                else if (prior.MetadataHash != metadataHash) { entry.Id = prior.Id; plan.MetadataOnly.Add(entry); }
                else plan.Unchanged.Add(entry);
            }
            catch (Exception e) { plan.Failed.Add(new JsonObject { ["sourceKey"] = item.Key, ["reason"] = e.Message.SafeSubstring(0, 200) }); }
        }
        foreach (var doc in existing.Values)
            if (!seen.Contains(doc.SourceKey!) && doc.TombstonedAt == null) plan.Removed.Add(doc);
        return plan;
    }

    public static string? DeleteRefusal(GeminiIngestPlan plan, int existingCount)
    {
        if (plan.Removed.Count == 0 || existingCount == 0) return null;
        var ratio = (double)plan.Removed.Count / existingCount;
        return plan.Removed.Count > 100 || existingCount >= 20 && ratio > .2
            ? $"Refusing to remove {plan.Removed.Count} of {existingCount} documents ({ratio:P0}). Confirm explicitly if this is intended."
            : null;
    }
}
