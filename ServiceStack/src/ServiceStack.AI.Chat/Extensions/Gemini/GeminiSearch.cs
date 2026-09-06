using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ServiceStack.AI;

/// <summary>Configuration and heading-aware extraction for the independent local Search feature.</summary>
public static partial class GeminiSearch
{
    public const string IndexVersion = "4";
    public static readonly string[] ScopeFields = ["category", "docType", "status", "locale", "product", "versions", "tags"];
    static readonly HashSet<string> Themes = ["auto", "light", "dark", "nord", "matrix", "soft-pink"];
    static readonly HashSet<string> Positions = ["top-left", "top-right", "bottom-left", "bottom-right"];
    static readonly HashSet<string> SearchGroupStopWords =
        ["a", "an", "and", "are", "for", "how", "in", "is", "of", "on", "the", "to", "with"];
    static readonly HashSet<string> LauncherStyles = ["raised", "flat", "inset"];
    public static readonly string[] DefaultDeniedUserAgents =
    [
        "bytespider", "gptbot", "claudebot", "amazonbot", "imagesiftbot", "semrushbot",
        "dotbot", "dataforseobot", "whatsapp bot", "petalbot",
    ];

    static int Bounded(int? value, int fallback, int min, int max) => Math.Clamp(value ?? fallback, min, max);
    static double Bounded(double? value, double fallback, double min, double max) => Math.Clamp(value ?? fallback, min, max);
    static double? Number(JsonObject? value, string name) => value.GetDouble(name) ?? value.GetLong(name);

    public static JsonObject NormalizeRanking(JsonObject? supplied = null)
    {
        supplied ??= new JsonObject();
        var rawTypes = supplied.GetObject("docTypeWeights") ?? new JsonObject();
        var docTypeWeights = new JsonObject();
        foreach (var (key, _) in rawTypes.Take(50))
        {
            var name = key.Trim().SafeSubstring(0, 100);
            var weight = Bounded(Number(rawTypes, key), 0, -20, 50);
            if (name.Length > 0 && weight != 0) docTypeWeights[name] = weight;
        }
        return new JsonObject
        {
            ["titleWeight"] = Bounded(Number(supplied, "titleWeight"), 8, 0, 50),
            ["headingWeight"] = Bounded(Number(supplied, "headingWeight"), 5, 0, 50),
            ["contentWeight"] = Bounded(Number(supplied, "contentWeight"), 1, 0, 50),
            ["phraseBoost"] = Bounded(Number(supplied, "phraseBoost"), 4, 0, 50),
            ["exactTitleBoost"] = Bounded(Number(supplied, "exactTitleBoost"), 6, 0, 50),
            ["freshnessWeight"] = Bounded(Number(supplied, "freshnessWeight"), 20, 0, 50),
            ["freshnessHalfLifeDays"] = Bounded(supplied.GetInt("freshnessHalfLifeDays"), 365, 1, 3650),
            ["nativeWeight"] = Bounded(Number(supplied, "nativeWeight"), 2, 0, 20),
            ["docTypeWeights"] = docTypeWeights,
        };
    }

