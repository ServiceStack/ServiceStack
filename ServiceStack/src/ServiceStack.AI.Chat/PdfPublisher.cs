using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Promotes a template from a user's PDF Designer folder into the shared App_Data/pdf folder.
/// <para>
/// Published templates are flat — App_Data/pdf/&lt;name&gt;.typ with its data, schema and assets
/// beside it — so a template authored in a subfolder has its references rewritten on the way in.
/// Who published what is recorded in .published.json, which is also how the Admin UI links back to
/// the document in the designer.
/// </para>
/// </summary>
public partial class PdfPublisher(PdfFeature feature)
{
    /// <summary>Files a template reads: json("x.json"), image("logo.png"), #import "lib.typ", …</summary>
    [GeneratedRegex(@"\b(?<fn>json|yaml|toml|csv|xml|cbor|read|image|bibliography|load-data)\s*\(\s*""(?<arg>[^""]+)""" +
                    @"|#?\b(?<kw>include|import)\s+""(?<path>[^""]+)""")]
    private static partial Regex ResourceRegex();

    /// <summary>Manifest of what's published, kept out of listings by its leading dot</summary>
    public const string ManifestFile = ".published.json";

    /// <summary>Immutable published snapshots, kept out of template listings by the leading dot.</summary>
    public const string VersionsDir = ".versions";

    /// <summary>Metadata stored beside every immutable revision.</summary>
    public const string RevisionFile = "revision.json";

    /// <summary>How deep #include/#import chains are followed when collecting a template's files</summary>
    public const int MaxDepth = 8;

    public string PdfPath => feature.PdfPath
        ?? throw new Exception($"{nameof(PdfFeature)}.{nameof(PdfFeature.PdfPath)} is not configured");

    // ── Manifest ──

    public JsonObject GetManifest()
    {
        var path = Path.Combine(PdfPath, ManifestFile);
        if (!File.Exists(path))
            return new JsonObject();
        try
        {
            return ChatJson.TryParseObject(File.ReadAllText(path)) ?? new JsonObject();
        }
        catch (Exception e)
        {
            feature.Log.LogWarning(e, "Could not read {Manifest}", path);
            return new JsonObject();
        }
    }

    public void SaveManifest(JsonObject manifest) =>
        File.WriteAllText(Path.Combine(PdfPath, ManifestFile), manifest.ToJsonString(ChatJson.Indented));

    // ── Publish ──

    public class PublishResult
    {
        public string Name { get; set; } = null!;
        public List<string> Files { get; set; } = [];
        public List<string> Warnings { get; set; } = [];
        public bool LibUpdated { get; set; }
    }

