using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>Exports project output for an independent static server; never requires a remote grant.</summary>
public sealed class StaticProjectPublisher(ExtensionContext ctx, StaticPublishConfig settings)
{
    public const string DefaultBaseUrl = "";
    static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    static readonly Regex ReservedName = new(@"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\..*)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static StaticPublishConfig Configure(JsonObject config, string startupDirectory, StaticPublishConfig? codeConfig = null, string? webContentDirectory = null)
    {
        var value = codeConfig;
        if (value == null)
        {
            try
            {
                value = config.Deserialize<StaticPublishConfig>(ChatJson.Options) ?? new StaticPublishConfig();
            }
            catch (JsonException e)
            {
                throw new ArgumentException("Invalid share_static configuration", e);
            }
        }
        var directory = value.Directory;
        if (directory == null && (codeConfig != null || !config.ContainsKey("directory")))
            directory = Path.Combine(webContentDirectory ?? startupDirectory, "p");
        if (string.IsNullOrWhiteSpace(directory) || directory.Contains('\0'))
            throw new ArgumentException("share_static.directory must be a non-empty filesystem path");
        var basePath = value.BasePath;
        if (basePath == null || !basePath.StartsWith('/') || basePath.StartsWith("//")
            || basePath.IndexOfAny(['?', '#', '\\']) >= 0 || basePath.Any(char.IsControl)
            || basePath.Split('/').Any(p => p is "." or ".."))
            throw new ArgumentException("share_static.basePath must be an absolute URL path");
        var baseUrl = value.BaseUrl;
        baseUrl = baseUrl?.TrimEnd('/');
        if (!string.IsNullOrEmpty(baseUrl))
        {
            // Validate the original path before Uri can normalize away traversal segments.
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var url) || url.Scheme is not ("http" or "https")
                || string.IsNullOrEmpty(url.Host) || url.UserInfo.Length > 0
                || baseUrl.IndexOfAny(['?', '#', '\\']) >= 0 || baseUrl.Any(char.IsWhiteSpace)
                || baseUrl.Split('/').Any(p => p is "." or ".."))
                throw new ArgumentException("share_static.baseUrl must be an HTTP(S) URL");
            basePath = url.AbsolutePath;
        }
        // Snapshot normalized settings; do not mutate the host's configuration object.
        return new StaticPublishConfig {
            Enabled = value.Enabled,
            Directory = Path.GetFullPath(Path.Combine(startupDirectory, directory)),
            BasePath = basePath.TrimEnd('/') + "/",
            BaseUrl = baseUrl,
        };
    }

    static string Component(string? value)
    {
        if (string.IsNullOrEmpty(value) || value is "." or ".." || value.Any(char.IsControl)
            || value.IndexOfAny(['/', '\\', '<', '>', ':', '"', '|', '?', '*']) >= 0
            || value.EndsWith(' ') || value.EndsWith('.') || ReservedName.IsMatch(value))
            throw HttpError.BadRequest("Invalid static publication directory name");
        return value;
    }

    static void NoLinks(string path, string root)
    {
        path = Path.GetFullPath(path);
        root = Path.GetFullPath(root);
        if (!ProjectsExtension.IsWithin(path, root))
            throw HttpError.BadRequest("Publication path is outside its configured root");
        var current = root;
        var parts = Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        Check(current);
        foreach (var part in parts)
        {
            current = Path.Combine(current, part);
            Check(current);
        }
        static void Check(string candidate)
        {
            FileSystemInfo info = Directory.Exists(candidate) ? new DirectoryInfo(candidate) : new FileInfo(candidate);
            if (info.LinkTarget != null || info.Exists && (info.Attributes & FileAttributes.ReparsePoint) != 0)
                throw HttpError.BadRequest($"Static publishing does not support symlinks or junctions: {candidate}. Publish a build folder containing regular files instead.");
        }
    }

    static async Task CopyOutputAsync(string source, string stage, CancellationToken token)
    {
        // Enumerate each directory without following links; errors abort the whole staged publication.
        var pending = new Stack<string>();
        pending.Push(source);
        while (pending.TryPop(out var directory))
        {
            token.ThrowIfCancellationRequested();
            NoLinks(directory, source);
            var target = Path.Combine(stage, Path.GetRelativePath(source, directory));
            Directory.CreateDirectory(target);
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
            {
                token.ThrowIfCancellationRequested();
                NoLinks(entry, source);
                if (Directory.Exists(entry)) pending.Push(entry);
                else
                {
                    var destination = Path.Combine(target, Path.GetFileName(entry));
                    await using var input = new FileStream(entry, FileMode.Open, FileAccess.Read, FileShare.Read,
                        65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
                    await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write,
                        FileShare.None, 65536, FileOptions.Asynchronous);
                    await input.CopyToAsync(output, token).ConfigAwait();
                }
            }
        }
    }

    public async Task<JsonObject> PublishAsync(string? user, string projectId, CancellationToken token = default)
    {
        if (!settings.Enabled) throw HttpError.Forbidden("Static folder publishing is disabled");
        // One host owns App_Data; synchronize publishes independently from the projects metadata lock.
        var gate = Gates.GetOrAdd(Path.GetFullPath(ctx.GetUserPath(user)), _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(token).ConfigAwait();
        try
        {
            var project = ctx.Projects.GetUserProjects(user).FirstOrDefault(p => p.GetString("id") == projectId)
                ?? throw HttpError.NotFound("Project not found");
            var folder = Component(ProjectsExtension.GetProjectFolder(project));
            var username = Component(string.IsNullOrEmpty(user) ? "default" : user);
            var workspace = ctx.Projects.ResolveWorkspace(projectId, user).GetArray("directories")![0]!.GetValue<string>();
            var output = project.GetString("publish")
                ?? throw HttpError.BadRequest("No publish directory configured for the project");
            if (Path.IsPathRooted(output) || output.Split(['/', '\\']).Any(p => p == ".."))
                throw HttpError.BadRequest("Publish directory must be within the project folder");
            var source = Path.GetFullPath(Path.Combine(workspace, output.Replace('\\', Path.DirectorySeparatorChar)));
            NoLinks(source, workspace);
            if (!Directory.Exists(source)) throw HttpError.BadRequest($"Publish directory does not exist: {source}. Build the project first or select an existing Build Directory.");
            var root = settings.Directory!;
            var destination = Path.Combine(root, username, folder);
            NoLinks(destination, root);
            if (ProjectsExtension.IsWithin(destination, source) || ProjectsExtension.IsWithin(source, destination))
                throw HttpError.BadRequest($"Publication source and destination must not overlap. Source: {source}. Destination: {destination}.");
            var previous = project.GetObject("staticPublication");
            if (File.Exists(destination) || Directory.Exists(destination)
                && previous.GetString("publishedPath") != destination)
                throw HttpError.Conflict($"Destination already exists and is not this project's publication: {destination}. Choose a different static publishing directory or move the existing folder before retrying.");
            var parent = Path.GetDirectoryName(destination)!;
            Directory.CreateDirectory(parent);
            var transaction = Path.Combine(parent, ".publish-" + Guid.NewGuid().ToString("N"));
            var stage = Path.Combine(transaction, "output");
            var backup = Path.Combine(transaction, "previous");
            var installed = false;
            var committed = false;
            try
            {
                Directory.CreateDirectory(stage);
                await CopyOutputAsync(source, stage, token).ConfigAwait();
                var relativeUrl = Uri.EscapeDataString(username) + "/" + Uri.EscapeDataString(folder) + "/";
                var path = settings.BasePath + relativeUrl;
                foreach (var index in Directory.EnumerateFiles(stage).Where(p => Path.GetFileName(p).Equals("index.html", StringComparison.OrdinalIgnoreCase)))
                {
                    // Match Python's utf-8-sig contract; StreamReader would also accept UTF-16 BOMs.
                    var bytes = await File.ReadAllBytesAsync(index, token).ConfigAwait();
                    var contents = new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF');
                    await File.WriteAllTextAsync(index, RewriteIndex(contents, path), new UTF8Encoding(false), token).ConfigAwait();
                }
                var baseUrl = settings.BaseUrl;
                var result = new JsonObject {
                    ["publishedPath"] = destination, ["urlPath"] = path,
                    ["publishedUrl"] = string.IsNullOrEmpty(baseUrl) ? null : baseUrl + "/" + relativeUrl,
                    ["publishedAt"] = DateTimeOffset.UtcNow.ToString("O"),
                };
                token.ThrowIfCancellationRequested();
                NoLinks(destination, root);
                if (Directory.Exists(destination)) Directory.Move(destination, backup);
                Directory.Move(stage, destination);
                installed = true;
                await ctx.Projects.UpdateStaticPublicationAsync(project, result, user).ConfigAwait();
                committed = true;
                return result;
            }
            catch
            {
                if (installed) Directory.Delete(destination, true);
                if (Directory.Exists(backup)) Directory.Move(backup, destination);
                throw;
            }
            finally
            {
                // Retain a backup if rollback itself failed; never discard the only previous output.
                if (!Directory.Exists(backup) || committed)
                    if (Directory.Exists(transaction)) Directory.Delete(transaction, true);
            }
        }
        finally { gate.Release(); }
    }

    static readonly Regex Tags = new("""<!--[\s\S]*?(?:-->|$)|<![^>]*>|<(?<closing>/)?(?<name>[A-Za-z][A-Za-z0-9:-]*)(?=\s|/?>)(?<attrs>(?:[^'"<>]|"[^"]*"|'[^']*')*)>""", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(2));
    static readonly Regex Attributes = new("""([^\s=<>/]+)(?:\s*=\s*(?:"([^"]*)"|'([^']*)'|([^\s>]+)))?""", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(2));

    /// <summary>Preserve source markup, raw script/style text and comments while editing real attributes.</summary>
    public static string RewriteIndex(string contents, string basePath)
    {
        var tags = new List<Match>();
        var skipUntil = 0;
        foreach (Match match in Tags.Matches(contents))
        {
            if (match.Index < skipUntil || !match.Groups["name"].Success || match.Groups["closing"].Success) continue;
            tags.Add(match);
            var name = match.Groups["name"].Value.ToLowerInvariant();
            if (name is "script" or "style" or "textarea" or "title")
            {
                var close = Regex.Match(contents[(match.Index + match.Length)..], @"</" + name + @"\s*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(2));
                skipUntil = close.Success ? match.Index + match.Length + close.Index + close.Length : contents.Length;
            }
        }
        if (tags.Any(t => t.Groups["name"].Value.Equals("base", StringComparison.OrdinalIgnoreCase))) return contents;
        var edits = new List<(int Start, int Length, string Replacement)>();
        var baseTag = "\n    <base href=\"" + WebUtility.HtmlEncode(basePath) + "\">";
        var head = tags.FirstOrDefault(t => t.Groups["name"].Value.Equals("head", StringComparison.OrdinalIgnoreCase));
        if (head != null) edits.Add((head.Index + head.Length, 0, baseTag));
        else
        {
            var html = tags.FirstOrDefault(t => t.Groups["name"].Value.Equals("html", StringComparison.OrdinalIgnoreCase));
            edits.Add((html == null ? 0 : html.Index + html.Length, 0, "<head>" + baseTag + "\n</head>"));
        }
        foreach (var tag in tags)
        {
            var attrs = tag.Groups["attrs"];
            foreach (Match attr in Attributes.Matches(attrs.Value))
            {
                if (attr.Groups[1].Value.ToLowerInvariant() is not ("src" or "href")) continue;
                var group = Enumerable.Range(2, 3).Select(i => attr.Groups[i]).FirstOrDefault(g => g.Success);
                if (group == null) continue;
                var value = WebUtility.HtmlDecode(group.Value);
                if (!value.StartsWith('/') || value.StartsWith("//")) continue;
                var prefix = attr.Value[..(attr.Value.IndexOf('=') + 1)];
                edits.Add((attrs.Index + attr.Index, attr.Length, prefix + "\"" + WebUtility.HtmlEncode(value[1..]) + "\""));
            }
        }
        var result = new StringBuilder(contents);
        foreach (var edit in edits.OrderByDescending(e => e.Start))
            result.Remove(edit.Start, edit.Length).Insert(edit.Start, edit.Replacement);
        return result.ToString();
    }
}
