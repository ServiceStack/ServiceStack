using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// PDF Designer (port of llms-py's "pdf" extension): edit typst (.typ) templates with a sidecar
/// .json data file and preview the compiled PDF live.
/// <para>
/// Requires the `typst` CLI (https://typst.app) on PATH, otherwise the extension disables itself.
/// Templates are stored per user in App_Data/chat/user/&lt;user&gt;/pdf.
/// </para>
/// </summary>
public partial class PdfExtension() : ChatExtension("pdf")
{
    /// <summary>A template importing the shared library, e.g. #import "../lib.typ"</summary>
    [GeneratedRegex(@"#(?:import|include)\s+""(?:\.\./)*lib\.typ""")]
    private static partial Regex ImportsLibRegex();

    /// <summary>Path to the typst CLI, resolved on install</summary>
    public string? TypstPath { get; set; }

    /// <summary>How long a single typst compile may run before it's killed</summary>
    public TimeSpan RenderTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Largest PDF the preview may save back into the templates folder</summary>
    public int MaxPdfBytes { get; set; } = 50 * 1024 * 1024;

    /// <summary>Images a template can #image() — typst reads png, jpeg, gif, svg and webp</summary>
    public HashSet<string> AssetExtensions { get; set; } =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp" };

    /// <summary>Largest image that can be uploaded into the templates folder</summary>
    public int MaxAssetBytes { get; set; } = 20 * 1024 * 1024;

    /// <summary>What an uploaded image has to start with: PNG, JPEG, GIF, RIFF (webp), or SVG markup</summary>
    static readonly byte[][] ImageMagic =
    [
        [0x89, (byte)'P', (byte)'N', (byte)'G'],
        [0xff, 0xd8, 0xff],
        "GIF8"u8.ToArray(),
        "RIFF"u8.ToArray(),
        "<svg"u8.ToArray(),
        "<?xml"u8.ToArray(),
    ];

    // shared styles every template imports, plus the document that shows what it does
    public const string LibName = "lib.typ";
    public const string LibPreview = "lib.preview.typ";

    /// <summary>Bundled starter templates, synced from llms-py (chat/ext/pdf/examples)</summary>
    const string ExamplesDir = "chat/ext/pdf/examples";

    /// <summary>Renders are serialised per user: they all compile out of the same mirror dir</summary>
    readonly Dictionary<string, SemaphoreSlim> renderLocks = new();

