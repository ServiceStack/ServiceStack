using ServiceStack.Text;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>Bounded authenticated read-only filesystem projection, with physical link containment.</summary>
public static class ProjectsExplorer
{
    public const int TextLimit = 1024 * 1024;
    public const int ImageLimit = 10 * TextLimit;
    static readonly Dictionary<string, string> ImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg",
        [".webp"] = "image/webp", [".gif"] = "image/gif", [".bmp"] = "image/bmp",
        [".avif"] = "image/avif", [".ico"] = "image/x-icon", [".svg"] = "image/svg+xml",
    };

    public static string PhysicalPath(string path, int depth = 0)
    {
        if (depth > 40) throw HttpError.Forbidden("Too many filesystem links");
        var full = Path.GetFullPath(path);
        var root = Path.GetPathRoot(full)!;
        var result = root;
        foreach (var part in full[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            result = Path.Combine(result, part);
            FileSystemInfo info = Directory.Exists(result) ? new DirectoryInfo(result) : new FileInfo(result);
            if (info.LinkTarget != null)
            {
                var target = info.ResolveLinkTarget(false) ?? throw HttpError.Forbidden("Unavailable filesystem link");
                result = PhysicalPath(target.FullName, depth + 1);
            }
            else if (info.Exists && (info.Attributes & FileAttributes.ReparsePoint) != 0)
                throw HttpError.Forbidden("Unsupported filesystem link");
        }
        return Path.TrimEndingDirectorySeparator(result);
    }

    /// <summary>
    /// True when a listed entry physically resolves inside root. A looping, broken or unsupported
    /// link is skipped like an outside link, rather than failing the whole listing.
    /// </summary>
    public static bool IsContained(string path, string root)
    {
        try { return ProjectsExtension.IsWithin(PhysicalPath(path), root); }
        catch (HttpError) { return false; }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    public static JsonObject Browse(IEnumerable<string> allowedRoots, string? selectedPath = null, string? file = null)
    {
        var roots = allowedRoots.Select(x => PhysicalPath(x)).Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal).ToList();
        if (roots.Count == 0) return new JsonObject { ["roots"] = new JsonArray(), ["entries"] = new JsonArray() };
        var path = PhysicalPath(string.IsNullOrEmpty(selectedPath) ? roots[0] : selectedPath);
        var root = roots.FirstOrDefault(r => ProjectsExtension.IsWithin(path, r))
            ?? throw HttpError.Forbidden("Path is outside the workspace");
        if (!Directory.Exists(path)) throw HttpError.NotFound("Directory is unavailable");
        var result = new JsonObject {
            ["roots"] = new JsonArray(roots.Select(x => (JsonNode)JsonValue.Create(x)!).ToArray()),
            ["path"] = path, ["parent"] = path == root ? null : Path.GetDirectoryName(path),
        };
        if (!string.IsNullOrEmpty(file))
        {
            var target = PhysicalPath(file);
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!ProjectsExtension.IsWithin(target, root) || !string.Equals(Path.GetDirectoryName(target), path, comparison))
                throw HttpError.Forbidden("File is outside the selected directory");
            if (!File.Exists(target)) throw HttpError.NotFound("File is unavailable");
            var preview = new JsonObject { ["path"] = target, ["name"] = Path.GetFileName(target) };
            ImageTypes.TryGetValue(Path.GetExtension(target), out var mime);
            var raster = mime != null && mime != "image/svg+xml";
            var limit = raster ? ImageLimit : TextLimit;
            using var stream = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var bytes = new byte[limit + 1];
            var length = 0;
            while (length < bytes.Length)
            {
                var read = stream.Read(bytes, length, bytes.Length - length);
                if (read == 0) break;
                length += read;
            }
            var content = bytes.AsSpan(0, length);
            if (length > limit) preview["message"] = $"File is too large to preview ({limit / TextLimit} MiB limit).";
            else if (!raster && content.Contains((byte)0)) preview["message"] = "Binary file preview is unavailable.";
            else
            {
                if (mime != null)
                {
                    preview["mimeType"] = mime;
                    preview["image"] = $"data:{mime};base64," + Convert.ToBase64String(content);
                }
                if (!raster) preview["content"] = Encoding.UTF8.GetString(content);
            }
            result["file"] = preview;
        }
        var entries = new List<JsonObject>();
        foreach (var item in new DirectoryInfo(path).EnumerateFileSystemInfos())
        {
            if (!IsContained(item.FullName, root)) continue;
            entries.Add(new JsonObject { ["name"] = item.Name, ["path"] = item.FullName, ["directory"] = Directory.Exists(item.FullName) });
            if (entries.Count >= 2000) { result["truncated"] = true; break; }
        }
        result["entries"] = new JsonArray(entries.OrderByDescending(x => x.GetBool("directory"))
            .ThenBy(x => x.GetString("name"), StringComparer.OrdinalIgnoreCase).Select(x => (JsonNode)x).ToArray());
        return result;
    }
}

public partial class ProjectsExtension
{
    /// <summary>
    /// Explorer/Git roots without a project: the user's own workspace. Host-shared
    /// ToolsConfig.AllowedDirectories are only browsable by admins (llms-py's admin fallback).
    /// Never derived from the legacy active-project selection.
    /// </summary>
    public JsonObject ResolveExplorerWorkspace(string? projectId, string? user = null, bool isAdmin = false)
    {
        if (projectId != null) return ResolveWorkspace(projectId, user);
        var roots = new List<string> { Ctx.Feature.GetUserWorkspace(user) };
        if (isAdmin) roots.AddRange(Ctx.Feature.SharedDirectories.Select(Ctx.ResolveDirectory).Where(p => p != null).Select(p => p!));
        return new JsonObject { ["projectId"] = null,
            ["directories"] = new JsonArray(roots.Distinct().Select(p => (JsonNode)JsonValue.Create(p)!).ToArray()) };
    }
    void RegisterExplorerRoutes(ExtensionContext ctx) => ctx.AddGet("explorer", async req =>
    {
        var user = req.AssertUserName();
        if ((req.QueryString("view") ?? "files") != "files") throw HttpError.BadRequest("Unsupported explorer view");
        JsonObject workspace;
        try { workspace = ResolveExplorerWorkspace(string.IsNullOrEmpty(req.QueryString("projectId")) ? null : req.QueryString("projectId"), user, Ctx.IsAdmin(req.Request)); }
        catch (ArgumentException e) { throw HttpError.BadRequest(e.Message); }
        var roots = workspace.GetArray("directories")!.Select(p => p!.GetValue<string>()).ToList();
        try { return await Task.Run(() => ProjectsExplorer.Browse(roots, req.QueryString("path"), req.QueryString("file")), req.Request.RequestAborted).ConfigAwait(); }
        catch (IOException) { throw HttpError.BadRequest("Unable to read this directory"); }
        catch (UnauthorizedAccessException) { throw HttpError.Forbidden("Unable to read this directory"); }
        catch (ArgumentException) { throw HttpError.BadRequest("Invalid workspace path"); }
    });
}
