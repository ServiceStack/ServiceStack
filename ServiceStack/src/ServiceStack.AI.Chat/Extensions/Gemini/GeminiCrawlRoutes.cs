using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ServiceStack.Text;

namespace ServiceStack.AI;

public partial class GeminiExtension
{
    const string ImportManifest = "import.json";

    static readonly JsonObject CrawlRuleSchema = JsonNode.Parse("""
    {"title":"Crawl rules","description":"Ordered rules; the first matching rule wins.","type":"array","items":{"title":"Rule","oneOf":[{"title":"Path rule","type":"object","additionalProperties":false,"properties":{"match":{"title":"Path glob","type":"string","minLength":1,"examples":["/archives/**"],"description":"Uses the same glob syntax as folder imports."},"action":{"title":"Action","type":"string","enum":["exclude","followOnly","save"],"x-enumNames":["Exclude","Follow links only","Save page"],"default":"exclude"}},"required":["match","action"]},{"title":"Query-string rule","type":"object","additionalProperties":false,"properties":{"queryString":{"title":"Has query string","type":"boolean","const":true},"action":{"title":"Action","type":"string","enum":["exclude","followOnly","save"],"x-enumNames":["Exclude","Follow links only","Save page"],"default":"exclude"}},"required":["queryString","action"]}]}}
    """)!.AsObject();

    static readonly JsonObject TransformSchema = JsonNode.Parse("""
    {"title":"Regex transforms","description":"Applied in order to matching generated Markdown files.","type":"array","items":{"title":"Transform","type":"object","additionalProperties":false,"properties":{"match":{"title":"File glob","type":"string","default":"**/*.md","examples":["**/*.md"],"description":"Optional; defaults to every Markdown file."},"pattern":{"title":"Regex pattern","type":"string","minLength":1,"x-widget":"textarea","examples":["Version: (v\\d+)"],"description":"Example: Version: (v\\d+) captures the version for use in the replacement."},"replacement":{"title":"Replacement","type":"string","default":"","x-widget":"textarea","examples":["Release \\1"],"description":"Example: Release \\1 inserts capture group 1. Named groups can use \\g<name>."},"flags":{"title":"Flags","type":"string","default":"g","pattern":"^[gims]*$","examples":["gim"],"description":"g global, i ignore case, m multiline, s . matches lines"}},"required":["pattern"]}}
    """)!.AsObject();

    string CrawlImportsRoot(string? user) => Path.Combine(Ctx.GetUserPath(user), "gemini", "imports");

    string CrawlWorkspace(string? user, string? name)
    {
        var safe = Regex.Replace((name ?? "").Trim(), "[^A-Za-z0-9._-]+", "-").Trim('.', '-');
        if (string.IsNullOrEmpty(safe) || safe is "." or "..") throw new ArgumentException("Import name is required");
        var root = Path.GetFullPath(CrawlImportsRoot(user));
        var path = Path.GetFullPath(Path.Combine(root, safe));
        if (!GeminiIngest.WithinRoots(path, [root])) throw new ArgumentException("Import path escapes the user's imports folder");
        return path;
    }

    internal static JsonObject ReadImportJson(string path)
    {
        if (!File.Exists(path)) return new JsonObject();
        try { return ChatJson.TryParseObject(File.ReadAllText(path)) ?? new JsonObject(); }
        catch (Exception e) { throw new ArgumentException($"Invalid {path}: {e.Message}", e); }
    }

    internal static async Task WriteImportJsonAsync(string path, JsonObject value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".tmp";
        await File.WriteAllTextAsync(temp, value.ToJsonString(ChatJson.Indented) + "\n").ConfigAwait();
        File.Move(temp, path, true);
    }

    internal static JsonObject MergeImportMetadata(JsonObject? parent, JsonObject? child)
    {
        parent ??= new JsonObject(); child ??= new JsonObject();
        var defaults = parent.GetObject("defaults")?.Clone() ?? new JsonObject();
        foreach (var (key, value) in child.GetObject("defaults") ?? []) defaults[key] = value?.DeepClone();
        var rules = new JsonArray();
        foreach (var value in parent.GetArray("rules") ?? []) rules.Add(value?.DeepClone());
        foreach (var value in child.GetArray("rules") ?? []) rules.Add(value?.DeepClone());
        var ret = new JsonObject { ["defaults"] = defaults, ["rules"] = rules };
        if (child["frontmatter"] != null) ret["frontmatter"] = child["frontmatter"]!.DeepClone();
        else if (parent["frontmatter"] != null) ret["frontmatter"] = parent["frontmatter"]!.DeepClone();
        return ret;
    }