    public override void Install(ExtensionContext ctx)
    {
        TypstPath ??= ProcessUtils.FindExePath("typst");
        if (string.IsNullOrEmpty(TypstPath))
        {
            Log.LogInformation("typst not found in PATH, PDF Designer disabled");
            ctx.Disabled = true;
            return;
        }
        Log.LogInformation("Using {Typst} for PDF rendering", TypstPath);

        ctx.AddGet("files", req =>
        {
            var root = PdfRoot(req.AssertUserName());
            SeedExamples(root);
            return Task.FromResult<object?>(new JsonObject
            {
                ["path"] = root,
                ["files"] = FileTree(root),
            });
        });

        ctx.AddGet("file", req =>
        {
            var root = PdfRoot(req.AssertUserName());
            var relPath = req.QueryString("path");
            var fullPath = Resolve(root, relPath);
            // the UI asks for resources a template references before they necessarily exist
            if (!File.Exists(fullPath))
                return Task.FromResult<object?>(NotFound(relPath));
            return Task.FromResult<object?>(new JsonObject
            {
                ["path"] = relPath,
                ["content"] = File.ReadAllText(fullPath),
                ["modified"] = Modified(fullPath),
            });
        });

        // serve a referenced resource as-is, for previewing images the template uses
        ctx.AddGet("raw", req =>
        {
            var root = PdfRoot(req.AssertUserName());
            var relPath = req.QueryString("path");
            var fullPath = Resolve(root, relPath);
            return Task.FromResult<object?>(File.Exists(fullPath)
                ? new ChatFileResult(fullPath)
                {
                    ContentType = MimeTypes.GetMimeType(fullPath),
                    Headers = new Dictionary<string, string> { ["Cache-Control"] = "no-store" },
                }
                : NotFound(relPath));
        });

        ctx.AddPost("file", async req =>
        {
            var root = PdfRoot(req.AssertUserName());
            var body = await req.GetJsonBodyAsync().ConfigAwait();
            var relPath = body.GetString("path");
            var content = body.GetString("content")
                ?? throw new ArgumentException("Missing 'content'");
            var fullPath = Resolve(root, relPath);
            WriteText(fullPath, content);
            return new JsonObject { ["path"] = relPath, ["modified"] = Modified(fullPath) };
        });

        ctx.AddPost("pdf", SavePdfAsync);
        ctx.AddPost("asset", UploadAssetAsync);
        ctx.AddPost("create", CreateTemplateAsync);

        ctx.AddPost("folder", async req =>
        {
            var root = PdfRoot(req.AssertUserName());
            var body = await req.GetJsonBodyAsync().ConfigAwait();
            var relPath = body.GetString("path");
            Directory.CreateDirectory(Resolve(root, relPath));
            return new JsonObject { ["path"] = relPath };
        });

        ctx.AddPost("rename", RenameAsync);
        ctx.AddDelete("file", req => Task.FromResult<object?>(Delete(req)));
        ctx.AddPost("render", RenderAsync);
        ctx.AddGet("fonts", ListFontsAsync);
        ctx.AddPost("ai", AiEditAsync);
    }

    // ── Paths ──

    /// <summary>Where a user's templates live</summary>
    public string PdfRoot(string? user)
    {
        var path = Path.Combine(Ctx.GetUserPath(user), "pdf");
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>The scratch copy typst compiles out of, so unsaved buffers can be overlaid</summary>
    string MirrorRoot(string? user)
    {
        var path = Ctx.GetCachePath(Path.Combine("pdf", user ?? "default"));
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>Validate a client supplied relative path and return its absolute path within root</summary>
    public static string Resolve(string root, string? relPath, bool mustExist = false)
    {
        if (string.IsNullOrEmpty(relPath))
            throw new ArgumentException("Missing 'path'");
        relPath = relPath.Replace('\\', '/').TrimStart('/');
        if (IsHidden(relPath))
            throw new ArgumentException("Invalid file path");
        var fullPath = Path.Combine(root, relPath);
        if (!IsSafePath(root, fullPath))
            throw new ArgumentException("Invalid file path");
        if (mustExist && !File.Exists(fullPath) && !Directory.Exists(fullPath))
            throw new ArgumentException($"'{relPath}' not found");
        return fullPath;
    }

    /// <summary>Whether requestedPath resolves to somewhere inside basePath</summary>
    public static bool IsSafePath(string basePath, string requestedPath)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(basePath));
        var target = Path.GetFullPath(requestedPath);
        return target == root || target.StartsWith(root + Path.DirectorySeparatorChar, PathComparison);
    }

    static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    static bool IsHidden(string relPath) => relPath.Replace('\\', '/').Split('/')
        .Any(part => part.Length > 0 && part.StartsWith('.'));

    /// <summary>The &lt;name&gt;.json data file that belongs to a &lt;name&gt;.typ template</summary>
    public static string SidecarPath(string relPath) => Path.ChangeExtension(relPath, ".json");

    /// <summary>
    /// How a template refers to a file: relative to the templates root, with forward slashes.
    /// <para>
    /// Data files are loaded through lib.typ's load-data(), and typst resolves a json() path
    /// relative to the file that calls it — lib.typ at the root — not to the template passing it.
    /// A path relative to the template only works for templates in the root folder.
    /// </para>
    /// </summary>
    static string TypstRef(string relPath) => relPath.Replace('\\', '/').TrimStart('/');

