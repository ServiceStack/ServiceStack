using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Compiling templates with the typst CLI: the mirror dir unsaved buffers are overlaid onto, the
/// compile itself, and the diagnostics the editor underlines with.
/// </summary>
public partial class PdfExtension
{
    // typst --diagnostic-format short, e.g: /path/to/invoice.typ:3:5: error: unknown variable: total
    [GeneratedRegex(@"^(?<file>.+?):(?<line>\d+):(?<col>\d+): (?<severity>error|warning): (?<message>.*)$")]
    private static partial Regex DiagnosticRegex();

    /// <summary>Font families typst can see, keyed by the fonts dir + its mtime</summary>
    readonly Dictionary<string, List<string>> fontsCache = new();

    /// <summary>
    /// Compile relPath with the given unsaved buffers overlaid.
    /// Returns the PDF bytes, or null with the diagnostics that stopped it.
    /// </summary>
    async Task<(byte[]? Pdf, List<JsonObject> Diagnostics)> CompilePdfAsync(string? user, string? relPath,
        JsonObject? overlay, CancellationToken token = default)
    {
        var root = PdfRoot(user);
        Resolve(root, relPath); // validate before touching the mirror
        foreach (var entry in overlay ?? [])
        {
            Resolve(root, entry.Key);
        }

        var mirror = MirrorRoot(user);
        SemaphoreSlim renderLock;
        lock (renderLocks)
        {
            renderLock = renderLocks.GetOrAdd(user ?? "default", _ => new SemaphoreSlim(1, 1));
        }

        byte[] stdout;
        string stderr;
        int exitCode;
        await renderLock.WaitAsync(token).ConfigAwait();
        try
        {
            // mirror the templates dir so relative json()/image()/include paths resolve as they do on
            // disk, then overwrite just the buffers being edited so unsaved changes are what gets compiled
            SyncTree(root, mirror);
            var typPath = Resolve(mirror, relPath);
            foreach (var entry in overlay ?? [])
            {
                if (entry.Value is JsonValue value && value.TryGetValue<string>(out var content))
                    WriteText(Resolve(mirror, entry.Key), content);
            }
            if (!File.Exists(typPath))
                throw new ArgumentException($"'{relPath}' not found");

            var psi = new ProcessStartInfo
            {
                FileName = TypstPath!,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            psi.ArgumentList.Add("compile");
            psi.ArgumentList.Add("--root");
            psi.ArgumentList.Add(mirror);
            psi.ArgumentList.Add(typPath);
            psi.ArgumentList.Add("-"); // stdout
            psi.ArgumentList.Add("-f");
            psi.ArgumentList.Add("pdf");
            psi.ArgumentList.Add("--diagnostic-format");
            psi.ArgumentList.Add("short");

            var fontsDir = Path.Combine(root, "fonts");
            if (Directory.Exists(fontsDir))
            {
                psi.ArgumentList.Add("--font-path");
                psi.ArgumentList.Add(fontsDir);
            }

            Log.LogDebug("Rendering: {Typst} {Args}", TypstPath, string.Join(' ', psi.ArgumentList));
            var result = await RunAsync(psi, RenderTimeout, token).ConfigAwait();
            if (result == null)
            {
                return (null, [Diagnostic($"typst compile timed out after {RenderTimeout.TotalSeconds:0}s")]);
            }
            (stdout, stderr, exitCode) = result.Value;
        }
        finally
        {
            renderLock.Release();
        }

        var diagnostics = ParseDiagnostics(stderr, mirror);
        if (exitCode != 0 || stdout.Length == 0)
        {
            if (!diagnostics.Any(x => x.GetString("severity") == "error"))
                diagnostics.Add(Diagnostic("typst compile failed"));
            return (null, diagnostics);
        }
        return (stdout, diagnostics);
    }

    /// <summary>Run a process to completion, or null if it had to be killed on timeout</summary>
    static async Task<(byte[] Stdout, string Stderr, int ExitCode)?> RunAsync(ProcessStartInfo psi,
        TimeSpan timeout, CancellationToken token)
    {
        using var process = new Process { StartInfo = psi };
        process.Start();

        // typst writes the PDF to stdout, so it has to be read as bytes
        using var stdout = new MemoryStream();
        var stdoutTask = process.StandardOutput.BaseStream.CopyToAsync(stdout, token);
        var stderrTask = process.StandardError.ReadToEndAsync(token);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token).ConfigAwait();
            await stdoutTask.ConfigAwait();
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            try { process.Kill(entireProcessTree: true); } catch (Exception) { /* already exited */ }
            return null;
        }
        return (stdout.ToArray(), await stderrTask.ConfigAwait(), process.ExitCode);
    }

    static JsonObject Diagnostic(string message, string severity = "error") =>
        new() { ["severity"] = severity, ["message"] = message };

    List<JsonObject> ParseDiagnostics(string stderr, string mirror)
    {
        var diagnostics = new List<JsonObject>();
        foreach (var rawLine in stderr.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;

            var match = DiagnosticRegex().Match(line);
            if (!match.Success)
            {
                diagnostics.Add(Diagnostic(line));
                continue;
            }

            // typst reports paths relative to the cwd, map them back to the user's templates folder
            var filePath = Path.GetFullPath(match.Groups["file"].Value);
            if (IsSafePath(mirror, filePath))
                filePath = Path.GetRelativePath(mirror, filePath);

            diagnostics.Add(new JsonObject
            {
                ["file"] = filePath.Replace('\\', '/'),
                ["line"] = int.Parse(match.Groups["line"].Value),
                ["col"] = int.Parse(match.Groups["col"].Value),
                ["severity"] = match.Groups["severity"].Value,
                ["message"] = match.Groups["message"].Value,
            });
        }
        return diagnostics;
    }

    /// <summary>Mirror srcDir into dstDir, copying only what changed and removing what no longer exists</summary>
    static void SyncTree(string srcDir, string dstDir)
    {
        Directory.CreateDirectory(dstDir);
        var srcNames = Directory.EnumerateFileSystemEntries(srcDir)
            .Select(Path.GetFileName)
            .Where(name => name != null && !name.StartsWith('.'))
            .ToHashSet(StringComparer.Ordinal)!;

        foreach (var stale in Directory.EnumerateFileSystemEntries(dstDir))
        {
            if (srcNames.Contains(Path.GetFileName(stale)))
                continue;
            if (Directory.Exists(stale))
                Directory.Delete(stale, recursive: true);
            else
                File.Delete(stale);
        }

        foreach (var name in srcNames)
        {
            var src = Path.Combine(srcDir, name!);
            var dst = Path.Combine(dstDir, name!);
            if (Directory.Exists(src))
            {
                if (File.Exists(dst))
                    File.Delete(dst);
                SyncTree(src, dst);
            }
            else
            {
                if (Directory.Exists(dst))
                    Directory.Delete(dst, recursive: true);
                var srcInfo = new FileInfo(src);
                if (File.Exists(dst))
                {
                    var dstInfo = new FileInfo(dst);
                    if (dstInfo.Length == srcInfo.Length && dstInfo.LastWriteTimeUtc >= srcInfo.LastWriteTimeUtc)
                        continue;
                }
                File.Copy(src, dst, overwrite: true);
            }
        }
    }

    static JsonObject CompileError(List<JsonObject> diagnostics)
    {
        var message = diagnostics.FirstOrDefault(x => x.GetString("severity") == "error")
            ?.GetString("message") ?? "typst compile failed";
        var to = ChatJson.CreateErrorResponse(message, "CompileError");
        to["diagnostics"] = new JsonArray(diagnostics.Select(x => (JsonNode)x.Clone()).ToArray());
        return to;
    }

    /// <summary>Compile a (possibly unsaved) template to PDF and return the raw bytes</summary>
    async Task<object?> RenderAsync(ChatRequestContext req)
    {
        var user = req.AssertUserName();
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        // unsaved buffers to compile with, keyed by their path relative to the templates folder
        var (pdf, diagnostics) = await CompilePdfAsync(user, body.GetString("path"), body.GetObject("files"))
            .ConfigAwait();
        if (pdf == null)
            return ChatResult.Json(CompileError(diagnostics), 400);

        var headers = new Dictionary<string, string> { ["Cache-Control"] = "no-store" };
        var warnings = diagnostics.Where(x => x.GetString("severity") == "warning").ToList();
        if (warnings.Count > 0)
        {
            headers["X-Typst-Warnings"] = new JsonArray(warnings.Select(x => (JsonNode)x.Clone()).ToArray())
                .ToJsonString(ChatJson.Options);
        }
        return new ChatResult { Body = pdf, ContentType = "application/pdf", Headers = headers };
    }

    /// <summary>Families `typst fonts` can see, including anything in the user's own fonts/ folder</summary>
    async Task<object?> ListFontsAsync(ChatRequestContext req)
    {
        var fontDir = Path.Combine(PdfRoot(req.AssertUserName()), "fonts");
        var hasDir = Directory.Exists(fontDir);
        // keyed on the folder's mtime so a font dropped in there shows up without a restart
        var key = hasDir ? $"{fontDir}|{Directory.GetLastWriteTimeUtc(fontDir).Ticks}" : "";

        lock (fontsCache)
        {
            if (fontsCache.TryGetValue(key, out var cached))
                return new JsonObject { ["fonts"] = new JsonArray(cached.Select(x => (JsonNode)x).ToArray()) };
        }

        var psi = new ProcessStartInfo
        {
            FileName = TypstPath!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("fonts");
        if (hasDir)
        {
            psi.ArgumentList.Add("--font-path");
            psi.ArgumentList.Add(fontDir);
        }

        var result = await RunAsync(psi, RenderTimeout, default).ConfigAwait();
        var names = (result == null ? "" : Encoding.UTF8.GetString(result.Value.Stdout))
            .Split('\n')
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct()
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        lock (fontsCache)
        {
            fontsCache[key] = names;
        }
        return new JsonObject { ["fonts"] = new JsonArray(names.Select(x => (JsonNode)x).ToArray()) };
    }
}
