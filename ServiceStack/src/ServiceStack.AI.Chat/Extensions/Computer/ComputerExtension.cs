using Microsoft.Extensions.FileSystemGlobbing;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Filesystem + shell tools (port of llms-py's "computer" extension, itself a port of Anthropic's
/// Filesystem MCP server). All paths are validated against the user's resolved allowed directories.
/// Both groups are OFF by default: "filesystem" requires ToolsConfig.EnableFilesystemTools and
/// "computer" (run_bash) requires ToolsConfig.EnableCodeExecution.
/// The desktop-only tools (open, screen control, Anthropic editor) are not ported — they don't
/// apply to a web host; edit_file covers file editing.
/// </summary>
public class ComputerExtension() : ChatExtension("computer")
{
    public override void Install(ExtensionContext ctx)
    {
        if (ctx.Tools.EnableCodeExecution)
        {
            ctx.RegisterTool(Def("run_bash", "A tool that allows the agent to run bash commands.",
                    new JsonObject
                    {
                        ["command"] = new JsonObject { ["type"] = "string", ["description"] = "Command to run" },
                    }, ["command"]),
                async (args, c) => await RunBashAsync(args.GetString("command") ?? "", c).ConfigAwait(),
                "computer");
        }

        if (!ctx.Tools.EnableFilesystemTools)
            return;

        const string fs = "filesystem";

        ctx.RegisterTool(Def("list_allowed_directories",
                "Returns the list of directories that this server is allowed to access. Subdirectories within these allowed directories are also accessible. " +
                "Use this to understand which directories and their nested paths are available before trying to access files.",
                new JsonObject()),
            (args, c) => Result("Allowed directories:\n" + string.Join("\n", AllowedDirs(args, c))), fs);

        ctx.RegisterTool(Def("read_text_file",
                "Read the complete contents of a file from the file system as text. Use the 'head' parameter to read only the first N lines of a file, or the 'tail' parameter to read only the last N lines of a file. Only works within allowed directories.",
                new JsonObject
                {
                    ["path"] = Str("Path to the file."),
                    ["head"] = Int("If provided, returns only the first N lines of the file"),
                    ["tail"] = Int("If provided, returns only the last N lines of the file"),
                }, ["path"]),
            (args, c) => Result(ReadTextFile(ValidatePath(args, c), args.GetInt("head"), args.GetInt("tail"))), fs);

        ctx.RegisterTool(Def("read_media_file",
                "Read an image or audio file. Returns the base64 encoded data and MIME type. Only works within allowed directories.",
                new JsonObject { ["path"] = Str("Path to the file") }, ["path"]),
            (args, c) => Result(ReadMediaFile(ValidatePath(args, c))), fs);

        ctx.RegisterTool(Def("read_multiple_files",
                "Read the contents of multiple files simultaneously. Each file's content is returned with its path as a reference. Failed reads for individual files won't stop the entire operation. Only works within allowed directories.",
                new JsonObject
                {
                    ["paths"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject { ["type"] = "string" },
                        ["description"] = "List of file paths to read",
                    },
                }, ["paths"]),
            (args, c) => Result(ReadMultipleFiles(args, c)), fs);

        ctx.RegisterTool(Def("write_file",
                "Create a new file or completely overwrite an existing file with new content. Use with caution as it will overwrite existing files without warning. Only works within allowed directories.",
                new JsonObject
                {
                    ["path"] = Str("Path to the file"),
                    ["content"] = Str("Content to write"),
                }, ["path", "content"]),
            (args, c) =>
            {
                var path = ValidatePath(args, c);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, args.GetString("content") ?? "");
                return Result($"Successfully wrote to {args.GetString("path")}");
            }, fs);

        ctx.RegisterTool(Def("edit_file",
                "Make line-based edits to a text file. Each edit replaces exact text sequences with new content. Returns a git-style diff showing the changes made. Only works within allowed directories.",
                new JsonObject
                {
                    ["path"] = Str("Path to the file"),
                    ["edits"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JsonObject
                            {
                                ["oldText"] = new JsonObject { ["type"] = "string" },
                                ["newText"] = new JsonObject { ["type"] = "string" },
                            },
                        },
                        ["description"] = "List of dicts with 'oldText' and 'newText'",
                    },
                    ["dry_run"] = new JsonObject { ["type"] = "boolean", ["default"] = false },
                }, ["path", "edits"]),
            (args, c) => Result(EditFile(args, c)), fs);

        ctx.RegisterTool(Def("create_directory",
                "Create a new directory or ensure a directory exists. Can create multiple nested directories in one operation. If the directory already exists, this operation will succeed silently. Only works within allowed directories.",
                new JsonObject { ["path"] = Str("Path to the directory") }, ["path"]),
            (args, c) =>
            {
                Directory.CreateDirectory(ValidatePath(args, c));
                return Result($"Successfully created directory {args.GetString("path")}");
            }, fs);

        ctx.RegisterTool(Def("list_directory",
                "Get a detailed listing of all files and directories in a specified path. Results clearly distinguish between files and directories with [FILE] and [DIR] prefixes. Only works within allowed directories.",
                new JsonObject { ["path"] = Str("Path to the directory") }, ["path"]),
            (args, c) => Result(ListDirectory(ValidatePath(args, c))), fs);

        ctx.RegisterTool(Def("list_directory_with_sizes",
                "Get a detailed listing of all files and directories in a specified path, including sizes. Only works within allowed directories.",
                new JsonObject
                {
                    ["path"] = Str("Path to the directory"),
                    ["sort_by"] = new JsonObject
                    {
                        ["type"] = "string", ["enum"] = new JsonArray("name", "size"), ["default"] = "name",
                        ["description"] = "Sort by name or size",
                    },
                }, ["path"]),
            (args, c) => Result(ListDirectoryWithSizes(ValidatePath(args, c), args.GetString("sort_by") ?? "name")), fs);

        ctx.RegisterTool(Def("directory_tree",
                "Get a recursive tree view of files and directories as a JSON structure. Only works within allowed directories.",
                new JsonObject
                {
                    ["path"] = Str("Path to the root directory"),
                    ["max_depth"] = Int("Maximum depth to traverse"),
                }, ["path"]),
            (args, c) => Result(DirectoryTree(ValidatePath(args, c), args.GetInt("max_depth") ?? 5)
                .ToJsonString(ChatJson.Indented)), fs);

        ctx.RegisterTool(Def("move_file",
                "Move or rename files and directories. If the destination exists, the operation will fail. Both source and destination must be within allowed directories.",
                new JsonObject
                {
                    ["source"] = Str("Source path"),
                    ["destination"] = Str("Destination path"),
                }, ["source", "destination"]),
            (args, c) =>
            {
                var source = ValidatePath(args.GetString("source"), args, c);
                var destination = ValidatePath(args.GetString("destination"), args, c);
                if (File.Exists(destination) || Directory.Exists(destination))
                    throw new Exception($"Destination already exists: {args.GetString("destination")}");
                if (Directory.Exists(source))
                    Directory.Move(source, destination);
                else
                    File.Move(source, destination);
                return Result($"Successfully moved {args.GetString("source")} to {args.GetString("destination")}");
            }, fs);

        ctx.RegisterTool(Def("search_files",
                "Recursively search for files and directories matching a glob pattern. Returns full paths to all matching items. Only searches within allowed directories. If no path is provided, searches in the first allowed directory.",
                new JsonObject
                {
                    ["pattern"] = Str("Glob pattern to match"),
                    ["path"] = Str("Path to search in"),
                    ["max_results"] = new JsonObject { ["type"] = "integer", ["default"] = 200 },
                }, ["pattern"]),
            (args, c) => Result(SearchFiles(args, c)), fs);

        ctx.RegisterTool(Def("get_file_info",
                "Retrieve detailed metadata about a file or directory: size, timestamps and type. Only works within allowed directories.",
                new JsonObject { ["path"] = Str("Path to the file") }, ["path"]),
            (args, c) => Result(GetFileInfo(ValidatePath(args, c))), fs);
    }

    // ── Helpers ──

    static Task<object?> Result(object? value) => Task.FromResult(value);

    static JsonObject Str(string description) => new() { ["type"] = "string", ["description"] = description };
    static JsonObject Int(string description) => new() { ["type"] = "integer", ["description"] = description };

    static JsonObject Def(string name, string description, JsonObject properties, string[]? required = null)
    {
        // tools declaring "user" receive the authenticated username (partition for allowed dirs)
        properties["user"] = new JsonObject { ["type"] = "string" };
        var parameters = new JsonObject { ["type"] = "object", ["properties"] = properties };
        if (required is { Length: > 0 })
            parameters["required"] = new JsonArray(required.Select(x => (JsonNode)x).ToArray());
        return new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = name,
                ["description"] = description,
                ["parameters"] = parameters,
            },
        };
    }

    List<string> AllowedDirs(JsonObject args, ChatContext c) =>
        Ctx.ResolveAllowedDirectories(args.GetString("user") ?? c.User);

    string ValidatePath(JsonObject args, ChatContext c) => ValidatePath(args.GetString("path"), args, c);

    /// <summary>Resolve + verify a path is inside an allowed directory (port of _validate_path)</summary>
    string ValidatePath(string? pathStr, JsonObject args, ChatContext c)
    {
        if (string.IsNullOrEmpty(pathStr))
            throw new ArgumentException("Path cannot be empty");
        if (pathStr.StartsWith("~/"))
            pathStr = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), pathStr[2..]);

        var absPath = Path.GetFullPath(pathStr);
        var allowedDirs = AllowedDirs(args, c);
        foreach (var allowedDir in allowedDirs)
        {
            var dirWithSep = allowedDir.EndsWith(Path.DirectorySeparatorChar)
                ? allowedDir
                : allowedDir + Path.DirectorySeparatorChar;
            if (absPath.StartsWith(dirWithSep, StringComparison.Ordinal) || absPath == allowedDir)
                return absPath;
        }
        throw new UnauthorizedAccessException(
            $"Access denied: {absPath} is not within allowed directories:\n{string.Join(", ", allowedDirs)}");
    }

    static string ReadTextFile(string path, int? head, int? tail)
    {
        if (head != null && tail != null)
            throw new ArgumentException("Cannot specify both head and tail parameters simultaneously");
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");

        if (head != null)
            return string.Join("\n", File.ReadLines(path).Take(head.Value));
        if (tail != null)
            return string.Join("\n", File.ReadLines(path).TakeLast(tail.Value));
        return File.ReadAllText(path);
    }

    static JsonObject ReadMediaFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");
        var mimeType = MimeTypes.GetMimeType(path);
        var fileType = mimeType.StartsWith("image/") ? "image"
            : mimeType.StartsWith("audio/") ? "audio"
            : "blob";
        return new JsonObject
        {
            ["type"] = fileType,
            ["data"] = Convert.ToBase64String(File.ReadAllBytes(path)),
            ["mimeType"] = mimeType,
        };
    }

    string ReadMultipleFiles(JsonObject args, ChatContext c)
    {
        var results = new List<string>();
        foreach (var p in args.GetArray("paths") ?? [])
        {
            var path = p?.GetValue<string>();
            if (path == null)
                continue;
            try
            {
                results.Add($"{path}:\n{ReadTextFile(ValidatePath(path, args, c), null, null)}\n");
            }
            catch (Exception e)
            {
                results.Add($"{path}: Error - {e.Message}");
            }
        }
        return string.Join("\n---\n", results);
    }

    string EditFile(JsonObject args, ChatContext c)
    {
        var path = ValidatePath(args, c);
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");

        var originalContent = File.ReadAllText(path);
        var currentContent = originalContent;

        foreach (var editNode in args.GetArray("edits") ?? [])
        {
            if (editNode is not JsonObject edit)
                continue;
            var oldText = edit.GetString("oldText") ?? "";
            var newText = edit.GetString("newText") ?? "";
            var idx = currentContent.IndexOf(oldText, StringComparison.Ordinal);
            if (idx < 0)
                throw new Exception($"Could not find exact match for text to replace: {oldText.SafeSubstring(0, 50)}...");
            currentContent = currentContent[..idx] + newText + currentContent[(idx + oldText.Length)..];
        }

        var diff = UnifiedDiff(originalContent, currentContent, args.GetString("path") ?? path);
        if (!args.GetBool("dry_run"))
        {
            File.WriteAllText(path, currentContent);
        }
        return diff;
    }

    /// <summary>Single-hunk unified diff via common prefix/suffix trim — edits are localized replacements</summary>
    static string UnifiedDiff(string original, string updated, string path)
    {
        if (original == updated)
            return "";
        var a = original.Split('\n');
        var b = updated.Split('\n');

        var prefix = 0;
        while (prefix < a.Length && prefix < b.Length && a[prefix] == b[prefix])
            prefix++;
        var suffix = 0;
        while (suffix < a.Length - prefix && suffix < b.Length - prefix
            && a[^(suffix + 1)] == b[^(suffix + 1)])
            suffix++;

        var sb = new StringBuilder();
        sb.Append($"--- a/{path}\n+++ b/{path}\n");
        var aLen = a.Length - prefix - suffix;
        var bLen = b.Length - prefix - suffix;
        sb.Append($"@@ -{prefix + 1},{aLen} +{prefix + 1},{bLen} @@\n");
        for (var i = prefix; i < a.Length - suffix; i++)
            sb.Append($"-{a[i]}\n");
        for (var i = prefix; i < b.Length - suffix; i++)
            sb.Append($"+{b[i]}\n");
        return sb.ToString();
    }

    static string ListDirectory(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        var lines = Directory.EnumerateFileSystemEntries(path)
            .OrderBy(x => Path.GetFileName(x), StringComparer.Ordinal)
            .Select(entry => Directory.Exists(entry)
                ? $"[DIR] {Path.GetFileName(entry)}"
                : $"[FILE] {Path.GetFileName(entry)}");
        return string.Join("\n", lines);
    }

    static string ListDirectoryWithSizes(string path, string sortBy)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");

        var entries = new DirectoryInfo(path).EnumerateFileSystemInfos()
            .Select(info => (info.Name, IsDir: info is DirectoryInfo, Size: info is FileInfo f ? f.Length : 0))
            .ToList();
        entries = sortBy == "size"
            ? entries.OrderByDescending(x => x.Size).ToList()
            : entries.OrderBy(x => x.Name, StringComparer.Ordinal).ToList();

        var lines = new List<string>();
        long totalSize = 0;
        int totalFiles = 0, totalDirs = 0;
        foreach (var e in entries)
        {
            var prefix = e.IsDir ? "[DIR] " : "[FILE]";
            var sizeStr = e.IsDir ? "" : FormatSize(e.Size).PadLeft(10);
            lines.Add($"{prefix} {e.Name,-30} {sizeStr}");
            if (e.IsDir) totalDirs++;
            else { totalFiles++; totalSize += e.Size; }
        }
        lines.Add("");
        lines.Add($"Total: {totalFiles} files, {totalDirs} directories");
        lines.Add($"Combined size: {FormatSize(totalSize)}");
        return string.Join("\n", lines);
    }

    static string FormatSize(long bytes)
    {
        double size = bytes;
        foreach (var unit in new[] { "B", "KB", "MB", "GB", "TB" })
        {
            if (size < 1024)
                return $"{size:0.0} {unit}";
            size /= 1024;
        }
        return $"{size:0.0} PB";
    }

    static JsonArray DirectoryTree(string path, int maxDepth)
    {
        var tree = new JsonArray();
        if (maxDepth <= 0 || !Directory.Exists(path))
            return tree;
        foreach (var entry in Directory.EnumerateFileSystemEntries(path).OrderBy(Path.GetFileName, StringComparer.Ordinal))
        {
            var name = Path.GetFileName(entry);
            if (Directory.Exists(entry))
            {
                tree.Add(new JsonObject
                {
                    ["name"] = name,
                    ["type"] = "directory",
                    ["children"] = DirectoryTree(entry, maxDepth - 1),
                });
            }
            else
            {
                tree.Add(new JsonObject { ["name"] = name, ["type"] = "file" });
            }
        }
        return tree;
    }

    string SearchFiles(JsonObject args, ChatContext c)
    {
        var pattern = args.GetString("pattern") ?? "*";
        var searchPath = args.GetString("path");
        if (string.IsNullOrEmpty(searchPath))
        {
            searchPath = AllowedDirs(args, c).FirstOrDefault()
                ?? throw new Exception("No allowed directories configured");
        }
        var validPath = ValidatePath(searchPath, args, c);
        var maxResults = args.GetInt("max_results") ?? 200;

        var matcher = new Matcher();
        matcher.AddInclude(pattern.Contains('/') ? pattern : $"**/{pattern}");
        var results = matcher.GetResultsInFullPath(validPath).Take(maxResults).OrderBy(x => x, StringComparer.Ordinal).ToList();
        return results.Count == 0 ? "No matches found" : string.Join("\n", results);
    }

    static string GetFileInfo(string path)
    {
        var isDir = Directory.Exists(path);
        var isFile = File.Exists(path);
        if (!isDir && !isFile)
            throw new FileNotFoundException($"Path not found: {path}");

        FileSystemInfo info = isDir ? new DirectoryInfo(path) : new FileInfo(path);
        string FormatDate(DateTime d) => d.ToString("ddd MMM dd yyyy HH:mm:ss");
        return string.Join("\n",
        [
            $"size: {(info as FileInfo)?.Length ?? 0}",
            $"created: {FormatDate(info.CreationTime)}",
            $"modified: {FormatDate(info.LastWriteTime)}",
            $"accessed: {FormatDate(info.LastAccessTime)}",
            $"isDirectory: {(isDir ? "true" : "false")}",
            $"isFile: {(isFile ? "true" : "false")}",
        ]);
    }

    /// <summary>
    /// Run a bash command (fresh shell per invocation — unlike Python's persistent Anthropic
    /// bash session, which doesn't suit a multi-user web host). Runs in the first allowed
    /// directory when one is configured.
    /// </summary>
    async Task<object?> RunBashAsync(string command, ChatContext c)
    {
        if (string.IsNullOrEmpty(command))
            throw new ArgumentException("no command provided.");
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            throw new NotSupportedException("run_bash is only supported on Linux/macOS hosts");

        var cwd = Ctx.ResolveAllowedDirectories(c.User).FirstOrDefault()
            ?? Ctx.GetUserPath(c.User);
        Directory.CreateDirectory(cwd);

        var psi = new ProcessStartInfo
        {
            FileName = "bash",
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add(command);

        using var process = new Process();
        process.StartInfo = psi;
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(c.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(120));
        try
        {
            await process.WaitForExitAsync(cts.Token).ConfigAwait();
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch (Exception) { /* already exited */ }
            throw new TimeoutException("bash command timed out");
        }

        var stdout = await stdoutTask.ConfigAwait();
        var stderr = await stderrTask.ConfigAwait();
        var output = stdout;
        if (stderr.Length > 0)
            output += (output.Length > 0 ? "\n" : "") + stderr;
        return output;
    }
}