    /// <summary>lib.typ sits at the root, so a template in a subfolder imports it as ../lib.typ</summary>
    static string LibImport(string relPath)
    {
        var dir = Path.GetDirectoryName(relPath.Replace('\\', '/'))?.Trim('/') ?? "";
        var depth = dir.Length == 0 ? 0 : dir.Split('/').Length;
        return string.Concat(Enumerable.Repeat("../", depth)) + LibName;
    }

    static long Modified(string path) => new DateTimeOffset(File.GetLastWriteTimeUtc(path)).ToUnixTimeMilliseconds();

    static void WriteText(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    static ChatResult NotFound(string? relPath) =>
        ChatResult.Json(ChatJson.CreateErrorResponse($"'{relPath}' not found", "NotFound"), 404);

    static ChatResult Conflict(string? relPath) =>
        ChatResult.Json(ChatJson.CreateErrorResponse($"'{relPath}' already exists", "AlreadyExists"), 409);

    // ── Seeding ──

    /// <summary>
    /// The bundled example files, by the name they're written out as.
    /// <para>
    /// Embedded resources use '.' as their path separator, so a flat "invoice.ui.json" is listed by
    /// the VFS as invoice/ui.json — joining each path segment back with '.' restores the filename
    /// the examples were synced under (and which templates import each other by).
    /// </para>
    /// </summary>
    static Dictionary<string, IO.IVirtualFile> BundledExamples()
    {
        var examples = HostContext.VirtualFileSources.GetDirectory(ExamplesDir);
        return examples?.GetAllMatchingFiles("*")
            .ToDictionary(
                file => file.VirtualPath[(ExamplesDir.Length + 1)..].Replace('/', '.'),
                file => file) ?? [];
    }

    /// <summary>Copy the starter templates into an empty pdf folder so there's something to look at</summary>
    void SeedExamples(string root)
    {
        var examples = BundledExamples();
        if (examples.Count == 0)
            return;
        if (Directory.EnumerateFileSystemEntries(root).Any())
        {
            SeedLibrary(root, examples);
            return;
        }
        foreach (var (name, file) in examples)
        {
            File.WriteAllBytes(Path.Combine(root, name), file.ReadAllBytes());
        }
        Log.LogInformation("Seeded example templates in {Path}", root);
    }

    /// <summary>
    /// Templates don't compile without the library they import, so put it back if it goes missing —
    /// but only while something still imports it. Renaming lib.typ shouldn't conjure up a second copy.
    /// </summary>
    void SeedLibrary(string root, Dictionary<string, IO.IVirtualFile> examples)
    {
        if (File.Exists(Path.Combine(root, LibName)))
            return;
        if (!AllTemplates(root).Any(rel => ImportsLibRegex().IsMatch(File.ReadAllText(Path.Combine(root, rel)))))
            return;
        foreach (var name in new[] { LibName, LibPreview })
        {
            if (examples.TryGetValue(name, out var file))
                File.WriteAllBytes(Path.Combine(root, name), file.ReadAllBytes());
        }
        Log.LogInformation("Restored {Lib} in {Path}", LibName, root);
    }

    // ── File tree ──

    static JsonArray FileTree(string dirPath, string relPrefix = "")
    {
        var nodes = new List<JsonObject>();
        foreach (var fullPath in Directory.EnumerateFileSystemEntries(dirPath).OrderBy(x => x, StringComparer.Ordinal))
        {
            var name = Path.GetFileName(fullPath);
            if (name.StartsWith('.'))
                continue;
            var relPath = $"{relPrefix}{name}";
            if (Directory.Exists(fullPath))
            {
                nodes.Add(new JsonObject
                {
                    ["name"] = name,
                    ["path"] = relPath,
                    ["isFile"] = false,
                    ["children"] = FileTree(fullPath, $"{relPath}/"),
                });
            }
            else
            {
                var info = new FileInfo(fullPath);
                nodes.Add(new JsonObject
                {
                    ["name"] = name,
                    ["path"] = relPath,
                    ["isFile"] = true,
                    ["ext"] = Path.GetExtension(name).ToLower(),
                    ["size"] = info.Length,
                    ["modified"] = new DateTimeOffset(info.LastWriteTimeUtc).ToUnixTimeMilliseconds(),
                });
            }
        }
        // folders first, then files, each alphabetically
        return new JsonArray(nodes
            .OrderBy(x => x.GetBool("isFile"))
            .ThenBy(x => x.GetString("name"), StringComparer.OrdinalIgnoreCase)
            .Cast<JsonNode>().ToArray());
    }

    // ── Routes ──

    /// <summary>Store the exact PDF bytes the preview produced</summary>
    async Task<object?> SavePdfAsync(ChatRequestContext req)
    {
        var root = PdfRoot(req.AssertUserName());
        var relPath = req.QueryString("path");
        if (relPath == null || !relPath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Path must be a .pdf");
        var fullPath = Resolve(root, relPath);

        var data = await req.Request.InputStream.ReadFullyAsync().ConfigAwait();
        if (data.Length < 4 || Encoding.ASCII.GetString(data, 0, 4) != "%PDF")
            throw new ArgumentException("That isn't a PDF");
        if (data.Length > MaxPdfBytes)
            throw new ArgumentException($"PDF is too large: {data.Length / 1024}KB");

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, data).ConfigAwait();
        return new JsonObject
        {
            ["path"] = relPath,
            ["size"] = data.Length,
            ["modified"] = Modified(fullPath),
        };
    }

    /// <summary>Store an uploaded image in the templates folder so templates can #image() it</summary>
    async Task<object?> UploadAssetAsync(ChatRequestContext req)
    {
        var root = PdfRoot(req.AssertUserName());
        var relPath = req.QueryString("path") ?? "";
        var ext = Path.GetExtension(relPath).ToLower();
        if (!AssetExtensions.Contains(ext))
            throw new ArgumentException(
                $"'{(ext.Length > 0 ? ext : relPath)}' isn't a supported image " +
                $"({string.Join(", ", AssetExtensions.OrderBy(x => x, StringComparer.Ordinal))})");

        var fullPath = Resolve(root, relPath);
        if (File.Exists(fullPath) || Directory.Exists(fullPath))
            return Conflict(relPath);

        var data = await req.Request.InputStream.ReadFullyAsync().ConfigAwait();
        if (data.Length == 0)
            throw new ArgumentException("No image data");
        if (data.Length > MaxAssetBytes)
            throw new ArgumentException(
                $"Image is too large: {data.Length / 1024}KB, at most {MaxAssetBytes / 1024}KB");
        // SVG is text, so it's the extension that vouches for it; everything else has to look the part
        if (ext != ".svg" && !LooksLikeImage(data))
            throw new ArgumentException("That doesn't look like an image");

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, data).ConfigAwait();
        return new JsonObject { ["path"] = relPath, ["size"] = data.Length };
    }