    internal static JsonObject EffectiveImportMetadata(string root, string relativeFile)
    {
        var merged = new JsonObject(); var current = Path.GetFullPath(root);
        var directories = (Path.GetDirectoryName(relativeFile.Replace('/', Path.DirectorySeparatorChar)) ?? "")
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in new string?[] { null }.Concat(directories)) {
            if (part != null) current = Path.Combine(current, part);
            var config = ReadImportJson(Path.Combine(current, ImportManifest));
            merged = MergeImportMetadata(merged, config.GetObject("metadata"));
        }
        return merged;
    }

    internal static async Task SaveImportMetadataAsync(string root, JsonObject? metadata)
    {
        var path = Path.Combine(GeminiIngest.ResolvePath(root), ImportManifest); var config = ReadImportJson(path);
        config["version"] ??= 1; config["metadata"] = metadata?.DeepClone() ?? new JsonObject
            { ["defaults"] = new JsonObject(), ["rules"] = new JsonArray() };
        await WriteImportJsonAsync(path, config).ConfigAwait();
    }

    static List<string> GeneratedPages(string root)
    {
        if (!Directory.Exists(root)) return [];
        var generated = GeminiMetadata.AsList(ReadImportJson(Path.Combine(root, ImportManifest)).GetObject("crawl")?["generated"]);
        return generated.Select(x => x.Replace('\\', '/').TrimStart('/')).Where(rel => rel.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .Where(rel => { var full = Path.GetFullPath(Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar))); return GeminiIngest.WithinRoots(full, [root]) && File.Exists(full); })
            .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();
    }

    Task<object?> ListCrawlImportsAsync(ChatRequestContext req)
    {
        var root = CrawlImportsRoot(UserOf(req)); Directory.CreateDirectory(root);
        var ret = new JsonArray(Directory.EnumerateDirectories(root).OrderBy(x => x).Select(path => (JsonNode)new JsonObject
        {
            ["name"] = Path.GetFileName(path), ["path"] = path, ["pages"] = GeneratedPages(path).Count,
            ["config"] = ReadImportJson(Path.Combine(path, ImportManifest)),
        }).ToArray());
        return Task.FromResult<object?>(ret);
    }

    Task<object?> CrawlImportSchemaAsync(ChatRequestContext req) => Task.FromResult<object?>(new JsonObject
    { ["rules"] = CrawlRuleSchema.DeepClone(), ["transforms"] = TransformSchema.DeepClone() });

    Task<object?> GetCrawlImportAsync(ChatRequestContext req)
    {
        var name = req.GetPathParam("name"); var path = CrawlWorkspace(UserOf(req), name);
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException("Import does not exist");
        return Task.FromResult<object?>(new JsonObject { ["name"] = name, ["path"] = path,
            ["pages"] = GeneratedPages(path).Count, ["config"] = ReadImportJson(Path.Combine(path, ImportManifest)) });
    }

    Task<object?> ListCrawlPagesAsync(ChatRequestContext req)
    {
        var path = CrawlWorkspace(UserOf(req), req.GetPathParam("name"));
        return Task.FromResult<object?>(new JsonObject { ["pages"] = new JsonArray(GeneratedPages(path).Select(x => (JsonNode)x).ToArray()) });
    }

    Task<object?> GetCrawlPageAsync(ChatRequestContext req)
    {
        var root = CrawlWorkspace(UserOf(req), req.GetPathParam("name"));
        var rel = (req.QueryString("path") ?? "").Replace('\\', '/').TrimStart('/');
        if (!GeneratedPages(root).Contains(rel)) throw new ArgumentException("Crawled page was not found");
        var full = Path.GetFullPath(Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar)));
        if (!GeminiIngest.WithinRoots(full, [root]) || !File.Exists(full)) throw new ArgumentException("Crawled page was not found");
        return Task.FromResult<object?>(new JsonObject { ["path"] = rel, ["content"] = File.ReadAllText(full) });
    }

    async Task<object?> SaveCrawlConfigAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait(); var name = req.GetPathParam("name");
        var path = CrawlWorkspace(UserOf(req), name); if (!Directory.Exists(path)) throw new DirectoryNotFoundException("Import does not exist");
        var config = await req.GetJsonBodyAsync().ConfigAwait(); await WriteImportJsonAsync(Path.Combine(path, ImportManifest), config).ConfigAwait();
        return new JsonObject { ["name"] = name, ["path"] = path, ["config"] = config };
    }

    static RegexOptions TransformOptions(string flags) => RegexOptions.CultureInvariant
        | (flags.Contains('i') ? RegexOptions.IgnoreCase : 0) | (flags.Contains('m') ? RegexOptions.Multiline : 0)
        | (flags.Contains('s') ? RegexOptions.Singleline : 0);

    static string DotNetReplacement(string replacement, Regex regex, int index)
    {
        foreach (Match m in Regex.Matches(replacement, @"\\(?:([1-9]\d*)|g<([A-Za-z_]\w*)>)"))
        {
            var number = m.Groups[1].Value; var name = m.Groups[2].Value;
            if (number.Length > 0 && !regex.GetGroupNumbers().Contains(int.Parse(number)))
                throw new ArgumentException($"Regex transform {index} replacement is invalid: invalid group reference {number} at position {m.Index + 1}");
            if (name.Length > 0 && !regex.GetGroupNames().Contains(name))
                throw new ArgumentException($"Regex transform {index} replacement is invalid: unknown group name '{name}'");
        }
        return Regex.Replace(replacement, @"\\(?:([1-9]\d*)|g<([A-Za-z_]\w*)>)", m =>
            m.Groups[1].Success ? "$" + m.Groups[1].Value : "${" + m.Groups[2].Value + "}");
    }

    static List<(string? Match, Regex Regex, string Replacement, bool Global)> ValidateTransforms(JsonArray transforms)
    {
        var ret = new List<(string?, Regex, string, bool)>(); var index = 0;
        foreach (var node in transforms) {
            index++; if (node is not JsonObject rule) throw new ArgumentException($"Regex transform {index} must be an object");
            var pattern = rule.GetString("pattern"); if (string.IsNullOrEmpty(pattern)) throw new ArgumentException($"Regex transform {index} needs a pattern");
            var flags = rule.GetString("flags") ?? "g"; if (Regex.IsMatch(flags, "[^gims]")) throw new ArgumentException($"Regex transform {index} has unsupported flags");
            Regex regex; try { regex = new Regex(pattern, TransformOptions(flags)); } catch (ArgumentException e) { throw new ArgumentException($"Regex transform {index} is invalid: {e.Message}", e); }
            ret.Add((rule.GetString("match"), regex, DotNetReplacement(rule.GetString("replacement") ?? "", regex, index), flags.Contains('g')));
        }
        return ret;
    }

    async Task<object?> TransformCrawlImportAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait(); var name = req.GetPathParam("name"); var root = CrawlWorkspace(UserOf(req), name);
        var configPath = Path.Combine(root, ImportManifest); var config = ReadImportJson(configPath); var body = await req.GetJsonBodyAsync().ConfigAwait();
        var transforms = body.GetArray("transforms") ?? config.GetArray("transforms") ?? new JsonArray(); var validated = ValidateTransforms(transforms); var changed = 0;
        foreach (var full in Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories)) {
            var rel = Path.GetRelativePath(root, full).Replace('\\', '/'); var text = await File.ReadAllTextAsync(full).ConfigAwait(); var updated = text;
            foreach (var rule in validated) { if (rule.Match != null && !GeminiIngest.GlobMatch(rel, rule.Match)) continue; updated = rule.Global ? rule.Regex.Replace(updated, rule.Replacement) : rule.Regex.Replace(updated, rule.Replacement, 1); }
            if (updated != text) { await File.WriteAllTextAsync(full, updated).ConfigAwait(); changed++; }
        }
        config["transforms"] = transforms.DeepClone(); await WriteImportJsonAsync(configPath, config).ConfigAwait();
        return new JsonObject { ["name"] = name, ["path"] = root, ["changed"] = changed, ["config"] = config };
    }

    static string SiteName(Uri uri) => Regex.Replace(uri.Authority.ToLowerInvariant().Replace(':', '-'), "[^a-z0-9._-]+", "-").Trim('.', '-');
    static string PageRelativePath(Uri uri)
    {
        var path = Uri.UnescapeDataString(uri.AbsolutePath).Trim('/'); string stem;
        if (path.Length == 0) stem = "index"; else if (uri.AbsolutePath.EndsWith('/')) stem = path + "/index"; else stem = path[..^Path.GetExtension(path).Length];
        if (uri.Query.Length > 1) stem += "--" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uri.Query[1..]))).ToLowerInvariant()[..10];
        return stem + ".md";
    }

    static string? Attr(string attrs, string name)
    {
        var pattern = "\\b" + Regex.Escape(name) + "\\s*=\\s*(?:\"(?<v>[^\"]*)\"|'(?<v>[^']*)')";
        var match = Regex.Match(attrs, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? WebUtility.HtmlDecode(match.Groups["v"].Value) : null;
    }
    static (string Title, string Description, List<string> Tags, HashSet<string> Robots, string? Canonical, List<(string Url,bool NoFollow)> Links) ParsePage(string html)
    {
        var title = WebUtility.HtmlDecode(Regex.Match(html, "<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups[1].Value).Trim();
        var description = ""; var tags = new List<string>(); var robots = new HashSet<string>(StringComparer.OrdinalIgnoreCase); string? canonical = null; var links = new List<(string,bool)>();
        foreach (Match m in Regex.Matches(html, "<(meta|link|a)\\b(?<a>[^>]*)>", RegexOptions.IgnoreCase | RegexOptions.Singleline)) {
            var tag = m.Groups[1].Value.ToLowerInvariant(); var attrs = m.Groups["a"].Value; var rel = Attr(attrs, "rel") ?? "";
            if (tag == "a" && Attr(attrs, "href") is { } href) links.Add((href, rel.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("nofollow", StringComparer.OrdinalIgnoreCase)));
            else if (tag == "link" && rel.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("canonical", StringComparer.OrdinalIgnoreCase)) canonical = Attr(attrs, "href");
            else if (tag == "meta") { var key = (Attr(attrs,"name") ?? Attr(attrs,"property") ?? "").ToLowerInvariant(); var content = Attr(attrs,"content") ?? ""; if (description.Length == 0 && key is "description" or "og:description") description = content; if (key is "keywords" or "tags") tags.AddRange(content.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)); if (key is "robots" or "googlebot") foreach (var x in content.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) robots.Add(x); }
        }
        return (title, description, tags, robots, canonical, links);
    }

    static bool GlobAny(string value, JsonArray? patterns) => patterns?.Any(x => x != null && GeminiIngest.GlobMatch(value, x.GetValue<string>())) == true;
    static string CrawlAction(Uri uri, JsonObject options) { foreach (var rule in options.GetArray("rules")?.OfType<JsonObject>() ?? []) { if (rule.GetString("match") is { } glob && !GeminiIngest.GlobMatch(uri.AbsolutePath, glob)) continue; if (rule.ContainsKey("queryString") && rule.GetBool("queryString") != (uri.Query.Length > 1)) continue; return rule.GetString("action") ?? "save"; } return "save"; }

    static void ValidateCrawlRules(JsonArray? rules)
    {
        var index = 0;
        foreach (var node in rules ?? [])
        {
            index++; if (node is not JsonObject rule) throw new ArgumentException($"Crawl rule {index} must be an object");
            if (rule.GetString("action") is not ("exclude" or "followOnly" or "save")) throw new ArgumentException($"Crawl rule {index} has an invalid action");
            if (string.IsNullOrEmpty(rule.GetString("match")) && !rule.GetBool("queryString")) throw new ArgumentException($"Crawl rule {index} needs a path glob or Has query string");
        }
    }

    static async Task<List<(bool Allow, string Path)>> LoadRobotsAsync(HttpClient http, Uri start)
    {
        var rules = new List<(bool, string)>();
        try
        {
            var robotsUri = new UriBuilder(start) { Path = "/robots.txt", Query = "", Fragment = "" }.Uri;
            using var response = await http.GetAsync(robotsUri).ConfigAwait();
            if (!response.IsSuccessStatusCode) return rules;
            var applies = false;
            foreach (var raw in (await response.Content.ReadAsStringAsync().ConfigAwait()).Split('\n'))
            {
                var line = raw.Split('#', 2)[0].Trim(); var colon = line.IndexOf(':'); if (colon < 0) continue;
                var key = line[..colon].Trim().ToLowerInvariant(); var value = line[(colon + 1)..].Trim();
                if (key == "user-agent") applies = value == "*" || "llms-gemini-crawler".Contains(value, StringComparison.OrdinalIgnoreCase);
                else if (applies && key is "allow" or "disallow" && value.Length > 0) rules.Add((key == "allow", value));
            }
        }
        catch { /* An unavailable robots.txt is permissive, matching RobotFileParser behavior. */ }
        return rules;
    }

    static bool RobotsAllow(Uri uri, List<(bool Allow, string Path)> rules)
    {
        var match = rules.Where(x => uri.PathAndQuery.StartsWith(x.Path, StringComparison.Ordinal))
            .OrderByDescending(x => x.Path.Length).ThenByDescending(x => x.Allow).FirstOrDefault();
        return match.Path == null || match.Allow;
    }

    static Uri? CanonicalUrl(string raw, Uri start, JsonObject options)
    {
        if (!Uri.TryCreate(start, raw, out var uri) || uri.Scheme is not ("http" or "https")) return null;
        var allowed = GeminiMetadata.AsList(options["allowedHosts"]); if (options.GetBool("sameOrigin", true) && uri.GetLeftPart(UriPartial.Authority) != start.GetLeftPart(UriPartial.Authority) && !allowed.Contains(uri.Authority, StringComparer.OrdinalIgnoreCase)) return null;
        var basePath = start.AbsolutePath.TrimEnd('/'); if (basePath.Length > 0 && uri.AbsolutePath != basePath && !uri.AbsolutePath.StartsWith(basePath + "/", StringComparison.Ordinal)) return null;
        if (options.GetArray("include") is { Count: > 0 } include && !GlobAny(uri.AbsolutePath, include) || GlobAny(uri.AbsolutePath, options.GetArray("exclude"))) return null;
        var query = options.GetObject("query") ?? new JsonObject(); var mode = query.GetString("mode") ?? "ignore"; var pairs = System.Web.HttpUtility.ParseQueryString(uri.Query); var kept = new List<(string,string)>();
        if (mode != "ignore") foreach (var key in pairs.AllKeys.Where(x => x != null).Cast<string>()) foreach (var value in pairs.GetValues(key) ?? [""]) { if (mode == "allow" && !GeminiMetadata.AsList(query["allow"]).Any(x => GeminiIngest.GlobMatch(key,x))) continue; var excludes=GeminiMetadata.AsList(query["exclude"]); if ((excludes.Count>0?excludes:["utm_*","fbclid","gclid","ref","session","token"]).Any(x => GeminiIngest.GlobMatch(key,x))) continue; kept.Add((key,value)); }
        var builder = new UriBuilder(uri) { Fragment = "", Query = string.Join("&", kept.OrderBy(x=>x.Item1).ThenBy(x=>x.Item2).Select(x => Uri.EscapeDataString(x.Item1)+"="+Uri.EscapeDataString(x.Item2))) }; return builder.Uri;
    }

    async Task<object?> StartCrawlAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait(); var supplied = await req.GetJsonBodyAsync().ConfigAwait(); var startText = supplied.GetString("url")?.Trim();
        if (!Uri.TryCreate(startText, UriKind.Absolute, out var start) || start.Scheme is not ("http" or "https")) throw new ArgumentException("A valid http:// or https:// URL is required");
        var name = supplied.GetString("name") ?? SiteName(start); var root = CrawlWorkspace(UserOf(req), name); Directory.CreateDirectory(root); var configPath = Path.Combine(root, ImportManifest); var config = ReadImportJson(configPath); var previous = GeminiMetadata.AsList(config.GetObject("crawl")?["generated"]).ToHashSet();
        var options = new JsonObject { ["sameOrigin"] = true, ["respectRobots"] = true, ["respectNoIndex"] = true, ["followNoFollow"] = false, ["useCanonical"] = true, ["dedupeContent"] = true, ["contentTypes"] = new JsonArray("text/html") };
        foreach (var (key,value) in supplied) options[key] = value?.DeepClone(); options["url"] = startText; options["name"] = name; ValidateCrawlRules(options.GetArray("rules"));
        var maxPages = Math.Clamp(options.GetInt("maxPages") ?? 500, 1, 10000); var maxDepth = Math.Clamp(options.GetInt("maxDepth") ?? 10, 0, 100); var maxRequests = Math.Clamp(options.GetInt("maxRequests") ?? maxPages * 5, maxPages, 50000);
        var startUrl = CanonicalUrl(startText!, start, options) ?? throw new ArgumentException("The start URL is excluded by the crawl rules"); var queue = new Queue<(Uri,int)>(); queue.Enqueue((startUrl,0)); var queued = new HashSet<string>{startUrl.AbsoluteUri}; var seen = new HashSet<string>(); var pages = new List<string>(); var saved = new HashSet<string>(); var hashes = new HashSet<string>(); var variants = new Dictionary<string,int>(); var requests = 0;
        using var http = Ctx.Feature.HttpClientFactory.CreateClient(); http.Timeout = TimeSpan.FromSeconds(30); http.DefaultRequestHeaders.UserAgent.ParseAdd("llms-gemini-crawler/1.0");
        var robots = options.GetBool("respectRobots", true) ? await LoadRobotsAsync(http, startUrl).ConfigAwait() : [];
        while (queue.Count > 0 && pages.Count < maxPages && requests < maxRequests) { var (url,depth)=queue.Dequeue(); if (!seen.Add(url.AbsoluteUri) || CrawlAction(url,options)=="exclude" || !RobotsAllow(url, robots)) continue; requests++; using var response = await http.GetAsync(url).ConfigAwait(); if (!response.IsSuccessStatusCode || response.Content.Headers.ContentType?.MediaType is not "text/html") continue; var final = CanonicalUrl(response.RequestMessage!.RequestUri!.AbsoluteUri,start,options); if (final == null) continue; var html = await response.Content.ReadAsStringAsync().ConfigAwait(); var parsed = ParsePage(html); var pageUrl = options.GetBool("useCanonical",true) && parsed.Canonical != null ? CanonicalUrl(parsed.Canonical,final,options) ?? final : final; var text = new HtmlToMarkdownParser(includeLinks:false).Parse(html).Trim(); var digest = GeminiIngest.Sha256(text); var action = CrawlAction(url,options);
            if (action != "followOnly" && !(options.GetBool("respectNoIndex",true) && parsed.Robots.Contains("noindex")) && text.Length > 0 && saved.Add(pageUrl.AbsoluteUri) && (!options.GetBool("dedupeContent",true) || hashes.Add(digest))) { var rel=PageRelativePath(pageUrl); var full=Path.Combine(root,rel.Replace('/',Path.DirectorySeparatorChar)); Directory.CreateDirectory(Path.GetDirectoryName(full)!); var meta=new JsonObject{{"title",parsed.Title},{"sourceUrl",pageUrl.AbsoluteUri},{"path",pageUrl.AbsolutePath},{"queryString",pageUrl.Query.TrimStart('?')},{"description",parsed.Description},{"tags",new JsonArray(parsed.Tags.Select(x=>(JsonNode)x).ToArray())}}; var front="---\n"+string.Join("\n",meta.Where(x=>x.Value != null && x.Value.ToJsonString() is not "\"\"" and not "[]").Select(x=>$"{x.Key}: {x.Value!.ToJsonString()}"))+"\n---\n"; await File.WriteAllTextAsync(full,front+text+"\n").ConfigAwait(); pages.Add(rel); }
            if (depth >= maxDepth || parsed.Robots.Contains("nofollow") && !options.GetBool("followNoFollow")) continue; foreach (var link in parsed.Links) { if (link.NoFollow && !options.GetBool("followNoFollow")) continue; var clean=CanonicalUrl(link.Url,final,options); if (clean==null || seen.Contains(clean.AbsoluteUri) || !queued.Add(clean.AbsoluteUri) || CrawlAction(clean,options)=="exclude") continue; if (clean.Query.Length>1) { var key=clean.GetLeftPart(UriPartial.Path); var limit=options.GetObject("query")?.GetInt("maxVariantsPerPath")??5; if (variants.GetValueOrDefault(key)>=limit) continue; variants[key]=variants.GetValueOrDefault(key)+1; } queue.Enqueue((clean,depth+1)); }
        }
        foreach (var rel in previous.Except(pages)) { var full=Path.GetFullPath(Path.Combine(root,rel.Replace('/',Path.DirectorySeparatorChar))); if (GeminiIngest.WithinRoots(full,[root]) && File.Exists(full)) File.Delete(full); }
        var crawl = config.GetObject("crawl") ?? new JsonObject(); foreach (var (key,value) in options) crawl[key]=value?.DeepClone(); crawl["generated"]=new JsonArray(pages.Select(x=>(JsonNode)x).ToArray()); config["version"]=1; config["crawl"]=crawl; config["metadata"]??=new JsonObject{{"defaults",new JsonObject()},{"rules",new JsonArray()}}; config["transforms"]??=new JsonArray(); await WriteImportJsonAsync(configPath,config).ConfigAwait(); return new JsonObject{{"name",name},{"path",root},{"pages",pages.Count},{"config",config}};
    }
}