    public static JsonObject NormalizeConfig(JsonObject? supplied = null)
    {
        supplied ??= new JsonObject();
        var identity = supplied.GetObject("identity") ?? new JsonObject();
        var rawScope = supplied.GetObject("scope") ?? new JsonObject();
        var ranking = NormalizeRanking(supplied.GetObject("ranking"));
        var rawBehavior = supplied.GetObject("behavior") ?? new JsonObject();
        var rawAnalytics = supplied.GetObject("analytics") ?? new JsonObject();
        var rawAppearance = supplied.GetObject("appearance") ?? new JsonObject();
        var rawHosting = supplied.GetObject("hosting") ?? new JsonObject();
        var scope = new JsonObject();
        foreach (var field in ScopeFields)
            if (rawScope.GetString(field)?.Trim() is { Length: > 0 } value) scope[field] = value.SafeSubstring(0, 300);
        var theme = rawAppearance.GetString("theme") ?? "auto";
        if (!Themes.Contains(theme)) theme = "auto";
        var highlightColor = rawAppearance.GetString("highlightColor")?.Trim() ?? "";
        if (!Regex.IsMatch(highlightColor, "^#[0-9a-fA-F]{6}$")) highlightColor = "";
        var fontFamily = Regex.Replace(rawAppearance.GetString("fontFamily") ?? "", "[\\x00-\\x1f{};]", "")
            .Trim().SafeSubstring(0, 300);
        var position = rawAppearance.GetString("position") ?? "bottom-right";
        if (!Positions.Contains(position)) position = "bottom-right";
        var launcherStyle = rawAppearance.GetString("launcherStyle") ?? "flat";
        if (!LauncherStyles.Contains(launcherStyle)) launcherStyle = "flat";
        var mount = GeminiAssistants.CleanSelector((rawAppearance.GetString("mount") ?? "").Trim().SafeSubstring(0, 300));
        var rawOffset = rawAppearance.GetObject("offset") ?? new JsonObject();
        var legacyShortcut = rawBehavior.TryGetPropertyValue("keyboardShortcut", out _)
            ? rawBehavior.GetBool("keyboardShortcut") : (bool?)null;
        var origins = rawHosting.GetArray("allowedOrigins")?.Select(x => x?.GetValue<string>()?.Trim().TrimEnd('/'))
            .Where(x => !string.IsNullOrEmpty(x)).Distinct().Take(100).ToArray() ?? [];
        var deniedUserAgents = rawAnalytics.TryGetPropertyValue("deniedUserAgents", out var deniedUserAgentNode)
            ? NormalizeRules(GeminiMetadata.AsList(deniedUserAgentNode), x => x.ToLowerInvariant(), 100, 200)
            : DefaultDeniedUserAgents;
        var deniedIpRanges = NormalizeRules(GeminiMetadata.AsList(rawAnalytics["deniedIpRanges"]),
            NormalizeIpRule, 100, 100);
        var excludedPaths = NormalizeRules(GeminiMetadata.AsList(rawAnalytics["excludedPaths"]),
            NormalizePathRule, 100, 500);
        return new JsonObject
        {
            ["identity"] = new JsonObject
            {
                ["title"] = (identity.GetString("title") ?? "Search documentation").Trim().SafeSubstring(0, 200),
                ["placeholder"] = (identity.GetString("placeholder") ?? "Search docs").Trim().SafeSubstring(0, 120),
                ["emptyText"] = (identity.GetString("emptyText") ?? "No matching documents found.").Trim().SafeSubstring(0, 300),
                ["tooltip"] = (identity.GetString("tooltip") ?? "").Trim().SafeSubstring(0, 200),
            },
            ["scope"] = scope,
            ["ranking"] = ranking,
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
            ["analytics"] = new JsonObject
            {
                ["enabled"] = rawAnalytics.GetBool("enabled"),
                ["retentionDays"] = Bounded(rawAnalytics.GetInt("retentionDays"), 90, 1, 3650),
                ["anonymizeIp"] = rawAnalytics.GetBool("anonymizeIp", true),
                ["respectDoNotTrack"] = rawAnalytics.GetBool("respectDoNotTrack", true),
                ["excludeBots"] = rawAnalytics.GetBool("excludeBots", true),
                ["requireConsent"] = rawAnalytics.GetBool("requireConsent"),
                ["deniedUserAgents"] = new JsonArray(deniedUserAgents.Select(x => (JsonNode)x).ToArray()),
                ["deniedIpRanges"] = new JsonArray(deniedIpRanges.Select(x => (JsonNode)x).ToArray()),
                ["excludedPaths"] = new JsonArray(excludedPaths.Select(x => (JsonNode)x).ToArray()),
            },
            ["appearance"] = new JsonObject
            {
                ["theme"] = theme,
                ["highlightColor"] = highlightColor,
                ["fontFamily"] = fontFamily,
                ["position"] = position,
                ["launcherStyle"] = launcherStyle,
                ["mount"] = mount,
                ["offset"] = new JsonObject
                {
                    ["top"] = Bounded(rawOffset.GetInt("top"), 20, 0, 400),
                    ["right"] = Bounded(rawOffset.GetInt("right"), 20, 0, 400),
                    ["bottom"] = Bounded(rawOffset.GetInt("bottom"), 20, 0, 400),
                    ["left"] = Bounded(rawOffset.GetInt("left"), 20, 0, 400),
                },
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

    public static bool IsBot(string? userAgent) => !string.IsNullOrEmpty(userAgent) &&
        Regex.IsMatch(userAgent,
            "bot|crawler|spider|slurp|bingpreview|headlesschrome|lighthouse|pagespeed|" +
            "uptimerobot|pingdom|statuscake|facebookexternalhit|twitterbot|linkedinbot|" +
            "whatsapp|google-inspectiontool", RegexOptions.IgnoreCase);

    static string[] NormalizeRules(IEnumerable<string> values, Func<string, string?> normalize, int take, int maxLength) =>
        values.Select(x => normalize(x.Trim().SafeSubstring(0, maxLength)))
            .Where(x => !string.IsNullOrEmpty(x)).Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(take).ToArray();

    static string? NormalizePathRule(string value)
    {
        value = value.Trim();
        if (value.Length == 0) return null;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            value = uri.AbsolutePath;
        if (!value.StartsWith('/')) value = "/" + value;
        return value;
    }

    public static bool IsDeniedUserAgent(string? userAgent, IEnumerable<string> deniedUserAgents) =>
        !string.IsNullOrEmpty(userAgent) && deniedUserAgents.Any(rule =>
            !string.IsNullOrWhiteSpace(rule) && userAgent.Contains(rule.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string? NormalizeIpRule(string? value)
    {
        value = value?.Trim();
        if (string.IsNullOrEmpty(value)) return null;
        if (value.Contains('*'))
        {
            var parts = value.Split('.');
            if (parts.Length is < 1 or > 4) return null;
            Array.Resize(ref parts, 4);
            var wildcard = false;
            var prefix = 0;
            var bytes = new byte[4];
            for (var i = 0; i < 4; i++)
            {
                var part = parts[i];
                if (string.IsNullOrEmpty(part)) part = "*";
                if (part == "*") wildcard = true;
                else
                {
                    if (wildcard || !byte.TryParse(part, out bytes[i])) return null;
                    prefix += 8;
                }
            }
            return prefix == 32 ? new IPAddress(bytes).ToString() : $"{new IPAddress(bytes)}/{prefix}";
        }
        var slash = value.IndexOf('/');
        var addressText = slash >= 0 ? value[..slash] : value;
        if (!IPAddress.TryParse(addressText, out var address)) return null;
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (slash < 0) return address.ToString();
        var bitCount = address.GetAddressBytes().Length * 8;
        if (!int.TryParse(value[(slash + 1)..], out var prefixLength) || prefixLength < 0 || prefixLength > bitCount)
            return null;
        var networkBytes = address.GetAddressBytes();
        MaskAddress(networkBytes, prefixLength);
        return prefixLength == bitCount
            ? new IPAddress(networkBytes).ToString()
            : $"{new IPAddress(networkBytes)}/{prefixLength}";
    }

    static void MaskAddress(byte[] bytes, int prefixLength)
    {
        var wholeBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;
        if (remainingBits > 0 && wholeBytes < bytes.Length)
            bytes[wholeBytes++] &= (byte)(0xff << (8 - remainingBits));
        for (var i = wholeBytes; i < bytes.Length; i++) bytes[i] = 0;
    }

    public static bool IsDeniedIp(string? value, IEnumerable<string> deniedIpRanges)
    {
        var normalizedAddress = GeminiSearchGeo.NormalizeIpAddress(value);
        if (normalizedAddress == null || !IPAddress.TryParse(normalizedAddress, out var address)) return false;
        var addressBytes = address.GetAddressBytes();
        foreach (var rawRule in deniedIpRanges)
        {
            var rule = NormalizeIpRule(rawRule);
            if (rule == null) continue;
            var slash = rule.IndexOf('/');
            if (slash < 0)
            {
                if (string.Equals(normalizedAddress, rule, StringComparison.OrdinalIgnoreCase)) return true;
                continue;
            }
            if (!IPAddress.TryParse(rule[..slash], out var network) ||
                !int.TryParse(rule[(slash + 1)..], out var prefixLength)) continue;
            var networkBytes = network.GetAddressBytes();
            if (networkBytes.Length != addressBytes.Length) continue;
            var candidate = addressBytes.ToArray();
            MaskAddress(candidate, prefixLength);
            if (candidate.SequenceEqual(networkBytes)) return true;
        }
        return false;
    }

    public static bool IsExcludedPath(string? value, IEnumerable<string> excludedPaths)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var path = Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"
            ? uri.AbsolutePath
            : value.Split('?', '#')[0];
        if (!path.StartsWith('/')) path = "/" + path;
        return excludedPaths.Any(rawRule =>
        {
            var rule = NormalizePathRule(rawRule);
            if (rule == null) return false;
            var pattern = "^" + Regex.Escape(rule).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return Regex.IsMatch(path, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        });
    }

    public static string? AnonymizeIp(string? value)
    {
        if (!IPAddress.TryParse(value, out var address)) return null;
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        var bytes = address.GetAddressBytes();
        if (bytes.Length == 4)
            bytes[3] = 0;
        else
            Array.Clear(bytes, 6, bytes.Length - 6); // retain an IPv6 /48 network only
        return new IPAddress(bytes).ToString();
    }

    public static List<ChatSearchResult> RankResults(IEnumerable<ChatSearchResult> candidates, string query,
        IReadOnlyDictionary<long, ChatDocument>? documents = null, JsonObject? ranking = null)
    {
        var rows = candidates.ToList();
        if (rows.Count == 0) return rows;
        var config = NormalizeRanking(ranking);
        var normalizedQuery = NormalizeSearchQuery(query);
        var tokens = normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
        documents ??= new Dictionary<long, ChatDocument>();

        (double Coverage, bool Phrase, bool Exact) Quality(string? value)
        {
            var text = NormalizeSearchQuery(value);
            if (text.Length == 0 || tokens.Length == 0) return (0, false, false);
            var coverage = tokens.Count(token => text.Contains(token, StringComparison.Ordinal)) / (double)tokens.Length;
            return (coverage, text.Contains(normalizedQuery, StringComparison.Ordinal), text == normalizedQuery);
        }

        var titleWeight = config.GetDouble("titleWeight")!.Value;
        var headingWeight = config.GetDouble("headingWeight")!.Value;
        var contentWeight = config.GetDouble("contentWeight")!.Value;
        var phraseBoost = config.GetDouble("phraseBoost")!.Value;
        var exactTitleBoost = config.GetDouble("exactTitleBoost")!.Value;
        var freshnessWeight = config.GetDouble("freshnessWeight")!.Value;
        var halfLifeDays = config.GetInt("freshnessHalfLifeDays")!.Value;
        var nativeWeight = config.GetDouble("nativeWeight")!.Value;
        var typeWeights = config.GetObject("docTypeWeights")!;
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return rows.Select((row, position) =>
            {
                var title = Quality(row.DocumentTitle);
                var heading = Quality(row.Heading);
                var content = Quality(row.Content);
                var score = titleWeight * title.Coverage + headingWeight * heading.Coverage
                    + contentWeight * content.Coverage;
                if (title.Phrase || heading.Phrase || content.Phrase) score += phraseBoost;
                if (title.Exact) score += exactTitleBoost;
                documents.TryGetValue(row.DocumentId, out var document);
                var updated = document?.SourceUpdatedAt;
                var updatedAt = updated > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(updated.Value)
                    : document?.UploadedAt is { } uploadedAt
                        ? new DateTimeOffset(uploadedAt.ToUniversalTime())
                        : document == null ? (DateTimeOffset?)null : new DateTimeOffset(document.CreatedAt.ToUniversalTime());
                if (updatedAt != null && freshnessWeight > 0)
                {
                    var ageDays = Math.Max(0, now - updatedAt.Value.ToUnixTimeSeconds()) / 86400d;
                    score += freshnessWeight * Math.Pow(.5, ageDays / halfLifeDays);
                }
                var docType = row.DocType ?? document?.DocType;
                if (docType != null) score += typeWeights.GetDouble(docType) ?? 0;
                var nativeQuality = rows.Count == 1 ? 1 : 1 - position / (double)(rows.Count - 1);
                score += nativeWeight * nativeQuality;
                row.Score = Math.Round(score, 6);
                return (Row: row, Score: score, Position: position);
            })
            .OrderByDescending(x => x.Score).ThenBy(x => x.Position).Select(x => x.Row).ToList();
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

    public static string NormalizeSearchQuery(string? value)
    {
        var decomposed = (value ?? "").Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var text = new string(decomposed.Where(x => CharUnicodeInfo.GetUnicodeCategory(x) != UnicodeCategory.NonSpacingMark).ToArray());
        return string.Join(' ', Regex.Matches(text, @"[\p{L}\p{N}_]+")
            .Select(x => x.Value)).SafeSubstring(0, 300);
    }

    static string SearchTokenRoot(string token)
    {
        foreach (var (suffix, minimum) in new[]
                 {
                     ("ations", 7), ("ation", 7), ("ments", 7), ("ment", 7), ("ings", 6),
                     ("ing", 6), ("ies", 5), ("ed", 5), ("es", 5), ("e", 6), ("s", 4),
                 })
        {
            if (token.Length < minimum || !token.EndsWith(suffix, StringComparison.Ordinal)) continue;
            return token[..^suffix.Length] + (suffix == "ies" ? "y" : "");
        }
        return token;
    }

    public static string SearchQueryGroupKey(string? value)
    {
        var normalized = NormalizeSearchQuery(value);
        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(x => !SearchGroupStopWords.Contains(x)).Select(SearchTokenRoot)
            .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal);
        var key = string.Join(' ', tokens);
        return key.Length > 0 ? key : normalized;
    }

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

    static string CleanSearchMarkdown(string? value)
    {
        static string CleanFragment(string fragment)
        {
            var inlineCode = new List<string>();
            fragment = Regex.Replace(fragment, @"`([^`\n]*)`", match =>
            {
                inlineCode.Add(match.Groups[1].Value);
                return $"\u0001CODE{inlineCode.Count - 1}\u0002";
            });
            fragment = Regex.Replace(fragment, @"^\s*:{3,}.*$", "", RegexOptions.Multiline);
            fragment = Regex.Replace(fragment, @"<!--[\s\S]*?-->", " ");
            fragment = Regex.Replace(fragment,
                @"<(script|style|noscript|svg|form|iframe)\b[^>]*>[\s\S]*?</\1\s*>", " ",
                RegexOptions.IgnoreCase);
            fragment = Regex.Replace(fragment,
                @"<h([1-6])\b[^>]*>([\s\S]*?)</h\1\s*>", match =>
                {
                    var level = int.Parse(match.Groups[1].Value);
                    var content = Regex.Replace(match.Groups[2].Value, "<[^>]+>", " ");
                    content = Regex.Replace(content, "\\s+", " ").Trim();
                    return $"\n{new string('#', level)} {content}\n";
                }, RegexOptions.IgnoreCase);
            fragment = Regex.Replace(fragment,
                @"</?(?:address|article|aside|blockquote|div|dl|dt|dd|fieldset|figcaption|figure|footer|header|hr|li|main|nav|ol|p|pre|section|table|tbody|td|tfoot|th|thead|tr|ul)\b[^>]*>|<br\s*/?>",
                "\n", RegexOptions.IgnoreCase);
            fragment = Regex.Replace(fragment, "<[^>]+>", " ");
            fragment = WebUtility.HtmlDecode(fragment);
            for (var i = 0; i < inlineCode.Count; i++)
                fragment = fragment.Replace($"\u0001CODE{i}\u0002", inlineCode[i]);
            return fragment;
        }

        var parts = Regex.Split(value ?? "", @"(```[\s\S]*?```|~~~[\s\S]*?~~~)");
        return string.Concat(parts.Select((part, index) => index % 2 == 1 ? "\n" : CleanFragment(part)));
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
        text = CleanSearchMarkdown(text);
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