    static bool LooksLikeImage(byte[] data) =>
        ImageMagic.Any(magic => data.Length >= magic.Length && data.AsSpan(0, magic.Length).SequenceEqual(magic));

    /// <summary>Create a new .typ template (and optionally its .json sidecar) from the starter example</summary>
    async Task<object?> CreateTemplateAsync(ChatRequestContext req)
    {
        var root = PdfRoot(req.AssertUserName());
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var relPath = body.GetString("path") ?? "";
        if (!relPath.EndsWith(".typ", StringComparison.OrdinalIgnoreCase))
            relPath += ".typ";
        var fullPath = Resolve(root, relPath);
        if (File.Exists(fullPath) || Directory.Exists(fullPath))
            return Conflict(relPath);

        var name = Path.GetFileNameWithoutExtension(relPath);
        var relData = SidecarPath(relPath);
        var withData = body.GetBool("withData", defaultValue: true);
        if (withData)
        {
            var title = name.Replace('-', ' ').Replace('_', ' ').ToTitleCase();
            WriteText(Resolve(root, relData), new JsonObject
            {
                ["title"] = title,
                ["body"] = "Hello, World!",
            }.ToJsonString(ChatJson.Indented) + "\n");
            WriteText(fullPath,
                $"""
                 #import "{LibImport(relPath)}": *

                 #let data = load-data("{TypstRef(relData)}")

                 #show: theme

                 #title-block(data.title)

                 #data.body

                 """);
        }
        else
        {
            WriteText(fullPath,
                $"""
                 #import "{LibImport(relPath)}": *

                 #show: theme

                 #title-block("{name}")

                 Hello, World!

                 """);
        }

        return new JsonObject { ["path"] = relPath, ["data"] = withData ? relData : null };
    }