    public class Revision
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string? PublishedBy { get; set; }
        public string? Source { get; set; }
        public string Action { get; set; } = "publish";
        public string? RestoredFrom { get; set; }
        public List<string> Files { get; set; } = [];
    }

    /// <summary>The existing owner of a published name, or null when it's free</summary>
    public JsonObject? GetOwner(string name)
    {
        if (!File.Exists(Path.Combine(PdfPath, name + ".typ")))
            return null;
        return GetManifest().GetObject(name) ?? new JsonObject();
    }

    /// <summary>
    /// Copy relPath (and everything it needs) out of srcRoot into App_Data/pdf as `name`.
    /// Nothing is written until the whole set is collected and rewritten.
    /// </summary>
    public PublishResult Publish(string srcRoot, string relPath, string name, string? user, bool publishLib = true)
    {
        var to = new PublishResult { Name = name };
        var srcTyp = PdfExtension.Resolve(srcRoot, relPath, mustExist: true);
        if (!relPath.EndsWith(".typ", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only .typ templates can be published");

        // 1. collect: the template, its companions, and whatever they reference
        var sources = CollectSources(srcRoot, relPath, to.Warnings);

        // 2. name each source file as it will land in the flat published folder
        var flatMap = BuildFlatMap(sources, relPath, name);

        // 3. rewrite references in every .typ being published, then stage the bytes
        var staged = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var srcRel in sources)
        {
            var flatName = flatMap[srcRel];
            if (flatName == PdfExtension.LibName && !publishLib && File.Exists(Path.Combine(PdfPath, flatName)))
                continue;

            var fullPath = PdfExtension.Resolve(srcRoot, srcRel);
            staged[flatName] = srcRel.EndsWith(".typ", StringComparison.OrdinalIgnoreCase)
                ? RewriteReferences(File.ReadAllText(fullPath), srcRel, flatMap, to.Warnings).ToUtf8Bytes()
                : File.ReadAllBytes(fullPath);
        }

        // 4. lib.typ is shared by every published template, so say so when it changes
        if (staged.TryGetValue(PdfExtension.LibName, out var libBytes))
        {
            var libPath = Path.Combine(PdfPath, PdfExtension.LibName);
            if (File.Exists(libPath) && !File.ReadAllBytes(libPath).AsSpan().SequenceEqual(libBytes))
            {
                var others = GetPublishedNames().Where(x => x != name).ToList();
                if (others.Count > 0)
                {
                    to.LibUpdated = true;
                    to.Warnings.Add($"{PdfExtension.LibName} was updated — {others.Count} other published " +
                                    $"template(s) share it: {string.Join(", ", others)}");
                }
            }
        }

        // 5. write
        Directory.CreateDirectory(PdfPath);
        foreach (var (flatName, bytes) in staged)
        {
            File.WriteAllBytes(Path.Combine(PdfPath, flatName), bytes);
            to.Files.Add(flatName);
        }
        to.Files.Sort(StringComparer.Ordinal);

        // 6. record who published what, and where it came from
        var manifest = GetManifest();
        manifest[name] = new JsonObject
        {
            ["user"] = user,
            ["source"] = relPath,
            ["publishedAt"] = DateTime.UtcNow.ToString("O"),
            ["files"] = new JsonArray(to.Files.Select(x => (JsonNode)x).ToArray()),
        };
        SaveManifest(manifest);

        return to;
    }

    /// <summary>
    /// The published files a publish is about to overwrite (&lt;name&gt;.* plus the shared library),
    /// so a template that turns out not to compile can be put back the way it was.
    /// </summary>
    public Dictionary<string, byte[]> Snapshot(string name)
    {
        var to = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        if (!Directory.Exists(PdfPath))
            return to;
        foreach (var path in Directory.EnumerateFiles(PdfPath, name + ".*")
                     .Append(Path.Combine(PdfPath, PdfExtension.LibName))
                     .Append(Path.Combine(PdfPath, ManifestFile)))
        {
            if (File.Exists(path))
                to[Path.GetFileName(path)] = File.ReadAllBytes(path);
        }
        return to;
    }

    /// <summary>Undo a publish: restore what Snapshot captured and drop anything it added</summary>
    public void Restore(string name, Dictionary<string, byte[]> snapshot)
    {
        foreach (var path in Directory.EnumerateFiles(PdfPath, name + ".*"))
        {
            if (!snapshot.ContainsKey(Path.GetFileName(path)))
                File.Delete(path);
        }
        foreach (var (fileName, bytes) in snapshot)
        {
            File.WriteAllBytes(Path.Combine(PdfPath, fileName), bytes);
        }

        // New snapshots include the manifest, so restoring its bytes already restores ownership and
        // currentRevision. Keep the fallback for snapshots made by older callers.
        if (!snapshot.ContainsKey(ManifestFile))
        {
            var manifest = GetManifest();
            if (snapshot.Keys.All(x => x != name + ".typ") && manifest.Remove(name))
                SaveManifest(manifest);
        }
    }

    /// <summary>
    /// Store the current live files as an immutable filesystem revision. The directory is staged and
    /// renamed only after every file and its metadata have been written.
    /// </summary>
    public Revision SaveRevision(string name, string? user, string? source, IEnumerable<string> files,
        string action = "publish", string? restoredFrom = null)
    {
        AssertRevisionName(name);
        var now = DateTime.UtcNow;
        var id = $"{now:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}"[..38];
        var templateDir = Path.Combine(PdfPath, VersionsDir, name);
        var finalDir = Path.Combine(templateDir, id);
        var stagingDir = finalDir + ".tmp";
        Directory.CreateDirectory(stagingDir);

        var revision = new Revision
        {
            Id = id,
            Name = name,
            CreatedAt = now,
            PublishedBy = user,
            Source = source,
            Action = action,
            RestoredFrom = restoredFrom,
        };

        try
        {
            foreach (var fileName in files.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.Ordinal))
            {
                if (fileName != Path.GetFileName(fileName) || fileName is ManifestFile or RevisionFile)
                    continue;
                var sourcePath = Path.Combine(PdfPath, fileName);
                if (!File.Exists(sourcePath))
                    continue;
                File.Copy(sourcePath, Path.Combine(stagingDir, fileName));
                revision.Files.Add(fileName);
            }

            File.WriteAllText(Path.Combine(stagingDir, RevisionFile), ChatJson.Serialize(revision));
            Directory.CreateDirectory(templateDir);
            Directory.Move(stagingDir, finalDir);

            var manifest = GetManifest();
            var entry = manifest.GetObject(name) ?? new JsonObject();
            entry["currentRevision"] = id;
            entry["publishedAt"] = now.ToString("O");
            entry["user"] = user;
            entry["source"] = source;
            entry["files"] = new JsonArray(revision.Files.Select(x => (JsonNode)x).ToArray());
            manifest[name] = entry;
            SaveManifest(manifest);
            return revision;
        }
        catch
        {
            if (Directory.Exists(stagingDir))
                Directory.Delete(stagingDir, recursive: true);
            if (Directory.Exists(finalDir))
                Directory.Delete(finalDir, recursive: true);
            throw;
        }
    }

    public List<Revision> GetRevisions(string name)
    {
        AssertRevisionName(name);
        var templateDir = Path.Combine(PdfPath, VersionsDir, name);
        if (!Directory.Exists(templateDir))
            return [];

        var revisions = new List<Revision>();
        foreach (var dir in Directory.EnumerateDirectories(templateDir).OrderByDescending(x => x, StringComparer.Ordinal))
        {
            var metadataPath = Path.Combine(dir, RevisionFile);
            if (!File.Exists(metadataPath))
                continue;
            try
            {
                if (ChatJson.Deserialize<Revision>(File.ReadAllText(metadataPath)) is { } revision)
                    revisions.Add(revision);
            }
            catch (Exception e)
            {
                feature.Log.LogWarning(e, "Could not read PDF revision {Revision}", dir);
            }
        }
        return revisions;
    }

    /// <summary>Activate an immutable snapshot and record that activation as a new revision.</summary>
    public (Revision Revision, List<string> Warnings) Rollback(string name, string revisionId, string? user)
    {
        AssertRevisionName(name);
        if (!ValidRevisionId(revisionId))
            throw new ArgumentException($"Invalid PDF revision '{revisionId}'");
        var revisionDir = Path.Combine(PdfPath, VersionsDir, name, revisionId);
        var metadataPath = Path.Combine(revisionDir, RevisionFile);
        if (!File.Exists(metadataPath))
            throw new FileNotFoundException($"PDF revision '{revisionId}' does not exist", metadataPath);
        var target = ChatJson.Deserialize<Revision>(File.ReadAllText(metadataPath))
            ?? throw new InvalidDataException($"PDF revision '{revisionId}' has invalid metadata");
        if (!target.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"PDF revision '{revisionId}' belongs to another template");

        var snapshot = Snapshot(name);
        try
        {
            var currentFiles = GetManifestFiles(GetManifest().GetObject(name));
            var targetFiles = target.Files.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var usedByOthers = FilesUsedByOtherTemplates(name);
            foreach (var fileName in currentFiles.Where(x => !targetFiles.Contains(x) && !usedByOthers.Contains(x)))
            {
                if (fileName == Path.GetFileName(fileName))
                    File.Delete(Path.Combine(PdfPath, fileName));
            }
            foreach (var fileName in target.Files)
            {
                if (fileName != Path.GetFileName(fileName) || fileName is ManifestFile or RevisionFile)
                    throw new InvalidDataException($"PDF revision '{revisionId}' contains an invalid file name");
                File.Copy(Path.Combine(revisionDir, fileName), Path.Combine(PdfPath, fileName), overwrite: true);
            }

            var warnings = new List<string>();
            if (target.Files.Contains(PdfExtension.LibName, StringComparer.OrdinalIgnoreCase) &&
                GetPublishedNames().Any(x => !x.Equals(name, StringComparison.OrdinalIgnoreCase)))
                warnings.Add($"{PdfExtension.LibName} was restored and may affect other published templates");

            return (SaveRevision(name, user, target.Source, target.Files, "rollback", revisionId), warnings);
        }
        catch
        {
            Restore(name, snapshot);
            throw;
        }
    }

    static bool ValidRevisionId(string value) => value.Length is >= 20 and <= 80 &&
        value.All(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_');

    static void AssertRevisionName(string name)
    {
        if (string.IsNullOrEmpty(name) || name != Path.GetFileName(name) || name.StartsWith('.') ||
            name.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '.' or '_' or '-')))
            throw new ArgumentException($"Invalid PDF template name '{name}'");
    }

    HashSet<string> FilesUsedByOtherTemplates(string name)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { PdfExtension.LibName };
        foreach (var other in GetManifest().Where(x => !x.Key.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            if (other.Value is JsonObject entry)
                used.UnionWith(GetManifestFiles(entry));
        }
        return used;
    }

    /// <summary>
    /// Removes files owned only by this template. Files claimed by another manifest entry (shared
    /// assets/partials) and the shared library are retained.
    /// </summary>
    public List<string> Unpublish(string name)
    {
        if (PdfRenderer.ReservedNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"'{name}' is the shared library, not a template");

        var manifest = GetManifest();
        var files = GetManifestFiles(manifest.GetObject(name));
        // Support manifests created before the files list existed, while still protecting files
        // claimed by any other template below.
        if (files.Count == 0 && Directory.Exists(PdfPath))
            files.UnionWith(Directory.EnumerateFiles(PdfPath, name + ".*").Select(Path.GetFileName));

        var usedByOthers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PdfExtension.LibName,
        };
        foreach (var other in manifest.Where(x => !x.Key.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            if (other.Value is JsonObject otherEntry)
                usedByOthers.UnionWith(GetManifestFiles(otherEntry));
        }

        var deleted = new List<string>();
        foreach (var fileName in files)
        {
            // A hand-edited manifest must not be able to turn this into a traversal delete.
            if (fileName != Path.GetFileName(fileName) || usedByOthers.Contains(fileName))
                continue;
            var path = Path.Combine(PdfPath, fileName);
            if (!File.Exists(path))
                continue;
            File.Delete(path);
            deleted.Add(fileName);
        }

        if (manifest.Remove(name))
            SaveManifest(manifest);

        deleted.Sort(StringComparer.Ordinal);
        return deleted;
    }

    public static HashSet<string> GetManifestFiles(JsonObject? entry) => entry?.GetArray("files")?
        .OfType<JsonValue>()
        .Select(x => x.TryGetValue<string>(out var file) ? file : null)
        .Where(x => !string.IsNullOrEmpty(x))
        .Select(x => x!)
        .ToHashSet(StringComparer.OrdinalIgnoreCase)
        ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public List<string> GetPublishedNames()
    {
        if (!Directory.Exists(PdfPath))
            return [];

        // A published document can bring other .typ files with it. Those files are flattened beside
        // the entry point (e.g. invoice.v1.typ), but they are implementation dependencies rather than
        // independently published templates. Once a manifest exists it is the authoritative list.
        var manifest = GetManifest();
        var names = manifest.Count > 0
            ? manifest.Select(x => x.Key)
                .Where(x => File.Exists(Path.Combine(PdfPath, x + ".typ")))
            // Preserve browsing/unpublishing support for installations created before manifests.
            : Directory.EnumerateFiles(PdfPath, "*.typ")
                .Select(x => Path.GetFileNameWithoutExtension(x)!)
                .Where(x => !PdfRenderer.ReservedNames.Contains(x, StringComparer.OrdinalIgnoreCase));

        return names
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // ── Collecting a template's files ──

    /// <summary>
    /// Every file the template needs, relative to srcRoot: itself, its companions (data, schema,
    /// stem-prefixed assets) and anything it or its partials reference.
    /// </summary>
    List<string> CollectSources(string srcRoot, string relPath, List<string> warnings)
    {
        var sources = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Add(string rel)
        {
            if (seen.Add(rel))
                sources.Add(rel);
        }

        void Walk(string rel, int depth)
        {
            var fullPath = PdfExtension.Resolve(srcRoot, rel);
            if (!File.Exists(fullPath))
                return;
            Add(rel);

            // the .json/.ui.json/assets that belong to this document
            foreach (var companion in PdfExtension.Companions(srcRoot, rel))
            {
                if (!companion.EndsWith(".typ", StringComparison.OrdinalIgnoreCase))
                    Add(companion);
            }

            if (depth >= MaxDepth || !rel.EndsWith(".typ", StringComparison.OrdinalIgnoreCase))
                return;

            foreach (var reference in FindReferences(File.ReadAllText(fullPath), rel))
            {
                if (reference.Path.StartsWith('@')) // @preview/... package, not a file
                    continue;
                var refPath = PdfExtension.Resolve(srcRoot, reference.Path, mustExist: false);
                if (!File.Exists(refPath))
                {
                    if (seen.Add("!" + reference.Path))
                        warnings.Add($"{rel} references '{reference.Raw}' which doesn't exist");
                    continue;
                }
                if (reference.Path.EndsWith(".typ", StringComparison.OrdinalIgnoreCase))
                    Walk(reference.Path, depth + 1);
                else
                    Add(reference.Path);
            }
        }

        Walk(relPath, 0);
        return sources;
    }

    record Reference(string Raw, string Path);

    /// <summary>
    /// The files a typst source reads, resolved to paths relative to the templates root.
    /// <para>
    /// Only load-data() is root-relative: it hands its argument to json() inside lib.typ, and typst
    /// resolves a path against the file that calls it. Everything else — json(), image(), #import —
    /// is called from this template, so it resolves against this template's folder.
    /// </para>
    /// </summary>
    static List<Reference> FindReferences(string source, string relPath)
    {
        var dir = GetParentDir(relPath);
        var to = new List<Reference>();
        foreach (Match match in ResourceRegex().Matches(source))
        {
            var fn = match.Groups["fn"].Success ? match.Groups["fn"].Value : null;
            var raw = match.Groups["fn"].Success ? match.Groups["arg"].Value : match.Groups["path"].Value;
            if (raw.Length == 0 || raw.StartsWith('@'))
                continue;
            var rootRelative = fn == "load-data";
            to.Add(new Reference(raw, rootRelative ? Normalize(raw) : Normalize(Combine(dir, raw))));
        }
        return to;
    }

    // ── Flattening ──

    /// <summary>What each source file is called once it lands in the flat published folder</summary>
    static Dictionary<string, string> BuildFlatMap(List<string> sources, string relPath, string name)
    {
        var stem = Path.GetFileName(relPath).LeftPart('.');
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var src in sources)
        {
            var fileName = Path.GetFileName(src);
            if (fileName.EqualsIgnoreCase(PdfExtension.LibName))
            {
                map[src] = PdfExtension.LibName;
            }
            else if (fileName.StartsWith(stem + ".", StringComparison.Ordinal))
            {
                // invoice.ui.json keeps everything after the stem
                map[src] = name + fileName[stem.Length..];
            }
            else
            {
                // someone else's file (shared/logo.png): prefix it so two templates can't collide
                map[src] = $"{name}.{fileName}";
            }
        }
        map[relPath] = name + ".typ";
        return map;
    }

    /// <summary>
    /// Point a published template at its flattened files, replacing only the quoted path inside each
    /// reference so prose that happens to name a file is left alone.
    /// </summary>
    static string RewriteReferences(string source, string relPath, Dictionary<string, string> flatMap,
        List<string> warnings)
    {
        var dir = GetParentDir(relPath);
        return ResourceRegex().Replace(source, match =>
        {
            var isCall = match.Groups["fn"].Success;
            var raw = isCall ? match.Groups["arg"].Value : match.Groups["path"].Value;
            if (raw.Length == 0 || raw.StartsWith('@'))
                return match.Value;

            var rootRelative = isCall && match.Groups["fn"].Value == "load-data";
            var srcRel = rootRelative ? Normalize(raw) : Normalize(Combine(dir, raw));
            if (!flatMap.TryGetValue(srcRel, out var flatName))
            {
                warnings.Add($"{relPath} references '{raw}' which wasn't published");
                return match.Value;
            }
            // at the root both conventions are just the file name, so one substitution serves both
            return match.Value.Replace($"\"{raw}\"", $"\"{flatName}\"");
        });
    }

    // ── Paths ──

    public static string GetParentDir(string relPath)
    {
        var path = relPath.Replace('\\', '/');
        var at = path.LastIndexOf('/');
        return at <= 0 ? "" : path[..at];
    }

    static string Combine(string dir, string relPath) =>
        dir.Length == 0 ? relPath : $"{dir}/{relPath}";

    /// <summary>Collapse ./ and ../ segments so a reference matches how the file is listed</summary>
    static string Normalize(string path)
    {
        var segments = new List<string>();
        foreach (var part in path.Replace('\\', '/').Split('/'))
        {
            switch (part)
            {
                case "" or ".":
                    continue;
                case ".." when segments.Count > 0:
                    segments.RemoveAt(segments.Count - 1);
                    break;
                case "..":
                    continue; // above the root, which Resolve() rejects anyway
                default:
                    segments.Add(part);
                    break;
            }
        }
        return string.Join('/', segments);
    }
}