    /// <summary>Siblings that belong to the same document: invoice.json, invoice.ui.json, lib.preview.typ</summary>
    public static List<string> Companions(string root, string relPath)
    {
        var fileName = Path.GetFileName(relPath);
        var relDir = ParentDir(relPath);
        var fullDir = Path.GetDirectoryName(Resolve(root, relPath));
        if (fullDir == null || !Directory.Exists(fullDir))
            return [];

        var stem = fileName.LeftPart('.');
        return Directory.EnumerateFiles(fullDir)
            .Select(Path.GetFileName)
            .Where(name => name != fileName && name!.StartsWith(stem + ".", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => relDir.Length > 0 ? $"{relDir}/{name}" : name!)
            .ToList();
    }

    /// <summary>Every .typ in the folder, relative to it</summary>
    public static List<string> AllTemplates(string root)
    {
        var to = new List<string>();
        Walk(root, "");
        to.Sort(StringComparer.Ordinal);
        return to;

        void Walk(string dir, string prefix)
        {
            foreach (var file in Directory.EnumerateFiles(dir, "*.typ"))
            {
                to.Add(prefix + Path.GetFileName(file));
            }
            foreach (var subDir in Directory.EnumerateDirectories(dir))
            {
                var name = Path.GetFileName(subDir);
                if (!name.StartsWith('.'))
                    Walk(subDir, $"{prefix}{name}/");
            }
        }
    }

    static string ParentDir(string relPath)
    {
        var path = relPath.Replace('\\', '/');
        var at = path.LastIndexOf('/');
        return at <= 0 ? "" : path[..at];
    }

    /// <summary>
    /// Point a template at renamed files: json("invoice.json"), #import "lib.typ".
    /// <para>
    /// The renamed template gets every quoted reference updated — its own data and schema. Other
    /// templates only get their #import/#include lines touched, since a mention of another
    /// document's data file is almost always an example in a comment rather than a real reference.
    /// </para>
    /// </summary>
    static bool RetargetReferences(string root, string relPath, List<(string From, string To)> renames,
        bool importsOnly = false)
    {
        var fullPath = Resolve(root, relPath);
        var text = File.ReadAllText(fullPath);
        var updated = text;

        foreach (var (oldRel, newRel) in renames)
        {
            var refs = new List<(string Old, string New)> { (TypstRef(oldRel), TypstRef(newRel)) };
            var (oldName, newName) = (Path.GetFileName(oldRel), Path.GetFileName(newRel));
            if (oldName != refs[0].Old)
                refs.Add((oldName, newName));

            foreach (var (oldRef, newRef) in refs)
            {
                if (oldRef == newRef)
                    continue;
                updated = importsOnly
                    ? Regex.Replace(updated,
                        @"(#(?:import|include)\s+"")" + Regex.Escape(oldRef) + @"("")",
                        "$1" + newRef.Replace("$", "$$") + "$2")
                    // only inside a string literal, so prose that happens to say the name is left alone
                    : updated.Replace($"\"{oldRef}\"", $"\"{newRef}\"");
            }
        }

        if (updated == text)
            return false;
        WriteText(fullPath, updated);
        return true;
    }

    /// <summary>Rename a template along with the files that belong to it, fixing the references between them</summary>
    async Task<object?> RenameAsync(ChatRequestContext req)
    {
        var root = PdfRoot(req.AssertUserName());
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var relFrom = body.GetString("from");
        var relTo = body.GetString("to");
        Resolve(root, relFrom, mustExist: true); // validates it exists and stays inside the folder
        var toPath = Resolve(root, relTo);
        if (File.Exists(toPath) || Directory.Exists(toPath))
            return Conflict(relTo);

        var renames = new List<(string From, string To)> { (relFrom!, relTo!) };
        if (relFrom!.EndsWith(".typ", StringComparison.OrdinalIgnoreCase)
            && relTo!.EndsWith(".typ", StringComparison.OrdinalIgnoreCase))
        {
            var toStem = Path.GetFileName(relTo)[..^".typ".Length];
            var toDir = ParentDir(relTo);
            var fromStemLength = Path.GetFileName(relFrom).LeftPart('.').Length;
            foreach (var relOther in Companions(root, relFrom))
            {
                // invoice.ui.json keeps everything after the stem, so .ui.json and .preview.typ survive
                var suffix = Path.GetFileName(relOther)[fromStemLength..];
                var relOtherTo = toDir.Length > 0 ? $"{toDir}/{toStem}{suffix}" : toStem + suffix;
                var otherToPath = Resolve(root, relOtherTo);
                if (!File.Exists(otherToPath) && !Directory.Exists(otherToPath))
                    renames.Add((relOther, relOtherTo));
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(toPath)!);
        foreach (var (oldRel, newRel) in renames)
        {
            var from = Resolve(root, oldRel);
            var to = Resolve(root, newRel);
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            if (Directory.Exists(from))
                Directory.Move(from, to);
            else
                File.Move(from, to);
        }

        // the renamed templates in full, every other one's imports — so a renamed lib.typ doesn't
        // break the documents importing it
        var renamedPaths = renames.Select(x => x.To).ToHashSet(StringComparer.Ordinal);
        var updated = AllTemplates(root)
            .Where(rel => RetargetReferences(root, rel, renames, importsOnly: !renamedPaths.Contains(rel)))
            .ToList();

        return new JsonObject
        {
            ["path"] = relTo,
            ["data"] = renames
                .Where(x => x.To.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                            && !x.To.EndsWith(CoreToolsExtension.SchemaSuffix, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.To)
                .FirstOrDefault(),
            ["renamed"] = new JsonArray(renames
                .Select(x => (JsonNode)new JsonObject { ["from"] = x.From, ["to"] = x.To }).ToArray()),
            ["updated"] = new JsonArray(updated.Select(x => (JsonNode)x).ToArray()),
        };
    }

    object Delete(ChatRequestContext req)
    {
        var root = PdfRoot(req.AssertUserName());
        var relPath = req.QueryString("path");
        var fullPath = Resolve(root, relPath, mustExist: true);
        var deleted = new JsonArray(relPath!);

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
        }
        else
        {
            File.Delete(fullPath);
            // the whole document goes with it: its data, schema, preview, any <stem>.* image
            if (req.QueryString("sidecar") == "true" && relPath!.EndsWith(".typ", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var relOther in Companions(root, relPath))
                {
                    var other = Resolve(root, relOther);
                    if (File.Exists(other))
                    {
                        File.Delete(other);
                        deleted.Add(relOther);
                    }
                }
            }
        }

        PruneEmptyDirs(root, Path.GetDirectoryName(fullPath));
        return new JsonObject { ["deleted"] = deleted };
    }

    static void PruneEmptyDirs(string root, string? dir)
    {
        while (dir != null && IsSafePath(root, dir)
               && Path.GetFullPath(dir) != Path.GetFullPath(root)
               && Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
        {
            Directory.Delete(dir);
            dir = Path.GetDirectoryName(dir);
        }
    }
}
